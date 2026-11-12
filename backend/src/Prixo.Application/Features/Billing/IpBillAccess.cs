using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Billing;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Billing;

/// <summary>
/// The one running IP bill of an admission. Clinical modules that create charges (pharmacy IP
/// issues, OT implant/consumable usage, OT time) post straight onto it, creating it on first use,
/// so a charge is never lost and theatre/pharmacy staff never wait on the billing desk.
/// </summary>
public static class IpBillAccess
{
    public static async Task<Bill> GetOrCreateOpenAsync(
        IAppDbContext db, ISequenceService sequences, long admissionId, DateOnly today, CancellationToken ct)
    {
        var bill = await db.Bills.Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.AdmissionId == admissionId && b.Status != BillStatus.Cancelled, ct);
        if (bill is not null)
        {
            if (bill.Status == BillStatus.Parked)
            {
                throw new DomainException($"IP bill {bill.BillNumber} is parked; unpark it before posting charges.");
            }
            return bill;
        }

        var admission = await db.Admissions.FirstOrDefaultAsync(a => a.Id == admissionId, ct)
            ?? throw new NotFoundException(nameof(Admission), admissionId);
        var number = await sequences.NextValueAsync(SequenceKeys.BillNumber, admission.BranchId, today.Year, ct);
        bill = new Bill
        {
            BranchId = admission.BranchId,
            PatientId = admission.PatientId,
            AdmissionId = admission.Id,
            BillNumber = $"BILL-{today.Year}-{number.ToString().PadLeft(6, '0')}",
            BillDate = today,
            BillType = BillType.Ip,
            Status = BillStatus.Open,
        };
        db.Bills.Add(bill);
        await db.SaveChangesAsync(ct);
        return bill;
    }

    /// <summary>Tariff-resolved rate for a service (patient category / room type / effective window), falling back to the service default rate.</summary>
    public static async Task<decimal> ResolveServiceRateAsync(
        IAppDbContext db, long branchId, Service service, long? patientCategoryId, DateOnly asOf, long? roomTypeId, CancellationToken ct)
    {
        var candidates = await db.TariffItems.AsNoTracking()
            .Where(ti => ti.ServiceId == service.Id && ti.Tariff!.BranchId == branchId)
            .Select(ti => new TariffCandidate(ti.Tariff!.PatientCategoryId, ti.Tariff!.EffectiveFrom, ti.Tariff!.EffectiveTo, ti.Rate, ti.RoomTypeId))
            .ToListAsync(ct);
        return BillMath.ResolveTariffRate(candidates, patientCategoryId, asOf, service.DefaultRate, roomTypeId);
    }

    /// <summary>Adds a line to the bill and recomputes totals (status flips back to PartiallyPaid if the bill was settled).</summary>
    public static BillItem AddLine(Bill bill, BillItemSourceType sourceType, string description, decimal quantity, decimal rate,
        decimal taxPercent = 0, long? serviceId = null)
    {
        var item = new BillItem
        {
            BillId = bill.Id,
            SourceType = sourceType,
            ServiceId = serviceId,
            Description = description,
            Quantity = quantity,
            Rate = rate,
            TaxPercent = taxPercent,
        };
        BillMath.ComputeItemAmounts(item);
        bill.Items.Add(item);
        BillMath.RecomputeBillTotals(bill);
        return item;
    }
}
