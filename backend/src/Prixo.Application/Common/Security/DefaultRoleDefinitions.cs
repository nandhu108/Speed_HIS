namespace Prixo.Application.Common.Security;

/// <summary>A system role's seeded default: what DbSeeder creates it with, and what "Restore to default" resets it back to.</summary>
public record DefaultRoleDefinition(string Code, string Name, string Description, IReadOnlyList<string> Permissions);

/// <summary>
/// The single source of truth for every system (IsSystem = true) role's default permission set —
/// seeded into the database by DbSeeder, and re-applied verbatim by RoleService's
/// "Restore to default" action when an admin's hand-tuning of a role has drifted and they want
/// to undo it. Modules append to the department-role list here as phases land, exactly like
/// PermissionRegistry — codes/roles are never invented ad hoc in DbSeeder itself anymore.
/// </summary>
public static class DefaultRoleDefinitions
{
    /// <summary>
    /// HOSPITAL_ADMIN's operational module whitelist (plan §8: "day-to-day administration") —
    /// widened as modules land in later phases; REPORT.CROSSBRANCH.VIEW and DOCTORPORTAL.* stay
    /// out deliberately (branch-scoped admin, not a doctor identity) — see docs/modules/admin.md.
    /// </summary>
    private static readonly string[] HospitalAdminModules =
    [
        "MASTER", "PATIENT", "APPOINTMENT", "OPD", "MHC", "LOYALTY", "BILLING",
        "LAB", "PHARMACY", "IPD", "NURSING", "EMR", "RAD",
        "OT", "ICU", "EMERGENCY", "MRD", "FINANCE", "BIOMED", "QUALITY",
    ];

    public static IReadOnlyList<DefaultRoleDefinition> All { get; } = Build();

    public static DefaultRoleDefinition? Find(string code) =>
        All.FirstOrDefault(r => r.Code == code);

