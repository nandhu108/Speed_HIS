namespace Prixo.Application.Common.Security;

public record PermissionDefinition(string Code, string Description)
{
    public string Module => Code.Split('.')[0];
    public string Resource => Code.Split('.')[1];
    public string Action => Code.Split('.')[2];
}

/// <summary>
/// The single source of truth for permission codes. Seeded into the database at startup;
/// referenced by [HasPermission] and mirrored by the frontend. Modules append their
/// blocks here as phases land — codes are never invented ad hoc.
/// </summary>
public static class PermissionRegistry
{
    // ── Admin / RBAC (Phase 1) ─────────────────────────────────────────────
    public const string UserView = "ADMIN.USER.VIEW";
    public const string UserCreate = "ADMIN.USER.CREATE";
    public const string UserUpdate = "ADMIN.USER.UPDATE";
    public const string UserDelete = "ADMIN.USER.DELETE";
    public const string UserResetPassword = "ADMIN.USER.RESETPASSWORD";
    public const string UserManagePermissions = "ADMIN.USER.MANAGEPERMISSIONS";

    public const string RoleView = "ADMIN.ROLE.VIEW";
    public const string RoleCreate = "ADMIN.ROLE.CREATE";
    public const string RoleUpdate = "ADMIN.ROLE.UPDATE";
    public const string RoleDelete = "ADMIN.ROLE.DELETE";
    public const string RoleAssignPermissions = "ADMIN.ROLE.ASSIGNPERMISSIONS";

    public const string PermissionView = "ADMIN.PERMISSION.VIEW";

    public const string MenuView = "ADMIN.MENU.VIEW";
    public const string MenuManage = "ADMIN.MENU.MANAGE";

    public const string BranchView = "ADMIN.BRANCH.VIEW";
    public const string BranchManage = "ADMIN.BRANCH.MANAGE";

    public const string DesignationView = "ADMIN.DESIGNATION.VIEW";
    public const string DesignationManage = "ADMIN.DESIGNATION.MANAGE";

    public const string SettingView = "ADMIN.SETTING.VIEW";
    public const string SettingUpdate = "ADMIN.SETTING.UPDATE";
    public const string AbdmAdminManage = "ADMIN.ABDM.MANAGE";

    public const string SessionView = "ADMIN.SESSION.VIEW";
    public const string SessionRevoke = "ADMIN.SESSION.REVOKE";

    public const string AuditView = "ADMIN.AUDIT.VIEW";
    public const string ActivityView = "ADMIN.ACTIVITY.VIEW";

    // ── Masters (Phase 2) ──────────────────────────────────────────────────
    public const string DepartmentView = "MASTER.DEPARTMENT.VIEW";
    public const string DepartmentManage = "MASTER.DEPARTMENT.MANAGE";

    public const string SpecialtyView = "MASTER.SPECIALTY.VIEW";
    public const string SpecialtyManage = "MASTER.SPECIALTY.MANAGE";

    public const string PatientCategoryView = "MASTER.PATIENTCATEGORY.VIEW";
    public const string PatientCategoryManage = "MASTER.PATIENTCATEGORY.MANAGE";

    public const string DoctorView = "MASTER.DOCTOR.VIEW";
    public const string DoctorManage = "MASTER.DOCTOR.MANAGE";

    public const string EmployeeView = "MASTER.EMPLOYEE.VIEW";
    public const string EmployeeManage = "MASTER.EMPLOYEE.MANAGE";

    /// <summary>Covers services and service categories.</summary>
    public const string ServiceView = "MASTER.SERVICE.VIEW";
    public const string ServiceManage = "MASTER.SERVICE.MANAGE";

    public const string TariffView = "MASTER.TARIFF.VIEW";
    public const string TariffManage = "MASTER.TARIFF.MANAGE";
    public const string TariffImport = "MASTER.TARIFF.IMPORT";

    public const string PackageView = "MASTER.PACKAGE.VIEW";
    public const string PackageManage = "MASTER.PACKAGE.MANAGE";

    /// <summary>Covers the building → floor → ward → room → bed hierarchy and room types.</summary>
    public const string LocationView = "MASTER.LOCATION.VIEW";
    public const string LocationManage = "MASTER.LOCATION.MANAGE";

    public const string VendorView = "MASTER.VENDOR.VIEW";
    public const string VendorManage = "MASTER.VENDOR.MANAGE";

    /// <summary>Covers items and item categories.</summary>
    public const string ItemView = "MASTER.ITEM.VIEW";
    public const string ItemManage = "MASTER.ITEM.MANAGE";

    /// <summary>Covers drugs, generics and manufacturers.</summary>
    public const string DrugView = "MASTER.DRUG.VIEW";
    public const string DrugManage = "MASTER.DRUG.MANAGE";

    public const string TaxView = "MASTER.TAX.VIEW";
    public const string TaxManage = "MASTER.TAX.MANAGE";

    public const string DiscountTypeView = "MASTER.DISCOUNTTYPE.VIEW";
    public const string DiscountTypeManage = "MASTER.DISCOUNTTYPE.MANAGE";

    public const string ReferrerView = "MASTER.REFERRER.VIEW";
    public const string ReferrerManage = "MASTER.REFERRER.MANAGE";

    public const string IcdView = "MASTER.ICD.VIEW";
    public const string IcdManage = "MASTER.ICD.MANAGE";
    public const string IcdImport = "MASTER.ICD.IMPORT";

    public const string CorporateView = "MASTER.CORPORATE.VIEW";
    public const string CorporateManage = "MASTER.CORPORATE.MANAGE";

    public const string InsuranceView = "MASTER.INSURANCE.VIEW";
    public const string InsuranceManage = "MASTER.INSURANCE.MANAGE";

    public const string UomView = "MASTER.UOM.VIEW";
    public const string UomManage = "MASTER.UOM.MANAGE";

    public const string SampleTypeView = "MASTER.SAMPLETYPE.VIEW";
    public const string SampleTypeManage = "MASTER.SAMPLETYPE.MANAGE";

    public const string ModalityView = "MASTER.MODALITY.VIEW";
    public const string ModalityManage = "MASTER.MODALITY.MANAGE";

    public const string ConsentTemplateView = "MASTER.CONSENTTEMPLATE.VIEW";
    public const string ConsentTemplateManage = "MASTER.CONSENTTEMPLATE.MANAGE";

    /// <summary>MRD document-type master (Phase 17) — lives with the other masters even though the tables it feeds live in the `mrd` schema.</summary>
    public const string RecordDocumentTypeView = "MASTER.RECORDDOCUMENTTYPE.VIEW";
    public const string RecordDocumentTypeManage = "MASTER.RECORDDOCUMENTTYPE.MANAGE";

    // Front Office masters
    public const string CampView = "MASTER.CAMP.VIEW";
    public const string CampManage = "MASTER.CAMP.MANAGE";
    public const string StateView = "MASTER.STATE.VIEW";
    public const string StateManage = "MASTER.STATE.MANAGE";
    public const string CityView = "MASTER.CITY.VIEW";
    public const string CityManage = "MASTER.CITY.MANAGE";
    public const string NationalityView = "MASTER.NATIONALITY.VIEW";
    public const string NationalityManage = "MASTER.NATIONALITY.MANAGE";
    public const string ReligionView = "MASTER.RELIGION.VIEW";
    public const string ReligionManage = "MASTER.RELIGION.MANAGE";
    public const string LanguageView = "MASTER.LANGUAGE.VIEW";
    public const string LanguageManage = "MASTER.LANGUAGE.MANAGE";
    public const string SpeciallyAbledCategoryView = "MASTER.SPECIALLYABLEDCATEGORY.VIEW";
    public const string SpeciallyAbledCategoryManage = "MASTER.SPECIALLYABLEDCATEGORY.MANAGE";
    public const string PatientHcTagView = "MASTER.PATIENTHCTAG.VIEW";
    public const string PatientHcTagManage = "MASTER.PATIENTHCTAG.MANAGE";
    public const string PatientCaseTypeView = "MASTER.PATIENTCASETYPE.VIEW";
    public const string PatientCaseTypeManage = "MASTER.PATIENTCASETYPE.MANAGE";
    public const string MlcTypeView = "MASTER.MLCTYPE.VIEW";
    public const string MlcTypeManage = "MASTER.MLCTYPE.MANAGE";
    public const string MhcDoctorView = "MASTER.MHCDOCTOR.VIEW";
    public const string MhcDoctorManage = "MASTER.MHCDOCTOR.MANAGE";
    public const string LoyaltyProgramView = "MASTER.LOYALTYPROGRAM.VIEW";
    public const string LoyaltyProgramManage = "MASTER.LOYALTYPROGRAM.MANAGE";
    public const string UnitView = "MASTER.UNIT.VIEW";
    public const string UnitManage = "MASTER.UNIT.MANAGE";
    public const string AppointmentTypeView = "MASTER.APPOINTMENTTYPE.VIEW";
    public const string AppointmentTypeManage = "MASTER.APPOINTMENTTYPE.MANAGE";
    public const string AreaView = "MASTER.AREA.VIEW";
    public const string AreaManage = "MASTER.AREA.MANAGE";
    public const string MhcPackageView = "MASTER.MHCPACKAGE.VIEW";
    public const string MhcPackageManage = "MASTER.MHCPACKAGE.MANAGE";
    public const string LoyaltyCardCategoryView = "MASTER.LOYALTYCARDCATEGORY.VIEW";
    public const string LoyaltyCardCategoryManage = "MASTER.LOYALTYCARDCATEGORY.MANAGE";
    public const string MemberRelationTagView = "MASTER.MEMBERRELATIONTAG.VIEW";
    public const string MemberRelationTagManage = "MASTER.MEMBERRELATIONTAG.MANAGE";
    public const string LoyaltyCardTypeView = "MASTER.LOYALTYCARDTYPE.VIEW";
    public const string LoyaltyCardTypeManage = "MASTER.LOYALTYCARDTYPE.MANAGE";
    public const string LoyaltyCardPriceView = "MASTER.LOYALTYCARDPRICE.VIEW";
    public const string LoyaltyCardPriceManage = "MASTER.LOYALTYCARDPRICE.MANAGE";

