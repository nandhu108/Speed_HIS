using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Billing;
using Prixo.Domain.Entities.Finance;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Opd;
using Prixo.Domain.Enums;

namespace Prixo.Infrastructure.Persistence;

/// <summary>
/// Six months of day-by-day operational history, because *when* the data sits matters as much as
/// how much of it there is.
///
/// <para>The dev database's entire operational history had collapsed onto a single day: 1,676 of
/// its 1,686 bills and 1,245 of its 1,269 OP visits were dated 2026-07-23. Every dashboard tile
/// scoped to "today", "this month" or "last 7 days" therefore rendered zero, every trend line was
/// one vertical spike, and month-over-month comparison was impossible — not because the reports
/// were wrong but because there was nothing for them to plot.</para>
///
/// <para>This section generates visits, bills, payments and the matching Finance postings across
/// the whole window ending today, weekday-weighted with a gentle upward growth trend. It is
/// purely additive: the pre-existing spike is left exactly as it is (re-dating it would mean
/// re-dating the ledger entries and vouchers already posted against those bills in lockstep,
/// turning cosmetic sparseness into a real Finance inconsistency), and the per-day guard below
/// skips any branch-day that already has bills.</para>
///
/// <para><b>Every generated bill line carries a real ServiceId.</b> That is not incidental:
/// <c>reporting.mv_revenue_daily</c> derives department-wise revenue through
/// <c>bill_items → services.department_id</c>, and not one of the 1,251 pre-existing service
/// lines had a ServiceId set, so department revenue was a single unlabelled bucket no matter what
/// date range you asked for. The other half of that fix — services that had no DepartmentId — is
/// in <see cref="BackfillServiceDepartmentsAsync"/>.</para>
///
/// <para>Payments post through the same double-entry shape as the live code path
/// (VoucherService.PostBillPaymentAsync: debit the payment mode's linked bank/cash account,
/// credit Patient Revenue, one LedgerEntry per VoucherLine, tagged
/// <c>SourceReference = "BILL_PAYMENT:{id}"</c>), so cash/bank collection reports and the ledger
/// viewer reconcile against the bills rather than disagreeing with them.</para>
/// </summary>
public partial class DemoDataSeeder
{
    /// <summary>How far back the generated history reaches. Ends today so "today" tiles are live.</summary>
    private const int HistoryDays = 182;

    private async Task SeedOperationalHistoryAsync(DemoContext ctx, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = today.AddDays(-HistoryDays);

        var branches = await db.Branches.IgnoreQueryFilters()
            .Where(b => !b.IsDeleted).OrderBy(b => b.Id).Select(b => b.Id).ToListAsync(ct);
        var patients = await db.Patients.IgnoreQueryFilters()
            .Where(p => !p.IsDeleted)
            .Select(p => new PatientRef(p.Id, p.Gender, p.DateOfBirth))
            .ToListAsync(ct);
        var doctors = await db.Doctors
            .Select(d => new DoctorRef(d.Id, d.ConsultationFee, d.FollowUpFee,
                d.DoctorDepartments.Select(dd => dd.DepartmentId).ToList()))
            .ToListAsync(ct);
        if (patients.Count == 0 || doctors.Count == 0)
        {
            logger.LogWarning("Demo history skipped — no patients or doctors exist to attach visits to.");
            return;
        }

        var catalog = await LoadBillingCatalogAsync(ct);
        if (catalog is null)
        {
            logger.LogWarning("Demo history skipped — the billing catalog (services / payment modes) is not seeded.");
            return;
        }

        // Branch-days that already carry bills are left completely alone — that is what keeps the
        // pre-existing 2026-07-23 spike (and any hand-entered testing) intact, and what makes a
        // re-run resume rather than duplicate.
        var occupied = await db.Bills.IgnoreQueryFilters()
            .Where(b => !b.IsDeleted && b.BillDate >= startDate)
            .Select(b => new { b.BranchId, b.BillDate })
            .Distinct()
            .ToListAsync(ct);
        var occupiedDays = occupied.Select(o => (o.BranchId, o.BillDate)).ToHashSet();

        var totals = new HistoryTotals();
        foreach (var branchId in branches)
        {
            for (var offset = 0; offset <= HistoryDays; offset++)
            {
                var date = startDate.AddDays(offset);
                if (occupiedDays.Contains((branchId, date)))
                {
                    continue;
                }
                await SeedBranchDayAsync(ctx, branchId, date, offset, patients, doctors, catalog, totals, ct);
            }

            await SeedBranchAdmissionsAsync(ctx, branchId, startDate, today, patients, doctors, catalog, totals, ct);
        }

        if (totals.Visits == 0 && totals.Admissions == 0)
        {
            logger.LogInformation("Demo history: nothing to add — the window already has data on every day");
            return;
        }

        logger.LogInformation(
            "Demo history: {Visits} OP visits, {Bills} bills, {Payments} payments, {Vouchers} vouchers, {Admissions} admissions over {Days} days",
            totals.Visits, totals.Bills, totals.Payments, totals.Vouchers, totals.Admissions, HistoryDays);

        await RefreshReportingViewsAsync(ct);
    }

