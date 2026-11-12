using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prixo.Domain.Entities.Finance;
using Prixo.Domain.Entities.Icu;
using Prixo.Domain.Entities.Lab;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Opd;
using Prixo.Domain.Entities.Rad;
using Prixo.Domain.Enums;

namespace Prixo.Infrastructure.Persistence;

/// <summary>
/// Catalog depth for the demo environment.
///
/// <para>The other <see cref="DemoDataSeeder"/> sections fill modules that ship with *no* rows.
/// This one addresses the different complaint that they don't: catalogs which technically have
/// rows but far too few to demo or screenshot — a Stores item dropdown with 3 items in 1
/// category, a lab order screen offering 17 tests and 1 profile, a radiology report-entry screen
/// with 1 template, a Chart of Accounts of 9 accounts, and no tariff at all on branches 2 and 3
/// (so rate resolution there silently fell back to Service.DefaultRate for every line).</para>
///
/// <para>It also repairs two catalog defects that broke reporting independently of how much data
/// existed. <c>reporting.mv_revenue_daily</c> groups revenue by
/// <c>bill_items → services.department_id</c>; 16 of the 50 seeded services (every consultation,
/// room, OT, nursing and procedure charge) had a null DepartmentId, so department-wise revenue
/// could only ever resolve Laboratory and Radiology and dumped everything else into one
/// unlabelled bucket. <see cref="BackfillServiceDepartmentsAsync"/> assigns those by service
/// code. The matching half of that defect — bill lines not carrying a ServiceId at all — is
/// fixed in the history seeder, which always writes one.</para>
///
/// <para>Idempotency is per row and keyed by the natural code (not "does the table have any
/// rows"), because unlike the empty-module sections these tables already contain rows that must
/// be left alone. Re-running only ever adds what is missing.</para>
/// </summary>
public partial class DemoDataSeeder
{
    private async Task SeedCatalogDepthAsync(DemoContext ctx, CancellationToken ct)
    {
        await BackfillServiceDepartmentsAsync(ct);
        await SeedServiceDepthAsync(ct);
        await SeedTariffsForAllBranchesAsync(ct);
        await SeedChartOfAccountsDepthAsync(ct);
        await SeedItemCatalogDepthAsync(ct);
        await SeedDrugCatalogDepthAsync(ct);
        await SeedLabCatalogDepthAsync(ct);
        await SeedRadiologyCatalogDepthAsync(ct);
        await SeedIcdDepthAsync(ct);
        await SeedPackageDepthAsync(ct);
        await SeedPrescriptionTemplateDepthAsync(ct);
        await SeedIcuAlertRuleDepthAsync(ct);
        await SeedNationalityDepthAsync(ct);
    }

    // ── Service → Department backfill ─────────────────────────────────────────
    /// <summary>
    /// Gives the 16 department-less baseline services a home department, keyed by service code.
    /// Without this, department-wise revenue (mv_revenue_daily) can only group Lab and Radiology.
    /// Only ever fills a null — never reassigns a department someone has already chosen.
    /// </summary>
    private async Task BackfillServiceDepartmentsAsync(CancellationToken ct)
    {
        (string ServiceCode, string DepartmentCode)[] map =
        [
            ("S0001", "GENMED"), ("S0002", "GENMED"), ("S0003", "GENMED"),
            ("S0004", "FRONT"),
            ("S0005", "NURSG"), ("S0006", "NURSG"), ("S0007", "NURSG"), ("S0009", "NURSG"),
            ("S0008", "ICU"),
            ("S0010", "EMERG"),
            ("S0011", "GSURG"), ("S0012", "GSURG"),
            ("S0013", "OT"), ("S0014", "OT"),
            ("S0015", "ANAES"),
            ("S0016", "PHYSIO"),
        ];

        var departments = await db.Departments.ToDictionaryAsync(d => d.Code, d => d.Id, ct);
        var codes = map.Select(m => m.ServiceCode).ToList();
        var services = await db.Services.Where(s => codes.Contains(s.Code) && s.DepartmentId == null).ToListAsync(ct);
        if (services.Count == 0)
        {
            return;
        }

        foreach (var service in services)
        {
            var departmentCode = map.First(m => m.ServiceCode == service.Code).DepartmentCode;
            if (departments.TryGetValue(departmentCode, out var departmentId))
            {
                service.DepartmentId = departmentId;
            }
        }
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Demo: backfilled DepartmentId on {Count} services", services.Count);
    }

    // ── Services ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Specialty procedure/consultation charges across the clinical departments, so
    /// department-wise revenue has more than two departments to show and the billing
    /// service-picker looks like a real hospital's catalog rather than a starter list.
    /// </summary>
    private async Task SeedServiceDepthAsync(CancellationToken ct)
    {
        // (Code, Name, ServiceCategoryCode, DepartmentCode, DefaultRate)
        (string Code, string Name, string Category, string Department, decimal Rate)[] rows =
        [
            // Consultations, per specialty — the single biggest OP revenue line.
            ("S0051", "Consultation - Cardiology", "SVC1", "CARD", 900m),
            ("S0052", "Consultation - Orthopedics", "SVC1", "ORTHO", 800m),
            ("S0053", "Consultation - Pediatrics", "SVC1", "PEDS", 700m),
            ("S0054", "Consultation - Obstetrics & Gynaecology", "SVC1", "GYNEC", 800m),
            ("S0055", "Consultation - ENT", "SVC1", "ENT", 700m),
            ("S0056", "Consultation - Ophthalmology", "SVC1", "OPHTH", 700m),
            ("S0057", "Consultation - Dermatology", "SVC1", "DERMA", 800m),
            ("S0058", "Consultation - General Surgery", "SVC1", "GSURG", 800m),
            ("S0059", "Consultation - Neurology", "SVC1", "NEURO", 1000m),
            ("S0060", "Consultation - Nephrology", "SVC1", "NEPHRO", 900m),
            ("S0061", "Consultation - Urology", "SVC1", "UROLOGY", 900m),
            ("S0062", "Consultation - Oncology", "SVC1", "ONCO", 1200m),
            ("S0063", "Consultation - Pulmonology", "SVC1", "PULMO", 800m),
            ("S0064", "Consultation - Gastroenterology", "SVC1", "GASTRO", 900m),
            ("S0065", "Consultation - Endocrinology", "SVC1", "ENDO", 900m),
            ("S0066", "Consultation - Psychiatry", "SVC1", "PSYCH", 1000m),
            ("S0067", "Consultation - Dentistry", "SVC1", "DENTAL", 500m),
            ("S0068", "Consultation - Plastic Surgery", "SVC1", "PLASTIC", 1000m),
            ("S0069", "Consultation - Neurosurgery", "SVC1", "NEUROSURG", 1200m),
            ("S0070", "Consultation - Cardiothoracic Surgery", "SVC1", "CTVS", 1200m),
            ("S0071", "Consultation - Rheumatology", "SVC1", "RHEUM", 900m),
            ("S0072", "Consultation - Emergency Medicine", "SVC1", "EMERG", 600m),
            ("S0073", "Consultation - Dietetics & Nutrition", "SVC1", "DIETETICS", 400m),

            // Cardiology
            ("S0074", "Holter Monitoring - 24 Hour", "SVC2", "CARD", 3500m),
            ("S0075", "Stress Echocardiography", "SVC2", "CARD", 4500m),
            ("S0076", "Coronary Angiography", "SVC2", "CARD", 22000m),
            ("S0077", "Coronary Angioplasty with Stent", "SVC6", "CARD", 165000m),
            ("S0078", "Permanent Pacemaker Implantation", "SVC6", "CARD", 210000m),
            ("S0079", "Ambulatory BP Monitoring", "SVC2", "CARD", 2200m),

            // Orthopedics
            ("S0080", "Total Knee Replacement - Unilateral", "SVC6", "ORTHO", 195000m),
            ("S0081", "Total Hip Replacement - Unilateral", "SVC6", "ORTHO", 215000m),
            ("S0082", "Arthroscopic ACL Reconstruction", "SVC6", "ORTHO", 95000m),
            ("S0083", "Closed Reduction with POP Cast", "SVC2", "ORTHO", 4500m),
            ("S0084", "Intra-articular Injection", "SVC2", "ORTHO", 3000m),
            ("S0085", "Plaster / Splint Application", "SVC2", "ORTHO", 1500m),

            // General surgery
            ("S0086", "Laparoscopic Cholecystectomy", "SVC6", "GSURG", 78000m),
            ("S0087", "Laparoscopic Appendicectomy", "SVC6", "GSURG", 62000m),
            ("S0088", "Open Inguinal Hernia Repair", "SVC6", "GSURG", 55000m),
            ("S0089", "Haemorrhoidectomy", "SVC6", "GSURG", 42000m),
            ("S0090", "Incision & Drainage of Abscess", "SVC2", "GSURG", 6500m),
            ("S0091", "Wound Debridement & Dressing", "SVC2", "GSURG", 2500m),

            // Obstetrics & gynaecology
            ("S0092", "Normal Vaginal Delivery", "SVC6", "GYNEC", 45000m),
            ("S0093", "Lower Segment Caesarean Section", "SVC6", "GYNEC", 85000m),
            ("S0094", "Total Abdominal Hysterectomy", "SVC6", "GYNEC", 95000m),
            ("S0095", "Dilatation & Curettage", "SVC2", "GYNEC", 15000m),
            ("S0096", "Antenatal Check-up", "SVC2", "GYNEC", 600m),
            ("S0097", "Pap Smear Collection", "SVC2", "GYNEC", 900m),

            // Pediatrics / neonatal
            ("S0098", "Neonatal Phototherapy per day", "SVC2", "PEDS", 3500m),
            ("S0099", "Immunisation Administration", "SVC2", "PEDS", 400m),
            ("S0100", "Paediatric Nebulisation", "SVC2", "PEDS", 500m),
            ("S0101", "Newborn Hearing Screening", "SVC2", "PEDS", 1200m),

            // ENT
            ("S0102", "Tonsillectomy", "SVC6", "ENT", 48000m),
            ("S0103", "Functional Endoscopic Sinus Surgery", "SVC6", "ENT", 72000m),
            ("S0104", "Myringotomy with Grommet Insertion", "SVC6", "ENT", 35000m),
            ("S0105", "Diagnostic Nasal Endoscopy", "SVC2", "ENT", 2500m),
            ("S0106", "Pure Tone Audiometry", "SVC5", "ENT", 1200m),
            ("S0107", "Ear Syringing / Wax Removal", "SVC2", "ENT", 800m),

            // Ophthalmology
            ("S0108", "Cataract Surgery with Foldable IOL", "SVC6", "OPHTH", 42000m),
            ("S0109", "YAG Laser Capsulotomy", "SVC2", "OPHTH", 9500m),
            ("S0110", "Intravitreal Injection", "SVC2", "OPHTH", 28000m),
            ("S0111", "Applanation Tonometry", "SVC5", "OPHTH", 600m),
            ("S0112", "Refraction & Vision Testing", "SVC5", "OPHTH", 400m),
            ("S0113", "Optical Coherence Tomography", "SVC5", "OPHTH", 3200m),

            // Dermatology
            ("S0114", "Skin Biopsy - Punch", "SVC2", "DERMA", 3500m),
            ("S0115", "Cryotherapy Session", "SVC2", "DERMA", 2500m),
            ("S0116", "Chemical Peel Session", "SVC2", "DERMA", 4000m),
            ("S0117", "Intralesional Injection", "SVC2", "DERMA", 1800m),

            // Neurology / neurosurgery
            ("S0118", "Nerve Conduction Study", "SVC5", "NEURO", 4500m),
            ("S0119", "Electromyography", "SVC5", "NEURO", 4500m),
            ("S0120", "Lumbar Puncture", "SVC2", "NEURO", 6500m),
            ("S0121", "Video EEG - 6 Hour", "SVC5", "NEURO", 8500m),
            ("S0122", "Lumbar Microdiscectomy", "SVC6", "NEUROSURG", 165000m),
            ("S0123", "Burr Hole Evacuation", "SVC6", "NEUROSURG", 125000m),

            // Nephrology / urology
            ("S0124", "Renal Biopsy", "SVC2", "NEPHRO", 12000m),
            ("S0125", "Arteriovenous Fistula Creation", "SVC6", "NEPHRO", 45000m),
            ("S0126", "Transurethral Resection of Prostate", "SVC6", "UROLOGY", 92000m),
            ("S0127", "Percutaneous Nephrolithotomy", "SVC6", "UROLOGY", 115000m),
            ("S0128", "Cystoscopy - Diagnostic", "SVC2", "UROLOGY", 12000m),
            ("S0129", "Ureteric Stent Placement", "SVC2", "UROLOGY", 28000m),
            ("S0130", "Urinary Catheterisation", "SVC2", "UROLOGY", 1200m),

            // Pulmonology
            ("S0131", "Pulmonary Function Test - Spirometry", "SVC5", "PULMO", 1800m),
            ("S0132", "Fibreoptic Bronchoscopy", "SVC2", "PULMO", 14000m),
            ("S0133", "Pleural Fluid Aspiration", "SVC2", "PULMO", 5500m),
            ("S0134", "Intercostal Drainage Tube Insertion", "SVC2", "PULMO", 12000m),
            ("S0135", "Nebulisation - Adult", "SVC2", "PULMO", 500m),

            // Gastroenterology
            ("S0136", "Upper GI Endoscopy", "SVC2", "GASTRO", 6500m),
            ("S0137", "Colonoscopy", "SVC2", "GASTRO", 11000m),
            ("S0138", "ERCP with Stenting", "SVC6", "GASTRO", 68000m),
            ("S0139", "Liver Biopsy", "SVC2", "GASTRO", 14000m),
            ("S0140", "Ascitic Fluid Tapping", "SVC2", "GASTRO", 4500m),

            // Endocrinology / rheumatology / psychiatry
            ("S0141", "Diabetic Foot Assessment", "SVC5", "ENDO", 1500m),
            ("S0142", "Insulin Titration Counselling", "SVC2", "ENDO", 800m),
            ("S0143", "Joint Aspiration", "SVC2", "RHEUM", 3500m),
            ("S0144", "Psychiatric Counselling Session", "SVC2", "PSYCH", 1500m),
            ("S0145", "Psychometric Assessment", "SVC5", "PSYCH", 3500m),

            // Oncology
            ("S0146", "Chemotherapy Administration - Day Care", "SVC2", "ONCO", 8500m),
            ("S0147", "Bone Marrow Aspiration", "SVC2", "ONCO", 9500m),
            ("S0148", "Port-a-Cath Insertion", "SVC6", "ONCO", 42000m),

            // Dental / plastic / CTVS
            ("S0149", "Dental Scaling & Polishing", "SVC2", "DENTAL", 1500m),
            ("S0150", "Dental Extraction - Simple", "SVC2", "DENTAL", 1200m),
            ("S0151", "Root Canal Treatment", "SVC2", "DENTAL", 6500m),
            ("S0152", "Dental Crown - Ceramic", "SVC2", "DENTAL", 8500m),
            ("S0153", "Split Skin Grafting", "SVC6", "PLASTIC", 55000m),
            ("S0154", "Flap Reconstruction", "SVC6", "PLASTIC", 95000m),
            ("S0155", "Coronary Artery Bypass Grafting", "SVC6", "CTVS", 325000m),
            ("S0156", "Valve Replacement - Single", "SVC6", "CTVS", 285000m),

            // Critical care / emergency
            ("S0157", "Ventilator Support per day", "SVC7", "ICU", 8500m),
            ("S0158", "Central Line Insertion", "SVC2", "ICU", 9500m),
            ("S0159", "Arterial Line Insertion", "SVC2", "ICU", 6500m),
            ("S0160", "ICU Monitoring Charge per day", "SVC7", "ICU", 4500m),
            ("S0161", "Emergency Resuscitation Charge", "SVC2", "EMERG", 7500m),
            ("S0162", "Suturing - Minor Laceration", "SVC2", "EMERG", 2200m),
            ("S0163", "Emergency Observation per hour", "SVC2", "EMERG", 600m),

            // Physiotherapy / dietetics / nursing / misc
            ("S0164", "Physiotherapy - Electrotherapy Session", "SVC2", "PHYSIO", 700m),
            ("S0165", "Physiotherapy - Post-operative Mobilisation", "SVC2", "PHYSIO", 800m),
            ("S0166", "Diet Counselling Session", "SVC2", "DIETETICS", 500m),
            ("S0167", "Therapeutic Diet per day", "SVC10", "DIETETICS", 450m),
            ("S0168", "IV Cannulation & Infusion", "SVC4", "NURSG", 600m),
            ("S0169", "Ryle's Tube Insertion", "SVC4", "NURSG", 900m),
            ("S0170", "Oxygen Administration per hour", "SVC4", "NURSG", 250m),
            ("S0171", "Medical Records Copy Charge", "SVC10", "MRD", 300m),
            ("S0172", "Medico-Legal Report Charge", "SVC10", "MRD", 1500m),
            ("S0173", "Health Certificate Issue", "SVC10", "ADMIN", 500m),
        ];

        var existing = await db.Services.Select(s => s.Code).ToListAsync(ct);
        var missing = rows.Where(r => !existing.Contains(r.Code)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        var categories = await db.ServiceCategories.ToDictionaryAsync(c => c.Code, c => c.Id, ct);
        var departments = await db.Departments.ToDictionaryAsync(d => d.Code, d => d.Id, ct);
        // Hospital services are GST-exempt in India; only the goods-like lines carry tax, so
        // leaving these untaxed matches the baseline S0001-S0050 rows rather than inventing tax.
        foreach (var (code, name, category, department, rate) in missing)
        {
            if (!categories.TryGetValue(category, out var categoryId) ||
                !departments.TryGetValue(department, out var departmentId))
            {
                continue;
            }
            db.Services.Add(new Service
            {
                Code = code,
                Name = name,
                ServiceCategoryId = categoryId,
                DepartmentId = departmentId,
                IsTaxable = false,
                DefaultRate = rate,
            });
        }
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Demo: added {Count} services", missing.Count);
    }

    // ── Tariffs ───────────────────────────────────────────────────────────────
    /// <summary>
    /// Every branch needs its own price list — tariffs are branch-scoped (BranchEntity), and only
    /// branch 1 had any, so on branches 2 and 3 the Tariff dropdown was empty and BillMath's rate
    /// resolution silently fell through to Service.DefaultRate for every single line. Branch 1's
    /// tariff set is used as the template; the other branches get a small price differential
    /// (a metro campus prices above the base one) so cross-branch revenue comparison isn't a
    /// set of identical numbers. Also tops up existing tariffs with items for any service added
    /// above, so a newly-catalogued service is actually priced rather than falling back.
    /// </summary>
    private async Task SeedTariffsForAllBranchesAsync(CancellationToken ct)
    {
        var branches = await db.Branches.IgnoreQueryFilters()
            .Where(b => !b.IsDeleted).OrderBy(b => b.Id).Select(b => b.Id).ToListAsync(ct);
        var templates = await db.Tariffs.IgnoreQueryFilters()
            .Include(t => t.Items)
            .Where(t => !t.IsDeleted)
            .ToListAsync(ct);
        var baseTariffs = templates.Where(t => t.BranchId == branches[0]).ToList();
        if (baseTariffs.Count == 0)
        {
            return;
        }

        // Multiplier per branch position: base branch as-is, later branches slightly higher.
        var multipliers = new[] { 1.00m, 1.08m, 1.15m };
        var added = 0;
        foreach (var (branchId, index) in branches.Select((b, i) => (b, i)).Skip(1))
        {
            var multiplier = multipliers[Math.Min(index, multipliers.Length - 1)];
            foreach (var template in baseTariffs)
            {
                if (templates.Any(t => t.BranchId == branchId && t.Code == template.Code))
                {
                    continue;
                }
                var tariff = new Tariff
                {
                    BranchId = branchId,
                    Code = template.Code,
                    Name = template.Name,
                    PatientCategoryId = template.PatientCategoryId,
                    EffectiveFrom = template.EffectiveFrom,
                    EffectiveTo = template.EffectiveTo,
                };
                foreach (var item in template.Items)
                {
                    tariff.Items.Add(new TariffItem
                    {
                        ServiceId = item.ServiceId,
                        RoomTypeId = item.RoomTypeId,
                        Rate = Math.Round(item.Rate * multiplier, 2),
                    });
                }
                db.Tariffs.Add(tariff);
                added++;
            }
        }
        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Demo: cloned {Count} tariffs to non-primary branches", added);
        }

        // Top up every tariff with rows for services it doesn't price yet.
        var services = await db.Services.Select(s => new { s.Id, s.DefaultRate }).ToListAsync(ct);
        var allTariffs = await db.Tariffs.IgnoreQueryFilters()
            .Include(t => t.Items).Where(t => !t.IsDeleted).ToListAsync(ct);
        var itemsAdded = 0;
        foreach (var tariff in allTariffs)
        {
            // Category-driven spread so the six patient-category tariffs aren't identical.
            var factor = tariff.Code switch
            {
                "TRF-VIP" => 1.35m,
                "TRF-FOREIGNNATIONAL" => 1.60m,
                "TRF-CORPORATE" => 0.92m,
                "TRF-INSURANCE" => 1.05m,
                "TRF-SENIORCITIZEN" => 0.85m,
                _ => 1.00m,
            };
            var branchFactor = multipliers[Math.Min(branches.IndexOf(tariff.BranchId), multipliers.Length - 1)];
            var priced = tariff.Items.Select(i => i.ServiceId).ToHashSet();
            foreach (var service in services.Where(s => !priced.Contains(s.Id)))
            {
                tariff.Items.Add(new TariffItem
                {
                    ServiceId = service.Id,
                    Rate = Math.Round(service.DefaultRate * factor * branchFactor, 2),
                });
                itemsAdded++;
            }
        }
        if (itemsAdded > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Demo: added {Count} tariff items", itemsAdded);
        }
    }