    /// <summary>"Bill Estimation" template master (Batch 2 of the billing extension).</summary>
    public const string BillEstimateTemplateView = "MASTER.BILLESTIMATETEMPLATE.VIEW";
    public const string BillEstimateTemplateManage = "MASTER.BILLESTIMATETEMPLATE.MANAGE";

    /// <summary>"Doctor Header Payout Definition" master (Batch 2 of the billing extension).</summary>
    public const string DoctorPayoutRuleView = "MASTER.DOCTORPAYOUTRULE.VIEW";
    public const string DoctorPayoutRuleManage = "MASTER.DOCTORPAYOUTRULE.MANAGE";

    // ── Patients (Phase 3) ─────────────────────────────────────────────────
    public const string PatientView = "PATIENT.PATIENT.VIEW";
    public const string PatientCreate = "PATIENT.PATIENT.CREATE";
    public const string PatientUpdate = "PATIENT.PATIENT.UPDATE";
    public const string PatientMerge = "PATIENT.PATIENT.MERGE";
    public const string PatientBlock = "PATIENT.PATIENT.BLOCK";
    /// <summary>ABDM/ABHA (Milestone M1): view a patient's linked ABHA.</summary>
    public const string AbhaView = "PATIENT.ABHA.VIEW";
    /// <summary>ABDM/ABHA (Milestone M1): link/create/unlink a patient's ABHA via ABDM.</summary>
    public const string AbhaManage = "PATIENT.ABHA.MANAGE";
    /// <summary>ABDM HIP (Milestone M2): manage care context linking and health data sharing.</summary>
    public const string AbdmHipManage = "ABDM.HIP.MANAGE";
    /// <summary>ABDM HIP (Milestone M2): view care context links and data transfer history.</summary>
    public const string AbdmHipView = "ABDM.HIP.VIEW";
    /// <summary>ABDM HIU (Milestone M3): create and manage consent requests to fetch external records.</summary>
    public const string AbdmHiuManage = "ABDM.HIU.MANAGE";
    /// <summary>ABDM HIU (Milestone M3): view external health records fetched via ABDM.</summary>
    public const string AbdmHiuView = "ABDM.HIU.VIEW";

    // ── Appointments (Phase 4) ─────────────────────────────────────────────
    public const string ScheduleView = "APPOINTMENT.SCHEDULE.VIEW";
    public const string ScheduleManage = "APPOINTMENT.SCHEDULE.MANAGE";
    /// <summary>"Schedule Unit - Day Wise" — the Unit analogue of Doctor Schedule, same VIEW/MANAGE split.</summary>
    public const string AppointmentUnitScheduleView = "APPOINTMENT.UNITSCHEDULE.VIEW";
    public const string AppointmentUnitScheduleManage = "APPOINTMENT.UNITSCHEDULE.MANAGE";

    public const string AppointmentView = "APPOINTMENT.APPOINTMENT.VIEW";
    public const string AppointmentBook = "APPOINTMENT.APPOINTMENT.BOOK";
    public const string AppointmentReschedule = "APPOINTMENT.APPOINTMENT.RESCHEDULE";
    public const string AppointmentCancel = "APPOINTMENT.APPOINTMENT.CANCEL";
    public const string AppointmentCheckIn = "APPOINTMENT.APPOINTMENT.CHECKIN";
    /// <summary>"Skip Token" — patient not present when called; also covers the paired "recall" action.</summary>
    public const string AppointmentSkip = "APPOINTMENT.APPOINTMENT.SKIP";
    /// <summary>"Token Transfer" — reassign a checked-in/scheduled token to a different doctor same day.</summary>
    public const string AppointmentTransfer = "APPOINTMENT.APPOINTMENT.TRANSFER";

    public const string QueueView = "APPOINTMENT.QUEUE.VIEW";

    // ── OPD (Phase 5) ────────────────────────────────────────────────────────
    public const string VisitView = "OPD.VISIT.VIEW";
    public const string VisitCreate = "OPD.VISIT.CREATE";
    /// <summary>Covers every clinical entry on the workspace: vitals, complaints, examination,
    /// diagnosis, prescription, orders, referral, follow-up, start-consult/complete/cancel.</summary>
    public const string VisitManage = "OPD.VISIT.MANAGE";
    /// <summary>"Camp Bulk Import" — CSV of UHIDs registered in bulk against one camp/doctor for the whole batch. Split out from VisitCreate since a bad file can misregister many patients at once, same posture as MASTER.ICD.IMPORT.</summary>
    public const string VisitImport = "OPD.VISIT.IMPORT";
    public const string TemplateView = "OPD.TEMPLATE.VIEW";
    public const string TemplateManage = "OPD.TEMPLATE.MANAGE";

    // ── MHC Registration (front-office extension of OPD's visit engine) ────
    public const string MhcVisitView = "MHC.VISIT.VIEW";
    public const string MhcVisitCreate = "MHC.VISIT.CREATE";
    /// <summary>Covers cancel — the only non-clinical status change on an MHC visit (no
    /// consultation workspace exists for MHC in this pass).</summary>
    public const string MhcVisitManage = "MHC.VISIT.MANAGE";
    /// <summary>"MHC Bulk Import" — CSV of UHIDs registered in bulk against one MHC doctor/package for the whole batch.</summary>
    public const string MhcVisitImport = "MHC.VISIT.IMPORT";

    // ── Loyalty Program engine (front-office extension, genuinely new module) ──
    public const string LoyaltyCardView = "LOYALTY.CARD.VIEW";
    public const string LoyaltyCardCreate = "LOYALTY.CARD.CREATE";
    /// <summary>Covers renew/upgrade, add/remove dependent, and duplicate issue — routine
    /// front-desk operations, same one-permission-per-module-action granularity as
    /// OPD.VISIT.MANAGE.</summary>
    public const string LoyaltyCardManage = "LOYALTY.CARD.MANAGE";
    /// <summary>Separate from Manage — ending a membership is more consequential than the
    /// routine actions above, mirrors AdmissionCancel being its own permission.</summary>
    public const string LoyaltyCardDeactivate = "LOYALTY.CARD.DEACTIVATE";

    // ── Billing (Phase 6) ──────────────────────────────────────────────────
    public const string BillView = "BILLING.BILL.VIEW";
    public const string BillCreate = "BILLING.BILL.CREATE";
    public const string BillCreateOp = "BILLING.BILL.CREATEOP";
    public const string BillCreateIp = "BILLING.BILL.CREATEIP";
    public const string BillCreateEmergency = "BILLING.BILL.CREATEEMERGENCY";
    /// <summary>Covers editing line items and applying a discount up to its auto-approve threshold.</summary>
    public const string BillManage = "BILLING.BILL.MANAGE";
    public const string BillCancel = "BILLING.BILL.CANCEL";

    /// <summary>
    /// Bill-type visibility scoping (RBAC customization: "show only the OP report"). Checked
    /// in-service (BillingService/BillingReportService), not by [HasPermission] — a role's
    /// BILLING.BILL.VIEW still gates the Bills/Reports feature as a whole; these three narrow
    /// *which BillType rows* that role's queries return, and the frontend hides the Bill Type
    /// filter down to whichever of these a role/user actually holds (one held = no choice to
    /// make, so the filter itself disappears). Seeded onto every existing bill-viewing role so
    /// current behavior (see every type) doesn't regress — an admin narrows a specific
    /// role/user by removing one of these three, same as any other permission.
    /// </summary>
    public const string BillViewOp = "BILLING.BILL.VIEWOP";
    public const string BillViewIp = "BILLING.BILL.VIEWIP";
    public const string BillViewEmergency = "BILLING.BILL.VIEWEMERGENCY";

    public const string PaymentCollect = "BILLING.PAYMENT.COLLECT";

    public const string RefundRequest = "BILLING.REFUND.REQUEST";
    public const string RefundApprove = "BILLING.REFUND.APPROVE";
    public const string RefundIssue = "BILLING.REFUND.ISSUE";

    public const string DiscountApprove = "BILLING.DISCOUNT.APPROVE";

    public const string AdvanceView = "BILLING.ADVANCE.VIEW";
    public const string AdvanceCollect = "BILLING.ADVANCE.COLLECT";
    public const string AdvanceTransfer = "BILLING.ADVANCE.TRANSFER";

    public const string DayCloseView = "BILLING.DAYCLOSE.VIEW";
    public const string DayCloseClose = "BILLING.DAYCLOSE.CLOSE";

