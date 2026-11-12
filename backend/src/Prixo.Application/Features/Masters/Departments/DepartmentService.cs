using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.Departments;

/// <summary>
/// Department master — bespoke (not ISimpleMaster-generic) since it carries a Clinical /
/// Non-Clinical type, same reasoning as PatientCategory's ColorHex. OP registration and IP
/// admission only accept active Clinical departments (see <see cref="EnsureClinicalAsync"/>).
/// </summary>
public class DepartmentService(IAppDbContext db)
{
    public async Task<IReadOnlyList<DepartmentDto>> GetAllAsync(
        string? search = null, DepartmentType? type = null, CancellationToken ct = default)
    {
        var query = db.Departments.AsNoTracking();
        if (type is { } t) query = query.Where(d => d.Type == t);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(d => EF.Functions.ILike(d.Code, term) || EF.Functions.ILike(d.Name, term));
        }
        return await query.OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(d.Id, d.Code, d.Name, d.Description, d.IsActive, d.Type))
            .ToListAsync(ct);
    }

    public async Task<long> CreateAsync(SaveDepartmentRequest request, CancellationToken ct = default)
    {
        await EnsureUniqueCodeAsync(request.Code, null, ct);
        var department = new Department
        {
            Code = request.Code, Name = request.Name, Description = request.Description,
            Type = request.Type, IsActive = request.IsActive,
        };
        db.Departments.Add(department);
        await db.SaveChangesAsync(ct);
        return department.Id;
    }

    public async Task UpdateAsync(long id, SaveDepartmentRequest request, CancellationToken ct = default)
    {
        var department = await db.Departments.FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new NotFoundException(nameof(Department), id);
        await EnsureUniqueCodeAsync(request.Code, id, ct);

        department.Code = request.Code;
        department.Name = request.Name;
        department.Description = request.Description;
        department.Type = request.Type;
        department.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var department = await db.Departments.FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new NotFoundException(nameof(Department), id);

        if (await db.Services.AnyAsync(s => s.DepartmentId == id, ct)) ThrowInUse("services");
        if (await db.DoctorDepartments.AnyAsync(dd => dd.DepartmentId == id, ct)) ThrowInUse("doctors");
        if (await db.Employees.IgnoreQueryFilters().AnyAsync(e => !e.IsDeleted && e.DepartmentId == id, ct)) ThrowInUse("employees");
        if (await db.OpVisits.IgnoreQueryFilters().AnyAsync(v => !v.IsDeleted && v.DepartmentId == id, ct)) ThrowInUse("OP visits");
        if (await db.Admissions.IgnoreQueryFilters().AnyAsync(a => !a.IsDeleted && a.DepartmentId == id, ct)) ThrowInUse("admissions");

        db.Departments.Remove(department); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Shared gate for OP registration and IP admission: the department must exist, be active and
    /// be Clinical. Returns the department so callers can snapshot it.
    /// </summary>
    public static async Task<Department> EnsureClinicalAsync(IAppDbContext db, long departmentId, CancellationToken ct)
    {
        var department = await db.Departments.FirstOrDefaultAsync(d => d.Id == departmentId, ct)
            ?? throw new NotFoundException(nameof(Department), departmentId);
        if (!department.IsActive)
        {
            throw new DomainException($"Department '{department.Name}' is inactive.");
        }
        if (department.Type != DepartmentType.Clinical)
        {
            throw new DomainException($"'{department.Name}' is a non-clinical department; select a clinical department.");
        }
        return department;
    }

    private static void ThrowInUse(string usedBy) =>
        throw new DomainException($"Department is used by {usedBy}. Remove those references first.");

    private async Task EnsureUniqueCodeAsync(string code, long? selfId, CancellationToken ct)
    {
        if (await db.Departments.AnyAsync(d => d.Code == code && d.Id != selfId, ct))
        {
            throw new DomainException($"Department code '{code}' already exists.");
        }
    }
}
