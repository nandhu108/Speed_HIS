using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Biomed;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Biomed;

/// <summary>AMC/CMC contracts + renewal alerts (plan §19). IsRenewalDue is computed on read via BiomedMath, not stored.</summary>
public class AmcContractService(IAppDbContext db, TimeProvider timeProvider)
{
    public async Task<long> CreateAsync(SaveAmcContractRequest request, CancellationToken ct = default)
    {
        if (!await db.BiomedEquipment.AnyAsync(e => e.Id == request.EquipmentId, ct))
        {
            throw new NotFoundException(nameof(Domain.Entities.Biomed.BiomedEquipment), request.EquipmentId);
        }
        var equipment = await db.BiomedEquipment.FirstAsync(e => e.Id == request.EquipmentId, ct);
        if (request.EndDate < request.StartDate)
        {
            throw new DomainException("End date must be on or after the start date.");
        }

        var contract = new AmcContract
        {
            BranchId = equipment.BranchId, EquipmentId = request.EquipmentId, VendorId = request.VendorId, ContractType = request.ContractType,
            ContractNumber = request.ContractNumber, StartDate = request.StartDate, EndDate = request.EndDate,
            Cost = request.Cost, RenewalReminderDays = request.RenewalReminderDays, Notes = request.Notes,
        };
        db.AmcContracts.Add(contract);
        await db.SaveChangesAsync(ct);
        return contract.Id;
    }

    public async Task<IReadOnlyList<AmcContractDto>> GetAllAsync(long? equipmentId, bool renewalDueOnly, CancellationToken ct = default)
    {
        var contracts = db.AmcContracts.AsNoTracking().Include(c => c.Equipment).Include(c => c.Vendor).AsQueryable();
        if (equipmentId is { } id) contracts = contracts.Where(c => c.EquipmentId == id);

        var rows = await contracts.OrderByDescending(c => c.EndDate).ToListAsync(ct);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var dtos = rows.Select(c => new AmcContractDto(
            c.Id, c.EquipmentId, c.Equipment!.Name, c.VendorId, c.Vendor!.Name, c.ContractType, c.ContractNumber,
            c.StartDate, c.EndDate, c.Cost, c.RenewalReminderDays, BiomedMath.IsRenewalDue(c.EndDate, c.RenewalReminderDays, today), c.Notes));
        if (renewalDueOnly) dtos = dtos.Where(d => d.IsRenewalDue);
        return dtos.ToList();
    }
}