    public const string PaymentModeView = "BILLING.PAYMENTMODE.VIEW";
    public const string PaymentModeManage = "BILLING.PAYMENTMODE.MANAGE";

    // ── Billing Extension — Full-Fledged Billing ────────────────────────────
    public const string AdvanceDraw = "BILLING.ADVANCE.DRAW";
    public const string BillNoteManage = "BILLING.NOTE.MANAGE";
    public const string CreditNoteView = "BILLING.CREDITNOTE.VIEW";
    public const string CreditNoteCreate = "BILLING.CREDITNOTE.CREATE";
    public const string CreditNoteApprove = "BILLING.CREDITNOTE.APPROVE";
    public const string BillPark = "BILLING.BILL.PARK";
    public const string BillParkApprove = "BILLING.BILL.PARKAPPROVE";

    public const string CashierShiftView = "BILLING.CASHIERSHIFT.VIEW";
    /// <summary>Covers opening and closing (declaring amounts on) my own shift.</summary>
    public const string CashierShiftManage = "BILLING.CASHIERSHIFT.MANAGE";
    /// <summary>Supervisor action: confirm physical cash handed over from a closed shift.</summary>
    public const string CashierShiftReceive = "BILLING.CASHIERSHIFT.RECEIVE";

    public const string BillEstimateView = "BILLING.ESTIMATE.VIEW";
    /// <summary>Covers create, copy and print — an estimate has no approval workflow, it's informational only.</summary>
    public const string BillEstimateCreate = "BILLING.ESTIMATE.CREATE";

    /// <summary>Covers every BillingReportService report (ageing, ward-wise outstanding, day book, cancelled bills, concession, refund, advance, tariff list, doctors payout, user settlement, service-category breakup, IP service billing detail).</summary>
    public const string BillingReportView = "BILLING.REPORT.VIEW";

    /// <summary>
    /// The staff-side worklist over the Portal's "Pay Online" outbox (billing.online_payment_requests —
    /// same no-op-until-a-gateway posture as NotificationQueue). Added so the portal's "our billing
    /// team will contact you" promise has somewhere for staff to actually see and follow up on.
    /// </summary>
    public const string OnlinePaymentRequestView = "BILLING.ONLINEPAYMENT.VIEW";
    /// <summary>Mark a request as followed-up (Completed, after collecting payment the normal way on the bill) or Cancelled.</summary>
    public const string OnlinePaymentRequestManage = "BILLING.ONLINEPAYMENT.MANAGE";

    // ── Laboratory (Phase 7) ─────────────────────────────────────────────────
    public const string LabTestView = "LAB.TEST.VIEW";
    /// <summary>Covers the test catalog plus its parameters and reference ranges.</summary>
    public const string LabTestManage = "LAB.TEST.MANAGE";
    public const string LabProfileView = "LAB.PROFILE.VIEW";
    public const string LabProfileManage = "LAB.PROFILE.MANAGE";

    public const string LabOrderView = "LAB.ORDER.VIEW";
    public const string LabOrderCreate = "LAB.ORDER.CREATE";
    public const string LabOrderCancel = "LAB.ORDER.CANCEL";

    public const string LabSampleCollect = "LAB.SAMPLE.COLLECT";
    public const string LabSampleReceive = "LAB.SAMPLE.RECEIVE";
    public const string LabSampleReject = "LAB.SAMPLE.REJECT";

    public const string LabResultCreate = "LAB.RESULT.CREATE";
    public const string LabResultVerify = "LAB.RESULT.VERIFY";
    public const string LabResultApprove = "LAB.RESULT.APPROVE";

    public const string LabAlertAcknowledge = "LAB.ALERT.ACKNOWLEDGE";
    public const string LabReportView = "LAB.REPORT.VIEW";

    // ── Pharmacy (Phase 8) ───────────────────────────────────────────────────
    public const string PharmacyStoreView = "PHARMACY.STORE.VIEW";
    public const string PharmacyStoreManage = "PHARMACY.STORE.MANAGE";

    public const string PurchaseRequestView = "PHARMACY.PURCHASEREQUEST.VIEW";
    public const string PurchaseRequestCreate = "PHARMACY.PURCHASEREQUEST.CREATE";
    public const string PurchaseRequestApprove = "PHARMACY.PURCHASEREQUEST.APPROVE";

    public const string PurchaseOrderView = "PHARMACY.PURCHASEORDER.VIEW";
    public const string PurchaseOrderCreate = "PHARMACY.PURCHASEORDER.CREATE";
    public const string PurchaseOrderApprove = "PHARMACY.PURCHASEORDER.APPROVE";

    public const string GrnView = "PHARMACY.GRN.VIEW";
    /// <summary>Covers both drafting and posting a GRN — posting is what writes stock.</summary>
    public const string GrnCreate = "PHARMACY.GRN.CREATE";

    public const string PurchaseReturnView = "PHARMACY.PURCHASERETURN.VIEW";
    public const string PurchaseReturnCreate = "PHARMACY.PURCHASERETURN.CREATE";

    public const string PharmacySaleView = "PHARMACY.SALE.VIEW";
    public const string PharmacySaleCreate = "PHARMACY.SALE.CREATE";
    public const string PharmacySaleCancel = "PHARMACY.SALE.CANCEL";
    /// <summary>"Pharmacy Pay Type Modify" — correct a completed sale's payment mode.</summary>
    public const string PharmacySaleModify = "PHARMACY.SALE.MODIFY";

    public const string SalesReturnView = "PHARMACY.SALESRETURN.VIEW";
    public const string SalesReturnCreate = "PHARMACY.SALESRETURN.CREATE";

    public const string IssueView = "PHARMACY.ISSUE.VIEW";
    public const string IssueCreate = "PHARMACY.ISSUE.CREATE";
    public const string IssueReturnCreate = "PHARMACY.ISSUERETURN.CREATE";

    public const string StockView = "PHARMACY.STOCK.VIEW";
    public const string StockAdjust = "PHARMACY.STOCK.ADJUST";
    public const string StockApprove = "PHARMACY.STOCK.APPROVE";
    public const string StockTransfer = "PHARMACY.STOCK.TRANSFER";
    public const string StockReceive = "PHARMACY.STOCK.RECEIVE";
    public const string StockAudit = "PHARMACY.STOCK.AUDIT";

    public const string PharmacyReportView = "PHARMACY.REPORT.VIEW";

    // ── Stores & Inventory (Phase 13) ────────────────────────────────────────
    public const string IndentView = "PHARMACY.INDENT.VIEW";
    public const string IndentCreate = "PHARMACY.INDENT.CREATE";
    public const string IndentApprove = "PHARMACY.INDENT.APPROVE";

    // ── IPD (Phase 9) ─────────────────────────────────────────────────────────
    public const string AdmissionView = "IPD.ADMISSION.VIEW";
    public const string AdmissionCreate = "IPD.ADMISSION.CREATE";
    public const string AdmissionCancel = "IPD.ADMISSION.CANCEL";
    public const string AdmissionRequestView = "IPD.ADMISSIONREQUEST.VIEW";
    public const string AdmissionRequestCreate = "IPD.ADMISSIONREQUEST.CREATE";
    public const string AdmissionRequestDecide = "IPD.ADMISSIONREQUEST.DECIDE";

    public const string BedView = "IPD.BED.VIEW";
    /// <summary>Covers bed allocation, ward/room/bed transfer and manual status toggles (reserved/cleaning/blocked).</summary>
    public const string BedManage = "IPD.BED.MANAGE";

    public const string ConsultantManage = "IPD.CONSULTANT.MANAGE";

    public const string DoctorOrderView = "IPD.ORDER.VIEW";
    public const string DoctorOrderCreate = "IPD.ORDER.CREATE";
    public const string DoctorOrderCancel = "IPD.ORDER.CANCEL";

    /// <summary>Covers progress notes, vitals chart, intake/output chart and procedure notes.</summary>
    public const string ChartView = "IPD.CHART.VIEW";
    public const string ChartManage = "IPD.CHART.MANAGE";

    public const string DischargeRequest = "IPD.DISCHARGE.REQUEST";
    public const string DischargeClear = "IPD.DISCHARGE.CLEAR";
    /// <summary>May act on the discharge clearance checklist at all (endpoint gate); the per-department codes below decide which item.</summary>
    public const string DischargeClearItem = "IPD.DISCHARGE.CLEARITEM";
    public const string DischargeClearBilling = "IPD.DISCHARGECLEAR.BILLING";
    public const string DischargeClearPharmacy = "IPD.DISCHARGECLEAR.PHARMACY";
    public const string DischargeClearNursing = "IPD.DISCHARGECLEAR.NURSING";
    public const string DischargeClearInvestigation = "IPD.DISCHARGECLEAR.INVESTIGATION";
    public const string DischargeApproveSummary = "IPD.DISCHARGE.APPROVESUMMARY";
    public const string DischargeFinalize = "IPD.DISCHARGE.FINALIZE";

    // ── Nursing (Phase 10) ───────────────────────────────────────────────────
    /// <summary>Covers nursing assessments, shift notes, risk scores (Pain/Fall/Bed Sore), care plans and transfer notes.</summary>
    public const string NursingChartView = "NURSING.CHART.VIEW";
    public const string NursingChartManage = "NURSING.CHART.MANAGE";

