using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Admin.Auth;
using Prixo.Application.Features.Appointments;
using Prixo.Application.Features.Admin.Branches;
using Prixo.Application.Features.Admin.Designations;
using Prixo.Application.Features.Admin.Logs;
using Prixo.Application.Features.Admin.Menus;
using Prixo.Application.Features.Admin.Roles;
using Prixo.Application.Features.Admin.Sessions;
using Prixo.Application.Features.Admin.Settings;
using Prixo.Application.Features.Admin.Users;
using Prixo.Application.Features.Billing;
using Prixo.Application.Features.DoctorPortal;
using Prixo.Application.Features.Emr;
using Prixo.Application.Features.Reports;
using Prixo.Application.Features.Ipd;
using Prixo.Application.Features.Lab;
using Prixo.Application.Features.Masters.Billing;
using Prixo.Application.Features.Pharmacy;
using Prixo.Application.Features.Masters.Areas;
using Prixo.Application.Features.Masters.Common;
using Prixo.Application.Features.Masters.Corporates;
using Prixo.Application.Features.Masters.Departments;
using Prixo.Application.Features.Masters.Doctors;
using Prixo.Application.Features.Masters.Drugs;
using Prixo.Application.Features.Masters.Employees;
using Prixo.Application.Features.Masters.Consents;
using Prixo.Application.Features.Masters.Icd;
using Prixo.Application.Features.Masters.Insurance;
using Prixo.Application.Features.Masters.Items;
using Prixo.Application.Features.Masters.AppointmentTypes;
using Prixo.Application.Features.Masters.Locations;
using Prixo.Application.Features.Masters.Loyalty;
using Prixo.Application.Features.Masters.MhcPackages;
using Prixo.Application.Features.Masters.Packages;
using Prixo.Application.Features.Masters.PatientCategories;
using Prixo.Application.Features.Masters.Units;
using Prixo.Application.Features.Loyalty;
using Prixo.Application.Features.Masters.Referrers;
using Prixo.Application.Features.Masters.Services;
using Prixo.Application.Features.Masters.Tariffs;
using Prixo.Application.Features.Masters.Vendors;
using Prixo.Application.Features.Biomed;
using Prixo.Application.Features.Er;
using Prixo.Application.Features.Finance;
using Prixo.Application.Features.Icu;
using Prixo.Application.Features.Insurance;
using Prixo.Application.Features.Mrd;
using Prixo.Application.Features.Nursing;
using Prixo.Application.Features.Ot;
using Prixo.Application.Features.Opd;
using Prixo.Application.Features.Patients;
using Prixo.Application.Features.Portal;
using Prixo.Application.Features.Quality;
using Prixo.Application.Features.Rad;

