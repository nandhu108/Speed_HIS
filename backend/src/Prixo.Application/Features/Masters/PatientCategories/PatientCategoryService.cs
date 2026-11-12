using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.PatientCategories;

/// <summary>"Patient-type colors" — bespoke (not ISimpleMaster) since it carries ColorHex,
/// same reasoning as AppointmentType/DoctorPayoutRule.</summary>
public class PatientCategoryService(IAppDbContext db)
{
    public async Task<IReadOnlyList<PatientCategoryDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.PatientCategories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new PatientCategoryDto(c.Id, c.Code, c.Name, c.Description, c.ColorHex, c.IsActive))
            .ToListAsync(ct);

    public async Task<long> CreateAsync(SavePatientCategoryRequest request, CancellationToken ct = default)
    {
        await EnsureUniqueCodeAsync(request.Code, null, ct);

        var category = new PatientCategory
        {
            Code = request.Code, Name = request.Name, Description = request.Description,
            ColorHex = request.ColorHex, IsActive = request.IsActive,
        };
        db.PatientCategories.Add(category);
        await db.SaveChangesAsync(ct);
        return category.Id;
    }

    public async Task UpdateAsync(long id, SavePatientCategoryRequest request, CancellationToken ct = default)
    {
        var category = await db.PatientCategories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(PatientCategory), id);
        await EnsureUniqueCodeAsync(request.Code, id, ct);

        category.Code = request.Code;
        category.Name = request.Name;
        category.Description = request.Description;
        category.ColorHex = request.ColorHex;
        category.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var category = await db.PatientCategories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(PatientCategory), id);
        if (await db.Tariffs.AnyAsync(t => t.PatientCategoryId == id, ct))
        {
            throw new DomainException("This patient category is used by one or more tariffs and cannot be deleted.");
        }
        if (await db.Patients.AnyAsync(p => p.PatientCategoryId == id, ct))
        {
            throw new DomainException("This patient category is assigned to one or more patients and cannot be deleted.");
        }
        db.PatientCategories.Remove(category);
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureUniqueCodeAsync(string code, long? selfId, CancellationToken ct)
    {
        if (await db.PatientCategories.AnyAsync(c => c.Code == code && c.Id != selfId, ct))
        {
            throw new DomainException($"Patient category code '{code}' already exists.");
        }
    }
}