    public const string MarView = "NURSING.MAR.VIEW";
    public const string MarAdminister = "NURSING.MAR.ADMINISTER";

    public const string HandoverView = "NURSING.HANDOVER.VIEW";
    public const string HandoverManage = "NURSING.HANDOVER.MANAGE";

    public const string IncidentView = "NURSING.INCIDENT.VIEW";
    public const string IncidentCreate = "NURSING.INCIDENT.CREATE";
    public const string IncidentReview = "NURSING.INCIDENT.REVIEW";

    // ── EMR (Phase 11) ───────────────────────────────────────────────────────
    /// <summary>Covers the unified timeline and every read-only history view (prescriptions, investigations, procedures, discharge summary archive, results trend).</summary>
    public const string EmrTimelineView = "EMR.TIMELINE.VIEW";
    /// <summary>Covers medical/surgical/family/allergy/immunization histories.</summary>
    public const string EmrHistoryView = "EMR.HISTORY.VIEW";
    public const string EmrHistoryManage = "EMR.HISTORY.MANAGE";
    public const string EmrProblemListView = "EMR.PROBLEMLIST.VIEW";
    public const string EmrProblemListManage = "EMR.PROBLEMLIST.MANAGE";
    public const string EmrNoteView = "EMR.NOTE.VIEW";
    public const string EmrNoteManage = "EMR.NOTE.MANAGE";
    public const string EmrAttachmentView = "EMR.ATTACHMENT.VIEW";
    public const string EmrAttachmentManage = "EMR.ATTACHMENT.MANAGE";

    // ── Radiology (Phase 12) ─────────────────────────────────────────────────
    public const string RadTestView = "RAD.TEST.VIEW";
    public const string RadTestManage = "RAD.TEST.MANAGE";
    public const string RadTemplateView = "RAD.TEMPLATE.VIEW";
    public const string RadTemplateManage = "RAD.TEMPLATE.MANAGE";

    public const string RadOrderView = "RAD.ORDER.VIEW";
    public const string RadOrderCreate = "RAD.ORDER.CREATE";
    public const string RadOrderCancel = "RAD.ORDER.CANCEL";

    /// <summary>Covers the scheduling worklist and scheduling a test.</summary>
    public const string RadScheduleManage = "RAD.SCHEDULE.MANAGE";
    /// <summary>Covers the technician image-capture worklist and marking a test captured.</summary>
    public const string RadCaptureManage = "RAD.CAPTURE.MANAGE";
    public const string RadContrastConsentCapture = "RAD.CONTRASTCONSENT.CAPTURE";

    public const string RadReportCreate = "RAD.REPORT.CREATE";
    public const string RadReportVerify = "RAD.REPORT.VERIFY";
    public const string RadReportApprove = "RAD.REPORT.APPROVE";
    public const string RadReportView = "RAD.REPORT.VIEW";

    // ── Operation Theatre (Phase 14) ─────────────────────────────────────────
    public const string OtTheatreView = "OT.THEATRE.VIEW";
    public const string OtTheatreManage = "OT.THEATRE.MANAGE";

    public const string OtBookingView = "OT.BOOKING.VIEW";
    public const string OtBookingCreate = "OT.BOOKING.CREATE";
    /// <summary>Covers reschedule/cancel/postpone — every non-clinical status change on a booking.</summary>
    public const string OtBookingManage = "OT.BOOKING.MANAGE";
    /// <summary>Covers Start/Complete (the clinical status transitions gated on the checklist/consent).</summary>
    public const string OtSurgeryConduct = "OT.SURGERY.CONDUCT";

    public const string OtChecklistManage = "OT.CHECKLIST.MANAGE";
    public const string OtAnaesthesiaNoteManage = "OT.ANAESTHESIANOTE.MANAGE";
    public const string OtOperativeNoteManage = "OT.OPERATIVENOTE.MANAGE";
    public const string OtUsageRecord = "OT.USAGE.RECORD";
    public const string OtRegisterView = "OT.REGISTER.VIEW";

    // ── ICU / Critical Care (Phase 15) ───────────────────────────────────────
    public const string IcuRuleView = "ICU.RULE.VIEW";
    public const string IcuRuleManage = "ICU.RULE.MANAGE";
    public const string IcuVitalsRecord = "ICU.VITALS.RECORD";
    public const string IcuAlertView = "ICU.ALERT.VIEW";
    public const string IcuAlertAcknowledge = "ICU.ALERT.ACKNOWLEDGE";
    public const string IcuChartView = "ICU.CHART.VIEW";
    /// <summary>Covers ventilator/infusion session start/stop/observations and severity assessment.</summary>
    public const string IcuChartManage = "ICU.CHART.MANAGE";
    public const string IcuDashboardView = "ICU.DASHBOARD.VIEW";

    // ── Emergency / Casualty (Phase 16) ──────────────────────────────────────
    public const string EmergencyVisitView = "EMERGENCY.VISIT.VIEW";
    public const string EmergencyVisitCreate = "EMERGENCY.VISIT.CREATE";
    /// <summary>Covers cancel — the only non-clinical status change on a visit.</summary>
    public const string EmergencyVisitManage = "EMERGENCY.VISIT.MANAGE";
    public const string EmergencyTriage = "EMERGENCY.TRIAGE.RECORD";
    public const string EmergencyAssessmentManage = "EMERGENCY.ASSESSMENT.MANAGE";
    public const string EmergencyObservationManage = "EMERGENCY.OBSERVATION.MANAGE";
    public const string EmergencyMlcManage = "EMERGENCY.MLC.MANAGE";

    // ── MRD (Phase 17) ────────────────────────────────────────────────────────
    public const string MrdFileView = "MRD.FILE.VIEW";
    public const string MrdFileCreate = "MRD.FILE.CREATE";
    /// <summary>Covers recording a movement (location/custodian change).</summary>
    public const string MrdFileManage = "MRD.FILE.MANAGE";
    /// <summary>Covers approve/reject/issue/return on a file request.</summary>
    public const string MrdFileIssue = "MRD.FILE.ISSUE";
    public const string MrdRequestCreate = "MRD.REQUEST.CREATE";

    public const string MrdDocumentView = "MRD.DOCUMENT.VIEW";
    public const string MrdDocumentUpload = "MRD.DOCUMENT.UPLOAD";

    public const string MrdCodingView = "MRD.CODING.VIEW";
    public const string MrdCodingManage = "MRD.CODING.MANAGE";

    public const string MrdAuditView = "MRD.AUDIT.VIEW";
    public const string MrdAuditManage = "MRD.AUDIT.MANAGE";

    public const string MrdRetentionView = "MRD.RETENTION.VIEW";
    public const string MrdRetentionManage = "MRD.RETENTION.MANAGE";

    public const string MrdDestructionView = "MRD.DESTRUCTION.VIEW";
    public const string MrdDestructionManage = "MRD.DESTRUCTION.MANAGE";

    public const string MrdReportView = "MRD.REPORT.VIEW";

    // ── Finance & Accounts (Phase 18) ────────────────────────────────────────
    public const string FinanceAccountView = "FINANCE.ACCOUNT.VIEW";
    public const string FinanceAccountManage = "FINANCE.ACCOUNT.MANAGE";

    public const string FinanceVoucherView = "FINANCE.VOUCHER.VIEW";
    public const string FinanceVoucherCreate = "FINANCE.VOUCHER.CREATE";
    /// <summary>Covers cancelling a posted voucher (creates a reversal).</summary>
    public const string FinanceVoucherCancel = "FINANCE.VOUCHER.CANCEL";

    public const string FinanceLedgerView = "FINANCE.LEDGER.VIEW";

    /// <summary>Covers viewing payables, the pending-GRN worklist, and aging.</summary>
    public const string FinancePayableView = "FINANCE.PAYABLE.VIEW";
    /// <summary>Covers raising a payable from a GRN and recording a payment against it.</summary>
    public const string FinancePayableManage = "FINANCE.PAYABLE.MANAGE";

    /// <summary>Covers viewing receivables, the pending-bill worklist, and aging.</summary>
    public const string FinanceReceivableView = "FINANCE.RECEIVABLE.VIEW";
    /// <summary>Covers raising a receivable from a credit bill and recording a collection against it.</summary>
    public const string FinanceReceivableManage = "FINANCE.RECEIVABLE.MANAGE";

    public const string FinanceReportView = "FINANCE.REPORT.VIEW";
    /// <summary>The centralized (cross-branch) day-close console — distinct from BILLING.DAYCLOSE.VIEW's single-branch view.</summary>
    public const string FinanceDayCloseConsoleView = "FINANCE.DAYCLOSECONSOLE.VIEW";

    // ── Biomedical Engineering (Phase 19) ────────────────────────────────────
    public const string BiomedEquipmentView = "BIOMED.EQUIPMENT.VIEW";
    /// <summary>Covers create/update and recording a location movement.</summary>
    public const string BiomedEquipmentManage = "BIOMED.EQUIPMENT.MANAGE";

    public const string BiomedContractView = "BIOMED.CONTRACT.VIEW";
    public const string BiomedContractManage = "BIOMED.CONTRACT.MANAGE";

    public const string BiomedPmView = "BIOMED.PM.VIEW";
    /// <summary>Covers creating/updating a PM schedule and generating due work orders.</summary>
    public const string BiomedPmManage = "BIOMED.PM.MANAGE";

