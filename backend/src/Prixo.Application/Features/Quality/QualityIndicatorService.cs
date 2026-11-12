using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Ipd;
using Prixo.Application.Features.Lab;
using Prixo.Domain.Entities.Quality;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Quality;

/// <summary>
/// Quality Indicators master + monthly entries (plan §20: "numerator/denominator definitions
/// + monthly data entry / auto-pull where computable (TAT, occupancy, incidents)"). Auto-compute
/// is an explicit action ("Compute" button), not a background job — same posture as Biomed's PM
/// "generate due" button — and reuses the exact existing engines rather than re-deriving TAT or
/// occupancy: LabOrderService's TAT report and BedBoardService's occupancy dashboard.
/// </summary>
public class QualityIndicatorService(
    IAppDbContext db, ICurrentUser currentUser, LabOrderService labOrders, BedBoardService bedBoard)
{
    public async Task<IReadOnlyList<IndicatorDto>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        var query = db.QualityIndicators.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(i => EF.Functions.ILike(i.Code, term) || EF.Functions.ILike(i.Name, term));
        }
        return await query.OrderBy(i => i.Name).Select(i => ToDto(i)).ToListAsync(ct);
    }

    public async Task<long> CreateAsync(SaveIndicatorRequest request, CancellationToken ct = default)
    {
        if (await db.QualityIndicators.AnyAsync(i => i.Code == request.Code, ct))
        {
            throw new DomainException($"Indicator code '{request.Code}' already exists.");
        }
        var indicator = new QualityIndicator
        {
            Code = request.Code, Name = request.Name, Description = request.Description, Category = request.Category,
            NumeratorDefinition = request.NumeratorDefinition, DenominatorDefinition = request.DenominatorDefinition,
            Unit = request.Unit, TargetValue = request.TargetValue, AutoComputeSource = request.AutoComputeSource,
        };
        db.QualityIndicators.Add(indicator);
        await db.SaveChangesAsync(ct);
        return indicator.Id;
    }

    public async Task UpdateAsync(long id, SaveIndicatorRequest request, CancellationToken ct = default)
    {
        var indicator = await db.QualityIndicators.FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new NotFoundException(nameof(QualityIndicator), id);
        if (await db.QualityIndicators.AnyAsync(i => i.Code == request.Code && i.Id != id, ct))
        {
            throw new DomainException($"Indicator code '{request.Code}' already exists.");
        }
        indicator.Code = request.Code;
        indicator.Name = request.Name;
        indicator.Description = request.Description;
        indicator.Category = request.Category;
        indicator.NumeratorDefinition = request.NumeratorDefinition;
        indicator.DenominatorDefinition = request.DenominatorDefinition;
        indicator.Unit = request.Unit;
        indicator.TargetValue = request.TargetValue;
        indicator.AutoComputeSource = request.AutoComputeSource;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<IndicatorEntryDto>> GetEntriesAsync(long indicatorId, CancellationToken ct = default)
    {
        var entries = await db.QualityIndicatorEntries.AsNoTracking()
            .Where(e => e.IndicatorId == indicatorId)
            .Include(e => e.EnteredByUser)
            .OrderByDescending(e => e.PeriodMonth)
            .ToListAsync(ct);
        return entries.Select(ToEntryDto).ToList();
    }

    public async Task<long> RecordEntryAsync(long indicatorId, RecordIndicatorEntryRequest request, CancellationToken ct = default)
    {
        if (!await db.QualityIndicators.AnyAsync(i => i.Id == indicatorId, ct))
        {
            throw new NotFoundException(nameof(QualityIndicator), indicatorId);
        }
        var periodMonth = FirstOfMonth(request.PeriodMonth);
        var branchId = RequireBranch();
        if (await db.QualityIndicatorEntries.AnyAsync(
            e => e.IndicatorId == indicatorId && e.BranchId == branchId && e.PeriodMonth == periodMonth, ct))
        {
            throw new DomainException("An entry already exists for this indicator/branch/period. Update it instead.");
        }

        var entry = new QualityIndicatorEntry
        {
            BranchId = branchId, IndicatorId = indicatorId, PeriodMonth = periodMonth,
            Numerator = request.Numerator, Denominator = request.Denominator, Remarks = request.Remarks,
            IsAutoComputed = false, EnteredByUserId = currentUser.UserId,
        };
        db.QualityIndicatorEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry.Id;
    }

    /// <summary>Explicit "Compute" action — pulls Numerator/Denominator from the indicator's AutoComputeSource for the given month.</summary>
    public async Task<long> AutoComputeAsync(long indicatorId, DateOnly period, CancellationToken ct = default)
    {
        var indicator = await db.QualityIndicators.AsNoTracking().FirstOrDefaultAsync(i => i.Id == indicatorId, ct)
            ?? throw new NotFoundException(nameof(QualityIndicator), indicatorId);
        if (indicator.AutoComputeSource is null)
        {
            throw new DomainException($"Indicator '{indicator.Name}' has no auto-compute source; record a manual entry instead.");
        }

        var periodMonth = FirstOfMonth(period);
        var branchId = RequireBranch();
        var (numerator, denominator) = await ComputeAsync(indicator.AutoComputeSource.Value, periodMonth, branchId, ct);

        var existing = await db.QualityIndicatorEntries.FirstOrDefaultAsync(
            e => e.IndicatorId == indicatorId && e.BranchId == branchId && e.PeriodMonth == periodMonth, ct);
        if (existing is not null)
        {
            existing.Numerator = numerator;
            existing.Denominator = denominator;
            existing.IsAutoComputed = true;
            existing.EnteredByUserId = currentUser.UserId;
            await db.SaveChangesAsync(ct);
            return existing.Id;
        }

        var entry = new QualityIndicatorEntry
        {
            BranchId = branchId, IndicatorId = indicatorId, PeriodMonth = periodMonth,
            Numerator = numerator, Denominator = denominator, IsAutoComputed = true, EnteredByUserId = currentUser.UserId,
        };
        db.QualityIndicatorEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry.Id;
    }

    private async Task<(decimal Numerator, decimal Denominator)> ComputeAsync(
        QualityAutoComputeSource source, DateOnly periodMonth, long branchId, CancellationToken ct)
    {
        switch (source)
        {
            case QualityAutoComputeSource.LabTat:
            {
                var from = periodMonth;
                var to = periodMonth.AddMonths(1).AddDays(-1);
                var rows = await labOrders.GetTatReportAsync(from, to, ct);
                var withTat = rows.Where(r => r.TurnaroundHours is not null).ToList();
                var totalHours = withTat.Sum(r => (decimal)r.TurnaroundHours!.Value);
                return (totalHours, withTat.Count);
            }
            case QualityAutoComputeSource.BedOccupancy:
            {
                var dashboard = await bedBoard.GetOccupancyDashboardAsync(ct);
                return (dashboard.Occupied, dashboard.TotalBeds);
            }
            case QualityAutoComputeSource.IncidentCount:
            {
                var from = new DateTimeOffset(periodMonth.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
                var to = new DateTimeOffset(periodMonth.AddMonths(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
                var count = await db.Incidents.AsNoTracking()
                    .CountAsync(i => i.BranchId == branchId && i.IncidentDate >= from && i.IncidentDate < to, ct);
                return (count, 1);
            }
            default:
                throw new DomainException($"Unsupported auto-compute source: {source}.");
        }
    }

    private static DateOnly FirstOfMonth(DateOnly date) => new(date.Year, date.Month, 1);

    private static IndicatorDto ToDto(QualityIndicator i) => new(
        i.Id, i.Code, i.Name, i.Description, i.Category, i.NumeratorDefinition, i.DenominatorDefinition,
        i.Unit, i.TargetValue, i.AutoComputeSource);

    private static IndicatorEntryDto ToEntryDto(QualityIndicatorEntry e) => new(
        e.Id, e.IndicatorId, e.PeriodMonth, e.Numerator, e.Denominator,
        QualityMath.ComputedValue(e.Numerator, e.Denominator), e.IsAutoComputed, e.Remarks, e.EnteredByUser!.FullName);

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");
}
