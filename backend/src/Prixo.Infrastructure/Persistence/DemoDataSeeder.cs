using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Billing;
using Prixo.Domain.Entities.Biomed;
using Prixo.Domain.Entities.Insurance;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Loyalty;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Mrd;
using Prixo.Domain.Entities.Opd;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Entities.Pharmacy;
using Prixo.Domain.Entities.Quality;
using Prixo.Domain.Enums;

namespace Prixo.Infrastructure.Persistence;

/// <summary>
/// Demo/dummy data for evaluation, UAT rehearsal and screenshot-ready dev environments —
/// deliberately NOT part of <see cref="DbSeeder"/>, which only ever seeds genuine reference
/// data and must stay safe to run in production. This one invents patients, claims, incidents
/// and audit findings, so it hard-refuses to run outside Development and is opt-in even there
/// (config <c>Seed:DemoData=true</c>, wired in Program.cs).
///
/// <para>Every section is guarded "if this table already has rows, skip it", so re-running is
/// safe and never duplicates. It also means a section you clear out by hand will be re-seeded
/// on the next run — that is the intended way to reset one module.</para>
///
/// <para>Sections write entities through <see cref="AppDbContext"/> directly rather than through
/// the Application services. That is deliberate: the services enforce workflow guards (day-close
/// windows, approval separation-of-duties, sequence generation) that exist to protect real
/// operational data and would reject the back-dated, already-in-a-terminal-state rows demo data
/// needs. The trade-off is that this seeder is responsible for writing self-consistent rows
/// itself — notably the pharmacy returns below, which post their own stock-ledger movements and
/// batch increments so stock reports still reconcile.</para>
///
/// <para>Deliberately NOT seeded: the outbox/placeholder tables that later phases will own —
/// <c>admin.notification_queue</c>, <c>billing.online_payment_requests</c>,
/// <c>reporting.report_subscriptions</c>, <c>icu.device_readings</c> — plus
/// <c>patient.portal_refresh_tokens</c> (live session state; a fabricated token is meaningless).
/// Faking rows there would imply queued work that no sender will ever process.</para>
/// </summary>
public partial class DemoDataSeeder(
    AppDbContext db,
    IPasswordHasherService hasher,
    IHostEnvironment environment,
    ISequenceService sequences,
    ILogger<DemoDataSeeder> logger)
{
    // Fixed seed so a rebuilt demo environment reproduces the same data.
    private readonly Random _random = new(20260808);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "DemoDataSeeder generates fictitious patients/claims/incidents and must never run outside Development.");
        }

        var branch = await db.Branches.IgnoreQueryFilters().OrderBy(b => b.Id).FirstOrDefaultAsync(ct);
        if (branch is null)
        {
            logger.LogWarning("Demo seed skipped — no branch exists yet. Run the normal DbSeeder first.");
            return;
        }

        var userId = await db.Users.IgnoreQueryFilters()
            .Where(u => u.Username == "admin").Select(u => u.Id).FirstOrDefaultAsync(ct);
        if (userId == 0)
        {
            logger.LogWarning("Demo seed skipped — the admin user does not exist yet.");
            return;
        }

        var ctx = new DemoContext(branch.Id, userId);

        // Catalog depth first — the operational history below prices its bill lines off the
        // Service/Tariff rows this fills in, and several sections below read the same catalogs.
        await SeedCatalogDepthAsync(ctx, ct);

        await SeedFrontOfficeMastersAsync(ctx, ct);
        await SeedProcurementMastersAsync(ctx, ct);
        await SeedBillingMastersAsync(ctx, ct);
        await SeedLoyaltyAsync(ctx, ct);
        await SeedInsuranceAsync(ctx, ct);
        await SeedQualityAsync(ctx, ct);
        await SeedBiomedAsync(ctx, ct);
        await SeedMrdAsync(ctx, ct);
        await SeedBillingWorkflowAsync(ctx, ct);
        await SeedPharmacyExtrasAsync(ctx, ct);
        await SeedClinicalExtrasAsync(ctx, ct);
        await SeedPatientExperienceAsync(ctx, ct);
        await SeedFinanceExtrasAsync(ctx, ct);

        // Last: needs the catalogs above and posts against every branch, not just ctx.BranchId.
        await SeedOperationalHistoryAsync(ctx, ct);

        logger.LogInformation("Demo data seed completed");
    }

    private sealed record DemoContext(long BranchId, long UserId);

    // ── Front-office / registration masters ──────────────────────────────────
    // DbSeeder deliberately leaves the hospital-specific ones empty ("each hospital populates
    // via their own screen"); a demo environment needs them filled or half the registration
    // and OP-billing screens render empty dropdowns.
    private async Task SeedFrontOfficeMastersAsync(DemoContext ctx, CancellationToken ct)
    {
        // Salutations, marital statuses and identity-document types moved to DbSeeder — they
        // are universal reference data every deployment needs, not demo dressing.

        await EnsureAsync(db.VisitClasses,
        [("REGULAR", "Regular"), ("EXPRESS", "Express"), ("PREMIUM", "Premium"), ("CORPORATE", "Corporate"),
         ("SENIOR", "Senior Citizen"), ("STAFF", "Staff"), ("CAMP", "Camp"), ("MHC", "Health Checkup"),
         ("EMERGENCY", "Emergency"), ("TELE", "Tele-consultation"), ("REVIEW", "Review"), ("VIP", "VIP")],
            (c, n) => new VisitClass { Code = c, Name = n }, ct);

        await EnsureAsync(db.VisitSessions,
        [("MORNING", "Morning"), ("FORENOON", "Forenoon"), ("AFTERNOON", "Afternoon"),
         ("EVENING", "Evening"), ("NIGHT", "Night"), ("EMERGENCY", "Emergency / Off-hours")],
            (c, n) => new VisitSession { Code = c, Name = n }, ct);

        await EnsureAsync(db.PatientCaseTypes,
        [
            ("GENERAL", "General"), ("MLC", "Medico-Legal"), ("ACCIDENT", "Accident"),
            ("INSURANCE", "Insurance"), ("CAMP", "Camp"), ("EMPLOYEE", "Hospital Employee"),
            ("CORPORATE", "Corporate Tie-up"), ("SCHEME", "Government Scheme"),
            ("MHC", "Master Health Checkup"), ("REFERRAL", "Referral In"),
            ("SECOND_OPINION", "Second Opinion"), ("FOLLOWUP", "Follow-up"),
            ("MATERNITY", "Maternity"), ("NEONATAL", "Neonatal"),
            ("DAYCARE", "Day Care"), ("INTERNATIONAL", "International Patient"),
        ], (c, n) => new PatientCaseType { Code = c, Name = n }, ct);

        await EnsureAsync(db.PatientHcTags,
        [("DIABETIC", "Diabetic"), ("HYPERTENSIVE", "Hypertensive"), ("CARDIAC", "Cardiac"),
         ("RENAL", "Renal"), ("ONCOLOGY", "Oncology"), ("GERIATRIC", "Geriatric"),
         ("ASTHMA", "Asthma / COPD"), ("THYROID", "Thyroid Disorder"), ("EPILEPSY", "Epilepsy"),
         ("HIV", "HIV Positive"), ("HEPB", "Hepatitis B Positive"), ("HEPC", "Hepatitis C Positive"),
         ("TB", "Tuberculosis"), ("ANTICOAG", "On Anticoagulants"), ("DIALYSIS", "On Dialysis"),
         ("TRANSPLANT", "Transplant Recipient"), ("IMMUNO", "Immunocompromised"),
         ("PREGNANT", "Pregnant"), ("PALLIATIVE", "Palliative Care"), ("BARIATRIC", "Bariatric"),
         ("FALL_RISK", "Fall Risk"), ("ALLERGY", "Multiple Drug Allergies")],
            (c, n) => new PatientHcTag { Code = c, Name = n }, ct);

        await EnsureAsync(db.Cities,
        [
            ("CHENNAI", "Chennai"), ("COIMBATORE", "Coimbatore"), ("MADURAI", "Madurai"),
            ("TRICHY", "Tiruchirappalli"), ("SALEM", "Salem"), ("BENGALURU", "Bengaluru"),
            ("HYDERABAD", "Hyderabad"), ("MUMBAI", "Mumbai"), ("DELHI", "New Delhi"), ("KOCHI", "Kochi"),
            ("ERODE", "Erode"), ("TIRUPPUR", "Tiruppur"), ("VELLORE", "Vellore"),
            ("THANJAVUR", "Thanjavur"), ("TIRUNELVELI", "Tirunelveli"), ("THOOTHUKUDI", "Thoothukudi"),
            ("DINDIGUL", "Dindigul"), ("KANCHIPURAM", "Kanchipuram"), ("CUDDALORE", "Cuddalore"),
            ("NAGERCOIL", "Nagercoil"), ("HOSUR", "Hosur"), ("KARUR", "Karur"),
            ("PONDICHERRY", "Puducherry"), ("MYSURU", "Mysuru"), ("MANGALURU", "Mangaluru"),
            ("THIRUVANANTHAPURAM", "Thiruvananthapuram"), ("KOZHIKODE", "Kozhikode"),
            ("THRISSUR", "Thrissur"), ("VIJAYAWADA", "Vijayawada"), ("VISAKHAPATNAM", "Visakhapatnam"),
            ("TIRUPATI", "Tirupati"), ("PUNE", "Pune"), ("KOLKATA", "Kolkata"),
            ("AHMEDABAD", "Ahmedabad"), ("JAIPUR", "Jaipur"), ("LUCKNOW", "Lucknow"),
        ], (c, n) => new City { Code = c, Name = n }, ct);

        // Keyed on Code rather than "skip if the table has any rows": the list below grows over
        // time, and a whole-table guard would freeze an environment at whatever it seeded first.
        {
            var cityByCode = await db.Cities.ToDictionaryAsync(c => c.Code, ct);
            var existingAreas = await db.Areas.Select(a => a.Code).ToListAsync(ct);
            (string Code, string Name, string City)[] areas =
            [
                ("ADYAR", "Adyar", "CHENNAI"), ("TNAGAR", "T. Nagar", "CHENNAI"),
                ("ANNANAGAR", "Anna Nagar", "CHENNAI"), ("VELACHERY", "Velachery", "CHENNAI"),
                ("MYLAPORE", "Mylapore", "CHENNAI"), ("GUINDY", "Guindy", "CHENNAI"),
                ("TAMBARAM", "Tambaram", "CHENNAI"), ("PORUR", "Porur", "CHENNAI"),
                ("AMBATTUR", "Ambattur", "CHENNAI"), ("PERUNGUDI", "Perungudi", "CHENNAI"),
                ("NUNGAMBAKKAM", "Nungambakkam", "CHENNAI"), ("KODAMBAKKAM", "Kodambakkam", "CHENNAI"),
                ("RSPURAM", "R.S. Puram", "COIMBATORE"), ("GANDHIPURAM", "Gandhipuram", "COIMBATORE"),
                ("PEELAMEDU", "Peelamedu", "COIMBATORE"), ("SAIBABA_COLONY", "Saibaba Colony", "COIMBATORE"),
                ("SINGANALLUR", "Singanallur", "COIMBATORE"),
                ("JAYANAGAR", "Jayanagar", "BENGALURU"), ("WHITEFIELD", "Whitefield", "BENGALURU"),
                ("INDIRANAGAR", "Indiranagar", "BENGALURU"), ("KORAMANGALA", "Koramangala", "BENGALURU"),
                ("HEBBAL", "Hebbal", "BENGALURU"), ("ELECTRONIC_CITY", "Electronic City", "BENGALURU"),
                ("ANNA_NAGAR_MDU", "Anna Nagar", "MADURAI"), ("KKNAGAR_MDU", "K.K. Nagar", "MADURAI"),
                ("SRIRANGAM", "Srirangam", "TRICHY"), ("THILLAI_NAGAR", "Thillai Nagar", "TRICHY"),
                ("FAIRLANDS", "Fairlands", "SALEM"), ("HASTHAMPATTI", "Hasthampatti", "SALEM"),
                ("BANJARA_HILLS", "Banjara Hills", "HYDERABAD"), ("GACHIBOWLI", "Gachibowli", "HYDERABAD"),
                ("ANDHERI", "Andheri", "MUMBAI"), ("BANDRA", "Bandra", "MUMBAI"),
                ("SAKET", "Saket", "DELHI"), ("DWARKA", "Dwarka", "DELHI"),
                ("KALOOR", "Kaloor", "KOCHI"), ("EDAPPALLY", "Edappally", "KOCHI"),
            ];
            foreach (var (code, name, city) in areas
                .Where(a => !existingAreas.Contains(a.Code) && cityByCode.ContainsKey(a.City)))
            {
                db.Areas.Add(new Area { Code = code, Name = name, CityId = cityByCode[city].Id });
            }
            await db.SaveChangesAsync(ct);
        }

        // master.loyalty_programs — the legacy front-office list (distinct from the Phase-23
        // loyalty card masters below); it was shipping empty, so its dropdown rendered blank.
        await EnsureAsync(db.LoyaltyPrograms,
        [
            ("LP-SILVER", "Silver Membership"), ("LP-GOLD", "Gold Membership"),
            ("LP-PLATINUM", "Platinum Membership"), ("LP-SENIOR", "Senior Citizen Privilege"),
            ("LP-CORP", "Corporate Wellness Programme"), ("LP-MATERNITY", "Maternity Care Programme"),
            ("LP-DIABETES", "Diabetes Care Programme"), ("LP-CARDIAC", "Cardiac Care Programme"),
            ("LP-STAFF", "Staff Welfare Programme"), ("LP-ALUMNI", "Alumni Privilege"),
        ], (c, n) => new LoyaltyProgram { Code = c, Name = n }, ct);

        await EnsureAsync(db.Camps,
        [
            ("CAMP-DIAB-01", "Diabetes Screening Camp — Adyar"),
            ("CAMP-EYE-01", "Free Eye Check-up Camp — Velachery"),
            ("CAMP-CARD-01", "Cardiac Screening Camp — Anna Nagar"),
            ("CAMP-WOMEN-01", "Women's Health Camp — T. Nagar"),
            ("CAMP-BONE-01", "Bone Density Camp — Mylapore"),
            ("CAMP-DENTAL-01", "Dental Check-up Camp — Guindy"),
            ("CAMP-BP-01", "Blood Pressure Awareness Camp — Tambaram"),
            ("CAMP-CANCER-01", "Cancer Screening Camp — Porur"),
            ("CAMP-CHILD-01", "Child Immunisation Camp — Ambattur"),
            ("CAMP-ORTHO-01", "Orthopaedic Screening Camp — Peelamedu"),
            ("CAMP-ENT-01", "ENT & Hearing Camp — Gandhipuram"),
            ("CAMP-SKIN-01", "Dermatology Camp — R.S. Puram"),
            ("CAMP-CORP-TCS", "Corporate Wellness Camp — TCS Siruseri"),
            ("CAMP-CORP-INFY", "Corporate Wellness Camp — Infosys Sholinganallur"),
            ("CAMP-SCHOOL-01", "School Health Screening — DAV Velachery"),
            ("CAMP-RURAL-01", "Rural Outreach Camp — Kanchipuram"),
        ], (c, n) => new Camp { Code = c, Name = n }, ct);

        await EnsureAsync(db.MhcDoctors,
        [("MHC-GEN", "MHC General Physician"), ("MHC-CARD", "MHC Cardiologist"),
         ("MHC-GYN", "MHC Gynaecologist"), ("MHC-ORTHO", "MHC Orthopaedician"),
         ("MHC-ENT", "MHC ENT Surgeon"), ("MHC-OPHTHAL", "MHC Ophthalmologist"),
         ("MHC-DENT", "MHC Dental Surgeon"), ("MHC-DERM", "MHC Dermatologist"),
         ("MHC-DIET", "MHC Dietician"), ("MHC-PSYCH", "MHC Psychologist")],
            (c, n) => new MhcDoctor { Code = c, Name = n }, ct);

        {
            var existingMhc = await db.MhcPackages.Select(p => p.Code).ToListAsync(ct);
            (string Code, string Name, decimal Price)[] packages =
            [
                ("MHC-BASIC", "Basic Health Checkup", 1500m),
                ("MHC-EXEC", "Executive Health Checkup", 4500m),
                ("MHC-CARDIAC", "Comprehensive Cardiac Checkup", 7500m),
                ("MHC-WOMEN", "Well-Woman Checkup", 3800m),
                ("MHC-SENIOR", "Senior Citizen Checkup", 5200m),
                ("MHC-DIABETIC", "Diabetic Care Checkup", 2900m),
                ("MHC-CORPORATE", "Corporate Annual Checkup", 3200m),
                ("MHC-PREEMP", "Pre-Employment Checkup", 1800m),
                ("MHC-PREMARITAL", "Pre-Marital Screening", 2600m),
                ("MHC-ANTENATAL", "Antenatal Package", 4200m),
                ("MHC-CHILD", "Child Wellness Checkup", 2100m),
                ("MHC-RENAL", "Renal Profile Checkup", 3400m),
                ("MHC-LIVER", "Liver Profile Checkup", 3100m),
                ("MHC-THYROID", "Thyroid Screening", 1600m),
                ("MHC-CANCER-M", "Cancer Screening — Male", 8900m),
                ("MHC-CANCER-F", "Cancer Screening — Female", 9400m),
                ("MHC-PLATINUM", "Platinum Whole-Body Checkup", 14500m),
            ];
            foreach (var (code, name, price) in packages.Where(p => !existingMhc.Contains(p.Code)))
            {
                db.MhcPackages.Add(new MhcPackage { Code = code, Name = name, Price = price });
            }
            await db.SaveChangesAsync(ct);
        }

        await EnsureAsync(db.Units,
        [
            ("UNIT-A", "Unit A — General Medicine"), ("UNIT-B", "Unit B — General Surgery"),
            ("UNIT-C", "Unit C — Orthopaedics"), ("UNIT-D", "Unit D — Paediatrics"),
            ("UNIT-E", "Unit E — Cardiology"), ("UNIT-F", "Unit F — Neurology"),
            ("UNIT-G", "Unit G — Obstetrics & Gynaecology"), ("UNIT-H", "Unit H — Nephrology"),
            ("UNIT-I", "Unit I — Pulmonology"), ("UNIT-J", "Unit J — Gastroenterology"),
            ("UNIT-K", "Unit K — Oncology"), ("UNIT-L", "Unit L — Urology"),
        ], (c, n) => new Unit { Code = c, Name = n }, ct);

        if (!await db.UnitDoctors.AnyAsync(ct))
        {
            var units = await db.Units.OrderBy(u => u.Id).ToListAsync(ct);
            var doctorIds = await db.Doctors.OrderBy(d => d.Id).Select(d => d.Id).Take(12).ToListAsync(ct);
            for (var i = 0; i < doctorIds.Count && units.Count > 0; i++)
            {
                db.UnitDoctors.Add(new UnitDoctor { UnitId = units[i % units.Count].Id, DoctorId = doctorIds[i] });
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.UnitSchedules.AnyAsync(ct))
        {
            foreach (var unit in await db.Units.OrderBy(u => u.Id).ToListAsync(ct))
            {
                foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday })
                {
                    db.UnitSchedules.Add(new Domain.Entities.Appointments.UnitSchedule
                    {
                        BranchId = ctx.BranchId, UnitId = unit.Id, DayOfWeek = day,
                        StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(13, 0),
                    });
                }
            }
            await db.SaveChangesAsync(ct);
        }

        {
            var existingApptTypes = await db.AppointmentTypes.Select(t => t.Code).ToListAsync(ct);
            (string Code, string Name, int Minutes)[] types =
            [
                ("NEW", "New Consultation", 15), ("FOLLOWUP", "Follow-up", 10),
                ("PROCEDURE", "Procedure", 30), ("TELE", "Tele-consultation", 15),
                ("SECOND_OPINION", "Second Opinion", 20),
                ("REVIEW", "Review", 10), ("COUNSELLING", "Counselling", 30),
                ("DRESSING", "Dressing / Minor OP", 15), ("VACCINATION", "Vaccination", 10),
                ("MHC", "Health Checkup", 45), ("PRE_ANAESTHESIA", "Pre-Anaesthesia Review", 20),
                ("PHYSIO", "Physiotherapy Session", 30), ("DIET", "Dietician Consultation", 20),
                ("REPORT_REVIEW", "Report Review", 10), ("EMERGENCY", "Emergency Walk-in", 20),
                ("HOME_VISIT", "Home Visit", 60),
            ];
            foreach (var (code, name, minutes) in types.Where(t => !existingApptTypes.Contains(t.Code)))
            {
                db.AppointmentTypes.Add(new AppointmentType { Code = code, Name = name, DefaultSlotMinutes = minutes });
            }
            await db.SaveChangesAsync(ct);
        }

        {
            var existingRefs = await db.Referrers.IgnoreQueryFilters().Select(r => r.Code).ToListAsync(ct);
            (string Code, string Name, ReferrerType Type, string? Org)[] referrers =
            [
                ("REF-EXT-01", "Dr. Meenakshi Sundaram", ReferrerType.External, "Sundaram Clinic, Adyar"),
                ("REF-EXT-02", "Dr. Anand Krishnan", ReferrerType.External, "Krishnan Poly Clinic"),
                ("REF-EXT-03", "Lakshmi Diagnostics", ReferrerType.External, "Lakshmi Diagnostics Pvt Ltd"),
                ("REF-EXT-04", "Walk-in / Self", ReferrerType.External, null),
                ("REF-EXT-05", "Corporate Health Desk", ReferrerType.External, "TCS Occupational Health"),
                ("REF-EXT-06", "Dr. Priya Raghavan", ReferrerType.External, "Raghavan Child Care"),
                ("REF-EXT-07", "Dr. Suresh Babu", ReferrerType.External, "Babu Ortho Centre, Velachery"),
                ("REF-EXT-08", "Dr. Fathima Noor", ReferrerType.External, "Noor Womens Clinic"),
                ("REF-EXT-09", "Vijaya Scan Centre", ReferrerType.External, "Vijaya Scans, T. Nagar"),
                ("REF-EXT-10", "Apollo Pharmacy Health Desk", ReferrerType.External, "Apollo Pharmacy"),
                ("REF-EXT-11", "Dr. Ramesh Iyer", ReferrerType.External, "Iyer Cardiac Care"),
                ("REF-EXT-12", "Dr. Kavitha Menon", ReferrerType.External, "Menon Skin Clinic"),
                ("REF-EXT-13", "Sri Balaji Nursing Home", ReferrerType.External, "Sri Balaji Nursing Home"),
                ("REF-EXT-14", "Government PHC Kanchipuram", ReferrerType.External, "PHC Kanchipuram"),
                ("REF-EXT-15", "Infosys Occupational Health", ReferrerType.External, "Infosys Ltd"),
                ("REF-INT-01", "Dr. In-house — General Medicine", ReferrerType.Internal, null),
                ("REF-INT-02", "Dr. In-house — Surgery", ReferrerType.Internal, null),
                ("REF-INT-03", "Dr. In-house — Emergency", ReferrerType.Internal, null),
                ("REF-INT-04", "Hospital Health Camp Desk", ReferrerType.Internal, null),
                ("REF-INT-05", "Patient Portal Self-Booking", ReferrerType.Internal, null),
            ];
            foreach (var (code, name, type, org) in referrers.Where(r => !existingRefs.Contains(r.Code)))
            {
                db.Referrers.Add(new Referrer
                {
                    BranchId = ctx.BranchId, Code = code, Name = name, Type = type, Organization = org,
                    Phone = $"98{_random.Next(10000000, 99999999)}",
                });
            }
            await db.SaveChangesAsync(ct);
        }
    }

    // ── Procurement / payer / catalog masters ────────────────────────────────
    // These four had no seeder at all: whatever rows a dev database held were typed in through
    // the UI. That left Pharmacy/Stores purchase screens, corporate billing and the Insurance
    // module picking from a handful of ad-hoc entries. Codes are distinct from the generic
    // hand-entered ones (VND1, SVC1…) so both can coexist without a unique-index clash.
    private async Task SeedProcurementMastersAsync(DemoContext ctx, CancellationToken ct)
    {
        await EnsureAsync(db.ServiceCategories,
        [
            ("SC-PHARMACY", "Pharmacy"), ("SC-LAB", "Laboratory"), ("SC-RADIOLOGY", "Radiology"),
            ("SC-PHYSIO", "Physiotherapy"), ("SC-DIETARY", "Dietary"), ("SC-BLOODBANK", "Blood Bank"),
            ("SC-DIALYSIS", "Dialysis"), ("SC-DAYCARE", "Day Care"), ("SC-PACKAGE", "Package"),
            ("SC-EMERGENCY", "Emergency"), ("SC-ANAESTHESIA", "Anaesthesia"), ("SC-IMPLANT", "Implant"),
            ("SC-CONSUMABLE", "Consumables"), ("SC-OXYGEN", "Medical Gases"), ("SC-HOMECARE", "Home Care"),
            ("SC-TELEMED", "Telemedicine"), ("SC-MHC", "Health Checkup"), ("SC-VACCINE", "Vaccination"),
            ("SC-DENTAL", "Dental"), ("SC-COUNSELLING", "Counselling"),
        ], (c, n) => new ServiceCategory { Code = c, Name = n }, ct);

        {
            var existingVendors = await db.Vendors.IgnoreQueryFilters()
                .Where(v => v.BranchId == ctx.BranchId).Select(v => v.Code).ToListAsync(ct);
            (string Code, string Name, string Contact, string City)[] vendors =
            [
                ("VEN-PH-01", "Sundar Pharma Distributors", "R. Sundararajan", "Chennai"),
                ("VEN-PH-02", "Deccan Medical Agencies", "K. Prakash", "Hyderabad"),
                ("VEN-PH-03", "Kaveri Drug House", "M. Lakshmi", "Trichy"),
                ("VEN-PH-04", "Gokul Pharma Traders", "S. Gokulnath", "Coimbatore"),
                ("VEN-PH-05", "Meridian Healthcare Supplies", "A. Farhan", "Bengaluru"),
                ("VEN-SU-01", "Surgicare Disposables", "V. Ravichandran", "Chennai"),
                ("VEN-SU-02", "Sterimed Surgicals", "P. Nandhini", "Madurai"),
                ("VEN-SU-03", "OrthoLine Implants", "J. Mathew", "Kochi"),
                ("VEN-LB-01", "Precision Lab Reagents", "D. Suganya", "Chennai"),
                ("VEN-LB-02", "BioServe Diagnostics Supply", "N. Arvind", "Pune"),
                ("VEN-RD-01", "Imaging Consumables India", "T. Balaji", "Chennai"),
                ("VEN-GS-01", "Chola Stationery Mart", "G. Ramesh", "Chennai"),
                ("VEN-GS-02", "Bharat Linen Suppliers", "H. Devika", "Erode"),
                ("VEN-GS-03", "CleanPro Housekeeping Supplies", "L. Senthil", "Chennai"),
                ("VEN-BM-01", "MedTech Biomedical Spares", "U. Kannan", "Bengaluru"),
                ("VEN-BM-02", "Siemens Service Partner — South", "C. Vignesh", "Chennai"),
                ("VEN-OX-01", "Southern Oxygen & Gases", "B. Murugan", "Tiruppur"),
                ("VEN-FD-01", "Annapoorna Dietary Services", "R. Kalaivani", "Chennai"),
                ("VEN-IT-01", "Netlink IT Solutions", "S. Aravind", "Chennai"),
                ("VEN-WS-01", "GreenEarth Biomedical Waste", "K. Jayanthi", "Chennai"),
            ];
            foreach (var (code, name, contact, city) in vendors.Where(v => !existingVendors.Contains(v.Code)))
            {
                db.Vendors.Add(new Vendor
                {
                    BranchId = ctx.BranchId, Code = code, Name = name, ContactPerson = contact,
                    Phone = $"98{_random.Next(10000000, 99999999)}",
                    Email = $"{code.ToLowerInvariant()}@example.com",
                    Address = $"{city}, India",
                    GstNumber = $"33{_random.Next(100000000, 999999999)}Z{_random.Next(1, 9)}",
                });
            }
            await db.SaveChangesAsync(ct);
        }

        {
            var existingCorporates = await db.Corporates.Select(c => c.Code).ToListAsync(ct);
            (string Code, string Name, decimal Limit)[] corporates =
            [
                ("CORP-TCS", "Tata Consultancy Services", 2500000m),
                ("CORP-INFY", "Infosys Limited", 2000000m),
                ("CORP-WIPRO", "Wipro Technologies", 1800000m),
                ("CORP-HCL", "HCL Technologies", 1500000m),
                ("CORP-COGNI", "Cognizant Technology Solutions", 2200000m),
                ("CORP-ASHOK", "Ashok Leyland", 1200000m),
                ("CORP-TVS", "TVS Motor Company", 1000000m),
                ("CORP-SAINT", "Saint-Gobain India", 900000m),
                ("CORP-HYUNDAI", "Hyundai Motor India", 1600000m),
                ("CORP-BHEL", "Bharat Heavy Electricals", 1100000m),
                ("CORP-SRAIL", "Southern Railway", 1400000m),
                ("CORP-TNEB", "TN Electricity Board", 800000m),
                ("CORP-INDBANK", "Indian Bank", 700000m),
                ("CORP-LIC", "Life Insurance Corporation", 650000m),
                ("CORP-POLICE", "TN Police Welfare Board", 600000m),
            ];
            foreach (var (code, name, limit) in corporates.Where(c => !existingCorporates.Contains(c.Code)))
            {
                db.Corporates.Add(new Corporate
                {
                    Code = code, Name = name, CreditLimit = limit,
                    ContactPerson = "HR Benefits Desk",
                    Phone = $"044{_random.Next(10000000, 99999999)}",
                    Email = $"benefits@{code.Replace("CORP-", "").ToLowerInvariant()}.example.com",
                });
            }
            await db.SaveChangesAsync(ct);
        }

        {
            var existingInsurers = await db.InsuranceCompanies.Select(c => c.Code).ToListAsync(ct);
            (string Code, string Name, bool IsTpa)[] insurers =
            [
                ("INS-STAR", "Star Health & Allied Insurance", false),
                ("INS-NIA", "New India Assurance", false),
                ("INS-UIIC", "United India Insurance", false),
                ("INS-ORIENTAL", "Oriental Insurance", false),
                ("INS-NATIONAL", "National Insurance", false),
                ("INS-HDFC", "HDFC ERGO Health", false),
                ("INS-ICICI", "ICICI Lombard", false),
                ("INS-BAJAJ", "Bajaj Allianz Health", false),
                ("INS-CARE", "Care Health Insurance", false),
                ("INS-NIVA", "Niva Bupa Health", false),
                ("INS-ADITYA", "Aditya Birla Health", false),
                ("INS-TATA", "Tata AIG Health", false),
                ("TPA-MDINDIA", "MDIndia Health Insurance TPA", true),
                ("TPA-PARAMOUNT", "Paramount Health Services TPA", true),
                ("TPA-VIDAL", "Vidal Health TPA", true),
                ("TPA-MEDIASSIST", "Medi Assist TPA", true),
                ("TPA-HERITAGE", "Heritage Health TPA", true),
                ("TPA-FHPL", "Family Health Plan TPA", true),
                ("TPA-RAKSHA", "Raksha Health TPA", true),
                ("SCHEME-CMCHIS", "CM Comprehensive Health Insurance Scheme", true),
                ("SCHEME-ABPMJAY", "Ayushman Bharat PM-JAY", true),
                ("SCHEME-ECHS", "Ex-Servicemen Contributory Health Scheme", true),
                ("SCHEME-CGHS", "Central Government Health Scheme", true),
                ("SCHEME-ESI", "Employees State Insurance", true),
            ];
            foreach (var (code, name, isTpa) in insurers.Where(i => !existingInsurers.Contains(i.Code)))
            {
                db.InsuranceCompanies.Add(new InsuranceCompany
                {
                    Code = code, Name = name, IsTpa = isTpa,
                    ContactPerson = isTpa ? "Claims Desk" : "Provider Relations",
                    Phone = $"1800{_random.Next(100000, 999999)}",
                    Email = $"claims@{code.Split('-')[1].ToLowerInvariant()}.example.com",
                });
            }
            await db.SaveChangesAsync(ct);
        }
    }

    // ── Billing-adjacent masters ─────────────────────────────────────────────
    private async Task SeedBillingMastersAsync(DemoContext ctx, CancellationToken ct)
    {
        {
            var existingDiscounts = await db.DiscountTypes.Select(t => t.Code).ToListAsync(ct);
            (string Code, string Name, decimal Max, decimal Threshold)[] discounts =
            [
                ("STAFF", "Hospital Staff Concession", 50m, 20m),
                ("CAMP", "Camp Concession", 25m, 10m),
                ("SENIOR", "Senior Citizen Concession", 15m, 15m),
                ("CHARITY", "Charitable Trust Concession", 100m, 25m),
                ("CORPORATE", "Corporate Tie-up Discount", 30m, 15m),
                ("STAFF_DEP", "Staff Dependent Concession", 30m, 15m),
                ("BPL", "Below Poverty Line Concession", 100m, 30m),
                ("SCHEME", "Government Scheme Concession", 100m, 30m),
                ("MANAGEMENT", "Management Approved Discount", 100m, 10m),
                ("LOYALTY", "Loyalty Card Discount", 20m, 20m),
                ("INSURANCE", "Insurance Negotiated Rate", 40m, 20m),
                ("PACKAGE", "Package Rate Adjustment", 35m, 20m),
                ("PROMO", "Promotional Offer", 20m, 20m),
                ("GOODWILL", "Service Recovery / Goodwill", 25m, 5m),
                ("DOCTOR_REF", "Referring Doctor Courtesy", 20m, 10m),
                ("CLERGY", "Clergy / Religious Concession", 40m, 20m),
                ("EX_SERVICEMAN", "Ex-Serviceman Concession", 25m, 15m),
                ("DISABILITY", "Differently-Abled Concession", 30m, 15m),
            ];
            foreach (var (code, name, max, threshold) in discounts.Where(x => !existingDiscounts.Contains(x.Code)))
            {
                db.DiscountTypes.Add(new DiscountType
                {
                    Code = code, Name = name, MaxPercent = max, ApprovalThresholdPercent = threshold,
                });
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.RoomTypeCharges.IgnoreQueryFilters().AnyAsync(ct))
        {
            var effectiveFrom = new DateOnly(DateTime.UtcNow.Year, 1, 1);
            var rate = 1500m;
            foreach (var roomType in await db.RoomTypes.OrderBy(r => r.Id).ToListAsync(ct))
            {
                db.RoomTypeCharges.Add(new RoomTypeCharge
                {
                    BranchId = ctx.BranchId, RoomTypeId = roomType.Id, Rate = rate, EffectiveFrom = effectiveFrom,
                });
                rate += 750m;
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.DoctorPayoutRules.IgnoreQueryFilters().AnyAsync(ct))
        {
            var categoryIds = await db.ServiceCategories.OrderBy(c => c.Id).Select(c => c.Id).Take(4).ToListAsync(ct);
            var doctorIds = await db.Doctors.OrderBy(d => d.Id).Select(d => d.Id).Take(8).ToListAsync(ct);
            foreach (var doctorId in doctorIds)
            {
                foreach (var categoryId in categoryIds)
                {
                    db.DoctorPayoutRules.Add(new DoctorPayoutRule
                    {
                        BranchId = ctx.BranchId, DoctorId = doctorId, ServiceCategoryId = categoryId,
                        PayoutType = PayoutType.Percent, Value = _random.Next(10, 41),
                    });
                }
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.BillEstimateTemplates.IgnoreQueryFilters().AnyAsync(ct))
        {
            var services = await db.Services.OrderBy(s => s.Id).Take(8).ToListAsync(ct);
            if (services.Count > 0)
            {
                (string Code, string Name)[] templates =
                [
                    ("EST-NORMAL-DEL", "Normal Delivery Package Estimate"),
                    ("EST-LSCS", "LSCS Package Estimate"),
                    ("EST-APPENDIX", "Appendectomy Estimate"),
                    ("EST-ANGIO", "Coronary Angiogram Estimate"),
                ];
                foreach (var (code, name) in templates)
                {
                    var template = new BillEstimateTemplate { BranchId = ctx.BranchId, Code = code, Name = name };
                    foreach (var service in services.OrderBy(_ => _random.Next()).Take(4))
                    {
                        template.Items.Add(new BillEstimateTemplateItem
                        {
                            SourceType = BillItemSourceType.Service, ServiceId = service.Id, Quantity = _random.Next(1, 4),
                        });
                    }
                    db.BillEstimateTemplates.Add(template);
                }
                await db.SaveChangesAsync(ct);
            }
        }

        if (!await db.Racks.IgnoreQueryFilters().AnyAsync(ct))
        {
            foreach (var store in await db.Stores.IgnoreQueryFilters().OrderBy(s => s.Id).ToListAsync(ct))
            {
                foreach (var suffix in new[] { "A1", "A2", "B1", "B2", "COLD" })
                {
                    db.Racks.Add(new Rack
                    {
                        BranchId = store.BranchId, StoreId = store.Id,
                        Code = $"{store.Code}-{suffix}",
                        Name = suffix == "COLD" ? "Cold Storage" : $"Rack {suffix}",
                    });
                }
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Equipment.IgnoreQueryFilters().AnyAsync(ct))
        {
            var theatres = await db.Theatres.IgnoreQueryFilters().OrderBy(t => t.Id).ToListAsync(ct);
            (string Code, string Name)[] equipment =
            [
                ("OT-EQ-ANAES", "Anaesthesia Workstation"), ("OT-EQ-CAUTERY", "Electrosurgical Cautery Unit"),
                ("OT-EQ-CARM", "C-Arm Image Intensifier"), ("OT-EQ-MONITOR", "Multi-para Monitor"),
                ("OT-EQ-LAMP", "Ceiling OT Lamp"), ("OT-EQ-SUCTION", "Suction Apparatus"),
            ];
            for (var i = 0; i < equipment.Length; i++)
            {
                var theatre = theatres.Count > 0 ? theatres[i % theatres.Count] : null;
                db.Equipment.Add(new Domain.Entities.Ot.Equipment
                {
                    BranchId = theatre?.BranchId ?? ctx.BranchId, TheatreId = theatre?.Id,
                    Code = $"{equipment[i].Code}-{i + 1}", Name = equipment[i].Name,
                });
            }
            await db.SaveChangesAsync(ct);
        }
    }

    // ── Loyalty ──────────────────────────────────────────────────────────────
    private async Task SeedLoyaltyAsync(DemoContext ctx, CancellationToken ct)
    {
        await EnsureAsync(db.LoyaltyCardCategories,
        [
            ("INDIVIDUAL", "Individual"), ("FAMILY", "Family"), ("CORPORATE", "Corporate"),
            ("SENIOR", "Senior Citizen"), ("MATERNITY", "Maternity"), ("CHRONIC", "Chronic Care"),
            ("STAFF", "Staff"), ("STUDENT", "Student"), ("NRI", "NRI"), ("PREMIUM", "Premium"),
        ],
            (c, n) => new LoyaltyCardCategory { Code = c, Name = n }, ct);

        await EnsureAsync(db.MemberRelationTags,
        [
            ("SELF", "Self"), ("SPOUSE", "Spouse"), ("SON", "Son"), ("DAUGHTER", "Daughter"),
            ("FATHER", "Father"), ("MOTHER", "Mother"), ("BROTHER", "Brother"), ("SISTER", "Sister"),
            ("GRANDFATHER", "Grandfather"), ("GRANDMOTHER", "Grandmother"),
            ("FATHER_IN_LAW", "Father-in-law"), ("MOTHER_IN_LAW", "Mother-in-law"),
            ("GUARDIAN", "Guardian"), ("WARD", "Ward / Dependent"), ("OTHER", "Other"),
        ],
            (c, n) => new MemberRelationTag { Code = c, Name = n }, ct);

        {
            var categories = await db.LoyaltyCardCategories.ToDictionaryAsync(c => c.Code, ct);
            var existingCardTypes = await db.LoyaltyCardTypes.Select(t => t.Code).ToListAsync(ct);
            (string Code, string Name, int Limit, string Category)[] types =
            [
                ("SILVER", "Silver Card", 1, "INDIVIDUAL"),
                ("GOLD", "Gold Family Card", 4, "FAMILY"),
                ("PLATINUM", "Platinum Family Card", 6, "FAMILY"),
                ("CORP", "Corporate Employee Card", 3, "CORPORATE"),
                ("SENIOR", "Senior Citizen Card", 2, "SENIOR"),
                ("TITANIUM", "Titanium Family Card", 8, "PREMIUM"),
                ("MATERNITY", "Maternity Care Card", 2, "MATERNITY"),
                ("DIABETES", "Diabetes Care Card", 2, "CHRONIC"),
                ("CARDIAC", "Cardiac Care Card", 2, "CHRONIC"),
                ("STAFF_CARD", "Staff Privilege Card", 4, "STAFF"),
                ("STUDENT", "Student Care Card", 1, "STUDENT"),
                ("NRI_CARD", "NRI Privilege Card", 5, "NRI"),
            ];
            foreach (var (code, name, limit, category) in types.Where(t => !existingCardTypes.Contains(t.Code)))
            {
                db.LoyaltyCardTypes.Add(new LoyaltyCardType
                {
                    Code = code, Name = name, MemberLimit = limit,
                    LoyaltyCardCategoryId = categories.TryGetValue(category, out var c) ? c.Id : null,
                });
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.LoyaltyCardPrices.IgnoreQueryFilters().AnyAsync(ct))
        {
            var effectiveFrom = new DateOnly(DateTime.UtcNow.Year, 1, 1);
            var basePrice = 2000m;
            foreach (var type in await db.LoyaltyCardTypes.OrderBy(t => t.Id).ToListAsync(ct))
            {
                var price = new LoyaltyCardPrice
                {
                    BranchId = ctx.BranchId, LoyaltyCardTypeId = type.Id, EffectiveFrom = effectiveFrom,
                };
                foreach (var months in new[] { 12, 24, 36 })
                {
                    // Longer commitments get a progressively better per-year rate.
                    price.Items.Add(new LoyaltyCardPriceItem
                    {
                        DurationMonths = months,
                        Amount = Math.Round(basePrice * (months / 12m) * (months == 12 ? 1m : months == 24 ? 0.9m : 0.8m), 2),
                    });
                }
                db.LoyaltyCardPrices.Add(price);
                basePrice += 1500m;
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.LoyaltyCards.IgnoreQueryFilters().AnyAsync(ct))
        {
            var types = await db.LoyaltyCardTypes.OrderBy(t => t.Id).ToListAsync(ct);
            var relationTags = await db.MemberRelationTags.OrderBy(t => t.Id).ToListAsync(ct);
            var patients = await db.Patients.IgnoreQueryFilters().OrderBy(p => p.Id).Take(60).ToListAsync(ct);
            if (types.Count > 0 && patients.Count >= 30)
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var cardHolders = patients.Take(18).ToList();
                var dependentPool = patients.Skip(18).ToList();
                var dependentCursor = 0;

                for (var i = 0; i < cardHolders.Count; i++)
                {
                    var type = types[i % types.Count];
                    var issued = today.AddDays(-_random.Next(30, 600));
                    var duration = new[] { 12, 24, 36 }[i % 3];
                    // A couple of older cards are deactivated so the status filter has something to show.
                    var deactivated = i % 9 == 8;
                    var card = new LoyaltyCard
                    {
                        BranchId = ctx.BranchId,
                        CardNumber = $"LOY-{issued.Year}-{(i + 1).ToString().PadLeft(5, '0')}",
                        PatientId = cardHolders[i].Id,
                        LoyaltyCardTypeId = type.Id,
                        IssuedDate = issued,
                        DurationMonths = duration,
                        ExpiryDate = issued.AddMonths(duration),
                        Amount = 2000m + (i % 5 * 1500m),
                        Status = deactivated ? LoyaltyCardStatus.Deactivated : LoyaltyCardStatus.Active,
                        DeactivateReason = deactivated ? "Member relocated out of station" : null,
                        DeactivatedAt = deactivated ? DateTimeOffset.UtcNow.AddDays(-_random.Next(5, 90)) : null,
                        DuplicateIssueCount = i % 7 == 6 ? 1 : 0,
                    };
                    db.LoyaltyCards.Add(card);
                    await db.SaveChangesAsync(ct);

                    // Fill the card up to (but never past) its type's member limit.
                    var dependents = Math.Min(type.MemberLimit, 3);
                    for (var d = 0; d < dependents && dependentCursor < dependentPool.Count; d++, dependentCursor++)
                    {
                        db.LoyaltyCardMembers.Add(new LoyaltyCardMember
                        {
                            LoyaltyCardId = card.Id,
                            PatientId = dependentPool[dependentCursor].Id,
                            MemberRelationTagId = relationTags[(i + d) % relationTags.Count].Id,
                            AddedAt = new DateTimeOffset(issued.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                        });
                    }
                    await db.SaveChangesAsync(ct);
                }
            }
        }
    }

    // ── Insurance / TPA ──────────────────────────────────────────────────────
    private async Task SeedInsuranceAsync(DemoContext ctx, CancellationToken ct)
    {
        await EnsureAsync(db.ClaimDocumentTypes,
        [
            ("PREAUTH_FORM", "Pre-Authorization Form"), ("DISCHARGE_SUMMARY", "Discharge Summary"),
            ("BILL_COPY", "Final Bill Copy"), ("POLICY_COPY", "Policy Copy"),
            ("ID_PROOF", "Patient ID Proof"), ("INVESTIGATION", "Investigation Reports"),
        ], (c, n) => new ClaimDocumentType { Code = c, Name = n }, ct);

        await EnsureAsync(db.ClaimDenialReasons,
        [
            ("PRE_EXISTING", "Pre-existing condition exclusion"), ("NON_PAYABLE", "Non-payable items claimed"),
            ("DOC_INCOMPLETE", "Documentation incomplete"), ("POLICY_LAPSED", "Policy lapsed on date of service"),
            ("LIMIT_EXHAUSTED", "Sum insured exhausted"), ("WAITING_PERIOD", "Within policy waiting period"),
            ("NO_PREAUTH", "Pre-authorisation not obtained"), ("LATE_INTIMATION", "Late intimation to insurer"),
            ("NOT_COVERED", "Treatment not covered under policy"), ("COSMETIC", "Cosmetic / elective procedure"),
            ("SELF_INFLICTED", "Self-inflicted injury"), ("SUBSTANCE_ABUSE", "Substance abuse related"),
            ("DUPLICATE", "Duplicate claim"), ("TARIFF_MISMATCH", "Tariff / package rate mismatch"),
            ("ROOM_RENT_CAP", "Room rent above eligible category"),
            ("DISCHARGE_MISSING", "Discharge summary not submitted"),
            ("SIGNATURE_MISSING", "Treating doctor signature missing"),
            ("POLICY_NOT_ACTIVE", "Policy not active on admission date"),
        ], (c, n) => new ClaimDenialReason { Code = c, Name = n }, ct);

        if (await db.PatientInsurancePolicies.AnyAsync(ct))
        {
            return;
        }

        var companies = await db.InsuranceCompanies.OrderBy(c => c.Id).ToListAsync(ct);
        var admissions = await db.Admissions.IgnoreQueryFilters()
            .OrderByDescending(a => a.Id).Take(40).ToListAsync(ct);
        if (companies.Count == 0 || admissions.Count == 0)
        {
            logger.LogInformation("Insurance demo seed skipped — needs insurance companies and admissions.");
            return;
        }

        // One policy per admitted patient, so pre-auths and claims have something real to hang off.
        var policies = new List<PatientInsurancePolicy>();
        var seenPatients = new HashSet<long>();
        foreach (var admission in admissions.Where(a => seenPatients.Add(a.PatientId)))
        {
            var company = companies[_random.Next(companies.Count)];
            var validFrom = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-_random.Next(2, 18));
            var policy = new PatientInsurancePolicy
            {
                PatientId = admission.PatientId,
                InsuranceCompanyId = company.Id,
                PolicyNumber = $"POL/{company.Code}/{_random.Next(100000, 999999)}",
                PolicyValidFrom = validFrom,
                PolicyValidTo = validFrom.AddYears(1),
                IsPrimary = true,
            };
            policies.Add(policy);
            db.PatientInsurancePolicies.Add(policy);
        }
        await db.SaveChangesAsync(ct);

        var policyByPatient = policies.ToDictionary(p => p.PatientId);
        var billsByAdmission = await db.Bills.IgnoreQueryFilters()
            .Where(b => b.AdmissionId != null && b.Status != BillStatus.Cancelled)
            .GroupBy(b => b.AdmissionId!.Value)
            .Select(g => new { AdmissionId = g.Key, BillId = g.Max(b => b.Id) })
            .ToDictionaryAsync(x => x.AdmissionId, x => x.BillId, ct);

        var preAuths = new List<PreAuthRequest>();
        var index = 0;
        foreach (var admission in admissions)
        {
            if (!policyByPatient.TryGetValue(admission.PatientId, out var policy))
            {
                continue;
            }
            index++;
            var requestedAt = DateTimeOffset.UtcNow.AddDays(-_random.Next(5, 180));
            var estimated = _random.Next(20, 220) * 1000m;

            // Spread across the whole status vocabulary so every filter/tab has rows.
            var status = (index % 5) switch
            {
                0 => PreAuthStatus.Requested,
                1 => PreAuthStatus.Approved,
                2 => PreAuthStatus.PartiallyApproved,
                3 => PreAuthStatus.Rejected,
                _ => PreAuthStatus.Approved,
            };
            var preAuth = new PreAuthRequest
            {
                BranchId = admission.BranchId,
                PreAuthNumber = $"PA-{requestedAt.Year}-{index.ToString().PadLeft(5, '0')}",
                PatientId = admission.PatientId,
                PolicyId = policy.Id,
                TreatmentType = PreAuthTreatmentType.Ip,
                AdmissionId = admission.Id,
                EstimatedAmount = estimated,
                Diagnosis = DemoDiagnoses[index % DemoDiagnoses.Length],
                RequestedByUserId = ctx.UserId,
                RequestedAt = requestedAt,
                Status = status,
                ApprovedAmount = status switch
                {
                    PreAuthStatus.Approved => estimated,
                    PreAuthStatus.PartiallyApproved => Math.Round(estimated * 0.7m, 2),
                    _ => null,
                },
                DecidedByUserId = status == PreAuthStatus.Requested ? null : ctx.UserId,
                DecidedAt = status == PreAuthStatus.Requested ? null : requestedAt.AddHours(_random.Next(4, 72)),
                RejectionReason = status == PreAuthStatus.Rejected ? "Treatment falls within policy waiting period." : null,
            };
            preAuths.Add(preAuth);
            db.PreAuthRequests.Add(preAuth);
        }
        await db.SaveChangesAsync(ct);

        // Mid-treatment enhancements on a handful of approved pre-auths.
        foreach (var preAuth in preAuths.Where(p => p.Status is PreAuthStatus.Approved or PreAuthStatus.PartiallyApproved).Take(8))
        {
            var enhancementStatus = (int)(preAuth.Id % 3) switch
            {
                0 => PreAuthEnhancementStatus.Requested,
                1 => PreAuthEnhancementStatus.Approved,
                _ => PreAuthEnhancementStatus.Rejected,
            };
            var requestedAt = preAuth.RequestedAt.AddDays(_random.Next(1, 6));
            db.PreAuthEnhancements.Add(new PreAuthEnhancement
            {
                PreAuthRequestId = preAuth.Id,
                RequestedAmount = Math.Round(preAuth.EstimatedAmount * 0.35m, 2),
                Reason = "Extended ICU stay and additional investigations beyond the original estimate.",
                RequestedByUserId = ctx.UserId,
                RequestedAt = requestedAt,
                Status = enhancementStatus,
                DecidedByUserId = enhancementStatus == PreAuthEnhancementStatus.Requested ? null : ctx.UserId,
                DecidedAt = enhancementStatus == PreAuthEnhancementStatus.Requested ? null : requestedAt.AddHours(18),
                RejectionReason = enhancementStatus == PreAuthEnhancementStatus.Rejected
                    ? "Enhancement exceeds the balance sum insured." : null,
            });
        }
        await db.SaveChangesAsync(ct);

        var docTypes = await db.ClaimDocumentTypes.OrderBy(t => t.Id).ToListAsync(ct);
        var claimIndex = 0;
        foreach (var preAuth in preAuths.Where(p => p.Status != PreAuthStatus.Rejected))
        {
            if (preAuth.AdmissionId is not { } admissionId || !billsByAdmission.TryGetValue(admissionId, out var billId))
            {
                continue;
            }
            claimIndex++;
            var submittedAt = preAuth.RequestedAt.AddDays(_random.Next(3, 20));
            var claimAmount = preAuth.ApprovedAmount ?? preAuth.EstimatedAmount;

            var status = (claimIndex % 6) switch
            {
                0 => ClaimStatus.Submitted,
                1 => ClaimStatus.UnderReview,
                2 => ClaimStatus.Approved,
                3 => ClaimStatus.PartiallyApproved,
                4 => ClaimStatus.Denied,
                _ => ClaimStatus.Settled,
            };
            var approved = status switch
            {
                ClaimStatus.Approved or ClaimStatus.Settled => claimAmount,
                ClaimStatus.PartiallyApproved => Math.Round(claimAmount * 0.75m, 2),
                _ => (decimal?)null,
            };
            var claim = new Claim
            {
                BranchId = preAuth.BranchId,
                ClaimNumber = $"CLM-{submittedAt.Year}-{claimIndex.ToString().PadLeft(5, '0')}",
                PatientId = preAuth.PatientId,
                PolicyId = preAuth.PolicyId,
                PreAuthRequestId = preAuth.Id,
                BillId = billId,
                ClaimAmount = claimAmount,
                ApprovedAmount = approved,
                Status = status,
                SubmittedByUserId = ctx.UserId,
                SubmittedAt = submittedAt,
                DecidedByUserId = status is ClaimStatus.Submitted or ClaimStatus.UnderReview ? null : ctx.UserId,
                DecidedAt = status is ClaimStatus.Submitted or ClaimStatus.UnderReview
                    ? null : submittedAt.AddDays(_random.Next(3, 25)),
                DenialReasonId = status == ClaimStatus.Denied
                    ? await db.ClaimDenialReasons.OrderBy(r => r.Id).Select(r => (long?)r.Id).FirstOrDefaultAsync(ct)
                    : null,
                DenialNotes = status == ClaimStatus.Denied ? "Claim repudiated after document review." : null,
                SettledAmount = status == ClaimStatus.Settled ? approved : null,
                DeductionAmount = status == ClaimStatus.Settled ? Math.Round(claimAmount * 0.05m, 2) : null,
                SettlementReferenceNumber = status == ClaimStatus.Settled ? $"UTR{_random.Next(100000000, 999999999)}" : null,
                SettledAt = status == ClaimStatus.Settled ? submittedAt.AddDays(_random.Next(25, 60)) : null,
            };
            foreach (var docType in docTypes.Take(3))
            {
                claim.Documents.Add(new ClaimDocument
                {
                    DocumentTypeId = docType.Id,
                    FileName = $"{docType.Code.ToLowerInvariant()}.pdf",
                    // Demo rows reference a conventional path; no file is written to disk.
                    FilePath = $"demo/insurance/{claim.ClaimNumber}/{docType.Code.ToLowerInvariant()}.pdf",
                    ContentType = "application/pdf",
                    SizeBytes = _random.Next(40_000, 900_000),
                    UploadedByUserId = ctx.UserId,
                    UploadedAt = submittedAt,
                });
            }
            db.Claims.Add(claim);
        }
        await db.SaveChangesAsync(ct);
    }

    // ── Quality / NABH ───────────────────────────────────────────────────────
    private async Task SeedQualityAsync(DemoContext ctx, CancellationToken ct)
    {
        // Chapter-level NABH reference codes only. Deliberately NOT Objective-Element numbers:
        // those must come from the hospital's licensed standard, not be invented here.
        await EnsureAsync(db.QualityNabhStandards,
        [
            ("AAC", "Access, Assessment and Continuity of Care"),
            ("COP", "Care of Patients"),
            ("MOM", "Management of Medication"),
            ("PRE", "Patient Rights and Education"),
            ("HIC", "Hospital Infection Control"),
            ("PSQ", "Patient Safety and Quality Improvement"),
            ("ROM", "Responsibilities of Management"),
            ("FMS", "Facilities Management and Safety"),
            ("HRM", "Human Resource Management"),
            ("IMS", "Information Management System"),
        ], (c, n) => new QualityNabhStandard { Code = c, Name = n }, ct);

        await EnsureAsync(db.QualityCommittees,
        [
            ("QC", "Quality Assurance Committee"), ("IC", "Infection Control Committee"),
            ("PSC", "Patient Safety Committee"), ("PTC", "Pharmacy & Therapeutics Committee"),
            ("MRC", "Medical Records Committee"), ("SAFETY", "Safety Committee"),
            ("ETHICS", "Ethics Committee"), ("CPR", "Code Blue / CPR Committee"),
            ("BLOOD", "Transfusion Committee"), ("DISASTER", "Disaster Management Committee"),
            ("GRIEVANCE", "Patient Grievance Redressal Committee"),
            ("ANTIBIOTIC", "Antimicrobial Stewardship Committee"),
            ("BMW", "Biomedical Waste Management Committee"),
            ("RADIATION", "Radiation Safety Committee"),
            ("CREDENTIAL", "Credentialing & Privileging Committee"),
            ("MORTALITY", "Mortality Review Committee"),
            ("NURSING", "Nursing Quality Committee"),
            ("ICSC", "Internal Complaints Committee (POSH)"),
        ], (c, n) => new QualityCommittee { Code = c, Name = n }, ct);

        var standards = await db.QualityNabhStandards.OrderBy(s => s.Id).ToListAsync(ct);
        var departments = await db.Departments.OrderBy(d => d.Id).Take(10).ToListAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (await db.QualityAuditTemplates.CountAsync(ct) < 8)
        {
            (string Code, string Name, string Category, string[] Questions)[] templates =
            [
                ("AUD-HH", "Hand Hygiene Compliance Audit", "Infection Control",
                [
                    "Are hand-rub dispensers available at every point of care?",
                    "Do staff perform hand hygiene before patient contact?",
                    "Do staff perform hand hygiene after body-fluid exposure risk?",
                    "Is hand-hygiene technique compliant with the 6-step method?",
                    "Are hand-hygiene posters displayed at all sinks?",
                ]),
                ("AUD-MR", "Medical Record Completeness Audit", "Medical Records",
                [
                    "Is the patient identified on every page of the record?",
                    "Is the initial assessment completed within the defined time?",
                    "Is informed consent present and signed for every procedure?",
                    "Are all entries dated, timed and authenticated?",
                    "Is the discharge summary complete and signed by the consultant?",
                ]),
                ("AUD-MED", "Medication Safety Audit", "Medication Safety",
                [
                    "Are high-alert medications stored separately and labelled?",
                    "Are LASA drugs physically segregated?",
                    "Is the medication administration record complete for the shift?",
                    "Are verbal orders countersigned within the defined time?",
                    "Are emergency-trolley drugs within expiry?",
                ]),
                ("AUD-BMW", "Biomedical Waste Segregation Audit", "Facility & Safety",
                [
                    "Are colour-coded bins available in every clinical area?",
                    "Is waste segregated correctly at the point of generation?",
                    "Are sharps containers below the three-quarter fill line?",
                    "Are BMW handlers wearing full PPE?",
                    "Is the BMW register up to date and signed?",
                ]),
                ("AUD-SSI", "Surgical Site Infection Prevention Audit", "Infection Control",
                [
                    "Is surgical antibiotic prophylaxis given within 60 minutes of incision?",
                    "Is hair removal done with clippers rather than a razor?",
                    "Is normothermia maintained through the procedure?",
                    "Is the surgical site marked and verified before incision?",
                    "Is the theatre air-handling log current?",
                ]),
                ("AUD-IDENT", "Patient Identification Audit", "Patient Safety",
                [
                    "Does every admitted patient wear an identification band?",
                    "Are two identifiers used before any procedure or medication?",
                    "Are identification bands legible and intact?",
                    "Are allergy bands applied where indicated?",
                    "Is identity re-verified before blood transfusion?",
                ]),
                ("AUD-FALL", "Fall Prevention Audit", "Patient Safety",
                [
                    "Is a fall-risk assessment completed on admission?",
                    "Is the assessment repeated after any change in condition?",
                    "Are bed rails and call bells within the patient's reach?",
                    "Are high-risk patients visually flagged?",
                    "Are floors dry and walkways clear of obstruction?",
                ]),
                ("AUD-CONSENT", "Informed Consent Audit", "Patient Rights",
                [
                    "Is consent obtained in a language the patient understands?",
                    "Does the consent name the specific procedure and its risks?",
                    "Is the consent signed by the performing clinician?",
                    "Is the time of consent recorded before the procedure?",
                    "Is a witness signature present where required?",
                ]),
                ("AUD-EQUIP", "Emergency Equipment Readiness Audit", "Facility & Safety",
                [
                    "Is the crash cart checked and signed off each shift?",
                    "Is the defibrillator tested and charged?",
                    "Are oxygen cylinders full and secured?",
                    "Are suction units functional at every bedside?",
                    "Is the emergency drug list complete and in date?",
                ]),
            ];
            var existingTemplates = await db.QualityAuditTemplates.Select(t => t.Code).ToListAsync(ct);
            foreach (var (code, name, category, questions) in templates.Where(t => !existingTemplates.Contains(t.Code)))
            {
                var template = new QualityAuditTemplate { Code = code, Name = name, Category = category };
                for (var i = 0; i < questions.Length; i++)
                {
                    template.Items.Add(new QualityAuditTemplateItem
                    {
                        SortOrder = i + 1, Question = questions[i], MaxScore = 5,
                    });
                }
                db.QualityAuditTemplates.Add(template);
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.QualityAudits.IgnoreQueryFilters().AnyAsync(ct))
        {
            var templates = await db.QualityAuditTemplates.Include(t => t.Items).OrderBy(t => t.Id).ToListAsync(ct);
            var runIndex = 0;
            foreach (var template in templates)
            {
                // Six monthly runs each: the older ones completed, the newest still scheduled.
                for (var monthsAgo = 5; monthsAgo >= 0; monthsAgo--)
                {
                    runIndex++;
                    var scheduled = today.AddMonths(-monthsAgo).AddDays(-today.Day + 10);
                    var completed = monthsAgo > 0;
                    var audit = new QualityAudit
                    {
                        BranchId = ctx.BranchId,
                        TemplateId = template.Id,
                        ScheduledDate = scheduled,
                        ConductedDate = completed ? scheduled.AddDays(_random.Next(0, 4)) : null,
                        Status = completed ? QualityAuditStatus.Completed : QualityAuditStatus.Scheduled,
                        ConductedByUserId = completed ? ctx.UserId : null,
                        Remarks = completed ? "Audit completed; deviations logged as non-conformances." : null,
                    };
                    if (completed)
                    {
                        foreach (var item in template.Items)
                        {
                            audit.Responses.Add(new QualityAuditResponse
                            {
                                TemplateItemId = item.Id,
                                // Trend upward over time so the KPI dashboard shows improvement.
                                Score = Math.Min(5, 3 + (5 - monthsAgo) * 0.3m + _random.Next(0, 2)),
                                Remarks = _random.Next(0, 4) == 0 ? "Minor deviation observed; staff re-briefed." : null,
                            });
                        }
                    }
                    db.QualityAudits.Add(audit);
                }
            }
            await db.SaveChangesAsync(ct);
        }

        if (await db.QualityNonConformances.IgnoreQueryFilters().CountAsync(ct) < 20)
        {
            (string Title, string Description, NonConformanceSeverity Severity)[] findings =
            [
                ("Hand-rub dispenser empty in Ward 3", "Two of six dispensers found empty during the monthly infection-control round.", NonConformanceSeverity.Minor),
                ("Discharge summary unsigned beyond 24 hours", "Four discharge summaries sampled were not authenticated by the consultant within the defined time.", NonConformanceSeverity.Major),
                ("High-alert drugs not segregated in ICU", "Concentrated potassium chloride found stored with routine IV fluids.", NonConformanceSeverity.Major),
                ("BMW sharps container overfilled", "Sharps container in the minor OT exceeded the three-quarter fill line.", NonConformanceSeverity.Minor),
                ("Crash-cart checklist not signed for two shifts", "Night-shift verification signatures missing on the emergency trolley checklist.", NonConformanceSeverity.Minor),
                ("Fire-exit route obstructed", "Linen trolleys parked across the second-floor fire exit corridor.", NonConformanceSeverity.Major),
                ("Calibration overdue on infusion pumps", "Three infusion pumps in use past their calibration due date.", NonConformanceSeverity.Major),
                ("Patient identification band missing", "One inpatient found without an identification band during the ward round.", NonConformanceSeverity.Major),
                ("Expired reagents in the laboratory", "Two biochemistry reagent packs past expiry were still on the analyser bench.", NonConformanceSeverity.Major),
                ("Consent form without procedure name", "Sampled consent recorded only the word surgery rather than the specific procedure.", NonConformanceSeverity.Major),
                ("Blood-bank refrigerator temperature not logged", "Temperature chart had no entries for a 12-hour period.", NonConformanceSeverity.Major),
                ("Verbal orders not countersigned", "Six verbal medication orders remained uncountersigned beyond 24 hours.", NonConformanceSeverity.Major),
                ("Oxygen cylinders unsecured in the corridor", "Three cylinders stored upright but unchained outside the ICU.", NonConformanceSeverity.Major),
                ("Radiation warning signage missing", "Warning light at the CT room entrance was non-functional.", NonConformanceSeverity.Major),
                ("Nurse-to-patient ratio below policy in night shift", "ICU staffing fell below the defined ratio on four nights in the month.", NonConformanceSeverity.Major),
                ("Linen stored with cleaning chemicals", "Clean linen shelf shared with housekeeping chemical stock.", NonConformanceSeverity.Minor),
                ("Pest-control record not maintained", "Kitchen pest-control log had no entry for the preceding quarter.", NonConformanceSeverity.Minor),
                ("Staff immunisation records incomplete", "Hepatitis-B vaccination status unrecorded for nine clinical staff.", NonConformanceSeverity.Minor),
                ("Emergency drug expired in the crash cart", "One adrenaline ampoule found two weeks past expiry.", NonConformanceSeverity.Major),
                ("Restraint use without documented review", "Physical restraint continued without the four-hourly medical review entry.", NonConformanceSeverity.Major),
                ("Biomedical waste handed over without weighing", "Waste transferred to the contractor without recording weight for three days.", NonConformanceSeverity.Minor),
                ("Incomplete initial nursing assessment", "Nutritional and pain screening left blank in eight sampled records.", NonConformanceSeverity.Minor),
            ];
            // Keyed on Title: the guard above is a count, so without this a re-run would file the
            // same findings a second time.
            var existingNcTitles = await db.QualityNonConformances.IgnoreQueryFilters()
                .Select(n => n.Title).ToListAsync(ct);
            for (var i = 0; i < findings.Length; i++)
            {
                var (title, description, severity) = findings[i];
                if (existingNcTitles.Contains(title))
                {
                    continue;
                }
                var status = (i % 3) switch
                {
                    0 => NonConformanceStatus.Open,
                    1 => NonConformanceStatus.InProgress,
                    _ => NonConformanceStatus.Closed,
                };
                db.QualityNonConformances.Add(new QualityNonConformance
                {
                    BranchId = ctx.BranchId,
                    StandardId = standards.Count > 0 ? standards[i % standards.Count].Id : null,
                    DepartmentId = departments.Count > 0 ? departments[i % departments.Count].Id : null,
                    Title = title,
                    Description = description,
                    Severity = severity,
                    RaisedByUserId = ctx.UserId,
                    Status = status,
                    ClosedAt = status == NonConformanceStatus.Closed ? DateTimeOffset.UtcNow.AddDays(-_random.Next(3, 60)) : null,
                    ClosedByUserId = status == NonConformanceStatus.Closed ? ctx.UserId : null,
                });
            }
            await db.SaveChangesAsync(ct);
        }

        if (await db.QualityRisks.IgnoreQueryFilters().CountAsync(ct) < 20)
        {
            (string Title, string Category, string Description, int Likelihood, int Impact)[] risks =
            [
                ("Medication error during high-alert drug administration", "Clinical", "Risk of wrong-dose administration of anticoagulants and insulin.", 3, 5),
                ("Patient fall in high-dependency areas", "Clinical", "Elderly and sedated patients at risk of fall during transfer.", 4, 4),
                ("Power failure affecting critical care areas", "Facility", "Generator changeover delay could interrupt ventilator supply.", 2, 5),
                ("Fire hazard in medical-records storage", "Facility", "Dense paper storage with limited suppression coverage.", 2, 5),
                ("Needle-stick injury to nursing staff", "Occupational", "Sharps handling during high-volume phlebotomy rounds.", 4, 3),
                ("Data breach of patient health information", "Information", "Shared workstations left logged in at nursing stations.", 3, 5),
                ("Surgical site infection following clean surgery", "Clinical", "Lapses in pre-operative antibiotic timing.", 3, 4),
                ("Critical lab value not communicated in time", "Clinical", "Delay in reaching the treating clinician after hours.", 3, 5),
                ("Wrong-site or wrong-patient surgery", "Clinical", "Site marking and time-out steps skipped under theatre time pressure.", 2, 5),
                ("Hospital-acquired bloodstream infection", "Clinical", "Central-line maintenance bundle not consistently applied.", 3, 5),
                ("Ventilator-associated pneumonia", "Clinical", "Head-of-bed elevation and oral care not consistently charted.", 3, 4),
                ("Transfusion reaction from mismatched blood", "Clinical", "Bedside identity re-verification not independently double-checked.", 2, 5),
                ("Maternal haemorrhage in obstetric emergency", "Clinical", "Blood availability delay during out-of-hours emergencies.", 2, 5),
                ("Neonatal abduction from the nursery", "Security", "Unrestricted visitor access to the postnatal ward.", 1, 5),
                ("Medical gas supply interruption", "Facility", "Single manifold with no automatic changeover alarm.", 2, 5),
                ("Lift entrapment during patient transfer", "Facility", "Ageing lift with intermittent door-sensor faults.", 3, 3),
                ("Water contamination in the dialysis unit", "Facility", "RO plant disinfection schedule not consistently logged.", 2, 5),
                ("Ransomware disabling the hospital information system", "Information", "Unpatched endpoints and shared administrative credentials.", 3, 5),
                ("Loss of backup for clinical records", "Information", "Offsite backup restore has not been test-verified this year.", 2, 5),
                ("Violence against staff in the emergency department", "Occupational", "No controlled entry or panic alarm at triage.", 3, 4),
                ("Chemotherapy spill exposure", "Occupational", "Spill kits not available at every preparation point.", 2, 4),
                ("Radiation overexposure to imaging staff", "Occupational", "Dosimeter badge returns inconsistent across the quarter.", 2, 4),
                ("Stock-out of a critical emergency drug", "Supply", "Single-source supplier with no defined reorder buffer.", 3, 4),
                ("Counterfeit or substandard drug entering stock", "Supply", "Incoming goods inspection not documented for every GRN.", 2, 5),
                ("Cold-chain break for vaccines", "Supply", "Refrigerator temperature excursions not alarmed after hours.", 3, 4),
                ("Billing fraud or revenue leakage", "Financial", "Manual bill adjustments possible without secondary approval.", 3, 4),
                ("Insurance claim denial backlog", "Financial", "Documentation gaps identified only after claim submission.", 4, 3),
                ("Fire in the oxygen manifold room", "Facility", "Hot works permitted near the manifold without a fire watch.", 1, 5),
                ("Mass-casualty incident exceeding surge capacity", "Facility", "Disaster plan not rehearsed with a live drill this year.", 2, 5),
            ];
            // Keyed on Title for the same reason as the non-conformances above.
            var existingRiskTitles = await db.QualityRisks.IgnoreQueryFilters()
                .Select(r => r.Title).ToListAsync(ct);
            for (var i = 0; i < risks.Length; i++)
            {
                var (title, category, description, likelihood, impact) = risks[i];
                if (existingRiskTitles.Contains(title))
                {
                    continue;
                }
                var status = (i % 3) switch
                {
                    0 => RiskStatus.Identified,
                    1 => RiskStatus.Mitigating,
                    _ => RiskStatus.Closed,
                };
                db.QualityRisks.Add(new QualityRisk
                {
                    BranchId = ctx.BranchId,
                    Title = title, Category = category, Description = description,
                    Likelihood = likelihood, Impact = impact,
                    MitigationPlan = "Documented SOP, targeted staff training, and monthly compliance audit.",
                    OwnerUserId = ctx.UserId,
                    Status = status,
                    IdentifiedDate = today.AddDays(-_random.Next(30, 400)),
                    ReviewDate = today.AddDays(_random.Next(15, 120)),
                    ClosedAt = status == RiskStatus.Closed ? DateTimeOffset.UtcNow.AddDays(-_random.Next(5, 45)) : null,
                });
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.QualityCapas.IgnoreQueryFilters().AnyAsync(ct))
        {
            // CAPA is polymorphic over Incident/NonConformance/Audit/Risk — seed a spread of all
            // four so the tracker's source filter is exercised.
            var ncIds = await db.QualityNonConformances.IgnoreQueryFilters().Select(n => n.Id).ToListAsync(ct);
            var auditIds = await db.QualityAudits.IgnoreQueryFilters().Select(a => a.Id).Take(6).ToListAsync(ct);
            var riskIds = await db.QualityRisks.IgnoreQueryFilters().Select(r => r.Id).ToListAsync(ct);
            var incidentIds = await db.Incidents.IgnoreQueryFilters().Select(i => i.Id).ToListAsync(ct);

            var sources = new List<(CapaSourceType Type, long Id)>();
            sources.AddRange(ncIds.Select(id => (CapaSourceType.NonConformance, id)));
            sources.AddRange(auditIds.Select(id => (CapaSourceType.Audit, id)));
            sources.AddRange(riskIds.Take(4).Select(id => (CapaSourceType.Risk, id)));
            sources.AddRange(incidentIds.Select(id => (CapaSourceType.Incident, id)));

            string[] plans =
            [
                "Re-train the department on the relevant SOP and verify competency within 30 days.",
                "Revise the checklist, re-issue the controlled document and brief all shifts.",
                "Install additional supplies at the point of care and add to the daily round checklist.",
                "Introduce a double-verification step and audit compliance weekly for two months.",
                "Escalate to the department head and add the item to the monthly quality dashboard.",
            ];
            for (var i = 0; i < sources.Count; i++)
            {
                var (type, sourceId) = sources[i];
                var status = (i % 4) switch
                {
                    0 => CapaStatus.Open,
                    1 => CapaStatus.InProgress,
                    2 => CapaStatus.Verified,
                    _ => CapaStatus.Closed,
                };
                // Every third CAPA is deliberately past due so the "overdue" dashboard tile is non-zero.
                var dueDate = i % 3 == 0 ? today.AddDays(-_random.Next(5, 40)) : today.AddDays(_random.Next(5, 60));
                db.QualityCapas.Add(new QualityCapa
                {
                    BranchId = ctx.BranchId,
                    SourceType = type,
                    SourceId = sourceId,
                    ActionPlan = plans[i % plans.Length],
                    OwnerUserId = ctx.UserId,
                    DueDate = dueDate,
                    Status = status,
                    CompletedAt = status is CapaStatus.Verified or CapaStatus.Closed
                        ? DateTimeOffset.UtcNow.AddDays(-_random.Next(2, 30)) : null,
                    VerifiedByUserId = status is CapaStatus.Verified or CapaStatus.Closed ? ctx.UserId : null,
                    VerifiedAt = status is CapaStatus.Verified or CapaStatus.Closed
                        ? DateTimeOffset.UtcNow.AddDays(-_random.Next(1, 10)) : null,
                    VerificationNotes = status is CapaStatus.Verified or CapaStatus.Closed
                        ? "Re-audit confirmed the corrective action is effective." : null,
                });
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.QualityCommitteeMeetings.IgnoreQueryFilters().AnyAsync(ct))
        {
            var committees = await db.QualityCommittees.OrderBy(c => c.Id).ToListAsync(ct);
            foreach (var committee in committees)
            {
                for (var quarter = 3; quarter >= 0; quarter--)
                {
                    var meeting = new QualityCommitteeMeeting
                    {
                        BranchId = ctx.BranchId,
                        CommitteeId = committee.Id,
                        MeetingDate = today.AddMonths(-quarter * 3),
                        AttendeesText = "Medical Superintendent, Quality Manager, Nursing Superintendent, Department Heads",
                        Minutes = $"Reviewed quality indicator trends for the period, discussed open non-conformances "
                                + $"and CAPA status for {committee.Name}. Agreed on the action items recorded below.",
                    };
                    meeting.ActionItems.Add(new QualityCommitteeActionItem
                    {
                        Description = "Circulate the revised SOP to all departments and confirm acknowledgement.",
                        OwnerUserId = ctx.UserId,
                        DueDate = today.AddMonths(-quarter * 3).AddDays(30),
                        IsCompleted = quarter > 0,
                        CompletedAt = quarter > 0 ? DateTimeOffset.UtcNow.AddDays(-quarter * 60) : null,
                    });
                    meeting.ActionItems.Add(new QualityCommitteeActionItem
                    {
                        Description = "Present the next quarter's indicator dashboard with benchmark comparison.",
                        OwnerUserId = ctx.UserId,
                        DueDate = today.AddMonths(-quarter * 3).AddDays(60),
                        IsCompleted = quarter > 1,
                        CompletedAt = quarter > 1 ? DateTimeOffset.UtcNow.AddDays(-quarter * 45) : null,
                    });
                    db.QualityCommitteeMeetings.Add(meeting);
                }
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.QualityEvidence.IgnoreQueryFilters().AnyAsync(ct))
        {
            foreach (var standard in standards)
            {
                foreach (var kind in new[] { "Policy", "SOP", "Training Record" })
                {
                    db.QualityEvidence.Add(new QualityEvidence
                    {
                        BranchId = ctx.BranchId,
                        StandardId = standard.Id,
                        Title = $"{standard.Code} — {kind}",
                        Description = $"{kind} evidence filed against the {standard.Name} chapter.",
                        FileName = $"{standard.Code.ToLowerInvariant()}-{kind.ToLowerInvariant().Replace(' ', '-')}.pdf",
                        // Demo rows reference a conventional path; no file is written to disk.
                        FilePath = $"demo/quality/{standard.Code}/{kind.ToLowerInvariant().Replace(' ', '-')}.pdf",
                        UploadedByUserId = ctx.UserId,
                        UploadedAt = DateTimeOffset.UtcNow.AddDays(-_random.Next(10, 300)),
                    });
                }
            }
            await db.SaveChangesAsync(ct);
        }

        // Twelve months of history for every indicator, so the KPI dashboard has real trends.
        if (await db.QualityIndicatorEntries.IgnoreQueryFilters().CountAsync(ct) < 10)
        {
            var indicators = await db.QualityIndicators.OrderBy(i => i.Id).ToListAsync(ct);
            var firstOfThisMonth = new DateOnly(today.Year, today.Month, 1);
            var existing = await db.QualityIndicatorEntries.IgnoreQueryFilters()
                .Select(e => new { e.IndicatorId, e.PeriodMonth, e.BranchId }).ToListAsync(ct);
            var existingKeys = existing
                .Select(e => (e.IndicatorId, e.PeriodMonth, e.BranchId)).ToHashSet();

            foreach (var indicator in indicators)
            {
                for (var monthsAgo = 11; monthsAgo >= 1; monthsAgo--)
                {
                    var period = firstOfThisMonth.AddMonths(-monthsAgo);
                    if (!existingKeys.Add((indicator.Id, period, ctx.BranchId)))
                    {
                        continue;
                    }
                    var (numerator, denominator) = DemoIndicatorValue(indicator.Unit, monthsAgo);
                    db.QualityIndicatorEntries.Add(new QualityIndicatorEntry
                    {
                        BranchId = ctx.BranchId,
                        IndicatorId = indicator.Id,
                        PeriodMonth = period,
                        Numerator = numerator,
                        Denominator = denominator,
                        IsAutoComputed = false,
                        Remarks = null,
                        EnteredByUserId = ctx.UserId,
                    });
                }
            }
            await db.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// Plausible numerator/denominator for a demo indicator month. Rates trend gently downward
    /// (i.e. improving) as <paramref name="monthsAgo"/> shrinks, so dashboard trend lines slope
    /// the right way instead of looking like noise.
    /// </summary>
    private (decimal Numerator, decimal Denominator) DemoIndicatorValue(string? unit, int monthsAgo)
    {
        var drift = (11 - monthsAgo) * 0.02m;
        return unit switch
        {
            "%" => (Math.Round(70m + drift * 100 + _random.Next(0, 12), 0), 100m),
            "Days" => (Math.Round(320m - drift * 40 + _random.Next(0, 30), 0), 80m),
            "Hours" => (Math.Round(210m - drift * 30 + _random.Next(0, 40), 0), 90m),
            "Minutes" => (Math.Round(1450m - drift * 200 + _random.Next(0, 200), 0), 95m),
            "Ratio" => (_random.Next(28, 46), 60m),
            "Per 100 staff" => (_random.Next(0, 4), 100m),
            _ => (_random.Next(1, 9), _random.Next(900, 1400)), // per-1000-day style rates
        };
    }

    // ── Biomedical engineering ───────────────────────────────────────────────
    private async Task SeedBiomedAsync(DemoContext ctx, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var vendors = await db.Vendors.OrderBy(v => v.Id).ToListAsync(ct);
        var departments = await db.Departments.OrderBy(d => d.Id).Take(10).ToListAsync(ct);

        if (await db.BiomedEquipment.IgnoreQueryFilters().CountAsync(ct) < 5)
        {
            (string Name, string Manufacturer, EquipmentCriticality Criticality, string Location)[] equipment =
            [
                ("Ventilator — ICU Bed 1", "Draeger", EquipmentCriticality.Critical, "ICU"),
                ("Ventilator — ICU Bed 2", "Draeger", EquipmentCriticality.Critical, "ICU"),
                ("Defibrillator — Casualty", "Philips", EquipmentCriticality.Critical, "Emergency"),
                ("Anaesthesia Workstation — OT 1", "GE Healthcare", EquipmentCriticality.Critical, "Operation Theatre"),
                ("Infusion Pump — Ward 2", "B.Braun", EquipmentCriticality.High, "Ward 2"),
                ("Syringe Pump — ICU", "Fresenius Kabi", EquipmentCriticality.High, "ICU"),
                ("Patient Monitor — HDU", "Mindray", EquipmentCriticality.High, "HDU"),
                ("Ultrasound Scanner", "Samsung Medison", EquipmentCriticality.Medium, "Radiology"),
                ("X-Ray Unit — Main", "Siemens", EquipmentCriticality.High, "Radiology"),
                ("Autoclave — CSSD", "Getinge", EquipmentCriticality.Critical, "CSSD"),
                ("Haematology Analyser", "Sysmex", EquipmentCriticality.High, "Laboratory"),
                ("Biochemistry Analyser", "Roche", EquipmentCriticality.High, "Laboratory"),
                ("ECG Machine — OPD", "BPL", EquipmentCriticality.Medium, "OPD"),
                ("Dialysis Machine — Unit 1", "Fresenius", EquipmentCriticality.Critical, "Dialysis"),
                ("Phototherapy Unit — NICU", "Phoenix", EquipmentCriticality.High, "NICU"),
                ("Suction Apparatus — Ward 3", "Allied", EquipmentCriticality.Low, "Ward 3"),
            ];
            var existingCodes = await db.BiomedEquipment.IgnoreQueryFilters()
                .Select(e => e.EquipmentCode).ToListAsync(ct);
            for (var i = 0; i < equipment.Length; i++)
            {
                var code = $"BME-{(i + 1).ToString().PadLeft(4, '0')}";
                if (existingCodes.Contains(code))
                {
                    continue;
                }
                var (name, manufacturer, criticality, location) = equipment[i];
                var purchased = today.AddDays(-_random.Next(200, 2200));
                db.BiomedEquipment.Add(new BiomedEquipment
                {
                    BranchId = ctx.BranchId,
                    EquipmentCode = code,
                    Name = name,
                    SerialNumber = $"SN{_random.Next(100000, 999999)}",
                    Model = $"MDL-{_random.Next(100, 999)}",
                    Manufacturer = manufacturer,
                    DepartmentId = departments.Count > 0 ? departments[i % departments.Count].Id : null,
                    VendorId = vendors.Count > 0 ? vendors[i % vendors.Count].Id : null,
                    Criticality = criticality,
                    Status = i % 11 == 10 ? EquipmentStatus.UnderMaintenance : EquipmentStatus.InService,
                    CurrentLocation = location,
                    PurchaseDate = purchased,
                    PurchaseCost = _random.Next(50, 4000) * 1000m,
                    WarrantyExpiryDate = purchased.AddYears(2),
                });
            }
            await db.SaveChangesAsync(ct);
        }

        var allEquipment = await db.BiomedEquipment.IgnoreQueryFilters().OrderBy(e => e.Id).ToListAsync(ct);
        if (allEquipment.Count == 0)
        {
            return;
        }

        if (!await db.EquipmentLocationLogs.AnyAsync(ct))
        {
            foreach (var equipment in allEquipment.Take(8))
            {
                db.EquipmentLocationLogs.Add(new EquipmentLocationLog
                {
                    EquipmentId = equipment.Id,
                    MovedAt = DateTimeOffset.UtcNow.AddDays(-_random.Next(10, 200)),
                    FromLocation = "Biomedical Store",
                    ToLocation = equipment.CurrentLocation,
                    Reason = "Commissioned and deployed to the user department.",
                    MovedByUserId = ctx.UserId,
                });
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.AmcContracts.IgnoreQueryFilters().AnyAsync(ct) && vendors.Count > 0)
        {
            for (var i = 0; i < allEquipment.Count; i++)
            {
                var equipment = allEquipment[i];
                // Stagger end dates so some contracts are already inside the renewal window.
                var start = today.AddDays(-_random.Next(60, 400));
                db.AmcContracts.Add(new AmcContract
                {
                    BranchId = equipment.BranchId,
                    EquipmentId = equipment.Id,
                    VendorId = vendors[i % vendors.Count].Id,
                    ContractType = i % 3 == 0 ? ContractType.Cmc : ContractType.Amc,
                    ContractNumber = $"AMC-{start.Year}-{(i + 1).ToString().PadLeft(4, '0')}",
                    StartDate = start,
                    EndDate = start.AddYears(1),
                    Cost = _random.Next(15, 180) * 1000m,
                    RenewalReminderDays = 30,
                    Notes = i % 3 == 0 ? "Comprehensive cover including spares." : "Labour-only annual maintenance.",
                });
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.PmSchedules.IgnoreQueryFilters().AnyAsync(ct))
        {
            foreach (var equipment in allEquipment)
            {
                var frequency = equipment.Criticality switch
                {
                    EquipmentCriticality.Critical => 30,
                    EquipmentCriticality.High => 90,
                    EquipmentCriticality.Medium => 180,
                    _ => 365,
                };
                db.PmSchedules.Add(new PmSchedule
                {
                    BranchId = equipment.BranchId,
                    EquipmentId = equipment.Id,
                    FrequencyDays = frequency,
                    // Some already due, so the "generate due work orders" action has something to do.
                    NextDueDate = today.AddDays(_random.Next(-20, 60)),
                    LastGeneratedDate = today.AddDays(-frequency),
                    ChecklistNotes = "Visual inspection, electrical safety test, functional check, calibration verification.",
                });
            }
            await db.SaveChangesAsync(ct);
        }

        if (await db.BreakdownComplaints.IgnoreQueryFilters().CountAsync(ct) < 5)
        {
            string[] faults =
            [
                "Unit powers on but display flickers intermittently.",
                "Alarm sounding continuously without a clinical trigger.",
                "Battery not holding charge beyond ten minutes.",
                "Pressure readings inconsistent with the reference gauge.",
                "Physical damage to the mains cable noticed during the round.",
                "Device fails self-test on start-up.",
            ];
            var existingCount = await db.BreakdownComplaints.IgnoreQueryFilters().CountAsync(ct);
            for (var i = 0; i < 12; i++)
            {
                var equipment = allEquipment[i % allEquipment.Count];
                var reportedAt = DateTimeOffset.UtcNow.AddDays(-_random.Next(2, 180));
                var status = (i % 4) switch
                {
                    0 => ComplaintStatus.Reported,
                    1 => ComplaintStatus.Assigned,
                    2 => ComplaintStatus.InProgress,
                    _ => ComplaintStatus.Resolved,
                };
                var resolvedAt = status == ComplaintStatus.Resolved
                    ? reportedAt.AddHours(_random.Next(4, 96)) : (DateTimeOffset?)null;
                db.BreakdownComplaints.Add(new BreakdownComplaint
                {
                    BranchId = equipment.BranchId,
                    EquipmentId = equipment.Id,
                    ComplaintNumber = $"BRK-{reportedAt.Year}-{(existingCount + i + 1).ToString().PadLeft(4, '0')}",
                    ReportedByUserId = ctx.UserId,
                    ReportedAt = reportedAt,
                    Description = faults[i % faults.Length],
                    Status = status,
                    AssignedToUserId = status == ComplaintStatus.Reported ? null : ctx.UserId,
                    AssignedAt = status == ComplaintStatus.Reported ? null : reportedAt.AddHours(1),
                    ResolvedAt = resolvedAt,
                    DowntimeMinutes = resolvedAt is null ? null : (int)(resolvedAt.Value - reportedAt).TotalMinutes,
                    Resolution = resolvedAt is null ? null : "Faulty module replaced under AMC; device returned to service.",
                });
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.WorkOrders.IgnoreQueryFilters().AnyAsync(ct))
        {
            var schedules = await db.PmSchedules.IgnoreQueryFilters().OrderBy(s => s.Id).ToListAsync(ct);
            var complaints = await db.BreakdownComplaints.IgnoreQueryFilters()
                .OrderBy(c => c.Id).Take(12).ToListAsync(ct);
            var index = 0;

            foreach (var schedule in schedules)
            {
                index++;
                var scheduled = today.AddDays(-_random.Next(0, 120));
                var status = (index % 3) switch
                {
                    0 => WorkOrderStatus.Open,
                    1 => WorkOrderStatus.InProgress,
                    _ => WorkOrderStatus.Completed,
                };
                db.WorkOrders.Add(new WorkOrder
                {
                    BranchId = schedule.BranchId,
                    EquipmentId = schedule.EquipmentId,
                    WorkOrderNumber = $"WO-{today.Year}-{index.ToString().PadLeft(5, '0')}",
                    WorkOrderType = WorkOrderType.PreventiveMaintenance,
                    PmScheduleId = schedule.Id,
                    Status = status,
                    ScheduledDate = scheduled,
                    CompletedAt = status == WorkOrderStatus.Completed
                        ? new DateTimeOffset(scheduled.ToDateTime(new TimeOnly(14, 0)), TimeSpan.Zero) : null,
                    AssignedToUserId = ctx.UserId,
                    Notes = "Routine preventive maintenance as per the checklist.",
                });
            }
            foreach (var complaint in complaints)
            {
                index++;
                db.WorkOrders.Add(new WorkOrder
                {
                    BranchId = complaint.BranchId,
                    EquipmentId = complaint.EquipmentId,
                    WorkOrderNumber = $"WO-{today.Year}-{index.ToString().PadLeft(5, '0')}",
                    WorkOrderType = WorkOrderType.Breakdown,
                    BreakdownComplaintId = complaint.Id,
                    // A Closed complaint used to fall into the Open branch, leaving a resolved
                    // breakdown with a work order still showing as outstanding — and starving the
                    // downtime/service-history/spare-usage reports that key off Completed.
                    Status = complaint.Status is ComplaintStatus.Resolved or ComplaintStatus.Closed
                        ? WorkOrderStatus.Completed
                        : WorkOrderStatus.Open,
                    ScheduledDate = DateOnly.FromDateTime(complaint.ReportedAt.UtcDateTime),
                    CompletedAt = complaint.ResolvedAt,
                    AssignedToUserId = ctx.UserId,
                    Notes = complaint.Resolution ?? "Awaiting spare part.",
                });
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Calibrations.IgnoreQueryFilters().AnyAsync(ct))
        {
            foreach (var equipment in allEquipment)
            {
                var calibrated = today.AddDays(-_random.Next(30, 340));
                db.Calibrations.Add(new Calibration
                {
                    BranchId = equipment.BranchId,
                    EquipmentId = equipment.Id,
                    CalibrationDate = calibrated,
                    // A few fall before today so the "overdue calibration" alert is non-empty.
                    NextDueDate = calibrated.AddDays(365),
                    PerformedBy = "National Accreditation Board Calibration Services",
                    CertificateNumber = $"CAL/{calibrated.Year}/{_random.Next(1000, 9999)}",
                    CertificateFilePath = $"demo/biomed/calibration/{equipment.EquipmentCode}.pdf",
                    Notes = "Calibrated against traceable reference standards; within permissible error.",
                });
            }
            await db.SaveChangesAsync(ct);
        }

        if (await db.SpareUsages.CountAsync(ct) < 10)
        {
            // Spares consumed on a breakdown repair. The real BiomedService decrements stock via
            // the shared StockLedgerWriter, so this mirrors it: batch down + a ledger row out.
            var breakdownWorkOrders = await db.WorkOrders.IgnoreQueryFilters()
                .Where(w => w.WorkOrderType == WorkOrderType.Breakdown && w.Status == WorkOrderStatus.Completed)
                .OrderBy(w => w.Id).Take(8).ToListAsync(ct);
            var spareBatches = await db.Batches.IgnoreQueryFilters()
                .Where(b => b.QuantityOnHand > 2)
                .OrderByDescending(b => b.QuantityOnHand).Take(8).ToListAsync(ct);
            var defaultStoreId = await db.Stores.IgnoreQueryFilters()
                .OrderBy(s => s.Id).Select(s => (long?)s.Id).FirstOrDefaultAsync(ct);

            var spentWorkOrders = await db.SpareUsages.Select(u => u.WorkOrderId).ToListAsync(ct);
            breakdownWorkOrders = [.. breakdownWorkOrders.Where(w => !spentWorkOrders.Contains(w.Id))];
            for (var i = 0; i < breakdownWorkOrders.Count && spareBatches.Count > 0; i++)
            {
                var workOrder = breakdownWorkOrders[i];
                var batch = spareBatches[i % spareBatches.Count];
                const decimal quantity = 1m;
                db.SpareUsages.Add(new SpareUsage
                {
                    WorkOrderId = workOrder.Id, BatchId = batch.Id, Quantity = quantity,
                });
                await db.SaveChangesAsync(ct);

                if (defaultStoreId is { } storeId)
                {
                    batch.QuantityOnHand -= quantity;
                    db.StockLedgers.Add(new StockLedger
                    {
                        BranchId = workOrder.BranchId,
                        StoreId = storeId,
                        BatchId = batch.Id,
                        MovementType = StockMovementType.IssueOut,
                        Quantity = -quantity,
                        RunningBalance = batch.QuantityOnHand,
                        ReferenceType = nameof(SpareUsage),
                        ReferenceId = workOrder.Id,
                        MovementAt = workOrder.CompletedAt ?? DateTimeOffset.UtcNow,
                        Notes = "Demo data biomedical spare consumption.",
                    });
                    await db.SaveChangesAsync(ct);
                }
            }
        }

        if (!await db.ServiceHistoryEntries.IgnoreQueryFilters().AnyAsync(ct))
        {
            var completedWorkOrders = await db.WorkOrders.IgnoreQueryFilters()
                .Where(w => w.Status == WorkOrderStatus.Completed).OrderBy(w => w.Id).ToListAsync(ct);
            foreach (var workOrder in completedWorkOrders)
            {
                db.ServiceHistoryEntries.Add(new ServiceHistoryEntry
                {
                    BranchId = workOrder.BranchId,
                    EquipmentId = workOrder.EquipmentId,
                    WorkOrderId = workOrder.Id,
                    ServiceDate = workOrder.ScheduledDate,
                    ServiceType = workOrder.WorkOrderType == WorkOrderType.PreventiveMaintenance
                        ? "Preventive Maintenance" : "Breakdown Repair",
                    PerformedBy = "In-house Biomedical Engineering",
                    Cost = workOrder.WorkOrderType == WorkOrderType.Breakdown ? _random.Next(2, 45) * 1000m : 0m,
                    Notes = workOrder.Notes,
                });
            }
            await db.SaveChangesAsync(ct);
        }
    }

    // ── MRD ──────────────────────────────────────────────────────────────────
    private async Task SeedMrdAsync(DemoContext ctx, CancellationToken ct)
    {
        var dischargedAdmissions = await db.Admissions.IgnoreQueryFilters()
            .Where(a => a.DischargedAt != null)
            .OrderByDescending(a => a.Id).Take(60).ToListAsync(ct);
        if (dischargedAdmissions.Count == 0)
        {
            logger.LogInformation("MRD demo seed skipped — no discharged admissions to file.");
            return;
        }

        if (await db.CodingWorklistEntries.CountAsync(ct) < 5)
        {
            var existing = await db.CodingWorklistEntries.Select(e => e.AdmissionId).ToListAsync(ct);
            var index = 0;
            foreach (var admission in dischargedAdmissions.Where(a => !existing.Contains(a.Id)))
            {
                index++;
                var coded = index % 3 != 0;
                db.CodingWorklistEntries.Add(new CodingWorklistEntry
                {
                    AdmissionId = admission.Id,
                    Status = coded ? CodingStatus.Coded : CodingStatus.Pending,
                    CodedByUserId = coded ? ctx.UserId : null,
                    CodedAt = coded ? admission.DischargedAt!.Value.AddDays(_random.Next(1, 10)) : null,
                    Notes = coded ? "Primary and secondary diagnoses coded from the discharge summary." : null,
                });
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.FileAudits.AnyAsync(ct))
        {
            var index = 0;
            foreach (var admission in dischargedAdmissions.Take(40))
            {
                index++;
                // Every fourth file is deliberately incomplete so the deficiency report is non-empty.
                var complete = index % 4 != 0;
                db.FileAudits.Add(new FileAudit
                {
                    AdmissionId = admission.Id,
                    DischargeSummarySigned = complete,
                    ConsentsPresent = true,
                    ProgressNotesComplete = complete,
                    InvestigationReportsPresent = complete || index % 8 != 0,
                    NursingRecordsComplete = complete,
                    IsComplete = complete,
                    MissingDocumentsNotes = complete
                        ? null : "Discharge summary not authenticated; nursing notes missing for one shift.",
                    AuditedByUserId = ctx.UserId,
                    AuditedAt = admission.DischargedAt!.Value.AddDays(_random.Next(2, 20)),
                });
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.ScannedDocuments.AnyAsync(ct))
        {
            var docTypes = await db.RecordDocumentTypes.OrderBy(t => t.Id).ToListAsync(ct);
            if (docTypes.Count > 0)
            {
                foreach (var admission in dischargedAdmissions.Take(30))
                {
                    foreach (var docType in docTypes.OrderBy(_ => _random.Next()).Take(3))
                    {
                        db.ScannedDocuments.Add(new ScannedDocument
                        {
                            AdmissionId = admission.Id,
                            DocumentTypeId = docType.Id,
                            FileName = $"{docType.Code.ToLowerInvariant()}-{admission.AdmissionNumber}.pdf",
                            // Demo rows reference a conventional path; no file is written to disk.
                            FilePath = $"demo/mrd/{admission.AdmissionNumber}/{docType.Code.ToLowerInvariant()}.pdf",
                            ContentType = "application/pdf",
                            SizeBytes = _random.Next(80_000, 2_500_000),
                            Description = $"{docType.Name} scanned into the medical record file.",
                            UploadedByUserId = ctx.UserId,
                            UploadedAt = admission.DischargedAt!.Value.AddDays(_random.Next(1, 15)),
                        });
                    }
                }
                await db.SaveChangesAsync(ct);
            }
        }

        // The physical record file itself, plus its movement log and issue/return requests.
        // These were never seeded, which left every MRD screen (file register, movement history,
        // pending requests, overdue list) empty and made the destruction register below fall
        // back to whatever single file happened to exist.
        if (await db.RecordFiles.IgnoreQueryFilters().CountAsync(ct) < 5)
        {
            var alreadyFiled = await db.RecordFiles.IgnoreQueryFilters()
                .Select(f => f.AdmissionId).ToListAsync(ct);
            var racks = new[] { "MRD Storage — Rack A", "MRD Storage — Rack B", "MRD Storage — Rack C", "MRD Storage — Rack D" };
            var index = 0;
            foreach (var admission in dischargedAdmissions.Where(a => !alreadyFiled.Contains(a.Id)))
            {
                index++;
                db.RecordFiles.Add(new RecordFile
                {
                    BranchId = admission.BranchId,
                    AdmissionId = admission.Id,
                    FileNumber = $"MRD-{admission.AdmissionNumber}",
                    Status = RecordFileStatus.Active,
                    CurrentLocation = racks[index % racks.Length],
                    CurrentCustodianUserId = null,
                    CreatedByUserId = ctx.UserId,
                });
            }
            await db.SaveChangesAsync(ct);
        }

        var files = await db.RecordFiles.IgnoreQueryFilters().OrderBy(f => f.Id).ToListAsync(ct);

        if (files.Count > 0 && await db.FileMovements.CountAsync(ct) < 10)
        {
            // Two hops per file — into MRD storage on receipt, then out to a department and back
            // for a sample of them, so the movement-history report has a real trail to show.
            var departments = new[] { "Ward 3B Nursing Station", "Coding Desk", "Insurance Desk", "Consultant Chamber", "Audit Room" };
            var index = 0;
            foreach (var file in files)
            {
                index++;
                var opened = DateTimeOffset.UtcNow.AddDays(-_random.Next(30, 400));
                db.FileMovements.Add(new FileMovement
                {
                    RecordFileId = file.Id,
                    MovedAt = opened,
                    FromLocation = "Ward — on discharge",
                    ToLocation = file.CurrentLocation,
                    MovedByUserId = ctx.UserId,
                    Reason = "File received in MRD after discharge.",
                });

                if (index % 3 != 0)
                {
                    continue;
                }

                var destination = departments[index % departments.Length];
                db.FileMovements.Add(new FileMovement
                {
                    RecordFileId = file.Id,
                    MovedAt = opened.AddDays(_random.Next(5, 60)),
                    FromLocation = file.CurrentLocation,
                    ToLocation = destination,
                    FromCustodianUserId = null,
                    ToCustodianUserId = ctx.UserId,
                    MovedByUserId = ctx.UserId,
                    Reason = "Issued for review.",
                });
                db.FileMovements.Add(new FileMovement
                {
                    RecordFileId = file.Id,
                    MovedAt = opened.AddDays(_random.Next(61, 90)),
                    FromLocation = destination,
                    ToLocation = file.CurrentLocation,
                    FromCustodianUserId = ctx.UserId,
                    ToCustodianUserId = null,
                    MovedByUserId = ctx.UserId,
                    Reason = "Returned to MRD storage.",
                });
            }
            await db.SaveChangesAsync(ct);
        }

        if (files.Count > 0 && await db.FileRequests.CountAsync(ct) < 10)
        {
            // Spread across every lifecycle state, including two deliberately overdue Issued rows
            // (past ExpectedReturnDate) so the overdue report — which computes that on read — is
            // not empty.
            (FileRequestType Type, string? Requester, string? Org, string Purpose)[] shapes =
            [
                (FileRequestType.Internal, null, null, "Clinical audit review"),
                (FileRequestType.Internal, null, null, "Readmission — previous notes required"),
                (FileRequestType.Internal, null, null, "Coding clarification"),
                (FileRequestType.External, "Dr. Anand Krishnan", "Krishnan Poly Clinic", "Second-opinion referral"),
                (FileRequestType.External, "Star Health Claims", "Star Health & Allied Insurance", "Insurance claim verification"),
                (FileRequestType.External, "Ms. Radha Subramanian", "Patient next-of-kin", "Copy of records requested by family"),
                (FileRequestType.Legal, "Adv. S. Karthikeyan", "District Court, Chennai", "Court summons — MLC case records"),
                (FileRequestType.Legal, "Inspector of Police", "Adyar Police Station", "Police investigation — accident case"),
            ];
            var index = 0;
            foreach (var file in files.Take(36))
            {
                index++;
                var shape = shapes[index % shapes.Length];
                var requestedAt = DateTimeOffset.UtcNow.AddDays(-_random.Next(2, 180));
                var status = (index % 6) switch
                {
                    0 => FileRequestStatus.Requested,
                    1 => FileRequestStatus.Approved,
                    2 => FileRequestStatus.Rejected,
                    3 or 4 => FileRequestStatus.Issued,
                    _ => FileRequestStatus.Returned,
                };

                // Every second Issued row is left past its expected return date.
                var expectedReturn = status == FileRequestStatus.Issued && index % 4 == 3
                    ? DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-_random.Next(3, 40))
                    : DateOnly.FromDateTime(requestedAt.UtcDateTime).AddDays(14);

                var decided = status is not FileRequestStatus.Requested;
                var issued = status is FileRequestStatus.Issued or FileRequestStatus.Returned;

                db.FileRequests.Add(new FileRequest
                {
                    RecordFileId = file.Id,
                    RequestType = shape.Type,
                    RequestedByUserId = shape.Type == FileRequestType.Internal ? ctx.UserId : null,
                    RequesterName = shape.Requester,
                    RequesterOrganization = shape.Org,
                    Purpose = shape.Purpose,
                    ExpectedReturnDate = expectedReturn,
                    Status = status,
                    RequestedAt = requestedAt,
                    ApprovedByUserId = decided && status != FileRequestStatus.Rejected ? ctx.UserId : null,
                    ApprovedAt = decided && status != FileRequestStatus.Rejected
                        ? requestedAt.AddHours(_random.Next(2, 48)) : null,
                    RejectReason = status == FileRequestStatus.Rejected
                        ? "Requester not authorised to receive the full record; summary issued instead." : null,
                    IssuedByUserId = issued ? ctx.UserId : null,
                    IssuedAt = issued ? requestedAt.AddDays(_random.Next(1, 4)) : null,
                    ReturnedToUserId = status == FileRequestStatus.Returned ? ctx.UserId : null,
                    ReturnedAt = status == FileRequestStatus.Returned
                        ? requestedAt.AddDays(_random.Next(5, 25)) : null,
                    Notes = shape.Type == FileRequestType.Legal
                        ? "Released under written legal authority; copy retained in MRD." : null,
                });
            }
            await db.SaveChangesAsync(ct);
        }

        if (await db.DestructionRegisterEntries.CountAsync(ct) < 10)
        {
            var alreadyRegistered = await db.DestructionRegisterEntries
                .Select(e => e.RecordFileId).ToListAsync(ct);
            var recordFiles = await db.RecordFiles
                .Where(f => !alreadyRegistered.Contains(f.Id))
                .OrderBy(f => f.Id).Take(30).ToListAsync(ct);
            var policy = await db.RetentionPolicies.OrderBy(p => p.Id).FirstOrDefaultAsync(ct);
            if (recordFiles.Count > 0 && policy is not null)
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                for (var i = 0; i < recordFiles.Count; i++)
                {
                    var status = (i % 3) switch
                    {
                        0 => DestructionStatus.Eligible,
                        1 => DestructionStatus.Approved,
                        _ => DestructionStatus.Destroyed,
                    };
                    db.DestructionRegisterEntries.Add(new DestructionRegisterEntry
                    {
                        RecordFileId = recordFiles[i].Id,
                        RetentionPolicyId = policy.Id,
                        EligibleDate = today.AddDays(-_random.Next(10, 400)),
                        Status = status,
                        ApprovedByUserId = status == DestructionStatus.Eligible ? null : ctx.UserId,
                        ApprovedAt = status == DestructionStatus.Eligible
                            ? null : DateTimeOffset.UtcNow.AddDays(-_random.Next(5, 60)),
                        DestroyedByUserId = status == DestructionStatus.Destroyed ? ctx.UserId : null,
                        DestroyedAt = status == DestructionStatus.Destroyed
                            ? DateTimeOffset.UtcNow.AddDays(-_random.Next(1, 20)) : null,
                        DestructionMethod = status == DestructionStatus.Destroyed ? "Cross-cut shredding, witnessed" : null,
                        Notes = status == DestructionStatus.Destroyed
                            ? "Destroyed under the retention policy; certificate filed." : null,
                    });
                }
                await db.SaveChangesAsync(ct);
            }
        }
    }

    // ── Billing workflow extras ──────────────────────────────────────────────
    private async Task SeedBillingWorkflowAsync(DemoContext ctx, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (!await db.BillEstimates.IgnoreQueryFilters().AnyAsync(ct))
        {
            var templates = await db.BillEstimateTemplates.IgnoreQueryFilters()
                .Include(t => t.Items).OrderBy(t => t.Id).ToListAsync(ct);
            var services = await db.Services.ToDictionaryAsync(s => s.Id, ct);
            var patients = await db.Patients.IgnoreQueryFilters().OrderByDescending(p => p.Id).Take(20).ToListAsync(ct);
            if (templates.Count > 0 && patients.Count > 0)
            {
                for (var i = 0; i < patients.Count; i++)
                {
                    var template = templates[i % templates.Count];
                    var estimateDate = today.AddDays(-_random.Next(1, 120));
                    var estimate = new BillEstimate
                    {
                        BranchId = ctx.BranchId,
                        PatientId = patients[i].Id,
                        BillEstimateTemplateId = template.Id,
                        EstimateNumber = $"EST-{estimateDate.Year}-{(i + 1).ToString().PadLeft(5, '0')}",
                        EstimateDate = estimateDate,
                        Status = i % 3 == 0 ? BillEstimateStatus.Draft : BillEstimateStatus.Printed,
                        Notes = "Indicative estimate; actual charges depend on the clinical course.",
                    };
                    decimal total = 0;
                    foreach (var templateItem in template.Items)
                    {
                        var rate = templateItem.ServiceId is { } serviceId && services.TryGetValue(serviceId, out var service)
                            ? service.DefaultRate : 1000m;
                        var amount = rate * templateItem.Quantity;
                        total += amount;
                        estimate.Items.Add(new BillEstimateItem
                        {
                            SourceType = templateItem.SourceType,
                            ServiceId = templateItem.ServiceId,
                            PackageId = templateItem.PackageId,
                            Description = templateItem.ServiceId is { } sid && services.TryGetValue(sid, out var s)
                                ? s.Name : "Estimated service",
                            Quantity = templateItem.Quantity,
                            Rate = rate,
                            TotalAmount = amount,
                        });
                    }
                    estimate.TotalAmount = total;
                    db.BillEstimates.Add(estimate);
                }
                await db.SaveChangesAsync(ct);
            }
        }

        if (!await db.BillNotes.AnyAsync(ct))
        {
            var bills = await db.Bills.IgnoreQueryFilters().OrderByDescending(b => b.Id).Take(25).ToListAsync(ct);
            string[] notes =
            [
                "Patient requested an itemised printout for insurance reimbursement.",
                "Corporate approval letter received; balance moved to credit.",
                "Attender informed of the estimated balance at discharge.",
                "Concession applied after Medical Superintendent's approval.",
                "Duplicate receipt issued on patient request.",
            ];
            for (var i = 0; i < bills.Count; i++)
            {
                db.BillNotes.Add(new BillNote { BillId = bills[i].Id, Note = notes[i % notes.Length] });
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.BillCreditNotes.AnyAsync(ct))
        {
            // Must filter on Items.Any() in the query, not after Take() — the newest bills are
            // often IP/emergency shells with no line items yet, which would yield nothing.
            var bills = await db.Bills.IgnoreQueryFilters()
                .Include(b => b.Items)
                .Where(b => b.Status != BillStatus.Cancelled && b.Items.Any())
                .OrderByDescending(b => b.Id).Take(12).ToListAsync(ct);
            var index = 0;
            foreach (var bill in bills)
            {
                index++;
                var status = (index % 3) switch
                {
                    0 => CreditNoteStatus.Pending,
                    1 => CreditNoteStatus.Approved,
                    _ => CreditNoteStatus.Rejected,
                };
                var item = bill.Items.First();
                var creditNote = new BillCreditNote
                {
                    BillId = bill.Id,
                    CreditNoteNumber = $"CN-{today.Year}-{index.ToString().PadLeft(5, '0')}",
                    CreditNoteDate = today.AddDays(-_random.Next(1, 90)),
                    Reason = "Service billed but not rendered — reversed after review.",
                    Amount = Math.Round(item.TotalAmount, 2),
                    Status = status,
                    DecidedByUserId = status == CreditNoteStatus.Pending ? null : ctx.UserId,
                    DecidedAt = status == CreditNoteStatus.Pending ? null : DateTimeOffset.UtcNow.AddDays(-_random.Next(1, 30)),
                    RejectionReason = status == CreditNoteStatus.Rejected ? "Service was in fact rendered; see nursing notes." : null,
                };
                creditNote.Items.Add(new BillCreditNoteItem
                {
                    BillItemId = item.Id, Amount = Math.Round(item.TotalAmount, 2),
                });
                db.BillCreditNotes.Add(creditNote);
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.CashierShifts.IgnoreQueryFilters().AnyAsync(ct))
        {
            var paymentModes = await db.PaymentModes.OrderBy(m => m.Id).ToListAsync(ct);
            for (var dayOffset = 6; dayOffset >= 0; dayOffset--)
            {
                // Built explicitly at offset 0 — Npgsql rejects a timestamptz with a local offset.
                var openedAt = new DateTimeOffset(
                    DateTime.UtcNow.Date.AddDays(-dayOffset).AddHours(8), TimeSpan.Zero);
                // Today's shift stays open; earlier ones are closed, and older ones handed over.
                var status = dayOffset == 0 ? CashierShiftStatus.Open
                    : dayOffset <= 2 ? CashierShiftStatus.Closed
                    : CashierShiftStatus.Received;
                var shift = new CashierShift
                {
                    BranchId = ctx.BranchId,
                    UserId = ctx.UserId,
                    OpenedAt = openedAt,
                    OpeningFloat = 5000m,
                    Status = status,
                    ClosedAt = status == CashierShiftStatus.Open ? null : openedAt.AddHours(9),
                    ReceivedByUserId = status == CashierShiftStatus.Received ? ctx.UserId : null,
                    ReceivedAt = status == CashierShiftStatus.Received ? openedAt.AddHours(10) : null,
                    Notes = status == CashierShiftStatus.Open ? null : "Shift reconciled at counter close.",
                };
                if (status != CashierShiftStatus.Open)
                {
                    foreach (var mode in paymentModes)
                    {
                        var system = _random.Next(5, 120) * 1000m;
                        // Small cash variances only; card/UPI reconcile exactly, as they would in practice.
                        var declared = mode.Code == "CASH" ? system + _random.Next(-200, 201) : system;
                        shift.ModeAmounts.Add(new CashierShiftModeAmount
                        {
                            PaymentModeId = mode.Id,
                            SystemAmount = system,
                            DeclaredAmount = declared,
                            Variance = declared - system,
                        });
                    }
                }
                db.CashierShifts.Add(shift);
            }
            await db.SaveChangesAsync(ct);
        }
    }

    // ── Pharmacy returns (with matching stock movements) ─────────────────────
    private async Task SeedPharmacyExtrasAsync(DemoContext ctx, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (!await db.SalesReturns.IgnoreQueryFilters().AnyAsync(ct))
        {
            var sales = await db.PharmacySales.IgnoreQueryFilters()
                .Include(s => s.Items)
                .OrderByDescending(s => s.Id).Take(10).ToListAsync(ct);
            var index = 0;
            foreach (var sale in sales.Where(s => s.Items.Count > 0))
            {
                index++;
                var item = sale.Items.First();
                var returnQty = Math.Min(1m, item.Quantity);
                var salesReturn = new SalesReturn
                {
                    BranchId = sale.BranchId,
                    PharmacySaleId = sale.Id,
                    ReturnNumber = $"SR-{today.Year}-{index.ToString().PadLeft(5, '0')}",
                    ReturnDate = today.AddDays(-_random.Next(1, 45)),
                    Reason = "Patient returned unused strip; medication changed by the treating doctor.",
                    TotalAmount = Math.Round(item.Rate * returnQty, 2),
                };
                salesReturn.Items.Add(new SalesReturnItem
                {
                    PharmacySaleItemId = item.Id,
                    Quantity = returnQty,
                    Amount = Math.Round(item.Rate * returnQty, 2),
                });
                db.SalesReturns.Add(salesReturn);
                await db.SaveChangesAsync(ct);

                // Stock coming back in must move the batch and leave a ledger trail, or the
                // stock reports would disagree with the return document.
                await PostStockInAsync(item.BatchId, sale.StoreId, sale.BranchId, returnQty,
                    StockMovementType.SaleReturnIn, nameof(SalesReturn), salesReturn.Id,
                    salesReturn.ReturnDate, ct);
            }
        }

        if (!await db.IssueReturns.IgnoreQueryFilters().AnyAsync(ct))
        {
            var issues = await db.Issues.IgnoreQueryFilters()
                .Include(i => i.Items)
                .OrderByDescending(i => i.Id).Take(5).ToListAsync(ct);
            var index = 0;
            foreach (var issue in issues.Where(i => i.Items.Count > 0))
            {
                index++;
                var item = issue.Items.First();
                var returnQty = Math.Min(1m, item.Quantity);
                var issueReturn = new IssueReturn
                {
                    BranchId = issue.BranchId,
                    IssueId = issue.Id,
                    ReturnNumber = $"IR-{today.Year}-{index.ToString().PadLeft(5, '0')}",
                    ReturnDate = today.AddDays(-_random.Next(1, 30)),
                    Reason = "Unused stock returned to pharmacy after the patient was discharged.",
                };
                issueReturn.Items.Add(new IssueReturnItem { IssueItemId = item.Id, Quantity = returnQty });
                db.IssueReturns.Add(issueReturn);
                await db.SaveChangesAsync(ct);

                await PostStockInAsync(item.BatchId, issue.StoreId, issue.BranchId, returnQty,
                    StockMovementType.IssueReturnIn, nameof(IssueReturn), issueReturn.Id,
                    issueReturn.ReturnDate, ct);
            }
        }
    }

    /// <summary>
    /// Increments a batch and appends the matching append-only stock-ledger row, mirroring what
    /// the real StockLedgerWriter does — demo returns must not silently desynchronise stock.
    /// </summary>
    private async Task PostStockInAsync(
        long batchId, long storeId, long branchId, decimal quantity,
        StockMovementType movementType, string referenceType, long referenceId, DateOnly movementDate,
        CancellationToken ct)
    {
        var batch = await db.Batches.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.Id == batchId, ct);
        if (batch is null)
        {
            return;
        }
        batch.QuantityOnHand += quantity;
        db.StockLedgers.Add(new StockLedger
        {
            BranchId = branchId,
            StoreId = storeId,
            BatchId = batchId,
            MovementType = movementType,
            Quantity = quantity,
            RunningBalance = batch.QuantityOnHand,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            MovementAt = new DateTimeOffset(movementDate.ToDateTime(new TimeOnly(11, 0)), TimeSpan.Zero),
            Notes = "Demo data return movement.",
        });
        await db.SaveChangesAsync(ct);
    }

    // ── Clinical extras (IPD / OPD / appointments) ───────────────────────────
    private async Task SeedClinicalExtrasAsync(DemoContext ctx, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (!await db.AdmissionRequests.IgnoreQueryFilters().AnyAsync(ct))
        {
            var patients = await db.Patients.IgnoreQueryFilters().OrderByDescending(p => p.Id).Take(15).ToListAsync(ct);
            var doctorIds = await db.Doctors.OrderBy(d => d.Id).Select(d => d.Id).Take(8).ToListAsync(ct);
            var wardIds = await db.Wards.IgnoreQueryFilters().OrderBy(w => w.Id).Select(w => w.Id).Take(5).ToListAsync(ct);
            for (var i = 0; i < patients.Count; i++)
            {
                var status = (i % 3) switch
                {
                    0 => AdmissionRequestStatus.Pending,
                    1 => AdmissionRequestStatus.Approved,
                    _ => AdmissionRequestStatus.Rejected,
                };
                db.AdmissionRequests.Add(new AdmissionRequest
                {
                    BranchId = ctx.BranchId,
                    PatientId = patients[i].Id,
                    RequestedConsultantId = doctorIds.Count > 0 ? doctorIds[i % doctorIds.Count] : null,
                    WardId = wardIds.Count > 0 ? wardIds[i % wardIds.Count] : null,
                    ExpectedAdmissionDate = today.AddDays(_random.Next(-10, 14)),
                    Reason = DemoDiagnoses[i % DemoDiagnoses.Length] + " — planned admission for management.",
                    Status = status,
                    RejectionReason = status == AdmissionRequestStatus.Rejected ? "No bed available in the requested ward." : null,
                    RequestedByUserId = ctx.UserId,
                    DecidedByUserId = status == AdmissionRequestStatus.Pending ? null : ctx.UserId,
                    DecidedAt = status == AdmissionRequestStatus.Pending ? null : DateTimeOffset.UtcNow.AddDays(-_random.Next(1, 10)),
                });
            }
            await db.SaveChangesAsync(ct);
        }

        var recentAdmissions = await db.Admissions.IgnoreQueryFilters()
            .OrderByDescending(a => a.Id).Take(25).ToListAsync(ct);

        if (!await db.IntakeOutputEntries.AnyAsync(ct) && recentAdmissions.Count > 0)
        {
            (string Category, bool IsIntake)[] categories =
            [
                ("Oral fluids", true), ("IV fluids", true), ("Ryles tube feed", true),
                ("Urine", false), ("Drain", false), ("Vomitus", false),
            ];
            foreach (var admission in recentAdmissions.Take(15))
            {
                // Two days of four-hourly charting per admission.
                for (var hoursAgo = 48; hoursAgo > 0; hoursAgo -= 4)
                {
                    var (category, isIntake) = categories[_random.Next(categories.Length)];
                    db.IntakeOutputEntries.Add(new IntakeOutputEntry
                    {
                        AdmissionId = admission.Id,
                        RecordedAt = DateTimeOffset.UtcNow.AddHours(-hoursAgo),
                        IsIntake = isIntake,
                        Category = category,
                        QuantityMl = _random.Next(50, 500),
                        Notes = null,
                    });
                }
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.ProcedureNotes.AnyAsync(ct) && recentAdmissions.Count > 0)
        {
            var doctorIds = await db.Doctors.OrderBy(d => d.Id).Select(d => d.Id).Take(10).ToListAsync(ct);
            (string Name, string Findings)[] procedures =
            [
                ("Central line insertion", "Right internal jugular line inserted under ultrasound guidance; no immediate complication."),
                ("Intercostal drainage", "ICD inserted in the fifth intercostal space; 400 ml serous fluid drained."),
                ("Lumbar puncture", "Clear CSF obtained at the L3-L4 space; opening pressure normal."),
                ("Foley catheterisation", "16 Fr catheter inserted aseptically; clear urine drained."),
                ("Wound debridement", "Necrotic tissue excised; wound packed with saline gauze."),
                ("Bone marrow aspiration", "Aspirate obtained from the posterior iliac crest; sample sent for cytology."),
            ];
            if (doctorIds.Count > 0)
            {
                for (var i = 0; i < recentAdmissions.Count; i++)
                {
                    var (name, findings) = procedures[i % procedures.Length];
                    db.ProcedureNotes.Add(new ProcedureNote
                    {
                        AdmissionId = recentAdmissions[i].Id,
                        ProcedureName = name,
                        PerformedByDoctorId = doctorIds[i % doctorIds.Count],
                        PerformedAt = DateTimeOffset.UtcNow.AddDays(-_random.Next(1, 20)),
                        Findings = findings,
                        Notes = "Procedure explained to the attender; consent obtained beforehand.",
                    });
                }
                await db.SaveChangesAsync(ct);
            }
        }

        if (!await db.VisitReferrals.AnyAsync(ct))
        {
            var visits = await db.OpVisits.IgnoreQueryFilters().OrderByDescending(v => v.Id).Take(30).ToListAsync(ct);
            var doctorIds = await db.Doctors.OrderBy(d => d.Id).Select(d => d.Id).Take(10).ToListAsync(ct);
            string[] reasons =
            [
                "Cardiology opinion for abnormal ECG findings.",
                "Orthopaedic opinion for persistent joint pain.",
                "Ophthalmology review for diabetic retinopathy screening.",
                "Psychiatry opinion for suspected depressive episode.",
                "Higher-centre referral for neurosurgical management.",
            ];
            for (var i = 0; i < visits.Count && doctorIds.Count > 0; i++)
            {
                var internalReferral = i % 3 != 0;
                // Some internal referrals already have the cross-consult opinion entered; the rest
                // stay pending so the Doctor Portal opinion inbox is non-empty.
                var opinionEntered = internalReferral && i % 2 == 0;
                db.VisitReferrals.Add(new VisitReferral
                {
                    VisitId = visits[i].Id,
                    Direction = internalReferral ? ReferralDirection.Internal : ReferralDirection.External,
                    ReferredToDoctorId = internalReferral ? doctorIds[i % doctorIds.Count] : null,
                    ExternalDetails = internalReferral ? null : "Government General Hospital, Neurosurgery Department",
                    Reason = reasons[i % reasons.Length],
                    OpinionNotes = opinionEntered
                        ? "Reviewed. Advise continuing current therapy and review in two weeks with repeat investigations."
                        : null,
                    OpinionEnteredAt = opinionEntered ? DateTimeOffset.UtcNow.AddDays(-_random.Next(1, 10)) : null,
                });
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.ScheduleOverrides.IgnoreQueryFilters().AnyAsync(ct))
        {
            var doctorIds = await db.Doctors.OrderBy(d => d.Id).Select(d => d.Id).Take(12).ToListAsync(ct);
            for (var i = 0; i < doctorIds.Count; i++)
            {
                // A leave day and an extended-hours day per doctor.
                db.ScheduleOverrides.Add(new Domain.Entities.Appointments.ScheduleOverride
                {
                    BranchId = ctx.BranchId,
                    DoctorId = doctorIds[i],
                    Date = today.AddDays(_random.Next(1, 30)),
                    IsAvailable = false,
                    Reason = i % 2 == 0 ? "Annual leave" : "Conference attendance",
                });
                db.ScheduleOverrides.Add(new Domain.Entities.Appointments.ScheduleOverride
                {
                    BranchId = ctx.BranchId,
                    DoctorId = doctorIds[i],
                    Date = today.AddDays(_random.Next(1, 30) + 30),
                    IsAvailable = true,
                    StartTime = new TimeOnly(17, 0),
                    EndTime = new TimeOnly(20, 0),
                    SlotMinutes = 10,
                    Reason = "Extended evening clinic",
                });
            }
            await db.SaveChangesAsync(ct);
        }
    }

    // ── Patient experience (portal, feedback, documents) ─────────────────────
    private async Task SeedPatientExperienceAsync(DemoContext ctx, CancellationToken ct)
    {
        if (!await db.PortalAccounts.AnyAsync(ct))
        {
            // Shared demo password; every account is forced to change it at first login.
            var passwordHash = hasher.Hash("Portal@123");
            var patientIds = await db.Patients.IgnoreQueryFilters()
                .OrderByDescending(p => p.Id).Select(p => p.Id).Take(25).ToListAsync(ct);
            foreach (var patientId in patientIds)
            {
                db.PortalAccounts.Add(new PortalAccount
                {
                    PatientId = patientId,
                    PasswordHash = passwordHash,
                    MustChangePassword = true,
                    LastLoginAt = _random.Next(0, 2) == 0 ? DateTimeOffset.UtcNow.AddDays(-_random.Next(1, 40)) : null,
                });
            }
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} demo portal accounts (password Portal@123, change forced at first login)",
                patientIds.Count);
        }

        if (!await db.Feedbacks.AnyAsync(ct))
        {
            var visits = await db.OpVisits.IgnoreQueryFilters()
                .OrderByDescending(v => v.Id).Take(60).ToListAsync(ct);
            (int Rating, string Comment)[] samples =
            [
                (5, "Excellent care. The consultant explained everything clearly and staff were courteous."),
                (4, "Good experience overall, though the billing counter queue was long."),
                (3, "Treatment was fine but waiting time to see the doctor was over an hour."),
                (5, "Nursing staff were very attentive during my stay. Thank you."),
                (2, "Difficult to find the department; signage needs improvement."),
                (4, "Clean facility and prompt pharmacy service."),
                (1, "Very long wait and no clear communication about the delay."),
                (5, "Smooth discharge process and clear follow-up instructions."),
            ];
            for (var i = 0; i < visits.Count; i++)
            {
                var (rating, comment) = samples[i % samples.Length];
                db.Feedbacks.Add(new Feedback
                {
                    PatientId = visits[i].PatientId,
                    OpVisitId = visits[i].Id,
                    Rating = rating,
                    Comments = comment,
                    SubmittedAt = DateTimeOffset.UtcNow.AddDays(-_random.Next(1, 180)),
                });
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.PatientDocuments.AnyAsync(ct))
        {
            var patientIds = await db.Patients.IgnoreQueryFilters()
                .OrderByDescending(p => p.Id).Select(p => p.Id).Take(20).ToListAsync(ct);
            foreach (var patientId in patientIds)
            {
                foreach (var type in new[] { PatientDocumentType.IdProof, PatientDocumentType.ConsentScan })
                {
                    db.PatientDocuments.Add(new PatientDocument
                    {
                        PatientId = patientId,
                        DocumentType = type,
                        FileName = $"{type.ToString().ToLowerInvariant()}.pdf",
                        // Demo rows reference a conventional path; no file is written to disk.
                        FilePath = $"demo/patients/{patientId}/{type.ToString().ToLowerInvariant()}.pdf",
                        ContentType = "application/pdf",
                        SizeBytes = _random.Next(50_000, 800_000),
                        Description = $"{type} uploaded at registration.",
                    });
                }
            }
            await db.SaveChangesAsync(ct);
        }
    }

    // ── Finance extras ───────────────────────────────────────────────────────
    private async Task SeedFinanceExtrasAsync(DemoContext ctx, CancellationToken ct)
    {
        if (await db.Payables.IgnoreQueryFilters().AnyAsync(ct))
        {
            return;
        }

        var grns = await db.Grns.IgnoreQueryFilters()
            .Include(g => g.Items)
            .OrderByDescending(g => g.Id).Take(20).ToListAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var index = 0;
        foreach (var grn in grns)
        {
            index++;
            var invoiceAmount = grn.Items.Sum(i => i.Amount);
            if (invoiceAmount <= 0)
            {
                invoiceAmount = _random.Next(10, 200) * 1000m;
            }
            // Spread across the aging buckets so the payables report has content in each.
            var status = (index % 3) switch
            {
                0 => PayableStatus.Open,
                1 => PayableStatus.PartiallyPaid,
                _ => PayableStatus.Paid,
            };
            var paid = status switch
            {
                PayableStatus.Paid => invoiceAmount,
                PayableStatus.PartiallyPaid => Math.Round(invoiceAmount * 0.4m, 2),
                _ => 0m,
            };
            var invoiceDate = today.AddDays(-_random.Next(5, 150));
            db.Payables.Add(new Domain.Entities.Finance.Payable
            {
                BranchId = grn.BranchId,
                VendorId = grn.VendorId,
                GrnId = grn.Id,
                InvoiceNumber = $"INV/{invoiceDate.Year}/{_random.Next(1000, 9999)}",
                InvoiceDate = invoiceDate,
                InvoiceAmount = Math.Round(invoiceAmount, 2),
                PaidAmount = paid,
                BalanceAmount = Math.Round(invoiceAmount - paid, 2),
                DueDate = invoiceDate.AddDays(30),
                Status = status,
            });
        }
        await db.SaveChangesAsync(ct);
    }

    private static readonly string[] DemoDiagnoses =
    [
        "Acute gastroenteritis with moderate dehydration",
        "Community-acquired pneumonia",
        "Type 2 diabetes mellitus with poor glycaemic control",
        "Acute coronary syndrome — NSTEMI",
        "Cholelithiasis for laparoscopic cholecystectomy",
        "Lower respiratory tract infection",
        "Chronic kidney disease stage 3",
        "Fracture of the shaft of femur",
        "Acute appendicitis",
        "Cerebrovascular accident — ischaemic",
        "Dengue fever with thrombocytopenia",
        "Urinary tract infection with pyelonephritis",
    ];

    /// <summary>Idempotent code-keyed insert for the simple Code/Name masters, mirroring DbSeeder's helper.</summary>
    private async Task EnsureAsync<TEntity>(
        DbSet<TEntity> set,
        (string Code, string Name)[] rows,
        Func<string, string, TEntity> factory,
        CancellationToken ct)
        where TEntity : class, ISimpleMaster
    {
        var existing = await set.Select(e => e.Code).ToListAsync(ct);
        foreach (var (code, name) in rows.Where(r => !existing.Contains(r.Code)))
        {
            set.Add(factory(code, name));
        }
        await db.SaveChangesAsync(ct);
    }
}