    public const string BiomedWorkOrderView = "BIOMED.WORKORDER.VIEW";
    /// <summary>Covers assign/start/complete/cancel and recording spare usage.</summary>
    public const string BiomedWorkOrderManage = "BIOMED.WORKORDER.MANAGE";

    public const string BiomedComplaintView = "BIOMED.COMPLAINT.VIEW";
    public const string BiomedComplaintCreate = "BIOMED.COMPLAINT.CREATE";
    /// <summary>Covers assign/resolve/close.</summary>
    public const string BiomedComplaintManage = "BIOMED.COMPLAINT.MANAGE";

    public const string BiomedCalibrationView = "BIOMED.CALIBRATION.VIEW";
    public const string BiomedCalibrationManage = "BIOMED.CALIBRATION.MANAGE";

    public const string BiomedReportView = "BIOMED.REPORT.VIEW";

    // ── Quality / NABH (Phase 20) ─────────────────────────────────────────────
    public const string QualityIndicatorView = "QUALITY.INDICATOR.VIEW";
    public const string QualityIndicatorManage = "QUALITY.INDICATOR.MANAGE";
    public const string QualityIndicatorEntryRecord = "QUALITY.INDICATORENTRY.RECORD";

    public const string QualityAuditTemplateView = "QUALITY.AUDITTEMPLATE.VIEW";
    public const string QualityAuditTemplateManage = "QUALITY.AUDITTEMPLATE.MANAGE";
    public const string QualityAuditView = "QUALITY.AUDIT.VIEW";
    /// <summary>Covers schedule/score/complete.</summary>
    public const string QualityAuditManage = "QUALITY.AUDIT.MANAGE";

    /// <summary>Record root-cause analysis and the sentinel-event flag on an existing (Nursing) incident — the widened Incident entity's own View/Create/Review permissions are unchanged.</summary>
    public const string QualityIncidentRca = "QUALITY.INCIDENT.RCA";

    public const string QualityNonConformanceView = "QUALITY.NONCONFORMANCE.VIEW";
    public const string QualityNonConformanceCreate = "QUALITY.NONCONFORMANCE.CREATE";
    /// <summary>Covers moving to In Progress and closing.</summary>
    public const string QualityNonConformanceManage = "QUALITY.NONCONFORMANCE.MANAGE";

    public const string QualityCapaView = "QUALITY.CAPA.VIEW";
    /// <summary>Covers raise/complete/verify/close.</summary>
    public const string QualityCapaManage = "QUALITY.CAPA.MANAGE";

    public const string QualityRiskView = "QUALITY.RISK.VIEW";
    public const string QualityRiskManage = "QUALITY.RISK.MANAGE";

    /// <summary>Covers the committee master, meeting records/action items, and the NABH standards master (a small reference table administered alongside committees, not worth its own permission code).</summary>
    public const string QualityCommitteeView = "QUALITY.COMMITTEE.VIEW";
    public const string QualityCommitteeManage = "QUALITY.COMMITTEE.MANAGE";

    public const string QualityEvidenceView = "QUALITY.EVIDENCE.VIEW";
    public const string QualityEvidenceUpload = "QUALITY.EVIDENCE.UPLOAD";

    /// <summary>Covers both the KPI (indicator trend) and compliance dashboards.</summary>
    public const string QualityDashboardView = "QUALITY.DASHBOARD.VIEW";

    // ── Doctor Portal (Phase 22) ─────────────────────────────────────────────
    // Every endpoint here resolves "my doctor profile" from the caller's own UserId —
    // these permissions gate access to the doctor-portal views themselves; the underlying
    // reused services (OpVisitService, DischargeService, EMR TimelineService) enforce their
    // own permissions unchanged. Approving a discharge summary reuses IPD.DISCHARGE.APPROVESUMMARY,
    // not a new code, since it's the exact same action IPD's staff screen already gates.
    public const string DoctorPortalDashboardView = "DOCTORPORTAL.DASHBOARD.VIEW";
    public const string DoctorPortalQueueView = "DOCTORPORTAL.QUEUE.VIEW";
    public const string DoctorPortalIpRoundsView = "DOCTORPORTAL.IPROUNDS.VIEW";
    public const string DoctorPortalOrdersView = "DOCTORPORTAL.ORDERS.VIEW";
    public const string DoctorPortalDischargeView = "DOCTORPORTAL.DISCHARGE.VIEW";
    /// <summary>"Doctor Opinion Entry" inbox — the write action reuses OPD.VISIT.MANAGE (already
    /// granted alongside DOCTOR_PORTAL via the base DOCTOR role), same posture as the discharge
    /// e-sign action reusing IPD.DISCHARGE.APPROVESUMMARY.</summary>
    public const string DoctorPortalOpinionView = "DOCTORPORTAL.OPINION.VIEW";

    // ── Cross-cutting reporting (plan §9/§10 example) ────────────────────────
    /// <summary>Gates every IgnoreQueryFilters() cross-branch read — first used by Finance's cross-branch day-close console and reports.</summary>
    public const string ReportCrossBranchView = "REPORT.CROSSBRANCH.VIEW";

    /// <summary>The generic "Dashboard" home/landing screen (path "/") — granted to every role by default (see DefaultRoleDefinitions), explicitly withheld only where a role has its own dedicated landing view instead (Front Office).</summary>
    public const string DashboardHomeView = "ADMIN.DASHBOARD.VIEW";

    // ── Reports & Dashboards (Phase 23) ──────────────────────────────────────
    public const string DashboardManagementView = "DASHBOARD.MANAGEMENT.VIEW";
    public const string DashboardOpView = "DASHBOARD.OP.VIEW";
    public const string DashboardIpView = "DASHBOARD.IP.VIEW";
    public const string DashboardBillingView = "DASHBOARD.BILLING.VIEW";
    public const string DashboardRevenueView = "DASHBOARD.REVENUE.VIEW";
    /// <summary>Custom Report Builder — view the catalog and run an executable report_definitions row (each row's own PermissionCode is also checked at execution time).</summary>
    public const string ReportCatalogView = "REPORT.CATALOG.VIEW";
    /// <summary>Report subscription placeholder — create/delete my own subscription rows (plan §23: "schedule + outbox").</summary>
    public const string ReportSubscriptionManage = "REPORT.SUBSCRIPTION.MANAGE";
    /// <summary>OPIP Census Report — department-wise New/Review×Gender/Child counts blending OpVisit and Admission, plus the patient-wise detail listing behind it. Front Office's own front-desk census, distinct from DASHBOARD.OP/IP.VIEW's day-granular materialized-view dashboards.</summary>
    public const string ReportCensusView = "REPORT.CENSUS.VIEW";

    // ── Registration gap-fill masters (Phase 24) ─────────────────────────────
    public const string SalutationView = "MASTER.SALUTATION.VIEW";
    public const string SalutationManage = "MASTER.SALUTATION.MANAGE";
    public const string MaritalStatusView = "MASTER.MARITALSTATUS.VIEW";
    public const string MaritalStatusManage = "MASTER.MARITALSTATUS.MANAGE";
    public const string IdentityDocumentTypeView = "MASTER.IDENTITYDOCUMENTTYPE.VIEW";
    public const string IdentityDocumentTypeManage = "MASTER.IDENTITYDOCUMENTTYPE.MANAGE";
    public const string VisitClassView = "MASTER.VISITCLASS.VIEW";
    public const string VisitClassManage = "MASTER.VISITCLASS.MANAGE";
    public const string VisitSessionView = "MASTER.VISITSESSION.VIEW";
    public const string VisitSessionManage = "MASTER.VISITSESSION.MANAGE";
    public const string ClaimDocumentTypeView = "MASTER.CLAIMDOCUMENTTYPE.VIEW";
    public const string ClaimDocumentTypeManage = "MASTER.CLAIMDOCUMENTTYPE.MANAGE";
    public const string ClaimDenialReasonView = "MASTER.CLAIMDENIALREASON.VIEW";
    public const string ClaimDenialReasonManage = "MASTER.CLAIMDENIALREASON.MANAGE";

    // ── Insurance / TPA (Phase 24) ────────────────────────────────────────────
    public const string InsurancePolicyView = "INSURANCE.POLICY.VIEW";
    /// <summary>Policies are captured inline on Patient registration/update; this covers viewing/managing them there and via the standalone lookup.</summary>
    public const string InsurancePolicyManage = "INSURANCE.POLICY.MANAGE";
    public const string InsurancePreAuthView = "INSURANCE.PREAUTH.VIEW";
    public const string InsurancePreAuthCreate = "INSURANCE.PREAUTH.CREATE";
    /// <summary>Covers approve/reject of both the pre-auth request itself and its enhancement requests.</summary>
    public const string InsurancePreAuthApprove = "INSURANCE.PREAUTH.APPROVE";
    public const string InsuranceClaimView = "INSURANCE.CLAIM.VIEW";
    public const string InsuranceClaimCreate = "INSURANCE.CLAIM.CREATE";
    /// <summary>Covers moving a claim through Submitted/UnderReview/PartiallyApproved/Approved/Denied and attaching documents.</summary>
    public const string InsuranceClaimManage = "INSURANCE.CLAIM.MANAGE";
    public const string InsuranceClaimSettle = "INSURANCE.CLAIM.SETTLE";

    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        new(UserView, "View users"),
        new(UserCreate, "Create users"),
        new(UserUpdate, "Update users"),
        new(UserDelete, "Deactivate/delete users"),
        new(UserResetPassword, "Reset another user's password"),
        new(UserManagePermissions, "Grant/deny individual permissions on a specific user, on top of their roles"),

