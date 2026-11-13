using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Application.Features.Admin.Settings;
using Prixo.Application.Features.Billing;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Entities.Rad;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Rad;

/// <summary>
/// Radiology order lifecycle: create (from a visit, an admission, a loosely-linked
/// ClinicalOrder, or a direct order), modality scheduling, technician image-capture
/// worklist, contrast consent capture, and the entry→verify→approve reporting pipeline.
/// Mirrors Lab's LabOrderService shape throughout — the same loose-coupling posture (no hard
/// billing gate) and the same order-status-recomputed-from-items idiom (RadMath, not LabMath).
/// </summary>
public class RadOrderService(
    IAppDbContext db, ISequenceService sequences, ICurrentUser currentUser,
    IActivityLogger activity, IPdfService pdf, TimeProvider timeProvider, SettingService settings)
{
    /// <summary>Two scans on the same modality closer together than this are treated as a double-booking.</summary>
    private static readonly TimeSpan ModalitySlot = TimeSpan.FromMinutes(15);

    // ── Orders ───────────────────────────────────────────────────────────────

    public async Task<RadOrderDetailDto> CreateOrderAsync(CreateRadOrderRequest request, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        if (!await db.Patients.AnyAsync(p => p.Id == request.PatientId, ct))
        {
            throw new NotFoundException(nameof(Patient), request.PatientId);
        }
        var tests = await db.RadiologyTests.AsNoTracking()
            .Where(t => request.RadiologyTestIds.Contains(t.Id))
            .ToListAsync(ct);
        if (request.RadiologyTestIds.Count == 0)
        {
            throw new DomainException("Select at least one radiology test.");
        }
        if (request.RadiologyTestIds.Distinct().Count() != request.RadiologyTestIds.Count)
        {
            throw new DomainException("The same scan appears more than once in this order.");
        }
        if (tests.Count != request.RadiologyTestIds.Count)
        {
            throw new DomainException("One or more selected radiology tests do not exist.");
        }
        var pendingDuplicate = await db.RadOrderItems.AsNoTracking()
            .Where(i => i.RadOrder!.PatientId == request.PatientId && request.RadiologyTestIds.Contains(i.RadiologyTestId) &&
                (i.Status == RadOrderItemStatus.Ordered || i.Status == RadOrderItemStatus.Scheduled))
            .Select(i => new { i.RadiologyTest!.Name, i.RadOrder!.OrderNumber }).FirstOrDefaultAsync(ct);
        if (pendingDuplicate is not null)
        {
            throw new DomainException($"{pendingDuplicate.Name} is already ordered for this patient on {pendingDuplicate.OrderNumber} and has not been performed yet.");
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var year = today.Year;
        var orderNumberValue = await sequences.NextValueAsync(SequenceKeys.RadOrderNumber, branchId, year, ct);

        var order = new RadOrder
        {
            BranchId = branchId,
            PatientId = request.PatientId,
            OpVisitId = request.OpVisitId,
            AdmissionId = request.AdmissionId,
            EmergencyVisitId = request.EmergencyVisitId,
            ClinicalOrderId = request.ClinicalOrderId,
            OrderedByDoctorId = request.OrderedByDoctorId,
            OrderNumber = $"RAD-{year}-{orderNumberValue.ToString().PadLeft(6, '0')}",
            OrderDate = today,
            Priority = request.Priority,
            ClinicalNotes = request.ClinicalNotes,
        };
        foreach (var testId in request.RadiologyTestIds)
        {
            var accessionValue = await sequences.NextValueAsync(SequenceKeys.RadAccessionNumber, branchId, year, ct);
            order.Items.Add(new RadOrderItem { RadiologyTestId = testId, AccessionNumber = $"ACC-{year}-{accessionValue.ToString().PadLeft(6, '0')}" });
        }

        db.RadOrders.Add(order);
        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(order.Id, ct);
    }

    public async Task<PagedResult<RadOrderListItemDto>> GetPagedAsync(RadOrderQuery query, CancellationToken ct = default)
    {
        var orders = db.RadOrders.AsNoTracking();

        if (query.PatientId is { } patientId) orders = orders.Where(o => o.PatientId == patientId);
        if (query.FromDate is { } from) orders = orders.Where(o => o.OrderDate >= from);
        if (query.ToDate is { } to) orders = orders.Where(o => o.OrderDate <= to);
        if (query.Status is { } status) orders = orders.Where(o => o.Status == status);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            orders = orders.Where(o =>
                EF.Functions.ILike(o.OrderNumber, term) ||
                EF.Functions.ILike(o.Patient!.Uhid, term) ||
                EF.Functions.ILike(o.Patient!.FirstName, term) ||
                EF.Functions.ILike(o.Patient!.LastName, term));
        }

        var totalCount = await orders.CountAsync(ct);
        var items = await orders
            .OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(o => new RadOrderListItemDto(
                o.Id, o.OrderNumber, o.OrderDate, o.PatientId, o.Patient!.Uhid,
                o.Patient!.FirstName + " " + o.Patient!.LastName, o.Priority, o.Status, o.Items.Count))
            .ToListAsync(ct);

        return PagedResult<RadOrderListItemDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<RadOrderDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var order = await db.RadOrders.AsNoTracking()
            .Include(o => o.Patient)
            .Include(o => o.OrderedByDoctor).ThenInclude(d => d!.User)
            .Include(o => o.Items).ThenInclude(i => i.RadiologyTest).ThenInclude(t => t!.Modality)
            .Include(o => o.Items).ThenInclude(i => i.Schedule)
            .Include(o => o.Items).ThenInclude(i => i.ContrastConsent)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException(nameof(RadOrder), id);
        return ToDetailDto(order);
    }

    public async Task CancelOrderAsync(long id, CancelRadOrderRequest request, CancellationToken ct = default)
    {
        var order = await db.RadOrders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException(nameof(RadOrder), id);
        // Ordered or Scheduled (not yet scanned) items can be cancelled — e.g. a no-show; the slot is released.
        if (order.Items.Any(i => i.Status is not (RadOrderItemStatus.Ordered or RadOrderItemStatus.Scheduled or RadOrderItemStatus.Cancelled)))
        {
            throw new DomainException("A scan on this order has already been performed — cancel the remaining items individually instead.");
        }

        foreach (var item in order.Items)
        {
            item.Status = RadOrderItemStatus.Cancelled;
        }
        await ReleaseSchedulesAsync(order.Items.Select(i => i.Id).ToList(), ct);
        order.Status = RadOrderStatus.Cancelled;
        order.CancelReason = request.Reason;
        order.CancelledAt = timeProvider.GetUtcNow();

        await activity.LogAsync(ActivityEvents.RadOrderCancelled, $"Radiology order {order.OrderNumber} cancelled: {request.Reason}", ct: ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task CancelItemAsync(long orderId, long itemId, CancellationToken ct = default)
    {
        var order = await db.RadOrders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new NotFoundException(nameof(RadOrder), orderId);
        var item = order.Items.FirstOrDefault(i => i.Id == itemId) ?? throw new NotFoundException(nameof(RadOrderItem), itemId);
        if (item.Status is not (RadOrderItemStatus.Ordered or RadOrderItemStatus.Scheduled))
        {
            throw new DomainException($"A scan that is {item.Status} can no longer be cancelled.");
        }

        item.Status = RadOrderItemStatus.Cancelled;
        await ReleaseSchedulesAsync([item.Id], ct);
        order.Status = RadMath.RecomputeOrderStatus(order.Items.Select(i => i.Status).ToList());
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Frees the modality slot of cancelled items (the schedule row is soft-deleted, kept for audit).</summary>
    private async Task ReleaseSchedulesAsync(IReadOnlyCollection<long> itemIds, CancellationToken ct)
    {
        var schedules = await db.RadSchedules.Where(s => itemIds.Contains(s.RadOrderItemId)).ToListAsync(ct);
        db.RadSchedules.RemoveRange(schedules); // converted to soft delete by the interceptor
    }

    // ── Scheduling ───────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SchedulingWorklistItemDto>> GetSchedulingWorklistAsync(long? modalityId, CancellationToken ct = default)
    {
        var items = db.RadOrderItems.AsNoTracking().Where(i => i.Status == RadOrderItemStatus.Ordered);
        if (modalityId is { } modId) items = items.Where(i => i.RadiologyTest!.ModalityId == modId);

        return await items
            .Include(i => i.RadOrder).ThenInclude(o => o!.Patient)
            .Include(i => i.RadiologyTest).ThenInclude(t => t!.Modality)
            .OrderBy(i => i.RadOrder!.Priority == ClinicalOrderPriority.Stat ? 0 : i.RadOrder!.Priority == ClinicalOrderPriority.Urgent ? 1 : 2)
            .ThenBy(i => i.RadOrder!.OrderDate)
            .Select(i => new SchedulingWorklistItemDto(
                i.Id, i.RadOrderId, i.RadOrder!.OrderNumber, i.RadOrder.PatientId, i.RadOrder.Patient!.Uhid,
                i.RadOrder.Patient!.FirstName + " " + i.RadOrder.Patient!.LastName, i.RadiologyTestId, i.RadiologyTest!.Name,
                i.RadiologyTest.ModalityId, i.RadiologyTest.Modality!.Name, i.RadiologyTest.RequiresContrast,
                i.RadOrder.Priority, i.RadOrder.OrderDate))
            .ToListAsync(ct);
    }

    public async Task ScheduleItemAsync(long itemId, ScheduleItemRequest request, CancellationToken ct = default)
    {
        var item = await db.RadOrderItems.Include(i => i.RadOrder).ThenInclude(o => o!.Items)
            .Include(i => i.RadiologyTest)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct)
            ?? throw new NotFoundException(nameof(RadOrderItem), itemId);
        if (item.Status is not (RadOrderItemStatus.Ordered or RadOrderItemStatus.Scheduled))
        {
            throw new DomainException($"A test that is {item.Status} cannot be (re)scheduled.");
        }
        var now = timeProvider.GetUtcNow();
        if (request.ScheduledAt < now.AddMinutes(-5))
        {
            throw new DomainException("A scan cannot be scheduled in the past.");
        }
        var modalityId = item.RadiologyTest!.ModalityId;
        var clash = await db.RadSchedules.AsNoTracking()
            .Where(s => s.ModalityId == modalityId && s.RadOrderItemId != item.Id &&
                s.RadOrderItem!.Status == RadOrderItemStatus.Scheduled &&
                s.ScheduledAt > request.ScheduledAt - ModalitySlot && s.ScheduledAt < request.ScheduledAt + ModalitySlot)
            .Select(s => new { s.ScheduledAt, s.RadOrderItem!.AccessionNumber }).FirstOrDefaultAsync(ct);
        if (clash is not null)
        {
            throw new DomainException($"The modality is already booked at {clash.ScheduledAt:HH:mm} (accession {clash.AccessionNumber}). Choose another time.");
        }
        var order = item.RadOrder!;
        if (order.OpVisitId is not null && order.EmergencyVisitId is null && order.AdmissionId is null)
        {
            await DiagnosticsBillingGate.EnsureBilledAsync(db, settings, [item.BillItemId], item.RadiologyTest.Name, ct);
        }

        var existing = await db.RadSchedules.FirstOrDefaultAsync(s => s.RadOrderItemId == item.Id, ct);
        if (existing is not null)
        {
            existing.ScheduledAt = request.ScheduledAt;
            existing.Notes = request.Notes;
            existing.ScheduledByUserId = currentUser.UserId;
        }
        else
        {
            db.RadSchedules.Add(new RadSchedule
            {
                RadOrderItemId = item.Id, ModalityId = modalityId, ScheduledAt = request.ScheduledAt,
                Notes = request.Notes, ScheduledByUserId = currentUser.UserId,
            });
        }
        item.Status = RadOrderItemStatus.Scheduled;
        item.RadOrder!.Status = RadMath.RecomputeOrderStatus(item.RadOrder.Items.Select(i => i.Status).ToList());
        await db.SaveChangesAsync(ct);
    }

    // ── Contrast consent ─────────────────────────────────────────────────────

    public async Task<ContrastConsentDto> CaptureContrastConsentAsync(long itemId, CaptureContrastConsentRequest request, CancellationToken ct = default)
    {
        // See VerifyReportAsync's comment — Including RadOrder is what applies branch scoping here.
        var item = await db.RadOrderItems.Include(i => i.RadiologyTest).Include(i => i.ContrastConsent).Include(i => i.RadOrder)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct)
            ?? throw new NotFoundException(nameof(RadOrderItem), itemId);
        if (!item.RadiologyTest!.RequiresContrast)
        {
            throw new DomainException("This test does not require a contrast consent.");
        }
        if (item.ContrastConsent is not null)
        {
            throw new DomainException("Contrast consent has already been captured for this test.");
        }

        var consent = new ContrastConsent
        {
            RadOrderItemId = itemId, SignedByName = request.SignedByName,
            RelationshipToPatient = request.RelationshipToPatient, SignedAt = timeProvider.GetUtcNow(),
        };
        db.ContrastConsents.Add(consent);
        await db.SaveChangesAsync(ct);
        return new ContrastConsentDto(consent.Id, consent.SignedByName, consent.RelationshipToPatient, consent.SignedAt);
    }

    // ── Image capture ────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<CaptureWorklistItemDto>> GetCaptureWorklistAsync(long? modalityId, CancellationToken ct = default)
    {
        var items = db.RadOrderItems.AsNoTracking().Where(i => i.Status == RadOrderItemStatus.Scheduled);
        if (modalityId is { } modId) items = items.Where(i => i.RadiologyTest!.ModalityId == modId);

        return await items
            .Include(i => i.RadOrder).ThenInclude(o => o!.Patient)
            .Include(i => i.RadiologyTest).ThenInclude(t => t!.Modality)
            .Include(i => i.Schedule)
            .Include(i => i.ContrastConsent)
            .OrderBy(i => i.Schedule!.ScheduledAt)
            .Select(i => new CaptureWorklistItemDto(
                i.Id, i.RadOrderId, i.RadOrder!.OrderNumber, i.RadOrder.PatientId, i.RadOrder.Patient!.Uhid,
                i.RadOrder.Patient!.FirstName + " " + i.RadOrder.Patient!.LastName, i.RadiologyTestId, i.RadiologyTest!.Name,
                i.RadiologyTest.ModalityId, i.RadiologyTest.Modality!.Name, i.RadiologyTest.RequiresContrast,
                i.ContrastConsent != null, i.Schedule!.ScheduledAt, i.RadOrder.Priority))
            .ToListAsync(ct);
    }

    public async Task MarkImageCapturedAsync(long itemId, CancellationToken ct = default)
    {
        var item = await db.RadOrderItems.Include(i => i.RadOrder).ThenInclude(o => o!.Items)
            .Include(i => i.RadiologyTest)
            .Include(i => i.ContrastConsent)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct)
            ?? throw new NotFoundException(nameof(RadOrderItem), itemId);
        if (item.Status != RadOrderItemStatus.Scheduled)
        {
            throw new DomainException($"A test that is {item.Status} cannot be marked as captured.");
        }
        if (item.RadiologyTest!.RequiresContrast && item.ContrastConsent is null)
        {
            throw new DomainException("Contrast consent must be captured before image capture for this test.");
        }

        item.Status = RadOrderItemStatus.ImageCaptured;
        item.ImageCapturedAt = timeProvider.GetUtcNow();
        item.ImageCapturedByUserId = currentUser.UserId;
        item.RadOrder!.Status = RadMath.RecomputeOrderStatus(item.RadOrder.Items.Select(i => i.Status).ToList());
        await db.SaveChangesAsync(ct);
    }

    // ── Reporting: entry / verify / approve ─────────────────────────────────

    public Task<IReadOnlyList<ReportingWorklistItemDto>> GetReportingEntryQueueAsync(CancellationToken ct = default) =>
        GetReportingQueueAsync(RadOrderItemStatus.ImageCaptured, ct);

    public Task<IReadOnlyList<ReportingWorklistItemDto>> GetReportingReviewQueueAsync(RadOrderItemStatus status, CancellationToken ct = default)
    {
        if (status is not (RadOrderItemStatus.ReportEntered or RadOrderItemStatus.Verified))
        {
            throw new DomainException("The review queue only covers items awaiting verification or approval.");
        }
        return GetReportingQueueAsync(status, ct);
    }

    private async Task<IReadOnlyList<ReportingWorklistItemDto>> GetReportingQueueAsync(RadOrderItemStatus status, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        return await db.RadOrderItems.AsNoTracking().Where(i => i.Status == status)
            .Include(i => i.RadOrder).ThenInclude(o => o!.Patient)
            .Include(i => i.RadiologyTest)
            .OrderBy(i => i.RadOrder!.Priority == ClinicalOrderPriority.Stat ? 0 : i.RadOrder!.Priority == ClinicalOrderPriority.Urgent ? 1 : 2)
            .ThenBy(i => i.RadOrder!.OrderDate)
            .Select(i => new ReportingWorklistItemDto(
                i.Id, i.RadOrderId, i.RadOrder!.OrderNumber, i.RadOrder.PatientId, i.RadOrder.Patient!.Uhid,
                i.RadOrder.Patient!.FirstName + " " + i.RadOrder.Patient!.LastName, i.RadOrder.Patient!.Gender.ToString(),
                FormatAge(i.RadOrder.Patient!.DateOfBirth, i.RadOrder.Patient!.EstimatedAge, now),
                i.RadiologyTestId, i.RadiologyTest!.Name, i.RadOrder.Priority, i.RadOrder.OrderDate))
            .ToListAsync(ct);
    }

    public async Task<RadReportDto> EnterReportAsync(long itemId, EnterReportRequest request, CancellationToken ct = default)
    {
        // See VerifyReportAsync's comment — Including RadOrder is what applies branch scoping here.
        var item = await db.RadOrderItems.Include(i => i.Report).Include(i => i.RadOrder)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct)
            ?? throw new NotFoundException(nameof(RadOrderItem), itemId);
        if (item.Status is not (RadOrderItemStatus.ImageCaptured or RadOrderItemStatus.ReportEntered))
        {
            throw new DomainException($"A test that is {item.Status} cannot accept a report.");
        }
        if (request.TemplateId is { } templateId && !await db.RadReportTemplates.AnyAsync(t => t.Id == templateId, ct))
        {
            throw new NotFoundException(nameof(RadReportTemplate), templateId);
        }

        var now = timeProvider.GetUtcNow();
        var report = item.Report ?? new RadReport
        {
            RadOrderItemId = item.Id, EnteredByUserId = currentUser.UserId, EnteredAt = now,
            Findings = request.Findings, Impression = request.Impression,
        };
        report.TemplateId = request.TemplateId;
        report.Findings = request.Findings;
        report.Impression = request.Impression;

        if (item.Report is null)
        {
            item.Report = report;
            db.RadReports.Add(report);
        }
        item.Status = RadOrderItemStatus.ReportEntered;

        await db.SaveChangesAsync(ct);
        return await GetReportByItemIdAsync(itemId, ct);
    }

    public async Task<RadReportDto> VerifyReportAsync(long itemId, CancellationToken ct = default)
    {
        // RadOrderItem itself isn't branch-scoped — Including RadOrder (a required navigation to a
        // BranchEntity) is what applies the branch query filter here, same as EnterReportAsync/
        // ApproveReportAsync below. Without it, this endpoint could verify another branch's report.
        var item = await db.RadOrderItems.Include(i => i.Report).Include(i => i.RadOrder)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct)
            ?? throw new NotFoundException(nameof(RadOrderItem), itemId);
        if (item.Status != RadOrderItemStatus.ReportEntered || item.Report is null)
        {
            throw new DomainException($"A test that is {item.Status} cannot be verified.");
        }

        item.Report.VerifiedByUserId = currentUser.UserId;
        item.Report.VerifiedAt = timeProvider.GetUtcNow();
        item.Status = RadOrderItemStatus.Verified;
        await db.SaveChangesAsync(ct);
        return await GetReportByItemIdAsync(itemId, ct);
    }

    public async Task<RadReportDto> ApproveReportAsync(long itemId, CancellationToken ct = default)
    {
        var item = await db.RadOrderItems.Include(i => i.Report)
            .Include(i => i.RadOrder).ThenInclude(o => o!.Items)
            .Include(i => i.RadiologyTest)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct)
            ?? throw new NotFoundException(nameof(RadOrderItem), itemId);
        if (item.Status != RadOrderItemStatus.Verified || item.Report is null)
        {
            throw new DomainException($"A test that is {item.Status} cannot be approved.");
        }

        item.Report.ApprovedByUserId = currentUser.UserId;
        item.Report.ApprovedAt = timeProvider.GetUtcNow();
        item.Status = RadOrderItemStatus.Approved;

        var order = item.RadOrder!;
        order.Status = RadMath.RecomputeOrderStatus(order.Items.Select(i => i.Status).ToList());
        if (order.Status == RadOrderStatus.Completed && order.ClinicalOrderId is { } clinicalOrderId)
        {
            var clinicalOrder = await db.ClinicalOrders.FirstOrDefaultAsync(o => o.Id == clinicalOrderId, ct);
            if (clinicalOrder is not null)
            {
                clinicalOrder.Status = ClinicalOrderStatus.Completed;
            }
        }

        await activity.LogAsync(ActivityEvents.RadReportApproved, $"Report approved for {item.RadiologyTest!.Name} on order {order.OrderNumber}", ct: ct);
        await db.SaveChangesAsync(ct);
        return await GetReportByItemIdAsync(itemId, ct);
    }

    public async Task<RadReportDto> GetReportByItemIdAsync(long itemId, CancellationToken ct = default)
    {
        // See GetResultByItemIdAsync's Lab counterpart — cascading through the required
        // RadOrderItem -> RadOrder chain is what keeps this view endpoint branch-scoped.
        var report = await db.RadReports.AsNoTracking()
            .Include(r => r.EnteredByUser).Include(r => r.VerifiedByUser).Include(r => r.ApprovedByUser)
            .Include(r => r.RadOrderItem).ThenInclude(i => i!.RadOrder)
            .FirstOrDefaultAsync(r => r.RadOrderItemId == itemId, ct)
            ?? throw new NotFoundException(nameof(RadReport), itemId);
        return ToReportDto(report);
    }

    // ── Report PDF / TAT ─────────────────────────────────────────────────────

    public async Task<byte[]> GenerateReportPdfAsync(long itemId, CancellationToken ct = default)
    {
        var item = await db.RadOrderItems.AsNoTracking()
            .Include(i => i.RadOrder).ThenInclude(o => o!.Patient)
            .Include(i => i.RadOrder).ThenInclude(o => o!.OrderedByDoctor).ThenInclude(d => d!.User)
            .Include(i => i.RadiologyTest).ThenInclude(t => t!.Modality)
            .Include(i => i.Report).ThenInclude(r => r!.ApprovedByUser)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct)
            ?? throw new NotFoundException(nameof(RadOrderItem), itemId);
        if (item.Status != RadOrderItemStatus.Approved || item.Report is null)
        {
            throw new DomainException("This report has not been approved yet.");
        }

        var order = item.RadOrder!;
        var branch = await db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == order.BranchId, ct);
        var now = timeProvider.GetUtcNow();

        return pdf.GenerateRadiologyReport(new RadiologyReportData(
            order.OrderNumber, item.AccessionNumber, order.OrderDate, FormatName(order.Patient!), order.Patient!.Uhid,
            order.Patient!.Gender.ToString(), FormatAge(order.Patient!.DateOfBirth, order.Patient!.EstimatedAge, now),
            branch?.Name ?? string.Empty, branch?.Address ?? string.Empty, order.OrderedByDoctor?.User?.FullName,
            item.RadiologyTest!.Name, item.RadiologyTest.Modality!.Name, item.RadiologyTest.BodyPart,
            item.Report.Findings, item.Report.Impression, item.Report.ApprovedByUser?.FullName, item.Report.ApprovedAt));
    }

    public async Task<IReadOnlyList<RadTatReportRowDto>> GetTatReportAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
    {
        var rows = await db.RadOrderItems.AsNoTracking()
            .Where(i => i.Status == RadOrderItemStatus.Approved
                && i.RadOrder!.OrderDate >= fromDate && i.RadOrder!.OrderDate <= toDate)
            .Include(i => i.RadOrder)
            .Include(i => i.RadiologyTest)
            .Include(i => i.Schedule)
            .Include(i => i.Report)
            .Select(i => new RadTatReportRowDto(
                i.Id, i.RadOrder!.OrderNumber, i.RadiologyTest!.Name, i.RadOrder.OrderDate,
                i.Schedule != null ? i.Schedule.ScheduledAt : (DateTimeOffset?)null,
                i.ImageCapturedAt, i.Report != null ? i.Report.ApprovedAt : null, null))
            .ToListAsync(ct);

        return rows.Select(r => r with
        {
            TurnaroundHours = r.ApprovedAt is { } approved
                ? (approved - new DateTimeOffset(r.OrderDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)).TotalHours
                : null,
        }).ToList();
    }

    private static readonly RadOrderItemStatus[] ResultedStatuses =
        [RadOrderItemStatus.ReportEntered, RadOrderItemStatus.Verified, RadOrderItemStatus.Approved];

    /// <summary>Mirrors Lab's GetUnbilledOrdersReportAsync — see its doc comment.</summary>
    public async Task<IReadOnlyList<UnbilledRadOrderRowDto>> GetUnbilledOrdersReportAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct = default) =>
        await db.RadOrderItems.AsNoTracking()
            .Where(i => i.BillItemId == null && ResultedStatuses.Contains(i.Status)
                && i.RadOrder!.OrderDate >= fromDate && i.RadOrder!.OrderDate <= toDate)
            .Include(i => i.RadOrder).ThenInclude(o => o!.Patient)
            .Include(i => i.RadiologyTest)
            .OrderBy(i => i.RadOrder!.OrderDate)
            .Select(i => new UnbilledRadOrderRowDto(
                i.Id, i.RadOrder!.OrderNumber, i.RadOrder.OrderDate, i.RadOrder.Patient!.Uhid,
                i.RadOrder.Patient!.FirstName + " " + i.RadOrder.Patient!.LastName, i.RadiologyTest!.Name, i.Status))
            .ToListAsync(ct);

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch before ordering radiology tests.");

    private static string FormatName(Patient patient) =>
        string.Join(' ', new[] { patient.FirstName, patient.MiddleName, patient.LastName }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

    private static string FormatAge(DateOnly? dateOfBirth, int? estimatedAge, DateTimeOffset now)
    {
        if (dateOfBirth is { } dob)
        {
            var today = DateOnly.FromDateTime(now.UtcDateTime);
            var years = today.Year - dob.Year;
            if (dob > today.AddYears(-years))
            {
                years--;
            }
            if (years >= 2)
            {
                return $"{years} yr(s)";
            }
            var months = ((today.Year - dob.Year) * 12) + today.Month - dob.Month - (today.Day < dob.Day ? 1 : 0);
            return months <= 0 ? "< 1 month" : $"{months} month(s)";
        }
        return estimatedAge is { } age ? $"~{age} yr(s) (estimated)" : "Unknown";
    }

    private static RadReportDto ToReportDto(RadReport r) => new(
        r.Id, r.RadOrderItemId, r.TemplateId, r.Findings, r.Impression, r.EnteredByUserId, r.EnteredByUser?.FullName, r.EnteredAt,
        r.VerifiedByUserId, r.VerifiedByUser?.FullName, r.VerifiedAt, r.ApprovedByUserId, r.ApprovedByUser?.FullName, r.ApprovedAt);

    private static RadOrderDetailDto ToDetailDto(RadOrder o) => new(
        o.Id, o.OrderNumber, o.OrderDate, o.PatientId, o.Patient!.Uhid, FormatName(o.Patient!), o.Patient!.Gender.ToString(),
        FormatAge(o.Patient!.DateOfBirth, o.Patient!.EstimatedAge, DateTimeOffset.UtcNow), o.OpVisitId, o.AdmissionId, o.EmergencyVisitId,
        o.OrderedByDoctorId, o.OrderedByDoctor?.User?.FullName, o.Priority, o.Status, o.ClinicalNotes, o.CancelReason, o.CancelledAt,
        o.Items.OrderBy(i => i.Id).Select(i => new RadOrderItemDto(
            i.Id, i.RadiologyTestId, i.RadiologyTest!.Name, i.RadiologyTest.Modality!.Name, i.AccessionNumber, i.Status,
            i.RadiologyTest.RequiresContrast, i.ContrastConsent != null, i.Schedule?.ScheduledAt)).ToList());
}
