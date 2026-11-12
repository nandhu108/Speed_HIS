using System.Text;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Reports;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.Tariffs;

public record TariffListItemDto(long Id, string Code, string Name, long? PatientCategoryId,
    string? PatientCategoryName, DateOnly EffectiveFrom, DateOnly? EffectiveTo,
    int ItemCount, bool IsActive);
public record TariffItemDto(long Id, long ServiceId, string ServiceCode, string ServiceName,
    long? RoomTypeId, string? RoomTypeName, decimal Rate);
public record TariffDetailDto(long Id, string Code, string Name, long? PatientCategoryId,
    DateOnly EffectiveFrom, DateOnly? EffectiveTo, bool IsActive, IReadOnlyList<TariffItemDto> Items);
/// <summary>RoomTypeId null prices the service the same regardless of room; set, this row only applies to that room type — a service can have both a room-agnostic row and one or more room-type-specific overrides in the same tariff (see BillMath.ResolveTariffRate).</summary>
public record SaveTariffItemRequest(long ServiceId, decimal Rate, long? RoomTypeId = null);
public record SaveTariffRequest(string Code, string Name, long? PatientCategoryId,
    DateOnly EffectiveFrom, DateOnly? EffectiveTo, IReadOnlyList<SaveTariffItemRequest> Items,
    bool IsActive = true);

public class SaveTariffRequestValidator : AbstractValidator<SaveTariffRequest>
{
    public SaveTariffRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EffectiveTo).GreaterThanOrEqualTo(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo is not null)
            .WithMessage("Effective-to must be on or after effective-from.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ServiceId).GreaterThan(0);
            item.RuleFor(i => i.Rate).GreaterThanOrEqualTo(0);
        });
        RuleFor(x => x.Items)
            .Must(items => items.Select(i => (i.ServiceId, i.RoomTypeId)).Distinct().Count() == items.Count)
            .WithMessage("A service (with the same room type, or the same room-agnostic row) appears more than once in the tariff.");
    }
}

public record TariffImportResultDto(int TariffsInserted, int TariffsUpdated, int ItemsUpserted, IReadOnlyList<string> Errors);