    private sealed record PatientRef(long Id, Gender Gender, DateOnly? DateOfBirth);

    private sealed record DoctorRef(long Id, decimal ConsultationFee, decimal FollowUpFee, List<long> DepartmentIds);

    private sealed class HistoryTotals
    {
        public int Visits { get; set; }
        public int Bills { get; set; }
        public int Payments { get; set; }
        public int Vouchers { get; set; }
        public int Admissions { get; set; }
    }

    /// <summary>
    /// The Service/tax/payment-mode/account rows the generated bills need, resolved once instead
    /// of per bill. Services are split by category so a bill can be composed the way a real one
    /// is — one consultation line plus a plausible mix of investigations and procedures.
    /// </summary>
    private sealed record BillingCatalog(
        List<ServiceRef> Consultations,
        List<ServiceRef> Investigations,
        List<ServiceRef> Procedures,
        List<ServiceRef> RoomCharges,
        List<ServiceRef> NursingCharges,
        List<PaymentModeRef> PaymentModes,
        long RevenueAccountId);

    private sealed record ServiceRef(long Id, string Name, decimal DefaultRate, long? DepartmentId, decimal TaxPercent);

    private sealed record PaymentModeRef(long Id, string Code, long? LinkedAccountId);

    private async Task<BillingCatalog?> LoadBillingCatalogAsync(CancellationToken ct)
    {
        var revenueAccountId = await db.Accounts
            .Where(a => a.Code == "4000").Select(a => (long?)a.Id).FirstOrDefaultAsync(ct);
        var modes = await db.PaymentModes
            .Select(m => new PaymentModeRef(m.Id, m.Code, m.LinkedAccountId)).ToListAsync(ct);
        if (revenueAccountId is null || modes.Count == 0)
        {
            return null;
        }

        var services = await db.Services
            .Include(s => s.Category)
            .Include(s => s.Tax)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.DefaultRate,
                s.DepartmentId,
                CategoryCode = s.Category!.Code,
                TaxPercent = s.IsTaxable && s.Tax != null ? s.Tax.RatePercent : 0m,
            })
            .ToListAsync(ct);
        if (services.Count == 0)
        {
            return null;
        }

        List<ServiceRef> Pick(params string[] categoryCodes) => services
            .Where(s => categoryCodes.Contains(s.CategoryCode))
            .Select(s => new ServiceRef(s.Id, s.Name, s.DefaultRate, s.DepartmentId, s.TaxPercent))
            .ToList();

        var consultations = Pick("SVC1");
        var investigations = Pick("SVC5");
        var procedures = Pick("SVC2");
        if (consultations.Count == 0 || investigations.Count == 0)
        {
            return null;
        }

        return new BillingCatalog(
            consultations, investigations,
            procedures.Count > 0 ? procedures : investigations,
            Pick("SVC3", "SVC7"), Pick("SVC4"),
            modes, revenueAccountId.Value);
    }

    // ── One branch-day of OP activity ─────────────────────────────────────────
    private async Task SeedBranchDayAsync(
        DemoContext ctx, long branchId, DateOnly date, int dayOffset,
        List<PatientRef> patients, List<DoctorRef> doctors, BillingCatalog catalog,
        HistoryTotals totals, CancellationToken ct)
    {
        var visitCount = DailyVisitCount(date, dayOffset);
        if (visitCount == 0)
        {
            return;
        }

        var year = date.Year;
        // Token numbers restart daily per doctor in the real engine; a running counter per
        // doctor for this day reproduces that without touching the token sequence.
        var tokensByDoctor = new Dictionary<long, int>();

        for (var i = 0; i < visitCount; i++)
        {
            var patient = patients[_random.Next(patients.Count)];
            var doctor = doctors[_random.Next(doctors.Count)];
            // Roughly a third of OP footfall is follow-up in a settled practice.
            var visitType = _random.Next(100) < 34 ? VisitType.Review : VisitType.New;
            var fee = visitType == VisitType.Review && doctor.FollowUpFee > 0 ? doctor.FollowUpFee : doctor.ConsultationFee;

            tokensByDoctor.TryGetValue(doctor.Id, out var token);
            tokensByDoctor[doctor.Id] = ++token;

            // Clinic hours 09:00-17:00, spread so the OP hourly-load view isn't flat.
            var registeredAt = date.ToDateTime(new TimeOnly(9, 0)).AddMinutes(_random.Next(0, 8 * 60));
            var visitNumber = await sequences.NextValueAsync(SequenceKeys.OpVisitNumber, branchId, year, ct);

            // A small slice of visits never complete — cancellations and no-shows are what the
            // OP cancellation and conversion reports are built to measure.
            var roll = _random.Next(100);
            var status = roll switch
            {
                < 4 => OpVisitStatus.Cancelled,
                < 7 => OpVisitStatus.Waiting,
                _ => OpVisitStatus.Completed,
            };

            var visit = new OpVisit
            {
                BranchId = branchId,
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                DepartmentId = doctor.DepartmentIds.Count > 0 ? doctor.DepartmentIds[0] : null,
                Category = VisitCategory.Opd,
                VisitNumber = $"OPD-{year}-{visitNumber.ToString().PadLeft(6, '0')}",
                VisitDate = date,
                TokenNumber = token,
                VisitType = visitType,
                Status = status,
                ConsultationFee = fee,
                BookingType = _random.Next(100) < 55 ? AppointmentBookingType.Booked : AppointmentBookingType.WalkIn,
                InConsultAt = status is OpVisitStatus.Completed
                    ? new DateTimeOffset(registeredAt.AddMinutes(_random.Next(10, 70)), TimeSpan.Zero)
                    : null,
                CompletedAt = status is OpVisitStatus.Completed
                    ? new DateTimeOffset(registeredAt.AddMinutes(_random.Next(75, 140)), TimeSpan.Zero)
                    : null,
                CancelReason = status is OpVisitStatus.Cancelled ? "Patient did not report for consultation." : null,
                CancelledAt = status is OpVisitStatus.Cancelled
                    ? new DateTimeOffset(registeredAt.AddMinutes(90), TimeSpan.Zero)
                    : null,
            };

            if (status == OpVisitStatus.Cancelled)
            {
                // No bill for a visit that never happened.
                db.OpVisits.Add(visit);
                await db.SaveChangesAsync(ct);
            }
            else
            {
                await SeedOpBillAsync(ctx, visit, patient, doctor, catalog, totals, ct);
            }
            totals.Visits++;

            // The change tracker would otherwise accumulate every entity written across the whole
            // six-month window, making each successive SaveChanges slower than the last. Nothing
            // above this point is read again, so dropping the tracked graph per visit keeps the
            // run at a constant cost per row.
            db.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// Weekday-weighted daily OP volume with a gentle growth trend across the window, so the
    /// dashboards show a shape (busy weekdays, quiet Sundays, slowly rising month on month)
    /// rather than uniform noise.
    /// </summary>
    private int DailyVisitCount(DateOnly date, int dayOffset)
    {
        var baseline = date.DayOfWeek switch
        {
            DayOfWeek.Sunday => _random.Next(3, 8),
            DayOfWeek.Saturday => _random.Next(8, 15),
            _ => _random.Next(14, 23),
        };
        // ~25% higher by the end of the window than at the start.
        var growth = 1m + (0.25m * dayOffset / HistoryDays);
        return (int)Math.Round(baseline * growth);
    }

    // ── OP bill + payment + Finance posting ───────────────────────────────────
    private async Task SeedOpBillAsync(
        DemoContext ctx, OpVisit visit, PatientRef patient, DoctorRef doctor,
        BillingCatalog catalog, HistoryTotals totals, CancellationToken ct)
    {
        var year = visit.VisitDate.Year;
        var billNumber = await sequences.NextValueAsync(SequenceKeys.BillNumber, visit.BranchId, year, ct);
        var bill = new Bill
        {
            BranchId = visit.BranchId,
            PatientId = patient.Id,
            // Assigning the navigation rather than the FK lets EF insert the visit and its bill in
            // one SaveChanges and fix up OpVisitId itself — one round trip per visit instead of two.
            OpVisit = visit,
            BillNumber = $"BILL-{year}-{billNumber.ToString().PadLeft(6, '0')}",
            BillDate = visit.VisitDate,
            BillType = BillType.Op,
        };

        // Line 1 is always the consultation, priced from the doctor's own fee but pointed at a
        // consultation Service so it lands in that department's revenue.
        var consultationService = PickConsultationService(catalog, doctor);
        AddBillItem(bill, BillItemSourceType.Consultation, consultationService, visit.ConsultationFee, 1m);

        // Then an investigation/procedure mix — most OP visits generate at least one order.
        var investigationCount = _random.Next(100) switch
        {
            < 30 => 0,
            < 65 => 1,
            < 88 => 2,
            _ => 3,
        };
        for (var i = 0; i < investigationCount; i++)
        {
            var service = catalog.Investigations[_random.Next(catalog.Investigations.Count)];
            AddBillItem(bill, BillItemSourceType.Service, service, service.DefaultRate, 1m);
        }
        if (_random.Next(100) < 12)
        {
            var service = catalog.Procedures[_random.Next(catalog.Procedures.Count)];
            AddBillItem(bill, BillItemSourceType.Service, service, service.DefaultRate, 1m);
        }

        // A minority of bills carry an approved bill-level discount, so the discount columns and
        // the concession reports aren't uniformly zero.
        if (_random.Next(100) < 14)
        {
            bill.BillDiscountPercent = new[] { 5m, 10m, 15m, 20m }[_random.Next(4)];
        }

        RecomputeBillTotals(bill);

        // Credit bills (corporate/insurance settlement) stay unpaid on purpose — that is what
        // the outstanding-bills and receivables reports exist to show.
        bill.IsCreditBill = _random.Next(100) < 8;
        // A few bills are cancelled after the fact — reason-coded, as the real cancel action is.
        var isCancelled = _random.Next(100) < 2;

        // The payment is decided before the insert and hung off the bill's own collection, so the
        // visit, bill, lines and receipt all land in a single SaveChanges.
        PaymentModeRef? mode = null;
        var amount = 0m;
        if (isCancelled)
        {
            bill.Status = BillStatus.Cancelled;
            bill.CancelReason = "Billed against the wrong visit; corrected bill raised.";
            bill.CancelledAt = new DateTimeOffset(bill.BillDate.ToDateTime(new TimeOnly(17, 30)), TimeSpan.Zero);
        }
        else if (!bill.IsCreditBill)
        {
            // Most counter bills are settled in full the same day; some part-paid.
            amount = _random.Next(100) < 82
                ? bill.TotalAmount
                : Math.Round(bill.TotalAmount * (_random.Next(30, 80) / 100m), 2);
            if (amount > 0)
            {
                mode = PickPaymentMode(catalog);
                var receiptNumber = await sequences.NextValueAsync(SequenceKeys.ReceiptNumber, bill.BranchId, year, ct);
                bill.Payments.Add(new BillPayment
                {
                    PaymentModeId = mode.Id,
                    Amount = amount,
                    ReferenceNumber = mode.Code == "CASH" ? null : $"{mode.Code}{_random.Next(100000, 999999)}",
                    ReceiptNumber = $"RCP-{year}-{receiptNumber.ToString().PadLeft(6, '0')}",
                    PaidAt = new DateTimeOffset(
                        bill.BillDate.ToDateTime(new TimeOnly(10, 0)).AddMinutes(_random.Next(0, 8 * 60)), TimeSpan.Zero),
                });
                bill.PaidAmount = amount;
                bill.BalanceAmount = Math.Round(bill.TotalAmount - amount, 2);
                bill.Status = bill.BalanceAmount <= 0 ? BillStatus.Paid : BillStatus.PartiallyPaid;
            }
        }

        db.Bills.Add(bill);
        await db.SaveChangesAsync(ct);
        totals.Bills++;
        if (mode is null)
        {
            return;
        }
        totals.Payments++;

        await PostBillPaymentVoucherAsync(ctx, bill, mode, amount, catalog.RevenueAccountId, totals, ct);
    }

    /// <summary>
    /// Prefers a consultation service belonging to one of the doctor's own departments, so
    /// department-wise and doctor-wise revenue agree with each other instead of attributing a
    /// cardiologist's consultation to General Medicine.
    /// </summary>
    private ServiceRef PickConsultationService(BillingCatalog catalog, DoctorRef doctor)
    {
        var matching = catalog.Consultations
            .Where(s => s.DepartmentId is not null && doctor.DepartmentIds.Contains(s.DepartmentId.Value))
            .ToList();
        return matching.Count > 0
            ? matching[_random.Next(matching.Count)]
            : catalog.Consultations[_random.Next(catalog.Consultations.Count)];
    }

    /// <summary>Cash-heavy but card/UPI represented, so the payment-mode split is worth charting.</summary>
    private PaymentModeRef PickPaymentMode(BillingCatalog catalog)
    {
        var preferred = _random.Next(100) switch
        {
            < 45 => "CASH",
            < 72 => "UPI",
            < 95 => "CARD",
            _ => "CHEQUE",
        };
        return catalog.PaymentModes.FirstOrDefault(m => m.Code == preferred)
            ?? catalog.PaymentModes[_random.Next(catalog.PaymentModes.Count)];
    }

    private static void AddBillItem(Bill bill, BillItemSourceType sourceType, ServiceRef service, decimal rate, decimal quantity)
    {
        var gross = Math.Round(rate * quantity, 2);
        var tax = Math.Round(gross * service.TaxPercent / 100m, 2);
        bill.Items.Add(new BillItem
        {
            SourceType = sourceType,
            // The point of this whole section: a real ServiceId, so department-wise revenue works.
            ServiceId = service.Id,
            Description = service.Name,
            Quantity = quantity,
            Rate = rate,
            TaxPercent = service.TaxPercent,
            TaxAmount = tax,
            TotalAmount = gross + tax,
        });
    }

    /// <summary>
    /// Rolls the line totals up into the header the same way BillMath does — item totals form the
    /// sub-total, the bill-level discount percent applies on top, and tax rides along.
    /// </summary>
    private static void RecomputeBillTotals(Bill bill)
    {
        var lineNet = bill.Items.Sum(i => Math.Round(i.Rate * i.Quantity, 2) - i.DiscountAmount);
        var lineTax = bill.Items.Sum(i => i.TaxAmount);
        var billDiscount = Math.Round(lineNet * bill.BillDiscountPercent / 100m, 2);

        bill.SubTotal = Math.Round(lineNet, 2);
        bill.DiscountAmount = billDiscount;
        bill.TaxAmount = Math.Round(lineTax, 2);
        bill.TotalAmount = Math.Round(lineNet - billDiscount + lineTax, 2);
        bill.PaidAmount = 0m;
        bill.BalanceAmount = bill.TotalAmount;
    }

    /// <summary>
    /// Mirrors VoucherService.PostBillPaymentAsync exactly: a Receipt voucher debiting the payment
    /// mode's linked bank/cash account and crediting Patient Revenue, one LedgerEntry per
    /// VoucherLine, carrying the same <c>BILL_PAYMENT:{id}</c> source reference the live poster
    /// writes. Skipped silently when the mode has no linked account — the same no-op-until-
    /// configured posture the real hook has, rather than inventing an account.
    /// </summary>
    private async Task PostBillPaymentVoucherAsync(
        DemoContext ctx, Bill bill, PaymentModeRef mode, decimal amount, long revenueAccountId,
        HistoryTotals totals, CancellationToken ct)
    {
        if (mode.LinkedAccountId is not { } bankAccountId)
        {
            return;
        }

        var year = bill.BillDate.Year;
        var number = await sequences.NextValueAsync(SequenceKeys.ReceiptVoucherNumber, bill.BranchId, year, ct);
        var voucher = new Voucher
        {
            BranchId = bill.BranchId,
            VoucherNumber = $"RV-{year}-{number.ToString().PadLeft(6, '0')}",
            VoucherType = VoucherType.Receipt,
            VoucherDate = bill.BillDate,
            Narration = $"Payment received for bill {bill.BillNumber}",
            PostedByUserId = ctx.UserId,
            PostedAt = new DateTimeOffset(bill.BillDate.ToDateTime(new TimeOnly(18, 0)), TimeSpan.Zero),
            SourceReference = $"BILL_PAYMENT:{bill.Id}",
        };
        voucher.Lines.Add(new VoucherLine { AccountId = bankAccountId, DebitAmount = amount, CreditAmount = 0m });
        voucher.Lines.Add(new VoucherLine { AccountId = revenueAccountId, DebitAmount = 0m, CreditAmount = amount });
        db.Vouchers.Add(voucher);
        await db.SaveChangesAsync(ct);

        foreach (var line in voucher.Lines)
        {
            db.LedgerEntries.Add(new LedgerEntry
            {
                BranchId = bill.BranchId,
                VoucherId = voucher.Id,
                VoucherLineId = line.Id,
                AccountId = line.AccountId,
                EntryDate = bill.BillDate,
                DebitAmount = line.DebitAmount,
                CreditAmount = line.CreditAmount,
                Narration = voucher.Narration,
            });
        }
        await db.SaveChangesAsync(ct);
        totals.Vouchers++;
    }

    // ── IP admissions across the window ───────────────────────────────────────
    /// <summary>
    /// A steady trickle of admissions with realistic lengths of stay, so bed-occupancy, ALOS and
    /// the IP dashboard have history instead of a single day's worth. All but the most recent are
    /// discharged, which both frees the bed and gives the discharge-based reports something to
    /// count; the handful still admitted keep the bed board populated.
    /// </summary>
    private async Task SeedBranchAdmissionsAsync(
        DemoContext ctx, long branchId, DateOnly startDate, DateOnly today,
        List<PatientRef> patients, List<DoctorRef> doctors, BillingCatalog catalog,
        HistoryTotals totals, CancellationToken ct)
    {
        var beds = await db.Beds.IgnoreQueryFilters()
            .Where(b => !b.IsDeleted && b.BranchId == branchId)
            .Select(b => b.Id).ToListAsync(ct);
        if (beds.Count == 0 || catalog.RoomCharges.Count == 0)
        {
            return;
        }

        // Only fill dates that have no admission yet, same resume-not-duplicate guard as above.
        var existingDates = await db.Admissions.IgnoreQueryFilters()
            .Where(a => !a.IsDeleted && a.BranchId == branchId)
            .Select(a => a.AdmissionDate)
            .ToListAsync(ct);
        var occupiedDates = existingDates.Select(d => DateOnly.FromDateTime(d.UtcDateTime)).ToHashSet();

        // A bed can hold exactly one un-released allocation — the DB enforces it with a partial
        // unique index on (bed_id) WHERE released_at IS NULL. Picking beds at random therefore
        // isn't just unrealistic, it violates that index outright. This calendar tracks the first
        // date each bed is free again, which both satisfies the constraint and keeps generated
        // stays from overlapping in the same bed — overlapping stays would push the bed-occupancy
        // dashboard and Quality's occupancy indicator above 100%.
        var bedFreeFrom = beds.ToDictionary(id => id, _ => startDate);
        var existingAllocations = await db.BedAllocations
            .Where(a => beds.Contains(a.BedId))
            .Select(a => new { a.BedId, a.ReleasedAt })
            .ToListAsync(ct);
        foreach (var allocation in existingAllocations)
        {
            // Still-occupied beds are off the table entirely; released ones free up the next day.
            var freeFrom = allocation.ReleasedAt is { } releasedAt
                ? DateOnly.FromDateTime(releasedAt.UtcDateTime).AddDays(1)
                : DateOnly.MaxValue;
            if (bedFreeFrom.TryGetValue(allocation.BedId, out var current) && freeFrom > current)
            {
                bedFreeFrom[allocation.BedId] = freeFrom;
            }
        }

        for (var date = startDate; date <= today; date = date.AddDays(1))
        {
            if (occupiedDates.Contains(date))
            {
                continue;
            }
            // ~2-3 admissions a week per branch. The gate is derived from (branch, date) rather
            // than drawn from _random, so a re-run reaches the same verdict for every branch-day
            // it already decided. With a re-rolled coin flip the guard above only skips dates that
            // happen to have been chosen last time, and every re-run quietly piles more admissions
            // onto the dates it previously skipped — the OP loop above doesn't have this problem
            // because it writes a bill on every day it touches, so its guard always sees them.
            if (AdmissionGate(branchId, date) >= 35)
            {
                continue;
            }

            var freeBeds = bedFreeFrom.Where(b => b.Value <= date).Select(b => b.Key).ToList();
            if (freeBeds.Count == 0)
            {
                continue; // Branch is notionally full on this date.
            }

            var patient = patients[_random.Next(patients.Count)];
            var doctor = doctors[_random.Next(doctors.Count)];
            var bedId = freeBeds[_random.Next(freeBeds.Count)];
            var year = date.Year;
            var number = await sequences.NextValueAsync(SequenceKeys.AdmissionNumber, branchId, year, ct);
            var admittedAt = new DateTimeOffset(date.ToDateTime(new TimeOnly(_random.Next(8, 21), 0)), TimeSpan.Zero);

            var lengthOfStay = _random.Next(100) switch
            {
                < 35 => _random.Next(1, 3),
                < 75 => _random.Next(3, 6),
                < 93 => _random.Next(6, 10),
                _ => _random.Next(10, 18),
            };
            var dischargeDate = date.AddDays(lengthOfStay);
            var stillAdmitted = dischargeDate > today;
            var roomCharge = catalog.RoomCharges[_random.Next(catalog.RoomCharges.Count)];

            // Claim the bed for the length of this stay — indefinitely if the patient is still in.
            bedFreeFrom[bedId] = stillAdmitted ? DateOnly.MaxValue : dischargeDate.AddDays(1);

            var admission = new Admission
            {
                BranchId = branchId,
                PatientId = patient.Id,
                AdmissionNumber = $"IP-{year}-{number.ToString().PadLeft(6, '0')}",
                AdmissionDate = admittedAt,
                AdmissionType = _random.Next(100) < 62 ? AdmissionType.Planned : AdmissionType.Emergency,
                PrimaryConsultantId = doctor.Id,
                DepartmentId = doctor.DepartmentIds.Count > 0 ? doctor.DepartmentIds[0] : null,
                CurrentBedId = bedId,
                ProvisionalDiagnosis = DemoDiagnoses[_random.Next(DemoDiagnoses.Length)],
                Status = stillAdmitted ? AdmissionStatus.Admitted : AdmissionStatus.Discharged,
            };
            if (!stillAdmitted)
            {
                var dischargedAt = new DateTimeOffset(dischargeDate.ToDateTime(new TimeOnly(11, 0)), TimeSpan.Zero);
                admission.DischargeRequestedAt = dischargedAt.AddHours(-3);
                admission.DischargeRequestedByDoctorId = doctor.Id;
                // Overwhelmingly routine discharges, with the other dispositions represented
                // because the discharge-type breakdown is a reported figure.
                admission.DischargeType = _random.Next(100) switch
                {
                    < 92 => DischargeType.Normal,
                    < 96 => DischargeType.Referral,
                    < 99 => DischargeType.Lama,
                    _ => DischargeType.Death,
                };
                admission.DischargedAt = dischargedAt;
            }
            admission.BedAllocations.Add(new BedAllocation
            {
                BedId = bedId,
                BedChargePerDay = roomCharge.DefaultRate,
                AllocatedAt = admittedAt,
                ReleasedAt = stillAdmitted
                    ? null
                    : new DateTimeOffset(dischargeDate.ToDateTime(new TimeOnly(11, 0)), TimeSpan.Zero),
            });
            admission.Consultants.Add(new AdmissionConsultant
            {
                DoctorId = doctor.Id,
                Role = ConsultantRole.Primary,
                AssignedAt = admittedAt,
            });

            db.Admissions.Add(admission);
            await db.SaveChangesAsync(ct);
            totals.Admissions++;
            occupiedDates.Add(date);

            // Keep the bed board honest: a bed under a still-admitted patient must read Occupied.
            if (stillAdmitted)
            {
                var bed = await db.Beds.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.Id == bedId, ct);
                if (bed is not null && bed.Status == BedStatus.Vacant)
                {
                    bed.Status = BedStatus.Occupied;
                    await db.SaveChangesAsync(ct);
                }
            }

            await SeedIpBillAsync(ctx, admission, roomCharge, lengthOfStay, stillAdmitted, catalog, totals, ct);

            // Same reason as the OP loop: keep the tracked graph from growing across the window.
            db.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// Stable 0-99 value for a branch-day, used to decide whether an admission happens. Depends
    /// only on its inputs (not on <c>_random</c>'s position in its sequence), so the decision
    /// survives a re-run and the admission guard stays genuinely idempotent.
    /// </summary>
    private static int AdmissionGate(long branchId, DateOnly date)
    {
        unchecked
        {
            var mixed = (int)(branchId * 73856093) ^ (date.DayNumber * 19349663);
            mixed ^= mixed >> 13;
            return (int)((uint)mixed % 100);
        }
    }

    /// <summary>
    /// The IP bill for an admission: auto-managed bed-charge line for the days stayed, nursing,
    /// and a mix of investigations/procedures, dated at discharge (or today for a live admission)
    /// so IP revenue lands in the right period.
    /// </summary>
    private async Task SeedIpBillAsync(
        DemoContext ctx, Admission admission, ServiceRef roomCharge, int lengthOfStay, bool stillAdmitted,
        BillingCatalog catalog, HistoryTotals totals, CancellationToken ct)
    {
        var billDate = admission.DischargedAt is { } dischargedAt
            ? DateOnly.FromDateTime(dischargedAt.UtcDateTime)
            : DateOnly.FromDateTime(DateTime.UtcNow);
        var year = billDate.Year;
        var billNumber = await sequences.NextValueAsync(SequenceKeys.BillNumber, admission.BranchId, year, ct);
        var days = Math.Max(1, lengthOfStay);

        var bill = new Bill
        {
            BranchId = admission.BranchId,
            PatientId = admission.PatientId,
            AdmissionId = admission.Id,
            BillNumber = $"BILL-{year}-{billNumber.ToString().PadLeft(6, '0')}",
            BillDate = billDate,
            BillType = BillType.Ip,
        };

        AddBillItem(bill, BillItemSourceType.BedCharge, roomCharge, roomCharge.DefaultRate, days);
        foreach (var nursing in catalog.NursingCharges.Take(1))
        {
            AddBillItem(bill, BillItemSourceType.Service, nursing, nursing.DefaultRate, days);
        }
        for (var i = 0; i < _random.Next(2, 7); i++)
        {
            var service = catalog.Investigations[_random.Next(catalog.Investigations.Count)];
            AddBillItem(bill, BillItemSourceType.Service, service, service.DefaultRate, 1m);
        }
        for (var i = 0; i < _random.Next(0, 3); i++)
        {
            var service = catalog.Procedures[_random.Next(catalog.Procedures.Count)];
            AddBillItem(bill, BillItemSourceType.Service, service, service.DefaultRate, 1m);
        }
        if (_random.Next(100) < 22)
        {
            bill.BillDiscountPercent = new[] { 5m, 10m, 15m }[_random.Next(3)];
        }

        RecomputeBillTotals(bill);
        // Insurance/corporate settles a good share of inpatient work.
        bill.IsCreditBill = _random.Next(100) < 26;

        // A live admission's bill is still running, so it stays open and unpaid. Otherwise the
        // receipt is attached before the insert, so bill and payment share one SaveChanges.
        PaymentModeRef? mode = null;
        var amount = 0m;
        if (!stillAdmitted && !bill.IsCreditBill)
        {
            amount = _random.Next(100) < 74
                ? bill.TotalAmount
                : Math.Round(bill.TotalAmount * (_random.Next(40, 85) / 100m), 2);
            if (amount > 0)
            {
                mode = PickPaymentMode(catalog);
                var receiptNumber = await sequences.NextValueAsync(SequenceKeys.ReceiptNumber, bill.BranchId, year, ct);
                bill.Payments.Add(new BillPayment
                {
                    PaymentModeId = mode.Id,
                    Amount = amount,
                    ReferenceNumber = mode.Code == "CASH" ? null : $"{mode.Code}{_random.Next(100000, 999999)}",
                    ReceiptNumber = $"RCP-{year}-{receiptNumber.ToString().PadLeft(6, '0')}",
                    PaidAt = new DateTimeOffset(billDate.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero),
                });
                bill.PaidAmount = amount;
                bill.BalanceAmount = Math.Round(bill.TotalAmount - amount, 2);
                bill.Status = bill.BalanceAmount <= 0 ? BillStatus.Paid : BillStatus.PartiallyPaid;
            }
        }

        db.Bills.Add(bill);
        await db.SaveChangesAsync(ct);
        totals.Bills++;
        if (mode is null)
        {
            return;
        }
        totals.Payments++;

        await PostBillPaymentVoucherAsync(ctx, bill, mode, amount, catalog.RevenueAccountId, totals, ct);
    }

    // ── Reporting views ───────────────────────────────────────────────────────
    /// <summary>
    /// The five cross-cutting dashboards read the <c>reporting</c> materialized views, which a
    /// Hangfire job only refreshes every 15 minutes — so without this the dashboards would still
    /// look empty immediately after seeding and read as "the seed did nothing".
    /// CONCURRENTLY needs a committed populated view; a plain refresh is correct here and the
    /// brief lock is irrelevant in a dev database.
    /// </summary>
    private async Task RefreshReportingViewsAsync(CancellationToken ct)
    {
        // Statements are literals, not composed from any input — the refresh job owns these names.
        string[] statements =
        [
            "REFRESH MATERIALIZED VIEW reporting.mv_revenue_daily;",
            "REFRESH MATERIALIZED VIEW reporting.mv_daily_collection;",
            "REFRESH MATERIALIZED VIEW reporting.mv_op_census;",
        ];
        foreach (var statement in statements)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync(statement, ct);
            }
            catch (Exception ex)
            {
                // Never let a reporting-view refresh failure abort the seed — the job will retry.
                logger.LogWarning(ex, "Demo: could not run {Statement}", statement);
            }
        }
        logger.LogInformation("Demo: reporting materialized views refreshed");
    }
}
