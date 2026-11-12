using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Billing;
using Prixo.Domain.Entities.Pharmacy;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Pharmacy;

/// <summary>
/// Non-sale stock issue. Department/Emergency are real, usable flows — issue creation is
/// a mandatory transaction (CLAUDE.md's "pharmacy sale/issue"). Ip is schema-ready only
/// (see IssueType) — nothing blocks creating one, but there's no admission to generate it
/// from until Phase 9.
/// </summary>
public class IssueService(
    IAppDbContext db, IUnitOfWork unitOfWork, ISequenceService sequences, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<long> CreateIssueAsync(CreateIssueRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var branchId = RequireBranch();
            if (!await db.Stores.AnyAsync(s => s.Id == request.StoreId, token))
            {
                throw new NotFoundException(nameof(Domain.Entities.Pharmacy.Store), request.StoreId);
            }
            if (request.Items.Count == 0)
            {
                throw new DomainException("Add at least one drug to the issue.");
            }
            if (request.AdmissionId is { } admissionId && !await db.Admissions.AnyAsync(a => a.Id == admissionId, token))
            {
                throw new NotFoundException(nameof(Domain.Entities.Ipd.Admission), admissionId);
            }

            Indent? indent = null;
            if (request.IndentId is { } indentId)
            {
                indent = await db.Indents.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == indentId, token)
                    ?? throw new NotFoundException(nameof(Indent), indentId);
                if (indent.Status != IndentStatus.Approved)
                {
                    throw new DomainException($"An indent that is {indent.Status} cannot be issued against.");
                }
            }

            var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
            var number = await sequences.NextValueAsync(SequenceKeys.IssueNumber, branchId, today.Year, token);
            var issue = new Issue
            {
                BranchId = branchId,
                StoreId = request.StoreId,
                IssueType = request.IssueType,
                DepartmentId = request.DepartmentId,
                PatientId = request.PatientId,
                AdmissionId = request.AdmissionId,
                IndentId = request.IndentId,
                IssueNumber = $"ISS-{today.Year}-{number.ToString().PadLeft(6, '0')}",
                IssueDate = today,
                Notes = request.Notes,
            };
            db.Issues.Add(issue);
            await db.SaveChangesAsync(token);

            var now = timeProvider.GetUtcNow();
            var issuedBatches = new List<Batch>();
            foreach (var line in request.Items)
            {
                await unitOfWork.LockAsync<Batch>(line.BatchId, token);
                var batch = await db.Batches.FirstOrDefaultAsync(b => b.Id == line.BatchId, token)
                    ?? throw new NotFoundException(nameof(Batch), line.BatchId);
                if (!StockMath.IsUsable(batch.ExpiryDate, today))
                {
                    throw new DomainException($"Batch {batch.BatchNumber} expired on {batch.ExpiryDate:dd-MMM-yyyy} and cannot be issued.");
                }
                if (batch.StoreId != request.StoreId)
                {
                    throw new DomainException($"Batch {batch.BatchNumber} is not in the selected store.");
                }
                if (batch.QuantityOnHand < line.Quantity)
                {
                    throw new DomainException($"Batch {batch.BatchNumber} only has {batch.QuantityOnHand} on hand.");
                }
                issue.Items.Add(new IssueItem { BatchId = batch.Id, Batch = batch, Quantity = line.Quantity });
                StockLedgerWriter.Write(db, batch, StockMovementType.IssueOut, line.Quantity, nameof(Issue), issue.Id, now);
                issuedBatches.Add(batch);
            }

            // Medicines issued to an inpatient are charged to the admission's running IP bill at MRP
            // (MRP is tax-inclusive, so no GST is added on top).
            if (request.AdmissionId is { } ipAdmissionId && request.IssueType == IssueType.Ip)
            {
                await db.SaveChangesAsync(token);
                var ipBill = await IpBillAccess.GetOrCreateOpenAsync(db, sequences, ipAdmissionId, today, token);
                var lines = new List<(IssueItem Item, Prixo.Domain.Entities.Billing.BillItem Line)>();
                foreach (var issueItem in issue.Items)
                {
                    var b = issueItem.Batch!;
                    var rate = b.Mrp ?? b.PurchaseRate;
                    var line = IpBillAccess.AddLine(ipBill, BillItemSourceType.Pharmacy,
                        $"Pharmacy — {ProductNaming.Of(b)} (batch {b.BatchNumber}, {issue.IssueNumber})", issueItem.Quantity, rate);
                    lines.Add((issueItem, line));
                }
                await db.SaveChangesAsync(token);
                foreach (var (issueItem, line) in lines)
                {
                    issueItem.BillItemId = line.Id;
                }
            }

            if (indent is not null)
            {
                foreach (var indentItem in indent.Items)
                {
                    var approved = indentItem.ApprovedQuantity ?? indentItem.RequestedQuantity;
                    var issuedForItem = request.Items.Zip(issuedBatches)
                        .Where(x => x.Second.DrugId == indentItem.DrugId && x.Second.ItemId == indentItem.ItemId)
                        .Sum(x => x.First.Quantity);
                    if (issuedForItem > approved)
                    {
                        throw new DomainException($"Issuing {issuedForItem} exceeds the indent's approved quantity of {approved}.");
                    }
                }
                indent.Status = IndentStatus.Issued;
            }

            await db.SaveChangesAsync(token);
            return issue.Id;
        }, ct);

    public async Task<IReadOnlyList<IssueListItemDto>> GetPagedAsync(IssueType? issueType, CancellationToken ct = default)
    {
        var issues = db.Issues.AsNoTracking();
        if (issueType is { } type) issues = issues.Where(i => i.IssueType == type);
        return await issues.Include(i => i.Department).Include(i => i.Admission)
            .OrderByDescending(i => i.IssueDate).ThenByDescending(i => i.Id)
            .Select(i => new IssueListItemDto(i.Id, i.IssueNumber, i.IssueDate, i.IssueType,
                i.Department != null ? i.Department.Name : null, i.AdmissionId,
                i.Admission != null ? i.Admission.AdmissionNumber : null, i.IndentId, i.Items.Count))
            .ToListAsync(ct);
    }

    public async Task<IssueDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var i = await db.Issues.AsNoTracking()
            .Include(x => x.Department).Include(x => x.Patient).Include(x => x.Admission)
            .Include(x => x.Items).ThenInclude(x => x.Batch).ThenInclude(b => b!.Drug)
            .Include(x => x.Items).ThenInclude(x => x.Batch).ThenInclude(b => b!.Item)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException(nameof(Issue), id);
        return new IssueDetailDto(i.Id, i.IssueNumber, i.IssueDate, i.IssueType, i.DepartmentId, i.Department?.Name,
            i.PatientId, i.Patient != null ? i.Patient.FirstName + " " + i.Patient.LastName : null,
            i.AdmissionId, i.Admission?.AdmissionNumber, i.IndentId, i.Notes,
            i.Items.Select(x => new IssueItemDto(x.Id, x.BatchId, ProductNaming.Of(x.Batch!), x.Batch!.BatchNumber, x.Quantity)).ToList());
    }

    public async Task<long> CreateIssueReturnAsync(CreateIssueReturnRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var branchId = RequireBranch();
            var issue = await db.Issues.Include(i => i.Items).ThenInclude(x => x.Batch)
                .FirstOrDefaultAsync(i => i.Id == request.IssueId, token) ?? throw new NotFoundException(nameof(Issue), request.IssueId);

            var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
            var number = await sequences.NextValueAsync(SequenceKeys.IssueReturnNumber, branchId, today.Year, token);
            var issueReturn = new IssueReturn
            {
                BranchId = branchId,
                IssueId = issue.Id,
                ReturnNumber = $"ISSRET-{today.Year}-{number.ToString().PadLeft(6, '0')}",
                ReturnDate = today,
                Reason = request.Reason,
            };
            db.IssueReturns.Add(issueReturn);
            await db.SaveChangesAsync(token);

            var now = timeProvider.GetUtcNow();
            foreach (var line in request.Items)
            {
                var issueItem = issue.Items.FirstOrDefault(x => x.Id == line.IssueItemId)
                    ?? throw new NotFoundException(nameof(IssueItem), line.IssueItemId);
                var alreadyReturned = await db.IssueReturnItems.Where(r => r.IssueItemId == issueItem.Id)
                    .SumAsync(r => (decimal?)r.Quantity, token) ?? 0;
                if (line.Quantity <= 0 || alreadyReturned + line.Quantity > issueItem.Quantity)
                {
                    throw new DomainException($"Return quantity exceeds what is still returnable ({issueItem.Quantity - alreadyReturned} of {issueItem.Quantity} issued).");
                }
                issueReturn.Items.Add(new IssueReturnItem { IssueItemId = issueItem.Id, Quantity = line.Quantity });
                if (issueItem.BillItemId is { } billItemId)
                {
                    // Reverse the IP charge for the returned quantity.
                    var billLine = await db.BillItems.Include(x => x.Bill).ThenInclude(x => x!.Items).FirstOrDefaultAsync(x => x.Id == billItemId, token);
                    if (billLine?.Bill is { Status: not BillStatus.Cancelled } chargedBill)
                    {
                        billLine.Quantity -= line.Quantity;
                        BillMath.ComputeItemAmounts(billLine);
                        BillMath.RecomputeBillTotals(chargedBill);
                    }
                }
                StockLedgerWriter.Write(db, issueItem.Batch!, StockMovementType.IssueReturnIn, line.Quantity, nameof(IssueReturn), issueReturn.Id, now);
            }

            await db.SaveChangesAsync(token);
            return issueReturn.Id;
        }, ct);

    public async Task<IReadOnlyList<IssueReturnListItemDto>> GetIssueReturnsAsync(CancellationToken ct = default) =>
        await db.IssueReturns.AsNoTracking()
            .OrderByDescending(r => r.ReturnDate).ThenByDescending(r => r.Id)
            .Select(r => new IssueReturnListItemDto(r.Id, r.ReturnNumber, r.ReturnDate, r.Issue!.IssueNumber))
            .ToListAsync(ct);

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");
}