        new(RoleView, "View roles"),
        new(RoleCreate, "Create roles"),
        new(RoleUpdate, "Update roles"),
        new(RoleDelete, "Delete roles"),
        new(RoleAssignPermissions, "Change a role's permissions"),

        new(PermissionView, "View the permission catalogue"),

        new(MenuView, "View menu configuration"),
        new(MenuManage, "Create/update/delete menus"),

        new(BranchView, "View branches"),
        new(BranchManage, "Create/update branches"),

        new(DesignationView, "View designations"),
        new(DesignationManage, "Create/update designations"),

        new(SettingView, "View system settings"),
        new(SettingUpdate, "Update system settings"),
        new(AbdmAdminManage, "Configure ABDM integration and toggle milestones (M1/M2/M3)"),

        new(SessionView, "View active sessions"),
        new(SessionRevoke, "Force-logout sessions"),

        new(AuditView, "View entity audit log"),
        new(ActivityView, "View activity log"),

        new(DepartmentView, "View departments"),
        new(DepartmentManage, "Create/update departments"),
        new(SpecialtyView, "View specialties"),
        new(SpecialtyManage, "Create/update specialties"),
        new(PatientCategoryView, "View patient categories"),
        new(PatientCategoryManage, "Create/update patient categories"),
        new(DoctorView, "View doctor profiles"),
        new(DoctorManage, "Create/update doctor profiles"),
        new(EmployeeView, "View employees"),
        new(EmployeeManage, "Create/update employees"),
        new(ServiceView, "View services and service categories"),
        new(ServiceManage, "Create/update services and service categories"),
        new(TariffView, "View tariffs"),
        new(TariffManage, "Create/update tariffs"),
        new(TariffImport, "Bulk-import tariffs from CSV"),
        new(PackageView, "View packages"),
        new(PackageManage, "Create/update packages"),
        new(LocationView, "View buildings, floors, wards, rooms and beds"),
        new(LocationManage, "Create/update buildings, floors, wards, rooms and beds"),
        new(VendorView, "View vendors"),
        new(VendorManage, "Create/update vendors"),
        new(ItemView, "View items and item categories"),
        new(ItemManage, "Create/update items and item categories"),
        new(DrugView, "View drugs, generics and manufacturers"),
        new(DrugManage, "Create/update drugs, generics and manufacturers"),
        new(TaxView, "View tax slabs"),
        new(TaxManage, "Create/update tax slabs"),
        new(DiscountTypeView, "View discount types"),
        new(DiscountTypeManage, "Create/update discount types"),
        new(ReferrerView, "View referrers"),
        new(ReferrerManage, "Create/update referrers"),
        new(IcdView, "View ICD-10 codes"),
        new(IcdManage, "Create/update ICD-10 codes"),
        new(IcdImport, "Bulk-import ICD-10 codes from CSV"),
        new(CorporateView, "View corporate clients"),
        new(CorporateManage, "Create/update corporate clients"),
        new(InsuranceView, "View insurance/TPA companies"),
        new(InsuranceManage, "Create/update insurance/TPA companies"),
        new(UomView, "View units of measure"),
        new(UomManage, "Create/update units of measure"),
        new(SampleTypeView, "View sample types"),
        new(SampleTypeManage, "Create/update sample types"),
        new(ModalityView, "View modalities"),
        new(ModalityManage, "Create/update modalities"),
        new(ConsentTemplateView, "View consent templates"),
        new(ConsentTemplateManage, "Create/update consent templates"),
        new(RecordDocumentTypeView, "View MRD document types"),
        new(RecordDocumentTypeManage, "Create/update MRD document types"),
        new(CampView, "View camps"),
        new(CampManage, "Create/update camps"),
        new(StateView, "View states"),
        new(StateManage, "Create/update states"),
        new(CityView, "View cities"),
        new(CityManage, "Create/update cities"),
        new(NationalityView, "View nationalities"),
        new(NationalityManage, "Create/update nationalities"),
        new(ReligionView, "View religions"),
        new(ReligionManage, "Create/update religions"),
        new(LanguageView, "View languages"),
        new(LanguageManage, "Create/update languages"),
        new(SpeciallyAbledCategoryView, "View specially-abled categories"),
        new(SpeciallyAbledCategoryManage, "Create/update specially-abled categories"),
        new(PatientHcTagView, "View patient HC tags"),
        new(PatientHcTagManage, "Create/update patient HC tags"),
        new(PatientCaseTypeView, "View patient case types"),
        new(PatientCaseTypeManage, "Create/update patient case types"),
        new(MlcTypeView, "View MLC types"),
        new(MlcTypeManage, "Create/update MLC types"),
        new(MhcDoctorView, "View MHC doctors"),
        new(MhcDoctorManage, "Create/update MHC doctors"),
        new(LoyaltyProgramView, "View loyalty programs"),
        new(LoyaltyProgramManage, "Create/update loyalty programs"),
        new(UnitView, "View units"),
        new(UnitManage, "Create/update units"),
        new(AppointmentTypeView, "View appointment types"),
        new(AppointmentTypeManage, "Create/update appointment types"),
        new(AreaView, "View areas"),
        new(AreaManage, "Create/update areas"),
        new(MhcPackageView, "View MHC checkup packages"),
        new(MhcPackageManage, "Create/update MHC checkup packages"),
        new(BillEstimateTemplateView, "View bill estimate templates"),
        new(BillEstimateTemplateManage, "Create/update bill estimate templates"),
        new(DoctorPayoutRuleView, "View doctor payout rules"),
        new(DoctorPayoutRuleManage, "Create/update doctor payout rules"),
        new(LoyaltyCardCategoryView, "View loyalty card categories"),
        new(LoyaltyCardCategoryManage, "Create/update loyalty card categories"),
        new(MemberRelationTagView, "View member relation tags"),
        new(MemberRelationTagManage, "Create/update member relation tags"),
        new(LoyaltyCardTypeView, "View loyalty card types"),
        new(LoyaltyCardTypeManage, "Create/update loyalty card types"),
        new(LoyaltyCardPriceView, "View the loyalty card price matrix"),
        new(LoyaltyCardPriceManage, "Create/update the loyalty card price matrix"),

        new(PatientView, "View patients and search"),
        new(PatientCreate, "Register new patients"),
        new(PatientUpdate, "Update patient demographics, contacts, documents and consents"),
        new(PatientMerge, "Merge duplicate patient records"),
        new(PatientBlock, "Block or unblock a patient record at the front desk"),
        new(AbhaView, "View a patient's linked ABHA (ABDM)"),
        new(AbhaManage, "Link, create or unlink a patient's ABHA via ABDM (M1)"),
        new(AbdmHipManage, "Link care contexts and share health data as HIP (M2)"),
        new(AbdmHipView, "View linked care contexts and data transfer logs (M2)"),
        new(AbdmHiuManage, "Initiate consent requests and fetch health records as HIU (M3)"),
        new(AbdmHiuView, "View external longitudinal health records via ABDM (M3)"),

        new(ScheduleView, "View doctor schedules"),
        new(ScheduleManage, "Create/update doctor weekly schedules and leave overrides"),
        new(AppointmentUnitScheduleView, "View unit weekly schedules"),
        new(AppointmentUnitScheduleManage, "Create/update unit weekly schedules"),
        new(AppointmentView, "View and search appointments"),
        new(AppointmentBook, "Book appointments (including walk-in)"),
        new(AppointmentReschedule, "Reschedule appointments"),
        new(AppointmentCancel, "Cancel appointments"),
        new(AppointmentCheckIn, "Check in a patient for their appointment"),
        new(AppointmentSkip, "Skip/recall a checked-in patient's token"),
        new(AppointmentTransfer, "Transfer a token to a different doctor"),
        new(QueueView, "View the doctor token queue display"),

        new(VisitView, "View and search OPD visits and the queue"),
        new(VisitCreate, "Register OPD visits (front desk)"),
        new(VisitManage, "Chart the consultation: vitals, history, diagnosis, prescription, orders, referral, follow-up"),
        new(VisitImport, "Bulk-import Camp registrations from a CSV of UHIDs"),
        new(TemplateView, "View prescription templates"),
        new(TemplateManage, "Create/update prescription templates"),

        new(MhcVisitView, "View and search MHC visits"),
        new(MhcVisitCreate, "Register MHC (Master Health Checkup) visits (front desk)"),
        new(MhcVisitManage, "Cancel MHC visits"),
        new(MhcVisitImport, "Bulk-import MHC registrations from a CSV of UHIDs"),

        new(LoyaltyCardView, "View and search loyalty cards"),
        new(LoyaltyCardCreate, "Register loyalty cards (front desk)"),
        new(LoyaltyCardManage, "Renew/upgrade a loyalty card, add/remove dependents, issue a duplicate"),
        new(LoyaltyCardDeactivate, "Deactivate a loyalty card"),

