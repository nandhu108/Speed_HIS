# Speed Multi Speciality Hospital — onboarding seed

Source: `HIS_Implementation_Data_Collection_20_Beds_Filled.xlsx` + the registration certificates.
Code: `backend/src/Prixo.Infrastructure/Persistence/SpeedMultiSeeder.cs` (branch code `SPEED_MULTI`).

## Running it

Real reference data, so it is **not** Development-gated and **not** part of `DemoDataSeeder`.
Opt in with config `Seed:SpeedMulti=true` (env `Seed__SpeedMulti=true`); startup then runs
`DbSeeder` (idempotent) followed by `SpeedMultiSeeder`. Migrations must already be applied.
Doctor logins get the password in `Seed:SpeedMultiDoctorPassword` (falls back to
`Seed:AdminPassword`) with forced change on first login.

Idempotent: rows are keyed by code/username and only added when missing; admin edits are never
overwritten. Verified against a fresh PostgreSQL 16 — two runs, identical counts.

## What is seeded

| Area | Rows |
|---|---|
| Branch | `SPEED_MULTI`, address/phone from the workbook, GSTIN `33AAPCS5975A1Z6` (from the GST certificate) |
| Departments | 21 (workbook list, **Pharmacy omitted**) — global; reused by code/name if present |
| Specialties / Doctors | 16 / 17 doctors, each with a login `dr.<name>` (DOCTOR + DOCTOR_PORTAL roles), mapped to this branch |
| Beds | 20: General 10, Semi-Private 4, Private 4, ICU 2 (ICU ward flagged `IsIcu`) |
| Services + tariff | 32 priced services (registration, consultations, OP procedures, IP/OT charges, X-Ray, USG, Echo, ECG, TMT) in one branch tariff `SM-GENERAL` (no category → applies to all) |
| Other | Default theatre `OT1`; `admin` user granted access to the branch |

Not created on purpose: pharmacy store/drugs, per-role staff users (counts not given), insurance/
corporate/staff/VIP tariffs (policy not given), health-check packages (billable price unclear).

## Open items to confirm with the hospital

1. **Doctor registration numbers** — stored as `PENDING-DRxxx`; need the TN Medical Council numbers.
2. **Doctor fees** — `ConsultationFee`/`FollowUpFee` are 0; the workbook only has generic tiers (visiting ₹600, specialist ₹1200, DMO ₹200, MD ₹200) and says doctor-wise mapping is pending. The tier fees exist as services but are not yet tied to doctors.
3. **Bed rates** — only ICU (₹7,500/day) is known. General/Semi-Private/Private are **0**; the unassigned "Room Rent ₹2,500/day" was not applied to any category.
4. **Bed count** — notes say 50, template 20, clinical certificate 30. 20 seeded.
5. **Dr S Sampath Kumari** — no department/specialization in the workbook; left unmapped (qualification suggests O&G, not assumed). Dr G Gopu and Dr Yazar Mohamed have no qualification.
6. **Unpriced** — CT Scan, MRI, surgeon fees, equipment, all lab tests, IP prices, insurance/corporate prices.
7. **Ambiguous prices** — four suturing tiers (labels unclear), "Dressing - major ₹900" (handwriting), OT assistant tiers, Physiotherapy ₹2,000.
8. **Health-check plans** — each has two prices; confirm which is billable, then add as MHC packages.
9. **Legal/compliance** — legal entity name ("A unit of Sai Speed"), GST certificates show two different addresses, Clinical Establishment certificate **expired 06-07-2026**, hospital registration number missing, contact person/email/go-live date missing.
