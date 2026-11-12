using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Rad;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Rad;

/// <summary>Global radiology test catalog — same posture as Lab's LabTestService, minus the parameter/reference-range machinery (radiology results are narrative, not per-parameter values).</summary>
public class RadiologyTestService(IAppDbContext db)
{
    public async Task<IReadOnlyList<RadiologyTestListItemDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.RadiologyTests.AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new RadiologyTestListItemDto(
                t.Id, t.Code, t.Name, t.ModalityId, t.Modality!.Name, t.BodyPart, t.RequiresContrast, t.TatHours, t.IsActive))
            .ToListAsync(ct);

    public async Task<RadiologyTestDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var test = await db.RadiologyTests.AsNoTracking()
            .Include(t => t.Modality)
            .Include(t => t.Service)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(RadiologyTest), id);
        return ToDetailDto(test);
    }

    public async Task<long> CreateAsync(SaveRadiologyTestRequest request, CancellationToken ct = default)
    {
        await EnsureReferencesAsync(request, null, ct);
        var test = new RadiologyTest
        {
            Code = request.Code, Name = request.Name, ModalityId = request.ModalityId, BodyPart = request.BodyPart,
            RequiresContrast = request.RequiresContrast, PrepInstructions = request.PrepInstructions,
            ServiceId = request.ServiceId, TatHours = request.TatHours, IsActive = request.IsActive,
        };
        db.RadiologyTests.Add(test);
        await db.SaveChangesAsync(ct);
        return test.Id;
    }

    public async Task UpdateAsync(long id, SaveRadiologyTestRequest request, CancellationToken ct = default)
    {
        var test = await db.RadiologyTests.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(RadiologyTest), id);
        await EnsureReferencesAsync(request, id, ct);

        test.Code = request.Code;
        test.Name = request.Name;
        test.ModalityId = request.ModalityId;
        test.BodyPart = request.BodyPart;
        test.RequiresContrast = request.RequiresContrast;
        test.PrepInstructions = request.PrepInstructions;
        test.ServiceId = request.ServiceId;
        test.TatHours = request.TatHours;
        test.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var test = await db.RadiologyTests.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(RadiologyTest), id);
        if (await db.RadOrderItems.AnyAsync(i => i.RadiologyTestId == id, ct))
        {
            throw new DomainException("This test has already been ordered and cannot be deleted.");
        }

        db.RadiologyTests.Remove(test); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureReferencesAsync(SaveRadiologyTestRequest request, long? selfId, CancellationToken ct)
    {
        if (await db.RadiologyTests.AnyAsync(t => t.Code == request.Code && t.Id != selfId, ct))
        {
            throw new DomainException($"Radiology test code '{request.Code}' already exists.");
        }
        if (!await db.Modalities.AnyAsync(m => m.Id == request.ModalityId, ct))
        {
            throw new NotFoundException("Modality", request.ModalityId);
        }
    }

    private static RadiologyTestDetailDto ToDetailDto(RadiologyTest t) => new(
        t.Id, t.Code, t.Name, t.ModalityId, t.Modality!.Name, t.BodyPart, t.RequiresContrast, t.PrepInstructions,
        t.ServiceId, t.Service?.Name, t.TatHours, t.IsActive);
}
