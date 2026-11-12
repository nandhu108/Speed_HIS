using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Rad;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Rad;

/// <summary>Canned Findings/Impression templates per radiology test (plan §12 "structured templates").</summary>
public class RadReportTemplateService(IAppDbContext db)
{
    public async Task<IReadOnlyList<RadReportTemplateListItemDto>> GetAllAsync(long? radiologyTestId, CancellationToken ct = default)
    {
        var query = db.RadReportTemplates.AsNoTracking().AsQueryable();
        if (radiologyTestId is { } testId) query = query.Where(t => t.RadiologyTestId == testId);
        return await query.Include(t => t.RadiologyTest)
            .OrderBy(t => t.Name)
            .Select(t => new RadReportTemplateListItemDto(t.Id, t.RadiologyTestId, t.RadiologyTest!.Name, t.Name, t.IsActive))
            .ToListAsync(ct);
    }

    public async Task<RadReportTemplateDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var template = await db.RadReportTemplates.AsNoTracking().Include(t => t.RadiologyTest)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(RadReportTemplate), id);
        return ToDetailDto(template);
    }

    public async Task<long> CreateAsync(SaveRadReportTemplateRequest request, CancellationToken ct = default)
    {
        if (!await db.RadiologyTests.AnyAsync(t => t.Id == request.RadiologyTestId, ct))
        {
            throw new NotFoundException(nameof(RadiologyTest), request.RadiologyTestId);
        }
        var template = new RadReportTemplate
        {
            RadiologyTestId = request.RadiologyTestId, Name = request.Name,
            FindingsTemplate = request.FindingsTemplate, ImpressionTemplate = request.ImpressionTemplate, IsActive = request.IsActive,
        };
        db.RadReportTemplates.Add(template);
        await db.SaveChangesAsync(ct);
        return template.Id;
    }

    public async Task UpdateAsync(long id, SaveRadReportTemplateRequest request, CancellationToken ct = default)
    {
        var template = await db.RadReportTemplates.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(RadReportTemplate), id);
        if (!await db.RadiologyTests.AnyAsync(t => t.Id == request.RadiologyTestId, ct))
        {
            throw new NotFoundException(nameof(RadiologyTest), request.RadiologyTestId);
        }

        template.RadiologyTestId = request.RadiologyTestId;
        template.Name = request.Name;
        template.FindingsTemplate = request.FindingsTemplate;
        template.ImpressionTemplate = request.ImpressionTemplate;
        template.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var template = await db.RadReportTemplates.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(RadReportTemplate), id);
        db.RadReportTemplates.Remove(template); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    private static RadReportTemplateDetailDto ToDetailDto(RadReportTemplate t) => new(
        t.Id, t.RadiologyTestId, t.RadiologyTest!.Name, t.Name, t.FindingsTemplate, t.ImpressionTemplate, t.IsActive);
}
