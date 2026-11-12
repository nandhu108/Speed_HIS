using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Ipd;

/// <summary>
/// The unified doctor order sheet (plan Â§9). Placing an order writes nothing to Pharmacy or
/// Lab â€” same loose-coupling posture as OPD's ClinicalOrder â€” so it's a plain single save,
/// not a mandated transaction.
/// </summary>
public class DoctorOrderService(IAppDbContext db, TimeProvider timeProvider, ICurrentUser currentUser)
{
    public async Task<long> CreateAsync(long admissionId, CreateDoctorOrderRequest request, CancellationToken ct = default)
    {
        var admission = await db.Admissions.FirstOrDefaultAsync(a => a.Id == admissionId, ct)
            ?? throw new NotFoundException(nameof(Admission), admissionId);
        if (admission.Status != AdmissionStatus.Admitted)
        {
            throw new DomainException("Orders can only be placed against an active admission.");
        }
        await Prixo.Application.Common.Security.ClinicalActor.ResolveAttributedDoctorAsync(db, currentUser, request.OrderedByDoctorId, ct);
        if (!await db.Doctors.AnyAsync(d => d.Id == request.OrderedByDoctorId, ct))
        {
            throw new NotFoundException(nameof(Doctor), request.OrderedByDoctorId);
        }
        if (request.OrderType == DoctorOrderType.Medication && request.Medication is null)
        {
            throw new DomainException("Medication details are required for a medication order.");
        }
        if (request.OrderType == DoctorOrderType.Diet && request.Diet is null)
        {
            throw new DomainException("Diet details are required for a diet order.");
        }
        if (request.OrderType == DoctorOrderType.Medication && !await db.Drugs.AnyAsync(d => d.Id == request.Medication!.DrugId, ct))
        {
            throw new NotFoundException(nameof(Drug), request.Medication!.DrugId);
        }

        var order = new DoctorOrder
        {
            AdmissionId = admissionId,
            OrderType = request.OrderType,
            OrderedByDoctorId = request.OrderedByDoctorId,
            OrderedAt = timeProvider.GetUtcNow(),
            Priority = request.Priority,
            Status = ClinicalOrderStatus.Ordered,
            Description = request.Description,
            Notes = request.Notes,
        };
        if (request.Medication is { } med)
        {
            order.MedicationOrder = new MedicationOrder
            {
                DrugId = med.DrugId, Dose = med.Dose, Route = med.Route, Frequency = med.Frequency,
                StartDate = med.StartDate, EndDate = med.EndDate, Instructions = med.Instructions,
            };
        }
        if (request.Diet is { } diet)
        {
            order.DietOrder = new DietOrder { DietType = diet.DietType, Instructions = diet.Instructions };
        }

        db.DoctorOrders.Add(order);
        await db.SaveChangesAsync(ct);
        return order.Id;
    }

    public async Task<IReadOnlyList<DoctorOrderDto>> GetByAdmissionAsync(
        long admissionId, DoctorOrderType? orderType, CancellationToken ct = default)
    {
        var orders = db.DoctorOrders.AsNoTracking().Where(o => o.AdmissionId == admissionId);
        if (orderType is { } type) orders = orders.Where(o => o.OrderType == type);
        return await orders
            .Include(o => o.OrderedByDoctor).ThenInclude(d => d!.User)
            .Include(o => o.MedicationOrder).ThenInclude(m => m!.Drug)
            .Include(o => o.DietOrder)
            .OrderByDescending(o => o.OrderedAt)
            .Select(o => ToDto(o))
            .ToListAsync(ct);
    }

    public async Task CancelAsync(long admissionId, long orderId, CancelDoctorOrderRequest request, CancellationToken ct = default)
    {
        var order = await db.DoctorOrders.FirstOrDefaultAsync(o => o.Id == orderId && o.AdmissionId == admissionId, ct)
            ?? throw new NotFoundException(nameof(DoctorOrder), orderId);
        if (order.Status is ClinicalOrderStatus.Completed or ClinicalOrderStatus.Cancelled)
        {
            throw new DomainException($"Order is already {order.Status} and cannot be cancelled.");
        }
        order.Status = ClinicalOrderStatus.Cancelled;
        order.CancelledAt = timeProvider.GetUtcNow();
        order.CancelReason = request.Reason;
        await db.SaveChangesAsync(ct);
    }

    private static DoctorOrderDto ToDto(DoctorOrder o) => new(
        o.Id, o.AdmissionId, o.OrderType, o.OrderedByDoctorId, o.OrderedByDoctor!.User!.FullName, o.OrderedAt,
        o.Priority, o.Status, o.Description, o.Notes,
        o.MedicationOrder is { } m ? new MedicationOrderDto(m.DrugId, m.Drug!.Name, m.Dose, m.Route, m.Frequency, m.StartDate, m.EndDate, m.Instructions) : null,
        o.DietOrder is { } d ? new DietOrderDto(d.DietType, d.Instructions) : null);
}