        new(BillView, "View and search bills"),
        new(BillViewOp, "View OP bills (in reports and lists, alongside BILLING.BILL.VIEW)"),
        new(BillViewIp, "View IP bills (in reports and lists, alongside BILLING.BILL.VIEW)"),
        new(BillViewEmergency, "View Emergency bills (in reports and lists, alongside BILLING.BILL.VIEW)"),
        new(BillCreate, "Generate bills"),
        new(BillCreateOp, "Generate OP bills"),
        new(BillCreateIp, "Generate IP bills"),
        new(BillCreateEmergency, "Generate Emergency bills"),
        new(BillManage, "Edit bill line items and apply discounts up to the auto-approve threshold"),
        new(BillCancel, "Cancel bills"),
        new(PaymentCollect, "Collect payments and print receipts"),
        new(RefundRequest, "Request a refund against a bill or an advance"),
        new(RefundApprove, "Approve or reject refund requests"),
        new(RefundIssue, "Pay out an approved refund"),
        new(DiscountApprove, "Approve or reject discounts above the auto-approve threshold"),
        new(AdvanceView, "View patient advances/deposits"),
        new(AdvanceCollect, "Collect an advance/deposit"),
        new(AdvanceTransfer, "Transfer an advance's unused balance to a different patient/admission"),
        new(DayCloseView, "View daily collection and day-close status"),
        new(DayCloseClose, "Close the day (cash reconciliation)"),
        new(PaymentModeView, "View payment modes"),
        new(PaymentModeManage, "Create/update payment modes"),
        new(AdvanceDraw, "Draw down a patient's advance/deposit against a bill's balance"),
        new(BillNoteManage, "Add a remark note to a bill"),
        new(CreditNoteView, "View bill credit notes"),
        new(CreditNoteCreate, "Request a credit note against a paid/partially paid bill"),
        new(CreditNoteApprove, "Approve or reject a credit note request"),
        new(BillPark, "Park a bill (put it on hold)"),
        new(BillParkApprove, "Unpark a bill"),
        new(CashierShiftView, "View cashier shifts"),
        new(CashierShiftManage, "Open and close my own cashier shift"),
        new(CashierShiftReceive, "Confirm physical cash received from a closed cashier shift"),
        new(BillEstimateView, "View bill estimates"),
        new(BillEstimateCreate, "Create, copy and print bill estimates"),
        new(BillingReportView, "View billing reports (ageing, outstanding, day book, concession, refund, advance, tariff list, doctors payout, settlements)"),
        new(OnlinePaymentRequestView, "View the Patient Portal's online payment request queue"),
        new(OnlinePaymentRequestManage, "Mark an online payment request completed (after collecting payment on the bill) or cancelled"),

        new(LabTestView, "View lab tests, parameters and reference ranges"),
        new(LabTestManage, "Create/update lab tests, parameters and reference ranges"),
        new(LabProfileView, "View lab test profiles"),
        new(LabProfileManage, "Create/update lab test profiles"),
        new(LabOrderView, "View and search lab orders and worklists"),
        new(LabOrderCreate, "Create lab orders"),
        new(LabOrderCancel, "Cancel lab orders"),
        new(LabSampleCollect, "Collect samples against a lab order"),
        new(LabSampleReceive, "Acknowledge sample receipt in the lab"),
        new(LabSampleReject, "Reject a sample (triggers recollection)"),
        new(LabResultCreate, "Enter lab result values"),
        new(LabResultVerify, "Verify entered lab results"),
        new(LabResultApprove, "Approve/sign lab results"),
        new(LabAlertAcknowledge, "Acknowledge critical value alerts"),
        new(LabReportView, "View/print lab reports and the TAT report"),

        new(PharmacyStoreView, "View pharmacy stores"),
        new(PharmacyStoreManage, "Create/update pharmacy stores"),
        new(PurchaseRequestView, "View purchase requests"),
        new(PurchaseRequestCreate, "Create purchase requests"),
        new(PurchaseRequestApprove, "Approve or reject purchase requests"),
        new(PurchaseOrderView, "View purchase orders"),
        new(PurchaseOrderCreate, "Create purchase orders"),
        new(PurchaseOrderApprove, "Approve purchase orders"),
        new(GrnView, "View goods receipt notes"),
        new(GrnCreate, "Create and post goods receipt notes (creates/tops up batches, writes stock in)"),
        new(PurchaseReturnView, "View purchase returns"),
        new(PurchaseReturnCreate, "Create and post purchase returns"),
        new(PharmacySaleView, "View pharmacy sales"),
        new(PharmacySaleCreate, "Create OP pharmacy POS sales"),
        new(PharmacySaleCancel, "Cancel pharmacy sales"),
        new(PharmacySaleModify, "Correct a completed sale's payment mode"),
        new(SalesReturnView, "View pharmacy sales returns"),
        new(SalesReturnCreate, "Create pharmacy sales returns"),
        new(IssueView, "View department/emergency/IP stock issues"),
        new(IssueCreate, "Create department/emergency stock issues"),
        new(IssueReturnCreate, "Return stock against an issue"),
        new(StockView, "View stock/batch/expiry across stores"),
        new(StockAdjust, "Request a stock adjustment"),
        new(StockApprove, "Approve or reject stock adjustments"),
        new(StockTransfer, "Create and dispatch a stock transfer"),
        new(StockReceive, "Receive a stock transfer at the destination store"),
        new(StockAudit, "Create and complete a stock audit (count sheet + variance posting)"),
        new(PharmacyReportView, "View near-expiry/low-stock alerts and the drug consumption report"),

        new(IndentView, "View department indents against a store"),
        new(IndentCreate, "Raise a department indent"),
        new(IndentApprove, "Approve or reject a department indent"),

        new(AdmissionView, "View and search admissions"),
        new(AdmissionCreate, "Admit a patient (bed allocation, consultant mapping, consent, deposit)"),
        new(AdmissionRequestView, "View pre-admission requests"),
        new(AdmissionRequestCreate, "Raise a pre-admission request"),
        new(AdmissionRequestDecide, "Approve or reject a pre-admission request"),
        new(AdmissionCancel, "Cancel an admission"),
        new(BedView, "View the bed board / occupancy dashboard"),
        new(BedManage, "Allocate/transfer beds and toggle bed status"),
        new(ConsultantManage, "Map or transfer admission consultants"),
        new(DoctorOrderView, "View the unified doctor order sheet"),
        new(DoctorOrderCreate, "Place doctor orders (medication/lab/radiology/diet/nursing)"),
        new(DoctorOrderCancel, "Cancel a doctor order"),
        new(ChartView, "View progress notes, vitals chart, intake/output chart and procedure notes"),
        new(ChartManage, "Record progress notes, vitals, intake/output and procedure notes"),
        new(DischargeRequest, "Request discharge and set discharge type"),
        new(DischargeClear, "Clear ANY discharge checklist item (administrator override)"),
        new(DischargeClearItem, "Use the discharge clearance checklist"),
        new(DischargeClearBilling, "Give the Billing discharge clearance (IP bill settled)"),
        new(DischargeClearPharmacy, "Give the Pharmacy discharge clearance"),
        new(DischargeClearNursing, "Give the Nursing discharge clearance"),
        new(DischargeClearInvestigation, "Give the Final Investigation discharge clearance"),
        new(DischargeApproveSummary, "Approve the discharge summary"),
        new(DischargeFinalize, "Finalize discharge (release bed, close admission)"),

        new(NursingChartView, "View nursing assessments, shift notes, risk scores, care plans and transfer notes"),
        new(NursingChartManage, "Record nursing assessments, shift notes, risk scores, care plans and transfer notes"),
        new(MarView, "View the Medication Administration Record"),
        new(MarAdminister, "Administer, skip or refuse a scheduled dose"),
        new(HandoverView, "View shift handovers"),
        new(HandoverManage, "Record a ward shift handover"),
        new(IncidentView, "View and search incident reports"),
        new(IncidentCreate, "Report an incident"),
        new(IncidentReview, "Review or close an incident report"),

        new(EmrTimelineView, "View the patient timeline and history views (prescriptions, investigations, procedures, discharge summaries, results trend)"),
        new(EmrHistoryView, "View medical, surgical, family, allergy and immunization histories"),
        new(EmrHistoryManage, "Record medical, surgical, family, allergy and immunization histories"),
        new(EmrProblemListView, "View the problem list"),
        new(EmrProblemListManage, "Add and resolve problem list entries"),
        new(EmrNoteView, "View clinical notes"),
        new(EmrNoteManage, "Record clinical notes"),
        new(EmrAttachmentView, "View and download EMR attachments"),
        new(EmrAttachmentManage, "Upload EMR attachments"),

        new(RadTestView, "View radiology tests"),
        new(RadTestManage, "Create/update radiology tests"),
        new(RadTemplateView, "View radiology report templates"),
        new(RadTemplateManage, "Create/update radiology report templates"),
        new(RadOrderView, "View and search radiology orders"),
        new(RadOrderCreate, "Create radiology orders"),
        new(RadOrderCancel, "Cancel radiology orders"),
        new(RadScheduleManage, "View the scheduling worklist and schedule a radiology test"),
        new(RadCaptureManage, "View the image-capture worklist and mark a test as captured"),
        new(RadContrastConsentCapture, "Capture contrast consent for a radiology test"),
        new(RadReportCreate, "Enter radiology report findings/impression"),
        new(RadReportVerify, "Verify entered radiology reports"),
        new(RadReportApprove, "Approve/sign radiology reports"),
        new(RadReportView, "View/print radiology reports and the TAT report"),