    private static List<DefaultRoleDefinition> Build()
    {
        string[] nursePermissions =
        [
            PermissionRegistry.PatientView,
            PermissionRegistry.AdmissionView, PermissionRegistry.BedView, PermissionRegistry.DoctorOrderView,
            PermissionRegistry.ChartView, PermissionRegistry.ChartManage,
            PermissionRegistry.NursingChartView, PermissionRegistry.NursingChartManage,
            PermissionRegistry.MarView, PermissionRegistry.MarAdminister,
            PermissionRegistry.HandoverView, PermissionRegistry.HandoverManage,
            PermissionRegistry.IncidentView, PermissionRegistry.IncidentCreate,
            PermissionRegistry.IcuChartView, PermissionRegistry.IcuVitalsRecord,
            PermissionRegistry.IcuAlertView, PermissionRegistry.IcuAlertAcknowledge,
            PermissionRegistry.EmergencyTriage, PermissionRegistry.EmergencyObservationManage,
            PermissionRegistry.EmrTimelineView, PermissionRegistry.EmrHistoryView,
        ];
        string[] pharmacistPermissions =
        [
            PermissionRegistry.DrugView, PermissionRegistry.PharmacyStoreView,
            PermissionRegistry.PharmacySaleView, PermissionRegistry.PharmacySaleCreate, PermissionRegistry.PharmacySaleCancel,
            PermissionRegistry.PharmacySaleModify,
            PermissionRegistry.SalesReturnView, PermissionRegistry.SalesReturnCreate,
            PermissionRegistry.IssueView, PermissionRegistry.IssueCreate, PermissionRegistry.IssueReturnCreate,
            PermissionRegistry.StockView, PermissionRegistry.StockAudit,
            PermissionRegistry.PharmacyReportView, PermissionRegistry.IndentView,
        ];
        string[] pharmacyPurchasePermissions =
        [
            PermissionRegistry.VendorView, PermissionRegistry.DrugView,
            PermissionRegistry.PurchaseRequestView, PermissionRegistry.PurchaseRequestCreate,
            PermissionRegistry.PurchaseOrderView, PermissionRegistry.PurchaseOrderCreate,
            PermissionRegistry.GrnView, PermissionRegistry.GrnCreate,
            PermissionRegistry.PurchaseReturnView, PermissionRegistry.PurchaseReturnCreate,
            PermissionRegistry.StockView, PermissionRegistry.PharmacyReportView,
        ];
        string[] insuranceExecutivePermissions =
        [
            PermissionRegistry.InsurancePolicyView, PermissionRegistry.InsurancePolicyManage,
            PermissionRegistry.InsurancePreAuthView, PermissionRegistry.InsurancePreAuthCreate,
            PermissionRegistry.InsuranceClaimView, PermissionRegistry.InsuranceClaimCreate, PermissionRegistry.InsuranceClaimManage,
        ];
        string[] billingExecutivePermissions =
        [
            PermissionRegistry.BillView, PermissionRegistry.BillViewOp, PermissionRegistry.BillViewIp, PermissionRegistry.BillViewEmergency,
            PermissionRegistry.BillCreate, PermissionRegistry.BillCreateOp, PermissionRegistry.BillCreateIp, PermissionRegistry.BillCreateEmergency,
            PermissionRegistry.BillManage,
            PermissionRegistry.PaymentCollect, PermissionRegistry.RefundRequest, PermissionRegistry.RefundIssue,
            PermissionRegistry.AdvanceView, PermissionRegistry.AdvanceCollect, PermissionRegistry.PaymentModeView,
            PermissionRegistry.AdvanceDraw, PermissionRegistry.BillNoteManage, PermissionRegistry.CreditNoteView,
            PermissionRegistry.CreditNoteCreate, PermissionRegistry.BillPark, PermissionRegistry.CashierShiftManage,
            PermissionRegistry.BillEstimateView, PermissionRegistry.BillEstimateCreate,
            PermissionRegistry.OnlinePaymentRequestView, PermissionRegistry.OnlinePaymentRequestManage,
        ];
        string[] opBillingPermissions =
        [
            // OP payment/search screen prerequisite (patient lookup in the bill-payment/advance
            // pickers). VisitView (OPD.VISIT.VIEW) is deliberately NOT included here — no billing
            // endpoint or frontend page checks it, and holding it only leaked the Front Office/OPD
            // registration menus into this role's sidebar, which the legacy OP billing menu never had.
            PermissionRegistry.PatientView,
            // Doctor lookup prerequisite for this role's own screens — the Doctor Payout Rules
            // master, Source Names' doctor-mapping dropdown, and the Doctors Payout Report filter
            // all call GET /masters/doctors, which is gated on DoctorView.
            PermissionRegistry.DoctorView,
            // OP bill generation, bill copy/print, service billing, payment collection and OP-only visibility.
            PermissionRegistry.BillView, PermissionRegistry.BillViewOp, PermissionRegistry.BillCreateOp,
            PermissionRegistry.BillManage, PermissionRegistry.PaymentCollect,
            PermissionRegistry.RefundRequest, PermissionRegistry.RefundIssue,
            // OP advance/pre-payment handling.
            PermissionRegistry.AdvanceView, PermissionRegistry.AdvanceCollect, PermissionRegistry.AdvanceDraw,
            // Counter settlement and routine bill remarks.
            PermissionRegistry.CashierShiftManage, PermissionRegistry.BillNoteManage,
            // OP estimate/package quotation support.
            PermissionRegistry.BillEstimateView, PermissionRegistry.BillEstimateCreate,
            // OP billing masters/config mapped from the legacy "Masters" section. Room-type-specific
            // tariff rows deliberately stay unusable here — the RoomType picker needs LocationView,
            // which would also leak the full Locations & Beds (branch/ward/room/bed) master screen
            // into this OP-only role's sidebar; room-agnostic tariff rows are unaffected.
            PermissionRegistry.TariffView, PermissionRegistry.TariffManage, PermissionRegistry.TariffImport,
            PermissionRegistry.ServiceView, PermissionRegistry.ServiceManage,
            PermissionRegistry.PackageView, PermissionRegistry.PackageManage,
            PermissionRegistry.MhcPackageView, PermissionRegistry.MhcPackageManage,
            PermissionRegistry.DiscountTypeView, PermissionRegistry.DiscountTypeManage,
            PermissionRegistry.ReferrerView, PermissionRegistry.ReferrerManage,
            PermissionRegistry.NationalityView, PermissionRegistry.NationalityManage,
            PermissionRegistry.PatientCategoryView, PermissionRegistry.PatientCategoryManage,
            PermissionRegistry.CorporateView, PermissionRegistry.CorporateManage,
            PermissionRegistry.BillEstimateTemplateView, PermissionRegistry.BillEstimateTemplateManage,
            PermissionRegistry.DoctorPayoutRuleView, PermissionRegistry.DoctorPayoutRuleManage,
            PermissionRegistry.PaymentModeView, PermissionRegistry.PaymentModeManage,
            // OP supervisory transactions: concessions, refunds, cancellations, parking and settlement receive/view.
            PermissionRegistry.BillCancel, PermissionRegistry.DiscountApprove,
            PermissionRegistry.RefundApprove, PermissionRegistry.AdvanceTransfer,
            PermissionRegistry.CreditNoteView, PermissionRegistry.CreditNoteCreate, PermissionRegistry.CreditNoteApprove,
            PermissionRegistry.BillPark, PermissionRegistry.BillParkApprove,
            PermissionRegistry.CashierShiftView, PermissionRegistry.CashierShiftReceive,
            PermissionRegistry.DayCloseView, PermissionRegistry.BillingReportView,
            PermissionRegistry.OnlinePaymentRequestView, PermissionRegistry.OnlinePaymentRequestManage,
            // Billing Dashboard — this role's DefaultLandingPath, so it must hold the view
            // permission or login redirects it straight into a 403.
            PermissionRegistry.DashboardBillingView,
        ];
        string[] financeExecutivePermissions =
        [
            PermissionRegistry.FinanceVoucherView, PermissionRegistry.FinanceVoucherCreate, PermissionRegistry.FinanceLedgerView,
            PermissionRegistry.FinancePayableView, PermissionRegistry.FinancePayableManage,
            PermissionRegistry.FinanceReceivableView, PermissionRegistry.FinanceReceivableManage,
            PermissionRegistry.FinanceReportView, PermissionRegistry.FinanceAccountView,
        ];
        string[] mrdOfficerPermissions =
        [
            PermissionRegistry.MrdFileView, PermissionRegistry.MrdFileCreate, PermissionRegistry.MrdFileManage,
            PermissionRegistry.MrdRequestCreate, PermissionRegistry.MrdDocumentView, PermissionRegistry.MrdDocumentUpload,
            PermissionRegistry.MrdCodingView, PermissionRegistry.MrdCodingManage,
            PermissionRegistry.MrdAuditView, PermissionRegistry.MrdReportView,
        ];

        List<DefaultRoleDefinition> definitions =
        [
            new("SUPER_ADMIN", "Super Administrator", "Full access to every module and branch",
                PermissionRegistry.All.Select(p => p.Code).ToList()),

            new("HOSPITAL_ADMIN", "Hospital Administrator", "Day-to-day administration without security configuration",
            [
                PermissionRegistry.UserView, PermissionRegistry.UserCreate,
                PermissionRegistry.UserUpdate, PermissionRegistry.UserResetPassword,
                PermissionRegistry.RoleView, PermissionRegistry.PermissionView,
                PermissionRegistry.MenuView, PermissionRegistry.BranchView,
                PermissionRegistry.DesignationView, PermissionRegistry.DesignationManage,
                PermissionRegistry.SettingView,
                PermissionRegistry.SessionView, PermissionRegistry.SessionRevoke,
                PermissionRegistry.AuditView, PermissionRegistry.ActivityView,
                .. PermissionRegistry.All.Where(p => HospitalAdminModules.Contains(p.Module)).Select(p => p.Code),
            ]),

            new("FRONT_OFFICE", "Front Office", "Full front-office suite: OP/IP/ER/MHC/Camp registration, doctor entry, appointments/token management, loyalty desk, admission requests, document attachments, front-office reference masters, reports and control-panel settings",
            [
                // Patient registration (prerequisite for OP/IP/ER/MHC/Camp registration and photo capture)
                PermissionRegistry.PatientView, PermissionRegistry.PatientCreate, PermissionRegistry.PatientUpdate, PermissionRegistry.PatientBlock,
                PermissionRegistry.AbhaView, PermissionRegistry.AbhaManage,
                PermissionRegistry.AbdmHipView, PermissionRegistry.AbdmHipManage,
                // OP / Camp registration + doctor entry (clinical workspace) + Camp Bulk Import
                PermissionRegistry.VisitView, PermissionRegistry.VisitCreate, PermissionRegistry.VisitManage, PermissionRegistry.VisitImport,
                // MHC registration + MHC Bulk Import
                PermissionRegistry.MhcVisitView, PermissionRegistry.MhcVisitCreate, PermissionRegistry.MhcVisitManage, PermissionRegistry.MhcVisitImport,
                // ER registration
                PermissionRegistry.EmergencyVisitView, PermissionRegistry.EmergencyVisitCreate, PermissionRegistry.EmergencyVisitManage,
                // IP registration + admission request
                PermissionRegistry.AdmissionView, PermissionRegistry.AdmissionCreate, PermissionRegistry.AdmissionCancel,
                PermissionRegistry.AdmissionRequestView, PermissionRegistry.AdmissionRequestCreate, PermissionRegistry.AdmissionRequestDecide,
                // Appointments / queue / token management
                PermissionRegistry.ScheduleView, PermissionRegistry.AppointmentUnitScheduleView,
                PermissionRegistry.AppointmentView, PermissionRegistry.AppointmentBook, PermissionRegistry.AppointmentReschedule,
                PermissionRegistry.AppointmentCancel, PermissionRegistry.AppointmentCheckIn, PermissionRegistry.AppointmentSkip,
                PermissionRegistry.AppointmentTransfer, PermissionRegistry.QueueView,
                // Loyalty desk / loyalty transactions
                PermissionRegistry.LoyaltyCardView, PermissionRegistry.LoyaltyCardCreate, PermissionRegistry.LoyaltyCardManage, PermissionRegistry.LoyaltyCardDeactivate,
                // Document attachment view
                PermissionRegistry.EmrAttachmentView, PermissionRegistry.EmrAttachmentManage,
                // Front-office reference masters
                PermissionRegistry.CampView, PermissionRegistry.CampManage,
                PermissionRegistry.StateView, PermissionRegistry.StateManage,
                PermissionRegistry.CityView, PermissionRegistry.CityManage,
                PermissionRegistry.AreaView, PermissionRegistry.AreaManage,
                PermissionRegistry.NationalityView, PermissionRegistry.NationalityManage,
                PermissionRegistry.ReligionView, PermissionRegistry.ReligionManage,
                PermissionRegistry.LanguageView, PermissionRegistry.LanguageManage,
                PermissionRegistry.SpeciallyAbledCategoryView, PermissionRegistry.SpeciallyAbledCategoryManage,
                PermissionRegistry.PatientHcTagView, PermissionRegistry.PatientHcTagManage,
                PermissionRegistry.PatientCaseTypeView, PermissionRegistry.PatientCaseTypeManage,
                PermissionRegistry.PatientCategoryView, PermissionRegistry.PatientCategoryManage,
                PermissionRegistry.MlcTypeView, PermissionRegistry.MlcTypeManage,
                PermissionRegistry.MhcDoctorView, PermissionRegistry.MhcDoctorManage,
                PermissionRegistry.UnitView, PermissionRegistry.UnitManage,
                PermissionRegistry.AppointmentTypeView, PermissionRegistry.AppointmentTypeManage,
                PermissionRegistry.ReferrerView, PermissionRegistry.ReferrerManage,
                PermissionRegistry.LoyaltyProgramView, PermissionRegistry.LoyaltyProgramManage,
                PermissionRegistry.LoyaltyCardCategoryView, PermissionRegistry.LoyaltyCardCategoryManage,
                PermissionRegistry.MemberRelationTagView, PermissionRegistry.MemberRelationTagManage,
                PermissionRegistry.LoyaltyCardTypeView, PermissionRegistry.LoyaltyCardTypeManage,
                PermissionRegistry.LoyaltyCardPriceView, PermissionRegistry.LoyaltyCardPriceManage,
                PermissionRegistry.SalutationView, PermissionRegistry.SalutationManage,
                PermissionRegistry.MaritalStatusView, PermissionRegistry.MaritalStatusManage,
                PermissionRegistry.IdentityDocumentTypeView, PermissionRegistry.IdentityDocumentTypeManage,
                PermissionRegistry.VisitClassView, PermissionRegistry.VisitClassManage,
                PermissionRegistry.VisitSessionView, PermissionRegistry.VisitSessionManage,
                // Insurance policy capture (inline on Patient registration) + advance collection at OP registration
                PermissionRegistry.InsurancePolicyView, PermissionRegistry.InsurancePolicyManage,
                // Reports (reuse the owning module's VIEW permission, per repo convention — see docs/modules/*.md "Permissions" sections)
                PermissionRegistry.BedView, PermissionRegistry.ReportCensusView,
                // Tools: Front Office / DA control panel settings, consultation charge settings
                PermissionRegistry.SettingView, PermissionRegistry.SettingUpdate,
                // Tools: Module Rights (Roles & Permissions screen)
                PermissionRegistry.RoleView, PermissionRegistry.RoleAssignPermissions,
            ]),

            new("RECEPTION", "Reception", "Front-desk patient registration, appointments and OP billing",
            [
                PermissionRegistry.PatientView, PermissionRegistry.PatientCreate, PermissionRegistry.PatientUpdate,
                PermissionRegistry.AbhaView, PermissionRegistry.AbhaManage,
                PermissionRegistry.ScheduleView, PermissionRegistry.AppointmentUnitScheduleView, PermissionRegistry.AppointmentView, PermissionRegistry.AppointmentBook,
                PermissionRegistry.AppointmentReschedule, PermissionRegistry.AppointmentCancel, PermissionRegistry.AppointmentCheckIn,
                PermissionRegistry.AppointmentSkip, PermissionRegistry.AppointmentTransfer,
                PermissionRegistry.QueueView, PermissionRegistry.VisitView, PermissionRegistry.VisitCreate,
                PermissionRegistry.MhcVisitView, PermissionRegistry.MhcVisitCreate, PermissionRegistry.MhcVisitManage,
                PermissionRegistry.CampView,
                PermissionRegistry.LoyaltyCardView, PermissionRegistry.LoyaltyCardCreate, PermissionRegistry.LoyaltyCardManage,
                PermissionRegistry.BillView, PermissionRegistry.BillViewOp, PermissionRegistry.BillViewIp, PermissionRegistry.BillViewEmergency,
                PermissionRegistry.BillCreate, PermissionRegistry.BillCreateOp, PermissionRegistry.BillCreateIp, PermissionRegistry.BillCreateEmergency,
                PermissionRegistry.PaymentCollect,
                PermissionRegistry.AdvanceView, PermissionRegistry.AdvanceCollect,
                PermissionRegistry.BillEstimateView, PermissionRegistry.BillEstimateCreate,
                PermissionRegistry.EmergencyVisitView, PermissionRegistry.EmergencyVisitCreate,
            ]),

            new("APPOINTMENT_DESK", "Appointment Desk", "Dedicated appointment booking desk",
            [
                PermissionRegistry.PatientView, PermissionRegistry.PatientCreate, PermissionRegistry.ScheduleView,
                PermissionRegistry.AppointmentUnitScheduleView,
                PermissionRegistry.AppointmentView, PermissionRegistry.AppointmentBook, PermissionRegistry.AppointmentReschedule,
                PermissionRegistry.AppointmentCancel, PermissionRegistry.AppointmentCheckIn,
                PermissionRegistry.AppointmentSkip, PermissionRegistry.AppointmentTransfer, PermissionRegistry.QueueView,
            ]),

            new("DOCTOR", "Doctor", "Clinical consultation across OPD/IPD/OT/ICU/Emergency",
            [
                PermissionRegistry.PatientView, PermissionRegistry.ScheduleView, PermissionRegistry.AppointmentView,
                PermissionRegistry.AbhaView, PermissionRegistry.AbdmHipView, PermissionRegistry.AbdmHiuManage, PermissionRegistry.AbdmHiuView,
                PermissionRegistry.VisitView, PermissionRegistry.VisitManage, PermissionRegistry.TemplateView, PermissionRegistry.TemplateManage,
                PermissionRegistry.LabOrderView, PermissionRegistry.LabOrderCreate, PermissionRegistry.LabOrderCancel, PermissionRegistry.LabReportView,
                PermissionRegistry.RadOrderView, PermissionRegistry.RadOrderCreate, PermissionRegistry.RadOrderCancel, PermissionRegistry.RadReportView,
                PermissionRegistry.AdmissionView, PermissionRegistry.AdmissionCreate, PermissionRegistry.BedView, PermissionRegistry.ConsultantManage,
                PermissionRegistry.DoctorOrderView, PermissionRegistry.DoctorOrderCreate, PermissionRegistry.DoctorOrderCancel,
                PermissionRegistry.ChartView, PermissionRegistry.ChartManage,
                PermissionRegistry.DischargeRequest, PermissionRegistry.DischargeApproveSummary,
                PermissionRegistry.DischargeClearItem, PermissionRegistry.DischargeClearInvestigation,
                PermissionRegistry.AdmissionRequestView, PermissionRegistry.AdmissionRequestCreate,
                PermissionRegistry.NursingChartView, PermissionRegistry.HandoverView,
                PermissionRegistry.EmrTimelineView, PermissionRegistry.EmrHistoryView, PermissionRegistry.EmrHistoryManage,
                PermissionRegistry.EmrProblemListView, PermissionRegistry.EmrProblemListManage,
                PermissionRegistry.EmrNoteView, PermissionRegistry.EmrNoteManage, PermissionRegistry.EmrAttachmentView, PermissionRegistry.EmrAttachmentManage,
                PermissionRegistry.OtBookingView, PermissionRegistry.OtBookingCreate, PermissionRegistry.OtSurgeryConduct,
                PermissionRegistry.OtChecklistManage, PermissionRegistry.OtAnaesthesiaNoteManage, PermissionRegistry.OtOperativeNoteManage,
                PermissionRegistry.IcuChartView, PermissionRegistry.IcuChartManage, PermissionRegistry.IcuVitalsRecord,
                PermissionRegistry.IcuAlertView, PermissionRegistry.IcuAlertAcknowledge, PermissionRegistry.IcuDashboardView,
                PermissionRegistry.EmergencyVisitView, PermissionRegistry.EmergencyAssessmentManage, PermissionRegistry.EmergencyMlcManage,
                PermissionRegistry.IncidentView, PermissionRegistry.IncidentCreate,
            ]),

            new("DOCTOR_PORTAL", "Doctor Portal", "Portal access to a doctor's own queue/inpatients/orders/discharge sign-off — grant alongside DOCTOR, not instead of it",
            [
                PermissionRegistry.DoctorPortalDashboardView, PermissionRegistry.DoctorPortalQueueView,
                PermissionRegistry.DoctorPortalIpRoundsView, PermissionRegistry.DoctorPortalOrdersView, PermissionRegistry.DoctorPortalDischargeView,
                PermissionRegistry.DoctorPortalOpinionView,
            ]),

            new("NURSE", "Nurse", "Ward nursing care, MAR, handovers, incident reporting", nursePermissions),

            new("HEAD_NURSE", "Head Nurse", "Ward supervision — incident review, bed management, discharge clearance",
            [
                .. nursePermissions, PermissionRegistry.IncidentReview, PermissionRegistry.BedManage,
                PermissionRegistry.DischargeClearItem, PermissionRegistry.DischargeClearNursing,
            ]),

            new("LAB_TECHNICIAN", "Lab Technician", "Sample collection and result entry",
            [
                PermissionRegistry.LabTestView, PermissionRegistry.LabProfileView, PermissionRegistry.LabOrderView,
                PermissionRegistry.LabSampleCollect, PermissionRegistry.LabSampleReceive, PermissionRegistry.LabSampleReject,
                PermissionRegistry.LabResultCreate, PermissionRegistry.LabReportView,
            ]),

            new("PATHOLOGIST", "Pathologist", "Result verification/approval and lab catalog ownership",
            [
                PermissionRegistry.LabTestView, PermissionRegistry.LabTestManage, PermissionRegistry.LabProfileView, PermissionRegistry.LabProfileManage,
                PermissionRegistry.LabOrderView, PermissionRegistry.LabResultVerify, PermissionRegistry.LabResultApprove,
                PermissionRegistry.LabAlertAcknowledge, PermissionRegistry.LabReportView,
            ]),

            new("RADIOLOGY_TECHNICIAN", "Radiology Technician", "Scheduling and image-capture worklist",
            [
                PermissionRegistry.RadTestView, PermissionRegistry.RadOrderView, PermissionRegistry.RadScheduleManage,
                PermissionRegistry.RadCaptureManage, PermissionRegistry.RadContrastConsentCapture, PermissionRegistry.RadReportView,
            ]),

            new("RADIOLOGIST", "Radiologist", "Report verification/approval and radiology catalog ownership",
            [
                PermissionRegistry.RadTestView, PermissionRegistry.RadTestManage, PermissionRegistry.RadTemplateView, PermissionRegistry.RadTemplateManage,
                PermissionRegistry.RadOrderView, PermissionRegistry.RadReportCreate, PermissionRegistry.RadReportVerify,
                PermissionRegistry.RadReportApprove, PermissionRegistry.RadReportView,
            ]),

            new("PHARMACIST", "Pharmacist", "Dispensing and OP pharmacy POS sale",
                [.. pharmacistPermissions, PermissionRegistry.DischargeClearItem, PermissionRegistry.DischargeClearPharmacy]),

            new("PHARMACY_PURCHASE", "Pharmacy Purchase", "Procurement workflow (purchase request/order/GRN)", pharmacyPurchasePermissions),

            new("PHARMACY_MANAGER", "Pharmacy Manager", "Pharmacy HOD — approvals across dispensing and procurement",
            [
                .. pharmacistPermissions, .. pharmacyPurchasePermissions,
                PermissionRegistry.DischargeClearItem, PermissionRegistry.DischargeClearPharmacy,
                PermissionRegistry.PurchaseRequestApprove, PermissionRegistry.PurchaseOrderApprove, PermissionRegistry.StockApprove,
                PermissionRegistry.StockTransfer, PermissionRegistry.StockReceive, PermissionRegistry.StockAdjust, PermissionRegistry.IndentApprove,
                PermissionRegistry.PharmacyStoreManage, PermissionRegistry.DrugManage,
            ]),

            new("STORE_KEEPER", "Store Keeper", "General (non-drug) stores issue and indent",
            [
                PermissionRegistry.ItemView, PermissionRegistry.StockView, PermissionRegistry.StockReceive,
                PermissionRegistry.IssueView, PermissionRegistry.IssueCreate,
                PermissionRegistry.IndentView, PermissionRegistry.IndentCreate, PermissionRegistry.PharmacyReportView,
            ]),

            new("BILLING_EXECUTIVE", "Billing Executive", "Bill generation and payment collection",
                [.. billingExecutivePermissions, PermissionRegistry.DischargeClearItem, PermissionRegistry.DischargeClearBilling]),

            new("OP_BILLING", "OP Billing", "Combined OP billing: legacy OP billing transactions, masters, approvals, reports and settlement without IP/Emergency bill access",
                opBillingPermissions),

            new("BILLING_SUPERVISOR", "Billing Supervisor", "Billing HOD — cancellations, discount/refund approval, day close",
            [
                .. billingExecutivePermissions, PermissionRegistry.DischargeClearItem, PermissionRegistry.DischargeClearBilling,
                PermissionRegistry.BillCancel, PermissionRegistry.DiscountApprove,
                PermissionRegistry.RefundApprove, PermissionRegistry.AdvanceTransfer,
                PermissionRegistry.DayCloseView, PermissionRegistry.DayCloseClose, PermissionRegistry.PaymentModeManage,
                PermissionRegistry.BillParkApprove, PermissionRegistry.CreditNoteApprove,
                PermissionRegistry.BillEstimateTemplateView, PermissionRegistry.BillEstimateTemplateManage,
                PermissionRegistry.DoctorPayoutRuleView, PermissionRegistry.DoctorPayoutRuleManage,
                PermissionRegistry.CashierShiftView, PermissionRegistry.CashierShiftReceive,
                PermissionRegistry.BillingReportView,
            ]),

            new("CASHIER", "Cashier", "Payment collection at the counter",
            [
                PermissionRegistry.BillView, PermissionRegistry.BillViewOp, PermissionRegistry.BillViewIp, PermissionRegistry.BillViewEmergency,
                PermissionRegistry.BillCreateOp, PermissionRegistry.BillCreateIp, PermissionRegistry.BillCreateEmergency,
                PermissionRegistry.PaymentCollect, PermissionRegistry.AdvanceView,
                PermissionRegistry.AdvanceCollect, PermissionRegistry.DayCloseView, PermissionRegistry.CashierShiftManage,
            ]),

            new("FINANCE_EXECUTIVE", "Finance/Accounts Executive", "Vouchers, payables/receivables, ledger", financeExecutivePermissions),

            new("FINANCE_MANAGER", "Finance/Accounts Manager", "Finance HOD — voucher reversal, chart of accounts, cross-branch day close",
            [
                .. financeExecutivePermissions, PermissionRegistry.FinanceVoucherCancel, PermissionRegistry.FinanceAccountManage,
                PermissionRegistry.FinanceDayCloseConsoleView, PermissionRegistry.ReportCrossBranchView,
            ]),

            new("MRD_OFFICER", "MRD Officer", "File management, document scanning, ICD coding worklist", mrdOfficerPermissions),

            new("MRD_MANAGER", "MRD Manager", "MRD HOD — file issue/return approval, retention/destruction, discharge audits",
            [
                .. mrdOfficerPermissions, PermissionRegistry.MrdFileIssue, PermissionRegistry.MrdRetentionView, PermissionRegistry.MrdRetentionManage,
                PermissionRegistry.MrdDestructionView, PermissionRegistry.MrdDestructionManage, PermissionRegistry.MrdAuditManage,
            ]),

            new("BIOMEDICAL_ENGINEER", "Biomedical Engineer", "Equipment, AMC/CMC contracts, PM, breakdown complaints, calibration",
            [
                PermissionRegistry.BiomedEquipmentView, PermissionRegistry.BiomedEquipmentManage,
                PermissionRegistry.BiomedContractView, PermissionRegistry.BiomedContractManage,
                PermissionRegistry.BiomedPmView, PermissionRegistry.BiomedPmManage,
                PermissionRegistry.BiomedWorkOrderView, PermissionRegistry.BiomedWorkOrderManage,
                PermissionRegistry.BiomedComplaintView, PermissionRegistry.BiomedComplaintCreate, PermissionRegistry.BiomedComplaintManage,
                PermissionRegistry.BiomedCalibrationView, PermissionRegistry.BiomedCalibrationManage, PermissionRegistry.BiomedReportView,
            ]),

            new("LOYALTY_DESK", "Loyalty Desk", "Loyalty card registration, renewal/upgrade, dependents, duplicate issue and deactivation, plus loyalty masters",
            [
                PermissionRegistry.LoyaltyCardView, PermissionRegistry.LoyaltyCardCreate,
                PermissionRegistry.LoyaltyCardManage, PermissionRegistry.LoyaltyCardDeactivate,
                PermissionRegistry.LoyaltyCardCategoryView, PermissionRegistry.LoyaltyCardCategoryManage,
                PermissionRegistry.MemberRelationTagView, PermissionRegistry.MemberRelationTagManage,
                PermissionRegistry.LoyaltyCardTypeView, PermissionRegistry.LoyaltyCardTypeManage,
                PermissionRegistry.LoyaltyCardPriceView, PermissionRegistry.LoyaltyCardPriceManage,
            ]),

            new("QUALITY_MANAGER", "Quality Manager", "Indicators, audits, incident RCA, non-conformance, CAPA, risk, committees, NABH evidence",
            [
                PermissionRegistry.QualityIndicatorView, PermissionRegistry.QualityIndicatorManage, PermissionRegistry.QualityIndicatorEntryRecord,
                PermissionRegistry.QualityAuditTemplateView, PermissionRegistry.QualityAuditTemplateManage,
                PermissionRegistry.QualityAuditView, PermissionRegistry.QualityAuditManage,
                PermissionRegistry.QualityIncidentRca, PermissionRegistry.IncidentView, PermissionRegistry.IncidentCreate, PermissionRegistry.IncidentReview,
                PermissionRegistry.QualityNonConformanceView, PermissionRegistry.QualityNonConformanceCreate, PermissionRegistry.QualityNonConformanceManage,
                PermissionRegistry.QualityCapaView, PermissionRegistry.QualityCapaManage,
                PermissionRegistry.QualityRiskView, PermissionRegistry.QualityRiskManage,
                PermissionRegistry.QualityCommitteeView, PermissionRegistry.QualityCommitteeManage,
                PermissionRegistry.QualityEvidenceView, PermissionRegistry.QualityEvidenceUpload, PermissionRegistry.QualityDashboardView,
            ]),

            new("OT_COORDINATOR", "OT Coordinator", "Theatre scheduling, the WHO surgical safety checklist gate, and implant/consumable usage recording",
            [
                PermissionRegistry.OtTheatreView, PermissionRegistry.OtBookingView, PermissionRegistry.OtBookingCreate,
                PermissionRegistry.OtBookingManage, PermissionRegistry.OtChecklistManage, PermissionRegistry.OtUsageRecord,
                PermissionRegistry.OtRegisterView,
            ]),

            new("ICU_INCHARGE", "ICU Incharge", "Critical-alert threshold rules, ICU charting and dashboard",
            [
                PermissionRegistry.IcuRuleView, PermissionRegistry.IcuRuleManage, PermissionRegistry.IcuChartView, PermissionRegistry.IcuChartManage,
                PermissionRegistry.IcuVitalsRecord, PermissionRegistry.IcuAlertView, PermissionRegistry.IcuAlertAcknowledge,
                PermissionRegistry.IcuDashboardView, PermissionRegistry.BedView, PermissionRegistry.BedManage,
            ]),

            new("MANAGEMENT", "Management", "Read-only cross-branch dashboards",
            [
                PermissionRegistry.DashboardManagementView, PermissionRegistry.DashboardOpView, PermissionRegistry.DashboardIpView,
                PermissionRegistry.DashboardBillingView, PermissionRegistry.DashboardRevenueView,
                PermissionRegistry.ReportCatalogView, PermissionRegistry.ReportCrossBranchView,
            ]),

            new("INSURANCE_EXECUTIVE", "Insurance Executive", "Policy capture, pre-authorization requests and claim submission",
            insuranceExecutivePermissions),

            new("INSURANCE_SUPERVISOR", "Insurance Supervisor", "Insurance HOD — pre-auth/enhancement approval and claim settlement",
            [
                .. insuranceExecutivePermissions,
                PermissionRegistry.InsurancePreAuthApprove, PermissionRegistry.InsuranceClaimSettle,
                PermissionRegistry.ClaimDenialReasonView, PermissionRegistry.ClaimDenialReasonManage,
                PermissionRegistry.ClaimDocumentTypeView, PermissionRegistry.ClaimDocumentTypeManage,
            ]),
        ];

        // Every role gets the home Dashboard landing screen by default; a role only loses it
        // here if it has its own dedicated landing view instead (Front Office — its curated
        // Registration/Transactions/Reports/Tools tree is the landing experience; OP Billing —
        // its DefaultLandingPath points at the Billing Dashboard).
        string[] dashboardExcluded = ["FRONT_OFFICE", "OP_BILLING"];
        return
        [
            .. definitions.Select(d => dashboardExcluded.Contains(d.Code)
                ? d
                : d with { Permissions = [.. d.Permissions, PermissionRegistry.DashboardHomeView] }),
        ];
    }
}
