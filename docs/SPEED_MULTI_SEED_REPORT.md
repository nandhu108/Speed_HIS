# Speed Multi Speciality Hospital — seed report

What was built, which hospital data it came from, and how it was checked.
Open questions for the hospital are in [SPEED_MULTI_ONBOARDING.md](SPEED_MULTI_ONBOARDING.md).

## 1. Source data reviewed (`/home/durgaram/Desktop/datahis/`)

| File | Content | Used for |
|---|---|---|
| `HIS_Implementation_Data_Collection_20_Beds_Filled.xlsx` | 12 sheets: Hospital Profile, Departments, Doctors, OP Price List, Lab/Service Price, Registration Fields, Patient Categories, Bed & Ward, User Roles, Other Charges, Health Check-up Plans | Everything seeded below |
| `3. Speed Regitration Certificate.pdf`, `MEDICAL CENTRE GST.pdf` | GST REG-06 for Sai Speed Medical Centre Pvt Ltd, GSTIN 33AAPCS5975A1Z6 | Branch GSTIN, legal entity |
| `1. Speed Clinical Certificate.pdf` | TN Clinical Establishment registration KCPMALL20210027963, 30 beds, valid to 06-07-2026 | Cross-checks only (not seeded) |
| `Dr.Vinayak Certificate .pdf` | Scanned image, no extractable text | Not read |

## 2. What was done

- Created git branch `speed-multi-seed` and a new `SpeedMultiSeeder` (`backend/src/Prixo.Infrastructure/Persistence/SpeedMultiSeeder.cs`), registered in DI and triggered from `Program.cs` by `Seed:SpeedMulti=true`.
- It is separate from `DemoDataSeeder` because this is real data: not Development-gated, idempotent (keyed by code/username), never overwrites admin edits.
- Pharmacy is out of scope per the user: no store, drugs or pharmacy department created; the Pharmacy code is untouched.
- Rule applied: values the hospital did not supply are **not guessed** (placeholders or 0, listed in the onboarding doc).

## 3. Seeded data vs. workbook (verification)

| Item | Workbook | Seeded | Check |
|---|---|---|---|
| Departments | 22 | 21 | OK — Pharmacy (DEP008) intentionally excluded |
| Doctors | 17 | 17 users + 17 doctor profiles + 16 specialties | OK |
| Doctor → department | 16 mapped, 1 blank | 16 mapped, 1 unmapped | OK (DR017 not inferred) |
| Beds | 20 (10 GW / 4 SP / 4 PW / 2 ICU) | 20, same split, ICU ward `IsIcu` | OK |
| ICU bed rate | ₹7,500/day | `RoomTypeCharge` ₹7,500 | OK |
| Other bed rates | blank | 0 | Flagged |
| Priced charges | OP list 5 + diagnostics 5 + "Other Charges" 24 = 34 | 32 services, 32 tariff items | OK — the 2 not seeded as services are Room Rent ₹2,500 (category unspecified, not applied) and ICU ₹7,500 (seeded as the ICU bed rate instead) |
| Unpriced items | CT, MRI, surgeon fees, equipment, IP/insurance prices | none created | OK (no invented prices) |
| Patient categories | General, Insurance, Corporate, Staff, VIP | already exist in base seed (GEN, INSURANCE, CORPORATE, STAFF, VIP) | OK — no new rows |
| User roles | 8 roles | map to existing system roles (Front Office, Billing Executive, Doctor, Lab Technician, Pathologist, Nurse, Hospital Admin); Pharmacist role not used | OK — no per-role users (counts not given) |
| Health check plans | 3 | not seeded | Flagged — billable price unclear |

Runtime test: fresh PostgreSQL 16, all migrations applied, API started twice with `Seed__SpeedMulti=true`.
Both runs completed without error; counts after run 2 were identical to run 1 (20 beds, 17 doctors,
17 users, 32 services, 16 specialties). A first attempt failed on a `varchar(30)` specialty-code overflow;
fixed with short codes (`SM-SP01…`) before the passing runs.

## 4. Findings from the data check

- Bed strength conflicts: notes 50, template 20, clinical certificate 30.
- Clinical Establishment certificate expired 06-07-2026 (today is after that).
- The two GST certificates show different addresses (527 Poonamallee High Road vs 25 School Street, Mangadu).
- Hospital registration number, email, website, contact person and go-live date are missing.
- Doctor registration numbers, mobiles and OP days are missing for all 17; 3 doctors lack qualification/department details.
- OP fees are generic tiers, not per doctor; doctor consultation fees are 0 until mapped.

## 5. How to run

```bash
# migrations applied first
Seed__SpeedMulti=true dotnet run --project backend/src/Prixo.Api
# optional: Seed__SpeedMultiDoctorPassword=<password>  (default: Seed:AdminPassword; forced change on first login)
```

Doctor logins: `dr.<name>`, e.g. `dr.saravanan`, `dr.k.vinayak.senthil`.
