using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Admin.Roles;

public record RolePresetDto(long Id, string Name, IReadOnlyList<long> RoleIds, IReadOnlyList<string> RoleNames);
public record SaveRolePresetRequest(string Name, IReadOnlyList<long> RoleIds);

public class SaveRolePresetRequestValidator : AbstractValidator<SaveRolePresetRequest>
{
    public SaveRolePresetRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.RoleIds).NotEmpty().WithMessage("Pick at least one role.");
    }
}

/// <summary>Named, reusable role combinations — a user-creation convenience, not a permission boundary.</summary>
public class RolePresetService(IAppDbContext db)
{
    public async Task<IReadOnlyList<RolePresetDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.RolePresets.AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new RolePresetDto(
                p.Id, p.Name,
                p.RolePresetRoles.Select(pr => pr.RoleId).ToList(),
                p.RolePresetRoles.Select(pr => pr.Role.Name).ToList()))
            .ToListAsync(ct);

    public async Task<long> CreateAsync(SaveRolePresetRequest request, CancellationToken ct = default)
    {
        if (await db.RolePresets.AnyAsync(p => p.Name == request.Name, ct))
        {
            throw new DomainException($"A preset named '{request.Name}' already exists.");
        }

        var roleIds = request.RoleIds.Distinct().ToList();
        var validRoles = await db.Roles.CountAsync(r => roleIds.Contains(r.Id), ct);
        if (validRoles != roleIds.Count)
        {
            throw new DomainException("One or more roles do not exist.");
        }

        var preset = new RolePreset
        {
            Name = request.Name,
            RolePresetRoles = roleIds.Select(roleId => new RolePresetRole { RoleId = roleId }).ToList(),
        };
        db.RolePresets.Add(preset);
        await db.SaveChangesAsync(ct);
        return preset.Id;
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var preset = await db.RolePresets.Include(p => p.RolePresetRoles)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(RolePreset), id);

        foreach (var row in preset.RolePresetRoles.ToList())
        {
            db.RolePresetRoles.Remove(row);
        }
        db.RolePresets.Remove(preset);
        await db.SaveChangesAsync(ct);
    }
}
