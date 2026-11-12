using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.Doctors;

public record DoctorListItemDto(long Id, long UserId, string FullName, string RegistrationNo,
    string? SpecialtyName, IReadOnlyList<string> Departments, decimal ConsultationFee,
    decimal FollowUpFee, bool IsActive);
public record DoctorDetailDto(long Id, long UserId, string FullName, string RegistrationNo,
    long? SpecialtyId, string? Qualification, decimal ConsultationFee, decimal FollowUpFee,
    int FollowUpWindowDays, int DefaultSlotMinutes, IReadOnlyList<long> DepartmentIds, bool IsActive);
public record SaveDoctorRequest(long UserId, string RegistrationNo, long? SpecialtyId,
    string? Qualification, decimal ConsultationFee, decimal FollowUpFee,
    int FollowUpWindowDays, int DefaultSlotMinutes, IReadOnlyList<long> DepartmentIds, bool IsActive = true);

public class SaveDoctorRequestValidator : AbstractValidator<SaveDoctorRequest>
{
    public SaveDoctorRequestValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.RegistrationNo).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Qualification).MaximumLength(300);
        RuleFor(x => x.ConsultationFee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FollowUpFee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FollowUpWindowDays).InclusiveBetween(0, 365);
        RuleFor(x => x.DefaultSlotMinutes).InclusiveBetween(1, 240);
        RuleFor(x => x.DepartmentIds).NotEmpty().WithMessage("Map the doctor to at least one department.");
    }
}

public class DoctorService(IAppDbContext db)
{
    public async Task<PagedResult<DoctorListItemDto>> GetPagedAsync(PagedQuery query, CancellationToken ct = default)
    {
        var doctors = db.Doctors.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            doctors = doctors.Where(d =>
                EF.Functions.ILike(d.User!.FullName, term) ||
                EF.Functions.ILike(d.RegistrationNo, term) ||
                (d.Specialty != null && EF.Functions.ILike(d.Specialty.Name, term)));
        }

        var totalCount = await doctors.CountAsync(ct);
        var items = await doctors
            .OrderBy(d => d.User!.FullName)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(d => new DoctorListItemDto(
                d.Id, d.UserId, d.User!.FullName, d.RegistrationNo,
                d.Specialty != null ? d.Specialty.Name : null,
                d.DoctorDepartments.Select(dd => dd.Department!.Name).ToList(),
                d.ConsultationFee, d.FollowUpFee, d.IsActive))
            .ToListAsync(ct);

        return PagedResult<DoctorListItemDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<DoctorDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var doctor = await db.Doctors.AsNoTracking()
            .Include(d => d.User)
            .Include(d => d.DoctorDepartments)
            .FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new NotFoundException(nameof(Doctor), id);

        return new DoctorDetailDto(doctor.Id, doctor.UserId, doctor.User!.FullName,
            doctor.RegistrationNo, doctor.SpecialtyId, doctor.Qualification,
            doctor.ConsultationFee, doctor.FollowUpFee, doctor.FollowUpWindowDays,
            doctor.DefaultSlotMinutes,
            doctor.DoctorDepartments.Select(dd => dd.DepartmentId).ToList(), doctor.IsActive);
    }

    public async Task<long> CreateAsync(SaveDoctorRequest request, CancellationToken ct = default)
    {
        await EnsureReferencesAsync(request, null, ct);

        var doctor = new Doctor
        {
            UserId = request.UserId,
            RegistrationNo = request.RegistrationNo,
            SpecialtyId = request.SpecialtyId,
            Qualification = request.Qualification,
            ConsultationFee = request.ConsultationFee,
            FollowUpFee = request.FollowUpFee,
            FollowUpWindowDays = request.FollowUpWindowDays,
            DefaultSlotMinutes = request.DefaultSlotMinutes,
            IsActive = request.IsActive,
            DoctorDepartments = request.DepartmentIds.Distinct()
                .Select(departmentId => new DoctorDepartment { DepartmentId = departmentId }).ToList(),
        };
        db.Doctors.Add(doctor);
        await db.SaveChangesAsync(ct);
        return doctor.Id;
    }

    public async Task UpdateAsync(long id, SaveDoctorRequest request, CancellationToken ct = default)
    {
        var doctor = await db.Doctors
            .Include(d => d.DoctorDepartments)
            .FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new NotFoundException(nameof(Doctor), id);
        await EnsureReferencesAsync(request, id, ct);

        doctor.UserId = request.UserId;
        doctor.RegistrationNo = request.RegistrationNo;
        doctor.SpecialtyId = request.SpecialtyId;
        doctor.Qualification = request.Qualification;
        doctor.ConsultationFee = request.ConsultationFee;
        doctor.FollowUpFee = request.FollowUpFee;
        doctor.FollowUpWindowDays = request.FollowUpWindowDays;
        doctor.DefaultSlotMinutes = request.DefaultSlotMinutes;
        doctor.IsActive = request.IsActive;

        var desired = request.DepartmentIds.Distinct().ToHashSet();
        foreach (var row in doctor.DoctorDepartments.Where(dd => !desired.Contains(dd.DepartmentId)).ToList())
        {
            db.DoctorDepartments.Remove(row);
            doctor.DoctorDepartments.Remove(row);
        }
        foreach (var departmentId in desired.Where(d => doctor.DoctorDepartments.All(dd => dd.DepartmentId != d)))
        {
            doctor.DoctorDepartments.Add(new DoctorDepartment { DoctorId = id, DepartmentId = departmentId });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var doctor = await db.Doctors.FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new NotFoundException(nameof(Doctor), id);
        if (await db.Referrers.IgnoreQueryFilters().AnyAsync(r => !r.IsDeleted && r.DoctorId == id, ct))
        {
            throw new DomainException("Doctor is linked to referrers. Remove those first.");
        }

        db.Doctors.Remove(doctor); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureReferencesAsync(SaveDoctorRequest request, long? selfId, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(u => u.Id == request.UserId, ct))
        {
            throw new DomainException("Linked user does not exist.");
        }
        if (await db.Doctors.AnyAsync(d => d.UserId == request.UserId && d.Id != selfId, ct))
        {
            throw new DomainException("A doctor profile already exists for this user.");
        }
        if (await db.Doctors.AnyAsync(d => d.RegistrationNo == request.RegistrationNo && d.Id != selfId, ct))
        {
            throw new DomainException($"Registration number '{request.RegistrationNo}' is already registered.");
        }
        if (request.SpecialtyId is not null && !await db.Specialties.AnyAsync(s => s.Id == request.SpecialtyId, ct))
        {
            throw new DomainException("Specialty does not exist.");
        }
        var validDepartments = await db.Departments.CountAsync(d => request.DepartmentIds.Contains(d.Id), ct);
        if (validDepartments != request.DepartmentIds.Distinct().Count())
        {
            throw new DomainException("One or more departments do not exist.");
        }
    }
}
