# Module Spec — Insurance / TPA (Phase 24)

Status: **complete** (2026-07-29). Listed in the dev plan as a held module (§21), but built
now at the user's explicit direction rather than deferred — the plan's own future-phase
table (F1) already specified this module's intended shape: *"Pre-auth → enhancement → claim
→ settlement → denial tracking; placeholders already in billing schema"* — `Bill.InsuranceCompanyId`
and `master.insurance_companies` existed inert exactly as described; this phase makes them real.

A prior gap-fill pass (`docs/modules/patients.md`, "Registration gap-fill" section) added
policy capture to Patient registration as a simple `InsuranceRequired` flag + repeatable
policy sub-form — that lands in this module's schema (`insurance.patient_insurance_policies`)
since a policy is meaningless without somewhere to submit a claim against it.

## New domain (schema `insurance`)

| Table | Notes |
|---|---|
| `insurance.patient_insurance_policies` | A patient may hold more than one policy over time (TPA changes, renewals) — same repeatable-collection shape as `PatientContact`/`PatientIdentifier`. `InsuranceCompanyId` FKs the existing `master.insurance_companies` (Phase 2 placeholder, now genuinely referenced). Captured inline on Patient registration/update, not its own screen. |
| `insurance.pre_auth_requests` | Modeled directly on `Ipd.AdmissionRequest` — the codebase's closest existing pure request→approve/reject template: single pair of decision columns (`DecidedByUserId`/`DecidedAt`), strict single-step state machine. `Status`: Requested → Approved / PartiallyApproved / Rejected. `TreatmentType` (Op/Ip) requires the matching `OpVisitId`/`AdmissionId`. |
| `insurance.pre_auth_enhancements` | Mid-treatment request to raise an already-approved pre-auth's amount — the plan's "enhancement" stage. Same request→approve/reject shape as Billing's `DiscountApproval`/`Refund`. Approving adds the enhancement amount onto the parent `PreAuthRequest.ApprovedAmount`. |
| `insurance.claims` | Submitted against a `billing.bills` row. `Status`: Draft → Submitted → UnderReview → PartiallyApproved/Approved → Settled, or → Denied at any review step. Settlement fields (`SettledAmount`, `DeductionAmount`, `SettlementReferenceNumber`, `SettledAt`) are folded directly onto this row rather than split into a separate settlement entity — unlike Billing/Finance's `Bill`↔`Receivable` split (which exists because those are different modules), Claim already owns its own settlement since this is all one module. |
| `insurance.claim_documents` | Supporting-document upload, mirrors `Mrd.ScannedDocument`'s shape exactly: typed via `master.claim_document_types`, `VirusScanStatus` stays the same no-op placeholder (`IVirusScanner`). |

New masters (`master` schema, generic `ISimpleMaster` framework): `claim_document_types`
(Pre-Auth Form/Discharge Summary/Bill Copy/Policy Copy/...), `claim_denial_reasons`
(Pre-existing exclusion/Non-payable item/Documentation incomplete/Policy lapsed/...).