        new(OtTheatreView, "View theatres and equipment"),
        new(OtTheatreManage, "Create/update theatres and equipment"),
        new(OtBookingView, "View surgery bookings and the OT register"),
        new(OtBookingCreate, "Book a surgery"),
        new(OtBookingManage, "Reschedule, cancel, postpone, or update the linked patient/admission of a surgery booking"),
        new(OtSurgeryConduct, "Start and complete a surgery (gated on the WHO checklist and consent)"),
        new(OtChecklistManage, "Complete the WHO surgical safety checklist and verify consent"),
        new(OtAnaesthesiaNoteManage, "Record pre-anaesthesia assessment and intra-op anaesthesia notes"),
        new(OtOperativeNoteManage, "Record operative and post-operative notes"),
        new(OtUsageRecord, "Record implant/consumable usage (decrements stock, posts IP bill charges)"),
        new(OtRegisterView, "View the OT register report"),

        new(IcuRuleView, "View critical alert threshold rules"),
        new(IcuRuleManage, "Create/update critical alert threshold rules"),
        new(IcuVitalsRecord, "Record ICU vitals (evaluates critical alert rules)"),
        new(IcuAlertView, "View raised critical alerts"),
        new(IcuAlertAcknowledge, "Acknowledge a raised critical alert"),
        new(IcuChartView, "View ventilator/infusion charts and severity history"),
        new(IcuChartManage, "Chart ventilator/infusion sessions and record severity assessments"),
        new(IcuDashboardView, "View the ICU dashboard (census, ventilated count, severity)"),

        new(EmergencyVisitView, "View the ER queue and visit history"),
        new(EmergencyVisitCreate, "Register an ER visit"),
        new(EmergencyVisitManage, "Cancel an ER visit"),
        new(EmergencyTriage, "Record ER triage (ESI level, chief complaint, vitals)"),
        new(EmergencyAssessmentManage, "Record the ER doctor's assessment and disposition"),
        new(EmergencyObservationManage, "Allocate/release an observation-ward bed"),
        new(EmergencyMlcManage, "Record MLC and accident case register details"),

        new(MrdFileView, "View the MRD file board and movement history"),
        new(MrdFileCreate, "Open a medical record file for an admission"),
        new(MrdFileManage, "Record a file movement (location/custodian change)"),
        new(MrdFileIssue, "Approve/reject/issue/return a file request"),
        new(MrdRequestCreate, "Raise a file issue or record request"),
        new(MrdDocumentView, "View scanned documents against an admission"),
        new(MrdDocumentUpload, "Upload a scanned document against an admission"),
        new(MrdCodingView, "View the ICD coding worklist"),
        new(MrdCodingManage, "Mark a discharged case as coded"),
        new(MrdAuditView, "View discharge file audits"),
        new(MrdAuditManage, "Record a discharge file audit checklist"),
        new(MrdRetentionView, "View retention policies"),
        new(MrdRetentionManage, "Create/update retention policies"),
        new(MrdDestructionView, "View the destruction register"),
        new(MrdDestructionManage, "Approve/destroy a record file"),
        new(MrdReportView, "View MRD reports (coding stats, file movement, incomplete records)"),

        new(FinanceAccountView, "View the Chart of Accounts"),
        new(FinanceAccountManage, "Create/update accounts"),
        new(FinanceVoucherView, "View vouchers"),
        new(FinanceVoucherCreate, "Post a Journal/Payment/Receipt voucher"),
        new(FinanceVoucherCancel, "Cancel a posted voucher (reversal)"),
        new(FinanceLedgerView, "View an account's ledger"),
        new(FinancePayableView, "View payables and the pending-GRN worklist"),
        new(FinancePayableManage, "Raise a payable from a GRN and record payments"),
        new(FinanceReceivableView, "View receivables and the pending-bill worklist"),
        new(FinanceReceivableManage, "Raise a receivable from a credit bill and record collections"),
        new(FinanceReportView, "View Finance reports (cash/bank collection, revenue, GST, outstanding)"),
        new(FinanceDayCloseConsoleView, "View the centralized cross-branch day-close console"),
        new(ReportCrossBranchView, "View cross-branch data in reports"),
        new(DashboardHomeView, "View the home Dashboard landing screen"),

        new(DashboardManagementView, "View the Management Dashboard"),
        new(DashboardOpView, "View the OP Dashboard"),
        new(DashboardIpView, "View the IP Dashboard"),
        new(DashboardBillingView, "View the Billing Dashboard"),
        new(DashboardRevenueView, "View the Revenue Dashboard"),
        new(ReportCatalogView, "View and run the Custom Report Builder catalog"),
        new(ReportSubscriptionManage, "Create/delete my own report subscriptions"),
        new(ReportCensusView, "View the OPIP Census Report"),

        new(BiomedEquipmentView, "View the equipment registry and location history"),
        new(BiomedEquipmentManage, "Create/update equipment and record a location movement"),
        new(BiomedContractView, "View AMC/CMC contracts"),
        new(BiomedContractManage, "Create/update AMC/CMC contracts"),
        new(BiomedPmView, "View preventive maintenance schedules"),
        new(BiomedPmManage, "Create/update PM schedules and generate due work orders"),
        new(BiomedWorkOrderView, "View work orders"),
        new(BiomedWorkOrderManage, "Assign/start/complete/cancel a work order and record spare usage"),
        new(BiomedComplaintView, "View breakdown complaints"),
        new(BiomedComplaintCreate, "Raise a breakdown complaint"),
        new(BiomedComplaintManage, "Assign/resolve/close a breakdown complaint"),
        new(BiomedCalibrationView, "View calibration records"),
        new(BiomedCalibrationManage, "Record a calibration"),
        new(BiomedReportView, "View Biomedical reports (downtime, PM compliance)"),

        new(QualityIndicatorView, "View quality indicators and their monthly entries"),
        new(QualityIndicatorManage, "Create/update quality indicators"),
        new(QualityIndicatorEntryRecord, "Record or auto-compute a monthly indicator entry"),
        new(QualityAuditTemplateView, "View audit checklist templates"),
        new(QualityAuditTemplateManage, "Create/update audit checklist templates"),
        new(QualityAuditView, "View scheduled/scored audits"),
        new(QualityAuditManage, "Schedule, score and complete an audit"),
        new(QualityIncidentRca, "Record root-cause analysis and the sentinel-event flag on an incident"),
        new(QualityNonConformanceView, "View non-conformance findings"),
        new(QualityNonConformanceCreate, "Raise a non-conformance finding"),
        new(QualityNonConformanceManage, "Move a non-conformance to In Progress or close it"),
        new(QualityCapaView, "View CAPA actions"),
        new(QualityCapaManage, "Raise, complete, verify and close a CAPA action"),
        new(QualityRiskView, "View the risk register"),
        new(QualityRiskManage, "Create/update risk register entries"),
        new(QualityCommitteeView, "View committees, meeting records, action items and NABH standards"),
        new(QualityCommitteeManage, "Create/update committees, meeting records, action items and NABH standards"),
        new(QualityEvidenceView, "View/download NABH evidence documents"),
        new(QualityEvidenceUpload, "Upload an NABH evidence document"),
        new(QualityDashboardView, "View the KPI and compliance dashboards"),

        new(DoctorPortalDashboardView, "View the Doctor Portal dashboard"),
        new(DoctorPortalQueueView, "View my own OP queue in the Doctor Portal"),
        new(DoctorPortalIpRoundsView, "View my own inpatients in the Doctor Portal"),
        new(DoctorPortalOrdersView, "View my own lab orders/results in the Doctor Portal"),
        new(DoctorPortalDischargeView, "View my own pending discharge-summary sign-offs"),
        new(DoctorPortalOpinionView, "View my own cross-consult opinion requests"),

        new(SalutationView, "View salutations"),
        new(SalutationManage, "Create/update salutations"),
        new(MaritalStatusView, "View marital statuses"),
        new(MaritalStatusManage, "Create/update marital statuses"),
        new(IdentityDocumentTypeView, "View identity document types"),
        new(IdentityDocumentTypeManage, "Create/update identity document types"),
        new(VisitClassView, "View OP registration classes"),
        new(VisitClassManage, "Create/update OP registration classes"),
        new(VisitSessionView, "View OP registration sessions"),
        new(VisitSessionManage, "Create/update OP registration sessions"),
        new(ClaimDocumentTypeView, "View claim document types"),
        new(ClaimDocumentTypeManage, "Create/update claim document types"),
        new(ClaimDenialReasonView, "View claim denial reasons"),
        new(ClaimDenialReasonManage, "Create/update claim denial reasons"),

        new(InsurancePolicyView, "View a patient's insurance policies"),
        new(InsurancePolicyManage, "Add/update a patient's insurance policies"),
        new(InsurancePreAuthView, "View pre-authorization requests"),
        new(InsurancePreAuthCreate, "Raise a pre-authorization request or enhancement"),
        new(InsurancePreAuthApprove, "Approve/reject a pre-authorization request or enhancement"),
        new(InsuranceClaimView, "View claims"),
        new(InsuranceClaimCreate, "Submit a claim and attach supporting documents"),
        new(InsuranceClaimManage, "Move a claim through review/approval/denial"),
        new(InsuranceClaimSettle, "Record a claim settlement"),
    ];
}
