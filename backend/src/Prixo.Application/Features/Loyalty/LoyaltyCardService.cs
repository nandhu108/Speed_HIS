using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Domain.Entities.Loyalty;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Loyalty;

public class LoyaltyCardService(
    IAppDbContext db,
    ISequenceService sequences,
    ICurrentUser currentUser,
    IActivityLogger activity,
    IPdfService pdf,
    TimeProvider timeProvider)
{
    public async Task<LoyaltyCardDetailDto> RegisterAsync(RegisterLoyaltyCardRequest request, CancellationToken ct = default)
    {
        var branchId = RequireBranch();

        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Id == request.PatientId, ct)
            ?? throw new NotFoundException(nameof(Patient), request.PatientId);
        var cardType = await db.LoyaltyCardTypes.FirstOrDefaultAsync(t => t.Id == request.LoyaltyCardTypeId, ct)
            ?? throw new NotFoundException(nameof(LoyaltyCardType), request.LoyaltyCardTypeId);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var amount = await ResolvePriceAsync(branchId, request.LoyaltyCardTypeId, request.DurationMonths, today, ct);

        var year = timeProvider.GetUtcNow().Year;
        var numberValue = await sequences.NextValueAsync(SequenceKeys.LoyaltyCardNumber, branchId, year, ct);
        var cardNumber = $"LC-{year}-{numberValue.ToString().PadLeft(6, '0')}";

        var card = new LoyaltyCard
        {
            BranchId = branchId,
            CardNumber = cardNumber,
            PatientId = request.PatientId,
            Patient = patient,
            LoyaltyCardTypeId = request.LoyaltyCardTypeId,
            LoyaltyCardType = cardType,
            IssuedDate = today,
            DurationMonths = request.DurationMonths,
            ExpiryDate = today.AddMonths(request.DurationMonths),
            Amount = amount,
        };
        db.LoyaltyCards.Add(card);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(card.Id, ct);
    }

    public async Task<PagedResult<LoyaltyCardListItemDto>> GetPagedAsync(LoyaltyCardQuery query, CancellationToken ct = default)
    {
        var cards = db.LoyaltyCards.AsNoTracking()
            .Include(c => c.Patient).Include(c => c.LoyaltyCardType).AsQueryable();

        if (query.PatientId is { } patientId) cards = cards.Where(c => c.PatientId == patientId);
        if (query.Status is { } status) cards = cards.Where(c => c.Status == status);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            cards = cards.Where(c =>
                EF.Functions.ILike(c.Patient!.Uhid, term) ||
                EF.Functions.ILike(c.Patient!.FirstName, term) ||
                EF.Functions.ILike(c.Patient!.LastName, term) ||
                EF.Functions.ILike(c.CardNumber, term));
        }

        var totalCount = await cards.CountAsync(ct);
        var items = await cards
            .OrderByDescending(c => c.IssuedDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new LoyaltyCardListItemDto(
                c.Id, c.CardNumber, c.PatientId, c.Patient!.Uhid, c.Patient!.FirstName + " " + c.Patient!.LastName,
                c.LoyaltyCardTypeId, c.LoyaltyCardType!.Name, c.IssuedDate, c.ExpiryDate, c.Amount, c.Status))
            .ToListAsync(ct);

        return PagedResult<LoyaltyCardListItemDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<LoyaltyCardDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var card = await db.LoyaltyCards.AsNoTracking()
            .Include(c => c.Patient)
            .Include(c => c.LoyaltyCardType).ThenInclude(t => t!.LoyaltyCardCategory)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(LoyaltyCard), id);

        var members = await db.LoyaltyCardMembers.AsNoTracking()
            .Include(m => m.Patient).Include(m => m.MemberRelationTag)
            .Where(m => m.LoyaltyCardId == id && m.RemovedAt == null)
            .OrderBy(m => m.AddedAt)
            .Select(m => new LoyaltyCardMemberDto(
                m.Id, m.PatientId, m.Patient!.Uhid, m.Patient!.FirstName + " " + m.Patient!.LastName,
                m.MemberRelationTagId, m.MemberRelationTag!.Name, m.AddedAt))
            .ToListAsync(ct);

        return ToDetailDto(card, members);
    }

    public async Task<LoyaltyCardDetailDto> RenewOrUpgradeAsync(long id, RenewLoyaltyCardRequest request, CancellationToken ct = default)
    {
        var card = await db.LoyaltyCards.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(LoyaltyCard), id);
        EnsureActive(card);

        var loyaltyCardTypeId = request.LoyaltyCardTypeId ?? card.LoyaltyCardTypeId;
        if (request.LoyaltyCardTypeId is { } newTypeId && !await db.LoyaltyCardTypes.AnyAsync(t => t.Id == newTypeId, ct))
        {
            throw new NotFoundException(nameof(LoyaltyCardType), newTypeId);
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var amount = await ResolvePriceAsync(card.BranchId, loyaltyCardTypeId, request.DurationMonths, today, ct);

        card.LoyaltyCardTypeId = loyaltyCardTypeId;
        card.DurationMonths = request.DurationMonths;
        card.ExpiryDate = today.AddMonths(request.DurationMonths);
        card.Amount = amount;
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task<long> AddDependentAsync(long id, AddDependentRequest request, CancellationToken ct = default)
    {
        var card = await db.LoyaltyCards.Include(c => c.LoyaltyCardType).FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(LoyaltyCard), id);
        EnsureActive(card);

        if (!await db.Patients.AnyAsync(p => p.Id == request.PatientId, ct))
        {
            throw new NotFoundException(nameof(Patient), request.PatientId);
        }
        if (!await db.MemberRelationTags.AnyAsync(t => t.Id == request.MemberRelationTagId, ct))
        {
            throw new NotFoundException(nameof(MemberRelationTag), request.MemberRelationTagId);
        }

        var activeCount = await db.LoyaltyCardMembers.CountAsync(m => m.LoyaltyCardId == id && m.RemovedAt == null, ct);
        if (activeCount >= card.LoyaltyCardType!.MemberLimit)
        {
            throw new DomainException($"This card's member limit ({card.LoyaltyCardType.MemberLimit}) has been reached.");
        }
        if (await db.LoyaltyCardMembers.AnyAsync(m => m.LoyaltyCardId == id && m.PatientId == request.PatientId && m.RemovedAt == null, ct))
        {
            throw new DomainException("This patient is already a dependent on this card.");
        }

        var member = new LoyaltyCardMember
        {
            LoyaltyCardId = id,
            PatientId = request.PatientId,
            MemberRelationTagId = request.MemberRelationTagId,
            AddedAt = timeProvider.GetUtcNow(),
        };
        db.LoyaltyCardMembers.Add(member);
        await db.SaveChangesAsync(ct);
        return member.Id;
    }

    public async Task RemoveDependentAsync(long id, long memberId, CancellationToken ct = default)
    {
        var member = await db.LoyaltyCardMembers.FirstOrDefaultAsync(m => m.Id == memberId && m.LoyaltyCardId == id, ct)
            ?? throw new NotFoundException(nameof(LoyaltyCardMember), memberId);
        if (member.RemovedAt is not null)
        {
            throw new DomainException("This dependent has already been removed.");
        }
        member.RemovedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    public async Task<LoyaltyCardDetailDto> DuplicateIssueAsync(long id, CancellationToken ct = default)
    {
        var card = await db.LoyaltyCards.Include(c => c.Patient).FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(LoyaltyCard), id);
        EnsureActive(card);

        card.DuplicateIssueCount += 1;
        await activity.LogAsync(ActivityEvents.LoyaltyCardDuplicateIssued,
            $"Duplicate card issued for loyalty card {card.CardNumber} ('{card.Patient!.FirstName} {card.Patient!.LastName}')", ct: ct);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task DeactivateAsync(long id, DeactivateLoyaltyCardRequest request, CancellationToken ct = default)
    {
        var card = await db.LoyaltyCards.Include(c => c.Patient).FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(LoyaltyCard), id);
        EnsureActive(card);

        card.Status = LoyaltyCardStatus.Deactivated;
        card.DeactivateReason = request.Reason;
        card.DeactivatedAt = timeProvider.GetUtcNow();

        await activity.LogAsync(ActivityEvents.LoyaltyCardDeactivated,
            $"Loyalty card {card.CardNumber} for '{card.Patient!.FirstName} {card.Patient!.LastName}' deactivated: {request.Reason}", ct: ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<byte[]> GenerateCardPdfAsync(long id, CancellationToken ct = default)
    {
        var card = await db.LoyaltyCards.AsNoTracking()
            .Include(c => c.Patient)
            .Include(c => c.LoyaltyCardType).ThenInclude(t => t!.LoyaltyCardCategory)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(LoyaltyCard), id);
        var branch = await db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == card.BranchId, ct);

        var data = new LoyaltyCardData(
            card.CardNumber, card.Patient!.FirstName + " " + card.Patient!.LastName, card.Patient!.Uhid,
            card.LoyaltyCardType!.Name, card.LoyaltyCardType!.LoyaltyCardCategory?.Name,
            card.IssuedDate, card.ExpiryDate, branch?.Name ?? string.Empty, null);
        return pdf.GenerateLoyaltyCard(data);
    }

    /// <summary>Resolves the newest LoyaltyCardPrice header for (branch, type) whose effective
    /// window covers asOfDate, then the child item matching durationMonths — mirrors Tariff's
    /// branch+category+date resolution.</summary>
    private async Task<decimal> ResolvePriceAsync(
        long branchId, long loyaltyCardTypeId, int durationMonths, DateOnly asOfDate, CancellationToken ct)
    {
        var header = await db.LoyaltyCardPrices.AsNoTracking()
            .Where(p => p.BranchId == branchId && p.LoyaltyCardTypeId == loyaltyCardTypeId &&
                p.EffectiveFrom <= asOfDate && (p.EffectiveTo == null || p.EffectiveTo >= asOfDate))
            .OrderByDescending(p => p.EffectiveFrom)
            .Include(p => p.Items)
            .FirstOrDefaultAsync(ct)
            ?? throw new DomainException("No price is configured for this card type on this branch.");

        var item = header.Items.FirstOrDefault(i => i.DurationMonths == durationMonths)
            ?? throw new DomainException($"No price is configured for a {durationMonths}-month duration on this card type.");
        return item.Amount;
    }

    private static void EnsureActive(LoyaltyCard card)
    {
        if (card.Status != LoyaltyCardStatus.Active)
        {
            throw new DomainException($"A card that is {card.Status} cannot be modified.");
        }
    }

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch before registering a loyalty card.");

    private static LoyaltyCardDetailDto ToDetailDto(LoyaltyCard c, IReadOnlyList<LoyaltyCardMemberDto> members) => new(
        c.Id, c.CardNumber, c.PatientId, c.Patient!.Uhid, c.Patient!.FirstName + " " + c.Patient!.LastName, c.Patient!.Mobile,
        c.LoyaltyCardTypeId, c.LoyaltyCardType!.Name, c.LoyaltyCardType!.MemberLimit, c.LoyaltyCardType!.LoyaltyCardCategory?.Name,
        c.IssuedDate, c.DurationMonths, c.ExpiryDate, c.Amount, c.Status,
        c.DeactivateReason, c.DeactivatedAt, c.DuplicateIssueCount, members);
}