Migration: `Phase24_PatientOpdInsuranceFields` (shared with the Patients/OPD gap-fill in the
same phase — one `EnsureSchema("insurance")` plus this module's tables).

## Workflow

1. **Raise pre-auth** (`PreAuthService.RaiseAsync`) — against a specific policy and either an
   OP visit or an admission, with an estimated amount and diagnosis.
2. **Decide** (`DecideAsync`) — approve (with an approved amount, auto-flagged
   PartiallyApproved if less than estimated) or reject with a reason. Strict: only a
   `Requested` pre-auth can be decided (`DomainException` otherwise, no state skipping).
3. **Enhancement** (optional, only against an already-approved pre-auth) — its own
   request→decide cycle; approving bumps the parent's `ApprovedAmount`.
4. **Submit claim** (`ClaimService.SubmitAsync`) — against a finalized bill, optionally
   referencing the pre-auth. Stamps `Bill.InsuranceCompanyId` from the policy (the
   previously-inert placeholder field now gets a real value).
5. **Review** (`UpdateStatusAsync`) — Submitted→UnderReview→{PartiallyApproved,Approved,Denied},
   one step at a time; a denial requires a `ClaimDenialReason`.
6. **Settle** (`RecordSettlementAsync`) — only from Approved/PartiallyApproved, inside a
   transaction: writes the settlement fields, then posts to Finance via
   `IFinanceLedgerPoster.PostClaimSettlementAsync` (Debit the payment mode's linked bank/cash
   account, Credit Patient Revenue — same shape as `PostBillPaymentAsync`, since money
   arriving from an insurer functions like a delayed patient payment; a silent no-op if the
   branch hasn't configured Finance accounts, same posture as every other `IFinanceLedgerPoster`
   method). This follows the "explicit raise + record settlement" precedent Finance already
   established for Payables/Receivables, rather than auto-posting at claim-creation time.

## Finance integration

`IFinanceLedgerPoster` (`Application/Common/Interfaces`) gained
`PostClaimSettlementAsync(claimId, claimNumber, paymentModeId, amount, date, ct)`, implemented
on `VoucherService` alongside its existing `PostBillPaymentAsync`/`PostCreditNoteAsync`.

## Endpoints (`/api/v1/insurance`)

- `pre-auth-requests`: `GET pending`, `GET {id}`, `POST` (raise), `POST {id}/decide`
- `pre-auth-requests/{id}/enhancements`: `GET`, `POST` (request)
- `pre-auth-requests/enhancements/{enhancementId}/decide`: `POST`
- `claims`: `GET pending`, `GET {id}`, `POST` (submit), `POST {id}/status`, `POST {id}/settle`
- `claims/{id}/documents`: `POST` (multipart upload), `GET`, `GET {documentId}/download`
- `patients/{patientId}/insurance-policies`: `GET` (read-only lookup for the raise/submit screens — policies are captured on Patient registration, not here)

## Permissions

`INSURANCE.POLICY.{VIEW,MANAGE}`, `INSURANCE.PREAUTH.{VIEW,CREATE,APPROVE}` (APPROVE covers
both pre-auth and enhancement decisions), `INSURANCE.CLAIM.{VIEW,CREATE,MANAGE,SETTLE}`
(MANAGE covers status review + document upload). Two roles: `INSURANCE_EXECUTIVE`
(view/create/manage) and `INSURANCE_SUPERVISOR` (adds APPROVE/SETTLE plus management of the
`claim_document_types`/`claim_denial_reasons` masters) — mirrors Billing's
`BILLING_EXECUTIVE`/`BILLING_SUPERVISOR` split.

## Frontend (`src/features/insurance`)

- `PreAuthWorklistPage` (`/insurance/pre-auth`) — pending-request table + "Raise
  pre-authorization" modal (patient picker → policy select, scoped to that patient's own
  policies via the read-only lookup endpoint) + Approve/Reject decide modal, plus a second
  "Approved — Enhancements" table (added 2026-09-17, see above) with an `EnhancementsModal`
  per row for the mid-treatment amount-increase sub-workflow.
- `ClaimsWorklistPage` (`/insurance/claims`) — pending-claim table + "Submit claim" modal
  (patient → policy → bill picker), an "Update status" modal (Under Review/Approved/Partially
  Approved/Denied, with a denial-reason select when denying), and a "Settle" modal (settled
  amount, deduction, payment mode, settlement reference).

## Cash-vs-cashless awareness (added 2026-09-16)

`ClaimDto` now carries the bill's current `BillPaidAmount`/`BillBalanceAmount`, and
`ClaimsWorklistPage` flags a claim whose bill already has cash collected against it — the
Billing-side half (a pre-auth/claim banner on the bill itself) is documented in
`docs/modules/billing.md`'s "Insurance awareness on the bill". Advisory only; nothing here
blocks `SubmitAsync`/`RecordSettlementAsync`.

## Pre-go-live QA fixes (2026-09-17)

