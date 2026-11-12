using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Billing;

/// <summary>Blocks bill/payment creation for a (branch, date) once that day's DayClose is Closed.</summary>
internal static class DayCloseGuard
{
    public static async Task EnsureOpenAsync(IAppDbContext db, long branchId, DateOnly date, CancellationToken ct)
    {
        var isClosed = await db.DayCloses.AsNoTracking()
            .AnyAsync(d => d.BranchId == branchId && d.Date == date && d.Status == DayCloseStatus.Closed, ct);
        if (isClosed)
        {
            throw new DomainException($"The books for {date:yyyy-MM-dd} are closed for this branch.");
        }
    }
}
