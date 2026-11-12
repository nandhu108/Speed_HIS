using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Admin.Auth;

namespace Prixo.Application.Features.Reports;

/// <summary>Custom Report Builder v1 catalog (plan §17/§23) — lists the report_definitions rows the caller's own permissions actually let them see, so the frontend's report picker never shows a report the user can't run.</summary>
public class ReportCatalogService(IAppDbContext db, ICurrentUser currentUser, PermissionService permissions)
{
    public async Task<IReadOnlyList<ReportCatalogEntryDto>> GetCatalogAsync(CancellationToken ct = default)
    {
        var myPermissions = await permissions.GetUserPermissionsAsync(currentUser.UserId, ct);
        var definitions = await db.ReportDefinitions.AsNoTracking().Where(d => d.IsActive).OrderBy(d => d.Category).ThenBy(d => d.Name).ToListAsync(ct);

        return definitions
            .Where(d => myPermissions.Contains(d.PermissionCode))
            .Select(d => new ReportCatalogEntryDto(
                d.Code, d.Name, d.Description, d.Category, d.LinkPath, d.ViewName is not null,
                JsonSerializer.Deserialize<List<ReportColumnDto>>(d.ColumnsJson, ReportJsonOptions.CaseInsensitive) ?? []))
            .ToList();
    }
}
