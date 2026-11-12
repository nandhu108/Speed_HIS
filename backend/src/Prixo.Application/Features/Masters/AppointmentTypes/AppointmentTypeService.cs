using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.AppointmentTypes;

public record AppointmentTypeDto(long Id, string Code, string Name, string? Description, int DefaultSlotMinutes, bool IsActive);

public record SaveAppointmentTypeRequest(string Code, string Name, string? Description, int DefaultSlotMinutes, bool IsActive = true);

public class SaveAppointmentTypeRequestValidator : AbstractValidator<SaveAppointmentTypeRequest>
{
    public SaveAppointmentTypeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30)
            .Matches("^[A-Z0-9_-]+$").WithMessage("Code must be uppercase letters, digits, '-' or '_'.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.DefaultSlotMinutes).InclusiveBetween(1, 240);
    }
}

/// <summary>"Appointment Masters" — a configurable categorization tag on top of the fixed
/// AppointmentBookingType enum (Booked/WalkIn). Bespoke (not ISimpleMaster) since it carries
/// DefaultSlotMinutes, same reasoning as DoctorPayoutRule.</summary>
public class AppointmentTypeService(IAppDbContext db)
{
    public async Task<IReadOnlyList<AppointmentTypeDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.AppointmentTypes.AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new AppointmentTypeDto(t.Id, t.Code, t.Name, t.Description, t.DefaultSlotMinutes, t.IsActive))
            .ToListAsync(ct);

    public async Task<long> CreateAsync(SaveAppointmentTypeRequest request, CancellationToken ct = default)
    {
        await EnsureUniqueCodeAsync(request.Code, null, ct);

        var type = new AppointmentType
        {
            Code = request.Code, Name = request.Name, Description = request.Description,
            DefaultSlotMinutes = request.DefaultSlotMinutes, IsActive = request.IsActive,
        };
        db.AppointmentTypes.Add(type);
        await db.SaveChangesAsync(ct);
        return type.Id;
    }

    public async Task UpdateAsync(long id, SaveAppointmentTypeRequest request, CancellationToken ct = default)
    {
        var type = await db.AppointmentTypes.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(AppointmentType), id);
        await EnsureUniqueCodeAsync(request.Code, id, ct);

        type.Code = request.Code;
        type.Name = request.Name;
        type.Description = request.Description;
        type.DefaultSlotMinutes = request.DefaultSlotMinutes;
        type.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var type = await db.AppointmentTypes.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(AppointmentType), id);
        if (await db.Appointments.AnyAsync(a => a.AppointmentTypeId == id, ct))
        {
            throw new DomainException("This appointment type is in use by one or more appointments and cannot be deleted.");
        }
        db.AppointmentTypes.Remove(type);
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureUniqueCodeAsync(string code, long? selfId, CancellationToken ct)
    {
        if (await db.AppointmentTypes.AnyAsync(t => t.Code == code && t.Id != selfId, ct))
        {
            throw new DomainException($"Appointment type code '{code}' already exists.");
        }
    }
}
