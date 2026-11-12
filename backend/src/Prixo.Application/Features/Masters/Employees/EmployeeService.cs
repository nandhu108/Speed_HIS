using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Masters.Employees;

public record EmployeeDto(long Id, string Code, string FullName, string? Phone, string? Email,
    long? DesignationId, string? DesignationName, long? DepartmentId, string? DepartmentName,
    DateOnly? JoinedOn, bool IsActive);
public record SaveEmployeeRequest(string Code, string FullName, string? Phone, string? Email,
    long? DesignationId, long? DepartmentId, DateOnly? JoinedOn, bool IsActive = true);

public class SaveEmployeeRequestValidator : AbstractValidator<SaveEmployeeRequest>
{
    public SaveEmployeeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}

/// <summary>Lightweight staff registry (full HR &amp; Payroll is a held module). Branch-scoped.</summary>
public class EmployeeService(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<PagedResult<EmployeeDto>> GetPagedAsync(PagedQuery query, CancellationToken ct = default)
    {
        var employees = db.Employees.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            employees = employees.Where(e =>
                EF.Functions.ILike(e.Code, term) ||
                EF.Functions.ILike(e.FullName, term) ||
                (e.Phone != null && EF.Functions.ILike(e.Phone, term)));
        }

        var totalCount = await employees.CountAsync(ct);
        var items = await employees
            .OrderBy(e => e.FullName)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(e => new EmployeeDto(e.Id, e.Code, e.FullName, e.Phone, e.Email,
                e.DesignationId, e.Designation != null ? e.Designation.Name : null,
                e.DepartmentId, e.Department != null ? e.Department.Name : null,
                e.JoinedOn, e.IsActive))
            .ToListAsync(ct);

        return PagedResult<EmployeeDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<long> CreateAsync(SaveEmployeeRequest request, CancellationToken ct = default)
    {
        await EnsureReferencesAsync(request, null, ct);

        var employee = new Employee
        {
            BranchId = RequireBranch(),
            Code = request.Code,
            FullName = request.FullName,
            Phone = request.Phone,
            Email = request.Email,
            DesignationId = request.DesignationId,
            DepartmentId = request.DepartmentId,
            JoinedOn = request.JoinedOn,
            IsActive = request.IsActive,
        };
        db.Employees.Add(employee);
        await db.SaveChangesAsync(ct);
        return employee.Id;
    }

    public async Task UpdateAsync(long id, SaveEmployeeRequest request, CancellationToken ct = default)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NotFoundException(nameof(Employee), id);
        await EnsureReferencesAsync(request, id, ct);

        employee.Code = request.Code;
        employee.FullName = request.FullName;
        employee.Phone = request.Phone;
        employee.Email = request.Email;
        employee.DesignationId = request.DesignationId;
        employee.DepartmentId = request.DepartmentId;
        employee.JoinedOn = request.JoinedOn;
        employee.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NotFoundException(nameof(Employee), id);
        db.Employees.Remove(employee); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    private long RequireBranch() =>
        currentUser.BranchId ?? throw new DomainException("A branch context is required.");

    private async Task EnsureReferencesAsync(SaveEmployeeRequest request, long? selfId, CancellationToken ct)
    {
        if (await db.Employees.AnyAsync(e => e.Code == request.Code && e.Id != selfId, ct))
        {
            throw new DomainException($"Employee code '{request.Code}' already exists.");
        }
        if (request.DesignationId is not null && !await db.Designations.AnyAsync(d => d.Id == request.DesignationId, ct))
        {
            throw new DomainException("Designation does not exist.");
        }
        if (request.DepartmentId is not null && !await db.Departments.AnyAsync(d => d.Id == request.DepartmentId, ct))
        {
            throw new DomainException("Department does not exist.");
        }
    }
}
