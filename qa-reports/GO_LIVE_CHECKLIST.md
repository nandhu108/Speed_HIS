# Speed Multi Speciality Hospital — Go-live checklist

The code defects from the QA pass are fixed. Go-live still depends on the items below. Most are **hospital data** that cannot be invented in code (prices, vendors) and **production setup**. Each item says who owns it and where to do it.

## A. Hospital data to enter before go-live (Masters screens)

| # | Item | Why it matters | Where |
|---|---|---|---|
| A1 | **Lab test prices.** Map each of the 69 lab tests to a billing service and give it a price in the tariff. | Without a price, lab tests appear on the bill at ₹0 "(price not configured)". | Masters › Services (create, e.g. "CBC"); Laboratory › Tests › edit › *Billing service*; Masters › Tariffs |
| A2 | **Radiology test prices.** Same for the 54 radiology tests. | Same as above. | Masters › Services; Radiology › Tests › *Billing service*; Tariffs |
| A3 | **Bed charges** for General Ward, Semi-Private and Private. Only ICU (₹7,500/day) was in the workbook. | IP stays in those wards bill ₹0 for the bed. | Masters › Locations & Beds › Room types (bed charge / room-type charges) |
| A4 | **Pharmacy vendors** (suppliers, GSTIN, drug licence). | Purchase orders and GRNs need a vendor. | Masters › Vendors |
| A5 | **Opening pharmacy stock**, received by GRN into "Main Pharmacy" (`SM-PHARM`) with batch, expiry and MRP. | Nothing can be dispensed until stock exists. | Pharmacy › Purchase Requests → Orders → GRN → Post |
| A6 | **Doctor weekly schedules.** All 17 doctors currently have none. | Appointments are refused outside a doctor's schedule. | Appointments › Doctor Schedules |
| A7 | **Doctor ↔ department mapping** for doctors still without one. | It narrows the doctor list after department selection, and the OP Camp CSV import uses it. | Masters › Doctors |
| A8 | **Staff user accounts**, one per person, with the right roles. Use the department roles: Front Office, OP Billing, Billing Supervisor, Cashier, Nurse, Head Nurse, Lab Technician, Pathologist, Radiology Technician, Radiologist, Pharmacist, Pharmacy Manager, OT Coordinator and so on. | Each department's actions and discharge clearances are permission-gated. | Administration › Users |
| A9 | **Remove QA test data.** Users `qa.*`, patients "QATEST …", store "QA Main Pharmacy", theatre "QA-OT2", service "QA-TAXED", corporate "QACORP". The simplest route is to go live on a **fresh database** (step B3). | Test records must not mix with real patients. | — |

## B. Production setup (IT)

| # | Item |
|---|---|
| B1 | Set `ASPNETCORE_ENVIRONMENT=Production`. Demo data is only ever seeded in Development, and `Seed:DemoData` must stay `false`. |
| B2 | Set a strong, secret `Jwt:Key` (32+ random characters), a non-default DB password in `ConnectionStrings:Default`, and the real site URL in `Cors:AllowedOrigins`. |
| B3 | On a fresh database run `dotnet ef database update` (migrations up to `20261113_GoLive_QaFixes`). Start once with `Seed:SpeedMulti=true`, which creates the branch, departments, doctors, wards/beds, services and tariff, "Main Pharmacy", OT1 and settings. Change the admin password at first login. |
| B4 | Serve over **HTTPS** (reverse proxy / nginx) and do not expose PostgreSQL (port 5432) publicly. |
| B5 | Set up **daily database backups** (`pg_dump`) with off-machine copies, and test one restore before go-live. |
| B6 | Keep `logs/` (Serilog, 30-day roll) and the `App_Data/uploads` folder on persistent storage. |
| B7 | Review **Administration › Settings**: password policy, session timeout, `Billing.DiagnosticsGate` (default **Paid**, meaning OP tests must be paid before sample collection or scan), `Ot.HourlyChargeServiceCode` (`SM-OT-HOUR`), receipt footer and the max advance booking days. |

## C. Before the first patient (dry run with real staff)

1. **OP journey:** register a patient, book and check in an appointment, consult, order lab and radiology, generate the bill (tests pulled in), take payment, collect the sample, do the scan, report, dispense, and check the EMR.
2. **IP journey:** admit with a deposit, enter orders and nursing notes, issue pharmacy to the IP patient (it appears on the IP bill), then settle the bill. Then give clearances: billing, pharmacy, nursing and doctor. The treating doctor e-signs the summary, then discharge.
3. **Surgical journey:** book OT, complete Sign In (all items) and consent, start, record implants with serial numbers, Time Out, Sign Out, complete (OT time charged), then discharge.
4. **Day close:** reconcile counter cash against Billing › Day Close. Deposits are included and cash refunds are netted.
5. **UI check:** screens were not exercised by the automated QA (API-level only). Have each department walk its screens once.