/// <summary>
/// Branch price lists with effective windows. Rate revision = create a new tariff
/// version effective from a later date; old versions are kept for history.
/// </summary>
public class TariffService(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<TariffListItemDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.Tariffs.AsNoTracking()
            .OrderByDescending(t => t.EffectiveFrom).ThenBy(t => t.Name)
            .Select(t => new TariffListItemDto(t.Id, t.Code, t.Name, t.PatientCategoryId,
                t.PatientCategory != null ? t.PatientCategory.Name : null,
                t.EffectiveFrom, t.EffectiveTo, t.Items.Count, t.IsActive))
            .ToListAsync(ct);

    public async Task<TariffDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var tariff = await db.Tariffs.AsNoTracking()
            .Include(t => t.Items).ThenInclude(i => i.Service)
            .Include(t => t.Items).ThenInclude(i => i.RoomType)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(Tariff), id);

        return new TariffDetailDto(tariff.Id, tariff.Code, tariff.Name, tariff.PatientCategoryId,
            tariff.EffectiveFrom, tariff.EffectiveTo, tariff.IsActive,
            tariff.Items.OrderBy(i => i.Service!.Name).ThenBy(i => i.RoomType != null ? i.RoomType.Name : "")
                .Select(i => new TariffItemDto(i.Id, i.ServiceId, i.Service!.Code, i.Service.Name, i.RoomTypeId, i.RoomType != null ? i.RoomType.Name : null, i.Rate))
                .ToList());
    }

    public async Task<long> CreateAsync(SaveTariffRequest request, CancellationToken ct = default)
    {
        await EnsureReferencesAsync(request, null, ct);

        var tariff = new Tariff
        {
            BranchId = RequireBranch(),
            Code = request.Code,
            Name = request.Name,
            PatientCategoryId = request.PatientCategoryId,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            IsActive = request.IsActive,
            Items = request.Items
                .Select(i => new TariffItem { ServiceId = i.ServiceId, RoomTypeId = i.RoomTypeId, Rate = i.Rate }).ToList(),
        };
        db.Tariffs.Add(tariff);
        await db.SaveChangesAsync(ct);
        return tariff.Id;
    }

    public async Task UpdateAsync(long id, SaveTariffRequest request, CancellationToken ct = default)
    {
        var tariff = await db.Tariffs
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(Tariff), id);
        await EnsureReferencesAsync(request, id, ct);

        tariff.Code = request.Code;
        tariff.Name = request.Name;
        tariff.PatientCategoryId = request.PatientCategoryId;
        tariff.EffectiveFrom = request.EffectiveFrom;
        tariff.EffectiveTo = request.EffectiveTo;
        tariff.IsActive = request.IsActive;

        var desired = request.Items.ToDictionary(i => (i.ServiceId, i.RoomTypeId), i => i.Rate);
        foreach (var row in tariff.Items.Where(i => !desired.ContainsKey((i.ServiceId, i.RoomTypeId))).ToList())
        {
            db.TariffItems.Remove(row);
            tariff.Items.Remove(row);
        }
        foreach (var row in tariff.Items)
        {
            row.Rate = desired[(row.ServiceId, row.RoomTypeId)];
        }
        foreach (var (key, rate) in desired.Where(d => tariff.Items.All(i => (i.ServiceId, i.RoomTypeId) != d.Key)))
        {
            tariff.Items.Add(new TariffItem { TariffId = id, ServiceId = key.ServiceId, RoomTypeId = key.RoomTypeId, Rate = rate });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var tariff = await db.Tariffs.Include(t => t.Items).FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(Tariff), id);

        foreach (var item in tariff.Items)
        {
            db.TariffItems.Remove(item);
        }
        db.Tariffs.Remove(tariff); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    private long RequireBranch() =>
        currentUser.BranchId ?? throw new DomainException("A branch context is required.");

    private async Task EnsureReferencesAsync(SaveTariffRequest request, long? selfId, CancellationToken ct)
    {
        if (await db.Tariffs.AnyAsync(t => t.Code == request.Code && t.Id != selfId, ct))
        {
            throw new DomainException($"Tariff code '{request.Code}' already exists.");
        }
        if (request.PatientCategoryId is not null
            && !await db.PatientCategories.AnyAsync(c => c.Id == request.PatientCategoryId, ct))
        {
            throw new DomainException("Patient category does not exist.");
        }
        var serviceIds = request.Items.Select(i => i.ServiceId).Distinct().ToList();
        var validServices = await db.Services.CountAsync(s => serviceIds.Contains(s.Id), ct);
        if (validServices != serviceIds.Count)
        {
            throw new DomainException("One or more services do not exist.");
        }
        var roomTypeIds = request.Items.Where(i => i.RoomTypeId is not null).Select(i => i.RoomTypeId!.Value).Distinct().ToList();
        if (roomTypeIds.Count > 0)
        {
            var validRoomTypes = await db.RoomTypes.CountAsync(r => roomTypeIds.Contains(r.Id), ct);
            if (validRoomTypes != roomTypeIds.Count)
            {
                throw new DomainException("One or more room types do not exist.");
            }
        }
    }

    // ── CSV import/export ─────────────────────────────────────────────────
    // One flat row per (Tariff, Service[, RoomType]) combination — export and import share
    // the exact same column shape so a round-trip export → edit in a spreadsheet → re-import
    // works without reformatting. Same "upsert by code, add-only within a save" posture as
    // IcdService.ImportCsvAsync, just two levels (tariff header + its items) instead of one.

    private const string ExportHeader = "TariffCode,TariffName,PatientCategoryCode,EffectiveFrom,EffectiveTo,ServiceCode,RoomTypeCode,Rate";

    public async Task<ReportResultDto> GetExportAsync(CancellationToken ct = default)
    {
        var rows = await db.Tariffs.AsNoTracking()
            .Include(t => t.Items).ThenInclude(i => i.Service)
            .Include(t => t.Items).ThenInclude(i => i.RoomType)
            .Include(t => t.PatientCategory)
            .OrderBy(t => t.Code).ThenBy(t => t.Items.Select(i => i.Service!.Code))
            .ToListAsync(ct);

        var flattened = rows.SelectMany(t => t.Items.OrderBy(i => i.Service!.Code).ThenBy(i => i.RoomType != null ? i.RoomType.Code : "")
            .Select(i => new Dictionary<string, object?>
            {
                ["tariffCode"] = t.Code,
                ["tariffName"] = t.Name,
                ["patientCategoryCode"] = t.PatientCategory?.Code,
                ["effectiveFrom"] = t.EffectiveFrom.ToString("yyyy-MM-dd"),
                ["effectiveTo"] = t.EffectiveTo?.ToString("yyyy-MM-dd"),
                ["serviceCode"] = i.Service!.Code,
                ["roomTypeCode"] = i.RoomType?.Code,
                ["rate"] = i.Rate,
            }))
            .ToList();

        return new ReportResultDto("Tariff List Export",
            [
                new("tariffCode", "TariffCode"), new("tariffName", "TariffName"), new("patientCategoryCode", "PatientCategoryCode"),
                new("effectiveFrom", "EffectiveFrom"), new("effectiveTo", "EffectiveTo"), new("serviceCode", "ServiceCode"),
                new("roomTypeCode", "RoomTypeCode"), new("rate", "Rate"),
            ],
            flattened);
    }

    /// <summary>
    /// Bulk upsert from CSV: TariffCode,TariffName,PatientCategoryCode,EffectiveFrom,EffectiveTo,
    /// ServiceCode,RoomTypeCode,Rate — optional header row, PatientCategoryCode/RoomTypeCode/
    /// EffectiveTo may be blank. Multiple rows sharing a TariffCode add items to the same
    /// tariff; the header fields (name/patient category/effective window) only apply from the
    /// first row referencing that code in this import.
    /// </summary>
    public async Task<TariffImportResultDto> ImportCsvAsync(Stream csv, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var rows = ParseCsv(csv, out var errors);
        if (rows.Count == 0)
        {
            throw new DomainException($"The CSV contains no valid rows. Expected: {ExportHeader}");
        }

        var patientCategoryCodes = rows.Where(r => r.PatientCategoryCode is not null).Select(r => r.PatientCategoryCode!).Distinct().ToList();
        var patientCategoriesByCode = await db.PatientCategories.AsNoTracking()
            .Where(c => patientCategoryCodes.Contains(c.Code)).ToDictionaryAsync(c => c.Code, c => c.Id, ct);
        var serviceCodes = rows.Select(r => r.ServiceCode).Distinct().ToList();
        var servicesByCode = await db.Services.AsNoTracking().Where(s => serviceCodes.Contains(s.Code)).ToDictionaryAsync(s => s.Code, s => s.Id, ct);
        var roomTypeCodes = rows.Where(r => r.RoomTypeCode is not null).Select(r => r.RoomTypeCode!).Distinct().ToList();
        var roomTypesByCode = await db.RoomTypes.AsNoTracking().Where(r => roomTypeCodes.Contains(r.Code)).ToDictionaryAsync(r => r.Code, r => r.Id, ct);

        var tariffCodes = rows.Select(r => r.TariffCode).Distinct().ToList();
        var existingTariffs = await db.Tariffs.Include(t => t.Items)
            .Where(t => tariffCodes.Contains(t.Code)).ToDictionaryAsync(t => t.Code, ct);

        int tariffsInserted = 0, tariffsUpdated = 0, itemsUpserted = 0;
        var headerApplied = new HashSet<string>();

        foreach (var row in rows)
        {
            if (!servicesByCode.TryGetValue(row.ServiceCode, out var serviceId))
            {
                errors.Add($"Line {row.LineNo}: service code '{row.ServiceCode}' not found — row skipped.");
                continue;
            }
            long? roomTypeId = null;
            if (row.RoomTypeCode is { } rtc)
            {
                if (!roomTypesByCode.TryGetValue(rtc, out var rtId))
                {
                    errors.Add($"Line {row.LineNo}: room type code '{rtc}' not found — row skipped.");
                    continue;
                }
                roomTypeId = rtId;
            }
            long? patientCategoryId = null;
            if (row.PatientCategoryCode is { } pcc)
            {
                if (!patientCategoriesByCode.TryGetValue(pcc, out var pcId))
                {
                    errors.Add($"Line {row.LineNo}: patient category code '{pcc}' not found — row skipped.");
                    continue;
                }
                patientCategoryId = pcId;
            }

            if (!existingTariffs.TryGetValue(row.TariffCode, out var tariff))
            {
                tariff = new Tariff
                {
                    BranchId = branchId, Code = row.TariffCode, Name = row.TariffName,
                    PatientCategoryId = patientCategoryId, EffectiveFrom = row.EffectiveFrom, EffectiveTo = row.EffectiveTo,
                };
                db.Tariffs.Add(tariff);
                existingTariffs[row.TariffCode] = tariff;
                headerApplied.Add(row.TariffCode);
                tariffsInserted++;
            }
            else if (headerApplied.Add(row.TariffCode))
            {
                tariff.Name = row.TariffName;
                tariff.PatientCategoryId = patientCategoryId;
                tariff.EffectiveFrom = row.EffectiveFrom;
                tariff.EffectiveTo = row.EffectiveTo;
                tariffsUpdated++;
            }

            var existingItem = tariff.Items.FirstOrDefault(i => i.ServiceId == serviceId && i.RoomTypeId == roomTypeId);
            if (existingItem is not null)
            {
                existingItem.Rate = row.Rate;
            }
            else
            {
                tariff.Items.Add(new TariffItem { ServiceId = serviceId, RoomTypeId = roomTypeId, Rate = row.Rate });
            }
            itemsUpserted++;
        }

        await db.SaveChangesAsync(ct);
        return new TariffImportResultDto(tariffsInserted, tariffsUpdated, itemsUpserted, errors);
    }

    private static List<(
        string TariffCode, string TariffName, string? PatientCategoryCode, DateOnly EffectiveFrom, DateOnly? EffectiveTo,
        string ServiceCode, string? RoomTypeCode, decimal Rate, int LineNo)> ParseCsv(Stream csv, out List<string> errors)
    {
        errors = [];
        var rows = new List<(string, string, string?, DateOnly, DateOnly?, string, string?, decimal, int)>();

        using var reader = new StreamReader(csv);
        var lineNo = 0;
        while (reader.ReadLine() is { } line)
        {
            lineNo++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = SplitCsvLine(line);
            if (lineNo == 1 && fields.Count >= 8 && fields[0].Trim().Equals("TariffCode", StringComparison.OrdinalIgnoreCase))
            {
                continue; // header row
            }

            if (fields.Count < 8)
            {
                errors.Add($"Line {lineNo}: expected 8 columns ({ExportHeader}), found {fields.Count}.");
                continue;
            }

            var tariffCode = fields[0].Trim();
            var tariffName = fields[1].Trim();
            if (tariffCode.Length == 0 || tariffName.Length == 0)
            {
                errors.Add($"Line {lineNo}: TariffCode and TariffName are required.");
                continue;
            }
            if (!DateOnly.TryParse(fields[3].Trim(), out var effectiveFrom))
            {
                errors.Add($"Line {lineNo}: EffectiveFrom '{fields[3]}' is not a valid date.");
                continue;
            }
            DateOnly? effectiveTo = null;
            if (!string.IsNullOrWhiteSpace(fields[4]))
            {
                if (!DateOnly.TryParse(fields[4].Trim(), out var parsedTo))
                {
                    errors.Add($"Line {lineNo}: EffectiveTo '{fields[4]}' is not a valid date.");
                    continue;
                }
                effectiveTo = parsedTo;
            }
            var serviceCode = fields[5].Trim();
            if (serviceCode.Length == 0)
            {
                errors.Add($"Line {lineNo}: ServiceCode is required.");
                continue;
            }
            if (!decimal.TryParse(fields[7].Trim(), out var rate) || rate < 0)
            {
                errors.Add($"Line {lineNo}: Rate '{fields[7]}' is not a valid non-negative number.");
                continue;
            }

            var patientCategoryCode = string.IsNullOrWhiteSpace(fields[2]) ? null : fields[2].Trim();
            var roomTypeCode = string.IsNullOrWhiteSpace(fields[6]) ? null : fields[6].Trim();

            rows.Add((tariffCode, tariffName, patientCategoryCode, effectiveFrom, effectiveTo, serviceCode, roomTypeCode, rate, lineNo));
        }

        return rows;
    }

    /// <summary>Minimal CSV field splitter with double-quote support — same shape as IcdService's, kept local since neither module shares a common CSV utility yet.</summary>
    private static List<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        fields.Add(current.ToString());
        return fields;
    }
}