- **`GET pre-auth-requests/pending` and `GET claims/pending` 500'd on every call.** Both
  `PreAuthService.QueryDto()`/`ClaimService.QueryDto()` projected straight into their DTO
  record (`PreAuthRequestDto`/`ClaimDto` — each built from 5-6 joined tables) and then
  `GetPendingAsync` filtered *that already-projected* `IQueryable<TDto>` by `.Status`. EF Core
  can't translate a `Where` that reaches back through a many-argument record constructor
  spanning that many joins ("The LINQ expression ... could not be translated") — meaning the
  **Pre-Authorization Requests and Claims worklists have never loaded**, always throwing
  before the RBAC/data even mattered. Fixed by renaming `QueryDto()` to `Query()` (returns
  `IQueryable<PreAuthRequest>`/`IQueryable<Claim>`, the raw entity), filtering that directly
  (a simple, directly-mapped column — always translatable), and only then projecting via a
  reusable `Expression<Func<TEntity, TDto>> ToDto` — same shape `BillingService.GetByIdAsync`
  already used successfully. `GetByIdAsync` on both services had the identical latent bug and
  is fixed the same way.
- **Enhancement sub-workflow had no UI at all.** The backend fully implements pre-auth
  "enhancement" (mid-treatment amount increase — `GET/POST .../enhancements`,
  `POST .../enhancements/{id}/decide`) and `insurance/api.ts` already had the hooks
  (`usePreAuthEnhancements`/`useRequestEnhancement`/`useDecideEnhancement`), but
  `PreAuthWorklistPage` never called them — no button anywhere led to them. Worse,
  `usePendingPreAuthRequests` only ever returns `Requested` rows, and an enhancement can only
  be raised against an already-**Approved**/PartiallyApproved pre-auth, so even a determined
  developer wiring a button onto that table would find it structurally unreachable — an
  approved pre-auth simply disappears from the only list the page fetched. Added
  `PreAuthService.GetApprovedAsync` + `GET pre-auth-requests/approved` (same
  `INSURANCE.PREAUTH.VIEW` gate) and a second "Approved — Enhancements" table on
  `PreAuthWorklistPage`, each row driving a new `EnhancementsModal` (history list + "Request
  enhancement" for `INSURANCE.PREAUTH.CREATE` + inline Approve/Reject for
  `INSURANCE.PREAUTH.APPROVE`) — the same two-section "Pending / Approved" layout
  `RefundApprovalsPage` already uses for its own pending-vs-issuable split.
- Confirmed as correct, not a bug: `ClaimsWorklistPage`'s "Submit claim" modal always sends
  `preAuthRequestId: null` — there's no picker for it. Per the workflow doc this reference is
  optional, and Claim/Settlement function correctly without it; it's a nice-to-have (letting
  staff link a claim back to the pre-auth it followed) rather than a wrong-data bug, so left
  alone here.
- **`PreAuthWorklistPage`'s "Raise pre-authorization" modal always sent `opVisitId: null,
  admissionId: null` — unlike the Claims case above, this one *did* break something already
  shipped.** `BillingService.GetInsuranceStatusAsync` (the "Insurance awareness on the bill"
  banner, `docs/modules/billing.md`) only ever finds a pre-auth for a bill by matching
  `PreAuthRequest.OpVisitId`/`.AdmissionId` against the bill's own visit/admission — with the
  raise form never populating either, no pre-auth could ever match, so the pre-auth half of that
  banner (the claim half, keyed off `Claim.BillId` which *is* required, still worked) was dead
  on arrival for every pre-auth raised through the UI. Fixed: the modal now has an optional
  "OP visit"/"Admission" `<Select>` (label follows the Treatment type field, reset when treatment
  type changes) sourced from the patient's own visits/admissions (`useOpVisits`/`useAdmissions`,
  reused as-is from OPD/IPD) — picking one sets `opVisitId` or `admissionId` accordingly; leaving
  it blank still raises the pre-auth exactly as before (genuinely optional — a pre-auth can
  predate the visit being booked).

## Deferred / notes

- No dedicated claims-aging or TAT report yet — the worklist pages are the only reporting
  surface; a proper report would reuse the same `ReportResultDto`/`ReportExportHelper` path
  every other ad-hoc report in this codebase uses.
- `PatientInsurancePolicy` has no renewal/expiry notification — same posture as the rest of
  this codebase's placeholder-heavy modules (e.g. `notification_queue` stays an outbox with
  no sender wired).
- Denial tracking is a single `DenialReasonId` + free-text `DenialNotes`; no structured
  appeal/resubmission workflow after a denial (resubmitting means submitting a new claim).