namespace Prixo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddMemoryCache();

        // Admin / RBAC (Phase 1)
        services.AddScoped<AuthService>();
        services.AddScoped<PermissionService>();
        services.AddScoped<PasswordPolicyService>();
        services.AddScoped<SettingService>();
        services.AddScoped<UserService>();
        services.AddScoped<RoleService>();
        services.AddScoped<RolePresetService>();
        services.AddScoped<MenuService>();
        services.AddScoped<BranchService>();
        services.AddScoped<DesignationService>();
        services.AddScoped<SessionService>();
        services.AddScoped<LogQueryService>();

        // Masters (Phase 2) — generic simple-master framework
        services.AddScoped<DepartmentService>();
        services.AddScoped<SpecialtyService>();
        services.AddScoped<PatientCategoryService>();
        services.AddScoped<ServiceCategoryService>();
        services.AddScoped<ItemCategoryService>();
        services.AddScoped<GenericService>();
        services.AddScoped<ManufacturerService>();
        services.AddScoped<UomService>();
        services.AddScoped<SampleTypeService>();
        services.AddScoped<ModalityService>();

        // Masters (Phase 2) — dedicated services
        services.AddScoped<DoctorService>();
        services.AddScoped<EmployeeService>();
        services.AddScoped<ServiceCatalogService>();
        services.AddScoped<TariffService>();
        services.AddScoped<PackageService>();
        services.AddScoped<BillEstimateTemplateService>();
        services.AddScoped<DoctorPayoutRuleService>();
        services.AddScoped<LocationService>();
        services.AddScoped<VendorService>();
        services.AddScoped<ItemService>();
        services.AddScoped<DrugService>();
        services.AddScoped<TaxService>();
        services.AddScoped<DiscountTypeService>();
        services.AddScoped<ReferrerService>();
        services.AddScoped<IcdService>();
        services.AddScoped<CorporateService>();
        services.AddScoped<InsuranceCompanyService>();
        services.AddScoped<ConsentTemplateService>();
        services.AddScoped<RecordDocumentTypeService>();

        // Front Office masters
        services.AddScoped<CampService>();
        services.AddScoped<StateService>();
        services.AddScoped<CityService>();
        services.AddScoped<NationalityService>();
        services.AddScoped<ReligionService>();
        services.AddScoped<LanguageService>();
        services.AddScoped<SpeciallyAbledCategoryService>();
        services.AddScoped<PatientHcTagService>();
        services.AddScoped<PatientCaseTypeService>();
        services.AddScoped<MlcTypeService>();
        services.AddScoped<MhcDoctorService>();
        services.AddScoped<LoyaltyProgramService>();
        services.AddScoped<UnitService>();
        services.AddScoped<AreaService>();
        services.AddScoped<MhcPackageService>();
        services.AddScoped<LoyaltyCardCategoryService>();
        services.AddScoped<MemberRelationTagService>();
        services.AddScoped<LoyaltyCardTypeService>();
        services.AddScoped<LoyaltyCardPriceService>();
        services.AddScoped<LoyaltyCardService>();
        services.AddScoped<UnitDoctorService>();
        services.AddScoped<AppointmentTypeService>();
        services.AddScoped<SalutationService>();
        services.AddScoped<MaritalStatusService>();
        services.AddScoped<IdentityDocumentTypeService>();
        services.AddScoped<VisitClassService>();
        services.AddScoped<VisitSessionService>();
        services.AddScoped<ClaimDocumentTypeService>();
        services.AddScoped<ClaimDenialReasonService>();

        // Patients (Phase 3)
        services.AddScoped<PatientService>();
        services.AddScoped<PatientReportService>();
        services.AddScoped<Features.Patients.Abdm.FhirBundleService>();
        services.AddScoped<Features.Patients.Abdm.AbdmService>();
        services.AddScoped<Features.Patients.Abdm.AbdmHipService>();
        services.AddScoped<Features.Patients.Abdm.AbdmHiuService>();
        services.AddScoped<Features.Patients.Abdm.AbdmAdminService>();

        // Appointments (Phase 4)
        services.AddScoped<DoctorScheduleService>();
        services.AddScoped<UnitScheduleService>();
        services.AddScoped<AppointmentService>();
        services.AddScoped<AppointmentReportService>();
        services.AddScoped<INoShowJob, NoShowJob>();

        // OPD (Phase 5)
        services.AddScoped<OpVisitService>();
        services.AddScoped<PrescriptionTemplateService>();
        services.AddScoped<OpReportService>();

        // Billing (Phase 6)
        services.AddScoped<BillingService>();
        services.AddScoped<DayCloseService>();
        services.AddScoped<PaymentModeService>();
        services.AddScoped<AdvanceService>();
        services.AddScoped<OnlinePaymentRequestService>();
        services.AddScoped<IAdvanceCollector>(sp => sp.GetRequiredService<AdvanceService>());
        services.AddScoped<IIpBedChargeRefreshJob, IpBedChargeRefreshJob>();
        services.AddScoped<CashierShiftService>();
        services.AddScoped<BillEstimateService>();
        services.AddScoped<BillingReportService>();

        // Laboratory (Phase 7)
        services.AddScoped<LabTestService>();
        services.AddScoped<TestProfileService>();
        services.AddScoped<LabOrderService>();

        // Pharmacy (Phase 8)
        services.AddScoped<StoreService>();
        services.AddScoped<PurchaseService>();
        services.AddScoped<PharmacySaleService>();
        services.AddScoped<IssueService>();
        services.AddScoped<StockService>();
        services.AddScoped<IndentService>();

        // IPD (Phase 9)
        services.AddScoped<AdmissionService>();
        services.AddScoped<AdmissionRequestService>();
        services.AddScoped<DoctorOrderService>();
        services.AddScoped<ClinicalChartService>();
        services.AddScoped<DischargeService>();
        services.AddScoped<BedBoardService>();
        services.AddScoped<IpReportService>();

        // Nursing (Phase 10)
        services.AddScoped<NursingChartService>();
        services.AddScoped<MarService>();
        services.AddScoped<HandoverService>();
        services.AddScoped<IncidentService>();

        // EMR (Phase 11)
        services.AddScoped<HistoryService>();
        services.AddScoped<ProblemListService>();
        services.AddScoped<ClinicalNoteService>();
        services.AddScoped<AttachmentService>();
        services.AddScoped<TimelineService>();

        // Radiology (Phase 12)
        services.AddScoped<RadiologyTestService>();
        services.AddScoped<RadReportTemplateService>();
        services.AddScoped<RadOrderService>();

        // Operation Theatre (Phase 14)
        services.AddScoped<OtMasterService>();
        services.AddScoped<BookingService>();
        services.AddScoped<OtClinicalService>();
        services.AddScoped<OtUsageService>();

        // ICU / Critical Care (Phase 15)
        services.AddScoped<CriticalAlertRuleService>();
        services.AddScoped<IcuChartService>();
        services.AddScoped<IcuVitalsService>();
        services.AddScoped<IcuDashboardService>();

        // Emergency / Casualty (Phase 16)
        services.AddScoped<EmergencyVisitService>();
        services.AddScoped<EmergencyAssessmentService>();
        services.AddScoped<MlcService>();
        services.AddScoped<ErReportService>();

        // MRD (Phase 17)
        services.AddScoped<RecordFileService>();
        services.AddScoped<FileRequestService>();
        services.AddScoped<ScannedDocumentService>();
        services.AddScoped<CodingWorklistService>();
        services.AddScoped<FileAuditService>();
        services.AddScoped<RetentionPolicyService>();
        services.AddScoped<DestructionRegisterService>();
        services.AddScoped<MrdReportService>();

        // Finance & Accounts (Phase 18)
        services.AddScoped<AccountService>();
        services.AddScoped<VoucherService>();
        services.AddScoped<IFinanceLedgerPoster>(sp => sp.GetRequiredService<VoucherService>());
        services.AddScoped<LedgerService>();
        services.AddScoped<PayableService>();
        services.AddScoped<ReceivableService>();
        services.AddScoped<FinanceReportService>();

        // Biomedical Engineering (Phase 19)
        services.AddScoped<EquipmentService>();
        services.AddScoped<AmcContractService>();
        services.AddScoped<WorkOrderService>();
        services.AddScoped<PmScheduleService>();
        services.AddScoped<BreakdownComplaintService>();
        services.AddScoped<CalibrationService>();
        services.AddScoped<ServiceHistoryService>();
        services.AddScoped<BiomedReportService>();

        // Quality / NABH (Phase 20)
        services.AddScoped<QualityIndicatorService>();
        services.AddScoped<QualityAuditService>();
        services.AddScoped<QualityNonConformanceService>();
        services.AddScoped<QualityRiskService>();
        services.AddScoped<QualityCapaService>();
        services.AddScoped<QualityCommitteeMeetingService>();
        services.AddScoped<QualityEvidenceService>();
        services.AddScoped<QualityDashboardService>();
        services.AddScoped<QualityCommitteeService>();
        services.AddScoped<QualityNabhStandardService>();

        // Patient Portal (Phase 21)
        services.AddScoped<PortalAuthService>();
        services.AddScoped<PortalAppointmentService>();
        services.AddScoped<PortalRecordsService>();
        services.AddScoped<PortalFeedbackService>();

        // Doctor Portal (Phase 22)
        services.AddScoped<DoctorPortalService>();

        // Reports & Dashboards (Phase 23)
        services.AddScoped<DashboardService>();
        services.AddScoped<ReportCatalogService>();
        services.AddScoped<ReportExecutionService>();
        services.AddScoped<ReportSubscriptionService>();
        services.AddScoped<IReportRefreshJob, ReportRefreshJob>();
        services.AddScoped<CensusReportService>();

        // Insurance / TPA (Phase 24)
        services.AddScoped<PatientInsurancePolicyService>();
        services.AddScoped<PreAuthService>();
        services.AddScoped<ClaimService>();

        return services;
    }
}
