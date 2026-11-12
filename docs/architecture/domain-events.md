# Domain Events — Prixo HIS

**Status:** design, 2026-08-13. **Nothing exists today** — the codebase has no MediatR, no domain
events, no outbox, no broker. This is a greenfield addition (Phase 8), which means the vocabulary can
be designed once, correctly, without migration pain.

---

## 1. Design position

- **Events are facts about the past**, named `<Aggregate><PastTenseVerb>`. Never commands.
- **Published in the same transaction as the change** via a transactional outbox — an event that
  claims something happened must not exist if it did not.
- **Dispatched asynchronously, in-process first.** A broker is introduced only when a consumer runs
  in a different process (Phase 9, Notification). No Kafka.
- **Consumers are additive.** Publishing an event must never change the publisher's behaviour, and no
  business rule may depend on a consumer having run.
- **Audit is the first consumer of everything.** Every event listed here lands in
  `audit.audit_event`.

---

## 2. Envelope

Every event carries the same envelope. Fields marked ● are mandatory.

```jsonc
{
  "eventId":        "●uuid",           // idempotency key for consumers
  "eventType":      "●LabResultApproved",
  "schemaVersion":  "●1",
  "occurredAt":     "●2026-08-13T09:41:22Z",
  "correlationId":  "●uuid",           // the causal thread (see §5)
  "causationId":    "uuid",            // the event/request that caused this one
  "sourceService":  "●Laboratory",
  "branchId":       "●long",
  "departmentId":   "long",
  "actor": {
    "userId": "●long", "username": "string",
    "roleCodes": ["ROLE_AS_AT_EVENT_TIME"],   // point-in-time, not current
    "sessionId": "uuid", "ipAddress": "string", "deviceId": "string"
  },
  "subject": {
    "patientId":     "long",           // ● when the event concerns a patient
    "encounterType": "OpVisit|Admission|EmergencyVisit",
    "encounterId":   "long",
    "entityType":    "●LabResult",
    "entityId":      "●long"
  },
  "payload":  { /* event-specific, minimal, no free-text PHI */ },
  "reason":   "string",                // ● for cancel / override / correction events
  "approvalId": "long"                 // ● when the action was approval-gated
}
```

**Rules**
- `payload` carries identifiers and state transitions, **not clinical narrative**. Notes, findings,
  and summaries stay in the owning table; the event references them.
- No PHI beyond `patientId` in any field that reaches a log sink.
- `roleCodes` is captured **as at event time** — a later role change must not rewrite history.

---

## 3. Event catalogue

Grouped by publishing service. All are `schemaVersion: 1` at introduction.

### Patient Identity
`PatientRegistered` · `PatientUpdated` · `PatientMerged` · `PatientBlocked` · `PatientUnblocked` ·
`ConsentCaptured` · `PatientDocumentUploaded`

### Appointment
`AppointmentBooked` · `AppointmentRescheduled` · `AppointmentCancelled` · `AppointmentCheckedIn` ·
`TokenGenerated` · `TokenSkipped` · `TokenTransferred` · `AppointmentNoShow`

### Encounter / Clinical
`EncounterCreated` · `EncounterClosed` · `VitalsRecorded` · `DiagnosisRecorded` ·
`ClinicalNoteSigned` · `PrescriptionCreated` · `ReferralRaised` · `OpinionRecorded` ·
`FollowUpAdvised`

### Laboratory
`LabOrderCreated` · `LabOrderCancelled` · `SampleCollected` · `SampleReceived` · `SampleRejected` ·
`LabResultEntered` · `LabResultVerified` · `LabResultApproved` · **`CriticalResultDetected`** ·
`CriticalResultAcknowledged`

### Radiology
`RadiologyOrderCreated` · `RadiologyScheduled` · `ContrastConsentVerified` · `ImagesCaptured` ·
`RadiologyReportEntered` · `RadiologyReportVerified` · `RadiologyReportApproved`

### Pharmacy / Supply
`PurchaseOrderPlaced` · `StockReceived` (GRN) · `MedicationDispensed` · `StockIssued` ·
`StockReturned` · `StockAdjusted` · `StockTransferred` · `NearExpiryDetected` · `LowStockDetected`

