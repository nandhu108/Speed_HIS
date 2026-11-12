using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.Icd;

public record IcdCodeDto(long Id, string Code, string Description, string? Category, bool IsActive);
public record SaveIcdCodeRequest(string Code, string Description, string? Category, bool IsActive = true);
public record IcdImportResultDto(int Inserted, int Updated, int Skipped, IReadOnlyList<string> Errors);

public class SaveIcdCodeRequestValidator : AbstractValidator<SaveIcdCodeRequest>
{
    public SaveIcdCodeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Category).MaximumLength(200);
    }
}

/// <summary>ICD-10 diagnosis catalog (global) with CSV bulk import.</summary>
public class IcdService(IAppDbContext db)
{
    public async Task<PagedResult<IcdCodeDto>> GetPagedAsync(PagedQuery query, CancellationToken ct = default)
    {
        var codes = db.IcdCodes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            codes = codes.Where(c =>
                EF.Functions.ILike(c.Code, term) ||
                EF.Functions.ILike(c.Description, term) ||
                (c.Category != null && EF.Functions.ILike(c.Category, term)));
        }

        var totalCount = await codes.CountAsync(ct);
        var items = await codes
            .OrderBy(c => c.Code)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new IcdCodeDto(c.Id, c.Code, c.Description, c.Category, c.IsActive))
            .ToListAsync(ct);

        return PagedResult<IcdCodeDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<long> CreateAsync(SaveIcdCodeRequest request, CancellationToken ct = default)
    {
        await EnsureUniqueCodeAsync(request.Code, null, ct);
        var icd = new IcdCode
        {
            Code = request.Code,
            Description = request.Description,
            Category = request.Category,
            IsActive = request.IsActive,
        };
        db.IcdCodes.Add(icd);
        await db.SaveChangesAsync(ct);
        return icd.Id;
    }

    public async Task UpdateAsync(long id, SaveIcdCodeRequest request, CancellationToken ct = default)
    {
        var icd = await db.IcdCodes.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(IcdCode), id);
        await EnsureUniqueCodeAsync(request.Code, id, ct);

        icd.Code = request.Code;
        icd.Description = request.Description;
        icd.Category = request.Category;
        icd.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var icd = await db.IcdCodes.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(IcdCode), id);
        db.IcdCodes.Remove(icd); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Bulk upsert from CSV: <c>code,description[,category]</c>, optional header row.
    /// Existing codes are updated, new ones inserted — idempotent re-import.
    /// </summary>
    public async Task<IcdImportResultDto> ImportCsvAsync(Stream csv, CancellationToken ct = default)
    {
        var rows = ParseCsv(csv, out var errors);
        if (rows.Count == 0)
        {
            throw new DomainException("The CSV contains no valid rows. Expected: code,description[,category].");
        }

        var codes = rows.Select(r => r.Code).ToList();
        var existing = await db.IcdCodes
            .IgnoreQueryFilters()
            .Where(c => !c.IsDeleted && codes.Contains(c.Code))
            .ToDictionaryAsync(c => c.Code, ct);

        int inserted = 0, updated = 0;
        foreach (var row in rows)
        {
            if (existing.TryGetValue(row.Code, out var icd))
            {
                icd.Description = row.Description;
                icd.Category = row.Category;
                updated++;
            }
            else
            {
                db.IcdCodes.Add(new IcdCode
                {
                    Code = row.Code,
                    Description = row.Description,
                    Category = row.Category,
                });
                inserted++;
            }
        }

        await db.SaveChangesAsync(ct);
        return new IcdImportResultDto(inserted, updated, errors.Count, errors);
    }

    /// <summary>Parses the import CSV; exposed for unit tests.</summary>
    public static List<(string Code, string Description, string? Category)> ParseCsv(
        Stream csv, out List<string> errors)
    {
        errors = [];
        var rows = new List<(string, string, string?)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
            if (lineNo == 1 && fields.Count >= 2
                && fields[0].Trim().Equals("code", StringComparison.OrdinalIgnoreCase))
            {
                continue; // header row
            }

            if (fields.Count < 2 || string.IsNullOrWhiteSpace(fields[0]) || string.IsNullOrWhiteSpace(fields[1]))
            {
                errors.Add($"Line {lineNo}: expected at least code and description.");
                continue;
            }

            var code = fields[0].Trim().ToUpperInvariant();
            if (code.Length > 20)
            {
                errors.Add($"Line {lineNo}: code '{code}' exceeds 20 characters.");
                continue;
            }
            if (!seen.Add(code))
            {
                errors.Add($"Line {lineNo}: duplicate code '{code}' in file.");
                continue;
            }

            var description = fields[1].Trim();
            if (description.Length > 500)
            {
                description = description[..500];
            }
            var category = fields.Count > 2 && !string.IsNullOrWhiteSpace(fields[2])
                ? fields[2].Trim() : null;

            rows.Add((code, description, category));
        }

        return rows;
    }

    /// <summary>Minimal CSV field splitter with double-quote support.</summary>
    private static List<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
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

    private async Task EnsureUniqueCodeAsync(string code, long? selfId, CancellationToken ct)
    {
        if (await db.IcdCodes.AnyAsync(c => c.Code == code && c.Id != selfId, ct))
        {
            throw new DomainException($"ICD code '{code}' already exists.");
        }
    }
}
