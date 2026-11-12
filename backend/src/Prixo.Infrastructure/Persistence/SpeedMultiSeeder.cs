using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Ot;
using Prixo.Domain.Enums;

namespace Prixo.Infrastructure.Persistence;

/// <summary>
/// Real (non-demo) reference data for SPEED MULTI SPECIALITY HOSPITAL, taken from the hospital's
/// filled "HIS Implementation Data Collection (20 Beds)" workbook and registration certificates.
/// Unlike <see cref="DemoDataSeeder"/> this invents nothing and is safe in any environment.
///
/// <para>Rule applied throughout: a value the hospital did not supply is NOT guessed. Blank
/// prices create no row (CT, MRI), unknown doctor registration numbers get an obvious
/// <c>PENDING-</c> placeholder, and unknown bed rates stay 0 — all listed in
/// docs/SPEED_MULTI_ONBOARDING.md as items to confirm with the hospital.</para>
///
/// <para>Idempotent: every row is keyed by a stable code/username and only added when missing,
/// so re-running never duplicates and never overwrites an admin's later edits. Pharmacy is out
/// of scope for this hospital, so no store, drug or pharmacy department is created.</para>
/// </summary>
public class SpeedMultiSeeder(
    AppDbContext db,
    IPasswordHasherService hasher,
    IConfiguration configuration,
    ILogger<SpeedMultiSeeder> logger)
{
    public const string BranchCode = "SPEED_MULTI";
    private static readonly DateOnly EffectiveFrom = new(2026, 1, 1);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var branch = await SeedBranchAsync(ct);
        var departments = await SeedDepartmentsAsync(ct);
        var specialties = await SeedSpecialtiesAsync(ct);
        await SeedDoctorsAsync(branch, departments, specialties, ct);
        await SeedAdminAccessAsync(branch, ct);
        await SeedWardsAndBedsAsync(branch, ct);
        await SeedServicesAndTariffAsync(branch, departments, ct);
        await SeedTheatreAsync(branch, ct);
        logger.LogInformation("Speed Multi seed completed for branch {Branch}", BranchCode);
    }

    // ── Branch ────────────────────────────────────────────────────────────────
    private async Task<Branch> SeedBranchAsync(CancellationToken ct)
    {
        var branch = await db.Branches.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.Code == BranchCode, ct);
        if (branch is not null)
        {
            return branch;
        }
        branch = new Branch
        {
            Code = BranchCode,
            Name = "Speed Multi Speciality Hospital",
            Address = "Kundrathur Main Road, Padmavathi Nagar, Kamatchiamman Nagar, Mangadu",
            City = "Chennai",
            State = "Tamil Nadu",
            Pincode = "600122",
            Phone = "+91 80159 32591",
            // GSTIN of Sai Speed Medical Centre Pvt Ltd per the GST REG-06 certificate in the data
            // folder (the workbook's own GSTIN cell was left blank).
            GstNumber = "33AAPCS5975A1Z6",
            ReportHeader = "Speed Multi Speciality Hospital (a unit of Sai Speed Medical Centre Pvt Ltd)",
        };
        db.Branches.Add(branch);
        await db.SaveChangesAsync(ct);
        return branch;
    }

    // ── Departments (workbook "Department Master"; Pharmacy deliberately omitted) ──
    private static readonly (string Code, string Name, string Type)[] DepartmentRows =
    [
        ("DEP001", "General Medicine", "Clinical"), ("DEP002", "Paediatrics", "Clinical"),
        ("DEP003", "Orthopaedics", "Clinical"), ("DEP004", "Obstetrics & Gynaecology", "Clinical"),
        ("DEP005", "Emergency", "Clinical"), ("DEP006", "Laboratory", "Diagnostic"),
        ("DEP007", "Radiology", "Diagnostic"), ("DEP009", "Cardiology", "Clinical"),
        ("DEP010", "General Surgery", "Clinical"), ("DEP011", "Cardio Vascular Thoracic Surgery", "Clinical"),
        ("DEP012", "Surgical Oncology", "Clinical"), ("DEP013", "Medical Oncology", "Clinical"),
        ("DEP014", "Nephrology", "Clinical"), ("DEP015", "Plastic & Reconstructive Surgery", "Clinical"),
        ("DEP016", "ENT", "Clinical"), ("DEP017", "Pulmonology", "Clinical"),
        ("DEP018", "Oral & Maxillofacial Surgery", "Clinical"), ("DEP019", "Physiotherapy", "Clinical"),
        ("DEP020", "Critical Care & Anaesthesiology", "Clinical"), ("DEP021", "Medical Gastroenterology", "Clinical"),
        ("DEP022", "Urology", "Clinical"),
    ];

    /// <summary>Departments are global. Match an existing row by code or by name before adding, so a
    /// department another branch already has (e.g. "Radiology") is reused rather than duplicated.</summary>
    private async Task<Dictionary<string, long>> SeedDepartmentsAsync(CancellationToken ct)
    {
        var existing = await db.Departments.IgnoreQueryFilters().ToListAsync(ct);
        var result = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var (code, name, type) in DepartmentRows)
        {
            var row = existing.FirstOrDefault(d => d.Code == code)
                ?? existing.FirstOrDefault(d => string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase));
            // The workbook's "Diagnostic" units (Laboratory, Radiology) are support departments that
            // never take OP/IP patients directly, so they are NonClinical; everything else is Clinical.
            var departmentType = type == "Clinical" ? DepartmentType.Clinical : DepartmentType.NonClinical;
            if (row is null)
            {
                row = new Department { Code = code, Name = name, Description = $"{type} department", Type = departmentType };
                db.Departments.Add(row);
                existing.Add(row);
            }
            else
            {
                row.Type = departmentType; // idempotent: re-running corrects rows seeded before Type existed
            }
            result[name] = 0; // filled after save
        }
        await db.SaveChangesAsync(ct);
        foreach (var (code, name, _) in DepartmentRows)
        {
            var row = existing.First(d => d.Code == code || string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase));
            result[name] = row.Id;
        }
        return result;
    }

    // ── Specialties ───────────────────────────────────────────────────────────
    private async Task<Dictionary<string, long>> SeedSpecialtiesAsync(CancellationToken ct)
    {
        var names = DoctorRows.Select(d => d.Specialization).Distinct().ToList();
        var existing = await db.Specialties.IgnoreQueryFilters().ToListAsync(ct);
        for (var i = 0; i < names.Count; i++)
        {
            var name = names[i];
            if (existing.Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }
            // Short positional code (column is varchar(30)); DoctorRows is static so the index is stable.
            var row = new Specialty { Code = $"SM-SP{i + 1:00}", Name = name };
            db.Specialties.Add(row);
            existing.Add(row);
        }
        await db.SaveChangesAsync(ct);
        return existing.GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);
    }

    // ── Doctors (workbook "Doctor Master") ────────────────────────────────────
    private static readonly (string Id, string Name, string? Department, string? Qualification, string Specialization)[] DoctorRows =
    [
        ("DR001", "Dr K Vinayak Senthil", "Cardio Vascular Thoracic Surgery", "MBBS, M.Ch (CVTS), MRCS (UK), DNB (Gen Surgery), FIAGES, FAIS, FMAS, FLS (SAGES) USA", "Cardio Vascular Thoracic Surgery"),
        ("DR002", "Dr S Manimaran", "Cardio Vascular Thoracic Surgery", "MBBS, MS (Gen), M.Ch (CTVS)", "Cardio Vascular Thoracic Surgery"),
        ("DR003", "Dr Saravanan", "Cardiology", "MBBS, MD (Gen), DM (Cardiology)", "Cardiology"),
        ("DR004", "Prof. G. Gopu", "Surgical Oncology", null, "Surgical Oncology"),
        ("DR005", "Dr G Vijay", "Medical Oncology", "MBBS, MD (Paediatrics) (AIIMS), DNB (Paed), DM (Med Oncology)", "Medical Oncology"),
        ("DR006", "Dr S Sujit", "Nephrology", "MD, DM (Nephrology)", "Nephrology"),
        ("DR007", "Dr A Sivakumar", "Plastic & Reconstructive Surgery", "MS, M.Ch (Plastic)", "Plastic & Reconstructive Surgery"),
        ("DR008", "Dr Prathish Kumar", "General Medicine", "MBBS, MD (GM), FDM, FICM", "Diabetology & General Medicine"),
        ("DR009", "Dr Chandramouli", "ENT", "MS (ENT)", "ENT Surgery"),
        ("DR010", "Dr K Vishali", "Pulmonology", "MBBS, MD, DNB (Respiratory Medicine)", "Pulmonology"),
        ("DR011", "Dr Yazar Mohamed", "Oral & Maxillofacial Surgery", null, "Oral & Maxillofacial Surgery"),
        ("DR012", "Dr S Parthasarathy", "Physiotherapy", "BPT, PGDFN, MIAP", "Physiotherapy"),
        ("DR013", "Dr Jeff Walter Rajadurai", "Orthopaedics", "MS (Ortho), DNB (Ortho), M.Ch Ortho (UK), Dip FIFA SM", "Orthopaedics"),
        ("DR014", "Dr N Sathyan", "Critical Care & Anaesthesiology", "MBBS, MD, DNB", "Critical Care & Anaesthesiology"),
        ("DR015", "Dr S M Sivaraj", "Medical Gastroenterology", "MS (Gen), M.Ch (Gastro)", "Medical Gastroenterology"),
        ("DR016", "Dr Pavan Arcot Prakash", "Urology", "MBBS, MS, M.Ch (Urology), DNB (Urology)", "Urology"),
        // Department and specialization blank in the workbook — left unmapped, not inferred.
        ("DR017", "Prof. Dr. S Sampath Kumari", null, "MD, DGO, FC Dia, FICOG", "General"),
    ];

    private async Task SeedDoctorsAsync(
        Branch branch, Dictionary<string, long> departments, Dictionary<string, long> specialties, CancellationToken ct)
    {
        var roleIds = await db.Roles.IgnoreQueryFilters()
            .Where(r => r.Code == "DOCTOR" || r.Code == "DOCTOR_PORTAL")
            .Select(r => r.Id).ToListAsync(ct);
        if (roleIds.Count == 0)
        {
            throw new InvalidOperationException("DOCTOR role missing — DbSeeder must run before SpeedMultiSeeder.");
        }

        var password = configuration["Seed:SpeedMultiDoctorPassword"]
            ?? configuration["Seed:AdminPassword"] ?? "Admin@123";
        var registrationNos = await db.Doctors.IgnoreQueryFilters().Select(d => d.RegistrationNo).ToListAsync(ct);

        foreach (var (id, name, department, qualification, specialization) in DoctorRows)
        {
            var username = "dr." + Slug(name).Replace("-", ".");
            var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Username == username, ct);
            if (user is null)
            {
                user = new User
                {
                    Username = username,
                    FullName = name,
                    PasswordHash = hasher.Hash(password),
                    EmployeeCode = id,
                    DefaultBranchId = branch.Id,
                    MustChangePassword = true,
                    UserRoles = roleIds.Select(r => new UserRole { RoleId = r }).ToList(),
                    UserBranches = [new UserBranch { BranchId = branch.Id }],
                };
                db.Users.Add(user);
                await db.SaveChangesAsync(ct);
            }

            if (await db.Doctors.IgnoreQueryFilters().AnyAsync(d => d.UserId == user.Id, ct))
            {
                continue;
            }
            var registrationNo = $"PENDING-{id}"; // medical-council number not supplied by the hospital
            if (registrationNos.Contains(registrationNo))
            {
                continue;
            }
            var doctor = new Doctor
            {
                UserId = user.Id,
                RegistrationNo = registrationNo,
                Qualification = qualification,
                SpecialtyId = specialties.TryGetValue(specialization, out var sid) ? sid : null,
                // Per-doctor fees are not mapped yet in the workbook ("doctor-wise mapping pending").
                ConsultationFee = 0,
                FollowUpFee = 0,
            };
            if (department is not null && departments.TryGetValue(department, out var did))
            {
                doctor.DoctorDepartments.Add(new DoctorDepartment { DepartmentId = did });
            }
            db.Doctors.Add(doctor);
            registrationNos.Add(registrationNo);
            await db.SaveChangesAsync(ct);
        }
    }

    /// <summary>Lets the existing admin sign in to the new branch.</summary>
    private async Task SeedAdminAccessAsync(Branch branch, CancellationToken ct)
    {
        var admin = await db.Users.IgnoreQueryFilters().Include(u => u.UserBranches)
            .FirstOrDefaultAsync(u => u.Username == "admin", ct);
        if (admin is null || admin.UserBranches.Any(b => b.BranchId == branch.Id))
        {
            return;
        }
        admin.UserBranches.Add(new UserBranch { BranchId = branch.Id });
        await db.SaveChangesAsync(ct);
    }

    // ── Wards, rooms, beds (workbook "Bed & Ward Master" — 20 proposed beds) ───
    private async Task SeedWardsAndBedsAsync(Branch branch, CancellationToken ct)
    {
        var bid = branch.Id;
        var building = await db.Buildings.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.BranchId == bid && b.Code == "MAIN", ct);
        if (building is null)
        {
            building = new Building { BranchId = bid, Code = "MAIN", Name = "Main Building" };
            db.Buildings.Add(building);
            await db.SaveChangesAsync(ct);
        }
        var floor = await db.Floors.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.BranchId == bid && f.Code == "F1", ct);
        if (floor is null)
        {
            floor = new Floor { BranchId = bid, Code = "F1", Name = "Inpatient Floor", BuildingId = building.Id };
            db.Floors.Add(floor);
            await db.SaveChangesAsync(ct);
        }

        // (wardCode, wardName, roomCode, roomType, isIcu, bedCodePrefix, count, ratePerDay)
        // Only ICU has a stated rate (₹7,500/day). General/Semi-Private/Private rates were not
        // provided, so they stay 0 until the hospital confirms them.
        (string Ward, string WardName, string Room, string Type, string TypeName, bool Icu, string Prefix, int Count, decimal Rate)[] layout =
        [
            ("GW", "General Ward", "GW-01", "GENERAL", "General", false, "GW", 10, 0m),
            ("SP", "Semi-Private", "SP-01", "SEMIPVT", "Semi-Private", false, "SP", 4, 0m),
            ("PW", "Private Ward", "PW-01", "PRIVATE", "Private", false, "PW", 4, 0m),
            ("ICU", "ICU", "ICU-01", "ICU", "ICU", true, "ICU", 2, 7500m),
        ];

        foreach (var l in layout)
        {
            var roomType = await db.RoomTypes.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.BranchId == bid && r.Code == l.Type, ct);
            if (roomType is null)
            {
                roomType = new RoomType { BranchId = bid, Code = l.Type, Name = l.TypeName, BedChargePerDay = l.Rate };
                db.RoomTypes.Add(roomType);
                await db.SaveChangesAsync(ct);
            }
            if (l.Rate > 0 && !await db.RoomTypeCharges.IgnoreQueryFilters().AnyAsync(c => c.BranchId == bid && c.RoomTypeId == roomType.Id, ct))
            {
                db.RoomTypeCharges.Add(new RoomTypeCharge { BranchId = bid, RoomTypeId = roomType.Id, Rate = l.Rate, EffectiveFrom = EffectiveFrom });
                await db.SaveChangesAsync(ct);
            }

            var ward = await db.Wards.IgnoreQueryFilters().FirstOrDefaultAsync(w => w.BranchId == bid && w.Code == l.Ward, ct);
            if (ward is null)
            {
                ward = new Ward { BranchId = bid, Code = l.Ward, Name = l.WardName, FloorId = floor.Id, IsIcu = l.Icu };
                db.Wards.Add(ward);
                await db.SaveChangesAsync(ct);
            }
            var room = await db.Rooms.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.BranchId == bid && r.Code == l.Room, ct);
            if (room is null)
            {
                room = new Room { BranchId = bid, Code = l.Room, Name = l.Room, WardId = ward.Id, RoomTypeId = roomType.Id };
                db.Rooms.Add(room);
                await db.SaveChangesAsync(ct);
            }
            var bedCodes = await db.Beds.IgnoreQueryFilters().Where(b => b.BranchId == bid && b.RoomId == room.Id)
                .Select(b => b.Code).ToListAsync(ct);
            for (var i = 1; i <= l.Count; i++)
            {
                var code = $"{l.Prefix}{i:000}";
                if (!bedCodes.Contains(code))
                {
                    db.Beds.Add(new Bed { BranchId = bid, Code = code, RoomId = room.Id });
                }
            }
            await db.SaveChangesAsync(ct);
        }
    }

    // ── Services + General tariff (workbook price lists + "Other Charges") ─────
    // (code, name, categoryCode, categoryName, departmentName, rate). Rows with no price in the
    // workbook (CT Scan, MRI, surgeon fees, equipment, lab prices) are intentionally absent.
    private static readonly (string Code, string Name, string Cat, string CatName, string? Dept, decimal Rate)[] ServiceRows =
    [
        ("SM-REG", "Registration Fee (all patients)", "SM-CONSULT", "Consultation & Registration", null, 100m),
        ("SM-OPC-VISIT", "Visiting Consultant - Standard Consultation", "SM-CONSULT", "Consultation & Registration", null, 600m),
        ("SM-OPC-SPEC", "Specialist - Standard Consultation", "SM-CONSULT", "Consultation & Registration", null, 1200m),
        ("SM-OPC-DMO", "Duty Medical Officer Consultation", "SM-CONSULT", "Consultation & Registration", "Emergency", 200m),
        ("SM-OPC-MD", "MD Consultation (General Medicine)", "SM-CONSULT", "Consultation & Registration", "General Medicine", 200m),

        ("SM-RAD-XRAY", "X-Ray", "SC-RADIOLOGY", "Radiology", "Radiology", 500m),
        ("SM-RAD-USG", "Ultra Sound", "SC-RADIOLOGY", "Radiology", "Radiology", 1500m),
        ("SM-CAR-ECHO", "Echo", "SM-CARDIO", "Cardiology Diagnostics", "Cardiology", 2000m),
        ("SM-CAR-ECG", "ECG", "SM-CARDIO", "Cardiology Diagnostics", "Cardiology", 300m),
        ("SM-CAR-TMT", "Treadmill Test (TMT)", "SM-CARDIO", "Cardiology Diagnostics", "Cardiology", 2000m),

        ("SM-PRC-INJ", "Injection", "SM-PROCEDURE", "OP Procedures", null, 120m),
        ("SM-PRC-NEB", "Nebulizer", "SM-PROCEDURE", "OP Procedures", null, 150m),
        ("SM-PRC-SUT1", "Suturing - simple", "SM-PROCEDURE", "OP Procedures", null, 150m),
        ("SM-PRC-SUT2", "Suturing - tier 2", "SM-PROCEDURE", "OP Procedures", null, 500m),
        ("SM-PRC-SUT3", "Suturing - tier 3", "SM-PROCEDURE", "OP Procedures", null, 1000m),
        ("SM-PRC-SUT4", "Suturing - tier 4", "SM-PROCEDURE", "OP Procedures", null, 1500m),
        ("SM-PRC-DRSMIN", "Dressing - minor", "SM-PROCEDURE", "OP Procedures", null, 300m),
        ("SM-PRC-DRSMAJ", "Dressing - major", "SM-PROCEDURE", "OP Procedures", null, 900m),
        ("SM-PRC-OBS2", "Observation charges (up to 2 hours)", "SM-PROCEDURE", "OP Procedures", null, 100m),
        ("SM-PRC-OBS2P", "Observation charges (more than 2 hours)", "SM-PROCEDURE", "OP Procedures", null, 500m),
        ("SM-PRC-MEDCERT", "Medical certificate", "SM-PROCEDURE", "OP Procedures", null, 300m),

        ("SM-IP-REG", "IP Registration charge (medical record maintenance) - one time", "SM-IPCHARGE", "IP Charges", null, 500m),
        ("SM-IP-DIET", "Diet charges (per day)", "SM-IPCHARGE", "IP Charges", null, 800m),
        ("SM-IP-HK", "Housekeeping charges (per day)", "SM-IPCHARGE", "IP Charges", null, 750m),
        ("SM-IP-CONS", "Consumables & disposable charges (per day)", "SM-IPCHARGE", "IP Charges", null, 250m),
        ("SM-IP-NURSE", "Nurse charges (per day)", "SM-IPCHARGE", "IP Charges", null, 850m),
        ("SM-IP-DMO", "Duty doctor charges (per day)", "SM-IPCHARGE", "IP Charges", null, 1000m),
        ("SM-IP-PHYSIO", "Physiotherapy charges", "SM-IPCHARGE", "IP Charges", "Physiotherapy", 2000m),

        ("SM-OT-HOUR", "OT charge (per hour)", "SM-OT", "OT Charges", null, 8000m),
        ("SM-OT-ADDON", "OT charge add-on (more than 3 hours)", "SM-OT", "OT Charges", null, 1000m),
        ("SM-OT-ASST1", "OT assistant charges - tier 1", "SM-OT", "OT Charges", null, 10000m),
        ("SM-OT-ASST2", "OT assistant charges - tier 2", "SM-OT", "OT Charges", null, 5000m),
    ];

    private async Task SeedServicesAndTariffAsync(Branch branch, Dictionary<string, long> departments, CancellationToken ct)
    {
        var categories = await db.ServiceCategories.IgnoreQueryFilters().ToDictionaryAsync(c => c.Code, c => c.Id, ct);
        foreach (var (cat, catName) in ServiceRows.Select(r => (r.Cat, r.CatName)).Distinct())
        {
            if (!categories.ContainsKey(cat))
            {
                var row = new ServiceCategory { Code = cat, Name = catName };
                db.ServiceCategories.Add(row);
                await db.SaveChangesAsync(ct);
                categories[cat] = row.Id;
            }
        }

        var services = await db.Services.IgnoreQueryFilters().ToDictionaryAsync(s => s.Code, ct);
        foreach (var (code, name, cat, _, dept, rate) in ServiceRows)
        {
            if (services.ContainsKey(code))
            {
                continue;
            }
            var service = new Service
            {
                Code = code, Name = name, ServiceCategoryId = categories[cat],
                DepartmentId = dept is not null && departments.TryGetValue(dept, out var d) ? d : null,
                IsTaxable = false, // healthcare services are GST-exempt
                DefaultRate = rate,
            };
            db.Services.Add(service);
            services[code] = service;
        }
        await db.SaveChangesAsync(ct);

        // One branch-wide General tariff (no category), so every patient category resolves a price
        // until the hospital supplies its insurance/corporate/staff/VIP policies (not provided).
        var tariff = await db.Tariffs.IgnoreQueryFilters().Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.BranchId == branch.Id && t.Code == "SM-GENERAL", ct);
        if (tariff is null)
        {
            tariff = new Tariff { BranchId = branch.Id, Code = "SM-GENERAL", Name = "Speed Multi - Standard Tariff", EffectiveFrom = EffectiveFrom };
            db.Tariffs.Add(tariff);
        }
        var priced = tariff.Items.Select(i => i.ServiceId).ToHashSet();
        foreach (var (code, _, _, _, _, rate) in ServiceRows)
        {
            var service = services[code];
            if (!priced.Contains(service.Id))
            {
                tariff.Items.Add(new TariffItem { ServiceId = service.Id, Rate = rate });
            }
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task SeedTheatreAsync(Branch branch, CancellationToken ct)
    {
        if (await db.Theatres.IgnoreQueryFilters().AnyAsync(t => t.BranchId == branch.Id && t.Code == "OT1", ct))
        {
            return;
        }
        db.Theatres.Add(new Theatre { BranchId = branch.Id, Code = "OT1", Name = "Main Operation Theatre" });
        await db.SaveChangesAsync(ct);
    }

    private static string Slug(string value)
    {
        var chars = value.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var slug = new string(chars);
        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }
        return slug.Trim('-').Replace("dr-", "").Replace("prof-", "");
    }
}