### IPD
`AdmissionRequested` · `AdmissionCreated` · `BedAllocated` · `BedTransferred` ·
`ConsultantTransferred` · `MedicationOrdered` · `ProgressNoteRecorded` · `DischargeRequested` ·
`ClearanceGranted` · `DischargeSummaryApproved` · `DischargeCompleted`

### Nursing
`NursingAssessmentRecorded` · **`MedicationAdministered`** · `MedicationSkipped` ·
`MedicationRefused` · `HighAlertDoubleVerified` · `RiskScoreRecorded` · `CarePlanUpdated` ·
`HandoverCompleted` · `IncidentReported` · `IncidentReviewed` · `RcaRecorded` · `IncidentClosed`

### OT
`SurgeryScheduled` · `SurgeryRescheduled` · `SurgeryCancelled` · `ChecklistPhaseCompleted` ·
`SurgeryStarted` · `SurgeryCompleted` · `ImplantUsed` · `OperativeNoteRecorded` ·
`PostOpNoteRecorded`

### ICU
`VentilatorSessionStarted` · `VentilatorSessionEnded` · `InfusionStarted` · `IcuVitalsRecorded` ·
`IcuAlertRaised` · `SeverityAssessed`

### Emergency
`EmergencyVisitRegistered` · `TriageCompleted` · `EmergencyAssessed` · `DispositionRecorded` ·
`MlcRegistered` · `AdmitConverted`

### Billing
`BillCreated` · `BillFinalized` · `BillCancelled` · **`PaymentReceived`** · `AdvanceCollected` ·
`AdvanceDrawn` · `DiscountRequested` · `DiscountApproved` · `DiscountRejected` · `RefundRequested` ·
**`RefundApproved`** · `RefundIssued` · `CreditNoteIssued` · `DayClosed`

### Finance
`VoucherPosted` · `VoucherReversed` · `PayableRaised` · `PayableSettled` · `ReceivableRaised` ·
`ReceivableCollected`

### Insurance / TPA
`PolicyCaptured` · `PreAuthRequested` · `PreAuthApproved` · `PreAuthRejected` ·
`EnhancementRequested` · **`InsuranceClaimSubmitted`** · `ClaimUnderReview` · **`ClaimApproved`** ·
**`ClaimRejected`** · `ClaimSettled`

### MRD
`RecordFileCreated` · `FileIssued` · `FileReturned` · `DocumentScanned` · `CaseCoded` ·
`RecordDestructionApproved` · `RecordDestroyed`

### Biomedical
`EquipmentRegistered` · `EquipmentMoved` · `PmDue` · `WorkOrderRaised` · `WorkOrderCompleted` ·
`BreakdownReported` · `BreakdownResolved` · `CalibrationDue` · `CalibrationRecorded`

### Quality & Compliance *(new)*
`NonConformanceRaised` · **`AuditGapDetected`** · **`CapaCreated`** · `CapaVerified` ·
**`CapaClosed`** · `RiskRegistered` · `RiskReviewed` · `EvidenceUploaded` · `EvidenceVerified` ·
`RequirementMapped` · `FrameworkVersionActivated` · **`SafetyEventDetected`** ·
`SafetyEventAcknowledged` · `SafetyEventEscalated` · `DocumentationGapDetected`

### Identity & Access
`UserAuthenticated` · `AuthenticationFailed` · `SessionRevoked` · `PermissionsChanged` ·
`UserLocked` · `PasswordChanged`

### Access / Privacy *(new)*
`PatientRecordAccessed` · `DocumentDownloaded` · `ReportExported` · `CrossBranchAccessGranted` ·
`BreakGlassAccessUsed`

