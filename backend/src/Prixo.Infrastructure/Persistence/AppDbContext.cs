using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Common;
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
using Prixo.Domain.Entities.Loyalty;
using Prixo.Domain.Entities.Emr;
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

namespace Prixo.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser? currentUser = null)
    : DbContext(options), IAppDbContext
{
    /// <summary>
    /// Branch of the current request; null outside HTTP (seeding, jobs, tests), which
    /// disables branch filtering. Referenced by the global query filters below — EF
    /// rewrites the captured context instance per query, so this is evaluated live.
    /// </summary>
    public long? CurrentBranchId => currentUser?.BranchId;

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserPermissionOverride> UserPermissionOverrides => Set<UserPermissionOverride>();
    public DbSet<RolePreset> RolePresets => Set<RolePreset>();
    public DbSet<RolePresetRole> RolePresetRoles => Set<RolePresetRole>();
    public DbSet<UserBranch> UserBranches => Set<UserBranch>();
    public DbSet<Designation> Designations => Set<Designation>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<MenuRoleLayout> MenuRoleLayouts => Set<MenuRoleLayout>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    // Masters (Phase 2)
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<PatientCategory> PatientCategories => Set<PatientCategory>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DoctorDepartment> DoctorDepartments => Set<DoctorDepartment>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Tariff> Tariffs => Set<Tariff>();
    public DbSet<TariffItem> TariffItems => Set<TariffItem>();
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<PackageItem> PackageItems => Set<PackageItem>();
    public DbSet<BillEstimateTemplate> BillEstimateTemplates => Set<BillEstimateTemplate>();
    public DbSet<BillEstimateTemplateItem> BillEstimateTemplateItems => Set<BillEstimateTemplateItem>();
    public DbSet<DoctorPayoutRule> DoctorPayoutRules => Set<DoctorPayoutRule>();
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<RoomTypeCharge> RoomTypeCharges => Set<RoomTypeCharge>();
    public DbSet<Ward> Wards => Set<Ward>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Bed> Beds => Set<Bed>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<ItemCategory> ItemCategories => Set<ItemCategory>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Generic> Generics => Set<Generic>();
    public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();
    public DbSet<Drug> Drugs => Set<Drug>();
    public DbSet<Tax> Taxes => Set<Tax>();
    public DbSet<DiscountType> DiscountTypes => Set<DiscountType>();
    public DbSet<Referrer> Referrers => Set<Referrer>();
    public DbSet<IcdCode> IcdCodes => Set<IcdCode>();
    public DbSet<Corporate> Corporates => Set<Corporate>();
    public DbSet<InsuranceCompany> InsuranceCompanies => Set<InsuranceCompany>();
    public DbSet<Uom> Uoms => Set<Uom>();
    public DbSet<SampleType> SampleTypes => Set<SampleType>();
    public DbSet<Modality> Modalities => Set<Modality>();
    public DbSet<ConsentTemplate> ConsentTemplates => Set<ConsentTemplate>();
    public DbSet<RecordDocumentType> RecordDocumentTypes => Set<RecordDocumentType>();
    public DbSet<Camp> Camps => Set<Camp>();
    public DbSet<State> States => Set<State>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Nationality> Nationalities => Set<Nationality>();
    public DbSet<Religion> Religions => Set<Religion>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<SpeciallyAbledCategory> SpeciallyAbledCategories => Set<SpeciallyAbledCategory>();
    public DbSet<PatientHcTag> PatientHcTags => Set<PatientHcTag>();
    public DbSet<PatientCaseType> PatientCaseTypes => Set<PatientCaseType>();
    public DbSet<MlcType> MlcTypes => Set<MlcType>();
    public DbSet<MhcDoctor> MhcDoctors => Set<MhcDoctor>();
    public DbSet<MhcPackage> MhcPackages => Set<MhcPackage>();
    public DbSet<LoyaltyProgram> LoyaltyPrograms => Set<LoyaltyProgram>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<UnitDoctor> UnitDoctors => Set<UnitDoctor>();
    public DbSet<AppointmentType> AppointmentTypes => Set<AppointmentType>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<LoyaltyCardCategory> LoyaltyCardCategories => Set<LoyaltyCardCategory>();
    public DbSet<MemberRelationTag> MemberRelationTags => Set<MemberRelationTag>();
    public DbSet<LoyaltyCardType> LoyaltyCardTypes => Set<LoyaltyCardType>();
    public DbSet<LoyaltyCardPrice> LoyaltyCardPrices => Set<LoyaltyCardPrice>();
    public DbSet<LoyaltyCardPriceItem> LoyaltyCardPriceItems => Set<LoyaltyCardPriceItem>();
    public DbSet<Salutation> Salutations => Set<Salutation>();
    public DbSet<MaritalStatus> MaritalStatuses => Set<MaritalStatus>();
    public DbSet<IdentityDocumentType> IdentityDocumentTypes => Set<IdentityDocumentType>();
    public DbSet<VisitClass> VisitClasses => Set<VisitClass>();
    public DbSet<VisitSession> VisitSessions => Set<VisitSession>();
    public DbSet<ClaimDocumentType> ClaimDocumentTypes => Set<ClaimDocumentType>();
    public DbSet<ClaimDenialReason> ClaimDenialReasons => Set<ClaimDenialReason>();

    // Loyalty (front-office extension)
    public DbSet<LoyaltyCard> LoyaltyCards => Set<LoyaltyCard>();
    public DbSet<LoyaltyCardMember> LoyaltyCardMembers => Set<LoyaltyCardMember>();

    // Patients (Phase 3)
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientContact> PatientContacts => Set<PatientContact>();
    public DbSet<PatientIdentifier> PatientIdentifiers => Set<PatientIdentifier>();
    public DbSet<AbhaProfile> AbhaProfiles => Set<AbhaProfile>();
    public DbSet<AbdmVerificationSession> AbdmVerificationSessions => Set<AbdmVerificationSession>();
    public DbSet<AbdmCareContext> AbdmCareContexts => Set<AbdmCareContext>();
    public DbSet<AbdmDataTransferLog> AbdmDataTransferLogs => Set<AbdmDataTransferLog>();
    public DbSet<AbdmConsentRequest> AbdmConsentRequests => Set<AbdmConsentRequest>();
    public DbSet<AbdmExternalHealthRecord> AbdmExternalHealthRecords => Set<AbdmExternalHealthRecord>();
    public DbSet<PatientDocument> PatientDocuments => Set<PatientDocument>();
    public DbSet<PatientConsent> PatientConsents => Set<PatientConsent>();
    public DbSet<PatientMerge> PatientMerges => Set<PatientMerge>();

    // Patient Portal (Phase 21)
    public DbSet<PortalAccount> PortalAccounts => Set<PortalAccount>();
    public DbSet<PortalRefreshToken> PortalRefreshTokens => Set<PortalRefreshToken>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<SequenceCounter> SequenceCounters => Set<SequenceCounter>();

    // Appointments (Phase 4)
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<ScheduleOverride> ScheduleOverrides => Set<ScheduleOverride>();
    public DbSet<UnitSchedule> UnitSchedules => Set<UnitSchedule>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<NotificationQueue> NotificationQueue => Set<NotificationQueue>();

    // OPD (Phase 5)
    public DbSet<OpVisit> OpVisits => Set<OpVisit>();
    public DbSet<VisitVitals> VisitVitals => Set<VisitVitals>();
    public DbSet<VisitComplaint> VisitComplaints => Set<VisitComplaint>();
    public DbSet<VisitExamination> VisitExaminations => Set<VisitExamination>();
    public DbSet<VisitDiagnosis> VisitDiagnoses => Set<VisitDiagnosis>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<PrescriptionTemplate> PrescriptionTemplates => Set<PrescriptionTemplate>();
    public DbSet<PrescriptionTemplateItem> PrescriptionTemplateItems => Set<PrescriptionTemplateItem>();
    public DbSet<ClinicalOrder> ClinicalOrders => Set<ClinicalOrder>();
    public DbSet<VisitReferral> VisitReferrals => Set<VisitReferral>();
    public DbSet<FollowUpAdvice> FollowUpAdvices => Set<FollowUpAdvice>();

    // Billing (Phase 6)
    public DbSet<PaymentMode> PaymentModes => Set<PaymentMode>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<BillItem> BillItems => Set<BillItem>();
    public DbSet<BillPayment> BillPayments => Set<BillPayment>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<DiscountApproval> DiscountApprovals => Set<DiscountApproval>();
    public DbSet<Advance> Advances => Set<Advance>();
    public DbSet<DayClose> DayCloses => Set<DayClose>();
    public DbSet<DayCloseModeAmount> DayCloseModeAmounts => Set<DayCloseModeAmount>();
    public DbSet<OnlinePaymentRequest> OnlinePaymentRequests => Set<OnlinePaymentRequest>();
    public DbSet<BillNote> BillNotes => Set<BillNote>();
    public DbSet<BillCreditNote> BillCreditNotes => Set<BillCreditNote>();
    public DbSet<BillCreditNoteItem> BillCreditNoteItems => Set<BillCreditNoteItem>();
    public DbSet<CashierShift> CashierShifts => Set<CashierShift>();
    public DbSet<CashierShiftModeAmount> CashierShiftModeAmounts => Set<CashierShiftModeAmount>();
    public DbSet<BillEstimate> BillEstimates => Set<BillEstimate>();
    public DbSet<BillEstimateItem> BillEstimateItems => Set<BillEstimateItem>();

    // Lab (Phase 7)
    public DbSet<LabTest> LabTests => Set<LabTest>();
    public DbSet<LabTestParameter> LabTestParameters => Set<LabTestParameter>();
    public DbSet<ReferenceRange> ReferenceRanges => Set<ReferenceRange>();
    public DbSet<TestProfile> TestProfiles => Set<TestProfile>();
    public DbSet<ProfileTest> ProfileTests => Set<ProfileTest>();
    public DbSet<LabOrder> LabOrders => Set<LabOrder>();
    public DbSet<LabOrderItem> LabOrderItems => Set<LabOrderItem>();
    public DbSet<Sample> Samples => Set<Sample>();
    public DbSet<LabResult> LabResults => Set<LabResult>();
    public DbSet<ResultValue> ResultValues => Set<ResultValue>();
    public DbSet<CriticalAlert> CriticalAlerts => Set<CriticalAlert>();

    // Pharmacy (Phase 8)
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Rack> Racks => Set<Rack>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<StockLedger> StockLedgers => Set<StockLedger>();
    public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();
    public DbSet<PurchaseRequestItem> PurchaseRequestItems => Set<PurchaseRequestItem>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<Grn> Grns => Set<Grn>();
    public DbSet<GrnItem> GrnItems => Set<GrnItem>();
    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();
    public DbSet<PurchaseReturnItem> PurchaseReturnItems => Set<PurchaseReturnItem>();
    public DbSet<PharmacySale> PharmacySales => Set<PharmacySale>();
    public DbSet<PharmacySaleItem> PharmacySaleItems => Set<PharmacySaleItem>();
    public DbSet<SalesReturn> SalesReturns => Set<SalesReturn>();
    public DbSet<SalesReturnItem> SalesReturnItems => Set<SalesReturnItem>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<IssueItem> IssueItems => Set<IssueItem>();
    public DbSet<IssueReturn> IssueReturns => Set<IssueReturn>();
    public DbSet<IssueReturnItem> IssueReturnItems => Set<IssueReturnItem>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<StockAdjustmentItem> StockAdjustmentItems => Set<StockAdjustmentItem>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();
    public DbSet<StockAudit> StockAudits => Set<StockAudit>();
    public DbSet<StockAuditItem> StockAuditItems => Set<StockAuditItem>();

    // Stores & Inventory (Phase 13)
    public DbSet<Indent> Indents => Set<Indent>();
    public DbSet<IndentItem> IndentItems => Set<IndentItem>();

    // IPD (Phase 9)
    public DbSet<Admission> Admissions => Set<Admission>();
    public DbSet<AdmissionConsultant> AdmissionConsultants => Set<AdmissionConsultant>();
    public DbSet<BedAllocation> BedAllocations => Set<BedAllocation>();
    public DbSet<DoctorOrder> DoctorOrders => Set<DoctorOrder>();
    public DbSet<MedicationOrder> MedicationOrders => Set<MedicationOrder>();
    public DbSet<DietOrder> DietOrders => Set<DietOrder>();
    public DbSet<ProgressNote> ProgressNotes => Set<ProgressNote>();
    public DbSet<IpVitals> IpVitals => Set<IpVitals>();
    public DbSet<IntakeOutputEntry> IntakeOutputEntries => Set<IntakeOutputEntry>();
    public DbSet<ProcedureNote> ProcedureNotes => Set<ProcedureNote>();
    public DbSet<DischargeClearance> DischargeClearances => Set<DischargeClearance>();
    public DbSet<DischargeSummary> DischargeSummaries => Set<DischargeSummary>();
    public DbSet<AdmissionRequest> AdmissionRequests => Set<AdmissionRequest>();

    // Nursing (Phase 10)
    public DbSet<NursingAssessment> NursingAssessments => Set<NursingAssessment>();
    public DbSet<NursingNote> NursingNotes => Set<NursingNote>();
    public DbSet<RiskAssessment> RiskAssessments => Set<RiskAssessment>();
    public DbSet<CarePlan> CarePlans => Set<CarePlan>();
    public DbSet<TransferNote> TransferNotes => Set<TransferNote>();
    public DbSet<MarSchedule> MarSchedules => Set<MarSchedule>();
    public DbSet<MarAdministration> MarAdministrations => Set<MarAdministration>();
    public DbSet<ShiftHandover> ShiftHandovers => Set<ShiftHandover>();
    public DbSet<PatientHandoverNote> PatientHandoverNotes => Set<PatientHandoverNote>();
    public DbSet<Incident> Incidents => Set<Incident>();

    // EMR (Phase 11)
    public DbSet<MedicalHistory> MedicalHistories => Set<MedicalHistory>();
    public DbSet<SurgicalHistory> SurgicalHistories => Set<SurgicalHistory>();
    public DbSet<FamilyHistory> FamilyHistories => Set<FamilyHistory>();
    public DbSet<Allergy> Allergies => Set<Allergy>();
    public DbSet<Immunization> Immunizations => Set<Immunization>();
    public DbSet<ProblemListEntry> ProblemListEntries => Set<ProblemListEntry>();
    public DbSet<ClinicalNote> ClinicalNotes => Set<ClinicalNote>();
    public DbSet<Attachment> Attachments => Set<Attachment>();

    // Radiology (Phase 12)
    public DbSet<RadiologyTest> RadiologyTests => Set<RadiologyTest>();
    public DbSet<RadReportTemplate> RadReportTemplates => Set<RadReportTemplate>();
    public DbSet<RadOrder> RadOrders => Set<RadOrder>();
    public DbSet<RadOrderItem> RadOrderItems => Set<RadOrderItem>();
    public DbSet<RadSchedule> RadSchedules => Set<RadSchedule>();
    public DbSet<ContrastConsent> ContrastConsents => Set<ContrastConsent>();
    public DbSet<RadReport> RadReports => Set<RadReport>();

    // Operation Theatre (Phase 14)
    public DbSet<Theatre> Theatres => Set<Theatre>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<SurgeryBooking> SurgeryBookings => Set<SurgeryBooking>();
    public DbSet<SurgeryTeamMember> SurgeryTeamMembers => Set<SurgeryTeamMember>();
    public DbSet<SurgicalSafetyChecklist> SurgicalSafetyChecklists => Set<SurgicalSafetyChecklist>();
    public DbSet<PreAnaesthesiaAssessment> PreAnaesthesiaAssessments => Set<PreAnaesthesiaAssessment>();
    public DbSet<IntraOpAnaesthesiaRecord> IntraOpAnaesthesiaRecords => Set<IntraOpAnaesthesiaRecord>();
    public DbSet<OperativeNote> OperativeNotes => Set<OperativeNote>();
    public DbSet<PostOpNote> PostOpNotes => Set<PostOpNote>();
    public DbSet<ImplantUsage> ImplantUsages => Set<ImplantUsage>();
    public DbSet<ConsumableUsage> ConsumableUsages => Set<ConsumableUsage>();

    // ICU / Critical Care (Phase 15)
    public DbSet<VentilatorSession> VentilatorSessions => Set<VentilatorSession>();
    public DbSet<VentilatorObservation> VentilatorObservations => Set<VentilatorObservation>();
    public DbSet<InfusionChartEntry> InfusionChartEntries => Set<InfusionChartEntry>();
    public DbSet<InfusionRateEntry> InfusionRateEntries => Set<InfusionRateEntry>();
    public DbSet<IcuSeverityAssessment> IcuSeverityAssessments => Set<IcuSeverityAssessment>();
    public DbSet<CriticalAlertRule> CriticalAlertRules => Set<CriticalAlertRule>();
    public DbSet<IcuVitalsAlert> IcuVitalsAlerts => Set<IcuVitalsAlert>();
    public DbSet<DeviceReading> DeviceReadings => Set<DeviceReading>();

    // Emergency / Casualty (Phase 16)
    public DbSet<EmergencyVisit> EmergencyVisits => Set<EmergencyVisit>();
    public DbSet<Triage> Triages => Set<Triage>();
    public DbSet<EmergencyAssessment> EmergencyAssessments => Set<EmergencyAssessment>();
    public DbSet<MlcRecord> MlcRecords => Set<MlcRecord>();
    public DbSet<AccidentCaseRecord> AccidentCaseRecords => Set<AccidentCaseRecord>();
    public DbSet<ObservationAllocation> ObservationAllocations => Set<ObservationAllocation>();

    // MRD (Phase 17)
    public DbSet<RecordFile> RecordFiles => Set<RecordFile>();
    public DbSet<FileMovement> FileMovements => Set<FileMovement>();
    public DbSet<FileRequest> FileRequests => Set<FileRequest>();
    public DbSet<ScannedDocument> ScannedDocuments => Set<ScannedDocument>();
    public DbSet<CodingWorklistEntry> CodingWorklistEntries => Set<CodingWorklistEntry>();
    public DbSet<FileAudit> FileAudits => Set<FileAudit>();
    public DbSet<RetentionPolicy> RetentionPolicies => Set<RetentionPolicy>();
    public DbSet<DestructionRegisterEntry> DestructionRegisterEntries => Set<DestructionRegisterEntry>();

    // Finance (Phase 18)
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<VoucherLine> VoucherLines => Set<VoucherLine>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<Payable> Payables => Set<Payable>();
    public DbSet<Receivable> Receivables => Set<Receivable>();

    // Biomedical Engineering (Phase 19)
    public DbSet<BiomedEquipment> BiomedEquipment => Set<BiomedEquipment>();
    public DbSet<EquipmentLocationLog> EquipmentLocationLogs => Set<EquipmentLocationLog>();
    public DbSet<AmcContract> AmcContracts => Set<AmcContract>();
    public DbSet<PmSchedule> PmSchedules => Set<PmSchedule>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<BreakdownComplaint> BreakdownComplaints => Set<BreakdownComplaint>();
    public DbSet<Calibration> Calibrations => Set<Calibration>();
    public DbSet<ServiceHistoryEntry> ServiceHistoryEntries => Set<ServiceHistoryEntry>();
    public DbSet<SpareUsage> SpareUsages => Set<SpareUsage>();

    // Quality / NABH (Phase 20)
    public DbSet<QualityIndicator> QualityIndicators => Set<QualityIndicator>();
    public DbSet<QualityIndicatorEntry> QualityIndicatorEntries => Set<QualityIndicatorEntry>();
    public DbSet<QualityAuditTemplate> QualityAuditTemplates => Set<QualityAuditTemplate>();
    public DbSet<QualityAuditTemplateItem> QualityAuditTemplateItems => Set<QualityAuditTemplateItem>();
    public DbSet<QualityAudit> QualityAudits => Set<QualityAudit>();
    public DbSet<QualityAuditResponse> QualityAuditResponses => Set<QualityAuditResponse>();
    public DbSet<QualityNonConformance> QualityNonConformances => Set<QualityNonConformance>();
    public DbSet<QualityRisk> QualityRisks => Set<QualityRisk>();
    public DbSet<QualityCapa> QualityCapas => Set<QualityCapa>();
    public DbSet<QualityCommittee> QualityCommittees => Set<QualityCommittee>();
    public DbSet<QualityCommitteeMeeting> QualityCommitteeMeetings => Set<QualityCommitteeMeeting>();
    public DbSet<QualityCommitteeActionItem> QualityCommitteeActionItems => Set<QualityCommitteeActionItem>();
    public DbSet<QualityNabhStandard> QualityNabhStandards => Set<QualityNabhStandard>();
    public DbSet<QualityEvidence> QualityEvidence => Set<QualityEvidence>();

    // Reports & Dashboards (Phase 23)
    public DbSet<ReportDefinition> ReportDefinitions => Set<ReportDefinition>();
    public DbSet<ReportSubscription> ReportSubscriptions => Set<ReportSubscription>();
    public DbSet<RevenueDailyRow> RevenueDailyRows => Set<RevenueDailyRow>();
    public DbSet<DailyCollectionRow> DailyCollectionRows => Set<DailyCollectionRow>();
    public DbSet<OpCensusRow> OpCensusRows => Set<OpCensusRow>();

    // Insurance/TPA (Phase 24)
    public DbSet<PatientInsurancePolicy> PatientInsurancePolicies => Set<PatientInsurancePolicy>();
    public DbSet<PreAuthRequest> PreAuthRequests => Set<PreAuthRequest>();
    public DbSet<PreAuthEnhancement> PreAuthEnhancements => Set<PreAuthEnhancement>();
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<ClaimDocument> ClaimDocuments => Set<ClaimDocument>();

    /// <summary>
    /// PostgreSQL timestamptz only accepts UTC through Npgsql; a client that sends a local offset
    /// (e.g. +05:30) used to cause an unhandled 500. Normalise every DateTimeOffset to UTC on write
    /// (the instant is unchanged; only the representation).
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcDateTimeOffsetConverter>();
    }

    private sealed class UtcDateTimeOffsetConverter() : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset, DateTimeOffset>(
        v => v.ToUniversalTime(), v => v);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global filters: !IsDeleted for every ISoftDelete entity, plus branch scoping
        // for IBranchScoped entities (skipped when the caller has no branch — seeding,
        // background jobs). Cross-branch reads use IgnoreQueryFilters guarded by
        // REPORT.CROSSBRANCH.VIEW.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var body = (Expression)Expression.Not(
                Expression.Property(parameter, nameof(ISoftDelete.IsDeleted)));

            if (typeof(IBranchScoped).IsAssignableFrom(entityType.ClrType))
            {
                var currentBranchId = Expression.Property(
                    Expression.Constant(this), nameof(CurrentBranchId));
                var noBranchContext = Expression.Equal(
                    currentBranchId, Expression.Constant(null, typeof(long?)));
                var branchMatches = Expression.Equal(
                    Expression.Convert(
                        Expression.Property(parameter, nameof(IBranchScoped.BranchId)), typeof(long?)),
                    currentBranchId);
                body = Expression.AndAlso(body, Expression.OrElse(noBranchContext, branchMatches));
            }

            entityType.SetQueryFilter(Expression.Lambda(body, parameter));
        }
    }
}
