# Module Spec — Loyalty Program engine (front-office extension, added 2026-07-27)

Status: **complete**. Not a numbered phase — added on top of the existing gap-list
build-out after a front-office menu audit found a full loyalty-card subsystem missing.
Unlike Camp/MHC (which extended OPD's `OpVisit` engine), a loyalty membership isn't a
visit, so this is a genuinely new module: masters (Card Category, Card Type/Member
Limit, Member Relation Tag, Card Price matrix) and transactions (Registration,
Renewal/Upgrade, Dependent Add/Remove, Duplicate Issue, Deactivate, Print
Direct/Preview). The only prior trace was `LoyaltyProgram` — a bare, still-unused
tag master (`FrontOfficeMasters.cs`), deliberately left untouched by this batch (see
Deferred/notes).

## Database (schema `master` for the four masters, new schema `loyalty` for the two transactional tables)

| Table | Notes |
|---|---|
| `master.loyalty_card_categories` | Flat tag (Individual/Family/Corporate…), standard `ISimpleMaster` CRUD. |
| `master.member_relation_tags` | Flat tag for dependents (Spouse/Child/Parent…), standard `ISimpleMaster` CRUD. |
| `master.loyalty_card_types` | The card product — `Code/Name/Description` + `MemberLimit` (max active dependents) + optional `LoyaltyCardCategoryId` FK. Bespoke (not `ISimpleMaster`) since it carries fields beyond Code/Name/Description, same reasoning as `DoctorPayoutRule`. Global (`BaseEntity`), not branch-scoped — card *products* are standardized; pricing is what varies per branch. |
| `master.loyalty_card_prices` / `loyalty_card_price_items` | The "Loyalty Card Price Matrix" — header (`BranchEntity`: `LoyaltyCardTypeId`, `EffectiveFrom`, `EffectiveTo?`) + child rows (`DurationMonths`, `Amount`). Mirrors `Tariff`'s branch+effective-window resolution and `BillEstimateTemplate`'s header+child CRUD shape. Unique per `(BranchId, LoyaltyCardTypeId, EffectiveFrom)`; child unique per `(PriceId, DurationMonths)`. |
| `loyalty.loyalty_cards` | The membership itself (`BranchEntity`): `CardNumber` (sequence, `LC-2026-000123`), `PatientId` (primary member), `LoyaltyCardTypeId`, `IssuedDate`, `DurationMonths`/`Amount` (snapshotted at registration/renewal), `ExpiryDate`, `Status` (`Active`/`Deactivated`), `DeactivateReason`/`DeactivatedAt`, `DuplicateIssueCount`. |
| `loyalty.loyalty_card_members` | Dependents — an open-row-history join to a real `Patient` record (`AddedAt`/`RemovedAt`, `RemovedAt == null` means currently active), mirroring `AdmissionConsultant`'s pattern rather than `PatientContact`'s free-text one. A dependent must already be (or first become) a registered `Patient` with their own UHID. Unique per `(LoyaltyCardId, PatientId) WHERE removed_at IS NULL` — can't be added twice while active. |

Migration: `Loyalty_CardEngine`.

## Price resolution (the "Price Matrix")

`LoyaltyCardService.ResolvePriceAsync(branchId, loyaltyCardTypeId, durationMonths, asOfDate)`
finds the newest `LoyaltyCardPrice` header for `(branch, type)` whose effective window
covers `asOfDate`, then the child item matching `durationMonths` — the exact same
resolution shape as `Tariff`. Both registration and renewal/upgrade re-resolve and
snapshot the price into `LoyaltyCard.Amount`; there's no live-price lookup at read time.

## Renewal vs. Upgrade — one endpoint, no history table

`POST /loyalty-cards/{id}/renew` covers both: the request carries an optional new
`LoyaltyCardTypeId` (Upgrade) and a required `DurationMonths` (Renewal, or Renewal +
Upgrade together) — same request shape either way, `LoyaltyCardTypeId` just optionally
changes. There is **no dedicated renewal-history table**; `LoyaltyCard` stays a flat
mutable row and the existing `SaveChangesInterceptor` row-diff audit log already
captures every field change on renew/upgrade/deactivate, per CLAUDE.md's standard audit
mechanism — a parallel history entity would be redundant.

## Dependents and the member limit

`AddDependentAsync` guards the card is `Active`, counts currently-active
(`RemovedAt == null`) members, and rejects once the count reaches
`LoyaltyCardType.MemberLimit` — the "Card Type Member Limit Fix" from the legacy menu.
Removing a dependent (`DELETE /loyalty-cards/{id}/members/{memberId}`) just stamps
`RemovedAt`, freeing a slot for a future add without losing the historical row.

## Deactivate — a separate, more consequential permission

`DeactivateAsync` mirrors `AdmissionService.CancelAsync`'s status-guard idiom
(`if (card.Status != Active) throw ...`). Gated on its own `LOYALTY.CARD.DEACTIVATE`
permission, deliberately separate from `LOYALTY.CARD.MANAGE` (which covers the routine
renew/upgrade/add-dependent/remove-dependent/duplicate actions) — ending a membership is
more consequential, same reasoning as `IPD.ADMISSION.CANCEL` being its own permission.

## Card Print (Direct vs. Preview) — a frontend-only distinction

`GET /loyalty-cards/{id}/card` returns one PDF (A6 card + QR encoding the card number,
mirrors `GenerateRegistrationCard`'s layout via `QuestPdfService`). "Preview" opens it in
a new tab (same pattern as every other slip/summary PDF in this codebase). "Direct"
fetches the same PDF and immediately triggers the browser print dialog via a hidden
`<iframe>`'s `contentWindow.print()`. There is no backend-level distinction — no other
document in Prixo has one either, and inventing a server-side split would be
unjustified complexity for a purely presentational choice.

## Endpoints

- `/api/v1/loyalty-cards`: paged `GET`, `GET {id}`, `POST` (register), `POST
  {id}/renew`, `POST {id}/members` (add dependent), `DELETE {id}/members/{memberId}`
  (remove dependent), `POST {id}/duplicate`, `POST {id}/deactivate`, `GET {id}/card` (PDF).
- `/api/v1/loyalty-card-categories`, `/api/v1/member-relation-tags`: standard
  `SimpleMasterControllerBase` CRUD.
- `/api/v1/loyalty-card-types`: bespoke CRUD (mirrors `/api/v1/doctor-payout-rules`).
- `/api/v1/loyalty-card-prices`: bespoke header+child CRUD (mirrors
  `/api/v1/bill-estimate-templates`'s full-replace-children-on-update shape).

## Permissions

`MASTER.LOYALTYCARDCATEGORY.{VIEW,MANAGE}`, `MASTER.MEMBERRELATIONTAG.{VIEW,MANAGE}`,
`MASTER.LOYALTYCARDTYPE.{VIEW,MANAGE}`, `MASTER.LOYALTYCARDPRICE.{VIEW,MANAGE}`,
`LOYALTY.CARD.VIEW`, `LOYALTY.CARD.CREATE` (register), `LOYALTY.CARD.MANAGE` (renew/
upgrade, add/remove dependent, duplicate issue), `LOYALTY.CARD.DEACTIVATE` (separate,
see above). Granted to a new `LOYALTY_DESK` role (no HOD split, like
`BIOMEDICAL_ENGINEER`) and to `RECEPTION` (`View`/`Create`/`Manage` only — not
`Deactivate`, reserved for `LOYALTY_DESK` + Hospital Admin).

## Frontend

- `src/features/loyalty` (new, self-contained — genuinely new module, not nested under
  an existing feature): `LoyaltyCardsPage` (`/loyalty`) mirrors `CampRegistrationPage`'s
  register-form-above-list shape (`PatientPicker` + card type + duration → register,
  list with a "View" action). `LoyaltyCardDetailPage` (`/loyalty/cards/:id`) shows the
  card header, a Members section (add/remove dependent), and header actions —
  Renew/Upgrade, Duplicate Issue, Deactivate, Print Direct, Print Preview.
- `src/features/masters/pages`: `LoyaltyCardCategoriesPage`/`MemberRelationTagsPage`
  reuse the generic `SimpleMasterPage`; `LoyaltyCardTypesPage` mirrors
  `DoctorPayoutRulesPage`; `LoyaltyCardPricesPage` mirrors
  `BillEstimateTemplatesPage`'s header+child editor.

## Deferred / notes

- **`LoyaltyProgram` was deliberately left untouched.** It's a pre-existing, still-unused
  Code/Name/Description tag with no relationship to this module — renaming or repurposing
  it risked confusing overload (and menu-code churn, since `DbSeeder`'s `EnsureMenuAsync`
  keys idempotency on `path::title`). If a genuine "program enrollment" concept distinct
  from card category/type is ever needed, `LoyaltyProgram` is still there for it.
- **Duration is a free-typed number of months at registration**, not a dropdown
  constrained to configured price durations — if no matching `LoyaltyCardPriceItem`
  exists for the chosen type/duration, registration fails with a clear "no price
  configured" error rather than silently guessing. A duration picker scoped to the
  selected type's actual configured durations would be a natural follow-up if this proves
  a common friction point.
- **No cross-branch card lookup/transfer** — a card is issued at, and scoped to, one
  branch (`LoyaltyCard : BranchEntity`), consistent with how the rest of Prixo scopes
  transactional data; a member visiting a different branch isn't handled by this pass.
- **Bug fixed in passing**: `MhcPackageService` (built in the MHC Registration batch) was
  never registered in the DI container, so `/api/v1/mhc-packages` would have thrown at
  runtime the first time it was called. Fixed in `DependencyInjection.cs` alongside this
  batch's own service registrations.