> The last group is the answer to "who opened this record" — today unanswerable across 25
> file-returning endpoints. See [audit-architecture.md](audit-architecture.md#5-access-logging).

---

## 4. Consumer map

| Consumer | Subscribes to | Purpose |
|---|---|---|
| **Audit backbone** | **everything** | `audit.audit_event` rows; patient journey read model |
| **Notification** | `CriticalResultDetected`, `IcuAlertRaised`, `SafetyEventDetected`, `AuditGapDetected`, `AppointmentBooked`, `AppointmentReminderDue`, `DischargeSummaryApproved`, `CapaDueSoon`, `PmDue`, `CalibrationDue`, `NearExpiryDetected` | Deliver + escalate |
| **Documentation completeness** | encounter/clinical/nursing/OT/discharge events | Recompute completeness for the affected encounter |
| **Patient safety engine** | `PrescriptionCreated`, `MedicationAdministered`, `LabResultApproved`, `VitalsRecorded`, `RiskScoreRecorded`, `ChecklistPhaseCompleted`, `ConsentCaptured` | Evaluate safety rules |
| **Quality / compliance** | approval, incident, CAPA, evidence, gap events | Requirement evidence status, audit readiness |
| **Analytics** | financial, census, TAT, throughput events | Dashboards and warehouse |
| **Finance posting** | `PaymentReceived`, `ClaimSettled`, `StockReceived` | Ledger vouchers (no-op until accounts configured) |

---

## 5. Correlation

One `correlationId` is minted at the **start of a patient-facing thread of work** and propagated
through everything it causes:

```
PatientRegistered ──corr:A──▶ AppointmentBooked ──corr:A──▶ EncounterCreated ──corr:A──▶
DiagnosisRecorded ──corr:A──▶ LabOrderCreated ──corr:A──▶ SampleCollected ──corr:A──▶
LabResultApproved ──corr:A──▶ BillCreated ──corr:A──▶ PaymentReceived
```

**Propagation rules**
- HTTP: `X-Correlation-Id` request header; minted by the API if absent, echoed in the response, and
  attached to the Serilog `LogContext`.
- Events: `correlationId` copied from the causing request/event; `causationId` = the immediate cause.
- Background jobs: mint a new correlation id per job run and record it on the job's activity event.
- **Business identifiers stay in `subject`** (`patientId`, `encounterId`, `entityType/entityId`) and
  are the primary query axis; `correlationId` answers "what else happened because of this".

---

## 6. Delivery mechanics

### 6.1 Outbox (Phase 8)

```
audit.outbox_message
  id · event_id · event_type · schema_version · payload(jsonb) · correlation_id
  occurred_at · available_at · attempts · status(Pending|Dispatched|Failed|DeadLettered)
  last_error · dispatched_at
```

- Written **in the same transaction** as the business change (the codebase already has
  `IUnitOfWork.ExecuteInTransactionAsync` on every relevant path).
- A dispatcher (Hangfire recurring job initially) claims batches with `FOR UPDATE SKIP LOCKED`,
  invokes in-process handlers, and marks status.
- Retry: exponential backoff with jitter, capped; after N attempts → `DeadLettered` + alert.
- Consumers must be **idempotent on `eventId`** — at-least-once delivery is the contract.

### 6.2 When a broker becomes justified

Introduce one only when **all** hold: a consumer runs in another process; outbox dispatch latency or
throughput is measurably inadequate; and operational ownership for the broker exists. Evaluate
PostgreSQL `LISTEN/NOTIFY` first — the database is already there and already HA-relevant.

---

## 7. Versioning

- Events are versioned by `schemaVersion`, starting at `1`.
- **Additive changes only** within a version: new optional fields. Consumers ignore unknown fields.
- A breaking change publishes `<EventName>` v2 **alongside** v1 until every consumer migrates.
- Event names are never reused for a different meaning.
- The audit store keeps the raw payload and its `schemaVersion` forever — audit reports must be able
  to reproduce what the event meant when it was written.

---

## 8. Anti-patterns to reject in review

| Anti-pattern | Why |
|---|---|
| `SendSmsRequested` as a domain event | That is a command; publish `CriticalResultDetected` and let Notification decide |
| Business logic that waits for a consumer | Couples the transaction to async delivery |
| Clinical narrative in `payload` | PHI leakage into logs/queues; the owning table is the record |
| Publishing outside the transaction | Produces events for changes that rolled back |
| A module consuming its own event to do work it should have done inline | Hidden control flow |
| Consumers reading the publisher's tables to "enrich" the event | Recreates the coupling events were meant to remove |
