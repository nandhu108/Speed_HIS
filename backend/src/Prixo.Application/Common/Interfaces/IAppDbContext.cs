using Microsoft.EntityFrameworkCore;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Appointments;
using Prixo.Domain.Entities.Audit;
using Prixo.Domain.Entities.Billing;
using Prixo.Domain.Entities.Biomed;
using Prixo.Domain.Entities.Er;
using Prixo.Domain.Entities.Finance;
using Prixo.Domain.Entities.Icu;
using Prixo.Domain.Entities.Insurance;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Lab;
using Prixo.Domain.Entities.Emr;
using Prixo.Domain.Entities.Loyalty;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Mrd;
using Prixo.Domain.Entities.Nursing;
using Prixo.Domain.Entities.Ot;
using Prixo.Domain.Entities.Pharmacy;
using Prixo.Domain.Entities.Opd;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Entities.Quality;
using Prixo.Domain.Entities.Rad;
using Prixo.Domain.Entities.Reports;

namespace Prixo.Application.Common.Interfaces;

/// <summary>
/// Application-facing surface of the EF Core DbContext. DbSet properties are added
/// here as modules introduce entities, keeping Application free of Infrastructure.
/// </summary>
public interface IAppDbContext
{
    DbSet<Branch> Branches { get; }
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserPermissionOverride> UserPermissionOverrides { get; }
    DbSet<RolePreset> RolePresets { get; }
    DbSet<RolePresetRole> RolePresetRoles { get; }
    DbSet<UserBranch> UserBranches { get; }
    DbSet<Designation> Designations { get; }
    DbSet<Menu> Menus { get; }
    DbSet<MenuRoleLayout> MenuRoleLayouts { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<UserSession> UserSessions { get; }
    DbSet<SystemSetting> SystemSettings { get; }
    DbSet<PasswordHistory> PasswordHistories { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<ActivityLog> ActivityLogs { get; }

    // Masters (Phase 2)
    DbSet<Department> Departments { get; }
    DbSet<Specialty> Specialties { get; }
    DbSet<PatientCategory> PatientCategories { get; }
    DbSet<Doctor> Doctors { get; }
    DbSet<DoctorDepartment> DoctorDepartments { get; }
    DbSet<Employee> Employees { get; }
    DbSet<ServiceCategory> ServiceCategories { get; }
    DbSet<Service> Services { get; }
    DbSet<Tariff> Tariffs { get; }
    DbSet<TariffItem> TariffItems { get; }
    DbSet<Package> Packages { get; }
    DbSet<PackageItem> PackageItems { get; }
    DbSet<BillEstimateTemplate> BillEstimateTemplates { get; }
    DbSet<BillEstimateTemplateItem> BillEstimateTemplateItems { get; }
    DbSet<DoctorPayoutRule> DoctorPayoutRules { get; }
    DbSet<Building> Buildings { get; }
    DbSet<Floor> Floors { get; }
    DbSet<RoomType> RoomTypes { get; }
    DbSet<RoomTypeCharge> RoomTypeCharges { get; }
    DbSet<Ward> Wards { get; }
    DbSet<Room> Rooms { get; }
    DbSet<Bed> Beds { get; }
    DbSet<Vendor> Vendors { get; }
    DbSet<ItemCategory> ItemCategories { get; }
    DbSet<Item> Items { get; }
    DbSet<Generic> Generics { get; }
    DbSet<Manufacturer> Manufacturers { get; }
    DbSet<Drug> Drugs { get; }
    DbSet<Tax> Taxes { get; }
    DbSet<DiscountType> DiscountTypes { get; }
    DbSet<Referrer> Referrers { get; }
    DbSet<IcdCode> IcdCodes { get; }
    DbSet<Corporate> Corporates { get; }
    DbSet<InsuranceCompany> InsuranceCompanies { get; }
    DbSet<Uom> Uoms { get; }
    DbSet<SampleType> SampleTypes { get; }
    DbSet<Modality> Modalities { get; }
    DbSet<ConsentTemplate> ConsentTemplates { get; }
    DbSet<RecordDocumentType> RecordDocumentTypes { get; }
    DbSet<Camp> Camps { get; }
    DbSet<State> States { get; }
    DbSet<City> Cities { get; }
    DbSet<Nationality> Nationalities { get; }
    DbSet<Religion> Religions { get; }
    DbSet<Language> Languages { get; }
    DbSet<SpeciallyAbledCategory> SpeciallyAbledCategories { get; }
    DbSet<PatientHcTag> PatientHcTags { get; }
    DbSet<PatientCaseType> PatientCaseTypes { get; }
    DbSet<MlcType> MlcTypes { get; }
    DbSet<MhcDoctor> MhcDoctors { get; }
    DbSet<MhcPackage> MhcPackages { get; }
    DbSet<LoyaltyProgram> LoyaltyPrograms { get; }
    DbSet<Unit> Units { get; }
    DbSet<UnitDoctor> UnitDoctors { get; }
    DbSet<AppointmentType> AppointmentTypes { get; }
    DbSet<Area> Areas { get; }
    DbSet<LoyaltyCardCategory> LoyaltyCardCategories { get; }
    DbSet<MemberRelationTag> MemberRelationTags { get; }
    DbSet<LoyaltyCardType> LoyaltyCardTypes { get; }
    DbSet<LoyaltyCardPrice> LoyaltyCardPrices { get; }
    DbSet<LoyaltyCardPriceItem> LoyaltyCardPriceItems { get; }
    DbSet<Salutation> Salutations { get; }
    DbSet<MaritalStatus> MaritalStatuses { get; }
    DbSet<IdentityDocumentType> IdentityDocumentTypes { get; }
    DbSet<VisitClass> VisitClasses { get; }
    DbSet<VisitSession> VisitSessions { get; }
    DbSet<ClaimDocumentType> ClaimDocumentTypes { get; }
    DbSet<ClaimDenialReason> ClaimDenialReasons { get; }

    // Loyalty (front-office extension)
    DbSet<LoyaltyCard> LoyaltyCards { get; }
    DbSet<LoyaltyCardMember> LoyaltyCardMembers { get; }

    // Patients (Phase 3)
    DbSet<Patient> Patients { get; }
    DbSet<PatientContact> PatientContacts { get; }
    DbSet<PatientIdentifier> PatientIdentifiers { get; }
    DbSet<AbhaProfile> AbhaProfiles { get; }
    DbSet<AbdmVerificationSession> AbdmVerificationSessions { get; }
    DbSet<AbdmCareContext> AbdmCareContexts { get; }
    DbSet<AbdmDataTransferLog> AbdmDataTransferLogs { get; }
    DbSet<AbdmConsentRequest> AbdmConsentRequests { get; }
    DbSet<AbdmExternalHealthRecord> AbdmExternalHealthRecords { get; }
    DbSet<PatientDocument> PatientDocuments { get; }
    DbSet<PatientConsent> PatientConsents { get; }
    DbSet<PatientMerge> PatientMerges { get; }

    // Patient Portal (Phase 21)
    DbSet<PortalAccount> PortalAccounts { get; }
    DbSet<PortalRefreshToken> PortalRefreshTokens { get; }
    DbSet<Feedback> Feedbacks { get; }

    // Appointments (Phase 4)
    DbSet<DoctorSchedule> DoctorSchedules { get; }
    DbSet<ScheduleOverride> ScheduleOverrides { get; }
    DbSet<UnitSchedule> UnitSchedules { get; }
    DbSet<Appointment> Appointments { get; }
    DbSet<NotificationQueue> NotificationQueue { get; }

    // OPD (Phase 5)
    DbSet<OpVisit> OpVisits { get; }
    DbSet<VisitVitals> VisitVitals { get; }
    DbSet<VisitComplaint> VisitComplaints { get; }
    DbSet<VisitExamination> VisitExaminations { get; }
    DbSet<VisitDiagnosis> VisitDiagnoses { get; }
    DbSet<Prescription> Prescriptions { get; }
    DbSet<PrescriptionItem> PrescriptionItems { get; }
    DbSet<PrescriptionTemplate> PrescriptionTemplates { get; }
    DbSet<PrescriptionTemplateItem> PrescriptionTemplateItems { get; }
    DbSet<ClinicalOrder> ClinicalOrders { get; }
    DbSet<VisitReferral> VisitReferrals { get; }
    DbSet<FollowUpAdvice> FollowUpAdvices { get; }

    // Billing (Phase 6)
    DbSet<PaymentMode> PaymentModes { get; }
    DbSet<Bill> Bills { get; }
    DbSet<BillItem> BillItems { get; }
    DbSet<BillPayment> BillPayments { get; }
    DbSet<Refund> Refunds { get; }
    DbSet<DiscountApproval> DiscountApprovals { get; }
    DbSet<Advance> Advances { get; }
    DbSet<DayClose> DayCloses { get; }
    DbSet<DayCloseModeAmount> DayCloseModeAmounts { get; }
    DbSet<OnlinePaymentRequest> OnlinePaymentRequests { get; }
    DbSet<BillNote> BillNotes { get; }
    DbSet<BillCreditNote> BillCreditNotes { get; }
    DbSet<BillCreditNoteItem> BillCreditNoteItems { get; }
    DbSet<CashierShift> CashierShifts { get; }
    DbSet<CashierShiftModeAmount> CashierShiftModeAmounts { get; }
    DbSet<BillEstimate> BillEstimates { get; }
    DbSet<BillEstimateItem> BillEstimateItems { get; }

    // Lab (Phase 7)
    DbSet<LabTest> LabTests { get; }
    DbSet<LabTestParameter> LabTestParameters { get; }
    DbSet<ReferenceRange> ReferenceRanges { get; }
    DbSet<TestProfile> TestProfiles { get; }
    DbSet<ProfileTest> ProfileTests { get; }
    DbSet<LabOrder> LabOrders { get; }
    DbSet<LabOrderItem> LabOrderItems { get; }
    DbSet<Sample> Samples { get; }
    DbSet<LabResult> LabResults { get; }
    DbSet<ResultValue> ResultValues { get; }
    DbSet<CriticalAlert> CriticalAlerts { get; }

    // Pharmacy (Phase 8)
    DbSet<Store> Stores { get; }
    DbSet<Rack> Racks { get; }
    DbSet<Batch> Batches { get; }
    DbSet<StockLedger> StockLedgers { get; }
    DbSet<PurchaseRequest> PurchaseRequests { get; }
    DbSet<PurchaseRequestItem> PurchaseRequestItems { get; }
    DbSet<PurchaseOrder> PurchaseOrders { get; }
    DbSet<PurchaseOrderItem> PurchaseOrderItems { get; }
    DbSet<Grn> Grns { get; }
    DbSet<GrnItem> GrnItems { get; }
    DbSet<PurchaseReturn> PurchaseReturns { get; }
    DbSet<PurchaseReturnItem> PurchaseReturnItems { get; }
    DbSet<PharmacySale> PharmacySales { get; }
    DbSet<PharmacySaleItem> PharmacySaleItems { get; }
    DbSet<SalesReturn> SalesReturns { get; }
    DbSet<SalesReturnItem> SalesReturnItems { get; }
    DbSet<Issue> Issues { get; }
    DbSet<IssueItem> IssueItems { get; }
    DbSet<IssueReturn> IssueReturns { get; }
    DbSet<IssueReturnItem> IssueReturnItems { get; }
    DbSet<StockAdjustment> StockAdjustments { get; }
    DbSet<StockAdjustmentItem> StockAdjustmentItems { get; }
    DbSet<StockTransfer> StockTransfers { get; }
    DbSet<StockTransferItem> StockTransferItems { get; }
    DbSet<StockAudit> StockAudits { get; }
    DbSet<StockAuditItem> StockAuditItems { get; }

    // Stores & Inventory (Phase 13)
    DbSet<Indent> Indents { get; }
    DbSet<IndentItem> IndentItems { get; }

    // IPD (Phase 9)
    DbSet<Admission> Admissions { get; }
    DbSet<AdmissionConsultant> AdmissionConsultants { get; }
    DbSet<BedAllocation> BedAllocations { get; }
    DbSet<DoctorOrder> DoctorOrders { get; }
    DbSet<MedicationOrder> MedicationOrders { get; }
    DbSet<DietOrder> DietOrders { get; }
    DbSet<ProgressNote> ProgressNotes { get; }
    DbSet<IpVitals> IpVitals { get; }
    DbSet<IntakeOutputEntry> IntakeOutputEntries { get; }
    DbSet<ProcedureNote> ProcedureNotes { get; }
    DbSet<DischargeClearance> DischargeClearances { get; }
    DbSet<DischargeSummary> DischargeSummaries { get; }
    DbSet<AdmissionRequest> AdmissionRequests { get; }

    // Nursing (Phase 10)
    DbSet<NursingAssessment> NursingAssessments { get; }
    DbSet<NursingNote> NursingNotes { get; }
    DbSet<RiskAssessment> RiskAssessments { get; }
    DbSet<CarePlan> CarePlans { get; }
    DbSet<TransferNote> TransferNotes { get; }
    DbSet<MarSchedule> MarSchedules { get; }
    DbSet<MarAdministration> MarAdministrations { get; }
    DbSet<ShiftHandover> ShiftHandovers { get; }
    DbSet<PatientHandoverNote> PatientHandoverNotes { get; }
    DbSet<Incident> Incidents { get; }

    // EMR (Phase 11)
    DbSet<MedicalHistory> MedicalHistories { get; }
    DbSet<SurgicalHistory> SurgicalHistories { get; }
    DbSet<FamilyHistory> FamilyHistories { get; }
    DbSet<Allergy> Allergies { get; }
    DbSet<Immunization> Immunizations { get; }
    DbSet<ProblemListEntry> ProblemListEntries { get; }
    DbSet<ClinicalNote> ClinicalNotes { get; }
    DbSet<Attachment> Attachments { get; }

    // Radiology (Phase 12)
    DbSet<RadiologyTest> RadiologyTests { get; }
    DbSet<RadReportTemplate> RadReportTemplates { get; }
    DbSet<RadOrder> RadOrders { get; }
    DbSet<RadOrderItem> RadOrderItems { get; }
    DbSet<RadSchedule> RadSchedules { get; }
    DbSet<ContrastConsent> ContrastConsents { get; }
    DbSet<RadReport> RadReports { get; }

    // Operation Theatre (Phase 14)
    DbSet<Theatre> Theatres { get; }
    DbSet<Equipment> Equipment { get; }
    DbSet<SurgeryBooking> SurgeryBookings { get; }
    DbSet<SurgeryTeamMember> SurgeryTeamMembers { get; }
    DbSet<SurgicalSafetyChecklist> SurgicalSafetyChecklists { get; }
    DbSet<PreAnaesthesiaAssessment> PreAnaesthesiaAssessments { get; }
    DbSet<IntraOpAnaesthesiaRecord> IntraOpAnaesthesiaRecords { get; }
    DbSet<OperativeNote> OperativeNotes { get; }
    DbSet<PostOpNote> PostOpNotes { get; }
    DbSet<ImplantUsage> ImplantUsages { get; }
    DbSet<ConsumableUsage> ConsumableUsages { get; }

    // ICU / Critical Care (Phase 15)
    DbSet<VentilatorSession> VentilatorSessions { get; }
    DbSet<VentilatorObservation> VentilatorObservations { get; }
    DbSet<InfusionChartEntry> InfusionChartEntries { get; }
    DbSet<InfusionRateEntry> InfusionRateEntries { get; }
    DbSet<IcuSeverityAssessment> IcuSeverityAssessments { get; }
    DbSet<CriticalAlertRule> CriticalAlertRules { get; }
    DbSet<IcuVitalsAlert> IcuVitalsAlerts { get; }
    DbSet<DeviceReading> DeviceReadings { get; }

    // Emergency / Casualty (Phase 16)
    DbSet<EmergencyVisit> EmergencyVisits { get; }
    DbSet<Triage> Triages { get; }
    DbSet<EmergencyAssessment> EmergencyAssessments { get; }
    DbSet<MlcRecord> MlcRecords { get; }
    DbSet<AccidentCaseRecord> AccidentCaseRecords { get; }
    DbSet<ObservationAllocation> ObservationAllocations { get; }

    // MRD (Phase 17)
    DbSet<RecordFile> RecordFiles { get; }
    DbSet<FileMovement> FileMovements { get; }
    DbSet<FileRequest> FileRequests { get; }
    DbSet<ScannedDocument> ScannedDocuments { get; }
    DbSet<CodingWorklistEntry> CodingWorklistEntries { get; }
    DbSet<FileAudit> FileAudits { get; }
    DbSet<RetentionPolicy> RetentionPolicies { get; }
    DbSet<DestructionRegisterEntry> DestructionRegisterEntries { get; }

    // Finance (Phase 18)
    DbSet<Account> Accounts { get; }
    DbSet<Voucher> Vouchers { get; }
    DbSet<VoucherLine> VoucherLines { get; }
    DbSet<LedgerEntry> LedgerEntries { get; }
    DbSet<Payable> Payables { get; }
    DbSet<Receivable> Receivables { get; }

    // Biomedical Engineering (Phase 19)
    DbSet<BiomedEquipment> BiomedEquipment { get; }
    DbSet<EquipmentLocationLog> EquipmentLocationLogs { get; }
    DbSet<AmcContract> AmcContracts { get; }
    DbSet<PmSchedule> PmSchedules { get; }
    DbSet<WorkOrder> WorkOrders { get; }
    DbSet<BreakdownComplaint> BreakdownComplaints { get; }
    DbSet<Calibration> Calibrations { get; }
    DbSet<ServiceHistoryEntry> ServiceHistoryEntries { get; }
    DbSet<SpareUsage> SpareUsages { get; }

    // Quality / NABH (Phase 20)
    DbSet<QualityIndicator> QualityIndicators { get; }
    DbSet<QualityIndicatorEntry> QualityIndicatorEntries { get; }
    DbSet<QualityAuditTemplate> QualityAuditTemplates { get; }
    DbSet<QualityAuditTemplateItem> QualityAuditTemplateItems { get; }
    DbSet<QualityAudit> QualityAudits { get; }
    DbSet<QualityAuditResponse> QualityAuditResponses { get; }
    DbSet<QualityNonConformance> QualityNonConformances { get; }
    DbSet<QualityRisk> QualityRisks { get; }
    DbSet<QualityCapa> QualityCapas { get; }
    DbSet<QualityCommittee> QualityCommittees { get; }
    DbSet<QualityCommitteeMeeting> QualityCommitteeMeetings { get; }
    DbSet<QualityCommitteeActionItem> QualityCommitteeActionItems { get; }
    DbSet<QualityNabhStandard> QualityNabhStandards { get; }
    DbSet<QualityEvidence> QualityEvidence { get; }

    // Reports & Dashboards (Phase 23)
    DbSet<ReportDefinition> ReportDefinitions { get; }
    DbSet<ReportSubscription> ReportSubscriptions { get; }
    DbSet<RevenueDailyRow> RevenueDailyRows { get; }
    DbSet<DailyCollectionRow> DailyCollectionRows { get; }
    DbSet<OpCensusRow> OpCensusRows { get; }

    // Insurance/TPA (Phase 24)
    DbSet<PatientInsurancePolicy> PatientInsurancePolicies { get; }
    DbSet<PreAuthRequest> PreAuthRequests { get; }
    DbSet<PreAuthEnhancement> PreAuthEnhancements { get; }
    DbSet<Claim> Claims { get; }
    DbSet<ClaimDocument> ClaimDocuments { get; }

    /// <summary>Generic accessor used by the simple-master CRUD framework.</summary>
    DbSet<TEntity> Set<TEntity>() where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
