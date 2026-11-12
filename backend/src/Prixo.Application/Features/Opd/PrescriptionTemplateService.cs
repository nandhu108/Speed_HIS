using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Opd;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Opd;

/// <summary>A doctor's saved prescription favorites, applied client-side to prefill a new prescription.</summary>
public class PrescriptionTemplateService(IAppDbContext db)
{
    public async Task<IReadOnlyList<PrescriptionTemplateDto>> GetByDoctorIdAsync(long doctorId, CancellationToken ct = default)
    {
        var templates = await db.PrescriptionTemplates.AsNoTracking()
            .Where(t => t.DoctorId == doctorId)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
        var templateIds = templates.Select(t => t.Id).ToList();

        var items = await db.PrescriptionTemplateItems.AsNoTracking()
            .Where(i => templateIds.Contains(i.PrescriptionTemplateId))
            .Select(i => new
            {
                i.PrescriptionTemplateId, i.Id, i.DrugId, DrugName = i.Drug!.Name,
                i.Dose, i.Frequency, i.Duration, i.Route, i.Instructions,
            })
            .ToListAsync(ct);

        return templates.Select(t => new PrescriptionTemplateDto(
            t.Id, t.DoctorId, t.Name,
            items.Where(i => i.PrescriptionTemplateId == t.Id)
                .Select(i => new PrescriptionTemplateItemDto(i.Id, i.DrugId, i.DrugName, i.Dose, i.Frequency, i.Duration, i.Route, i.Instructions))
                .ToList())).ToList();
    }

    public async Task<long> CreateAsync(SavePrescriptionTemplateRequest request, CancellationToken ct = default)
    {
        await EnsureReferencesAsync(request, ct);
        var template = new PrescriptionTemplate
        {
            DoctorId = request.DoctorId,
            Name = request.Name,
            Items = request.Items.Select(ToItemEntity).ToList(),
        };
        db.PrescriptionTemplates.Add(template);
        await db.SaveChangesAsync(ct);
        return template.Id;
    }

    public async Task UpdateAsync(long id, SavePrescriptionTemplateRequest request, CancellationToken ct = default)
    {
        var template = await db.PrescriptionTemplates.Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(PrescriptionTemplate), id);
        await EnsureReferencesAsync(request, ct);

        template.DoctorId = request.DoctorId;
        template.Name = request.Name;
        db.PrescriptionTemplateItems.RemoveRange(template.Items);
        template.Items = request.Items.Select(ToItemEntity).ToList();

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var template = await db.PrescriptionTemplates.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(PrescriptionTemplate), id);
        db.PrescriptionTemplates.Remove(template); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureReferencesAsync(SavePrescriptionTemplateRequest request, CancellationToken ct)
    {
        if (!await db.Doctors.AnyAsync(d => d.Id == request.DoctorId, ct))
        {
            throw new DomainException("Doctor does not exist.");
        }
        var drugIds = request.Items.Select(i => i.DrugId).Distinct().ToList();
        var validCount = await db.Drugs.CountAsync(d => drugIds.Contains(d.Id), ct);
        if (validCount != drugIds.Count)
        {
            throw new DomainException("One or more drugs do not exist.");
        }
    }

    private static PrescriptionTemplateItem ToItemEntity(SaveTemplateItemRequest request) => new()
    {
        DrugId = request.DrugId,
        Dose = request.Dose,
        Frequency = request.Frequency,
        Duration = request.Duration,
        Route = request.Route,
        Instructions = request.Instructions,
    };
}
