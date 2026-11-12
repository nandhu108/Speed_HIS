using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Lab;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Lab;

/// <summary>
/// Global lab test catalog: the test itself, its result parameters (panels carry several;
/// a single-parameter test still gets exactly one), and each parameter's reference ranges.
/// Update replaces parameters/ranges wholesale rather than diffing — same "PUT replaces
/// the list" idiom OPD uses for complaints/diagnoses, simpler than a two-level diff and
/// safe because nothing else references a parameter/range row by id across requests.
/// </summary>
public class LabTestService(IAppDbContext db)
{
    public async Task<IReadOnlyList<LabTestListItemDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.LabTests.AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new LabTestListItemDto(
                t.Id, t.Code, t.Name, t.Department != null ? t.Department.Name : null,
                t.SampleType!.Name, t.TatHours, t.IsPanel, t.IsActive))
            .ToListAsync(ct);

    public async Task<LabTestDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var test = await db.LabTests.AsNoTracking()
            .Include(t => t.Department)
            .Include(t => t.SampleType)
            .Include(t => t.Service)
            .Include(t => t.Parameters).ThenInclude(p => p.ReferenceRanges)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(LabTest), id);
        return ToDetailDto(test);
    }

    public async Task<long> CreateAsync(SaveLabTestRequest request, CancellationToken ct = default)
    {
        await EnsureReferencesAsync(request, null, ct);

        var test = new LabTest
        {
            Code = request.Code,
            Name = request.Name,
            DepartmentId = request.DepartmentId,
            SampleTypeId = request.SampleTypeId,
            ServiceId = request.ServiceId,
            Method = request.Method,
            TatHours = request.TatHours,
            IsPanel = request.IsPanel,
            IsActive = request.IsActive,
            Parameters = request.Parameters.Select(ToParameterEntity).ToList(),
        };
        db.LabTests.Add(test);
        await db.SaveChangesAsync(ct);
        return test.Id;
    }

    public async Task UpdateAsync(long id, SaveLabTestRequest request, CancellationToken ct = default)
    {
        var test = await db.LabTests.Include(t => t.Parameters).ThenInclude(p => p.ReferenceRanges)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(LabTest), id);
        await EnsureReferencesAsync(request, id, ct);

        test.Code = request.Code;
        test.Name = request.Name;
        test.DepartmentId = request.DepartmentId;
        test.SampleTypeId = request.SampleTypeId;
        test.ServiceId = request.ServiceId;
        test.Method = request.Method;
        test.TatHours = request.TatHours;
        test.IsPanel = request.IsPanel;
        test.IsActive = request.IsActive;

        foreach (var parameter in test.Parameters.ToList())
        {
            db.ReferenceRanges.RemoveRange(parameter.ReferenceRanges);
            db.LabTestParameters.Remove(parameter);
        }
        test.Parameters.Clear();
        foreach (var parameter in request.Parameters.Select(ToParameterEntity))
        {
            test.Parameters.Add(parameter);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var test = await db.LabTests.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(LabTest), id);
        if (await db.LabOrderItems.AnyAsync(i => i.LabTestId == id, ct))
        {
            throw new DomainException("This test has already been ordered and cannot be deleted.");
        }

        db.LabTests.Remove(test); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureReferencesAsync(SaveLabTestRequest request, long? selfId, CancellationToken ct)
    {
        if (await db.LabTests.AnyAsync(t => t.Code == request.Code && t.Id != selfId, ct))
        {
            throw new DomainException($"Lab test code '{request.Code}' already exists.");
        }
        if (!await db.SampleTypes.AnyAsync(s => s.Id == request.SampleTypeId, ct))
        {
            throw new NotFoundException("SampleType", request.SampleTypeId);
        }
        if (request.Parameters.Select(p => p.Code).Distinct().Count() != request.Parameters.Count)
        {
            throw new DomainException("A parameter code appears more than once.");
        }
    }

    private static LabTestParameter ToParameterEntity(SaveLabTestParameterRequest request) => new()
    {
        Code = request.Code,
        Name = request.Name,
        Unit = request.Unit,
        ValueType = request.ValueType,
        DropdownOptions = request.DropdownOptions,
        SortOrder = request.SortOrder,
        ReferenceRanges = request.ReferenceRanges.Select(r => new ReferenceRange
        {
            Sex = r.Sex,
            MinAgeYears = r.MinAgeYears,
            MaxAgeYears = r.MaxAgeYears,
            NormalLow = r.NormalLow,
            NormalHigh = r.NormalHigh,
            CriticalLow = r.CriticalLow,
            CriticalHigh = r.CriticalHigh,
            TextNormalValue = r.TextNormalValue,
        }).ToList(),
    };

    private static LabTestDetailDto ToDetailDto(LabTest t) => new(
        t.Id, t.Code, t.Name, t.DepartmentId, t.Department?.Name, t.SampleTypeId, t.SampleType!.Name,
        t.ServiceId, t.Service?.Name, t.Method, t.TatHours, t.IsPanel, t.IsActive,
        t.Parameters.OrderBy(p => p.SortOrder).Select(p => new LabTestParameterDto(
            p.Id, p.Code, p.Name, p.Unit, p.ValueType, p.DropdownOptions, p.SortOrder,
            p.ReferenceRanges.Select(r => new ReferenceRangeDto(
                r.Id, r.Sex, r.MinAgeYears, r.MaxAgeYears, r.NormalLow, r.NormalHigh,
                r.CriticalLow, r.CriticalHigh, r.TextNormalValue)).ToList())).ToList());
}
