using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Domain.Entities.Quality;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Quality;

/// <summary>Audit Checklist templates + scheduled/scored audit runs (plan §20: "templates, scheduled audits, scoring").</summary>
public class QualityAuditService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<AuditTemplateDto>> GetTemplatesAsync(CancellationToken ct = default)
    {
        var templates = await db.QualityAuditTemplates.AsNoTracking().Include(t => t.Items).OrderBy(t => t.Name).ToListAsync(ct);
        return templates.Select(ToTemplateDto).ToList();
    }

    public async Task<long> CreateTemplateAsync(SaveAuditTemplateRequest request, CancellationToken ct = default)
    {
        if (await db.QualityAuditTemplates.AnyAsync(t => t.Code == request.Code, ct))
        {
            throw new DomainException($"Audit template code '{request.Code}' already exists.");
        }
        var template = new QualityAuditTemplate
        {
            Code = request.Code, Name = request.Name, Category = request.Category,
            Items = request.Items.Select(i => new QualityAuditTemplateItem
            {
                SortOrder = i.SortOrder, Question = i.Question, MaxScore = i.MaxScore,
            }).ToList(),
        };
        db.QualityAuditTemplates.Add(template);
        await db.SaveChangesAsync(ct);
        return template.Id;
    }

    public async Task<PagedResult<AuditDto>> GetPagedAsync(AuditQuery query, CancellationToken ct = default)
    {
        var audits = db.QualityAudits.AsNoTracking().AsQueryable();
        if (query.Status is { } status) audits = audits.Where(a => a.Status == status);

        var totalCount = await audits.CountAsync(ct);
        var items = await audits
            .Include(a => a.Template).ThenInclude(t => t!.Items)
            .Include(a => a.ConductedByUser)
            .Include(a => a.Responses)
            .OrderByDescending(a => a.ScheduledDate)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .ToListAsync(ct);
        return PagedResult<AuditDto>.Create(items.Select(ToAuditDto).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<long> ScheduleAsync(ScheduleAuditRequest request, CancellationToken ct = default)
    {
        if (!await db.QualityAuditTemplates.AnyAsync(t => t.Id == request.TemplateId, ct))
        {
            throw new NotFoundException(nameof(QualityAuditTemplate), request.TemplateId);
        }
        var audit = new QualityAudit
        {
            BranchId = RequireBranch(), TemplateId = request.TemplateId, ScheduledDate = request.ScheduledDate,
            Status = QualityAuditStatus.Scheduled,
        };
        db.QualityAudits.Add(audit);
        await db.SaveChangesAsync(ct);
        return audit.Id;
    }

    public async Task ScoreAsync(long id, ScoreAuditRequest request, CancellationToken ct = default)
    {
        var audit = await db.QualityAudits.Include(a => a.Responses).FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException(nameof(QualityAudit), id);
        if (audit.Status == QualityAuditStatus.Completed || audit.Status == QualityAuditStatus.Cancelled)
        {
            throw new DomainException($"Cannot score an audit that is already {audit.Status}.");
        }

        var existingByItem = audit.Responses.ToDictionary(r => r.TemplateItemId);
        foreach (var response in request.Responses)
        {
            if (existingByItem.TryGetValue(response.TemplateItemId, out var existing))
            {
                existing.Score = response.Score;
                existing.Remarks = response.Remarks;
            }
            else
            {
                audit.Responses.Add(new QualityAuditResponse
                {
                    AuditId = audit.Id, TemplateItemId = response.TemplateItemId, Score = response.Score, Remarks = response.Remarks,
                });
            }
        }
        audit.Status = QualityAuditStatus.InProgress;
        await db.SaveChangesAsync(ct);
    }

    public async Task CompleteAsync(long id, CancellationToken ct = default)
    {
        var audit = await db.QualityAudits.FirstOrDefaultAsync(a => a.Id == id, ct) ?? throw new NotFoundException(nameof(QualityAudit), id);
        if (audit.Status == QualityAuditStatus.Completed || audit.Status == QualityAuditStatus.Cancelled)
        {
            throw new DomainException($"Audit is already {audit.Status}.");
        }
        audit.Status = QualityAuditStatus.Completed;
        audit.ConductedDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        audit.ConductedByUserId = currentUser.UserId;
        await db.SaveChangesAsync(ct);
    }

    private static AuditTemplateDto ToTemplateDto(QualityAuditTemplate t) => new(
        t.Id, t.Code, t.Name, t.Category,
        t.Items.OrderBy(i => i.SortOrder).Select(i => new AuditTemplateItemDto(i.Id, i.SortOrder, i.Question, i.MaxScore)).ToList());

    private static AuditDto ToAuditDto(QualityAudit a)
    {
        var responseDtos = a.Template!.Items.OrderBy(i => i.SortOrder).Select(item =>
        {
            var response = a.Responses.FirstOrDefault(r => r.TemplateItemId == item.Id);
            return new AuditResponseDto(item.Id, item.Question, item.MaxScore, response?.Score ?? 0, response?.Remarks);
        }).ToList();
        var scorePercent = QualityMath.AuditScorePercent(
            a.Responses.Select(r => (r.Score, a.Template!.Items.First(i => i.Id == r.TemplateItemId).MaxScore)).ToList());

        return new AuditDto(
            a.Id, a.TemplateId, a.Template!.Name, a.ScheduledDate, a.ConductedDate, a.Status,
            a.ConductedByUser?.FullName, a.Remarks, scorePercent, responseDtos);
    }

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");
}
