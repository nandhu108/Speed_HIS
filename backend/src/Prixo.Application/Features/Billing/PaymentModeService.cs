using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Billing;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Billing;

/// <summary>Cash/Card/UPI/Cheque… global catalog. Not the generic simple-master framework — RequiresReference needs its own field.</summary>
public class PaymentModeService(IAppDbContext db)
{
    public async Task<IReadOnlyList<PaymentModeDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.PaymentModes.AsNoTracking()
            .OrderBy(m => m.Name)
            .Select(m => new PaymentModeDto(m.Id, m.Code, m.Name, m.Description, m.RequiresReference, m.IsActive))
            .ToListAsync(ct);

    public async Task<long> CreateAsync(SavePaymentModeRequest request, CancellationToken ct = default)
    {
        await EnsureUniqueCodeAsync(request.Code, null, ct);
        var mode = new PaymentMode
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            RequiresReference = request.RequiresReference,
            IsActive = request.IsActive,
        };
        db.PaymentModes.Add(mode);
        await db.SaveChangesAsync(ct);
        return mode.Id;
    }

    public async Task UpdateAsync(long id, SavePaymentModeRequest request, CancellationToken ct = default)
    {
        var mode = await db.PaymentModes.FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new NotFoundException(nameof(PaymentMode), id);
        await EnsureUniqueCodeAsync(request.Code, id, ct);

        mode.Code = request.Code;
        mode.Name = request.Name;
        mode.Description = request.Description;
        mode.RequiresReference = request.RequiresReference;
        mode.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var mode = await db.PaymentModes.FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new NotFoundException(nameof(PaymentMode), id);
        if (await db.BillPayments.AnyAsync(p => p.PaymentModeId == id, ct)
            || await db.Advances.AnyAsync(a => a.PaymentModeId == id, ct))
        {
            throw new DomainException("This payment mode has already been used in a transaction and cannot be deleted.");
        }

        db.PaymentModes.Remove(mode); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureUniqueCodeAsync(string code, long? selfId, CancellationToken ct)
    {
        if (await db.PaymentModes.AnyAsync(m => m.Code == code && m.Id != selfId, ct))
        {
            throw new DomainException($"Payment mode code '{code}' already exists.");
        }
    }
}
