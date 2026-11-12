using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Application.Features.Admin.Settings;
using Prixo.Application.Features.Billing;
using Prixo.Domain.Entities.Lab;
using Prixo.Domain.Entities.Opd;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Lab;

/// <summary>
/// Lab order lifecycle: create (from a visit, a loosely-linked ClinicalOrder, or a direct
/// walk-in), sample collection/receipt/rejection, result entry with automatic
/// reference-range flagging and critical alerts, and the validate→verify→approve pipeline.
/// No hard billing gate — LabOrderItem.BillItemId is a soft link set by front desk once
/// billed, not enforced here (see docs/modules/lab.md).
/// </summary>
public class LabOrderService(
    IAppDbContext db, ISequenceService sequences, ICurrentUser currentUser,
    IActivityLogger activity, IPdfService pdf, TimeProvider timeProvider, SettingService settings)
{
    // ── Orders ───────────────────────────────────────────────────────────────

    public async Task<LabOrderDetailDto> CreateOrderAsync(CreateLabOrderRequest request, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        if (!await db.Patients.AnyAsync(p => p.Id == request.PatientId, ct))
        {
            throw new NotFoundException(nameof(Patient), request.PatientId);
        }

        var labTestIds = new List<(long LabTestId, long? TestProfileId)>();
        foreach (var item in request.Items)
        {
            if (item.TestProfileId is { } profileId)
            {
                var members = await db.ProfileTests.AsNoTracking()
                    .Where(pt => pt.TestProfileId == profileId)
                    .Select(pt => pt.LabTestId)
                    .ToListAsync(ct);
                if (members.Count == 0)
                {
                    throw new NotFoundException(nameof(TestProfile), profileId);
                }
                labTestIds.AddRange(members.Select(id => (id, (long?)profileId)));
            }
            else if (item.LabTestId is { } testId)
            {
                labTestIds.Add((testId, null));
            }
        }
        if (labTestIds.Count == 0)
        {
            throw new DomainException("Select at least one test or profile.");
        }
        var distinctTests = labTestIds.Select(t => t.LabTestId).Distinct().ToList();
        if (distinctTests.Count != labTestIds.Count)
        {
            throw new DomainException("The same test appears more than once in this order (check selected tests and profiles).");
        }
        var known = await db.LabTests.AsNoTracking().Where(t => distinctTests.Contains(t.Id)).Select(t => t.Id).ToListAsync(ct);
        var missing = distinctTests.Except(known).ToList();
        if (missing.Count > 0)
        {
            throw new NotFoundException(nameof(LabTest), missing[0]);
        }
        var pendingDuplicate = await db.LabOrderItems.AsNoTracking()
            .Where(i => i.LabOrder!.PatientId == request.PatientId && i.Status == LabOrderItemStatus.Ordered && distinctTests.Contains(i.LabTestId))
            .Select(i => new { i.LabTest!.Name, i.LabOrder!.OrderNumber }).FirstOrDefaultAsync(ct);
        if (pendingDuplicate is not null)
        {
            throw new DomainException($"{pendingDuplicate.Name} is already ordered for this patient on {pendingDuplicate.OrderNumber} and is awaiting sample collection.");
        }
        if (request.AdmissionId is { } admissionId &&
            !await db.Admissions.AnyAsync(a => a.Id == admissionId && a.PatientId == request.PatientId && a.Status == AdmissionStatus.Admitted, ct))
        {
            throw new DomainException("The admission was not found, is not active, or belongs to a different patient.");
        }

        ClinicalOrder? clinicalOrder = null;
        if (request.ClinicalOrderId is { } clinicalOrderId)
        {
            clinicalOrder = await db.ClinicalOrders.FirstOrDefaultAsync(o => o.Id == clinicalOrderId, ct)
                ?? throw new NotFoundException(nameof(ClinicalOrder), clinicalOrderId);
            clinicalOrder.Status = ClinicalOrderStatus.InProgress;
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var year = today.Year;
        var orderNumberValue = await sequences.NextValueAsync(SequenceKeys.LabOrderNumber, branchId, year, ct);

        var order = new LabOrder
        {
            BranchId = branchId,
            PatientId = request.PatientId,
            OpVisitId = request.OpVisitId,
            EmergencyVisitId = request.EmergencyVisitId,
            AdmissionId = request.AdmissionId,
            ClinicalOrderId = request.ClinicalOrderId,
            OrderedByDoctorId = request.OrderedByDoctorId,
            OrderNumber = $"LAB-{year}-{orderNumberValue.ToString().PadLeft(6, '0')}",
            OrderDate = today,
            Priority = request.Priority,
            ClinicalNotes = request.ClinicalNotes,
        };
        foreach (var (labTestId, testProfileId) in labTestIds)
        {
            order.Items.Add(new LabOrderItem { LabTestId = labTestId, TestProfileId = testProfileId });
        }

        db.LabOrders.Add(order);
        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(order.Id, ct);
    }

    public async Task<PagedResult<LabOrderListItemDto>> GetPagedAsync(LabOrderQuery query, CancellationToken ct = default)
    {
        var orders = db.LabOrders.AsNoTracking();

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
            .Select(o => new LabOrderListItemDto(
                o.Id, o.OrderNumber, o.OrderDate, o.PatientId, o.Patient!.Uhid,
                o.Patient!.FirstName + " " + o.Patient!.LastName, o.Priority, o.Status, o.Items.Count))
            .ToListAsync(ct);

        return PagedResult<LabOrderListItemDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<LabOrderDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var order = await db.LabOrders.AsNoTracking()
            .Include(o => o.Patient)
            .Include(o => o.OrderedByDoctor).ThenInclude(d => d!.User)
            .Include(o => o.Items).ThenInclude(i => i.LabTest)
            .Include(o => o.Items).ThenInclude(i => i.TestProfile)
            .Include(o => o.Items).ThenInclude(i => i.Sample)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException(nameof(LabOrder), id);
        return ToDetailDto(order);
    }

    public async Task CancelOrderAsync(long id, CancelLabOrderRequest request, CancellationToken ct = default)
    {
        var order = await db.LabOrders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException(nameof(LabOrder), id);
        if (order.Items.Any(i => i.Status != LabOrderItemStatus.Ordered))
        {
            throw new DomainException("A sample has already been collected against this order — cancel the remaining items individually instead.");
        }

        foreach (var item in order.Items)
        {
            item.Status = LabOrderItemStatus.Cancelled;
        }
        order.Status = LabOrderStatus.Cancelled;
        order.CancelReason = request.Reason;
        order.CancelledAt = timeProvider.GetUtcNow();

        await activity.LogAsync(ActivityEvents.LabOrderCancelled, $"Lab order {order.OrderNumber} cancelled: {request.Reason}", ct: ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task CancelItemAsync(long orderId, long itemId, CancellationToken ct = default)
    {
        var order = await db.LabOrders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new NotFoundException(nameof(LabOrder), orderId);
        var item = order.Items.FirstOrDefault(i => i.Id == itemId) ?? throw new NotFoundException(nameof(LabOrderItem), itemId);
        if (item.Status != LabOrderItemStatus.Ordered)
        {
            throw new DomainException("A sample has already been collected for this test — it can no longer be cancelled on its own.");
        }

        item.Status = LabOrderItemStatus.Cancelled;
        order.Status = LabMath.RecomputeOrderStatus(order.Items.Select(i => i.Status).ToList());
        await db.SaveChangesAsync(ct);
    }

    // ── Collection ───────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<CollectionWorklistItemDto>> GetCollectionWorklistAsync(CancellationToken ct = default) =>
        await db.LabOrderItems.AsNoTracking()
            .Where(i => i.Status == LabOrderItemStatus.Ordered)
            .Include(i => i.LabOrder).ThenInclude(o => o!.Patient)
            .Include(i => i.LabTest)
            .OrderBy(i => i.LabOrder!.Priority == ClinicalOrderPriority.Stat ? 0 : i.LabOrder!.Priority == ClinicalOrderPriority.Urgent ? 1 : 2)
            .ThenBy(i => i.LabOrder!.OrderDate)
            .Select(i => new CollectionWorklistItemDto(
                i.Id, i.LabOrderId, i.LabOrder!.OrderNumber, i.LabOrder.PatientId, i.LabOrder.Patient!.Uhid,
                i.LabOrder.Patient!.FirstName + " " + i.LabOrder.Patient!.LastName, i.LabTestId, i.LabTest!.Name,
                i.LabTest.SampleTypeId, i.LabTest.SampleType!.Name, i.LabOrder.Priority, i.LabOrder.OrderDate))
            .ToListAsync(ct);

    public async Task<SampleDto> CollectSampleAsync(CollectSampleRequest request, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var order = await db.LabOrders.Include(o => o.Items).ThenInclude(i => i.LabTest)
            .FirstOrDefaultAsync(o => o.Id == request.LabOrderId, ct)
            ?? throw new NotFoundException(nameof(LabOrder), request.LabOrderId);

        var items = order.Items.Where(i => request.LabOrderItemIds.Contains(i.Id)).ToList();
        if (items.Count != request.LabOrderItemIds.Count)
        {
            throw new DomainException("One or more selected items do not belong to this order.");
        }
        if (items.Any(i => i.Status != LabOrderItemStatus.Ordered))
        {
            throw new DomainException("One or more selected items are not awaiting collection.");
        }
        if (items.Any(i => i.LabTest!.SampleTypeId != request.SampleTypeId))
        {
            throw new DomainException("All selected tests must use the same sample type.");
        }
        if (order.OpVisitId is not null && order.EmergencyVisitId is null && order.AdmissionId is null)
        {
            await DiagnosticsBillingGate.EnsureBilledAsync(db, settings, items.Select(i => i.BillItemId).ToList(),
                string.Join(", ", items.Select(i => i.LabTest!.Name)), ct);
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var sampleNumberValue = await sequences.NextValueAsync(SequenceKeys.LabSampleNumber, branchId, today.Year, ct);
        var sample = new Sample
        {
            BranchId = branchId,
            LabOrderId = order.Id,
            SampleTypeId = request.SampleTypeId,
            SampleNumber = $"SMP-{today.Year}-{sampleNumberValue.ToString().PadLeft(6, '0')}",
            CollectedAt = timeProvider.GetUtcNow(),
            CollectedByUserId = currentUser.UserId,
        };
        db.Samples.Add(sample);

        foreach (var item in items)
        {
            item.Sample = sample;
            item.Status = LabOrderItemStatus.Collected;
        }
        order.Status = LabMath.RecomputeOrderStatus(order.Items.Select(i => i.Status).ToList());

        await db.SaveChangesAsync(ct);
        return await GetSampleByIdAsync(sample.Id, ct);
    }

    public async Task<IReadOnlyList<SampleDto>> GetSamplesAwaitingReceiptAsync(CancellationToken ct = default) =>
        await db.Samples.AsNoTracking()
            .Where(s => s.Status == SampleStatus.Collected)
            .Include(s => s.SampleType)
            .Include(s => s.LabOrder)
            .OrderBy(s => s.CollectedAt)
            .Select(s => new SampleDto(
                s.Id, s.LabOrderId, s.LabOrder!.OrderNumber, s.SampleTypeId, s.SampleType!.Name, s.SampleNumber,
                s.Status, s.CollectedAt, s.ReceivedAt, s.RejectionReason, s.RejectedAt))
            .ToListAsync(ct);

    public async Task<SampleDto> ReceiveSampleAsync(long sampleId, CancellationToken ct = default)
    {
        var sample = await db.Samples.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == sampleId, ct)
            ?? throw new NotFoundException(nameof(Sample), sampleId);
        if (sample.Status != SampleStatus.Collected)
        {
            throw new DomainException($"A sample that is {sample.Status} cannot be received again.");
        }

        sample.Status = SampleStatus.Received;
        sample.ReceivedAt = timeProvider.GetUtcNow();
        sample.ReceivedByUserId = currentUser.UserId;
        foreach (var item in sample.Items.Where(i => i.Status == LabOrderItemStatus.Collected))
        {
            item.Status = LabOrderItemStatus.Received;
        }

        await db.SaveChangesAsync(ct);
        return await GetSampleByIdAsync(sampleId, ct);
    }

    public async Task RejectSampleAsync(long sampleId, RejectSampleRequest request, CancellationToken ct = default)
    {
        var sample = await db.Samples.Include(s => s.Items).ThenInclude(i => i.LabOrder).ThenInclude(o => o!.Items)
            .FirstOrDefaultAsync(s => s.Id == sampleId, ct)
            ?? throw new NotFoundException(nameof(Sample), sampleId);
        if (sample.Status == SampleStatus.Rejected)
        {
            throw new DomainException("This sample is already rejected.");
        }

        sample.Status = SampleStatus.Rejected;
        sample.RejectionReason = request.Reason;
        sample.RejectedAt = timeProvider.GetUtcNow();
        sample.RejectedByUserId = currentUser.UserId;

        foreach (var item in sample.Items)
        {
            item.SampleId = null;
            item.Status = LabOrderItemStatus.Ordered;
        }
        foreach (var order in sample.Items.Select(i => i.LabOrder!).DistinctBy(o => o.Id))
        {
            order.Status = LabMath.RecomputeOrderStatus(order.Items.Select(i => i.Status).ToList());
        }

        await activity.LogAsync(ActivityEvents.LabSampleRejected, $"Sample {sample.SampleNumber} rejected: {request.Reason}", ct: ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task<SampleDto> GetSampleByIdAsync(long id, CancellationToken ct)
    {
        var sample = await db.Samples.AsNoTracking()
            .Include(s => s.SampleType)
            .Include(s => s.LabOrder)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException(nameof(Sample), id);
        return ToSampleDto(sample);
    }

    // ── Result entry / verify / approve ─────────────────────────────────────

    public Task<IReadOnlyList<DepartmentWorklistItemDto>> GetDepartmentWorklistAsync(
        long? departmentId, CancellationToken ct = default) =>
        GetItemsByStatusAsync(LabOrderItemStatus.Received, departmentId, ct);

    /// <summary>The verify/approve queues — same shape as the department (entry) worklist, filtered by pipeline stage instead.</summary>
    public Task<IReadOnlyList<DepartmentWorklistItemDto>> GetResultQueueAsync(
        LabOrderItemStatus status, CancellationToken ct = default)
    {
        if (status is not (LabOrderItemStatus.ResultEntered or LabOrderItemStatus.Verified))
        {
            throw new DomainException("The result queue only covers items awaiting verification or approval.");
        }
        return GetItemsByStatusAsync(status, null, ct);
    }

    private async Task<IReadOnlyList<DepartmentWorklistItemDto>> GetItemsByStatusAsync(
        LabOrderItemStatus status, long? departmentId, CancellationToken ct)
    {
        var items = db.LabOrderItems.AsNoTracking().Where(i => i.Status == status);
        if (departmentId is { } deptId)
        {
            items = items.Where(i => i.LabTest!.DepartmentId == deptId);
        }

        var now = timeProvider.GetUtcNow();
        return await items
            .Include(i => i.LabOrder).ThenInclude(o => o!.Patient)
            .Include(i => i.LabTest).ThenInclude(t => t!.Department)
            .OrderBy(i => i.LabOrder!.Priority == ClinicalOrderPriority.Stat ? 0 : i.LabOrder!.Priority == ClinicalOrderPriority.Urgent ? 1 : 2)
            .ThenBy(i => i.LabOrder!.OrderDate)
            .Select(i => new DepartmentWorklistItemDto(
                i.Id, i.LabOrderId, i.LabOrder!.OrderNumber, i.LabOrder.PatientId, i.LabOrder.Patient!.Uhid,
                i.LabOrder.Patient!.FirstName + " " + i.LabOrder.Patient!.LastName, i.LabOrder.Patient!.Gender.ToString(),
                FormatAge(i.LabOrder.Patient!.DateOfBirth, i.LabOrder.Patient!.EstimatedAge, now),
                i.LabTestId, i.LabTest!.Name, i.LabTest.DepartmentId, i.LabTest.Department != null ? i.LabTest.Department.Name : null,
                i.LabOrder.Priority, i.LabOrder.OrderDate))
            .ToListAsync(ct);
    }

    public async Task<LabResultDto> EnterResultAsync(long itemId, EnterResultRequest request, CancellationToken ct = default)
    {
        var item = await db.LabOrderItems
            .Include(i => i.LabOrder).ThenInclude(o => o!.Patient)
            .Include(i => i.LabTest).ThenInclude(t => t!.Parameters).ThenInclude(p => p.ReferenceRanges)
            .Include(i => i.Result).ThenInclude(r => r!.Values)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct)
            ?? throw new NotFoundException(nameof(LabOrderItem), itemId);
        if (item.Status is not (LabOrderItemStatus.Received or LabOrderItemStatus.ResultEntered))
        {
            throw new DomainException($"A test that is {item.Status} cannot accept results.");
        }

        var patient = item.LabOrder!.Patient!;
        var now = timeProvider.GetUtcNow();
        var ageYears = LabMath.CalculateAgeYears(patient.DateOfBirth, patient.EstimatedAge, DateOnly.FromDateTime(now.UtcDateTime));

        if (item.Result is not null)
        {
            db.ResultValues.RemoveRange(item.Result.Values);
            db.CriticalAlerts.RemoveRange(await db.CriticalAlerts.Where(a => a.LabResultId == item.Result.Id).ToListAsync(ct));
        }

        var result = item.Result ?? new LabResult { LabOrderItemId = item.Id, EnteredByUserId = currentUser.UserId, EnteredAt = now };
        result.Notes = request.Notes;
        result.HasCriticalValue = false;

        var parametersById = item.LabTest!.Parameters.ToDictionary(p => p.Id);
        result.Values = request.Values.Select(v =>
        {
            var parameter = parametersById.TryGetValue(v.LabTestParameterId, out var p)
                ? p : throw new DomainException($"Parameter {v.LabTestParameterId} does not belong to this test.");

            var flag = ResultFlag.Normal;
            decimal? normalLow = null, normalHigh = null;
            if (string.IsNullOrWhiteSpace(v.Value))
            {
                throw new DomainException($"Enter a value for {parameter.Name}.");
            }
            if (parameter.ValueType == ParameterValueType.Numeric &&
                !decimal.TryParse(v.Value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out _))
            {
                throw new DomainException($"{parameter.Name} must be a number (got '{v.Value}').");
            }
            if (parameter.ValueType == ParameterValueType.Numeric &&
                decimal.TryParse(v.Value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var numeric))
            {
                // A negative value is impossible for a measure whose reference ranges are all
                // non-negative (APTT, Hb, glucose...); measures like base excess keep negatives.
                if (numeric < 0 && parameter.ReferenceRanges.Count > 0 && parameter.ReferenceRanges.All(r => (r.NormalLow ?? 0) >= 0))
                {
                    throw new DomainException($"{parameter.Name} cannot be negative.");
                }
                var candidates = parameter.ReferenceRanges.Select(r => new ReferenceRangeCandidate(
                    r.Id, r.Sex, r.MinAgeYears, r.MaxAgeYears, r.NormalLow, r.NormalHigh, r.CriticalLow, r.CriticalHigh, r.TextNormalValue));
                var range = LabMath.ResolveRange(candidates, patient.Gender, ageYears);
                flag = LabMath.ResolveFlag(numeric, range);
                normalLow = range?.NormalLow;
                normalHigh = range?.NormalHigh;
            }

            if (flag == ResultFlag.Critical)
            {
                result.HasCriticalValue = true;
            }
            return new ResultValue
            {
                LabTestParameterId = parameter.Id,
                Value = v.Value,
                Unit = parameter.Unit,
                NormalLow = normalLow,
                NormalHigh = normalHigh,
                Flag = flag,
            };
        }).ToList();

        if (item.Result is null)
        {
            item.Result = result;
            db.LabResults.Add(result);
        }
        item.Status = LabOrderItemStatus.ResultEntered;

        await db.SaveChangesAsync(ct);

        if (result.HasCriticalValue)
        {
            foreach (var value in result.Values.Where(v => v.Flag == ResultFlag.Critical))
            {
                db.CriticalAlerts.Add(new CriticalAlert
                {
                    BranchId = item.LabOrder!.BranchId,
                    LabResultId = result.Id,
                    ResultValueId = value.Id,
                    RaisedAt = now,
                });
            }
            await activity.LogAsync(ActivityEvents.LabCriticalAlertRaised,
                $"Critical value on order {item.LabOrder!.OrderNumber} ({item.LabTest!.Name})", ct: ct);
            await db.SaveChangesAsync(ct);
        }

        return await GetResultByItemIdAsync(itemId, ct);
    }

    public async Task<LabResultDto> VerifyResultAsync(long itemId, CancellationToken ct = default)
    {
        // LabOrderItem itself isn't branch-scoped — Including LabOrder (a required navigation to a
        // BranchEntity) is what applies the branch query filter here, same as EnterResultAsync/
        // ApproveResultAsync below. Without it, this endpoint could verify another branch's result.
        var item = await db.LabOrderItems.Include(i => i.Result).Include(i => i.LabOrder)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct)
            ?? throw new NotFoundException(nameof(LabOrderItem), itemId);
        if (item.Status != LabOrderItemStatus.ResultEntered || item.Result is null)
        {
            throw new DomainException($"A test that is {item.Status} cannot be verified.");
        }

        item.Result.VerifiedByUserId = currentUser.UserId;
        item.Result.VerifiedAt = timeProvider.GetUtcNow();
        item.Status = LabOrderItemStatus.Verified;
        await db.SaveChangesAsync(ct);
        return await GetResultByItemIdAsync(itemId, ct);
    }

    public async Task<LabResultDto> ApproveResultAsync(long itemId, CancellationToken ct = default)
    {
        var item = await db.LabOrderItems.Include(i => i.Result)
            .Include(i => i.LabOrder).ThenInclude(o => o!.Items)
            .Include(i => i.LabTest)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct)
            ?? throw new NotFoundException(nameof(LabOrderItem), itemId);
        if (item.Status != LabOrderItemStatus.Verified || item.Result is null)
        {
            throw new DomainException($"A test that is {item.Status} cannot be approved.");
        }

        item.Result.ApprovedByUserId = currentUser.UserId;
        item.Result.ApprovedAt = timeProvider.GetUtcNow();
        item.Status = LabOrderItemStatus.Approved;

        var order = item.LabOrder!;
        order.Status = LabMath.RecomputeOrderStatus(order.Items.Select(i => i.Status).ToList());
        if (order.Status == LabOrderStatus.Completed && order.ClinicalOrderId is { } clinicalOrderId)
        {
            var clinicalOrder = await db.ClinicalOrders.FirstOrDefaultAsync(o => o.Id == clinicalOrderId, ct);
            if (clinicalOrder is not null)
            {
                clinicalOrder.Status = ClinicalOrderStatus.Completed;
            }
        }

        await activity.LogAsync(ActivityEvents.LabResultApproved, $"Result approved for {item.LabTest!.Name} on order {order.OrderNumber}", ct: ct);
        await db.SaveChangesAsync(ct);
        return await GetResultByItemIdAsync(itemId, ct);
    }

    public async Task<LabResultDto> GetResultByItemIdAsync(long itemId, CancellationToken ct = default)
    {
        // LabResult isn't branch-scoped itself; cascading through the required LabOrderItem ->
        // LabOrder chain (same idiom as EnterResultAsync/VerifyResultAsync/ApproveResultAsync) is
        // what keeps this view endpoint from returning another branch's result.
        var result = await db.LabResults.AsNoTracking()
            .Include(r => r.Values).ThenInclude(v => v.LabTestParameter)
            .Include(r => r.LabOrderItem).ThenInclude(i => i!.LabOrder)
            .FirstOrDefaultAsync(r => r.LabOrderItemId == itemId, ct)
            ?? throw new NotFoundException(nameof(LabResult), itemId);
        return ToResultDto(result);
    }

    // ── Critical alerts ──────────────────────────────────────────────────────

    public async Task<IReadOnlyList<CriticalAlertDto>> GetPendingCriticalAlertsAsync(CancellationToken ct = default) =>
        await db.CriticalAlerts.AsNoTracking()
            .Where(a => !a.IsAcknowledged)
            .Include(a => a.ResultValue).ThenInclude(v => v!.LabTestParameter)
            .Include(a => a.LabResult).ThenInclude(r => r!.LabOrderItem).ThenInclude(i => i!.LabOrder).ThenInclude(o => o!.Patient)
            .OrderBy(a => a.RaisedAt)
            .Select(a => new CriticalAlertDto(
                a.Id, a.LabResultId, a.LabResult!.LabOrderItemId, a.LabResult.LabOrderItem!.LabOrder!.OrderNumber,
                a.LabResult.LabOrderItem.LabOrder!.Patient!.FirstName + " " + a.LabResult.LabOrderItem.LabOrder!.Patient!.LastName,
                a.ResultValueId, a.ResultValue!.LabTestParameter!.Name, a.ResultValue.Value, a.RaisedAt,
                a.IsAcknowledged, a.AcknowledgedByUserId, a.AcknowledgedAt, a.AcknowledgementNotes))
            .ToListAsync(ct);

    public async Task AcknowledgeAlertAsync(long alertId, AcknowledgeAlertRequest request, CancellationToken ct = default)
    {
        var alert = await db.CriticalAlerts.FirstOrDefaultAsync(a => a.Id == alertId, ct)
            ?? throw new NotFoundException(nameof(CriticalAlert), alertId);
        if (alert.IsAcknowledged)
        {
            throw new DomainException("This alert is already acknowledged.");
        }

        alert.IsAcknowledged = true;
        alert.AcknowledgedByUserId = currentUser.UserId;
        alert.AcknowledgedAt = timeProvider.GetUtcNow();
        alert.AcknowledgementNotes = request.Notes;

        await activity.LogAsync(ActivityEvents.LabCriticalAlertAcknowledged, $"Critical alert {alertId} acknowledged", ct: ct);
        await db.SaveChangesAsync(ct);
    }

    // ── Report / TAT ─────────────────────────────────────────────────────────

    public async Task<byte[]> GenerateReportAsync(long orderId, CancellationToken ct = default)
    {
        var order = await db.LabOrders.AsNoTracking()
            .Include(o => o.Patient)
            .Include(o => o.OrderedByDoctor).ThenInclude(d => d!.User)
            .Include(o => o.Items).ThenInclude(i => i.LabTest)
            .Include(o => o.Items).ThenInclude(i => i.Result).ThenInclude(r => r!.Values).ThenInclude(v => v.LabTestParameter)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new NotFoundException(nameof(LabOrder), orderId);
        var branch = await db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == order.BranchId, ct);

        var approvedItems = order.Items.Where(i => i.Status == LabOrderItemStatus.Approved && i.Result is not null).ToList();
        if (approvedItems.Count == 0)
        {
            throw new DomainException("No approved results yet for this order.");
        }

        var now = timeProvider.GetUtcNow();
        var sections = approvedItems.Select(i => new LabReportTestSection(
            i.LabTest!.Name, i.LabTest.Method,
            i.Result!.Values.Select(v => new LabReportValueLine(
                v.LabTestParameter!.Name, v.Value, v.Unit,
                FormatRangeDisplay(v.NormalLow, v.NormalHigh), v.Flag != ResultFlag.Normal, v.Flag == ResultFlag.Critical))
                .ToList(),
            i.Result.Notes)).ToList();

        var data = new LabReportData(
            order.OrderNumber, order.OrderDate, FormatName(order.Patient!), order.Patient!.Uhid,
            order.Patient!.Gender.ToString(), FormatAge(order.Patient!.DateOfBirth, order.Patient!.EstimatedAge, now),
            branch?.Name ?? string.Empty, branch?.Address ?? string.Empty,
            order.OrderedByDoctor?.User?.FullName, sections,
            approvedItems.Max(i => i.Result!.ApprovedAt), null);
        return pdf.GenerateLabReport(data);
    }

    public async Task<IReadOnlyList<TatReportRowDto>> GetTatReportAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
    {
        var rows = await db.LabOrderItems.AsNoTracking()
            .Where(i => i.Status == LabOrderItemStatus.Approved
                && i.LabOrder!.OrderDate >= fromDate && i.LabOrder!.OrderDate <= toDate)
            .Include(i => i.LabOrder)
            .Include(i => i.LabTest)
            .Include(i => i.Sample)
            .Include(i => i.Result)
            .Select(i => new TatReportRowDto(
                i.Id, i.LabOrder!.OrderNumber, i.LabTest!.Name, i.LabOrder.OrderDate,
                i.Sample != null ? i.Sample.CollectedAt : (DateTimeOffset?)null,
                i.Sample != null ? i.Sample.ReceivedAt : null,
                i.Result != null ? i.Result.ApprovedAt : null, null))
            .ToListAsync(ct);

        // OrderDate has no time component, so this TAT is approximate — documented in docs/modules/lab.md.
        return rows.Select(r => r with
        {
            TurnaroundHours = r.ApprovedAt is { } approved
                ? (approved - new DateTimeOffset(r.OrderDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)).TotalHours
                : null,
        }).ToList();
    }

    private static readonly LabOrderItemStatus[] ResultedStatuses =
        [LabOrderItemStatus.ResultEntered, LabOrderItemStatus.Verified, LabOrderItemStatus.Approved];

    /// <summary>
    /// "Unbilled Orders Report" — every item with a result entered (or further along) that
    /// still has no BillItemId, closing docs/PATIENT_JOURNEY_GAP_ANALYSIS.md §4.3. Advisory
    /// only; see LabOrderItem.BillItemId's own doc comment ("It is not an enforced gate").
    /// </summary>
    public async Task<IReadOnlyList<UnbilledLabOrderRowDto>> GetUnbilledOrdersReportAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct = default) =>
        await db.LabOrderItems.AsNoTracking()
            .Where(i => i.BillItemId == null && ResultedStatuses.Contains(i.Status)
                && i.LabOrder!.OrderDate >= fromDate && i.LabOrder!.OrderDate <= toDate)
            .Include(i => i.LabOrder).ThenInclude(o => o!.Patient)
            .Include(i => i.LabTest)
            .OrderBy(i => i.LabOrder!.OrderDate)
            .Select(i => new UnbilledLabOrderRowDto(
                i.Id, i.LabOrder!.OrderNumber, i.LabOrder.OrderDate, i.LabOrder.Patient!.Uhid,
                i.LabOrder.Patient!.FirstName + " " + i.LabOrder.Patient!.LastName, i.LabTest!.Name, i.Status))
            .ToListAsync(ct);

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch before ordering lab tests.");

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

    private static string? FormatRangeDisplay(decimal? low, decimal? high) =>
        (low, high) switch
        {
            (not null, not null) => $"{low}–{high}",
            (not null, null) => $"> {low}",
            (null, not null) => $"< {high}",
            _ => null,
        };

    private static SampleDto ToSampleDto(Sample s) => new(
        s.Id, s.LabOrderId, s.LabOrder!.OrderNumber, s.SampleTypeId, s.SampleType!.Name, s.SampleNumber,
        s.Status, s.CollectedAt, s.ReceivedAt, s.RejectionReason, s.RejectedAt);

    private static LabResultDto ToResultDto(LabResult r) => new(
        r.Id, r.LabOrderItemId, r.EnteredByUserId, null, r.EnteredAt, r.VerifiedByUserId, null, r.VerifiedAt,
        r.ApprovedByUserId, null, r.ApprovedAt, r.Notes, r.HasCriticalValue,
        r.Values.Select(v => new ResultValueDto(
            v.Id, v.LabTestParameterId, v.LabTestParameter!.Name, v.Value, v.Unit, v.NormalLow, v.NormalHigh, v.Flag)).ToList());

    private static LabOrderDetailDto ToDetailDto(LabOrder o) => new(
        o.Id, o.OrderNumber, o.OrderDate, o.PatientId, o.Patient!.Uhid, FormatName(o.Patient!), o.Patient!.Gender.ToString(),
        FormatAge(o.Patient!.DateOfBirth, o.Patient!.EstimatedAge, DateTimeOffset.UtcNow), o.OpVisitId, o.EmergencyVisitId, o.OrderedByDoctorId,
        o.OrderedByDoctor?.User?.FullName, o.Priority, o.Status, o.ClinicalNotes, o.CancelReason, o.CancelledAt,
        o.Items.OrderBy(i => i.Id).Select(i => new LabOrderItemDto(
            i.Id, i.LabTestId, i.LabTest!.Name, i.TestProfileId, i.TestProfile?.Name,
            i.SampleId, i.Sample?.SampleNumber, i.Status)).ToList());
}