    // ── Chart of Accounts ─────────────────────────────────────────────────────
    /// <summary>
    /// Fleshes out the 9-account starter CoA into something a Finance screen can demo — voucher
    /// entry, the ledger viewer and the trial-balance-shaped reports all pick accounts from this
    /// list. The 4 system accounts DbSeeder owns (AR, AP, Patient Revenue and the bank/cash pair
    /// the payment modes link to) are left untouched; these are ordinary, deletable accounts
    /// hung underneath the same five account types.
    /// </summary>
    private async Task SeedChartOfAccountsDepthAsync(CancellationToken ct)
    {
        // (Code, Name, AccountType, ParentCode)
        (string Code, string Name, AccountType Type, string? Parent)[] rows =
        [
            // Assets
            ("1020", "Petty Cash - Front Office", AccountType.Asset, "1000"),
            ("1030", "Cash in Transit", AccountType.Asset, "1000"),
            ("1011", "Bank - Current Account", AccountType.Asset, "1010"),
            ("1012", "Bank - Collection Account", AccountType.Asset, "1010"),
            ("1013", "Card / POS Settlement Receivable", AccountType.Asset, "1010"),
            ("1014", "UPI Settlement Receivable", AccountType.Asset, "1010"),
            ("1110", "Receivable - Corporate Clients", AccountType.Asset, "1100"),
            ("1120", "Receivable - Insurance / TPA", AccountType.Asset, "1100"),
            ("1130", "Receivable - Patient Credit Bills", AccountType.Asset, "1100"),
            ("1210", "Inventory - Pharmacy", AccountType.Asset, "1200"),
            ("1220", "Inventory - General Stores", AccountType.Asset, "1200"),
            ("1230", "Inventory - Surgical Consumables", AccountType.Asset, "1200"),
            ("1300", "Advance to Suppliers", AccountType.Asset, null),
            ("1400", "Prepaid Expenses", AccountType.Asset, null),
            ("1500", "Fixed Assets - Medical Equipment", AccountType.Asset, null),
            ("1510", "Fixed Assets - Furniture & Fixtures", AccountType.Asset, null),
            ("1520", "Fixed Assets - Buildings", AccountType.Asset, null),
            ("1530", "Fixed Assets - Computers & IT", AccountType.Asset, null),
            ("1590", "Accumulated Depreciation", AccountType.Asset, null),

            // Liabilities
            ("2010", "Payable - Pharmacy Suppliers", AccountType.Liability, "2000"),
            ("2020", "Payable - General Suppliers", AccountType.Liability, "2000"),
            ("2030", "Payable - Doctor Payouts", AccountType.Liability, "2000"),
            ("2100", "Patient Advances / Deposits", AccountType.Liability, null),
            ("2200", "GST Output Payable", AccountType.Liability, null),
            ("2210", "GST Input Credit", AccountType.Liability, null),
            ("2220", "TDS Payable", AccountType.Liability, null),
            ("2300", "Salaries & Wages Payable", AccountType.Liability, null),
            ("2400", "Statutory Dues Payable (PF / ESI)", AccountType.Liability, null),

            // Equity
            ("3010", "Retained Earnings", AccountType.Equity, "3000"),
            ("3020", "Current Year Surplus / Deficit", AccountType.Equity, "3000"),

            // Income
            ("4010", "Revenue - OP Consultation", AccountType.Income, "4000"),
            ("4020", "Revenue - IP Bed & Nursing", AccountType.Income, "4000"),
            ("4030", "Revenue - Laboratory", AccountType.Income, "4000"),
            ("4040", "Revenue - Radiology & Imaging", AccountType.Income, "4000"),
            ("4050", "Revenue - Operation Theatre", AccountType.Income, "4000"),
            ("4060", "Revenue - Critical Care", AccountType.Income, "4000"),
            ("4070", "Revenue - Procedures & Day Care", AccountType.Income, "4000"),
            ("4080", "Revenue - Health Check-up Packages", AccountType.Income, "4000"),
            ("4090", "Revenue - Emergency & Ambulance", AccountType.Income, "4000"),
            ("4110", "Revenue - Pharmacy Retail", AccountType.Income, "4100"),
            ("4200", "Other Income", AccountType.Income, null),
            ("4300", "Discount Allowed (contra-revenue)", AccountType.Income, null),

            // Expenses
            ("5010", "Salaries & Wages", AccountType.Expense, "5000"),
            ("5020", "Doctor Professional Fees", AccountType.Expense, "5000"),
            ("5030", "Pharmacy Cost of Goods Sold", AccountType.Expense, "5000"),
            ("5040", "Surgical & Medical Consumables", AccountType.Expense, "5000"),
            ("5050", "Laboratory Reagents & Kits", AccountType.Expense, "5000"),
            ("5060", "Electricity & Water", AccountType.Expense, "5000"),
            ("5070", "Housekeeping & Laundry", AccountType.Expense, "5000"),
            ("5080", "Biomedical Maintenance & AMC", AccountType.Expense, "5000"),
            ("5090", "Building & Facility Maintenance", AccountType.Expense, "5000"),
            ("5100", "Biomedical Waste Disposal", AccountType.Expense, "5000"),
            ("5110", "Printing & Stationery", AccountType.Expense, "5000"),
            ("5120", "Marketing & Outreach Camps", AccountType.Expense, "5000"),
            ("5130", "Insurance Premium", AccountType.Expense, "5000"),
            ("5140", "Professional & Legal Charges", AccountType.Expense, "5000"),
            ("5150", "IT & Software Subscriptions", AccountType.Expense, "5000"),
            ("5160", "Staff Training & NABH Compliance", AccountType.Expense, "5000"),
            ("5200", "Depreciation", AccountType.Expense, null),
            ("5300", "Bank & Payment Gateway Charges", AccountType.Expense, null),
            ("5400", "Bad Debts Written Off", AccountType.Expense, null),
        ];

        var existing = await db.Accounts.ToDictionaryAsync(a => a.Code, a => a.Id, ct);
        var missing = rows.Where(r => !existing.ContainsKey(r.Code)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        // Two passes: insert without parents, then resolve parent codes (a parent may itself be
        // one of the new rows).
        foreach (var (code, name, type, _) in missing)
        {
            db.Accounts.Add(new Account { Code = code, Name = name, AccountType = type, IsSystemAccount = false });
        }
        await db.SaveChangesAsync(ct);

        var byCode = await db.Accounts.ToDictionaryAsync(a => a.Code, a => a, ct);
        foreach (var (code, _, _, parent) in missing.Where(r => r.Parent is not null))
        {
            if (byCode.TryGetValue(code, out var account) && byCode.TryGetValue(parent!, out var parentAccount))
            {
                account.ParentAccountId = parentAccount.Id;
            }
        }
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Demo: added {Count} chart-of-accounts rows", missing.Count);
    }

    // ── General-store items ───────────────────────────────────────────────────
    /// <summary>
    /// The Stores &amp; Inventory module shipped with 3 items in 1 category, which makes every
    /// indent / purchase-request / GRN item picker in that module effectively unusable. These are
    /// the non-drug consumables a hospital store actually carries.
    /// </summary>
    private async Task SeedItemCatalogDepthAsync(CancellationToken ct)
    {
        await EnsureAsync(db.ItemCategories,
        [
            ("SURGDISP", "Surgical Disposables"),
            ("CONSUM", "General Consumables"),
            ("STATION", "Stationery"),
            ("PRINTED", "Printed Forms & Registers"),
            ("HOUSEKEEP", "Housekeeping & Sanitation"),
            ("PPE", "Personal Protective Equipment"),
            ("CSSD", "CSSD & Sterilisation"),
            ("BIOMEDSP", "Biomedical Spares"),
            ("ELECTRICAL", "Electrical & Maintenance"),
            ("DIETARY", "Dietary & Kitchen"),
            ("FURNISH", "Furnishing & Fixtures"),
            ("LABCONS", "Laboratory Consumables"),
            ("LINEN", "Linen & Laundry"),
            ("RADCONS", "Radiology Consumables"),
            ("OTCONS", "Operation Theatre Consumables"),
            ("DENTAL", "Dental Consumables"),
            ("PHYSIO", "Physiotherapy Equipment"),
            ("AMBULANCE", "Ambulance & Transport"),
            ("IT", "IT & Computer Peripherals"),
            ("SECURITY", "Security & Safety"),
            ("GARDEN", "Garden & Landscaping"),
            ("BIOWASTE", "Bio-Medical Waste Handling"),
            ("OXYGEN", "Medical Gases & Oxygen"),
            ("MORTUARY", "Mortuary Supplies"),
        ], (code, name) => new ItemCategory { Code = code, Name = name }, ct);

        // (Code, Name, CategoryCode, UomCode, ReorderLevel, TaxCode)
        (string Code, string Name, string Category, string Uom, decimal Reorder, string Tax)[] rows =
        [
            // Surgical disposables
            ("ITM0001", "Disposable Syringe 2 ml", "SURGDISP", "NOS", 500m, "GST12"),
            ("ITM0002", "Disposable Syringe 5 ml", "SURGDISP", "NOS", 500m, "GST12"),
            ("ITM0003", "Disposable Syringe 10 ml", "SURGDISP", "NOS", 300m, "GST12"),
            ("ITM0004", "IV Cannula 18G", "SURGDISP", "NOS", 200m, "GST12"),
            ("ITM0005", "IV Cannula 20G", "SURGDISP", "NOS", 200m, "GST12"),
            ("ITM0006", "IV Cannula 22G", "SURGDISP", "NOS", 200m, "GST12"),
            ("ITM0007", "IV Set with Airvent", "SURGDISP", "NOS", 250m, "GST12"),
            ("ITM0008", "Three-way Stopcock", "SURGDISP", "NOS", 100m, "GST12"),
            ("ITM0009", "Foley Catheter 16 Fr", "SURGDISP", "NOS", 60m, "GST12"),
            ("ITM0010", "Urine Collection Bag 2000 ml", "SURGDISP", "NOS", 80m, "GST12"),
            ("ITM0011", "Ryle's Tube 16 Fr", "SURGDISP", "NOS", 50m, "GST12"),
            ("ITM0012", "Suction Catheter 12 Fr", "SURGDISP", "NOS", 100m, "GST12"),
            ("ITM0013", "Endotracheal Tube 7.5 mm", "SURGDISP", "NOS", 30m, "GST12"),
            ("ITM0014", "Oxygen Mask - Adult", "SURGDISP", "NOS", 80m, "GST12"),
            ("ITM0015", "Nebuliser Mask - Paediatric", "SURGDISP", "NOS", 60m, "GST12"),
            ("ITM0016", "Surgical Blade No. 11", "SURGDISP", "NOS", 100m, "GST12"),
            ("ITM0017", "Surgical Blade No. 15", "SURGDISP", "NOS", 100m, "GST12"),
            ("ITM0018", "Suture - Vicryl 2-0", "SURGDISP", "NOS", 60m, "GST12"),
            ("ITM0019", "Suture - Silk 3-0", "SURGDISP", "NOS", 60m, "GST12"),
            ("ITM0020", "Suture - Prolene 4-0", "SURGDISP", "NOS", 60m, "GST12"),
            ("ITM0021", "Disposable Scalpel", "SURGDISP", "NOS", 80m, "GST12"),
            ("ITM0022", "Umbilical Cord Clamp", "SURGDISP", "NOS", 50m, "GST12"),
            ("ITM0023", "Disposable Delivery Kit", "SURGDISP", "NOS", 25m, "GST12"),

            // General consumables / dressings
            ("ITM0024", "Cotton Roll 500 g", "CONSUM", "NOS", 60m, "GST5"),
            ("ITM0025", "Gauze Roll 10 cm", "CONSUM", "NOS", 120m, "GST5"),
            ("ITM0026", "Gauze Swab Sterile 10x10", "CONSUM", "BOX", 80m, "GST5"),
            ("ITM0027", "Crepe Bandage 10 cm", "CONSUM", "NOS", 100m, "GST5"),
            ("ITM0028", "Adhesive Tape 2 inch", "CONSUM", "NOS", 100m, "GST12"),
            ("ITM0029", "Micropore Tape 1 inch", "CONSUM", "NOS", 100m, "GST12"),
            ("ITM0030", "Transparent Film Dressing", "CONSUM", "NOS", 80m, "GST12"),
            ("ITM0031", "Povidone Iodine Solution 500 ml", "CONSUM", "BOTTLE", 40m, "GST12"),
            ("ITM0032", "Hydrogen Peroxide 500 ml", "CONSUM", "BOTTLE", 30m, "GST12"),
            ("ITM0033", "Surgical Spirit 500 ml", "CONSUM", "BOTTLE", 40m, "GST18"),
            ("ITM0034", "Hand Sanitiser 500 ml", "CONSUM", "BOTTLE", 60m, "GST18"),
            ("ITM0035", "Thermometer - Digital", "CONSUM", "NOS", 20m, "GST12"),
            ("ITM0036", "Pulse Oximeter Probe Cover", "CONSUM", "NOS", 50m, "GST12"),
            ("ITM0037", "ECG Electrode - Disposable", "CONSUM", "NOS", 300m, "GST12"),
            ("ITM0038", "ECG Gel 250 ml", "CONSUM", "BOTTLE", 20m, "GST12"),
            ("ITM0039", "Ultrasound Gel 5 litre", "CONSUM", "NOS", 10m, "GST12"),

            // PPE
            ("ITM0040", "Examination Gloves - Medium", "PPE", "BOX", 100m, "GST12"),
            ("ITM0041", "Examination Gloves - Large", "PPE", "BOX", 100m, "GST12"),
            ("ITM0042", "Sterile Surgical Gloves 7.0", "PPE", "PAIR", 150m, "GST12"),
            ("ITM0043", "Sterile Surgical Gloves 7.5", "PPE", "PAIR", 150m, "GST12"),
            ("ITM0044", "Three-ply Face Mask", "PPE", "BOX", 120m, "GST12"),
            ("ITM0045", "N95 Respirator", "PPE", "NOS", 200m, "GST12"),
            ("ITM0046", "Face Shield", "PPE", "NOS", 80m, "GST18"),
            ("ITM0047", "Disposable Surgical Gown", "PPE", "NOS", 150m, "GST12"),
            ("ITM0048", "Disposable Cap - Bouffant", "PPE", "BOX", 100m, "GST12"),
            ("ITM0049", "Shoe Cover - Disposable", "PPE", "BOX", 100m, "GST12"),
            ("ITM0050", "Lead Apron - Radiology", "PPE", "NOS", 4m, "GST18"),

            // Linen & furnishing
            ("ITM0051", "Bed Sheet - Cotton White", "LINEN", "NOS", 150m, "GST5"),
            ("ITM0052", "Pillow Cover - Cotton", "LINEN", "NOS", 150m, "GST5"),
            ("ITM0053", "Blanket - Hospital Grade", "LINEN", "NOS", 80m, "GST5"),
            ("ITM0054", "Patient Gown", "LINEN", "NOS", 200m, "GST5"),
            ("ITM0055", "Draw Sheet", "LINEN", "NOS", 150m, "GST5"),
            ("ITM0056", "OT Green Drape", "LINEN", "NOS", 100m, "GST5"),
            ("ITM0057", "Towel - Hand", "LINEN", "NOS", 120m, "GST5"),
            ("ITM0058", "Mattress Protector", "FURNISH", "NOS", 40m, "GST18"),
            ("ITM0059", "Privacy Curtain", "FURNISH", "NOS", 30m, "GST18"),
            ("ITM0060", "Bedside Locker", "FURNISH", "NOS", 10m, "GST18"),
            ("ITM0061", "IV Stand - Stainless Steel", "FURNISH", "NOS", 15m, "GST18"),
            ("ITM0062", "Wheelchair - Foldable", "FURNISH", "NOS", 6m, "GST5"),
            ("ITM0063", "Patient Trolley", "FURNISH", "NOS", 4m, "GST18"),

            // Housekeeping
            ("ITM0064", "Floor Disinfectant 5 litre", "HOUSEKEEP", "NOS", 30m, "GST18"),
            ("ITM0065", "Bleaching Powder 1 kg", "HOUSEKEEP", "NOS", 40m, "GST18"),
            ("ITM0066", "Toilet Cleaner 500 ml", "HOUSEKEEP", "BOTTLE", 50m, "GST18"),
            ("ITM0067", "Glass Cleaner 500 ml", "HOUSEKEEP", "BOTTLE", 30m, "GST18"),
            ("ITM0068", "Mop with Handle", "HOUSEKEEP", "NOS", 40m, "GST18"),
            ("ITM0069", "Garbage Bag - Yellow (Biohazard)", "HOUSEKEEP", "NOS", 400m, "GST18"),
            ("ITM0070", "Garbage Bag - Red", "HOUSEKEEP", "NOS", 400m, "GST18"),
            ("ITM0071", "Garbage Bag - Black", "HOUSEKEEP", "NOS", 400m, "GST18"),
            ("ITM0072", "Sharps Disposal Container 5 litre", "HOUSEKEEP", "NOS", 60m, "GST18"),
            ("ITM0073", "Puncture-proof Needle Box", "HOUSEKEEP", "NOS", 60m, "GST18"),
            ("ITM0074", "Air Freshener", "HOUSEKEEP", "NOS", 40m, "GST18"),
            ("ITM0075", "Hand Wash Refill 5 litre", "HOUSEKEEP", "NOS", 25m, "GST18"),

            // Stationery & printed
            ("ITM0076", "A4 Paper Ream 75 GSM", "STATION", "NOS", 100m, "GST12"),
            ("ITM0077", "Ball Point Pen - Blue", "STATION", "NOS", 300m, "GST18"),
            ("ITM0078", "Marker Pen - Permanent", "STATION", "NOS", 100m, "GST18"),
            ("ITM0079", "File Folder - Board", "STATION", "NOS", 200m, "GST18"),
            ("ITM0080", "Box File", "STATION", "NOS", 100m, "GST18"),
            ("ITM0081", "Stapler with Pins", "STATION", "NOS", 30m, "GST18"),
            ("ITM0082", "Register 200 Page", "STATION", "NOS", 60m, "GST12"),
            ("ITM0083", "Thermal Roll 79 mm", "STATION", "NOS", 150m, "GST18"),
            ("ITM0084", "Barcode Label Roll", "STATION", "NOS", 80m, "GST18"),
            ("ITM0085", "Patient Case Sheet - Printed", "PRINTED", "BOX", 40m, "GST12"),
            ("ITM0086", "Consent Form - Printed", "PRINTED", "BOX", 40m, "GST12"),
            ("ITM0087", "Prescription Pad", "PRINTED", "NOS", 100m, "GST12"),
            ("ITM0088", "Discharge Summary Form", "PRINTED", "BOX", 30m, "GST12"),
            ("ITM0089", "MRD File Cover", "PRINTED", "NOS", 200m, "GST12"),
            ("ITM0090", "Lab Requisition Form", "PRINTED", "BOX", 40m, "GST12"),

            // CSSD
            ("ITM0091", "Sterilisation Pouch 100x200", "CSSD", "BOX", 50m, "GST12"),
            ("ITM0092", "Autoclave Indicator Tape", "CSSD", "NOS", 60m, "GST12"),
            ("ITM0093", "Biological Indicator Strip", "CSSD", "BOX", 20m, "GST12"),
            ("ITM0094", "Instrument Tray - Stainless", "CSSD", "NOS", 40m, "GST18"),
            ("ITM0095", "Kidney Tray - Stainless", "CSSD", "NOS", 60m, "GST18"),
            ("ITM0096", "Enzymatic Instrument Cleaner 1 litre", "CSSD", "BOTTLE", 20m, "GST18"),

            // Laboratory consumables
            ("ITM0097", "EDTA Vacutainer 2 ml", "LABCONS", "NOS", 500m, "GST12"),
            ("ITM0098", "Plain Vacutainer 5 ml", "LABCONS", "NOS", 500m, "GST12"),
            ("ITM0099", "Fluoride Vacutainer", "LABCONS", "NOS", 300m, "GST12"),
            ("ITM0100", "Citrate Vacutainer", "LABCONS", "NOS", 200m, "GST12"),
            ("ITM0101", "Micropipette Tip 200 µl", "LABCONS", "BOX", 60m, "GST12"),
            ("ITM0102", "Glass Slide - Frosted", "LABCONS", "BOX", 40m, "GST12"),
            ("ITM0103", "Cover Slip", "LABCONS", "BOX", 40m, "GST12"),
            ("ITM0104", "Urine Container - Sterile", "LABCONS", "NOS", 300m, "GST12"),
            ("ITM0105", "Blood Culture Bottle", "LABCONS", "NOS", 80m, "GST12"),
            ("ITM0106", "Swab Stick - Sterile", "LABCONS", "NOS", 200m, "GST12"),

            // Biomedical spares
            ("ITM0107", "Monitor ECG Cable - 5 Lead", "BIOMEDSP", "NOS", 8m, "GST18"),
            ("ITM0108", "NIBP Cuff - Adult", "BIOMEDSP", "NOS", 12m, "GST18"),
            ("ITM0109", "SpO2 Sensor - Reusable", "BIOMEDSP", "NOS", 10m, "GST18"),
            ("ITM0110", "Ventilator Patient Circuit", "BIOMEDSP", "NOS", 20m, "GST18"),
            ("ITM0111", "Ventilator HEPA Filter", "BIOMEDSP", "NOS", 25m, "GST18"),
            ("ITM0112", "Infusion Pump Battery", "BIOMEDSP", "NOS", 8m, "GST18"),
            ("ITM0113", "Defibrillator Pad - Adult", "BIOMEDSP", "PAIR", 15m, "GST18"),
            ("ITM0114", "Suction Machine Jar", "BIOMEDSP", "NOS", 10m, "GST18"),
            ("ITM0115", "Oxygen Flow Meter", "BIOMEDSP", "NOS", 12m, "GST18"),
            ("ITM0116", "Humidifier Bottle", "BIOMEDSP", "NOS", 20m, "GST18"),

            // Electrical / maintenance
            ("ITM0117", "LED Tube Light 20 W", "ELECTRICAL", "NOS", 60m, "GST18"),
            ("ITM0118", "LED Bulb 9 W", "ELECTRICAL", "NOS", 80m, "GST18"),
            ("ITM0119", "Switch Socket 6 A", "ELECTRICAL", "NOS", 50m, "GST18"),
            ("ITM0120", "Extension Board 4 Socket", "ELECTRICAL", "NOS", 20m, "GST18"),
            ("ITM0121", "PVC Insulation Tape", "ELECTRICAL", "NOS", 40m, "GST18"),
            ("ITM0122", "Door Closer", "ELECTRICAL", "NOS", 15m, "GST18"),
            ("ITM0123", "Water Tap - Elbow Operated", "ELECTRICAL", "NOS", 15m, "GST18"),

            // Dietary
            ("ITM0124", "Disposable Food Container", "DIETARY", "NOS", 300m, "GST18"),
            ("ITM0125", "Paper Cup 150 ml", "DIETARY", "NOS", 500m, "GST18"),
            ("ITM0126", "Disposable Spoon", "DIETARY", "NOS", 500m, "GST18"),
            ("ITM0127", "Serving Tray - Melamine", "DIETARY", "NOS", 40m, "GST18"),
            ("ITM0128", "Cling Film Roll", "DIETARY", "NOS", 30m, "GST18"),
        ];

        var existing = await db.Items.Select(i => i.Code).ToListAsync(ct);
        var missing = rows.Where(r => !existing.Contains(r.Code)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        var categories = await db.ItemCategories.ToDictionaryAsync(c => c.Code, c => c.Id, ct);
        var uoms = await db.Uoms.ToDictionaryAsync(u => u.Code, u => u.Id, ct);
        var taxes = await db.Taxes.ToDictionaryAsync(t => t.Code, t => t.Id, ct);
        foreach (var (code, name, category, uom, reorder, tax) in missing)
        {
            if (!categories.TryGetValue(category, out var categoryId) || !uoms.TryGetValue(uom, out var uomId))
            {
                continue;
            }
            db.Items.Add(new Item
            {
                Code = code,
                Name = name,
                ItemCategoryId = categoryId,
                UomId = uomId,
                TaxId = taxes.TryGetValue(tax, out var taxId) ? taxId : null,
                ReorderLevel = reorder,
            });
        }
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Demo: added {Count} general-store items", missing.Count);
    }

    // ── Drug catalog ──────────────────────────────────────────────────────────
    /// <summary>
    /// Deepens the drug catalog behind the pharmacy POS, the OPD prescription picker and IPD
    /// medication orders. Manufacturers and generics come first because drugs reference them;
    /// existing rows (MFG1-10, GEN1-40, DRG0001+) are left alone and these use their own code
    /// prefixes so nothing collides.
    /// </summary>
    private async Task SeedDrugCatalogDepthAsync(CancellationToken ct)
    {
        await EnsureAsync(db.Manufacturers,
        [
            ("MFGX01", "Cipla Ltd"), ("MFGX02", "Sun Pharmaceutical Industries"),
            ("MFGX03", "Dr Reddy's Laboratories"), ("MFGX04", "Lupin Ltd"),
            ("MFGX05", "Torrent Pharmaceuticals"), ("MFGX06", "Alkem Laboratories"),
            ("MFGX07", "Mankind Pharma"), ("MFGX08", "Zydus Lifesciences"),
            ("MFGX09", "Glenmark Pharmaceuticals"), ("MFGX10", "Intas Pharmaceuticals"),
            ("MFGX11", "Abbott India"), ("MFGX12", "GlaxoSmithKline Pharmaceuticals"),
            ("MFGX13", "Pfizer India"), ("MFGX14", "Macleods Pharmaceuticals"),
            ("MFGX15", "Emcure Pharmaceuticals"), ("MFGX16", "Aristo Pharmaceuticals"),
            ("MFGX17", "USV Private Ltd"), ("MFGX18", "Micro Labs"),
        ], (code, name) => new Manufacturer { Code = code, Name = name }, ct);

        await EnsureAsync(db.Generics,
        [
            ("GENX001", "Paracetamol"), ("GENX002", "Ibuprofen"), ("GENX003", "Diclofenac Sodium"),
            ("GENX004", "Aceclofenac"), ("GENX005", "Tramadol"), ("GENX006", "Amoxicillin"),
            ("GENX007", "Amoxicillin + Clavulanic Acid"), ("GENX008", "Azithromycin"),
            ("GENX009", "Cefixime"), ("GENX010", "Ceftriaxone"), ("GENX011", "Cefoperazone + Sulbactam"),
            ("GENX012", "Meropenem"), ("GENX013", "Piperacillin + Tazobactam"), ("GENX014", "Vancomycin"),
            ("GENX015", "Levofloxacin"), ("GENX016", "Ciprofloxacin"), ("GENX017", "Doxycycline"),
            ("GENX018", "Metronidazole"), ("GENX019", "Fluconazole"), ("GENX020", "Acyclovir"),
            ("GENX021", "Pantoprazole"), ("GENX022", "Omeprazole"), ("GENX023", "Ranitidine"),
            ("GENX024", "Ondansetron"), ("GENX025", "Domperidone"), ("GENX026", "Dicyclomine"),
            ("GENX027", "Lactulose"), ("GENX028", "Metformin"), ("GENX029", "Glimepiride"),
            ("GENX030", "Sitagliptin"), ("GENX031", "Insulin Human"), ("GENX032", "Insulin Glargine"),
            ("GENX033", "Amlodipine"), ("GENX034", "Telmisartan"), ("GENX035", "Losartan"),
            ("GENX036", "Ramipril"), ("GENX037", "Metoprolol"), ("GENX038", "Atenolol"),
            ("GENX039", "Carvedilol"), ("GENX040", "Furosemide"), ("GENX041", "Spironolactone"),
            ("GENX042", "Torsemide"), ("GENX043", "Atorvastatin"), ("GENX044", "Rosuvastatin"),
            ("GENX045", "Clopidogrel"), ("GENX046", "Aspirin"), ("GENX047", "Heparin"),
            ("GENX048", "Enoxaparin"), ("GENX049", "Warfarin"), ("GENX050", "Salbutamol"),
            ("GENX051", "Ipratropium"), ("GENX052", "Budesonide"), ("GENX053", "Formoterol + Budesonide"),
            ("GENX054", "Montelukast"), ("GENX055", "Levocetirizine"), ("GENX056", "Cetirizine"),
            ("GENX057", "Chlorpheniramine"), ("GENX058", "Prednisolone"), ("GENX059", "Dexamethasone"),
            ("GENX060", "Hydrocortisone"), ("GENX061", "Methylprednisolone"), ("GENX062", "Levothyroxine"),
            ("GENX063", "Carbimazole"), ("GENX064", "Phenytoin"), ("GENX065", "Sodium Valproate"),
            ("GENX066", "Levetiracetam"), ("GENX067", "Carbamazepine"), ("GENX068", "Clonazepam"),
            ("GENX069", "Lorazepam"), ("GENX070", "Midazolam"), ("GENX071", "Propofol"),
            ("GENX072", "Fentanyl"), ("GENX073", "Morphine"), ("GENX074", "Bupivacaine"),
            ("GENX075", "Lignocaine"), ("GENX076", "Atropine"), ("GENX077", "Adrenaline"),
            ("GENX078", "Noradrenaline"), ("GENX079", "Dopamine"), ("GENX080", "Dobutamine"),
            ("GENX081", "Potassium Chloride"), ("GENX082", "Sodium Bicarbonate"),
            ("GENX083", "Calcium Gluconate"), ("GENX084", "Magnesium Sulphate"),
            ("GENX085", "Tranexamic Acid"), ("GENX086", "Vitamin K"), ("GENX087", "Iron Sucrose"),
            ("GENX088", "Folic Acid"), ("GENX089", "Vitamin B Complex"), ("GENX090", "Vitamin D3"),
            ("GENX091", "Calcium Carbonate"), ("GENX092", "Multivitamin"), ("GENX093", "Oxytocin"),
            ("GENX094", "Misoprostol"), ("GENX095", "Mifepristone"), ("GENX096", "Tamsulosin"),
            ("GENX097", "Finasteride"), ("GENX098", "Sildenafil"), ("GENX099", "Allopurinol"),
            ("GENX100", "Colchicine"), ("GENX101", "Methotrexate"), ("GENX102", "Hydroxychloroquine"),
            ("GENX103", "Mycophenolate Mofetil"), ("GENX104", "Tacrolimus"),
            ("GENX105", "Ondansetron + Paracetamol"), ("GENX106", "Silver Sulphadiazine"),
            ("GENX107", "Mupirocin"), ("GENX108", "Clotrimazole"), ("GENX109", "Betamethasone"),
            ("GENX110", "Normal Saline 0.9%"), ("GENX111", "Ringer Lactate"),
            ("GENX112", "Dextrose 5%"), ("GENX113", "Dextrose Normal Saline"),
            ("GENX114", "Mannitol"), ("GENX115", "Albumin"),
        ], (code, name) => new Generic { Code = code, Name = name }, ct);

        // (Code, Name, GenericCode, Form, Strength, UomCode, Schedule, HighAlert)
        (string Code, string Name, string Generic, string Form, string Strength, string Uom, string? Schedule, bool HighAlert)[] rows =
        [
            ("DRGX0001", "Dolo 650", "GENX001", "Tablet", "650 mg", "TAB", null, false),
            ("DRGX0002", "Crocin Advance", "GENX001", "Tablet", "500 mg", "TAB", null, false),
            ("DRGX0003", "Paracetamol Infusion", "GENX001", "Infusion", "1 g/100 ml", "VIAL", null, false),
            ("DRGX0004", "Brufen 400", "GENX002", "Tablet", "400 mg", "TAB", null, false),
            ("DRGX0005", "Voveran SR", "GENX003", "Tablet", "100 mg", "TAB", "H", false),
            ("DRGX0006", "Diclofenac Injection", "GENX003", "Injection", "75 mg/3 ml", "AMP", "H", false),
            ("DRGX0007", "Zerodol SP", "GENX004", "Tablet", "100 mg", "TAB", "H", false),
            ("DRGX0008", "Tramadol Injection", "GENX005", "Injection", "100 mg/2 ml", "AMP", "H", true),
            ("DRGX0009", "Mox 500", "GENX006", "Capsule", "500 mg", "CAP", "H", false),
            ("DRGX0010", "Augmentin 625 Duo", "GENX007", "Tablet", "625 mg", "TAB", "H", false),
            ("DRGX0011", "Augmentin 1.2 g Injection", "GENX007", "Injection", "1.2 g", "VIAL", "H", false),
            ("DRGX0012", "Azithral 500", "GENX008", "Tablet", "500 mg", "TAB", "H", false),
            ("DRGX0013", "Taxim-O 200", "GENX009", "Tablet", "200 mg", "TAB", "H", false),
            ("DRGX0014", "Monocef 1 g Injection", "GENX010", "Injection", "1 g", "VIAL", "H", false),
            ("DRGX0015", "Magnex Forte 1.5 g", "GENX011", "Injection", "1.5 g", "VIAL", "H", false),
            ("DRGX0016", "Meronem 1 g", "GENX012", "Injection", "1 g", "VIAL", "H", true),
            ("DRGX0017", "Zosyn 4.5 g", "GENX013", "Injection", "4.5 g", "VIAL", "H", true),
            ("DRGX0018", "Vancomycin 500 mg", "GENX014", "Injection", "500 mg", "VIAL", "H", true),
            ("DRGX0019", "Levoflox 500", "GENX015", "Tablet", "500 mg", "TAB", "H", false),
            ("DRGX0020", "Ciplox 500", "GENX016", "Tablet", "500 mg", "TAB", "H", false),
            ("DRGX0021", "Doxy-1 L-DR", "GENX017", "Capsule", "100 mg", "CAP", "H", false),
            ("DRGX0022", "Metrogyl 400", "GENX018", "Tablet", "400 mg", "TAB", "H", false),
            ("DRGX0023", "Metrogyl IV 100 ml", "GENX018", "Infusion", "500 mg/100 ml", "BOTTLE", "H", false),
            ("DRGX0024", "Forcan 150", "GENX019", "Tablet", "150 mg", "TAB", "H", false),
            ("DRGX0025", "Acivir 400 DT", "GENX020", "Tablet", "400 mg", "TAB", "H", false),
            ("DRGX0026", "Pan 40", "GENX021", "Tablet", "40 mg", "TAB", null, false),
            ("DRGX0027", "Pantop 40 IV", "GENX021", "Injection", "40 mg", "VIAL", null, false),
            ("DRGX0028", "Omez 20", "GENX022", "Capsule", "20 mg", "CAP", null, false),
            ("DRGX0029", "Rantac 150", "GENX023", "Tablet", "150 mg", "TAB", null, false),
            ("DRGX0030", "Emeset 4", "GENX024", "Tablet", "4 mg", "TAB", null, false),
            ("DRGX0031", "Emeset 2 ml Injection", "GENX024", "Injection", "4 mg/2 ml", "AMP", null, false),
            ("DRGX0032", "Domstal 10", "GENX025", "Tablet", "10 mg", "TAB", null, false),
            ("DRGX0033", "Cyclopam", "GENX026", "Tablet", "20 mg", "TAB", null, false),
            ("DRGX0034", "Duphalac 200 ml", "GENX027", "Syrup", "10 g/15 ml", "BOTTLE", null, false),
            ("DRGX0035", "Glycomet 500", "GENX028", "Tablet", "500 mg", "TAB", null, false),
            ("DRGX0036", "Glycomet SR 1000", "GENX028", "Tablet", "1000 mg", "TAB", null, false),
            ("DRGX0037", "Amaryl 2", "GENX029", "Tablet", "2 mg", "TAB", "H", false),
            ("DRGX0038", "Januvia 100", "GENX030", "Tablet", "100 mg", "TAB", "H", false),
            ("DRGX0039", "Actrapid HM 40 IU", "GENX031", "Injection", "40 IU/ml", "VIAL", "H", true),
            ("DRGX0040", "Lantus SoloStar", "GENX032", "Injection", "100 IU/ml", "NOS", "H", true),
            ("DRGX0041", "Amlong 5", "GENX033", "Tablet", "5 mg", "TAB", "H", false),
            ("DRGX0042", "Telma 40", "GENX034", "Tablet", "40 mg", "TAB", "H", false),
            ("DRGX0043", "Losar 50", "GENX035", "Tablet", "50 mg", "TAB", "H", false),
            ("DRGX0044", "Cardace 5", "GENX036", "Tablet", "5 mg", "TAB", "H", false),
            ("DRGX0045", "Metolar XR 50", "GENX037", "Tablet", "50 mg", "TAB", "H", false),
            ("DRGX0046", "Aten 50", "GENX038", "Tablet", "50 mg", "TAB", "H", false),
            ("DRGX0047", "Carca 3.125", "GENX039", "Tablet", "3.125 mg", "TAB", "H", false),
            ("DRGX0048", "Lasix 40", "GENX040", "Tablet", "40 mg", "TAB", "H", false),
            ("DRGX0049", "Lasix 20 mg Injection", "GENX040", "Injection", "20 mg/2 ml", "AMP", "H", false),
            ("DRGX0050", "Aldactone 25", "GENX041", "Tablet", "25 mg", "TAB", "H", false),
            ("DRGX0051", "Dytor 10", "GENX042", "Tablet", "10 mg", "TAB", "H", false),
            ("DRGX0052", "Atorva 20", "GENX043", "Tablet", "20 mg", "TAB", "H", false),
            ("DRGX0053", "Rosuvas 10", "GENX044", "Tablet", "10 mg", "TAB", "H", false),
            ("DRGX0054", "Clopilet 75", "GENX045", "Tablet", "75 mg", "TAB", "H", false),
            ("DRGX0055", "Ecosprin 75", "GENX046", "Tablet", "75 mg", "TAB", null, false),
            ("DRGX0056", "Heparin 5000 IU", "GENX047", "Injection", "5000 IU/ml", "VIAL", "H", true),
            ("DRGX0057", "Clexane 40 mg", "GENX048", "Injection", "40 mg/0.4 ml", "NOS", "H", true),
            ("DRGX0058", "Warf 5", "GENX049", "Tablet", "5 mg", "TAB", "H", true),
            ("DRGX0059", "Asthalin Respules", "GENX050", "Respule", "2.5 mg/2.5 ml", "AMP", null, false),
            ("DRGX0060", "Asthalin Inhaler", "GENX050", "Inhaler", "100 mcg", "NOS", null, false),
            ("DRGX0061", "Duolin Respules", "GENX051", "Respule", "2.5 mg + 500 mcg", "AMP", null, false),
            ("DRGX0062", "Budecort Respules", "GENX052", "Respule", "0.5 mg/2 ml", "AMP", null, false),
            ("DRGX0063", "Foracort Inhaler 200", "GENX053", "Inhaler", "6/200 mcg", "NOS", null, false),
            ("DRGX0064", "Montair LC", "GENX054", "Tablet", "10 mg + 5 mg", "TAB", null, false),
            ("DRGX0065", "Levocet 5", "GENX055", "Tablet", "5 mg", "TAB", null, false),
            ("DRGX0066", "Cetzine 10", "GENX056", "Tablet", "10 mg", "TAB", null, false),
            ("DRGX0067", "Avil 25", "GENX057", "Tablet", "25 mg", "TAB", null, false),
            ("DRGX0068", "Wysolone 10", "GENX058", "Tablet", "10 mg", "TAB", "H", false),
            ("DRGX0069", "Dexona Injection", "GENX059", "Injection", "8 mg/2 ml", "AMP", "H", false),
            ("DRGX0070", "Efcorlin 100 mg", "GENX060", "Injection", "100 mg", "VIAL", "H", false),
            ("DRGX0071", "Solu-Medrol 500 mg", "GENX061", "Injection", "500 mg", "VIAL", "H", true),
            ("DRGX0072", "Thyronorm 50 mcg", "GENX062", "Tablet", "50 mcg", "TAB", "H", false),
            ("DRGX0073", "Neomercazole 5", "GENX063", "Tablet", "5 mg", "TAB", "H", false),
            ("DRGX0074", "Eptoin 100", "GENX064", "Tablet", "100 mg", "TAB", "H", true),
            ("DRGX0075", "Valparin Chrono 500", "GENX065", "Tablet", "500 mg", "TAB", "H", true),
            ("DRGX0076", "Levipil 500", "GENX066", "Tablet", "500 mg", "TAB", "H", false),
            ("DRGX0077", "Tegretol CR 200", "GENX067", "Tablet", "200 mg", "TAB", "H", false),
            ("DRGX0078", "Clonotril 0.5", "GENX068", "Tablet", "0.5 mg", "TAB", "H1", false),
            ("DRGX0079", "Ativan 2 mg", "GENX069", "Tablet", "2 mg", "TAB", "H1", false),
            ("DRGX0080", "Midazolam 5 mg", "GENX070", "Injection", "5 mg/ml", "AMP", "H1", true),
            ("DRGX0081", "Propofol 1%", "GENX071", "Injection", "10 mg/ml", "AMP", "H", true),
            ("DRGX0082", "Fentanyl 100 mcg", "GENX072", "Injection", "50 mcg/ml", "AMP", "H1", true),
            ("DRGX0083", "Morphine 10 mg", "GENX073", "Injection", "10 mg/ml", "AMP", "H1", true),
            ("DRGX0084", "Sensorcaine 0.5%", "GENX074", "Injection", "5 mg/ml", "VIAL", "H", false),
            ("DRGX0085", "Xylocaine 2%", "GENX075", "Injection", "20 mg/ml", "VIAL", "H", false),
            ("DRGX0086", "Atropine 0.6 mg", "GENX076", "Injection", "0.6 mg/ml", "AMP", "H", true),
            ("DRGX0087", "Adrenaline 1:1000", "GENX077", "Injection", "1 mg/ml", "AMP", "H", true),
            ("DRGX0088", "Noradrenaline 2 mg", "GENX078", "Injection", "2 mg/2 ml", "AMP", "H", true),
            ("DRGX0089", "Dopamine 200 mg", "GENX079", "Injection", "200 mg/5 ml", "AMP", "H", true),
            ("DRGX0090", "Dobutamine 250 mg", "GENX080", "Injection", "250 mg/5 ml", "VIAL", "H", true),
            ("DRGX0091", "Potassium Chloride 15%", "GENX081", "Injection", "15%/10 ml", "AMP", "H", true),
            ("DRGX0092", "Sodium Bicarbonate 7.5%", "GENX082", "Injection", "7.5%/25 ml", "AMP", "H", false),
            ("DRGX0093", "Calcium Gluconate 10%", "GENX083", "Injection", "10%/10 ml", "AMP", "H", false),
            ("DRGX0094", "Magnesium Sulphate 50%", "GENX084", "Injection", "50%/2 ml", "AMP", "H", true),
            ("DRGX0095", "Tranexa 500 mg", "GENX085", "Injection", "500 mg/5 ml", "AMP", "H", false),
            ("DRGX0096", "Vitamin K 10 mg", "GENX086", "Injection", "10 mg/ml", "AMP", "H", false),
            ("DRGX0097", "Orofer FCM 500", "GENX087", "Injection", "500 mg/10 ml", "VIAL", "H", false),
            ("DRGX0098", "Folvite 5 mg", "GENX088", "Tablet", "5 mg", "TAB", null, false),
            ("DRGX0099", "Becosules", "GENX089", "Capsule", "B-Complex", "CAP", null, false),
            ("DRGX0100", "Uprise D3 60K", "GENX090", "Capsule", "60000 IU", "CAP", null, false),
            ("DRGX0101", "Shelcal 500", "GENX091", "Tablet", "500 mg", "TAB", null, false),
            ("DRGX0102", "Zincovit", "GENX092", "Tablet", "Multivitamin", "TAB", null, false),
            ("DRGX0103", "Pitocin 5 IU", "GENX093", "Injection", "5 IU/ml", "AMP", "H", true),
            ("DRGX0104", "Misoprost 200", "GENX094", "Tablet", "200 mcg", "TAB", "H", true),
            ("DRGX0105", "Urimax 0.4", "GENX096", "Capsule", "0.4 mg", "CAP", "H", false),
            ("DRGX0106", "Finast 5", "GENX097", "Tablet", "5 mg", "TAB", "H", false),
            ("DRGX0107", "Zyloric 100", "GENX099", "Tablet", "100 mg", "TAB", "H", false),
            ("DRGX0108", "Colchicine 0.5", "GENX100", "Tablet", "0.5 mg", "TAB", "H", false),
            ("DRGX0109", "Folitrax 10", "GENX101", "Tablet", "10 mg", "TAB", "H", true),
            ("DRGX0110", "HCQS 200", "GENX102", "Tablet", "200 mg", "TAB", "H", false),
            ("DRGX0111", "Mycept 500", "GENX103", "Tablet", "500 mg", "TAB", "H", true),
            ("DRGX0112", "Pangraf 1 mg", "GENX104", "Capsule", "1 mg", "CAP", "H", true),
            ("DRGX0113", "Silverex Ointment", "GENX106", "Ointment", "1% w/w", "NOS", null, false),
            ("DRGX0114", "T-Bact Ointment", "GENX107", "Ointment", "2% w/w", "NOS", null, false),
            ("DRGX0115", "Candid Cream", "GENX108", "Cream", "1% w/w", "NOS", null, false),
            ("DRGX0116", "Betnovate Cream", "GENX109", "Cream", "0.1% w/w", "NOS", "H", false),
            ("DRGX0117", "Normal Saline 500 ml", "GENX110", "Infusion", "0.9%", "BOTTLE", null, false),
            ("DRGX0118", "Ringer Lactate 500 ml", "GENX111", "Infusion", "RL", "BOTTLE", null, false),
            ("DRGX0119", "Dextrose 5% 500 ml", "GENX112", "Infusion", "5%", "BOTTLE", null, false),
            ("DRGX0120", "DNS 500 ml", "GENX113", "Infusion", "5% + 0.9%", "BOTTLE", null, false),
            ("DRGX0121", "Mannitol 20% 100 ml", "GENX114", "Infusion", "20%", "BOTTLE", "H", false),
            ("DRGX0122", "Albumin 20% 100 ml", "GENX115", "Infusion", "20%", "BOTTLE", "H", true),
        ];

        var existing = await db.Drugs.Select(d => d.Code).ToListAsync(ct);
        var missing = rows.Where(r => !existing.Contains(r.Code)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        var generics = await db.Generics.ToDictionaryAsync(g => g.Code, g => g.Id, ct);
        var manufacturers = await db.Manufacturers.OrderBy(m => m.Code).Select(m => m.Id).ToListAsync(ct);
        var uoms = await db.Uoms.ToDictionaryAsync(u => u.Code, u => u.Id, ct);
        var taxes = await db.Taxes.ToDictionaryAsync(t => t.Code, t => t.Id, ct);
        var index = 0;
        foreach (var (code, name, generic, form, strength, uom, schedule, highAlert) in missing)
        {
            if (!uoms.TryGetValue(uom, out var uomId))
            {
                continue;
            }
            db.Drugs.Add(new Drug
            {
                Code = code,
                Name = name,
                GenericId = generics.TryGetValue(generic, out var genericId) ? genericId : null,
                // Spread brands round-robin across manufacturers — no real brand-to-maker claim
                // is intended here, it just keeps the manufacturer filter meaningful.
                ManufacturerId = manufacturers.Count > 0 ? manufacturers[index++ % manufacturers.Count] : null,
                Form = form,
                Strength = strength,
                UomId = uomId,
                // Medicines are GST 12% in India; the few devices/inhalers here are close enough
                // that using one slab keeps the GST register readable without inventing detail.
                TaxId = taxes.TryGetValue("GST12", out var taxId) ? taxId : null,
                Schedule = schedule,
                ReorderLevel = 50m,
                IsHighAlert = highAlert,
            });
        }
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Demo: added {Count} drugs", missing.Count);
    }

    // ── Laboratory catalog ────────────────────────────────────────────────────
    /// <summary>
    /// Grows the lab catalog from 17 tests / 1 profile to something the order screen, the
    /// department worklists and the cumulative report can actually demo. Every test gets at
    /// least one parameter (the LabTest contract: non-panel tests still carry exactly one, so
    /// result entry never special-cases them) and every numeric parameter gets an adult
    /// reference range so H/L flagging fires on result entry.
    /// </summary>
    private async Task SeedLabCatalogDepthAsync(CancellationToken ct)
    {
        // (TestCode, TestName, DeptCode, SampleTypeCode, TatHours, ServiceCode?,
        //  Parameters: (Code, Name, Unit, Low, High, CriticalLow, CriticalHigh))
        (string Code, string Name, string Sample, int Tat, string? Service,
            (string Code, string Name, string? Unit, decimal? Low, decimal? High, decimal? CritLow, decimal? CritHigh)[] Parameters)[] rows =
        [
            ("PLT", "Platelet Count", "WB", 4, null,
                [("PLT", "Platelet Count", "10^3/µL", 150m, 410m, 20m, 1000m)]),
            ("TLC", "Total Leucocyte Count", "WB", 4, null,
                [("TLC", "Total Leucocyte Count", "10^3/µL", 4m, 11m, 1m, 30m)]),
            ("DLC", "Differential Leucocyte Count", "WB", 4, null,
                [("NEUT", "Neutrophils", "%", 40m, 75m, null, null),
                 ("LYMPH", "Lymphocytes", "%", 20m, 45m, null, null),
                 ("EOS", "Eosinophils", "%", 1m, 6m, null, null),
                 ("MONO", "Monocytes", "%", 2m, 10m, null, null),
                 ("BASO", "Basophils", "%", 0m, 1m, null, null)]),
            ("RETIC", "Reticulocyte Count", "WB", 6, null,
                [("RETIC", "Reticulocyte Count", "%", 0.5m, 2.5m, null, null)]),
            ("PBS", "Peripheral Blood Smear", "WB", 8, null,
                [("PBS", "Smear Findings", null, null, null, null, null)]),
            ("PT-INR", "Prothrombin Time with INR", "PLAS", 4, null,
                [("PT", "Prothrombin Time", "seconds", 11m, 13.5m, null, 30m),
                 ("INR", "INR", "ratio", 0.8m, 1.2m, null, 5m)]),
            ("APTT", "Activated Partial Thromboplastin Time", "PLAS", 4, null,
                [("APTT", "APTT", "seconds", 25m, 35m, null, 90m)]),
            ("DDIMER", "D-Dimer", "PLAS", 6, null,
                [("DDIMER", "D-Dimer", "ng/mL FEU", 0m, 500m, null, 5000m)]),
            ("FIBRINOGEN", "Fibrinogen", "PLAS", 6, null,
                [("FIB", "Fibrinogen", "mg/dL", 200m, 400m, 100m, null)]),
            ("BLDGRP", "Blood Group & Rh Typing", "WB", 2, null,
                [("ABO", "ABO Group", null, null, null, null, null),
                 ("RH", "Rh (D) Typing", null, null, null, null, null)]),
            ("PPBS", "Post Prandial Blood Sugar", "PLAS", 3, null,
                [("PPBS", "Post Prandial Glucose", "mg/dL", 70m, 140m, 50m, 400m)]),
            ("OGTT", "Oral Glucose Tolerance Test", "PLAS", 6, null,
                [("OGTT0", "Fasting", "mg/dL", 70m, 100m, 50m, 400m),
                 ("OGTT1", "1 Hour", "mg/dL", 70m, 180m, null, 400m),
                 ("OGTT2", "2 Hour", "mg/dL", 70m, 140m, null, 400m)]),
            ("INSULIN", "Serum Insulin - Fasting", "SER", 24, null,
                [("INS", "Insulin", "µIU/mL", 2.6m, 24.9m, null, null)]),
            ("UREA", "Blood Urea", "SER", 4, null,
                [("UREA", "Urea", "mg/dL", 15m, 40m, null, 200m)]),
            ("CREAT", "Serum Creatinine", "SER", 4, null,
                [("CREAT", "Creatinine", "mg/dL", 0.6m, 1.3m, null, 8m)]),
            ("URICACID", "Serum Uric Acid", "SER", 6, null,
                [("UA", "Uric Acid", "mg/dL", 3.4m, 7m, null, 15m)]),
            ("EGFR", "Estimated GFR", "SER", 6, null,
                [("EGFR", "eGFR", "mL/min/1.73m²", 90m, null, 15m, null)]),
            ("CALCIUM", "Serum Calcium", "SER", 6, null,
                [("CA", "Total Calcium", "mg/dL", 8.6m, 10.3m, 6.5m, 13m)]),
            ("PHOSPHORUS", "Serum Phosphorus", "SER", 6, null,
                [("PHOS", "Phosphorus", "mg/dL", 2.5m, 4.5m, 1m, 9m)]),
            ("MAGNESIUM", "Serum Magnesium", "SER", 6, null,
                [("MG", "Magnesium", "mg/dL", 1.7m, 2.2m, 1m, 5m)]),
            ("TOTPROT", "Total Protein with A/G Ratio", "SER", 6, null,
                [("TP", "Total Protein", "g/dL", 6.4m, 8.3m, null, null),
                 ("ALB", "Albumin", "g/dL", 3.5m, 5.2m, 1.5m, null),
                 ("GLOB", "Globulin", "g/dL", 2m, 3.5m, null, null)]),
            ("BILIRUBIN", "Serum Bilirubin - Total & Direct", "SER", 4, null,
                [("TBIL", "Total Bilirubin", "mg/dL", 0.2m, 1.2m, null, 20m),
                 ("DBIL", "Direct Bilirubin", "mg/dL", 0m, 0.3m, null, null)]),
            ("AMYLASE", "Serum Amylase", "SER", 6, null,
                [("AMY", "Amylase", "U/L", 25m, 125m, null, 500m)]),
            ("LIPASE", "Serum Lipase", "SER", 6, null,
                [("LIP", "Lipase", "U/L", 13m, 60m, null, 600m)]),
            ("LDH", "Lactate Dehydrogenase", "SER", 6, null,
                [("LDH", "LDH", "U/L", 140m, 280m, null, null)]),
            ("CPK", "Creatine Phosphokinase - Total", "SER", 6, null,
                [("CPK", "CPK Total", "U/L", 30m, 200m, null, 1000m)]),
            ("CKMB", "CK-MB", "SER", 4, null,
                [("CKMB", "CK-MB", "ng/mL", 0m, 6.7m, null, 25m)]),
            ("TROPI", "Troponin I - Quantitative", "SER", 2, null,
                [("TROPI", "Troponin I", "ng/mL", 0m, 0.04m, null, 0.5m)]),
            ("NTPROBNP", "NT-proBNP", "SER", 4, null,
                [("BNP", "NT-proBNP", "pg/mL", 0m, 125m, null, 5000m)]),
            ("T3T4TSH", "Free T3, Free T4 & TSH", "SER", 24, null,
                [("FT3", "Free T3", "pg/mL", 2.3m, 4.2m, null, null),
                 ("FT4", "Free T4", "ng/dL", 0.89m, 1.76m, null, null),
                 ("TSH2", "TSH", "µIU/mL", 0.55m, 4.78m, null, null)]),
            ("VITD", "Vitamin D - 25 Hydroxy", "SER", 48, null,
                [("VITD", "25-OH Vitamin D", "ng/mL", 30m, 100m, 10m, null)]),
            ("VITB12", "Vitamin B12", "SER", 48, null,
                [("B12", "Vitamin B12", "pg/mL", 197m, 771m, 100m, null)]),
            ("FERRITIN", "Serum Ferritin", "SER", 24, null,
                [("FERR", "Ferritin", "ng/mL", 20m, 250m, null, null)]),
            ("IRONSTUDY", "Iron Studies", "SER", 24, null,
                [("FE", "Serum Iron", "µg/dL", 60m, 170m, null, null),
                 ("TIBC", "TIBC", "µg/dL", 240m, 450m, null, null),
                 ("TSAT", "Transferrin Saturation", "%", 20m, 50m, null, null)]),
            ("FOLATE", "Serum Folate", "SER", 48, null,
                [("FOL", "Folate", "ng/mL", 3m, 17m, null, null)]),
            ("PSA", "Prostate Specific Antigen - Total", "SER", 24, null,
                [("PSA", "Total PSA", "ng/mL", 0m, 4m, null, null)]),
            ("CA125", "CA-125", "SER", 48, null,
                [("CA125", "CA-125", "U/mL", 0m, 35m, null, null)]),
            ("CEA", "Carcinoembryonic Antigen", "SER", 48, null,
                [("CEA", "CEA", "ng/mL", 0m, 3m, null, null)]),
            ("AFP", "Alpha Fetoprotein", "SER", 48, null,
                [("AFP", "AFP", "ng/mL", 0m, 10m, null, null)]),
            ("BETAHCG", "Beta hCG - Quantitative", "SER", 12, null,
                [("BHCG", "Beta hCG", "mIU/mL", 0m, 5m, null, null)]),
            ("PROLACTIN", "Serum Prolactin", "SER", 24, null,
                [("PRL", "Prolactin", "ng/mL", 4.8m, 23.3m, null, null)]),
            ("LHFSH", "LH & FSH", "SER", 24, null,
                [("LH", "LH", "mIU/mL", 1.7m, 8.6m, null, null),
                 ("FSH", "FSH", "mIU/mL", 1.5m, 12.4m, null, null)]),
            ("TESTOSTERONE", "Serum Testosterone - Total", "SER", 24, null,
                [("TESTO", "Total Testosterone", "ng/dL", 280m, 1100m, null, null)]),
            ("CORTISOL", "Serum Cortisol - Morning", "SER", 24, null,
                [("CORT", "Cortisol 8 AM", "µg/dL", 6.2m, 19.4m, null, null)]),
            ("PTH", "Parathyroid Hormone", "SER", 48, null,
                [("PTH", "Intact PTH", "pg/mL", 15m, 65m, null, null)]),
            ("HBSAG", "HBsAg - Rapid Screening", "SER", 4, null,
                [("HBSAG", "HBsAg", null, null, null, null, null)]),
            ("ANTIHCV", "Anti-HCV Antibody", "SER", 6, null,
                [("HCV", "Anti-HCV", null, null, null, null, null)]),
            ("HIVSCREEN", "HIV I & II Antibody Screening", "SER", 6, null,
                [("HIV", "HIV I & II", null, null, null, null, null)]),
            ("VDRL", "VDRL / RPR", "SER", 6, null,
                [("VDRL", "VDRL", null, null, null, null, null)]),
            ("WIDAL", "Widal Test", "SER", 12, null,
                [("TO", "S. typhi O", "titre", null, null, null, null),
                 ("TH", "S. typhi H", "titre", null, null, null, null)]),
            ("DENGUE", "Dengue NS1, IgM & IgG", "SER", 6, null,
                [("NS1", "Dengue NS1 Antigen", null, null, null, null, null),
                 ("DIGM", "Dengue IgM", null, null, null, null, null),
                 ("DIGG", "Dengue IgG", null, null, null, null, null)]),
            ("MALARIA", "Malaria Antigen & Smear", "WB", 4, null,
                [("MPAG", "Malaria Antigen", null, null, null, null, null),
                 ("MPSMEAR", "Malaria Parasite Smear", null, null, null, null, null)]),
            ("SCRUB", "Scrub Typhus IgM", "SER", 12, null,
                [("SCRUB", "Scrub Typhus IgM", null, null, null, null, null)]),
            ("LEPTO", "Leptospira IgM", "SER", 12, null,
                [("LEPTO", "Leptospira IgM", null, null, null, null, null)]),
            ("PROCALCITONIN", "Procalcitonin", "SER", 4, null,
                [("PCT", "Procalcitonin", "ng/mL", 0m, 0.5m, null, 10m)]),
            ("RAFACTOR", "Rheumatoid Factor", "SER", 24, null,
                [("RF", "RA Factor", "IU/mL", 0m, 14m, null, null)]),
            ("ANA", "Antinuclear Antibody", "SER", 48, null,
                [("ANA", "ANA (IF)", null, null, null, null, null)]),
            ("ASO", "Anti-Streptolysin O Titre", "SER", 24, null,
                [("ASO", "ASO Titre", "IU/mL", 0m, 200m, null, null)]),
            ("ABG", "Arterial Blood Gas Analysis", "WB", 1, null,
                [("PH", "pH", null, 7.35m, 7.45m, 7.2m, 7.6m),
                 ("PCO2", "pCO2", "mmHg", 35m, 45m, 20m, 70m),
                 ("PO2", "pO2", "mmHg", 80m, 100m, 50m, null),
                 ("HCO3", "HCO3", "mmol/L", 22m, 26m, 10m, 40m),
                 ("BE", "Base Excess", "mmol/L", -2m, 2m, null, null),
                 ("LACTATE", "Lactate", "mmol/L", 0.5m, 2.2m, null, 5m)]),
            ("URINEPROT", "Urine Protein - 24 Hour", "URINE", 26, null,
                [("UPROT", "24-hour Urine Protein", "mg/24h", 0m, 150m, null, null)]),
            ("URINEMICRO", "Urine Microalbumin", "URINE", 12, null,
                [("UMALB", "Microalbumin", "mg/L", 0m, 30m, null, null)]),
            ("URINEKETONE", "Urine Ketone Bodies", "URINE", 2, null,
                [("UKET", "Ketones", null, null, null, null, null)]),
            ("STOOLROUTINE", "Stool Routine Examination", "STOOL", 8, null,
                [("SOCCULT", "Occult Blood", null, null, null, null, null),
                 ("SOVA", "Ova / Cyst", null, null, null, null, null),
                 ("SPUS", "Pus Cells", "/hpf", null, null, null, null)]),
            ("STOOLCULT", "Stool Culture & Sensitivity", "STOOL", 72, null,
                [("SCULT", "Culture Growth", null, null, null, null, null)]),
            ("CSFANALYSIS", "CSF Analysis", "CSF", 6, null,
                [("CSFCELL", "Cell Count", "/µL", 0m, 5m, null, null),
                 ("CSFPROT", "CSF Protein", "mg/dL", 15m, 45m, null, null),
                 ("CSFGLU", "CSF Glucose", "mg/dL", 40m, 70m, null, null)]),
            ("FLUIDANALYSIS", "Body Fluid Analysis", "FLUID", 8, null,
                [("FLCELL", "Cell Count", "/µL", null, null, null, null),
                 ("FLPROT", "Protein", "g/dL", null, null, null, null),
                 ("FLADA", "ADA", "U/L", null, null, null, null)]),
            ("SWABCULT", "Wound Swab Culture & Sensitivity", "SWAB", 72, null,
                [("WCULT", "Culture Growth", null, null, null, null, null)]),
            ("FNAC", "Fine Needle Aspiration Cytology", "TISSUE", 72, null,
                [("FNAC", "Cytology Findings", null, null, null, null, null)]),
            ("PAPSMEAR", "Pap Smear Cytology", "TISSUE", 72, null,
                [("PAP", "Cytology Interpretation", null, null, null, null, null)]),
        ];

        var existingTests = await db.LabTests.Select(t => t.Code).ToListAsync(ct);
        var missing = rows.Where(r => !existingTests.Contains(r.Code)).ToList();
        if (missing.Count > 0)
        {
            var sampleTypes = await db.SampleTypes.ToDictionaryAsync(s => s.Code, s => s.Id, ct);
            var labDepartmentId = await db.Departments.Where(d => d.Code == "LAB").Select(d => (long?)d.Id).FirstOrDefaultAsync(ct);
            var services = await db.Services.ToDictionaryAsync(s => s.Code, s => s.Id, ct);

            foreach (var (code, name, sample, tat, serviceCode, parameters) in missing)
            {
                if (!sampleTypes.TryGetValue(sample, out var sampleTypeId))
                {
                    continue;
                }
                var test = new LabTest
                {
                    Code = code,
                    Name = name,
                    DepartmentId = labDepartmentId,
                    SampleTypeId = sampleTypeId,
                    ServiceId = serviceCode is not null && services.TryGetValue(serviceCode, out var sid) ? sid : null,
                    TatHours = tat,
                    IsPanel = parameters.Length > 1,
                };
                var sortOrder = 0;
                foreach (var (paramCode, paramName, unit, low, high, critLow, critHigh) in parameters)
                {
                    var parameter = new LabTestParameter
                    {
                        // Parameter codes are unique per test, so prefix with the test code to keep
                        // shared abbreviations (ALB, PROT…) distinct across panels.
                        Code = $"{code}-{paramCode}",
                        Name = paramName,
                        Unit = unit,
                        ValueType = low is null && high is null && unit is null ? ParameterValueType.Text : ParameterValueType.Numeric,
                        SortOrder = sortOrder++,
                    };
                    // One adult, sex-agnostic range per numeric parameter — enough for H/L and
                    // critical flagging to fire; narrower age/sex brackets are a clinical decision
                    // a real lab makes, not something to invent here.
                    if (low is not null || high is not null || critLow is not null || critHigh is not null)
                    {
                        parameter.ReferenceRanges.Add(new ReferenceRange
                        {
                            MinAgeYears = 18,
                            NormalLow = low,
                            NormalHigh = high,
                            CriticalLow = critLow,
                            CriticalHigh = critHigh,
                        });
                    }
                    test.Parameters.Add(parameter);
                }
                db.LabTests.Add(test);
            }
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Demo: added {Count} lab tests", missing.Count);
        }

        // Profiles — the orderable bundles a doctor actually ticks, expanded per member test.
        (string Code, string Name, decimal? Price, string[] TestCodes)[] profiles =
        [
            ("PRF-LFT", "Liver Function Test - Full", 1100m, ["LFT", "BILIRUBIN", "TOTPROT"]),
            ("PRF-KFT", "Renal Function Test - Full", 1100m, ["KFT", "UREA", "CREAT", "ELECT", "URICACID", "EGFR"]),
            ("PRF-LIPID", "Lipid Profile - Extended", 900m, ["LIPID", "LDH"]),
            ("PRF-THYROID", "Thyroid Profile - Free", 900m, ["T3T4TSH"]),
            ("PRF-DIABETIC", "Diabetic Profile", 1600m, ["FBS", "PPBS", "HBA1C", "RUE", "CREAT", "LIPID"]),
            ("PRF-FEVER", "Fever Panel", 2400m, ["CBC", "DENGUE", "MALARIA", "WIDAL", "SCRUB", "CRP", "RUE"]),
            ("PRF-ANAEMIA", "Anaemia Profile", 2200m, ["CBC", "RETIC", "PBS", "IRONSTUDY", "FERRITIN", "VITB12", "FOLATE"]),
            ("PRF-PREOP", "Pre-operative Screening", 2600m, ["CBC", "PT-INR", "APTT", "BLDGRP", "RBS", "CREAT", "HBSAG", "HIVSCREEN", "ANTIHCV"]),
            ("PRF-ANC", "Antenatal Profile", 2800m, ["CBC", "BLDGRP", "RUE", "FBS", "TFT", "HBSAG", "HIVSCREEN", "VDRL"]),
            ("PRF-CARDIAC", "Cardiac Marker Panel", 3200m, ["TROPI", "CKMB", "CPK", "NTPROBNP", "ELECT"]),
            ("PRF-SEPSIS", "Sepsis Screen", 3600m, ["CBC", "CRP", "PROCALCITONIN", "BCULT", "ABG", "LFT", "KFT"]),
            ("PRF-COAG", "Coagulation Profile", 1800m, ["PT-INR", "APTT", "PLT", "FIBRINOGEN", "DDIMER"]),
            ("PRF-ARTHRITIS", "Arthritis Profile", 2600m, ["ESR", "CRP", "RAFACTOR", "ANA", "URICACID"]),
            ("PRF-HORMONE-F", "Female Hormone Profile", 3400m, ["LHFSH", "PROLACTIN", "T3T4TSH", "BETAHCG"]),
            ("PRF-BONE", "Bone Health Profile", 2200m, ["CALCIUM", "PHOSPHORUS", "VITD", "PTH"]),
            ("PRF-MASTER", "Master Health Check Panel", 5200m,
                ["CBC", "ESR", "RUE", "FBS", "HBA1C", "LIPID", "LFT", "KFT", "TFT", "VITD", "VITB12"]),
        ];

        var existingProfiles = await db.TestProfiles.Select(p => p.Code).ToListAsync(ct);
        var missingProfiles = profiles.Where(p => !existingProfiles.Contains(p.Code)).ToList();
        if (missingProfiles.Count == 0)
        {
            return;
        }

        var testIds = await db.LabTests.ToDictionaryAsync(t => t.Code, t => t.Id, ct);
        foreach (var (code, name, price, testCodes) in missingProfiles)
        {
            var profile = new TestProfile { Code = code, Name = name, Price = price };
            foreach (var testCode in testCodes.Where(testIds.ContainsKey))
            {
                profile.Tests.Add(new ProfileTest { LabTestId = testIds[testCode] });
            }
            // A profile with no resolvable member tests would expand to an empty order.
            if (profile.Tests.Count > 0)
            {
                db.TestProfiles.Add(profile);
            }
        }
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Demo: added {Count} lab test profiles", missingProfiles.Count);
    }

    // ── Radiology catalog ─────────────────────────────────────────────────────
    /// <summary>
    /// Adds radiology tests across the seeded modalities and — the bigger gap — a reporting
    /// template per test. Report entry prefills Findings/Impression from a template, and with
    /// exactly 1 template in the table that prefill was dead on every other test.
    /// </summary>
    private async Task SeedRadiologyCatalogDepthAsync(CancellationToken ct)
    {
        // (Code, Name, ModalityCode, BodyPart, RequiresContrast, TatHours)
        (string Code, string Name, string Modality, string BodyPart, bool Contrast, int Tat)[] rows =
        [
            ("RADX001", "X-Ray Chest PA - Erect", "XRAY", "Chest", false, 4),
            ("RADX002", "X-Ray Chest Lateral", "XRAY", "Chest", false, 4),
            ("RADX003", "X-Ray Abdomen Erect", "XRAY", "Abdomen", false, 4),
            ("RADX004", "X-Ray Pelvis AP", "XRAY", "Pelvis", false, 4),
            ("RADX005", "X-Ray Cervical Spine AP & Lateral", "XRAY", "Cervical Spine", false, 4),
            ("RADX006", "X-Ray Lumbosacral Spine AP & Lateral", "XRAY", "Lumbosacral Spine", false, 4),
            ("RADX007", "X-Ray Shoulder Joint", "XRAY", "Shoulder", false, 4),
            ("RADX008", "X-Ray Wrist AP & Lateral", "XRAY", "Wrist", false, 4),
            ("RADX009", "X-Ray Ankle AP & Lateral", "XRAY", "Ankle", false, 4),
            ("RADX010", "X-Ray Skull AP & Lateral", "XRAY", "Skull", false, 4),
            ("RADX011", "X-Ray Paranasal Sinuses", "XRAY", "Paranasal Sinuses", false, 4),
            ("RADX012", "X-Ray Femur AP & Lateral", "XRAY", "Femur", false, 4),
            ("RADX013", "X-Ray Both Knees Standing", "XRAY", "Knee", false, 4),
            ("RADX014", "CT Brain Plain", "CT", "Brain", false, 6),
            ("RADX015", "CT Brain with Contrast", "CT", "Brain", true, 6),
            ("RADX016", "CT Angiography - Cerebral", "CT", "Cerebral Vessels", true, 8),
            ("RADX017", "CT Chest - High Resolution", "CT", "Chest", false, 8),
            ("RADX018", "CT Pulmonary Angiography", "CT", "Pulmonary Arteries", true, 6),
            ("RADX019", "CT Abdomen & Pelvis - Triple Phase", "CT", "Abdomen & Pelvis", true, 8),
            ("RADX020", "CT KUB - Plain", "CT", "Kidney Ureter Bladder", false, 6),
            ("RADX021", "CT Cervical Spine", "CT", "Cervical Spine", false, 8),
            ("RADX022", "CT Temporal Bone", "CT", "Temporal Bone", false, 12),
            ("RADX023", "CT Coronary Angiography", "CT", "Coronary Arteries", true, 12),
            ("RADX024", "MRI Brain Plain", "MRI", "Brain", false, 12),
            ("RADX025", "MRI Brain with Contrast", "MRI", "Brain", true, 12),
            ("RADX026", "MRI Cervical Spine", "MRI", "Cervical Spine", false, 12),
            ("RADX027", "MRI Lumbosacral Spine", "MRI", "Lumbosacral Spine", false, 12),
            ("RADX028", "MRI Whole Spine Screening", "MRI", "Whole Spine", false, 24),
            ("RADX029", "MRI Shoulder Joint", "MRI", "Shoulder", false, 24),
            ("RADX030", "MRI Knee Joint", "MRI", "Knee", false, 24),
            ("RADX031", "MRI Abdomen - MRCP", "MRI", "Abdomen", false, 24),
            ("RADX032", "MRI Pelvis", "MRI", "Pelvis", false, 24),
            ("RADX033", "MR Angiography - Brain", "MRI", "Cerebral Vessels", true, 24),
            ("RADX034", "USG Whole Abdomen", "USG", "Abdomen", false, 4),
            ("RADX035", "USG KUB", "USG", "Kidney Ureter Bladder", false, 4),
            ("RADX036", "USG Neck / Thyroid", "USG", "Neck", false, 4),
            ("RADX037", "USG Breast - Bilateral", "USG", "Breast", false, 6),
            ("RADX038", "USG Scrotum", "USG", "Scrotum", false, 4),
            ("RADX039", "USG Obstetric - First Trimester", "USG", "Gravid Uterus", false, 4),
            ("RADX040", "USG Obstetric - Anomaly Scan", "USG", "Gravid Uterus", false, 6),
            ("RADX041", "USG Obstetric - Growth & Doppler", "USG", "Gravid Uterus", false, 6),
            ("RADX042", "USG Guided Pleural Tapping", "USG", "Chest", false, 4),
            ("RADX043", "USG Guided FNAC", "USG", "Variable", false, 6),
            ("RADX044", "Doppler - Carotid Arteries", "USG", "Carotid Arteries", false, 6),
            ("RADX045", "Doppler - Lower Limb Venous", "USG", "Lower Limb Veins", false, 6),
            ("RADX046", "Doppler - Renal Arteries", "USG", "Renal Arteries", false, 6),
            ("RADX047", "2D Echocardiography - Adult", "ECHO", "Heart", false, 4),
            ("RADX048", "2D Echo - Paediatric", "ECHO", "Heart", false, 6),
            ("RADX049", "Transoesophageal Echocardiography", "ECHO", "Heart", false, 12),
            ("RADX050", "Digital Mammography - Bilateral", "MAMMO", "Breast", false, 12),
            ("RADX051", "Mammography - Diagnostic Unilateral", "MAMMO", "Breast", false, 12),
            ("RADX052", "ECG - 12 Lead", "ECG", "Heart", false, 1),
            ("RADX053", "EEG - Routine 30 Minute", "EEG", "Brain", false, 12),
            ("RADX054", "EEG - Sleep Deprived", "EEG", "Brain", false, 24),
        ];

        var existing = await db.RadiologyTests.Select(t => t.Code).ToListAsync(ct);
        var missing = rows.Where(r => !existing.Contains(r.Code)).ToList();
        if (missing.Count > 0)
        {
            var modalities = await db.Modalities.ToDictionaryAsync(m => m.Code, m => m.Id, ct);
            foreach (var (code, name, modality, bodyPart, contrast, tat) in missing)
            {
                if (!modalities.TryGetValue(modality, out var modalityId))
                {
                    continue;
                }
                db.RadiologyTests.Add(new RadiologyTest
                {
                    Code = code,
                    Name = name,
                    ModalityId = modalityId,
                    BodyPart = bodyPart,
                    RequiresContrast = contrast,
                    PrepInstructions = contrast
                        ? "4 hours fasting. Recent serum creatinine required. Confirm no prior contrast reaction and obtain contrast consent."
                        : null,
                    TatHours = tat,
                });
            }
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Demo: added {Count} radiology tests", missing.Count);
        }

        // One starter reporting template per test that has none. Deliberately generic,
        // modality-shaped prose — a real department replaces these with its own house style,
        // and inventing test-specific "normal" wording would read as clinical authority the
        // seeder has no business asserting.
        var templatedTestIds = await db.RadReportTemplates.Select(t => t.RadiologyTestId).ToListAsync(ct);
        var tests = await db.RadiologyTests
            .Where(t => !templatedTestIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Name, t.BodyPart })
            .ToListAsync(ct);
        if (tests.Count == 0)
        {
            return;
        }
        foreach (var test in tests)
        {
            db.RadReportTemplates.Add(new RadReportTemplate
            {
                RadiologyTestId = test.Id,
                Name = $"{test.Name} - Standard Report",
                FindingsTemplate =
                    $"TECHNIQUE:\nStandard protocol images of the {test.BodyPart.ToLowerInvariant()} were acquired.\n\n" +
                    "FINDINGS:\n- \n- \n- \n\nNo other significant abnormality is seen within the imaged field.",
                ImpressionTemplate = "IMPRESSION:\n1. \n\nAdvice: clinical correlation and comparison with previous imaging where available.",
            });
        }
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Demo: added {Count} radiology report templates", tests.Count);
    }

    // ── ICD-10 ────────────────────────────────────────────────────────────────
    /// <summary>
    /// Widens the diagnosis code list behind OPD/IPD diagnosis capture, the EMR problem list and
    /// MRD's coding worklist. These are the commonly-coded ICD-10 categories, not a licensed
    /// copy of the full classification — enough for the search box to behave like a real one.
    /// </summary>
    private async Task SeedIcdDepthAsync(CancellationToken ct)
    {
        (string Code, string Description, string Category)[] rows =
        [
            ("A01.0", "Typhoid fever", "I Infectious and parasitic diseases"),
            ("A15.0", "Tuberculosis of lung, confirmed", "I Infectious and parasitic diseases"),
            ("A41.9", "Sepsis, unspecified organism", "I Infectious and parasitic diseases"),
            ("A90", "Dengue fever (classical dengue)", "I Infectious and parasitic diseases"),
            ("A91", "Dengue haemorrhagic fever", "I Infectious and parasitic diseases"),
            ("B15.9", "Hepatitis A without hepatic coma", "I Infectious and parasitic diseases"),
            ("B18.1", "Chronic viral hepatitis B without delta agent", "I Infectious and parasitic diseases"),
            ("B18.2", "Chronic viral hepatitis C", "I Infectious and parasitic diseases"),
            ("B50.9", "Plasmodium falciparum malaria, unspecified", "I Infectious and parasitic diseases"),
            ("B54", "Unspecified malaria", "I Infectious and parasitic diseases"),
            ("C18.9", "Malignant neoplasm of colon, unspecified", "II Neoplasms"),
            ("C20", "Malignant neoplasm of rectum", "II Neoplasms"),
            ("C34.9", "Malignant neoplasm of bronchus or lung, unspecified", "II Neoplasms"),
            ("C53.9", "Malignant neoplasm of cervix uteri, unspecified", "II Neoplasms"),
            ("C73", "Malignant neoplasm of thyroid gland", "II Neoplasms"),
            ("D50.9", "Iron deficiency anaemia, unspecified", "III Blood and immune disorders"),
            ("D51.0", "Vitamin B12 deficiency anaemia due to intrinsic factor deficiency", "III Blood and immune disorders"),
            ("D56.1", "Beta thalassaemia", "III Blood and immune disorders"),
            ("D69.6", "Thrombocytopenia, unspecified", "III Blood and immune disorders"),
            ("E05.9", "Thyrotoxicosis, unspecified", "IV Endocrine and metabolic"),
            ("E10.9", "Type 1 diabetes mellitus without complications", "IV Endocrine and metabolic"),
            ("E11.2", "Type 2 diabetes mellitus with kidney complications", "IV Endocrine and metabolic"),
            ("E11.4", "Type 2 diabetes mellitus with neurological complications", "IV Endocrine and metabolic"),
            ("E11.9", "Type 2 diabetes mellitus without complications", "IV Endocrine and metabolic"),
            ("E16.2", "Hypoglycaemia, unspecified", "IV Endocrine and metabolic"),
            ("E55.9", "Vitamin D deficiency, unspecified", "IV Endocrine and metabolic"),
            ("E66.9", "Obesity, unspecified", "IV Endocrine and metabolic"),
            ("E86", "Volume depletion / dehydration", "IV Endocrine and metabolic"),
            ("E87.1", "Hypo-osmolality and hyponatraemia", "IV Endocrine and metabolic"),
            ("E87.6", "Hypokalaemia", "IV Endocrine and metabolic"),
            ("F03", "Unspecified dementia", "V Mental and behavioural"),
            ("F10.2", "Alcohol dependence syndrome", "V Mental and behavioural"),
            ("F20.9", "Schizophrenia, unspecified", "V Mental and behavioural"),
            ("F31.9", "Bipolar affective disorder, unspecified", "V Mental and behavioural"),
            ("F33.9", "Recurrent depressive disorder, unspecified", "V Mental and behavioural"),
            ("F51.0", "Nonorganic insomnia", "V Mental and behavioural"),
            ("G20", "Parkinson's disease", "VI Nervous system"),
            ("G35", "Multiple sclerosis", "VI Nervous system"),
            ("G45.9", "Transient cerebral ischaemic attack, unspecified", "VI Nervous system"),
            ("G47.3", "Sleep apnoea", "VI Nervous system"),
            ("G56.0", "Carpal tunnel syndrome", "VI Nervous system"),
            ("G62.9", "Polyneuropathy, unspecified", "VI Nervous system"),
            ("H35.0", "Background retinopathy and retinal vascular changes", "VII Eye and adnexa"),
            ("H40.9", "Glaucoma, unspecified", "VII Eye and adnexa"),
            ("H52.4", "Presbyopia", "VII Eye and adnexa"),
            ("H61.2", "Impacted cerumen", "VIII Ear and mastoid"),
            ("H81.1", "Benign paroxysmal vertigo", "VIII Ear and mastoid"),
            ("H90.3", "Sensorineural hearing loss, bilateral", "VIII Ear and mastoid"),
            ("I10", "Essential (primary) hypertension", "IX Circulatory system"),
            ("I11.9", "Hypertensive heart disease without heart failure", "IX Circulatory system"),
            ("I20.9", "Angina pectoris, unspecified", "IX Circulatory system"),
            ("I21.4", "Acute subendocardial myocardial infarction (NSTEMI)", "IX Circulatory system"),
            ("I21.9", "Acute myocardial infarction, unspecified", "IX Circulatory system"),
            ("I25.1", "Atherosclerotic heart disease", "IX Circulatory system"),
            ("I48.9", "Atrial fibrillation and flutter, unspecified", "IX Circulatory system"),
            ("I50.0", "Congestive heart failure", "IX Circulatory system"),
            ("I50.9", "Heart failure, unspecified", "IX Circulatory system"),
            ("I63.9", "Cerebral infarction, unspecified", "IX Circulatory system"),
            ("I64", "Stroke, not specified as haemorrhage or infarction", "IX Circulatory system"),
            ("I80.2", "Phlebitis and thrombophlebitis of deep vessels of lower extremities", "IX Circulatory system"),
            ("I83.9", "Varicose veins of lower extremities without ulcer", "IX Circulatory system"),
            ("J06.9", "Acute upper respiratory infection, unspecified", "X Respiratory system"),
            ("J18.9", "Pneumonia, unspecified organism", "X Respiratory system"),
            ("J20.9", "Acute bronchitis, unspecified", "X Respiratory system"),
            ("J32.9", "Chronic sinusitis, unspecified", "X Respiratory system"),
            ("J35.0", "Chronic tonsillitis", "X Respiratory system"),
            ("J44.9", "Chronic obstructive pulmonary disease, unspecified", "X Respiratory system"),
            ("J45.9", "Asthma, unspecified", "X Respiratory system"),
            ("J90", "Pleural effusion", "X Respiratory system"),
            ("J96.0", "Acute respiratory failure", "X Respiratory system"),
            ("K21.0", "Gastro-oesophageal reflux disease with oesophagitis", "XI Digestive system"),
            ("K25.9", "Gastric ulcer, unspecified", "XI Digestive system"),
            ("K29.7", "Gastritis, unspecified", "XI Digestive system"),
            ("K35.8", "Acute appendicitis, other and unspecified", "XI Digestive system"),
            ("K40.9", "Unilateral inguinal hernia without obstruction or gangrene", "XI Digestive system"),
            ("K57.3", "Diverticular disease of large intestine without perforation", "XI Digestive system"),
            ("K64.9", "Haemorrhoids, unspecified", "XI Digestive system"),
            ("K70.3", "Alcoholic cirrhosis of liver", "XI Digestive system"),
            ("K74.6", "Other and unspecified cirrhosis of liver", "XI Digestive system"),
            ("K80.2", "Calculus of gallbladder without cholecystitis", "XI Digestive system"),
            ("K81.0", "Acute cholecystitis", "XI Digestive system"),
            ("K85.9", "Acute pancreatitis, unspecified", "XI Digestive system"),
            ("L02.9", "Cutaneous abscess, furuncle and carbuncle, unspecified", "XII Skin"),
            ("L03.9", "Cellulitis, unspecified", "XII Skin"),
            ("L20.9", "Atopic dermatitis, unspecified", "XII Skin"),
            ("L23.9", "Allergic contact dermatitis, unspecified cause", "XII Skin"),
            ("L40.0", "Psoriasis vulgaris", "XII Skin"),
            ("L50.9", "Urticaria, unspecified", "XII Skin"),
            ("L89.9", "Pressure ulcer, unspecified stage", "XII Skin"),
            ("M05.9", "Rheumatoid arthritis with rheumatoid factor, unspecified", "XIII Musculoskeletal"),
            ("M10.9", "Gout, unspecified", "XIII Musculoskeletal"),
            ("M17.9", "Osteoarthritis of knee, unspecified", "XIII Musculoskeletal"),
            ("M51.1", "Lumbar disc disorder with radiculopathy", "XIII Musculoskeletal"),
            ("M54.5", "Low back pain", "XIII Musculoskeletal"),
            ("M75.1", "Rotator cuff syndrome", "XIII Musculoskeletal"),
            ("M79.7", "Fibromyalgia", "XIII Musculoskeletal"),
            ("M81.9", "Osteoporosis, unspecified", "XIII Musculoskeletal"),
            ("N17.9", "Acute kidney failure, unspecified", "XIV Genitourinary system"),
            ("N18.3", "Chronic kidney disease, stage 3", "XIV Genitourinary system"),
            ("N18.5", "Chronic kidney disease, stage 5", "XIV Genitourinary system"),
            ("N20.0", "Calculus of kidney", "XIV Genitourinary system"),
            ("N20.1", "Calculus of ureter", "XIV Genitourinary system"),
            ("N39.0", "Urinary tract infection, site not specified", "XIV Genitourinary system"),
            ("N40", "Benign prostatic hyperplasia", "XIV Genitourinary system"),
            ("N80.9", "Endometriosis, unspecified", "XIV Genitourinary system"),
            ("N83.2", "Other and unspecified ovarian cysts", "XIV Genitourinary system"),
            ("N92.0", "Excessive and frequent menstruation with regular cycle", "XIV Genitourinary system"),
            ("O14.9", "Pre-eclampsia, unspecified", "XV Pregnancy and childbirth"),
            ("O24.4", "Diabetes mellitus arising in pregnancy", "XV Pregnancy and childbirth"),
            ("O42.9", "Premature rupture of membranes, unspecified", "XV Pregnancy and childbirth"),
            ("O80", "Single spontaneous delivery", "XV Pregnancy and childbirth"),
            ("O82", "Single delivery by caesarean section", "XV Pregnancy and childbirth"),
            ("P07.3", "Preterm newborn, other", "XVI Perinatal conditions"),
            ("P59.9", "Neonatal jaundice, unspecified", "XVI Perinatal conditions"),
            ("R05", "Cough", "XVIII Symptoms and signs"),
            ("R07.4", "Chest pain, unspecified", "XVIII Symptoms and signs"),
            ("R10.4", "Other and unspecified abdominal pain", "XVIII Symptoms and signs"),
            ("R11", "Nausea and vomiting", "XVIII Symptoms and signs"),
            ("R42", "Dizziness and giddiness", "XVIII Symptoms and signs"),
            ("R50.9", "Fever, unspecified", "XVIII Symptoms and signs"),
            ("R51", "Headache", "XVIII Symptoms and signs"),
            ("R55", "Syncope and collapse", "XVIII Symptoms and signs"),
            ("R56.8", "Other and unspecified convulsions", "XVIII Symptoms and signs"),
            ("S06.0", "Concussion", "XIX Injury and poisoning"),
            ("S22.3", "Fracture of one rib", "XIX Injury and poisoning"),
            ("S42.3", "Fracture of shaft of humerus", "XIX Injury and poisoning"),
            ("S52.5", "Fracture of lower end of radius", "XIX Injury and poisoning"),
            ("S72.0", "Fracture of neck of femur", "XIX Injury and poisoning"),
            ("S72.3", "Fracture of shaft of femur", "XIX Injury and poisoning"),
            ("S82.6", "Fracture of lateral malleolus", "XIX Injury and poisoning"),
            ("S93.4", "Sprain and strain of ankle", "XIX Injury and poisoning"),
            ("T14.1", "Open wound of unspecified body region", "XIX Injury and poisoning"),
            ("T30.0", "Burn of unspecified body region, unspecified degree", "XIX Injury and poisoning"),
            ("T63.0", "Toxic effect of snake venom", "XIX Injury and poisoning"),
            ("T78.2", "Anaphylactic shock, unspecified", "XIX Injury and poisoning"),
            ("Z00.0", "General medical examination", "XXI Factors influencing health status"),
            ("Z01.7", "Laboratory examination", "XXI Factors influencing health status"),
            ("Z13.1", "Special screening examination for diabetes mellitus", "XXI Factors influencing health status"),
            ("Z34.9", "Supervision of normal pregnancy, unspecified", "XXI Factors influencing health status"),
            ("Z51.1", "Chemotherapy session for neoplasm", "XXI Factors influencing health status"),
            ("Z96.6", "Presence of orthopaedic joint implants", "XXI Factors influencing health status"),
        ];

        var existing = await db.IcdCodes.Select(c => c.Code).ToListAsync(ct);
        var missing = rows.Where(r => !existing.Contains(r.Code)).ToList();
        if (missing.Count == 0)
        {
            return;
        }
        foreach (var (code, description, category) in missing)
        {
            db.IcdCodes.Add(new IcdCode { Code = code, Description = description, Category = category });
        }
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Demo: added {Count} ICD-10 codes", missing.Count);
    }

    // ── Packages ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Health-check and surgical packages, per branch (Package is branch-scoped), each priced
    /// below the sum of its member services so the bundle discount is visible on a bill.
    /// </summary>
    private async Task SeedPackageDepthAsync(CancellationToken ct)
    {
        (string Code, string Name, decimal Price, int ValidityDays, string[] ServiceCodes)[] rows =
        [
            ("PKG-BASIC-HC", "Basic Health Check-up", 2500m, 30,
                ["S0001", "S0017", "S0023", "S0028", "S0048", "S0033"]),
            ("PKG-EXEC-HC", "Executive Health Check-up", 7500m, 45,
                ["S0002", "S0017", "S0018", "S0019", "S0020", "S0021", "S0022", "S0023", "S0033", "S0042", "S0047", "S0048", "S0049"]),
            ("PKG-SENIOR-HC", "Senior Citizen Health Check-up", 6500m, 45,
                ["S0002", "S0017", "S0018", "S0019", "S0021", "S0022", "S0032", "S0033", "S0042", "S0047", "S0111", "S0106"]),
            ("PKG-WOMEN-HC", "Well Woman Check-up", 5500m, 45,
                ["S0054", "S0017", "S0021", "S0022", "S0097", "S0043", "S0046", "S0033"]),
            ("PKG-CARDIAC-HC", "Cardiac Screening Package", 8500m, 45,
                ["S0051", "S0017", "S0021", "S0028", "S0033", "S0047", "S0048", "S0049", "S0079"]),
            ("PKG-DIABETES-HC", "Diabetes Care Package", 4500m, 90,
                ["S0065", "S0020", "S0028", "S0029", "S0019", "S0023", "S0141", "S0166"]),
            ("PKG-PREOP", "Pre-operative Work-up Package", 5000m, 30,
                ["S0058", "S0017", "S0019", "S0032", "S0033", "S0048", "S0015"]),
            ("PKG-ANC-FULL", "Complete Antenatal Package", 22000m, 300,
                ["S0054", "S0096", "S0017", "S0022", "S0023", "S0028", "S0044", "S0033"]),
            ("PKG-DELIVERY-NVD", "Normal Delivery Package", 55000m, 0,
                ["S0092", "S0006", "S0009", "S0015", "S0168", "S0017"]),
            ("PKG-DELIVERY-LSCS", "Caesarean Delivery Package", 98000m, 0,
                ["S0093", "S0013", "S0006", "S0009", "S0015", "S0168", "S0017"]),
            ("PKG-CHOLE-LAP", "Laparoscopic Cholecystectomy Package", 88000m, 0,
                ["S0086", "S0013", "S0006", "S0009", "S0015", "S0017", "S0042"]),
            ("PKG-APPEND-LAP", "Laparoscopic Appendicectomy Package", 72000m, 0,
                ["S0087", "S0013", "S0006", "S0009", "S0015", "S0017"]),
            ("PKG-HERNIA", "Hernia Repair Package", 65000m, 0,
                ["S0088", "S0013", "S0006", "S0009", "S0015", "S0017"]),
            ("PKG-TKR", "Total Knee Replacement Package", 245000m, 0,
                ["S0080", "S0014", "S0007", "S0009", "S0015", "S0017", "S0013", "S0165"]),
            ("PKG-CATARACT", "Cataract Surgery Package", 48000m, 0,
                ["S0108", "S0013", "S0111", "S0112", "S0017"]),
            ("PKG-TONSIL", "Tonsillectomy Package", 55000m, 0,
                ["S0102", "S0013", "S0006", "S0009", "S0015", "S0017"]),
            ("PKG-DIALYSIS-ACCESS", "AV Fistula Creation Package", 58000m, 0,
                ["S0125", "S0013", "S0006", "S0015", "S0017", "S0019"]),
            ("PKG-ANGIO", "Coronary Angiography Package", 28000m, 0,
                ["S0076", "S0006", "S0009", "S0017", "S0019", "S0048"]),
        ];

        var branches = await db.Branches.IgnoreQueryFilters()
            .Where(b => !b.IsDeleted).OrderBy(b => b.Id).Select(b => b.Id).ToListAsync(ct);
        var existing = await db.Packages.IgnoreQueryFilters().Where(p => !p.IsDeleted)
            .Select(p => new { p.BranchId, p.Code }).ToListAsync(ct);
        var services = await db.Services.ToDictionaryAsync(s => s.Code, s => s.Id, ct);
        var added = 0;
        foreach (var branchId in branches)
        {
            foreach (var (code, name, price, validity, serviceCodes) in rows)
            {
                if (existing.Any(e => e.BranchId == branchId && e.Code == code))
                {
                    continue;
                }
                var package = new Package
                {
                    BranchId = branchId,
                    Code = code,
                    Name = name,
                    Description = $"Fixed-price bundle covering {serviceCodes.Length} services.",
                    Price = price,
                    ValidityDays = validity,
                };
                foreach (var serviceCode in serviceCodes.Where(services.ContainsKey))
                {
                    package.Items.Add(new PackageItem { ServiceId = services[serviceCode], Quantity = 1m });
                }
                if (package.Items.Count > 0)
                {
                    db.Packages.Add(package);
                    added++;
                }
            }
        }
        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Demo: added {Count} packages", added);
        }
    }

    // ── Prescription templates ────────────────────────────────────────────────
    /// <summary>
    /// Doctor prescription favourites, so OPD's "apply template" prefill has something to offer.
    /// Templates belong to a doctor, so these are spread across doctors by specialty where one
    /// matches; the regimens are ordinary symptomatic combinations, kept deliberately simple.
    /// </summary>
    private async Task SeedPrescriptionTemplateDepthAsync(CancellationToken ct)
    {
        // (TemplateName, SpecialtyNameFragment, items: (DrugCode, Dose, Frequency, Duration, Route))
        (string Name, string? Specialty, (string Drug, string Dose, string Frequency, string Duration, string Route)[] Items)[] rows =
        [
            ("Upper Respiratory Infection - Adult", null,
                [("DRGX0001", "1 tablet", "1-1-1", "5 days", "Oral"),
                 ("DRGX0012", "1 tablet", "1-0-0", "3 days", "Oral"),
                 ("DRGX0065", "1 tablet", "0-0-1", "5 days", "Oral"),
                 ("DRGX0026", "1 tablet", "1-0-0 before food", "5 days", "Oral")]),
            ("Acute Gastroenteritis", null,
                [("DRGX0026", "1 tablet", "1-0-1 before food", "5 days", "Oral"),
                 ("DRGX0030", "1 tablet", "1-1-1 SOS", "3 days", "Oral"),
                 ("DRGX0022", "1 tablet", "1-1-1", "5 days", "Oral"),
                 ("DRGX0033", "1 tablet", "1-1-1 SOS", "3 days", "Oral")]),
            ("Urinary Tract Infection", null,
                [("DRGX0019", "1 tablet", "1-0-1", "7 days", "Oral"),
                 ("DRGX0001", "1 tablet", "1-1-1 SOS", "3 days", "Oral"),
                 ("DRGX0026", "1 tablet", "1-0-0 before food", "7 days", "Oral")]),
            ("Hypertension - Newly Diagnosed", "Cardio",
                [("DRGX0042", "1 tablet", "1-0-0", "30 days", "Oral"),
                 ("DRGX0041", "1 tablet", "0-0-1", "30 days", "Oral"),
                 ("DRGX0052", "1 tablet", "0-0-1", "30 days", "Oral")]),
            ("Ischaemic Heart Disease - Maintenance", "Cardio",
                [("DRGX0055", "1 tablet", "0-1-0 after food", "30 days", "Oral"),
                 ("DRGX0054", "1 tablet", "0-1-0", "30 days", "Oral"),
                 ("DRGX0053", "1 tablet", "0-0-1", "30 days", "Oral"),
                 ("DRGX0045", "1 tablet", "1-0-1", "30 days", "Oral")]),
            ("Type 2 Diabetes - Starter", "Endocr",
                [("DRGX0035", "1 tablet", "1-0-1 after food", "30 days", "Oral"),
                 ("DRGX0100", "1 capsule", "Once weekly", "8 weeks", "Oral"),
                 ("DRGX0102", "1 tablet", "0-1-0", "30 days", "Oral")]),
            ("Bronchial Asthma - Maintenance", "Pulmo",
                [("DRGX0063", "2 puffs", "1-0-1", "30 days", "Inhalation"),
                 ("DRGX0064", "1 tablet", "0-0-1", "30 days", "Oral"),
                 ("DRGX0060", "2 puffs", "SOS", "30 days", "Inhalation")]),
            ("COPD Exacerbation", "Pulmo",
                [("DRGX0061", "1 respule", "1-1-1", "5 days", "Nebulisation"),
                 ("DRGX0062", "1 respule", "1-0-1", "5 days", "Nebulisation"),
                 ("DRGX0068", "1 tablet", "1-0-0 after food", "5 days", "Oral"),
                 ("DRGX0010", "1 tablet", "1-0-1", "7 days", "Oral")]),
            ("Osteoarthritis Knee", "Ortho",
                [("DRGX0007", "1 tablet", "1-0-1 after food", "10 days", "Oral"),
                 ("DRGX0026", "1 tablet", "1-0-0 before food", "10 days", "Oral"),
                 ("DRGX0101", "1 tablet", "0-0-1", "30 days", "Oral"),
                 ("DRGX0100", "1 capsule", "Once weekly", "8 weeks", "Oral")]),
            ("Post-operative Analgesia", "Surg",
                [("DRGX0001", "1 tablet", "1-1-1", "5 days", "Oral"),
                 ("DRGX0007", "1 tablet", "1-0-1 after food", "5 days", "Oral"),
                 ("DRGX0026", "1 tablet", "1-0-0 before food", "5 days", "Oral"),
                 ("DRGX0022", "1 tablet", "1-1-1", "5 days", "Oral")]),
            ("Allergic Dermatitis", "Derma",
                [("DRGX0065", "1 tablet", "0-0-1", "10 days", "Oral"),
                 ("DRGX0116", "Apply thinly", "1-0-1", "7 days", "Topical"),
                 ("DRGX0114", "Apply thinly", "1-0-1", "7 days", "Topical")]),
            ("Iron Deficiency Anaemia", null,
                [("DRGX0098", "1 tablet", "1-0-0", "60 days", "Oral"),
                 ("DRGX0099", "1 capsule", "1-0-0", "30 days", "Oral"),
                 ("DRGX0026", "1 tablet", "1-0-0 before food", "30 days", "Oral")]),
            ("Antenatal Supplementation", "Gyn",
                [("DRGX0098", "1 tablet", "1-0-0", "90 days", "Oral"),
                 ("DRGX0101", "1 tablet", "0-0-1", "90 days", "Oral"),
                 ("DRGX0100", "1 capsule", "Once weekly", "8 weeks", "Oral")]),
            ("Hypothyroidism - Maintenance", "Endocr",
                [("DRGX0072", "1 tablet", "Empty stomach, once daily", "60 days", "Oral")]),
            ("Acid Peptic Disease", "Gastro",
                [("DRGX0026", "1 tablet", "1-0-1 before food", "14 days", "Oral"),
                 ("DRGX0032", "1 tablet", "1-0-1 before food", "14 days", "Oral"),
                 ("DRGX0034", "15 ml", "0-0-1", "7 days", "Oral")]),
            ("Migraine - Acute & Prophylaxis", "Neuro",
                [("DRGX0001", "1 tablet", "SOS up to TDS", "5 days", "Oral"),
                 ("DRGX0030", "1 tablet", "SOS", "3 days", "Oral"),
                 ("DRGX0045", "1 tablet", "0-0-1", "30 days", "Oral")]),
        ];

        var doctors = await db.Doctors
            .Include(d => d.Specialty)
            .Select(d => new { d.Id, SpecialtyName = d.Specialty!.Name })
            .ToListAsync(ct);
        if (doctors.Count == 0)
        {
            return;
        }

        var existingNames = await db.PrescriptionTemplates.Select(t => t.Name).ToListAsync(ct);
        var missing = rows.Where(r => !existingNames.Contains(r.Name)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        var drugs = await db.Drugs.ToDictionaryAsync(d => d.Code, d => d.Id, ct);
        var fallbackIndex = 0;
        foreach (var (name, specialty, items) in missing)
        {
            // Prefer a doctor whose specialty matches the template's subject; otherwise spread
            // the general-purpose templates round-robin so they aren't all on one doctor.
            var doctor = specialty is null
                ? null
                : doctors.FirstOrDefault(d => d.SpecialtyName is not null &&
                    d.SpecialtyName.Contains(specialty, StringComparison.OrdinalIgnoreCase));
            doctor ??= doctors[fallbackIndex++ % doctors.Count];

            var template = new PrescriptionTemplate { DoctorId = doctor.Id, Name = name };
            foreach (var (drugCode, dose, frequency, duration, route) in items.Where(i => drugs.ContainsKey(i.Drug)))
            {
                template.Items.Add(new PrescriptionTemplateItem
                {
                    DrugId = drugs[drugCode],
                    Dose = dose,
                    Frequency = frequency,
                    Duration = duration,
                    Route = route,
                });
            }
            if (template.Items.Count > 0)
            {
                db.PrescriptionTemplates.Add(template);
            }
        }
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Demo: added {Count} prescription templates", missing.Count);
    }

    // ── ICU critical-alert rules ──────────────────────────────────────────────
    /// <summary>
    /// A full threshold set per branch, so an ICU vitals entry actually trips alerts on any
    /// branch. Rules are branch-scoped by design (each branch sets its own limits); only two
    /// existed in total, which left the alert engine effectively inert.
    /// </summary>
    private async Task SeedIcuAlertRuleDepthAsync(CancellationToken ct)
    {
        (CriticalAlertParameter Parameter, ComparisonOperator Operator, decimal Threshold, AlertSeverity Severity, string Description)[] rules =
        [
            (CriticalAlertParameter.SpO2, ComparisonOperator.LessThan, 92m, AlertSeverity.Warning, "SpO2 below 92% — reassess oxygenation."),
            (CriticalAlertParameter.SpO2, ComparisonOperator.LessThan, 88m, AlertSeverity.Critical, "SpO2 below 88% — escalate to intensivist immediately."),
            (CriticalAlertParameter.PulseRate, ComparisonOperator.LessThan, 50m, AlertSeverity.Critical, "Bradycardia — pulse below 50/min."),
            (CriticalAlertParameter.PulseRate, ComparisonOperator.GreaterThan, 120m, AlertSeverity.Warning, "Tachycardia — pulse above 120/min."),
            (CriticalAlertParameter.PulseRate, ComparisonOperator.GreaterThan, 140m, AlertSeverity.Critical, "Severe tachycardia — pulse above 140/min."),
            (CriticalAlertParameter.RespiratoryRate, ComparisonOperator.LessThan, 8m, AlertSeverity.Critical, "Respiratory rate below 8/min."),
            (CriticalAlertParameter.RespiratoryRate, ComparisonOperator.GreaterThan, 28m, AlertSeverity.Warning, "Tachypnoea — respiratory rate above 28/min."),
            (CriticalAlertParameter.RespiratoryRate, ComparisonOperator.GreaterThan, 35m, AlertSeverity.Critical, "Severe tachypnoea — respiratory rate above 35/min."),
            (CriticalAlertParameter.BloodPressureSystolic, ComparisonOperator.LessThan, 90m, AlertSeverity.Critical, "Hypotension — systolic below 90 mmHg."),
            (CriticalAlertParameter.BloodPressureSystolic, ComparisonOperator.GreaterThan, 180m, AlertSeverity.Warning, "Hypertension — systolic above 180 mmHg."),
            (CriticalAlertParameter.BloodPressureSystolic, ComparisonOperator.GreaterThan, 200m, AlertSeverity.Critical, "Hypertensive crisis — systolic above 200 mmHg."),
            (CriticalAlertParameter.BloodPressureDiastolic, ComparisonOperator.LessThan, 50m, AlertSeverity.Warning, "Diastolic below 50 mmHg."),
            (CriticalAlertParameter.BloodPressureDiastolic, ComparisonOperator.GreaterThan, 110m, AlertSeverity.Warning, "Diastolic above 110 mmHg."),
            (CriticalAlertParameter.TemperatureF, ComparisonOperator.LessThan, 95m, AlertSeverity.Critical, "Hypothermia — below 95 °F."),
            (CriticalAlertParameter.TemperatureF, ComparisonOperator.GreaterThan, 101.5m, AlertSeverity.Warning, "Fever above 101.5 °F."),
            (CriticalAlertParameter.TemperatureF, ComparisonOperator.GreaterThan, 104m, AlertSeverity.Critical, "Hyperpyrexia — above 104 °F."),
        ];

        var branches = await db.Branches.IgnoreQueryFilters()
            .Where(b => !b.IsDeleted).OrderBy(b => b.Id).Select(b => b.Id).ToListAsync(ct);
        var existing = await db.CriticalAlertRules.IgnoreQueryFilters().Where(r => !r.IsDeleted)
            .Select(r => new { r.BranchId, r.Parameter, r.Operator, r.ThresholdValue }).ToListAsync(ct);
        var added = 0;
        foreach (var branchId in branches)
        {
            foreach (var (parameter, op, threshold, severity, description) in rules)
            {
                if (existing.Any(e => e.BranchId == branchId && e.Parameter == parameter &&
                        e.Operator == op && e.ThresholdValue == threshold))
                {
                    continue;
                }
                db.CriticalAlertRules.Add(new CriticalAlertRule
                {
                    BranchId = branchId,
                    Parameter = parameter,
                    Operator = op,
                    ThresholdValue = threshold,
                    Severity = severity,
                    Description = description,
                });
                added++;
            }
        }
        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Demo: added {Count} ICU critical-alert rules", added);
        }
    }

    // ── Nationalities ─────────────────────────────────────────────────────────
    /// <summary>
    /// Registration's Nationality dropdown had exactly two options ("Indian" and "Other"), which
    /// makes the foreign-national tariff path impossible to demo properly.
    /// </summary>
    private async Task SeedNationalityDepthAsync(CancellationToken ct)
    {
        await EnsureAsync(db.Nationalities,
        [
            ("AF", "Afghan"), ("AU", "Australian"), ("BD", "Bangladeshi"), ("BT", "Bhutanese"),
            ("BR", "Brazilian"), ("CA", "Canadian"), ("CN", "Chinese"), ("EG", "Egyptian"),
            ("ET", "Ethiopian"), ("FR", "French"), ("DE", "German"), ("ID", "Indonesian"),
            ("IR", "Iranian"), ("IQ", "Iraqi"), ("IT", "Italian"), ("JP", "Japanese"),
            ("KE", "Kenyan"), ("KW", "Kuwaiti"), ("MY", "Malaysian"), ("MV", "Maldivian"),
            ("MM", "Myanmarese"), ("NP", "Nepalese"), ("NG", "Nigerian"), ("OM", "Omani"),
            ("PK", "Pakistani"), ("PH", "Filipino"), ("QA", "Qatari"), ("RU", "Russian"),
            ("SA", "Saudi Arabian"), ("SG", "Singaporean"), ("ZA", "South African"),
            ("KR", "South Korean"), ("LK", "Sri Lankan"), ("SD", "Sudanese"), ("TZ", "Tanzanian"),
            ("TH", "Thai"), ("TR", "Turkish"), ("UG", "Ugandan"), ("AE", "Emirati"),
            ("GB", "British"), ("US", "American"), ("UZ", "Uzbek"), ("YE", "Yemeni"),
        ], (code, name) => new Nationality { Code = code, Name = name }, ct);
    }
}
