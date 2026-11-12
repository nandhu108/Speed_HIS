using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Admin.Settings;
using Prixo.Application.Features.Finance;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Billing;
using Prixo.Domain.Entities.Finance;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Entities.Quality;
using Prixo.Domain.Entities.Reports;
using Prixo.Domain.Enums;

namespace Prixo.Infrastructure.Persistence;

/// <summary>
/// Idempotent seed keyed by stable codes — safe to run at every startup. Creates the
/// permission catalogue, MAIN branch, system roles, admin user, menus and settings.
/// </summary>
public class DbSeeder(
    AppDbContext db,
    IPasswordHasherService hasher,
    IConfiguration configuration,
    ILogger<DbSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedPermissionsAsync(ct);
        var mainBranch = await SeedMainBranchAsync(ct);
        var superAdmin = await SeedRolesAsync(ct);
        await SeedAdminUserAsync(mainBranch, superAdmin, ct);
        await SeedMenusAsync(ct);
        await SeedFrontOfficeMenusAsync(ct);
        await SeedSettingsAsync(ct);
        await SeedMasterDefaultsAsync(ct);
        await SeedConsentTemplatesAsync(ct);
        await SeedRetentionPoliciesAsync(ct);
        await SeedQualityIndicatorsAsync(ct);
        await SeedFinanceDefaultsAsync(ct);
        await SeedDefaultStoreAsync(mainBranch, ct);
        await SeedDefaultTheatreAsync(mainBranch, ct);
        await SeedReportDefinitionsAsync(ct);
        logger.LogInformation("Database seed completed");
    }

    private async Task SeedPermissionsAsync(CancellationToken ct)
    {
        var existing = await db.Permissions.ToDictionaryAsync(p => p.Code, ct);
        foreach (var definition in PermissionRegistry.All)
        {
            if (existing.TryGetValue(definition.Code, out var permission))
            {
                permission.Description = definition.Description;
            }
            else
            {
                db.Permissions.Add(new Permission
                {
                    Code = definition.Code,
                    Module = definition.Module,
                    Resource = definition.Resource,
                    Action = definition.Action,
                    Description = definition.Description,
                });
            }
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task<Branch> SeedMainBranchAsync(CancellationToken ct)
    {
        var branch = await db.Branches.FirstOrDefaultAsync(b => b.Code == "MAIN", ct);
        if (branch is null)
        {
            branch = new Branch { Code = "MAIN", Name = "Main Branch" };
            db.Branches.Add(branch);
            await db.SaveChangesAsync(ct);
        }
        return branch;
    }

    /// <summary>
    /// Every system (IsSystem = true) role, sourced from DefaultRoleDefinitions — the single
    /// source of truth also used by RoleService's "Restore to default" action. Pure additive
    /// seed data against the existing Role/Permission/RolePermission tables — an admin can
    /// hand-tune any of these afterwards from Admin -&gt; Roles &amp; Permissions.
    /// </summary>
    private async Task<Role> SeedRolesAsync(CancellationToken ct)
    {
        Role? superAdmin = null;
        foreach (var definition in DefaultRoleDefinitions.All)
        {
            var role = await EnsureRoleAsync(definition.Code, definition.Name, definition.Description, ct);
            await EnsureRolePermissionsAsync(role, definition.Permissions, ct);
            if (definition.Code == "SUPER_ADMIN")
            {
                superAdmin = role;
            }
        }

        return superAdmin ?? throw new InvalidOperationException("DefaultRoleDefinitions.All must include SUPER_ADMIN.");
    }

    private async Task<Role> EnsureRoleAsync(string code, string name, string description, CancellationToken ct)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Code == code, ct);
        if (role is null)
        {
            role = new Role { Code = code, Name = name, Description = description, IsSystem = true };
            db.Roles.Add(role);
            await db.SaveChangesAsync(ct);
        }
        else if (role.Name != name || role.Description != description || !role.IsSystem)
        {
            role.Name = name;
            role.Description = description;
            role.IsSystem = true;
            await db.SaveChangesAsync(ct);
        }
        return role;
    }

    private async Task EnsureRolePermissionsAsync(Role role, IEnumerable<string> codes, CancellationToken ct)
    {
        var permissionIds = await db.Permissions
            .Where(p => codes.Contains(p.Code))
            .Select(p => p.Id)
            .ToListAsync(ct);
        var current = await db.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync(ct);

        foreach (var permissionId in permissionIds.Except(current))
        {
            db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task SeedAdminUserAsync(Branch branch, Role superAdmin, CancellationToken ct)
    {
        if (await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == "admin", ct))
        {
            return;
        }

        var password = configuration["Seed:AdminPassword"] ?? "Admin@123";
        // Read from config rather than the Password.ForceChangeOnFirstLogin setting, because this
        // method runs before SeedSettingsAsync — the setting row does not exist yet. Defaults to
        // true so a real deployment still forces the seeded password to be replaced; set
        // "Seed:ForcePasswordChange": false in appsettings.Development.json to skip it locally.
        var forcePasswordChange = configuration.GetValue<bool?>("Seed:ForcePasswordChange") ?? true;
        var admin = new User
        {
            Username = "admin",
            PasswordHash = hasher.Hash(password),
            FullName = "System Administrator",
            DefaultBranchId = branch.Id,
            MustChangePassword = forcePasswordChange,
            UserRoles = [new UserRole { RoleId = superAdmin.Id }],
            UserBranches = [new UserBranch { BranchId = branch.Id }],
        };
        db.Users.Add(admin);
        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            forcePasswordChange
                ? "Seeded admin user (id {UserId}); the first login forces a password change"
                : "Seeded admin user (id {UserId}); forced password change is disabled by Seed:ForcePasswordChange",
            admin.Id);
    }

    private async Task SeedMenusAsync(CancellationToken ct)
    {
        var dashboard = await EnsureMenuAsync(null, "Dashboard", "home", "/", PermissionRegistry.DashboardHomeView, 0, ct);
        if (dashboard.PermissionCode != PermissionRegistry.DashboardHomeView)
        {
            // EnsureMenuAsync deliberately never updates an existing row (see its own doc
            // comment) — this one-time backfill covers environments seeded before Dashboard
            // had a permission gate at all (it used to be unconditionally visible to everyone).
            dashboard.PermissionCode = PermissionRegistry.DashboardHomeView;
            await db.SaveChangesAsync(ct);
        }

        var frontOffice = await EnsureMenuAsync(null, "Front Office", "headset", "/front-office", PermissionRegistry.VisitView, 1, ct);
        await EnsureMenuAsync(frontOffice.Id, "OP Registration", "user-plus", "/patients/new", PermissionRegistry.PatientCreate, 10, ct);
        await EnsureMenuAsync(frontOffice.Id, "IP Registration", "bed", "/ipd/admissions", PermissionRegistry.AdmissionView, 20, ct);

        var masters = await EnsureMenuAsync(null, "Masters", "database", null, null, 80, ct);
        await EnsureMenuAsync(masters.Id, "Departments", "layout-grid", "/masters/departments", PermissionRegistry.DepartmentView, 10, ct);
        await EnsureMenuAsync(masters.Id, "Specialties", "stethoscope", "/masters/specialties", PermissionRegistry.SpecialtyView, 20, ct);
        await EnsureMenuAsync(masters.Id, "Patient Categories", "tags", "/masters/patient-categories", PermissionRegistry.PatientCategoryView, 30, ct);
        await EnsureMenuAsync(masters.Id, "Doctors", "user-round", "/masters/doctors", PermissionRegistry.DoctorView, 40, ct);
        await EnsureMenuAsync(masters.Id, "Employees", "id-card", "/masters/employees", PermissionRegistry.EmployeeView, 50, ct);
        await EnsureMenuAsync(masters.Id, "Services", "briefcase-medical", "/masters/services", PermissionRegistry.ServiceView, 60, ct);
        await EnsureMenuAsync(masters.Id, "Tariffs", "receipt", "/masters/tariffs", PermissionRegistry.TariffView, 70, ct);
        await EnsureMenuAsync(masters.Id, "Packages", "package", "/masters/packages", PermissionRegistry.PackageView, 80, ct);
        await EnsureMenuAsync(masters.Id, "Locations & Beds", "bed", "/masters/locations", PermissionRegistry.LocationView, 90, ct);
        await EnsureMenuAsync(masters.Id, "Vendors", "truck", "/masters/vendors", PermissionRegistry.VendorView, 100, ct);
        await EnsureMenuAsync(masters.Id, "Items", "boxes", "/masters/items", PermissionRegistry.ItemView, 110, ct);
        await EnsureMenuAsync(masters.Id, "Drugs", "pill", "/masters/drugs", PermissionRegistry.DrugView, 120, ct);
        await EnsureMenuAsync(masters.Id, "Taxes", "percent", "/masters/taxes", PermissionRegistry.TaxView, 130, ct);
        await EnsureMenuAsync(masters.Id, "Discount Types", "badge-percent", "/masters/discount-types", PermissionRegistry.DiscountTypeView, 140, ct);
        await EnsureMenuAsync(masters.Id, "Referrers", "user-plus", "/masters/referrers", PermissionRegistry.ReferrerView, 150, ct);
        await EnsureMenuAsync(masters.Id, "ICD-10 Codes", "book-open", "/masters/icd-codes", PermissionRegistry.IcdView, 160, ct);
        await EnsureMenuAsync(masters.Id, "Corporates", "building-2", "/masters/corporates", PermissionRegistry.CorporateView, 170, ct);
        await EnsureMenuAsync(masters.Id, "Insurance / TPA", "shield-plus", "/masters/insurance", PermissionRegistry.InsuranceView, 180, ct);
        await EnsureMenuAsync(masters.Id, "Units of Measure", "ruler", "/masters/uoms", PermissionRegistry.UomView, 190, ct);
        await EnsureMenuAsync(masters.Id, "Sample Types", "test-tube", "/masters/sample-types", PermissionRegistry.SampleTypeView, 200, ct);
        await EnsureMenuAsync(masters.Id, "Modalities", "scan", "/masters/modalities", PermissionRegistry.ModalityView, 210, ct);
        await EnsureMenuAsync(masters.Id, "Consent Templates", "file-signature", "/masters/consent-templates", PermissionRegistry.ConsentTemplateView, 220, ct);
        await EnsureMenuAsync(masters.Id, "Document Types", "file-type", "/masters/document-types", PermissionRegistry.RecordDocumentTypeView, 230, ct);
        await EnsureMenuAsync(masters.Id, "Bill Estimate Templates", "file-spreadsheet", "/masters/bill-estimate-templates", PermissionRegistry.BillEstimateTemplateView, 240, ct);
        await EnsureMenuAsync(masters.Id, "Doctor Payout Rules", "hand-coins", "/masters/doctor-payout-rules", PermissionRegistry.DoctorPayoutRuleView, 250, ct);
        await EnsureMenuAsync(masters.Id, "Camps", "tent-tree", "/masters/camps", PermissionRegistry.CampView, 260, ct);
        await EnsureMenuAsync(masters.Id, "States", "map", "/masters/states", PermissionRegistry.StateView, 270, ct);
        await EnsureMenuAsync(masters.Id, "Cities", "map-pin", "/masters/cities", PermissionRegistry.CityView, 280, ct);
        await EnsureMenuAsync(masters.Id, "Areas", "map-pinned", "/masters/areas", PermissionRegistry.AreaView, 290, ct);
        await EnsureMenuAsync(masters.Id, "Nationalities", "flag", "/masters/nationalities", PermissionRegistry.NationalityView, 300, ct);
        await EnsureMenuAsync(masters.Id, "Religions", "hand-heart", "/masters/religions", PermissionRegistry.ReligionView, 310, ct);
        await EnsureMenuAsync(masters.Id, "Languages", "languages", "/masters/languages", PermissionRegistry.LanguageView, 320, ct);
        await EnsureMenuAsync(masters.Id, "Specially Abled Categories", "accessibility", "/masters/specially-abled-categories", PermissionRegistry.SpeciallyAbledCategoryView, 330, ct);
        await EnsureMenuAsync(masters.Id, "Patient HC Tagging", "tag", "/masters/patient-hc-tags", PermissionRegistry.PatientHcTagView, 340, ct);
        await EnsureMenuAsync(masters.Id, "Patient Case Types", "folder-tree", "/masters/patient-case-types", PermissionRegistry.PatientCaseTypeView, 350, ct);
        await EnsureMenuAsync(masters.Id, "MLC Types", "siren", "/masters/mlc-types", PermissionRegistry.MlcTypeView, 360, ct);
        await EnsureMenuAsync(masters.Id, "MHC Doctors", "user-round-cog", "/masters/mhc-doctors", PermissionRegistry.MhcDoctorView, 370, ct);
        await EnsureMenuAsync(masters.Id, "Loyalty Programs", "gem", "/masters/loyalty-programs", PermissionRegistry.LoyaltyProgramView, 380, ct);
        await EnsureMenuAsync(masters.Id, "Units", "layout-panel-top", "/masters/units", PermissionRegistry.UnitView, 390, ct);
        await EnsureMenuAsync(masters.Id, "Appointment Types", "calendar-cog", "/masters/appointment-types", PermissionRegistry.AppointmentTypeView, 395, ct);
        await EnsureMenuAsync(masters.Id, "MHC Packages", "package-plus", "/masters/mhc-packages", PermissionRegistry.MhcPackageView, 400, ct);
        await EnsureMenuAsync(masters.Id, "Loyalty Card Categories", "layers", "/masters/loyalty-card-categories", PermissionRegistry.LoyaltyCardCategoryView, 410, ct);
        await EnsureMenuAsync(masters.Id, "Member Relation Tags", "users", "/masters/member-relation-tags", PermissionRegistry.MemberRelationTagView, 420, ct);
        await EnsureMenuAsync(masters.Id, "Loyalty Card Types", "credit-card", "/masters/loyalty-card-types", PermissionRegistry.LoyaltyCardTypeView, 430, ct);
        await EnsureMenuAsync(masters.Id, "Loyalty Card Prices", "circle-dollar-sign", "/masters/loyalty-card-prices", PermissionRegistry.LoyaltyCardPriceView, 440, ct);
        await EnsureMenuAsync(masters.Id, "Salutations", "user-cog", "/masters/salutations", PermissionRegistry.SalutationView, 450, ct);
        await EnsureMenuAsync(masters.Id, "Marital Statuses", "heart-handshake", "/masters/marital-statuses", PermissionRegistry.MaritalStatusView, 460, ct);
        await EnsureMenuAsync(masters.Id, "Identity Document Types", "id-card", "/masters/identity-document-types", PermissionRegistry.IdentityDocumentTypeView, 470, ct);
        await EnsureMenuAsync(masters.Id, "Visit Classes", "layers", "/masters/visit-classes", PermissionRegistry.VisitClassView, 480, ct);
        await EnsureMenuAsync(masters.Id, "Visit Sessions", "sun", "/masters/visit-sessions", PermissionRegistry.VisitSessionView, 490, ct);
        await EnsureMenuAsync(masters.Id, "Claim Document Types", "file-text", "/masters/claim-document-types", PermissionRegistry.ClaimDocumentTypeView, 500, ct);
        await EnsureMenuAsync(masters.Id, "Claim Denial Reasons", "file-x", "/masters/claim-denial-reasons", PermissionRegistry.ClaimDenialReasonView, 510, ct);

        await EnsureMenuAsync(null, "Patients", "contact-round", "/patients", PermissionRegistry.PatientView, 5, ct);
        await EnsureMenuAsync(null, "Block/Unblock Report", "file-bar-chart", "/patients/reports/block-unblock", PermissionRegistry.PatientBlock, 5, ct);

        var appointments = await EnsureMenuAsync(null, "Appointments", "calendar-clock", null, null, 6, ct);
        await EnsureMenuAsync(appointments.Id, "Book Appointment", "calendar-plus", "/appointments/new", PermissionRegistry.AppointmentBook, 10, ct);
        await EnsureMenuAsync(appointments.Id, "Appointments", "calendar-days", "/appointments", PermissionRegistry.AppointmentView, 20, ct);
        await EnsureMenuAsync(appointments.Id, "Doctor Schedules", "calendar-cog", "/appointments/schedule", PermissionRegistry.ScheduleView, 30, ct);
        await EnsureMenuAsync(appointments.Id, "Unit Scheduling", "calendar-range", "/appointments/unit-schedule", PermissionRegistry.AppointmentUnitScheduleView, 35, ct);
        await EnsureMenuAsync(appointments.Id, "Queue Display", "monitor-play", "/appointments/queue-display", PermissionRegistry.QueueView, 40, ct);
        await EnsureMenuAsync(appointments.Id, "Doctors On Duty", "stethoscope", "/appointments/doctors-on-duty", PermissionRegistry.ScheduleView, 45, ct);
        await EnsureMenuAsync(appointments.Id, "Doctor-Appointment Report", "file-bar-chart", "/appointments/reports/doctor-appointment", PermissionRegistry.AppointmentView, 50, ct);

        var opd = await EnsureMenuAsync(null, "OPD", "stethoscope", null, null, 7, ct);
        await EnsureMenuAsync(opd.Id, "Front Desk", "clipboard-list", "/opd", PermissionRegistry.VisitView, 10, ct);
        await EnsureMenuAsync(opd.Id, "Doctor Queue", "list-ordered", "/opd/queue", PermissionRegistry.VisitView, 20, ct);
        await EnsureMenuAsync(opd.Id, "Prescription Templates", "file-heart", "/opd/templates", PermissionRegistry.TemplateView, 30, ct);
        await EnsureMenuAsync(opd.Id, "OP Report", "file-bar-chart", "/opd/report", PermissionRegistry.VisitView, 40, ct);
        await EnsureMenuAsync(opd.Id, "Camp Registration", "tent-tree", "/opd/camp-registration", PermissionRegistry.VisitView, 50, ct);
        await EnsureMenuAsync(opd.Id, "Camp Registration Report", "file-spreadsheet", "/opd/camp-registration/report", PermissionRegistry.VisitView, 60, ct);
        await EnsureMenuAsync(opd.Id, "OP TAT Report", "file-bar-chart", "/opd/reports/tat", PermissionRegistry.VisitView, 70, ct);
        await EnsureMenuAsync(opd.Id, "OP Cancel Report", "file-bar-chart", "/opd/reports/cancelled", PermissionRegistry.VisitView, 80, ct);
        await EnsureMenuAsync(opd.Id, "OP to IP Conversion Report", "file-bar-chart", "/opd/reports/conversion", PermissionRegistry.VisitView, 90, ct);
        await EnsureMenuAsync(opd.Id, "Referral Report", "file-bar-chart", "/opd/reports/referrals", PermissionRegistry.VisitView, 100, ct);

        var billing = await EnsureMenuAsync(null, "Billing", "receipt-indian-rupee", null, null, 8, ct);
        await EnsureMenuAsync(billing.Id, "Billing Dashboard", "credit-card", "/reports/billing", PermissionRegistry.DashboardBillingView, 1, ct);
        await EnsureMenuAsync(billing.Id, "OP Bill Payment", "search", "/billing/op-search", PermissionRegistry.BillView, 5, ct);
        await EnsureMenuAsync(billing.Id, "Bills", "file-text", "/billing/bills", PermissionRegistry.BillView, 10, ct);
        await EnsureMenuAsync(billing.Id, "Discount Approvals", "badge-percent", "/billing/discount-approvals", PermissionRegistry.DiscountApprove, 20, ct);
        await EnsureMenuAsync(billing.Id, "Refund Approvals", "rotate-ccw", "/billing/refund-approvals", PermissionRegistry.RefundApprove, 30, ct);
        await EnsureMenuAsync(billing.Id, "Advances", "wallet", "/billing/advances", PermissionRegistry.AdvanceView, 40, ct);
        await EnsureMenuAsync(billing.Id, "Day Close", "calendar-check", "/billing/day-close", PermissionRegistry.DayCloseView, 50, ct);
        await EnsureMenuAsync(billing.Id, "Payment Modes", "credit-card", "/billing/payment-modes", PermissionRegistry.PaymentModeView, 60, ct);
        await EnsureMenuAsync(billing.Id, "Credit Note Approvals", "receipt-text", "/billing/credit-notes", PermissionRegistry.CreditNoteView, 70, ct);
        await EnsureMenuAsync(billing.Id, "Parked Bills", "pause-circle", "/billing/parked-bills", PermissionRegistry.BillPark, 80, ct);
        await EnsureMenuAsync(billing.Id, "Cashier Shifts", "hand-coins", "/billing/cashier-shifts", PermissionRegistry.CashierShiftManage, 90, ct);
        await EnsureMenuAsync(billing.Id, "Bill Estimates", "calculator", "/billing/estimates", PermissionRegistry.BillEstimateView, 100, ct);
        await EnsureMenuAsync(billing.Id, "Online Payment Requests", "send", "/billing/online-payment-requests", PermissionRegistry.OnlinePaymentRequestView, 105, ct);

        // A dedicated "Reports" folder (matching the legacy menu's own separate REPORTS
        // section, rather than flat alongside Bills/Day Close/...), holding both the combined
        // tabbed page everyone already has and — off by default — one item per report tab for
        // a role that wants the legacy split-menu shape instead. Path is a route *segment*
        // (/billing/reports/<key>), not a query string: this app's tab-workspace reuses an
        // already-open tab for a sidebar click sharing the same pathname (see
        // TabWorkspaceContext.openInActiveTab), so two menu items differing only by query
        // string collapse to "already open" after the first click and silently do nothing —
        // a real route segment is what every other individually-linked report page in this
        // codebase already uses, and what makes each of these 15 a genuinely distinct screen.
        var billingReportsFolder = await EnsureMenuAsync(billing.Id, "Reports", "bar-chart-3", null, null, 110, ct);
        var billingReports = await EnsureMenuAsync(billingReportsFolder.Id, "Billing Reports", "bar-chart-3", "/billing/reports", PermissionRegistry.BillingReportView, 10, ct);

        (string Key, string Title, string? Permission)[] billingReportItems =
        [
            ("bill-report", "Bill Report (OP/IP)", PermissionRegistry.BillingReportView),
            ("ageing", "Ageing Report", PermissionRegistry.BillingReportView),
            ("ward-outstanding", "Ward-wise Outstanding Report", PermissionRegistry.BillViewIp),
            ("day-book", "Day Book Report", PermissionRegistry.BillingReportView),
            ("cancelled", "Bill Cancelled Report", PermissionRegistry.BillingReportView),
            ("concession", "Concession Report", PermissionRegistry.BillingReportView),
            ("refund", "Refund Report", PermissionRegistry.BillingReportView),
            ("advance", "Advance Report", PermissionRegistry.BillingReportView),
            ("tariff-list", "Tariff List Report", PermissionRegistry.BillingReportView),
            ("doctors-payout", "Doctors Payout Report", PermissionRegistry.BillingReportView),
            ("user-settlement", "User Settlement Report", PermissionRegistry.BillingReportView),
            ("service-breakup", "Service-wise Breakup Report", PermissionRegistry.BillingReportView),
            ("ip-detail", "IP Service Billing Detail Report", PermissionRegistry.BillViewIp),
            ("revenue", "Revenue Report", PermissionRegistry.BillingReportView),
            ("outstanding", "Outstanding Report", PermissionRegistry.BillingReportView),
        ];
        var billingReportSortOrder = 10;
        foreach (var (key, title, permission) in billingReportItems)
        {
            billingReportSortOrder += 10;
            await EnsureMenuAsync(billingReportsFolder.Id, title, "file-bar-chart", $"/billing/reports/{key}", permission,
                billingReportSortOrder, ct, hiddenByDefault: true);
        }

        var lab = await EnsureMenuAsync(null, "Laboratory", "microscope", null, null, 9, ct);
        await EnsureMenuAsync(lab.Id, "Orders", "flask-conical", "/lab/orders", PermissionRegistry.LabOrderView, 10, ct);
        await EnsureMenuAsync(lab.Id, "Collection Worklist", "test-tube", "/lab/collection", PermissionRegistry.LabSampleCollect, 20, ct);
        await EnsureMenuAsync(lab.Id, "Department Worklist", "list-checks", "/lab/worklist", PermissionRegistry.LabResultCreate, 30, ct);
        await EnsureMenuAsync(lab.Id, "Critical Alerts", "alert-triangle", "/lab/critical-alerts", PermissionRegistry.LabAlertAcknowledge, 40, ct);
        await EnsureMenuAsync(lab.Id, "Tests", "layers", "/lab/tests", PermissionRegistry.LabTestView, 50, ct);
        await EnsureMenuAsync(lab.Id, "Test Profiles", "clipboard-check", "/lab/profiles", PermissionRegistry.LabProfileView, 60, ct);
        await EnsureMenuAsync(lab.Id, "TAT Report", "clock", "/lab/tat-report", PermissionRegistry.LabReportView, 70, ct);
        await EnsureMenuAsync(lab.Id, "Unbilled Orders Report", "alert-triangle", "/lab/unbilled-report", PermissionRegistry.LabReportView, 80, ct);

        var pharmacy = await EnsureMenuAsync(null, "Pharmacy", "pill", null, null, 10, ct);
        await EnsureMenuAsync(pharmacy.Id, "POS Sale", "shopping-cart", "/pharmacy/sales/new", PermissionRegistry.PharmacySaleCreate, 10, ct);
        await EnsureMenuAsync(pharmacy.Id, "Sales", "receipt", "/pharmacy/sales", PermissionRegistry.PharmacySaleView, 20, ct);
        await EnsureMenuAsync(pharmacy.Id, "Purchase Requests", "file-text", "/pharmacy/purchase-requests", PermissionRegistry.PurchaseRequestView, 30, ct);
        await EnsureMenuAsync(pharmacy.Id, "Purchase Orders", "clipboard-list", "/pharmacy/purchase-orders", PermissionRegistry.PurchaseOrderView, 40, ct);
        await EnsureMenuAsync(pharmacy.Id, "GRN", "package-plus", "/pharmacy/grn", PermissionRegistry.GrnView, 50, ct);
        await EnsureMenuAsync(pharmacy.Id, "Purchase Returns", "undo-2", "/pharmacy/purchase-returns", PermissionRegistry.PurchaseReturnView, 60, ct);
        await EnsureMenuAsync(pharmacy.Id, "Stock Explorer", "warehouse", "/pharmacy/stock", PermissionRegistry.StockView, 70, ct);
        await EnsureMenuAsync(pharmacy.Id, "Issues", "send", "/pharmacy/issues", PermissionRegistry.IssueView, 80, ct);
        await EnsureMenuAsync(pharmacy.Id, "Indents", "clipboard-list", "/pharmacy/indents", PermissionRegistry.IndentView, 85, ct);
        await EnsureMenuAsync(pharmacy.Id, "Stock Adjustments", "sliders", "/pharmacy/adjustments", PermissionRegistry.StockAdjust, 90, ct);
        await EnsureMenuAsync(pharmacy.Id, "Stock Transfers", "arrow-left-right", "/pharmacy/transfers", PermissionRegistry.StockTransfer, 100, ct);
        await EnsureMenuAsync(pharmacy.Id, "Stock Audits", "clipboard-check", "/pharmacy/audits", PermissionRegistry.StockAudit, 110, ct);
        await EnsureMenuAsync(pharmacy.Id, "Alerts", "alert-triangle", "/pharmacy/alerts", PermissionRegistry.PharmacyReportView, 120, ct);
        await EnsureMenuAsync(pharmacy.Id, "Consumption Report", "bar-chart", "/pharmacy/consumption-report", PermissionRegistry.PharmacyReportView, 130, ct);
        await EnsureMenuAsync(pharmacy.Id, "Stores", "warehouse", "/pharmacy/stores", PermissionRegistry.PharmacyStoreView, 140, ct);

        var ipd = await EnsureMenuAsync(null, "IPD", "bed-double", null, null, 11, ct);
        await EnsureMenuAsync(ipd.Id, "Admissions", "clipboard-list", "/ipd/admissions", PermissionRegistry.AdmissionView, 10, ct);
        await EnsureMenuAsync(ipd.Id, "Admission Requests", "clipboard-check", "/ipd/admission-requests", PermissionRegistry.AdmissionRequestView, 12, ct);
        await EnsureMenuAsync(ipd.Id, "Convert to IP", "arrow-right-left", "/ipd/convert", PermissionRegistry.AdmissionCreate, 15, ct);
        await EnsureMenuAsync(ipd.Id, "Bed Board", "layout-grid", "/ipd/bed-board", PermissionRegistry.BedView, 20, ct);
        await EnsureMenuAsync(ipd.Id, "IP Report", "file-bar-chart", "/ipd/report", PermissionRegistry.AdmissionView, 30, ct);
        await EnsureMenuAsync(ipd.Id, "Ward Occupancy Report", "file-bar-chart", "/ipd/ward-occupancy-report", PermissionRegistry.BedView, 40, ct);

        var nursing = await EnsureMenuAsync(null, "Nursing", "heart-pulse", null, null, 12, ct);
        await EnsureMenuAsync(nursing.Id, "Shift Handovers", "repeat", "/nursing/handovers", PermissionRegistry.HandoverView, 10, ct);
        await EnsureMenuAsync(nursing.Id, "Incidents", "alert-octagon", "/nursing/incidents", PermissionRegistry.IncidentView, 20, ct);

        var rad = await EnsureMenuAsync(null, "Radiology", "scan", null, null, 13, ct);
        await EnsureMenuAsync(rad.Id, "Orders", "clipboard-list", "/rad/orders", PermissionRegistry.RadOrderView, 10, ct);
        await EnsureMenuAsync(rad.Id, "Scheduling Worklist", "calendar-clock", "/rad/scheduling", PermissionRegistry.RadScheduleManage, 20, ct);
        await EnsureMenuAsync(rad.Id, "Capture Worklist", "camera", "/rad/capture", PermissionRegistry.RadCaptureManage, 30, ct);
        await EnsureMenuAsync(rad.Id, "Reporting Worklist", "file-text", "/rad/reporting", PermissionRegistry.RadReportCreate, 40, ct);
        await EnsureMenuAsync(rad.Id, "Tests", "layers", "/rad/tests", PermissionRegistry.RadTestView, 50, ct);
        await EnsureMenuAsync(rad.Id, "Report Templates", "file-heart", "/rad/templates", PermissionRegistry.RadTemplateView, 60, ct);
        await EnsureMenuAsync(rad.Id, "TAT Report", "clock", "/rad/tat-report", PermissionRegistry.RadReportView, 70, ct);
        await EnsureMenuAsync(rad.Id, "Unbilled Orders Report", "alert-triangle", "/rad/unbilled-report", PermissionRegistry.RadReportView, 80, ct);

        var ot = await EnsureMenuAsync(null, "Operation Theatre", "scissors", null, null, 14, ct);
        await EnsureMenuAsync(ot.Id, "Bookings", "calendar-clock", "/ot/bookings", PermissionRegistry.OtBookingView, 10, ct);
        await EnsureMenuAsync(ot.Id, "Theatres", "layout-grid", "/ot/theatres", PermissionRegistry.OtTheatreView, 20, ct);
        await EnsureMenuAsync(ot.Id, "OT Register", "clipboard-list", "/ot/register", PermissionRegistry.OtRegisterView, 30, ct);

        var icu = await EnsureMenuAsync(null, "ICU / Critical Care", "activity", null, null, 15, ct);
        await EnsureMenuAsync(icu.Id, "Dashboard", "gauge", "/icu/dashboard", PermissionRegistry.IcuDashboardView, 10, ct);
        await EnsureMenuAsync(icu.Id, "Critical Alerts", "alert-triangle", "/icu/alerts", PermissionRegistry.IcuAlertView, 20, ct);
        await EnsureMenuAsync(icu.Id, "Alert Rules", "sliders", "/icu/alert-rules", PermissionRegistry.IcuRuleView, 30, ct);

        var er = await EnsureMenuAsync(null, "Emergency", "siren", null, null, 16, ct);
        await EnsureMenuAsync(er.Id, "ER Queue", "list-ordered", "/emergency/queue", PermissionRegistry.EmergencyVisitView, 10, ct);

        var mrd = await EnsureMenuAsync(null, "Medical Records", "archive", null, null, 17, ct);
        await EnsureMenuAsync(mrd.Id, "Record Files", "folder", "/mrd/files", PermissionRegistry.MrdFileView, 10, ct);
        await EnsureMenuAsync(mrd.Id, "File Requests", "send", "/mrd/requests", PermissionRegistry.MrdFileView, 20, ct);
        await EnsureMenuAsync(mrd.Id, "Coding Worklist", "list-checks", "/mrd/coding-worklist", PermissionRegistry.MrdCodingView, 30, ct);
        await EnsureMenuAsync(mrd.Id, "Discharge Audits", "clipboard-check", "/mrd/audits", PermissionRegistry.MrdAuditView, 40, ct);
        await EnsureMenuAsync(mrd.Id, "Retention & Destruction", "shredder", "/mrd/retention", PermissionRegistry.MrdRetentionView, 50, ct);
        await EnsureMenuAsync(mrd.Id, "MRD Reports", "bar-chart", "/mrd/reports", PermissionRegistry.MrdReportView, 60, ct);

        var finance = await EnsureMenuAsync(null, "Finance", "landmark", null, null, 18, ct);
        await EnsureMenuAsync(finance.Id, "Vouchers", "file-text", "/finance/vouchers", PermissionRegistry.FinanceVoucherView, 10, ct);
        await EnsureMenuAsync(finance.Id, "Chart of Accounts", "list-tree", "/finance/accounts", PermissionRegistry.FinanceAccountView, 20, ct);
        await EnsureMenuAsync(finance.Id, "Payables", "arrow-up-circle", "/finance/payables", PermissionRegistry.FinancePayableView, 30, ct);
        await EnsureMenuAsync(finance.Id, "Receivables", "arrow-down-circle", "/finance/receivables", PermissionRegistry.FinanceReceivableView, 40, ct);
        await EnsureMenuAsync(finance.Id, "Day Close Console", "calendar-check-2", "/finance/day-close-console", PermissionRegistry.FinanceDayCloseConsoleView, 50, ct);
        await EnsureMenuAsync(finance.Id, "Finance Reports", "bar-chart-3", "/finance/reports", PermissionRegistry.FinanceReportView, 60, ct);

        var biomed = await EnsureMenuAsync(null, "Biomedical", "wrench", null, null, 19, ct);
        await EnsureMenuAsync(biomed.Id, "Equipment Registry", "hard-drive", "/biomed/equipment", PermissionRegistry.BiomedEquipmentView, 10, ct);
        await EnsureMenuAsync(biomed.Id, "AMC/CMC Contracts", "file-text", "/biomed/contracts", PermissionRegistry.BiomedContractView, 20, ct);
        await EnsureMenuAsync(biomed.Id, "PM Schedules", "calendar-clock", "/biomed/pm-schedules", PermissionRegistry.BiomedPmView, 30, ct);
        await EnsureMenuAsync(biomed.Id, "Work Orders", "clipboard-list", "/biomed/work-orders", PermissionRegistry.BiomedWorkOrderView, 40, ct);
        await EnsureMenuAsync(biomed.Id, "Breakdown Complaints", "alert-octagon", "/biomed/complaints", PermissionRegistry.BiomedComplaintView, 50, ct);
        await EnsureMenuAsync(biomed.Id, "Calibrations", "gauge", "/biomed/calibrations", PermissionRegistry.BiomedCalibrationView, 60, ct);
        await EnsureMenuAsync(biomed.Id, "Biomedical Reports", "bar-chart", "/biomed/reports", PermissionRegistry.BiomedReportView, 70, ct);

        var doctorPortal = await EnsureMenuAsync(null, "Doctor Portal", "stethoscope", null, null, 20, ct);
        await EnsureMenuAsync(doctorPortal.Id, "Dashboard", "layout-dashboard", "/doctor-portal", PermissionRegistry.DoctorPortalDashboardView, 10, ct);
        await EnsureMenuAsync(doctorPortal.Id, "My Queue", "users", "/doctor-portal/queue", PermissionRegistry.DoctorPortalQueueView, 20, ct);
        await EnsureMenuAsync(doctorPortal.Id, "My Inpatients", "bed", "/doctor-portal/inpatients", PermissionRegistry.DoctorPortalIpRoundsView, 30, ct);
        await EnsureMenuAsync(doctorPortal.Id, "Orders & Results", "flask-conical", "/doctor-portal/orders", PermissionRegistry.DoctorPortalOrdersView, 40, ct);
        await EnsureMenuAsync(doctorPortal.Id, "Discharge Approvals", "file-check", "/doctor-portal/discharge-approvals", PermissionRegistry.DoctorPortalDischargeView, 50, ct);
        await EnsureMenuAsync(doctorPortal.Id, "Opinion Requests", "send", "/doctor-portal/opinion-requests", PermissionRegistry.DoctorPortalOpinionView, 60, ct);

        var reports = await EnsureMenuAsync(null, "Reports & Dashboards", "activity", null, null, 5, ct);
        await EnsureMenuAsync(reports.Id, "Management Dashboard", "activity", "/reports/management", PermissionRegistry.DashboardManagementView, 10, ct);
        await EnsureMenuAsync(reports.Id, "OP Dashboard", "stethoscope", "/reports/op", PermissionRegistry.DashboardOpView, 20, ct);
        await EnsureMenuAsync(reports.Id, "IP Dashboard", "bed-double", "/reports/ip", PermissionRegistry.DashboardIpView, 30, ct);
        // Billing Dashboard now lives under the Billing menu group (moved to mirror the
        // legacy OP billing menu, which nested its dashboard alongside its other billing
        // screens) — see EnsureMenuAsync(billing.Id, "Billing Dashboard", ...) above.
        await EnsureMenuAsync(reports.Id, "Revenue Dashboard", "bar-chart", "/reports/revenue", PermissionRegistry.DashboardRevenueView, 50, ct);
        await EnsureMenuAsync(reports.Id, "Custom Reports", "clipboard-list", "/reports/custom", PermissionRegistry.ReportCatalogView, 60, ct);
        await EnsureMenuAsync(reports.Id, "Report Subscriptions", "send", "/reports/subscriptions", PermissionRegistry.ReportSubscriptionManage, 70, ct);

        var mhc = await EnsureMenuAsync(null, "MHC", "clipboard-plus", null, null, 22, ct);
        await EnsureMenuAsync(mhc.Id, "MHC Registration", "clipboard-plus", "/mhc", PermissionRegistry.MhcVisitView, 10, ct);
        await EnsureMenuAsync(mhc.Id, "MHC Reports", "file-bar-chart", "/mhc/reports", PermissionRegistry.MhcVisitView, 20, ct);

        var loyalty = await EnsureMenuAsync(null, "Loyalty", "credit-card", null, null, 23, ct);
        await EnsureMenuAsync(loyalty.Id, "Loyalty Registration", "credit-card", "/loyalty", PermissionRegistry.LoyaltyCardView, 10, ct);

        var insurance = await EnsureMenuAsync(null, "Insurance", "shield-plus", null, null, 24, ct);
        await EnsureMenuAsync(insurance.Id, "Pre-Authorization Requests", "file-check", "/insurance/pre-auth", PermissionRegistry.InsurancePreAuthView, 10, ct);
        await EnsureMenuAsync(insurance.Id, "Claims", "receipt", "/insurance/claims", PermissionRegistry.InsuranceClaimView, 20, ct);

        var quality = await EnsureMenuAsync(null, "Quality / NABH", "shield-check", null, null, 21, ct);
        await EnsureMenuAsync(quality.Id, "Indicators", "gauge", "/quality/indicators", PermissionRegistry.QualityIndicatorView, 10, ct);
        await EnsureMenuAsync(quality.Id, "Audit Checklists", "clipboard-check", "/quality/audits", PermissionRegistry.QualityAuditView, 20, ct);
        await EnsureMenuAsync(quality.Id, "Incident Register", "alert-octagon", "/quality/incidents", PermissionRegistry.IncidentView, 30, ct);
        await EnsureMenuAsync(quality.Id, "Non-Conformances", "flag", "/quality/non-conformances", PermissionRegistry.QualityNonConformanceView, 40, ct);
        await EnsureMenuAsync(quality.Id, "CAPA Tracker", "list-checks", "/quality/capas", PermissionRegistry.QualityCapaView, 50, ct);
        await EnsureMenuAsync(quality.Id, "Risk Register", "triangle-alert", "/quality/risks", PermissionRegistry.QualityRiskView, 60, ct);
        await EnsureMenuAsync(quality.Id, "Committee Meetings", "users", "/quality/committee-meetings", PermissionRegistry.QualityCommitteeView, 70, ct);
        await EnsureMenuAsync(quality.Id, "Committees", "user-cog", "/quality/committees", PermissionRegistry.QualityCommitteeView, 75, ct);
        await EnsureMenuAsync(quality.Id, "NABH Evidence", "archive", "/quality/evidence", PermissionRegistry.QualityEvidenceView, 80, ct);
        await EnsureMenuAsync(quality.Id, "NABH Standards", "book-open", "/quality/nabh-standards", PermissionRegistry.QualityCommitteeView, 85, ct);
        await EnsureMenuAsync(quality.Id, "Compliance Dashboard", "activity", "/quality/dashboard", PermissionRegistry.QualityDashboardView, 90, ct);

        var admin = await EnsureMenuAsync(null, "Administration", "settings", null, null, 90, ct);
        await EnsureMenuAsync(admin.Id, "Users", "users", "/admin/users", PermissionRegistry.UserView, 10, ct);
        await EnsureMenuAsync(admin.Id, "Roles & Permissions", "shield", "/admin/roles", PermissionRegistry.RoleView, 20, ct);
        await EnsureMenuAsync(admin.Id, "Menus", "menu", "/admin/menus", PermissionRegistry.MenuView, 30, ct);
        await EnsureMenuAsync(admin.Id, "Branches", "building", "/admin/branches", PermissionRegistry.BranchView, 40, ct);
        await EnsureMenuAsync(admin.Id, "Designations", "badge", "/admin/designations", PermissionRegistry.DesignationView, 50, ct);
        await EnsureMenuAsync(admin.Id, "Settings", "sliders", "/admin/settings", PermissionRegistry.SettingView, 60, ct);
        await EnsureMenuAsync(admin.Id, "ABDM Integration", "activity", "/admin/abdm", PermissionRegistry.AbdmAdminManage, 65, ct);
        await EnsureMenuAsync(admin.Id, "Sessions", "monitor", "/admin/sessions", PermissionRegistry.SessionView, 70, ct);
        await EnsureMenuAsync(admin.Id, "Audit Log", "file-search", "/admin/audit-logs", PermissionRegistry.AuditView, 80, ct);
        await EnsureMenuAsync(admin.Id, "Activity Log", "activity", "/admin/activity-logs", PermissionRegistry.ActivityView, 90, ct);

        await SeedOpBillingReportMenuLayoutAsync(billingReportsFolder, billingReports, ct);
    }

    /// <summary>
    /// Opts OP_BILLING into the split-report-items view (see the billingReportItems seed
    /// above): shows each of the 13 individually-named report entries (HiddenByDefault
    /// globally, so no other role sees them) and hides the combined tabbed "Billing Reports"
    /// entry, matching the legacy OP billing menu's shape. Every other BILLING.REPORT.VIEW
    /// role keeps today's single tabbed page under the "Reports" folder, unchanged. Skipped
    /// if OP_BILLING hasn't been seeded yet, same guard as SeedFrontOfficeMenuLayoutAsync.
    /// </summary>
    private async Task SeedOpBillingReportMenuLayoutAsync(Menu billingReportsFolder, Menu billingReports, CancellationToken ct)
    {
        var opBillingRole = await db.Roles.FirstOrDefaultAsync(r => r.Code == "OP_BILLING", ct);
        if (opBillingRole is null)
        {
            return;
        }

        var reportItems = await db.Menus
            .Where(m => m.ParentId == billingReportsFolder.Id && m.HiddenByDefault && m.Path != null && m.Path.StartsWith("/billing/reports/"))
            .ToListAsync(ct);
        foreach (var item in reportItems)
        {
            await EnsureRoleMenuShownAsync(opBillingRole.Id, item.Id, item.ParentId, item.SortOrder, ct);
        }

        await EnsureRoleMenuHiddenAsync(opBillingRole.Id, billingReports.Id, ct);
    }

    /// <summary>
    /// A Front Office-specific sidebar tree (Masters/Registration/Transactions/Reports/Tools),
    /// built from the club of screens the front-desk team actually uses day to day. Most leaf
    /// items are brand-new global Menu rows scoped to whatever slice of an existing page/report
    /// they need (EnsureMenuAsync dedups by title+path, so a distinctly-worded item never
    /// collides with an existing entry elsewhere in the tree — it's just a second, differently
    /// labeled way into the same page). The handful that DO exactly match an existing global
    /// entry (loyalty masters, Patient HC Tagging, IP/Camp Registration, four OPD/IPD reports)
    /// are reused as-is and only repositioned for the FRONT_OFFICE role via
    /// SeedFrontOfficeMenuLayoutAsync — see that method for why (MenuRoleLayout, not a menu
    /// duplicate, is how one role gets its own placement for a shared item).
    /// </summary>
    private async Task SeedFrontOfficeMenusAsync(CancellationToken ct)
    {
        var frontOffice = await EnsureMenuAsync(null, "Front Office", "headset", "/front-office", PermissionRegistry.VisitView, 1, ct);

        var foMasters = await EnsureMenuAsync(frontOffice.Id, "Masters", "database", null, null, 30, ct);
        await EnsureMenuAsync(foMasters.Id, "Camp Name", "tent-tree", "/masters/camps", PermissionRegistry.CampView, 10, ct);
        await EnsureMenuAsync(foMasters.Id, "State", "map", "/masters/states", PermissionRegistry.StateView, 20, ct);
        await EnsureMenuAsync(foMasters.Id, "MHC Doctor", "user-round-cog", "/masters/mhc-doctors", PermissionRegistry.MhcDoctorView, 30, ct);
        await EnsureMenuAsync(foMasters.Id, "Specially Abled", "accessibility", "/masters/specially-abled-categories", PermissionRegistry.SpeciallyAbledCategoryView, 40, ct);
        await EnsureMenuAsync(foMasters.Id, "Referral Source Master", "user-plus", "/masters/referrers", PermissionRegistry.ReferrerView, 50, ct);
        await EnsureMenuAsync(foMasters.Id, "Religion", "hand-heart", "/masters/religions", PermissionRegistry.ReligionView, 60, ct);
        await EnsureMenuAsync(foMasters.Id, "Language", "languages", "/masters/languages", PermissionRegistry.LanguageView, 70, ct);
        // sortOrder 80 reserved for "Patient HC Tagging" — reused via layout override, see SeedFrontOfficeMenuLayoutAsync.
        await EnsureMenuAsync(foMasters.Id, "Patient Case Type", "folder-tree", "/masters/patient-case-types", PermissionRegistry.PatientCaseTypeView, 90, ct);
        await EnsureMenuAsync(foMasters.Id, "City", "map-pin", "/masters/cities", PermissionRegistry.CityView, 100, ct);
        await EnsureMenuAsync(foMasters.Id, "Unit Masters", "layout-panel-top", "/masters/units", PermissionRegistry.UnitView, 110, ct);
        await EnsureMenuAsync(foMasters.Id, "Appointment Masters", "calendar-cog", "/masters/appointment-types", PermissionRegistry.AppointmentTypeView, 120, ct);
        await EnsureMenuAsync(foMasters.Id, "Create MLC Type", "siren", "/masters/mlc-types", PermissionRegistry.MlcTypeView, 130, ct);
        var foLoyaltyMasters = await EnsureMenuAsync(foMasters.Id, "Loyalty Masters", "gem", null, null, 140, ct);
        await EnsureMenuAsync(foMasters.Id, "Nationality", "flag", "/masters/nationalities", PermissionRegistry.NationalityView, 150, ct);
        await EnsureMenuAsync(foMasters.Id, "Area and City Mapping", "map-pinned", "/masters/areas", PermissionRegistry.AreaView, 160, ct);

        var foRegistration = await EnsureMenuAsync(frontOffice.Id, "Registration", "clipboard-list", null, null, 40, ct);
        await EnsureMenuAsync(foRegistration.Id, "ER Registration", "siren", "/emergency/queue", PermissionRegistry.EmergencyVisitView, 10, ct);
        // sortOrder 20/30/40 reserved for "OP Registration" / "IP Registration" / "Camp Registration" — reused via layout override (they already exist as global Front Office quick-links, see SeedFrontOfficeMenuLayoutAsync); creating fresh rows here would just duplicate them for this role.
        await EnsureMenuAsync(foRegistration.Id, "Photo Capture", "camera", "/patients/photo-capture", PermissionRegistry.PatientUpdate, 50, ct);
        var foImport = await EnsureMenuAsync(foRegistration.Id, "Import", "upload", null, null, 60, ct);
        await EnsureMenuAsync(foImport.Id, "MHC Bulk Import", "upload", "/mhc/import", PermissionRegistry.MhcVisitImport, 10, ct);
        await EnsureMenuAsync(foImport.Id, "Camp Bulk Import", "upload", "/opd/camp-registration/import", PermissionRegistry.VisitImport, 20, ct);

        var foTransactions = await EnsureMenuAsync(frontOffice.Id, "Transactions", "list-checks", null, null, 50, ct);
        await EnsureMenuAsync(foTransactions.Id, "Appointment", "calendar-days", "/appointments", PermissionRegistry.AppointmentView, 10, ct);
        await EnsureMenuAsync(foTransactions.Id, "Doctors Entry", "list-ordered", "/opd/queue", PermissionRegistry.VisitView, 20, ct);
        await EnsureMenuAsync(foTransactions.Id, "Patient Block and Unblock", "user-x", "/patients/reports/block-unblock", PermissionRegistry.PatientBlock, 30, ct);
        await EnsureMenuAsync(foTransactions.Id, "Loyalty Transactions", "credit-card", "/loyalty", PermissionRegistry.LoyaltyCardView, 40, ct);
        await EnsureMenuAsync(foTransactions.Id, "Admission Request", "clipboard-check", "/ipd/admission-requests", PermissionRegistry.AdmissionRequestView, 50, ct);

        var foReports = await EnsureMenuAsync(frontOffice.Id, "Reports", "file-bar-chart", null, null, 60, ct);
        // sortOrder 10/20/30/40 reserved for "OP TAT Report" / "OP Report" / "IP Report" / "Camp Registration Report" — reused via layout override.
        await EnsureMenuAsync(foReports.Id, "OPIP Census Report", "file-bar-chart", "/reports/census", PermissionRegistry.ReportCensusView, 45, ct);
        await EnsureMenuAsync(foReports.Id, "Doctor Appointment Report", "file-bar-chart", "/appointments/reports/doctor-appointment", PermissionRegistry.AppointmentView, 50, ct);
        await EnsureMenuAsync(foReports.Id, "Conversion Report", "file-bar-chart", "/opd/reports/conversion", PermissionRegistry.VisitView, 60, ct);
        await EnsureMenuAsync(foReports.Id, "Patient Search", "search", "/patients", PermissionRegistry.PatientView, 70, ct);
        await EnsureMenuAsync(foReports.Id, "Bed Occupancy Report - Mapims", "file-bar-chart", "/ipd/ward-occupancy-report", PermissionRegistry.BedView, 80, ct);
        await EnsureMenuAsync(foReports.Id, "Patient Block Unblock Report", "file-bar-chart", "/patients/reports/block-unblock", PermissionRegistry.PatientBlock, 90, ct);
        await EnsureMenuAsync(foReports.Id, "Service List Report - MHC", "file-bar-chart", "/mhc/reports", PermissionRegistry.MhcVisitView, 100, ct);
        await EnsureMenuAsync(foReports.Id, "MHC Package Analysis", "file-bar-chart", "/mhc/reports", PermissionRegistry.MhcVisitView, 110, ct);
        await EnsureMenuAsync(foReports.Id, "Patient List Report - MHC", "file-bar-chart", "/mhc/reports", PermissionRegistry.MhcVisitView, 120, ct);
        await EnsureMenuAsync(foReports.Id, "Referral Patient Report", "file-bar-chart", "/opd/reports/referrals", PermissionRegistry.VisitView, 130, ct);
        await EnsureMenuAsync(foReports.Id, "Doctor Admission Request Report", "file-bar-chart", "/ipd/admission-requests/report", PermissionRegistry.AdmissionRequestView, 140, ct);
        // Same page as "IP Registration" (a distinct menu title with the same path is a distinct
        // Menu row — no collision) — a UHID/name/admission-no lookup over current inpatients,
        // reused as-is rather than a parallel enquiry screen/endpoint.
        await EnsureMenuAsync(foReports.Id, "IP Enquiry", "search", "/ipd/admissions", PermissionRegistry.AdmissionView, 150, ct);
        await EnsureMenuAsync(foReports.Id, "ER Report", "file-bar-chart", "/emergency/reports", PermissionRegistry.EmergencyVisitView, 160, ct);
        // Standalone printable summary — a patient picker over the same allergy/problem-list/
        // prescription-history/timeline hooks the EMR workspace's left sidebar already uses, no new endpoint.
        await EnsureMenuAsync(foReports.Id, "Patient Summary", "file-bar-chart", "/patients/reports/summary", PermissionRegistry.PatientView, 170, ct);

        var foTools = await EnsureMenuAsync(frontOffice.Id, "Tools", "settings", null, null, 70, ct);
        await EnsureMenuAsync(foTools.Id, "Control Panel", "sliders", "/admin/settings", PermissionRegistry.SettingView, 10, ct);
        await EnsureMenuAsync(foTools.Id, "Consultation Charge Settings", "sliders", "/admin/settings", PermissionRegistry.SettingView, 30, ct);
        await EnsureMenuAsync(foTools.Id, "Patient Type Color Settings", "palette", "/masters/patient-categories", PermissionRegistry.PatientCategoryView, 40, ct);
        await EnsureMenuAsync(foTools.Id, "Module Rights", "shield", "/admin/roles", PermissionRegistry.RoleView, 50, ct);

        await SeedFrontOfficeMenuLayoutAsync(frontOffice, foMasters, foLoyaltyMasters, foRegistration, foTransactions, foReports, foTools, ct);
    }

    /// <summary>
    /// Repositions the dozen menu items that already exist elsewhere globally (loyalty masters,
    /// Patient HC Tagging, IP/Camp Registration, four OPD/IPD reports) into the new Front Office
    /// subtree — but ONLY for the FRONT_OFFICE role, via a MenuRoleLayout row. A global reparent
    /// (editing the Menu row's own ParentId) would also move the item out of its original spot
    /// for every other role that references it (Reception, Doctor, Billing, ...); a per-role
    /// layout override leaves the shared global position untouched and just gives FRONT_OFFICE
    /// its own view. Also promotes the curated Masters/Registration/Transactions/Reports/Tools
    /// groups to top level and hides every other global top-level section — FRONT_OFFICE's
    /// broad permission set (VisitView, AdmissionView, AppointmentView, ...) is what those
    /// functions actually need, but it also happens to satisfy the *visibility* gate on
    /// Patients/Appointments/OPD/IPD/Emergency/MHC/Loyalty/Masters/Administration as ordinary
    /// global sections, which would otherwise leak into this role's sidebar alongside its own
    /// curated view. Hiding them here — rather than stripping the permissions — keeps the
    /// underlying functionality intact while giving this role the clean, legacy-matching menu.
    /// Skipped entirely if the FRONT_OFFICE role hasn't been seeded yet.
    /// </summary>
    private async Task SeedFrontOfficeMenuLayoutAsync(
        Menu frontOffice, Menu foMasters, Menu foLoyaltyMasters, Menu foRegistration, Menu foTransactions,
        Menu foReports, Menu foTools, CancellationToken ct)
    {
        var frontOfficeRole = await db.Roles.FirstOrDefaultAsync(r => r.Code == "FRONT_OFFICE", ct);
        if (frontOfficeRole is null)
        {
            return;
        }

        var patientHcTagging = await EnsureMenuAsync(null, "Patient HC Tagging", "tag", "/masters/patient-hc-tags", PermissionRegistry.PatientHcTagView, 340, ct);
        await EnsureRoleMenuLayoutAsync(frontOfficeRole.Id, patientHcTagging.Id, foMasters.Id, 80, ct);

        var loyaltyPrograms = await EnsureMenuAsync(null, "Loyalty Programs", "gem", "/masters/loyalty-programs", PermissionRegistry.LoyaltyProgramView, 380, ct);
        await EnsureRoleMenuLayoutAsync(frontOfficeRole.Id, loyaltyPrograms.Id, foLoyaltyMasters.Id, 10, ct);
        var loyaltyCardCategories = await EnsureMenuAsync(null, "Loyalty Card Categories", "layers", "/masters/loyalty-card-categories", PermissionRegistry.LoyaltyCardCategoryView, 410, ct);
        await EnsureRoleMenuLayoutAsync(frontOfficeRole.Id, loyaltyCardCategories.Id, foLoyaltyMasters.Id, 20, ct);
        var memberRelationTags = await EnsureMenuAsync(null, "Member Relation Tags", "users", "/masters/member-relation-tags", PermissionRegistry.MemberRelationTagView, 420, ct);
        await EnsureRoleMenuLayoutAsync(frontOfficeRole.Id, memberRelationTags.Id, foLoyaltyMasters.Id, 30, ct);
        var loyaltyCardTypes = await EnsureMenuAsync(null, "Loyalty Card Types", "credit-card", "/masters/loyalty-card-types", PermissionRegistry.LoyaltyCardTypeView, 430, ct);
        await EnsureRoleMenuLayoutAsync(frontOfficeRole.Id, loyaltyCardTypes.Id, foLoyaltyMasters.Id, 40, ct);
        var loyaltyCardPrices = await EnsureMenuAsync(null, "Loyalty Card Prices", "circle-dollar-sign", "/masters/loyalty-card-prices", PermissionRegistry.LoyaltyCardPriceView, 440, ct);
        await EnsureRoleMenuLayoutAsync(frontOfficeRole.Id, loyaltyCardPrices.Id, foLoyaltyMasters.Id, 50, ct);

        var opRegistration = await EnsureMenuAsync(null, "OP Registration", "user-plus", "/patients/new", PermissionRegistry.PatientCreate, 10, ct);
        await EnsureRoleMenuLayoutAsync(frontOfficeRole.Id, opRegistration.Id, foRegistration.Id, 20, ct);
        // "OP Registration" submenu — legacy lists New/Review as separate entry points into the
        // same OP/MHC registration screens (Prixo auto-detects New vs Review from visit history
        // rather than asking up front, so these are labeled shortcuts to the one real page, not
        // distinct workflows) plus a Cancel/Reprint utility screen shared by all three of
        // legacy's "OP Registration Cancel" / "OP Case Sheet Reprint-Direct" / "OP Casesheet
        // Reprint-Preview" entries (same page, three doors in — like the Tools group's three
        // Control-Panel entries sharing one Settings page).
        await EnsureMenuAsync(opRegistration.Id, "OP New Registration", "user-plus", "/opd", PermissionRegistry.VisitCreate, 10, ct);
        await EnsureMenuAsync(opRegistration.Id, "OP Review Registration", "user-plus", "/opd", PermissionRegistry.VisitCreate, 20, ct);
        await EnsureMenuAsync(opRegistration.Id, "New MHC Registration", "clipboard-plus", "/mhc", PermissionRegistry.MhcVisitCreate, 30, ct);
        await EnsureMenuAsync(opRegistration.Id, "MHC Review Registration", "clipboard-plus", "/mhc", PermissionRegistry.MhcVisitCreate, 40, ct);
        await EnsureMenuAsync(opRegistration.Id, "OP Registration Cancel", "x", "/opd/registration-utility", PermissionRegistry.VisitView, 50, ct);
        await EnsureMenuAsync(opRegistration.Id, "OP Case Sheet Reprint-Direct", "printer", "/opd/registration-utility", PermissionRegistry.VisitView, 60, ct);
        await EnsureMenuAsync(opRegistration.Id, "OP Casesheet Reprint-Preview", "printer", "/opd/registration-utility", PermissionRegistry.VisitView, 70, ct);

        var ipRegistration = await EnsureMenuAsync(null, "IP Registration", "bed", "/ipd/admissions", PermissionRegistry.AdmissionView, 20, ct);
        await EnsureRoleMenuLayoutAsync(frontOfficeRole.Id, ipRegistration.Id, foRegistration.Id, 30, ct);
        var campRegistration = await EnsureMenuAsync(null, "Camp Registration", "tent-tree", "/opd/camp-registration", PermissionRegistry.VisitView, 50, ct);
        await EnsureRoleMenuLayoutAsync(frontOfficeRole.Id, campRegistration.Id, foRegistration.Id, 40, ct);

        var opTatReport = await EnsureMenuAsync(null, "OP TAT Report", "file-bar-chart", "/opd/reports/tat", PermissionRegistry.VisitView, 70, ct);
        await EnsureRoleMenuLayoutAsync(frontOfficeRole.Id, opTatReport.Id, foReports.Id, 10, ct);
        var opReport = await EnsureMenuAsync(null, "OP Report", "file-bar-chart", "/opd/report", PermissionRegistry.VisitView, 40, ct);
        await EnsureRoleMenuLayoutAsync(frontOfficeRole.Id, opReport.Id, foReports.Id, 20, ct);
        var ipReport = await EnsureMenuAsync(null, "IP Report", "file-bar-chart", "/ipd/report", PermissionRegistry.AdmissionView, 30, ct);
        await EnsureRoleMenuLayoutAsync(frontOfficeRole.Id, ipReport.Id, foReports.Id, 30, ct);
        var campRegistrationReport = await EnsureMenuAsync(null, "Camp Registration Report", "file-spreadsheet", "/opd/camp-registration/report", PermissionRegistry.VisitView, 60, ct);
        await EnsureRoleMenuLayoutAsync(frontOfficeRole.Id, campRegistrationReport.Id, foReports.Id, 40, ct);
        var opCancelReport = await EnsureMenuAsync(null, "OP Cancel Report", "file-bar-chart", "/opd/reports/cancelled", PermissionRegistry.VisitView, 80, ct);
        await EnsureRoleMenuLayoutAsync(frontOfficeRole.Id, opCancelReport.Id, foReports.Id, 42, ct);

        // "Dashboard" is now permission-gated (DashboardHomeView) and DefaultRoleDefinitions
        // deliberately withholds that permission from FRONT_OFFICE — the standard permission
        // filter in GetMyMenuAsync already keeps it out of this role's sidebar, no override
        // needed for it specifically.

        // Promote the curated groups to the top level — this role's whole sidebar IS this
        // structure, it shouldn't be nested one level under a self-referential "Front Office"
        // heading.
        await EnsureRoleMenuLayoutAsync(frontOfficeRole.Id, foMasters.Id, null, 10, ct);
        await EnsureRoleMenuLayoutAsync(frontOfficeRole.Id, foRegistration.Id, null, 20, ct);
        await EnsureRoleMenuLayoutAsync(frontOfficeRole.Id, foTransactions.Id, null, 30, ct);
        await EnsureRoleMenuLayoutAsync(frontOfficeRole.Id, foReports.Id, null, 40, ct);
        await EnsureRoleMenuLayoutAsync(frontOfficeRole.Id, foTools.Id, null, 50, ct);

        // Hide the "Front Office" wrapper itself and every other global top-level section that
        // FRONT_OFFICE's permissions happen to also satisfy — an admin can still reach any of
        // this from the Menus screen if they explicitly want it back for this role (the Reset
        // control on the "By menu" tab drops the Hidden override same as any other).
        await EnsureRoleMenuHiddenAsync(frontOfficeRole.Id, frontOffice.Id, ct);
        var patients = await EnsureMenuAsync(null, "Patients", "contact-round", "/patients", PermissionRegistry.PatientView, 5, ct);
        await EnsureRoleMenuHiddenAsync(frontOfficeRole.Id, patients.Id, ct);
        var blockUnblockReport = await EnsureMenuAsync(null, "Block/Unblock Report", "file-bar-chart", "/patients/reports/block-unblock", PermissionRegistry.PatientBlock, 5, ct);
        await EnsureRoleMenuHiddenAsync(frontOfficeRole.Id, blockUnblockReport.Id, ct);
        var appointmentsGroup = await EnsureMenuAsync(null, "Appointments", "calendar-clock", null, null, 6, ct);
        await EnsureRoleMenuHiddenAsync(frontOfficeRole.Id, appointmentsGroup.Id, ct);
        var opdGroup = await EnsureMenuAsync(null, "OPD", "stethoscope", null, null, 7, ct);
        await EnsureRoleMenuHiddenAsync(frontOfficeRole.Id, opdGroup.Id, ct);
        var ipdGroup = await EnsureMenuAsync(null, "IPD", "bed-double", null, null, 11, ct);
        await EnsureRoleMenuHiddenAsync(frontOfficeRole.Id, ipdGroup.Id, ct);
        var emergencyGroup = await EnsureMenuAsync(null, "Emergency", "siren", null, null, 16, ct);
        await EnsureRoleMenuHiddenAsync(frontOfficeRole.Id, emergencyGroup.Id, ct);
        var mhcGroup = await EnsureMenuAsync(null, "MHC", "clipboard-plus", null, null, 22, ct);
        await EnsureRoleMenuHiddenAsync(frontOfficeRole.Id, mhcGroup.Id, ct);
        var loyaltyGroup = await EnsureMenuAsync(null, "Loyalty", "credit-card", null, null, 23, ct);
        await EnsureRoleMenuHiddenAsync(frontOfficeRole.Id, loyaltyGroup.Id, ct);
        var mastersGroup = await EnsureMenuAsync(null, "Masters", "database", null, null, 80, ct);
        await EnsureRoleMenuHiddenAsync(frontOfficeRole.Id, mastersGroup.Id, ct);
        var administrationGroup = await EnsureMenuAsync(null, "Administration", "settings", null, null, 90, ct);
        await EnsureRoleMenuHiddenAsync(frontOfficeRole.Id, administrationGroup.Id, ct);
    }

    /// <summary>Upserts (roleId, menuId)'s position for the FRONT_OFFICE seed layout — same idempotent get-or-set pattern as EnsureMenuAsync, but for a MenuRoleLayout row instead of a Menu row. No title override: every reused item here already carries the exact wording wanted.</summary>
    private async Task EnsureRoleMenuLayoutAsync(long roleId, long menuId, long? parentId, int sortOrder, CancellationToken ct)
    {
        var layout = await db.MenuRoleLayouts.FirstOrDefaultAsync(l => l.RoleId == roleId && l.MenuId == menuId, ct);
        if (layout is null)
        {
            layout = new MenuRoleLayout { RoleId = roleId, MenuId = menuId };
            db.MenuRoleLayouts.Add(layout);
        }
        layout.ParentId = parentId;
        layout.SortOrder = sortOrder;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Marks (roleId, menuId) Hidden — drops it (and its whole subtree) from that role's sidebar only; position fields are irrelevant once hidden so this leaves them alone for an item with no other override.</summary>
    private async Task EnsureRoleMenuHiddenAsync(long roleId, long menuId, CancellationToken ct)
    {
        var layout = await db.MenuRoleLayouts.FirstOrDefaultAsync(l => l.RoleId == roleId && l.MenuId == menuId, ct);
        if (layout is null)
        {
            layout = new MenuRoleLayout { RoleId = roleId, MenuId = menuId, ParentId = null, SortOrder = 0 };
            db.MenuRoleLayouts.Add(layout);
        }
        layout.Hidden = true;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Mirror of EnsureRoleMenuHiddenAsync for a HiddenByDefault item: explicitly marks (roleId, menuId) Shown for this role, at the item's own global position (Hidden = false actually renders the node, so — unlike the hidden case — ParentId/SortOrder must match where it really lives).</summary>
    private async Task EnsureRoleMenuShownAsync(long roleId, long menuId, long? parentId, int sortOrder, CancellationToken ct)
    {
        var layout = await db.MenuRoleLayouts.FirstOrDefaultAsync(l => l.RoleId == roleId && l.MenuId == menuId, ct);
        if (layout is null)
        {
            layout = new MenuRoleLayout { RoleId = roleId, MenuId = menuId };
            db.MenuRoleLayouts.Add(layout);
        }
        layout.ParentId = parentId;
        layout.SortOrder = sortOrder;
        layout.Hidden = false;
        await db.SaveChangesAsync(ct);
    }

    private async Task<Menu> EnsureMenuAsync(
        long? parentId, string title, string? icon, string? path, string? permissionCode,
        int sortOrder, CancellationToken ct, bool hiddenByDefault = false)
    {
        // Derived from this call site's own literal args (not the live row), so it stays
        // stable even after an admin renames/reparents the row via the Menus admin screen —
        // the next seeder run still finds it by Code and won't re-insert a duplicate. Title is
        // always included (not just Path) because two different seeded items can legitimately
        // point at the same Path (e.g. "IP Registration" and "Admissions" both open
        // /ipd/admissions) — Path alone would collide and silently skip the second insert.
        var code = $"{path ?? parentId?.ToString() ?? "root"}::{title}";

        var menu = await db.Menus.FirstOrDefaultAsync(m => m.Code == code, ct);
        if (menu is null)
        {
            menu = new Menu
            {
                Code = code,
                ParentId = parentId,
                Title = title,
                Icon = icon,
                Path = path,
                PermissionCode = permissionCode,
                SortOrder = sortOrder,
                HiddenByDefault = hiddenByDefault,
            };
            db.Menus.Add(menu);
            await db.SaveChangesAsync(ct);
        }
        return menu;
    }

    /// <summary>
    /// Universal reference values only (units, GST slabs, specimen/modality catalogues,
    /// baseline patient category) — hospital-specific data is never hardcoded.
    /// </summary>
    private async Task SeedMasterDefaultsAsync(CancellationToken ct)
    {
        await EnsureSimpleMastersAsync(db.Uoms,
        [
            ("NOS", "Numbers"), ("TAB", "Tablet"), ("CAP", "Capsule"), ("ML", "Millilitre"),
            ("MG", "Milligram"), ("GM", "Gram"), ("VIAL", "Vial"), ("AMP", "Ampoule"),
            ("BOX", "Box"), ("STRIP", "Strip"), ("BOTTLE", "Bottle"), ("PAIR", "Pair"),
            ("LTR", "Litre"), ("KG", "Kilogram"), ("MCG", "Microgram"), ("IU", "International Unit"),
            ("SACHET", "Sachet"), ("TUBE", "Tube"), ("DROP", "Drop"), ("PATCH", "Patch"),
            ("SUPP", "Suppository"), ("INHALER", "Inhaler"), ("RESPULE", "Respule"), ("KIT", "Kit"),
            ("PACK", "Pack"), ("ROLL", "Roll"), ("METER", "Metre"), ("SET", "Set"),
            ("PIECE", "Piece"), ("DOSE", "Dose"), ("PREFILL", "Pre-filled Syringe"), ("SYRINGE", "Syringe"),
        ], (code, name) => new Uom { Code = code, Name = name }, ct);

        await EnsureSimpleMastersAsync(db.SampleTypes,
        [
            ("WB", "Whole Blood"), ("SER", "Serum"), ("PLAS", "Plasma"), ("URINE", "Urine"),
            ("STOOL", "Stool"), ("CSF", "Cerebrospinal Fluid"), ("SPUTUM", "Sputum"),
            ("SWAB", "Swab"), ("TISSUE", "Tissue"), ("FLUID", "Body Fluid"),
            ("EDTA", "EDTA Blood"), ("CITRATE", "Citrated Plasma"), ("HEPARIN", "Heparinised Blood"),
            ("FLUORIDE", "Fluoride Blood"), ("ABG", "Arterial Blood"), ("URINE24", "24-Hour Urine"),
            ("NASO_SWAB", "Nasopharyngeal Swab"), ("THROAT_SWAB", "Throat Swab"),
            ("WOUND_SWAB", "Wound Swab"), ("PUS", "Pus"), ("SEMEN", "Semen"),
            ("PLEURAL", "Pleural Fluid"), ("ASCITIC", "Ascitic Fluid"), ("SYNOVIAL", "Synovial Fluid"),
            ("PERICARDIAL", "Pericardial Fluid"), ("MARROW", "Bone Marrow"), ("AMNIOTIC", "Amniotic Fluid"),
            ("HAIR", "Hair"), ("NAIL", "Nail Clipping"), ("SKIN", "Skin Scraping"),
        ], (code, name) => new SampleType { Code = code, Name = name }, ct);

        await EnsureSimpleMastersAsync(db.Modalities,
        [
            ("XRAY", "X-Ray"), ("CT", "CT Scan"), ("MRI", "MRI"), ("USG", "Ultrasound"),
            ("ECHO", "Echocardiography"), ("MAMMO", "Mammography"), ("ECG", "ECG"), ("EEG", "EEG"),
            ("PETCT", "PET-CT"), ("DEXA", "DEXA Bone Densitometry"), ("FLUORO", "Fluoroscopy"),
            ("OPG", "Orthopantomogram"), ("DOPPLER", "Doppler Study"), ("ANGIO", "Angiography"),
            ("NCS", "Nerve Conduction Study"), ("EMG", "Electromyography"), ("TMT", "Treadmill Test"),
            ("HOLTER", "Holter Monitoring"), ("SPIRO", "Spirometry"), ("ENDO", "Endoscopy"),
            ("COLONO", "Colonoscopy"), ("BRONCHO", "Bronchoscopy"), ("NUCLEAR", "Nuclear Medicine"),
            ("CARM", "C-Arm Imaging"), ("PORTABLE", "Portable X-Ray"),
        ], (code, name) => new Modality { Code = code, Name = name }, ct);

        // Bespoke (not ISimpleMaster, carries ColorHex — "patient-type colors"), so this can't
        // use the generic EnsureSimpleMastersAsync helper.
        var existingPatientCategoryCodes = await db.PatientCategories.Select(c => c.Code).ToListAsync(ct);
        (string Code, string Name, string ColorHex)[] patientCategoryDefaults =
        [
            ("GEN", "General", "#3B82F6"),
            // "Nationality & Pricelist Mapping" (legacy menu) is closed by reuse: seed a
            // Foreign category here rather than a new nationality dimension — Tariff
            // already resolves rate by PatientCategoryId (BillMath.ResolveTariffRate).
            ("FOREIGN", "Foreign National", "#F59E0B"),
            // Each of these resolves its own tariff rate, which is the whole point of the
            // category — a hospital prices staff, scheme and corporate patients differently.
            ("STAFF", "Hospital Staff", "#8B5CF6"),
            ("STAFF_DEP", "Staff Dependent", "#A78BFA"),
            ("SENIOR", "Senior Citizen", "#14B8A6"),
            ("CORPORATE", "Corporate", "#0EA5E9"),
            ("INSURANCE", "Insurance / TPA", "#6366F1"),
            ("BPL", "Below Poverty Line", "#22C55E"),
            ("SCHEME", "Government Scheme", "#84CC16"),
            ("CAMP", "Camp Patient", "#F97316"),
            ("VIP", "VIP", "#EAB308"),
            ("NRI", "NRI", "#EC4899"),
        ];
        foreach (var (code, name, colorHex) in patientCategoryDefaults.Where(d => !existingPatientCategoryCodes.Contains(d.Code)))
        {
            db.PatientCategories.Add(new PatientCategory { Code = code, Name = name, ColorHex = colorHex });
        }
        await db.SaveChangesAsync(ct);

        await EnsureSimpleMastersAsync(db.RecordDocumentTypes,
        [
            ("DISCHARGE_SUMMARY", "Discharge Summary"), ("CONSENT_FORM", "Consent Form"),
            ("PROGRESS_NOTES", "Progress Notes"), ("LAB_REPORT", "Lab Report"),
            ("RADIOLOGY_REPORT", "Radiology Report"), ("REFERRAL_LETTER", "Referral Letter"),
            ("OPERATION_NOTE", "Operation Note"), ("ANAESTHESIA_RECORD", "Anaesthesia Record"),
            ("ICU_CHART", "ICU Chart"), ("NURSING_NOTES", "Nursing Notes"),
            ("MEDICATION_CHART", "Medication Chart"), ("MLC_FORM", "MLC Form"),
            ("INSURANCE_DOC", "Insurance / TPA Document"), ("IDENTITY_PROOF", "Identity Proof"),
            ("BIRTH_CERTIFICATE", "Birth Certificate"), ("DEATH_CERTIFICATE", "Death Certificate"),
            ("IMAGING_FILM", "Imaging Film"), ("BILLING_DOC", "Billing Document"),
            ("TRANSFER_NOTE", "Transfer Note"), ("AUTOPSY_REPORT", "Autopsy Report"),
            ("OTHER", "Other"),
        ], (code, name) => new Domain.Entities.Masters.RecordDocumentType { Code = code, Name = name }, ct);

        // Universal person-descriptor lists. These used to sit in DemoDataSeeder, but a title,
        // a marital status and a government ID type are the same everywhere — a real hospital
        // needs them on day one, not just a demo environment.
        await EnsureSimpleMastersAsync(db.Salutations,
        [
            ("MR", "Mr."), ("MRS", "Mrs."), ("MS", "Ms."), ("MISS", "Miss"), ("DR", "Dr."),
            ("PROF", "Prof."), ("MASTER", "Master"), ("BABY", "Baby"), ("BABY_OF", "Baby of"),
            ("MX", "Mx."), ("REV", "Rev."), ("CAPT", "Capt."), ("MAJ", "Maj."), ("COL", "Col."),
            ("SR", "Sr."), ("BR", "Br."), ("SHRI", "Shri"), ("SMT", "Smt."), ("KUM", "Kumari"),
        ], (code, name) => new Salutation { Code = code, Name = name }, ct);

        await EnsureSimpleMastersAsync(db.MaritalStatuses,
        [
            ("SINGLE", "Single"), ("MARRIED", "Married"), ("DIVORCED", "Divorced"),
            ("WIDOWED", "Widowed"), ("SEPARATED", "Separated"), ("ENGAGED", "Engaged"),
            ("PARTNERED", "Domestic Partnership"), ("MINOR", "Minor / Not Applicable"),
            ("UNKNOWN", "Not Disclosed"),
        ], (code, name) => new MaritalStatus { Code = code, Name = name }, ct);

        // Aadhaar and ABHA are captured on the patient record itself (dedicated columns), so
        // they are intentionally absent here — this list is for the *other* proofs a
        // registration desk scans.
        await EnsureSimpleMastersAsync(db.IdentityDocumentTypes,
        [
            ("PAN", "PAN Card"), ("PASSPORT", "Passport"), ("VOTER", "Voter ID"),
            ("DL", "Driving License"), ("RATION", "Ration Card"),
            ("EMPLOYEE_ID", "Employee ID Card"), ("STUDENT_ID", "Student ID Card"),
            ("SENIOR_CARD", "Senior Citizen Card"), ("DISABILITY_CERT", "Disability Certificate"),
            ("BPL_CARD", "BPL Card"), ("INSURANCE_CARD", "Insurance / TPA Card"),
            ("CGHS", "CGHS Card"), ("ESI", "ESI Card"), ("EX_SERVICEMAN", "Ex-Serviceman ID"),
            ("BIRTH_CERT", "Birth Certificate"), ("NREGA", "NREGA Job Card"),
            ("OCI", "OCI / PIO Card"), ("VISA", "Visa Document"),
            ("OTHER", "Other"),
        ], (code, name) => new IdentityDocumentType { Code = code, Name = name }, ct);

        // Front Office masters — only genuinely universal/standardized lists get seed rows;
        // hospital-specific ones (Camp, City, Patient HC Tag, Patient Case Type, MHC Doctor,
        // Loyalty Program, Unit) start empty for each hospital to populate via their own screen.
        await EnsureSimpleMastersAsync(db.States,
        [
            ("AP", "Andhra Pradesh"), ("AR", "Arunachal Pradesh"), ("AS", "Assam"), ("BR", "Bihar"),
            ("CG", "Chhattisgarh"), ("GA", "Goa"), ("GJ", "Gujarat"), ("HR", "Haryana"),
            ("HP", "Himachal Pradesh"), ("JH", "Jharkhand"), ("KA", "Karnataka"), ("KL", "Kerala"),
            ("MP", "Madhya Pradesh"), ("MH", "Maharashtra"), ("MN", "Manipur"), ("ML", "Meghalaya"),
            ("MZ", "Mizoram"), ("NL", "Nagaland"), ("OD", "Odisha"), ("PB", "Punjab"),
            ("RJ", "Rajasthan"), ("SK", "Sikkim"), ("TN", "Tamil Nadu"), ("TS", "Telangana"),
            ("TR", "Tripura"), ("UP", "Uttar Pradesh"), ("UK", "Uttarakhand"), ("WB", "West Bengal"),
            ("AN", "Andaman and Nicobar Islands"), ("CH", "Chandigarh"),
            ("DNHDD", "Dadra and Nagar Haveli and Daman and Diu"), ("DL", "Delhi"),
            ("JK", "Jammu and Kashmir"), ("LA", "Ladakh"), ("LD", "Lakshadweep"), ("PY", "Puducherry"),
        ], (code, name) => new Domain.Entities.Masters.State { Code = code, Name = name }, ct);

        await EnsureSimpleMastersAsync(db.Nationalities,
        [
            ("IN", "Indian"), ("OTHER", "Other / Foreign National"),
        ], (code, name) => new Domain.Entities.Masters.Nationality { Code = code, Name = name }, ct);

        await EnsureSimpleMastersAsync(db.Religions,
        [
            ("HINDU", "Hindu"), ("MUSLIM", "Muslim"), ("CHRISTIAN", "Christian"), ("SIKH", "Sikh"),
            ("BUDDHIST", "Buddhist"), ("JAIN", "Jain"), ("PARSI", "Parsi"), ("JEWISH", "Jewish"),
            ("OTHER", "Other"),
        ], (code, name) => new Domain.Entities.Masters.Religion { Code = code, Name = name }, ct);

        await EnsureSimpleMastersAsync(db.Languages,
        [
            ("EN", "English"), ("HI", "Hindi"), ("BN", "Bengali"), ("TE", "Telugu"), ("MR", "Marathi"),
            ("TA", "Tamil"), ("UR", "Urdu"), ("GU", "Gujarati"), ("KN", "Kannada"), ("OR", "Odia"),
            ("ML", "Malayalam"), ("PA", "Punjabi"), ("AS", "Assamese"),
            ("MAI", "Maithili"), ("SAT", "Santali"), ("KS", "Kashmiri"), ("NE", "Nepali"),
            ("SD", "Sindhi"), ("KOK", "Konkani"), ("DOI", "Dogri"), ("MNI", "Manipuri"),
            ("BRX", "Bodo"), ("SA", "Sanskrit"), ("TCY", "Tulu"), ("BHO", "Bhojpuri"),
            ("MWR", "Marwari"), ("AR", "Arabic"), ("FR", "French"),
            ("OTHER", "Other"),
        ], (code, name) => new Domain.Entities.Masters.Language { Code = code, Name = name }, ct);

        await EnsureSimpleMastersAsync(db.SpeciallyAbledCategories,
        [
            ("VISUAL", "Visual Impairment"), ("HEARING", "Hearing Impairment"),
            ("LOCOMOTOR", "Locomotor Disability"), ("INTELLECTUAL", "Intellectual Disability"),
            ("MENTAL", "Mental Illness"), ("MULTIPLE", "Multiple Disabilities"),
            // The rest of the specified disabilities under the Rights of Persons with
            // Disabilities Act 2016, so the registration dropdown matches the statutory list
            // a hospital has to report against.
            ("BLINDNESS", "Blindness"), ("LOW_VISION", "Low Vision"),
            ("SPEECH", "Speech and Language Disability"), ("AUTISM", "Autism Spectrum Disorder"),
            ("CEREBRAL_PALSY", "Cerebral Palsy"), ("LEARNING", "Specific Learning Disability"),
            ("DWARFISM", "Dwarfism"), ("MUSCULAR_DYS", "Muscular Dystrophy"),
            ("THALASSEMIA", "Thalassaemia"), ("HAEMOPHILIA", "Haemophilia"),
            ("SICKLE_CELL", "Sickle Cell Disease"), ("CHRONIC_NEURO", "Chronic Neurological Condition"),
            ("ACID_ATTACK", "Acid Attack Victim"), ("LEPROSY_CURED", "Leprosy Cured Person"),
            ("PARKINSONS", "Parkinsons Disease"), ("MS", "Multiple Sclerosis"),
            ("OTHER", "Other"),
        ], (code, name) => new Domain.Entities.Masters.SpeciallyAbledCategory { Code = code, Name = name }, ct);

        await EnsureSimpleMastersAsync(db.MlcTypes,
        [
            ("RTA", "Road Traffic Accident"), ("ASSAULT", "Assault"), ("POISONING", "Poisoning"),
            ("BURN", "Burns"), ("SNAKEBITE", "Snake Bite"), ("FIREARM", "Firearm Injury"),
            ("SUICIDE_ATTEMPT", "Suicide Attempt"), ("HOMICIDE", "Suspected Homicide"),
            ("UNNATURAL_DEATH", "Unnatural Death"),
            ("DROWNING", "Drowning"), ("ELECTROCUTION", "Electrocution"),
            ("FALL_HEIGHT", "Fall From Height"), ("INDUSTRIAL", "Industrial / Workplace Accident"),
            ("RAILWAY", "Railway Accident"), ("ANIMAL_BITE", "Dog / Animal Bite"),
            ("SEXUAL_ASSAULT", "Sexual Assault"), ("CUSTODIAL", "Custodial Injury"),
            ("DOMESTIC_VIOLENCE", "Domestic Violence"), ("CHILD_ABUSE", "Suspected Child Abuse"),
            ("ALCOHOL", "Alcohol Intoxication"), ("DRUG_OVERDOSE", "Drug Overdose"),
            ("HANGING", "Hanging"), ("BROUGHT_DEAD", "Brought Dead"),
            ("OTHER", "Other"),
        ], (code, name) => new Domain.Entities.Masters.MlcType { Code = code, Name = name }, ct);

        // Indian GST slabs.
        (string Code, string Name, decimal Rate)[] taxes =
            [("GST0", "GST 0%", 0m), ("GST5", "GST 5%", 5m), ("GST12", "GST 12%", 12m),
             ("GST18", "GST 18%", 18m), ("GST28", "GST 28%", 28m)];
        var existingTaxes = await db.Taxes.Select(t => t.Code).ToListAsync(ct);
        foreach (var (code, name, rate) in taxes.Where(t => !existingTaxes.Contains(t.Code)))
        {
            db.Taxes.Add(new Tax { Code = code, Name = name, RatePercent = rate });
        }
        await db.SaveChangesAsync(ct);

        (string Code, string Name, bool RequiresReference)[] paymentModes =
            [("CASH", "Cash", false), ("CARD", "Card", true), ("UPI", "UPI", true), ("CHEQUE", "Cheque", true),
             ("NEFT", "NEFT / RTGS", true), ("NETBANKING", "Net Banking", true),
             ("WALLET", "Mobile Wallet", true), ("DD", "Demand Draft", true),
             // Settled by a third party rather than collected at the counter — these carry a
             // reference (claim/authorisation/PO number) so Finance can reconcile them later.
             ("INSURANCE", "Insurance / TPA", true), ("CORPORATE", "Corporate Credit", true),
             ("SCHEME", "Government Scheme", true), ("ADVANCE_ADJ", "Advance Adjustment", false)];
        var existingModes = await db.PaymentModes.Select(m => m.Code).ToListAsync(ct);
        foreach (var (code, name, requiresReference) in paymentModes.Where(m => !existingModes.Contains(m.Code)))
        {
            db.PaymentModes.Add(new PaymentMode { Code = code, Name = name, RequiresReference = requiresReference });
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Baseline retention policies (plan §17: "policy per record type") — hospital-wide, editable later via Retention Policies.</summary>
    private async Task SeedRetentionPoliciesAsync(CancellationToken ct)
    {
        (string Code, string Name, int Years)[] policies =
        [
            ("IP_RECORD", "Inpatient Record", 10),
            ("OP_RECORD", "Outpatient Record", 5),
            ("MLC_RECORD", "Medico-Legal Case Record", 25),
            // Minor/paediatric records run from majority rather than from the episode, so they
            // are held longest; maternity and implant records carry their own statutory tails.
            ("PAEDIATRIC_RECORD", "Paediatric Record", 21),
            ("MATERNITY_RECORD", "Maternity & Obstetric Record", 25),
            ("IMPLANT_RECORD", "Implant / Prosthesis Record", 15),
            ("ONCOLOGY_RECORD", "Oncology Record", 15),
            ("PSYCHIATRY_RECORD", "Psychiatric Record", 15),
            ("EMERGENCY_RECORD", "Emergency / Casualty Record", 5),
            ("DAYCARE_RECORD", "Day Care Record", 5),
            ("IMAGING_RECORD", "Radiology Images & Reports", 8),
            ("LAB_RECORD", "Laboratory Records", 5),
            ("BLOOD_RECORD", "Blood Transfusion Record", 10),
            ("DEATH_RECORD", "Death Case Record", 10),
            ("CONSENT_RECORD", "Consent Forms", 10),
            ("OT_RECORD", "Operation Theatre Record", 10),
            ("BIOMED_RECORD", "Biomedical Equipment Record", 7),
            ("FINANCE_RECORD", "Billing & Financial Record", 8),
        ];
        var existing = await db.RetentionPolicies.Select(p => p.Code).ToListAsync(ct);
        foreach (var (code, name, years) in policies.Where(p => !existing.Contains(p.Code)))
        {
            db.RetentionPolicies.Add(new Domain.Entities.Mrd.RetentionPolicy { Code = code, Name = name, RetentionYears = years });
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Starter set of commonly-tracked hospital quality/patient-safety indicators (2026-08-08),
    /// so `quality.quality_indicators` isn't empty on a fresh install — previously a hospital had
    /// to type every indicator in by hand via the Quality Indicators screen before recording any
    /// monthly data. This is NOT a verbatim reproduction of any specific NABH edition's Objective
    /// Element wording or a claim of official numbering — it's the widely-recognized set of
    /// hospital quality indicators (bed occupancy, LOS, HAI rates, falls, medication errors,
    /// TAT, etc.) that most Indian hospital quality/NABH programs track, given as a reviewable
    /// starting point. TargetValue is deliberately left null on every row: the acceptable
    /// threshold is a hospital-policy/Quality Committee decision, not something to invent here.
    /// A hospital's Quality Manager should review, rename, retarget, or delete any row via the
    /// existing Indicators screen — this seed never overwrites/re-adds a Code the admin already
    /// edited or removed, same idempotent-by-Code posture as SeedRetentionPoliciesAsync above.
    /// AutoComputeSource is set only where Prixo already has a matching engine (bed occupancy,
    /// lab TAT); everything else is manual entry, since forcing a mismatched auto-compute source
    /// (e.g. the generic org-wide IncidentCount) onto a category-specific indicator like "Patient
    /// Falls" would silently record the wrong number — an accuracy risk in a compliance record.
    /// </summary>
    private async Task SeedQualityIndicatorsAsync(CancellationToken ct)
    {
        (string Code, string Name, string Category, string NumeratorDefinition, string DenominatorDefinition,
            string Unit, QualityAutoComputeSource? AutoComputeSource)[] indicators =
        [
            ("QI-01", "Bed Occupancy Rate", "Facility & Operations",
                "Total occupied bed-days in the period", "Total available bed-days in the period", "%",
                QualityAutoComputeSource.BedOccupancy),
            ("QI-02", "Average Length of Stay", "Clinical Care",
                "Total inpatient days for discharges in the period", "Total number of discharges in the period", "Days", null),
            ("QI-03", "Patient/Family Satisfaction Rate", "Patient Experience",
                "Number of patients/families rating overall care as satisfactory or above",
                "Total number of patients/families surveyed", "%", null),
            ("QI-04", "Patient/Family Complaint Rate", "Patient Experience",
                "Number of complaints received in the period", "Total number of patients served in the period", "%", null),
            ("QI-05", "Employee Satisfaction Rate", "Human Resources",
                "Number of staff rating overall satisfaction as satisfactory or above",
                "Total number of staff surveyed", "%", null),
            ("QI-06", "OPD Initial Assessment Time", "Clinical Care",
                "Sum of time (minutes) from registration to initial clinical assessment", "Number of OPD patients assessed", "Minutes", null),
            ("QI-07", "Emergency Registration-to-Assessment Time", "Emergency Care",
                "Sum of time (minutes) from ER registration to physician assessment", "Number of ER patients assessed", "Minutes", null),
            ("QI-08", "Mortality Rate within 48 Hours of Admission", "Clinical Care",
                "Number of deaths within 48 hours of admission", "Total number of admissions in the period", "%", null),
            ("QI-09", "LAMA Rate (Left Against Medical Advice)", "Clinical Care",
                "Number of patients who left against medical advice", "Total number of discharges in the period", "%", null),
            ("QI-10", "Unplanned Readmission Rate (within 30 Days)", "Clinical Care",
                "Number of unplanned readmissions within 30 days of discharge", "Total number of discharges in the period", "%", null),
            ("QI-11", "Unplanned Return to OT Rate", "Surgical Care",
                "Number of unplanned returns to OT", "Total number of surgeries performed", "%", null),
            ("QI-12", "Unplanned ICU Readmission Rate (within 48 Hours)", "Clinical Care",
                "Number of unplanned ICU readmissions within 48 hours of ICU discharge", "Total number of ICU discharges", "%", null),
            ("QI-13", "Incidence of Patient Falls", "Patient Safety",
                "Number of reported patient fall incidents", "Total patient-days in the period", "Per 1000 patient-days", null),
            ("QI-14", "Incidence of Hospital-Acquired Pressure Ulcers", "Patient Safety",
                "Number of new pressure ulcers/bed sores identified after admission", "Total patient-days in the period", "Per 1000 patient-days", null),
            ("QI-15", "Medication Error Rate", "Medication Safety",
                "Number of medication errors reported", "Total number of prescriptions/medication orders in the period", "%", null),
            ("QI-16", "Adverse Drug Reaction (ADR) Rate", "Medication Safety",
                "Number of ADRs reported", "Total number of patients on medication in the period", "%", null),
            ("QI-17", "Blood/Blood-Component Transfusion Reaction Rate", "Patient Safety",
                "Number of transfusion reactions reported", "Total number of units transfused", "%", null),
            ("QI-18", "Needle-Stick / Sharps Injury Rate", "Staff Safety",
                "Number of needle-stick/sharps injuries reported", "Total number of staff in the period", "Per 100 staff", null),
            ("QI-19", "Hand Hygiene Compliance Rate", "Infection Control",
                "Number of hand-hygiene opportunities observed with compliance", "Total number of hand-hygiene opportunities observed", "%", null),
            ("QI-20", "Surgical Site Infection (SSI) Rate", "Infection Control",
                "Number of surgical site infections identified", "Total number of surgical procedures in the period", "%", null),
            ("QI-21", "Catheter-Associated Urinary Tract Infection (CAUTI) Rate", "Infection Control",
                "Number of CAUTI cases identified", "Total urinary catheter-days in the period", "Per 1000 catheter-days", null),
            ("QI-22", "Central Line-Associated Bloodstream Infection (CLABSI) Rate", "Infection Control",
                "Number of CLABSI cases identified", "Total central line-days in the period", "Per 1000 line-days", null),
            ("QI-23", "Ventilator-Associated Pneumonia (VAP) Rate", "Infection Control",
                "Number of VAP cases identified", "Total ventilator-days in the period", "Per 1000 ventilator-days", null),
            ("QI-24", "Average Laboratory Turnaround Time", "Diagnostics",
                "Sum of turnaround hours across lab orders with a result in the period", "Number of lab orders with a result in the period", "Hours",
                QualityAutoComputeSource.LabTat),
            ("QI-25", "Average Radiology Turnaround Time", "Diagnostics",
                "Sum of turnaround hours across radiology orders with a report in the period", "Number of radiology orders with a report in the period", "Hours", null),
            ("QI-26", "Discharge Summary Completion Rate (within Defined Time)", "Medical Records",
                "Number of discharge summaries completed within the hospital's defined time", "Total number of discharges in the period", "%", null),
            ("QI-27", "Medical Record Deficiency Rate", "Medical Records",
                "Number of medical records with a missing/incomplete mandatory field on audit", "Total number of medical records audited", "%", null),
            ("QI-28", "Nurse-to-Bed Ratio", "Human Resources",
                "Number of nursing staff on duty", "Total number of occupied/staffed beds", "Ratio", null),
            ("QI-29", "Critical Value Communication Compliance", "Patient Safety",
                "Number of critical values communicated to the treating clinician within the defined time", "Total number of critical values reported", "%", null),
            ("QI-30", "CAPA Closure-within-Timeline Rate", "Quality Management",
                "Number of CAPAs closed within their defined due date", "Total number of CAPAs due for closure in the period", "%", null),
        ];

        var existing = await db.QualityIndicators.Select(i => i.Code).ToListAsync(ct);
        foreach (var (code, name, category, num, den, unit, source) in indicators.Where(i => !existing.Contains(i.Code)))
        {
            db.QualityIndicators.Add(new QualityIndicator
            {
                Code = code, Name = name, Category = category,
                NumeratorDefinition = num, DenominatorDefinition = den, Unit = unit, AutoComputeSource = source,
            });
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Baseline Chart of Accounts (plan §18) — just enough for the auto-posting rules to have
    /// somewhere to post: Cash/Bank (asset), Accounts Receivable/Payable, Patient/Pharmacy
    /// revenue, a generic expense account. The three accounts the posting rules look up by
    /// code (SystemAccountCodes) are marked IsSystemAccount and protected from deletion; also
    /// links the default CASH payment mode to Cash and CARD/UPI/CHEQUE to Bank, so a fresh dev
    /// database posts real ledger entries on the very first bill payment.
    /// </summary>
    private async Task SeedFinanceDefaultsAsync(CancellationToken ct)
    {
        (string Code, string Name, AccountType Type, bool IsSystem)[] accounts =
        [
            ("1000", "Cash", AccountType.Asset, false),
            ("1010", "Bank Account", AccountType.Asset, false),
            (SystemAccountCodes.AccountsReceivable, "Accounts Receivable", AccountType.Asset, true),
            ("1200", "Inventory", AccountType.Asset, false),
            (SystemAccountCodes.AccountsPayable, "Accounts Payable", AccountType.Liability, true),
            ("3000", "Capital", AccountType.Equity, false),
            (SystemAccountCodes.PatientRevenue, "Patient Revenue", AccountType.Income, true),
            ("4100", "Pharmacy Revenue", AccountType.Income, false),
            ("5000", "Operating Expenses", AccountType.Expense, false),
        ];
        var existing = await db.Accounts.ToDictionaryAsync(a => a.Code, ct);
        foreach (var (code, name, type, isSystem) in accounts)
        {
            if (!existing.ContainsKey(code))
            {
                db.Accounts.Add(new Account { Code = code, Name = name, AccountType = type, IsSystemAccount = isSystem });
            }
        }
        await db.SaveChangesAsync(ct);

        var cashAccountId = await db.Accounts.Where(a => a.Code == "1000").Select(a => a.Id).FirstAsync(ct);
        var bankAccountId = await db.Accounts.Where(a => a.Code == "1010").Select(a => a.Id).FirstAsync(ct);
        var modes = await db.PaymentModes.Where(m => m.LinkedAccountId == null).ToListAsync(ct);
        foreach (var mode in modes)
        {
            mode.LinkedAccountId = mode.Code == "CASH" ? cashAccountId : bankAccountId;
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>One default pharmacy store per branch so day-one GRN/sale flows have somewhere to post stock.</summary>
    private async Task SeedDefaultStoreAsync(Branch branch, CancellationToken ct)
    {
        if (await db.Stores.AnyAsync(s => s.BranchId == branch.Id && s.Code == "MAIN", ct))
        {
            return;
        }
        db.Stores.Add(new Domain.Entities.Pharmacy.Store { BranchId = branch.Id, Code = "MAIN", Name = "Main Pharmacy Store", IsDefault = true });
        await db.SaveChangesAsync(ct);
    }

    /// <summary>One default theatre per branch so day-one OT booking has somewhere to book against — same reasoning as the default pharmacy store.</summary>
    private async Task SeedDefaultTheatreAsync(Branch branch, CancellationToken ct)
    {
        if (await db.Theatres.AnyAsync(t => t.BranchId == branch.Id && t.Code == "OT1", ct))
        {
            return;
        }
        db.Theatres.Add(new Domain.Entities.Ot.Theatre { BranchId = branch.Id, Code = "OT1", Name = "Main Operation Theatre" });
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Custom Report Builder v1 catalog (plan §17/§23). Three rows are genuinely "executable"
    /// against the new `reporting` materialized views; the rest are "linked" entries pointing
    /// the report picker at dedicated report pages earlier phases already built (Lab/Rad TAT,
    /// Finance reports, Biomed reports, MRD reports, Billing day close) — see docs/modules/reports.md
    /// for which is which.
    /// </summary>
    private async Task SeedReportDefinitionsAsync(CancellationToken ct)
    {
        (string Code, string Name, string Description, string Category, string Permission, string? ViewName, string? LinkPath, string ColumnsJson)[] definitions =
        [
            ("REVENUE_DAILY", "Revenue (Daily)", "Billed revenue by day, department and doctor.", "Finance", PermissionRegistry.FinanceReportView,
                "mv_revenue_daily", null,
                """[{"key":"revenueDate","label":"Date"},{"key":"departmentName","label":"Department"},{"key":"doctorName","label":"Doctor"},{"key":"amount","label":"Amount"}]"""),
            ("DAILY_COLLECTION", "Daily Collection", "Cash/bank collection by day and payment mode.", "Billing", PermissionRegistry.FinanceReportView,
                "mv_daily_collection", null,
                """[{"key":"collectionDate","label":"Date"},{"key":"paymentModeName","label":"Payment Mode"},{"key":"amount","label":"Amount"},{"key":"txnCount","label":"Transactions"}]"""),
            ("OP_CENSUS", "OP Census", "OPD visit counts by day, doctor and visit type.", "OPD", PermissionRegistry.VisitView,
                "mv_op_census", null,
                """[{"key":"visitDate","label":"Date"},{"key":"doctorName","label":"Doctor"},{"key":"visitType","label":"Visit Type"},{"key":"visitCount","label":"Visits"}]"""),
            ("OP_TAT", "OP TAT Report", "Registration-to-consult and consult-to-completion turnaround time.", "OPD", PermissionRegistry.VisitView,
                null, "/opd/reports/tat", "[]"),
            ("OP_CANCEL", "OP Cancel Report", "Cancelled OPD visits with reason.", "OPD", PermissionRegistry.VisitView,
                null, "/opd/reports/cancelled", "[]"),
            ("OP_CONVERSION", "OP to IP Conversion Report", "Completed OPD visits vs. those converted to an admission.", "OPD", PermissionRegistry.VisitView,
                null, "/opd/reports/conversion", "[]"),
            ("OP_REFERRAL", "Referral Report", "Internal/external referrals raised from OPD visits.", "OPD", PermissionRegistry.VisitView,
                null, "/opd/reports/referrals", "[]"),
            ("MHC_REPORTS", "MHC Reports", "Package-wise, doctor-wise and referrer-wise MHC registration breakdowns.", "MHC", PermissionRegistry.MhcVisitView,
                null, "/mhc/reports", "[]"),
            ("APPOINTMENT_DOCTOR_REPORT", "Doctor-Appointment Report", "Per-doctor Scheduled/Checked-in/Completed/Cancelled/No-show breakdown.", "Appointments", PermissionRegistry.AppointmentView,
                null, "/appointments/reports/doctor-appointment", "[]"),
            ("PATIENT_BLOCK_UNBLOCK", "Patient Block/Unblock Report", "Patient block/unblock history from the activity log.", "Patients", PermissionRegistry.PatientBlock,
                null, "/patients/reports/block-unblock", "[]"),
            ("LAB_TAT", "Lab TAT Report", "Turnaround time by test/department.", "Lab", PermissionRegistry.LabReportView, null, "/lab/tat-report", "[]"),
            ("LAB_UNBILLED", "Lab Unbilled Orders Report", "Resulted tests with no billed line attached — advisory only.", "Lab", PermissionRegistry.LabReportView, null, "/lab/unbilled-report", "[]"),
            ("RAD_TAT", "Radiology TAT Report", "Turnaround time by test/modality.", "Radiology", PermissionRegistry.RadReportView, null, "/rad/tat-report", "[]"),
            ("RAD_UNBILLED", "Radiology Unbilled Orders Report", "Reported studies with no billed line attached — advisory only.", "Radiology", PermissionRegistry.RadReportView, null, "/rad/unbilled-report", "[]"),
            ("FINANCE_REPORTS", "Finance Reports", "Cash/bank collection, revenue, GST, outstanding.", "Finance", PermissionRegistry.FinanceReportView, null, "/finance/reports", "[]"),
            ("BILLING_DAY_CLOSE", "Billing Day Close", "Branch day-close summary.", "Billing", PermissionRegistry.DayCloseView, null, "/billing/day-close", "[]"),
            ("BILLING_REPORTS", "Billing Reports", "Ageing, outstanding, day book, concession, refund, advance, tariff list, doctors payout, settlements.", "Billing", PermissionRegistry.BillingReportView, null, "/billing/reports", "[]"),
            ("PHARMACY_REPORTS", "Pharmacy Reports", "Near-expiry/low-stock alerts and drug consumption.", "Pharmacy", PermissionRegistry.PharmacyReportView, null, "/pharmacy/alerts", "[]"),
            ("BIOMED_REPORTS", "Biomedical Reports", "Equipment downtime and PM compliance.", "Biomedical", PermissionRegistry.BiomedReportView, null, "/biomed/reports", "[]"),
            ("MRD_REPORTS", "MRD Reports", "Coding stats, file movement, incomplete records.", "MRD", PermissionRegistry.MrdReportView, null, "/mrd/reports", "[]"),
            ("AUDIT_REPORT", "Audit Report", "Row-level change history across the system.", "Admin", PermissionRegistry.AuditView, null, "/admin/audit-logs", "[]"),
        ];

        var existing = await db.ReportDefinitions.Select(d => d.Code).ToListAsync(ct);
        foreach (var (code, name, description, category, permission, viewName, linkPath, columnsJson) in definitions)
        {
            if (!existing.Contains(code))
            {
                db.ReportDefinitions.Add(new ReportDefinition
                {
                    Code = code, Name = name, Description = description, Category = category,
                    PermissionCode = permission, ViewName = viewName, LinkPath = linkPath, ColumnsJson = columnsJson,
                });
            }
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Generic starter consent templates — hospitals customize the wording per policy.</summary>
    private async Task SeedConsentTemplatesAsync(CancellationToken ct)
    {
        (string Code, string Title, string BodyText)[] templates =
        [
            ("GENERAL_TREATMENT", "General Consent for Treatment",
                "I hereby consent to examination and general treatment by the hospital's medical and nursing staff, " +
                "including routine diagnostic procedures, as considered necessary in my case."),
            ("SURGICAL", "Consent for Surgical/Invasive Procedure",
                "I hereby authorize the surgeon and anaesthetist to perform the procedure explained to me, including " +
                "anaesthesia and any additional procedures they consider necessary during the operation."),
            ("PHOTO_DATA_USE", "Consent for Photography and Data Use",
                "I consent to clinical photography/imaging for my medical record and to the use of my de-identified " +
                "health data for quality improvement, subject to the hospital's privacy policy."),
        ];

        var existing = await db.ConsentTemplates.Select(c => c.Code).ToListAsync(ct);
        foreach (var (code, title, body) in templates.Where(t => !existing.Contains(t.Code)))
        {
            db.ConsentTemplates.Add(new ConsentTemplate { Code = code, Title = title, BodyText = body });
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureSimpleMastersAsync<TEntity>(
        DbSet<TEntity> set, (string Code, string Name)[] defaults,
        Func<string, string, TEntity> create, CancellationToken ct)
        where TEntity : class, Domain.Common.ISimpleMaster
    {
        var existing = await set.Select(e => EF.Property<string>(e, "Code")).ToListAsync(ct);
        foreach (var (code, name) in defaults.Where(d => !existing.Contains(d.Code)))
        {
            set.Add(create(code, name));
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task SeedSettingsAsync(CancellationToken ct)
    {
        (string Key, string Value, string Group, string Description)[] defaults =
        [
            (SettingKeys.PasswordMinLength, "8", "Password", "Minimum password length"),
            (SettingKeys.PasswordRequireUppercase, "true", "Password", "Require an uppercase letter"),
            (SettingKeys.PasswordRequireLowercase, "true", "Password", "Require a lowercase letter"),
            (SettingKeys.PasswordRequireDigit, "true", "Password", "Require a digit"),
            (SettingKeys.PasswordRequireSpecial, "true", "Password", "Require a special character"),
            (SettingKeys.PasswordExpiryDays, "0", "Password", "Password expiry in days (0 = never)"),
            (SettingKeys.PasswordHistoryCount, "5", "Password", "Recent passwords that cannot be reused"),
            (SettingKeys.PasswordForceChangeOnFirstLogin, "true", "Password",
                "Force a password change at first login for new and admin-reset accounts"),
            (SettingKeys.LockoutMaxFailedAttempts, "5", "Security", "Failed logins before lockout (0 = disabled)"),
            (SettingKeys.LockoutMinutes, "15", "Security", "Lockout duration in minutes"),
            (SettingKeys.SessionIdleTimeoutMinutes, "30", "Session", "Idle minutes before a session expires (0 = disabled)"),
            (SettingKeys.SessionMaxConcurrent, "0", "Session", "Max concurrent sessions per user (0 = unlimited)"),
            (SettingKeys.BackupSchedule, "0 2 * * *", "Backup", "Backup schedule (cron, informational)"),
            (SettingKeys.BackupRetentionDays, "30", "Backup", "Days to keep backups"),
            (SettingKeys.NotificationSmsEnabled, "false", "Notification", "SMS notifications (Communication module — held)"),
            (SettingKeys.NotificationEmailEnabled, "false", "Notification", "Email notifications (Communication module — held)"),
            (SettingKeys.NotificationWhatsAppEnabled, "false", "Notification", "WhatsApp notifications (Communication module — held)"),
            (SettingKeys.PatientUhidPrefix, "PRX", "Patient", "UHID prefix, e.g. PRX-2026-000123"),
            (SettingKeys.PatientUhidDigits, "6", "Patient", "Digits in the UHID running number"),
            (SettingKeys.RadiologyPacsViewerUrl, "", "Radiology", "PACS viewer URL template (placeholder — PACS/DICOM integration not built)"),
            (SettingKeys.BillingReceiptFooterNote, "", "Billing", "Custom footer line on the bill receipt PDF (empty = default thank-you line)"),
            (SettingKeys.BillingDefaultConsultationFee, "0", "Billing", "Fallback consultation fee used only when a doctor's own fee is 0"),
            (SettingKeys.BillingDiagnosticsGate, "Paid", "Billing", "OP lab/radiology billing gate before sample collection / scan scheduling: Paid, Billed or None"),
            (SettingKeys.OtHourlyChargeServiceCode, "", "Billing", "Service code billed per started hour of surgery on OT completion (empty = none)"),
            (SettingKeys.BillingDefaultFollowUpFee, "0", "Billing", "Fallback follow-up fee used only when a doctor's own fee is 0"),
            (SettingKeys.FrontOfficeMaxAdvanceBookingDays, "90", "Front Office", "Days ahead front-desk staff may book an appointment (0 = unlimited)"),
            (SettingKeys.CensusChildAgeThresholdYears, "12", "Front Office", "Age in years (strictly under) counted as \"Child\" instead of Male/Female on the OPIP Census Report"),
            (SettingKeys.AbdmEnabled, "true", "ABDM / Ayushman Bharat", "Master switch: Enable or disable ABDM integration"),
            (SettingKeys.AbdmM1Enabled, "true", "ABDM / Ayushman Bharat", "Milestone 1: Enable ABHA Creation & Verification"),
            (SettingKeys.AbdmM2Enabled, "true", "ABDM / Ayushman Bharat", "Milestone 2: Enable HIP Care Context Linking & FHIR Health Data Sharing"),
            (SettingKeys.AbdmM3Enabled, "true", "ABDM / Ayushman Bharat", "Milestone 3: Enable HIU Consent Requests & External Health Records"),
            (SettingKeys.AbdmBaseUrl, "https://dev.abdm.gov.in/gateway", "ABDM / Ayushman Bharat", "ABDM Gateway URL"),
            (SettingKeys.AbdmClientId, "SBX_PRIXO_DEV", "ABDM / Ayushman Bharat", "ABDM Client ID"),
            (SettingKeys.AbdmClientSecret, "secret-placeholder", "ABDM / Ayushman Bharat", "ABDM Client Secret"),
            (SettingKeys.AbdmHipId, "IN2910000123", "ABDM / Ayushman Bharat", "Facility HIP ID (Health Facility Registry)"),
            (SettingKeys.AbdmHiuId, "IN2910000123", "ABDM / Ayushman Bharat", "Facility HIU ID"),
        ];

        var existing = await db.SystemSettings.Select(s => s.Key).ToListAsync(ct);
        foreach (var (key, value, group, description) in defaults.Where(d => !existing.Contains(d.Key)))
        {
            db.SystemSettings.Add(new SystemSetting
            {
                Key = key,
                Value = value,
                Group = group,
                Description = description,
            });
        }
        await db.SaveChangesAsync(ct);
    }
}
