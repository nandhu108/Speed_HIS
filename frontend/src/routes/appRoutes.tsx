import type { RouteObject } from 'react-router-dom'
import { AbdmAdminPage } from '../features/admin/pages/AbdmAdminPage'
import { ActivityLogsPage } from '../features/admin/pages/ActivityLogsPage'
import { AuditLogsPage } from '../features/admin/pages/AuditLogsPage'
import { BranchesPage } from '../features/admin/pages/BranchesPage'
import { DesignationsPage } from '../features/admin/pages/DesignationsPage'
import { MenusPage } from '../features/admin/pages/MenusPage'
import { RolesPage } from '../features/admin/pages/RolesPage'
import { SessionsPage } from '../features/admin/pages/SessionsPage'
import { SettingsPage } from '../features/admin/pages/SettingsPage'
import { UsersPage } from '../features/admin/pages/UsersPage'
import { ChangePasswordPage } from '../features/auth/pages/ChangePasswordPage'
import { AppointmentsListPage } from '../features/appointments/pages/AppointmentsListPage'
import { BookingPage } from '../features/appointments/pages/BookingPage'
import { DoctorAppointmentReportPage } from '../features/appointments/pages/DoctorAppointmentReportPage'
import { DoctorSchedulePage } from '../features/appointments/pages/DoctorSchedulePage'
import { DoctorsOnDutyPage } from '../features/appointments/pages/DoctorsOnDutyPage'
import { UnitSchedulingPage } from '../features/appointments/pages/UnitSchedulingPage'
import { QueueDisplayPage } from '../features/appointments/pages/QueueDisplayPage'
import { HomePage } from '../features/dashboard/pages/HomePage'
import { AdvancesPage } from '../features/billing/pages/AdvancesPage'
import { BillDetailPage } from '../features/billing/pages/BillDetailPage'
import { BillEstimatesPage } from '../features/billing/pages/BillEstimatesPage'
import { BillingReportsPage } from '../features/billing/pages/BillingReportsPage'
import { BillsListPage } from '../features/billing/pages/BillsListPage'
import { CashierShiftsPage } from '../features/billing/pages/CashierShiftsPage'
import { CreditNotesApprovalPage } from '../features/billing/pages/CreditNotesApprovalPage'
import { DayClosePage } from '../features/billing/pages/DayClosePage'
import { DiscountApprovalsPage } from '../features/billing/pages/DiscountApprovalsPage'
import { OnlinePaymentRequestsPage } from '../features/billing/pages/OnlinePaymentRequestsPage'
import { OpBillingSearchPage } from '../features/billing/pages/OpBillingSearchPage'
import { ParkedBillsPage } from '../features/billing/pages/ParkedBillsPage'
import { PaymentModesPage } from '../features/billing/pages/PaymentModesPage'
import { RefundApprovalsPage } from '../features/billing/pages/RefundApprovalsPage'
import { CollectionWorklistPage } from '../features/lab/pages/CollectionWorklistPage'
import { CriticalAlertsPage } from '../features/lab/pages/CriticalAlertsPage'
import { LabOrderDetailPage } from '../features/lab/pages/LabOrderDetailPage'
import { LabOrdersPage } from '../features/lab/pages/LabOrdersPage'
import { LabTestsPage } from '../features/lab/pages/LabTestsPage'
import { ResultWorklistPage } from '../features/lab/pages/ResultWorklistPage'
import { TatReportPage } from '../features/lab/pages/TatReportPage'
import { TestProfilesPage } from '../features/lab/pages/TestProfilesPage'
import { UnbilledOrdersReportPage } from '../features/lab/pages/UnbilledOrdersReportPage'
import { AlertsPage } from '../features/pharmacy/pages/AlertsPage'
import { ConsumptionReportPage } from '../features/pharmacy/pages/ConsumptionReportPage'
import { GrnPage } from '../features/pharmacy/pages/GrnPage'
import { IndentsPage } from '../features/pharmacy/pages/IndentsPage'
import { IssuesPage } from '../features/pharmacy/pages/IssuesPage'
import { PosSalePage } from '../features/pharmacy/pages/PosSalePage'
import { PurchaseOrdersPage } from '../features/pharmacy/pages/PurchaseOrdersPage'
import { PurchaseRequestsPage } from '../features/pharmacy/pages/PurchaseRequestsPage'
import { PurchaseReturnsPage } from '../features/pharmacy/pages/PurchaseReturnsPage'
import { SaleDetailPage } from '../features/pharmacy/pages/SaleDetailPage'
import { SalesListPage } from '../features/pharmacy/pages/SalesListPage'
import { StockAdjustmentsPage } from '../features/pharmacy/pages/StockAdjustmentsPage'
import { StockAuditsPage } from '../features/pharmacy/pages/StockAuditsPage'
import { StockExplorerPage } from '../features/pharmacy/pages/StockExplorerPage'
import { StockTransfersPage } from '../features/pharmacy/pages/StockTransfersPage'
import { StoresPage } from '../features/pharmacy/pages/StoresPage'
import { AdmissionsPage } from '../features/ipd/pages/AdmissionsPage'
import { AdmissionRequestsPage } from '../features/ipd/pages/AdmissionRequestsPage'
import { AdmissionRequestConversionReportPage } from '../features/ipd/pages/AdmissionRequestConversionReportPage'
import { AdmissionDetailPage } from '../features/ipd/pages/AdmissionDetailPage'
import { BedBoardPage } from '../features/ipd/pages/BedBoardPage'
import { ConvertToIpPage } from '../features/ipd/pages/ConvertToIpPage'
import { IpReportPage } from '../features/ipd/pages/IpReportPage'
import { WardOccupancyReportPage } from '../features/ipd/pages/WardOccupancyReportPage'
import { ShiftHandoversPage } from '../features/nursing/pages/ShiftHandoversPage'
import { IncidentsPage } from '../features/nursing/pages/IncidentsPage'
import { RadOrdersPage } from '../features/rad/pages/RadOrdersPage'
import { RadOrderDetailPage } from '../features/rad/pages/RadOrderDetailPage'
import { SchedulingWorklistPage } from '../features/rad/pages/SchedulingWorklistPage'
import { CaptureWorklistPage } from '../features/rad/pages/CaptureWorklistPage'
import { ReportingWorklistPage } from '../features/rad/pages/ReportingWorklistPage'
import { RadiologyTestsPage } from '../features/rad/pages/RadiologyTestsPage'
import { RadReportTemplatesPage } from '../features/rad/pages/RadReportTemplatesPage'
import { TatReportPage as RadTatReportPage } from '../features/rad/pages/TatReportPage'
import { UnbilledOrdersReportPage as RadUnbilledOrdersReportPage } from '../features/rad/pages/UnbilledOrdersReportPage'
import { BookingDetailPage as OtBookingDetailPage } from '../features/ot/pages/BookingDetailPage'
import { BookingsPage as OtBookingsPage } from '../features/ot/pages/BookingsPage'
import { OtRegisterPage } from '../features/ot/pages/OtRegisterPage'
import { TheatresPage as OtTheatresPage } from '../features/ot/pages/TheatresPage'
import { AlertRulesPage as IcuAlertRulesPage } from '../features/icu/pages/AlertRulesPage'
import { AlertsPage as IcuAlertsPage } from '../features/icu/pages/AlertsPage'
import { IcuDashboardPage } from '../features/icu/pages/IcuDashboardPage'
import { ErReportPage } from '../features/er/pages/ErReportPage'
import { QueuePage as ErQueuePage } from '../features/er/pages/QueuePage'
import { VisitDetailPage as ErVisitDetailPage } from '../features/er/pages/VisitDetailPage'
import { AmcContractsPage } from '../features/biomed/pages/AmcContractsPage'
import { BiomedReportsPage } from '../features/biomed/pages/BiomedReportsPage'
import { CalibrationsPage } from '../features/biomed/pages/CalibrationsPage'
import { ComplaintsPage } from '../features/biomed/pages/ComplaintsPage'
import { EquipmentDetailPage } from '../features/biomed/pages/EquipmentDetailPage'
import { EquipmentListPage } from '../features/biomed/pages/EquipmentListPage'
import { PmSchedulesPage } from '../features/biomed/pages/PmSchedulesPage'
import { WorkOrdersPage } from '../features/biomed/pages/WorkOrdersPage'
import { AuditsPage } from '../features/quality/pages/AuditsPage'
import { CapaTrackerPage } from '../features/quality/pages/CapaTrackerPage'
import { CommitteeMeetingsPage } from '../features/quality/pages/CommitteeMeetingsPage'
import { IncidentRegisterPage } from '../features/quality/pages/IncidentRegisterPage'
import { IndicatorsPage } from '../features/quality/pages/IndicatorsPage'
import { NabhEvidencePage } from '../features/quality/pages/NabhEvidencePage'
import { NonConformancesPage } from '../features/quality/pages/NonConformancesPage'
import { QualityDashboardPage } from '../features/quality/pages/QualityDashboardPage'
import { PreAuthWorklistPage } from '../features/insurance/pages/PreAuthWorklistPage'
import { ClaimsWorklistPage } from '../features/insurance/pages/ClaimsWorklistPage'
import { RiskRegisterPage } from '../features/quality/pages/RiskRegisterPage'
import { CommitteesPage, NabhStandardsPage } from '../features/quality/pages/simpleMasterPages'
import { AccountLedgerPage } from '../features/finance/pages/AccountLedgerPage'
import { AccountsPage as FinanceAccountsPage } from '../features/finance/pages/AccountsPage'
import { DayCloseConsolePage } from '../features/finance/pages/DayCloseConsolePage'
import { FinanceReportsPage } from '../features/finance/pages/FinanceReportsPage'
import { PayablesPage } from '../features/finance/pages/PayablesPage'
import { ReceivablesPage } from '../features/finance/pages/ReceivablesPage'
import { VouchersPage } from '../features/finance/pages/VouchersPage'
import { DischargeApprovalsPage } from '../features/doctorportal/pages/DischargeApprovalsPage'
import { DoctorDashboardPage } from '../features/doctorportal/pages/DoctorDashboardPage'
import { MyInpatientsPage } from '../features/doctorportal/pages/MyInpatientsPage'
import { MyOrdersResultsPage } from '../features/doctorportal/pages/MyOrdersResultsPage'
import { MyQueuePage } from '../features/doctorportal/pages/MyQueuePage'
import { OpinionRequestsPage } from '../features/doctorportal/pages/OpinionRequestsPage'
import { AuditsPage as MrdAuditsPage } from '../features/mrd/pages/AuditsPage'
import { BillingDashboardPage } from '../features/reports/pages/BillingDashboardPage'
import { CensusReportPage } from '../features/reports/pages/CensusReportPage'
import { CustomReportsPage } from '../features/reports/pages/CustomReportsPage'
import { IpDashboardPage } from '../features/reports/pages/IpDashboardPage'
import { ManagementDashboardPage } from '../features/reports/pages/ManagementDashboardPage'
import { OpDashboardPage } from '../features/reports/pages/OpDashboardPage'
import { ReportSubscriptionsPage } from '../features/reports/pages/ReportSubscriptionsPage'
import { RevenueDashboardPage } from '../features/reports/pages/RevenueDashboardPage'
import { CodingWorklistPage as MrdCodingWorklistPage } from '../features/mrd/pages/CodingWorklistPage'
import { FileRequestsPage as MrdFileRequestsPage } from '../features/mrd/pages/FileRequestsPage'
import { MrdReportsPage } from '../features/mrd/pages/MrdReportsPage'
import { RecordFileDetailPage as MrdRecordFileDetailPage } from '../features/mrd/pages/RecordFileDetailPage'
import { RecordFilesPage as MrdRecordFilesPage } from '../features/mrd/pages/RecordFilesPage'
import { RetentionDestructionPage } from '../features/mrd/pages/RetentionDestructionPage'
import { ConsentTemplatesPage } from '../features/masters/pages/ConsentTemplatesPage'
import { ConsultationWorkspace } from '../features/opd/pages/ConsultationWorkspace'
import { DoctorQueuePage } from '../features/opd/pages/DoctorQueuePage'
import { FrontDeskPage } from '../features/opd/pages/FrontDeskPage'
import { OpReportPage } from '../features/opd/pages/OpReportPage'
import { CampRegistrationPage } from '../features/opd/pages/CampRegistrationPage'
import { CampBulkImportPage } from '../features/opd/pages/CampBulkImportPage'
import { OpRegistrationUtilityPage } from '../features/opd/pages/OpRegistrationUtilityPage'
import { CampRegistrationReportPage } from '../features/opd/pages/CampRegistrationReportPage'
import { OpTatReportPage } from '../features/opd/pages/OpTatReportPage'
import { OpCancelReportPage } from '../features/opd/pages/OpCancelReportPage'
import { ConversionReportPage } from '../features/opd/pages/ConversionReportPage'
import { ReferralReportPage } from '../features/opd/pages/ReferralReportPage'
import { MhcRegistrationPage } from '../features/mhc/pages/MhcRegistrationPage'
import { MhcReportsPage } from '../features/mhc/pages/MhcReportsPage'
import { MhcBulkImportPage } from '../features/mhc/pages/MhcBulkImportPage'
import { FrontOfficePage } from '../features/frontoffice/pages/FrontOfficePage'
import { PrescriptionTemplatesPage } from '../features/opd/pages/PrescriptionTemplatesPage'
import { BillEstimateTemplatesPage } from '../features/masters/pages/BillEstimateTemplatesPage'
import { CorporatesPage } from '../features/masters/pages/CorporatesPage'
import { DiscountTypesPage } from '../features/masters/pages/DiscountTypesPage'
import { DoctorPayoutRulesPage } from '../features/masters/pages/DoctorPayoutRulesPage'
import { AppointmentTypesPage } from '../features/masters/pages/AppointmentTypesPage'
import { DoctorsPage } from '../features/masters/pages/DoctorsPage'
import { DrugsPage } from '../features/masters/pages/DrugsPage'
import { EmployeesPage } from '../features/masters/pages/EmployeesPage'
import { IcdCodesPage } from '../features/masters/pages/IcdCodesPage'
import { InsurancePage } from '../features/masters/pages/InsurancePage'
import { ItemsPage } from '../features/masters/pages/ItemsPage'
import { LocationsPage } from '../features/masters/pages/LocationsPage'
import { PackagesPage } from '../features/masters/pages/PackagesPage'
import { ReferrersPage } from '../features/masters/pages/ReferrersPage'
import { ServicesPage } from '../features/masters/pages/ServicesPage'
import {
  CampsPage,
  CitiesPage,
  ClaimDenialReasonsPage,
  ClaimDocumentTypesPage,
  DocumentTypesPage,
  IdentityDocumentTypesPage,
  LanguagesPage,
  LoyaltyCardCategoriesPage,
  LoyaltyProgramsPage,
  MaritalStatusesPage,
  MemberRelationTagsPage,
  MhcDoctorsPage,
  MlcTypesPage,
  ModalitiesPage,
  NationalitiesPage,
  PatientCaseTypesPage,
  PatientHcTagsPage,
  ReligionsPage,
  SalutationsPage,
  SampleTypesPage,
  SpeciallyAbledCategoriesPage,
  SpecialtiesPage,
  StatesPage,
  UnitsPage,
  UomsPage,
  VisitClassesPage,
  VisitSessionsPage,
} from '../features/masters/pages/simpleMasterPages'
import { AreasPage } from '../features/masters/pages/AreasPage'
import { DepartmentsPage } from '../features/masters/pages/DepartmentsPage'
import { PatientCategoriesPage } from '../features/masters/pages/PatientCategoriesPage'
import { MhcPackagesPage } from '../features/masters/pages/MhcPackagesPage'
import { LoyaltyCardTypesPage } from '../features/masters/pages/LoyaltyCardTypesPage'
import { LoyaltyCardPricesPage } from '../features/masters/pages/LoyaltyCardPricesPage'
import { LoyaltyCardsPage } from '../features/loyalty/pages/LoyaltyCardsPage'
import { LoyaltyCardDetailPage } from '../features/loyalty/pages/LoyaltyCardDetailPage'
import { TariffsPage } from '../features/masters/pages/TariffsPage'
import { TaxesPage } from '../features/masters/pages/TaxesPage'
import { VendorsPage } from '../features/masters/pages/VendorsPage'
import { BlockUnblockReportPage } from '../features/patients/pages/BlockUnblockReportPage'
import { PatientDetailPage } from '../features/patients/pages/PatientDetailPage'
import { PatientRegistrationWizard } from '../features/patients/pages/PatientRegistrationWizard'
import { PatientsListPage } from '../features/patients/pages/PatientsListPage'
import { PatientSummaryPage } from '../features/patients/pages/PatientSummaryPage'
import { PhotoCapturePage } from '../features/patients/pages/PhotoCapturePage'
import { RequirePermission } from './guards'

/**
 * Every staff page route, keyed by path, permission-guarded. Rendered multiple times
 * simultaneously — once per open workspace tab (see lib/tabWorkspace/TabRouterPane.tsx),
 * each against that tab's own independent location via useRoutes() — not just once by the
 * top-level createBrowserRouter in index.tsx. Nothing here is data-router-specific (no
 * loader/action/errorElement), so this plain RouteObject[] renders identically either way.
 */
export const appRoutes: RouteObject[] = [
  { index: true, element: <HomePage /> },
  { path: 'change-password', element: <ChangePasswordPage /> },
  {
    path: 'front-office',
    element: <RequirePermission permission="OPD.VISIT.VIEW" />,
    children: [{ index: true, element: <FrontOfficePage /> }],
  },
  {
    path: 'patients',
    children: [
      {
        element: <RequirePermission permission="PATIENT.PATIENT.VIEW" />,
        children: [{ index: true, element: <PatientsListPage /> }],
      },
      {
        element: <RequirePermission permission="PATIENT.PATIENT.CREATE" />,
        children: [{ path: 'new', element: <PatientRegistrationWizard /> }],
      },
      {
        element: <RequirePermission permission="PATIENT.PATIENT.VIEW" />,
        children: [
          { path: 'reports/summary', element: <PatientSummaryPage /> },
          { path: ':id', element: <PatientDetailPage /> },
        ],
      },
      {
        element: <RequirePermission permission="PATIENT.PATIENT.BLOCK" />,
        children: [{ path: 'reports/block-unblock', element: <BlockUnblockReportPage /> }],
      },
      {
        element: <RequirePermission permission="PATIENT.PATIENT.UPDATE" />,
        children: [{ path: 'photo-capture', element: <PhotoCapturePage /> }],
      },
    ],
  },
  {
    path: 'appointments',
    children: [
      {
        element: <RequirePermission permission="APPOINTMENT.APPOINTMENT.VIEW" />,
        children: [
          { index: true, element: <AppointmentsListPage /> },
          { path: 'reports/doctor-appointment', element: <DoctorAppointmentReportPage /> },
        ],
      },
      {
        element: <RequirePermission permission="APPOINTMENT.APPOINTMENT.BOOK" />,
        children: [{ path: 'new', element: <BookingPage /> }],
      },
      {
        element: <RequirePermission permission="APPOINTMENT.SCHEDULE.VIEW" />,
        children: [{ path: 'schedule', element: <DoctorSchedulePage /> }],
      },
      {
        element: <RequirePermission permission="APPOINTMENT.UNITSCHEDULE.VIEW" />,
        children: [{ path: 'unit-schedule', element: <UnitSchedulingPage /> }],
      },
      {
        element: <RequirePermission permission="APPOINTMENT.QUEUE.VIEW" />,
        children: [{ path: 'queue-display', element: <QueueDisplayPage /> }],
      },
      {
        element: <RequirePermission permission="APPOINTMENT.SCHEDULE.VIEW" />,
        children: [{ path: 'doctors-on-duty', element: <DoctorsOnDutyPage /> }],
      },
    ],
  },
  {
    path: 'opd',
    children: [
      {
        element: <RequirePermission permission="OPD.VISIT.VIEW" />,
        children: [
          { index: true, element: <FrontDeskPage /> },
          { path: 'queue', element: <DoctorQueuePage /> },
          { path: 'visits/:id', element: <ConsultationWorkspace /> },
          { path: 'report', element: <OpReportPage /> },
          { path: 'camp-registration', element: <CampRegistrationPage /> },
          { path: 'camp-registration/report', element: <CampRegistrationReportPage /> },
          { path: 'reports/tat', element: <OpTatReportPage /> },
          { path: 'reports/cancelled', element: <OpCancelReportPage /> },
          { path: 'reports/conversion', element: <ConversionReportPage /> },
          { path: 'reports/referrals', element: <ReferralReportPage /> },
          { path: 'registration-utility', element: <OpRegistrationUtilityPage /> },
        ],
      },
      {
        element: <RequirePermission permission="OPD.TEMPLATE.VIEW" />,
        children: [{ path: 'templates', element: <PrescriptionTemplatesPage /> }],
      },
      {
        element: <RequirePermission permission="OPD.VISIT.IMPORT" />,
        children: [{ path: 'camp-registration/import', element: <CampBulkImportPage /> }],
      },
    ],
  },
  {
    path: 'mhc',
    children: [
      {
        element: <RequirePermission permission="MHC.VISIT.VIEW" />,
        children: [
          { index: true, element: <MhcRegistrationPage /> },
          { path: 'reports', element: <MhcReportsPage /> },
        ],
      },
      {
        element: <RequirePermission permission="MHC.VISIT.IMPORT" />,
        children: [{ path: 'import', element: <MhcBulkImportPage /> }],
      },
    ],
  },
  {
    path: 'loyalty',
    children: [
      {
        element: <RequirePermission permission="LOYALTY.CARD.VIEW" />,
        children: [
          { index: true, element: <LoyaltyCardsPage /> },
          { path: 'cards/:id', element: <LoyaltyCardDetailPage /> },
        ],
      },
    ],
  },
  {
    path: 'billing',
    children: [
      {
        element: <RequirePermission permission="BILLING.BILL.VIEW" />,
        children: [
          { index: true, element: <OpBillingSearchPage /> },
          { path: 'bills', element: <BillsListPage /> },
          { path: 'bills/:id', element: <BillDetailPage /> },
          { path: 'op-search', element: <OpBillingSearchPage /> },
        ],
      },
      {
        element: <RequirePermission permission="BILLING.DISCOUNT.APPROVE" />,
        children: [{ path: 'discount-approvals', element: <DiscountApprovalsPage /> }],
      },
      {
        element: <RequirePermission permission="BILLING.REFUND.APPROVE" />,
        children: [{ path: 'refund-approvals', element: <RefundApprovalsPage /> }],
      },
      {
        element: <RequirePermission permission="BILLING.ADVANCE.VIEW" />,
        children: [{ path: 'advances', element: <AdvancesPage /> }],
      },
      {
        element: <RequirePermission permission="BILLING.DAYCLOSE.VIEW" />,
        children: [{ path: 'day-close', element: <DayClosePage /> }],
      },
      {
        element: <RequirePermission permission="BILLING.PAYMENTMODE.VIEW" />,
        children: [{ path: 'payment-modes', element: <PaymentModesPage /> }],
      },
      {
        element: <RequirePermission permission="BILLING.CREDITNOTE.VIEW" />,
        children: [{ path: 'credit-notes', element: <CreditNotesApprovalPage /> }],
      },
      {
        element: <RequirePermission permission="BILLING.BILL.PARK" />,
        children: [{ path: 'parked-bills', element: <ParkedBillsPage /> }],
      },
      {
        element: <RequirePermission permission="BILLING.CASHIERSHIFT.MANAGE" />,
        children: [{ path: 'cashier-shifts', element: <CashierShiftsPage /> }],
      },
      {
        element: <RequirePermission permission="BILLING.ESTIMATE.VIEW" />,
        children: [{ path: 'estimates', element: <BillEstimatesPage /> }],
      },
      {
        element: <RequirePermission permission="BILLING.ONLINEPAYMENT.VIEW" />,
        children: [{ path: 'online-payment-requests', element: <OnlinePaymentRequestsPage /> }],
      },
      {
        element: <RequirePermission permission="BILLING.REPORT.VIEW" />,
        children: [{ path: 'reports/:tab?', element: <BillingReportsPage /> }],
      },
    ],
  },
  {
    path: 'lab',
    children: [
      {
        element: <RequirePermission permission="LAB.ORDER.VIEW" />,
        children: [
          { index: true, element: <LabOrdersPage /> },
          { path: 'orders', element: <LabOrdersPage /> },
          { path: 'orders/:id', element: <LabOrderDetailPage /> },
        ],
      },
      {
        element: <RequirePermission permission="LAB.SAMPLE.COLLECT" />,
        children: [{ path: 'collection', element: <CollectionWorklistPage /> }],
      },
      {
        element: <RequirePermission permission="LAB.RESULT.CREATE" />,
        children: [{ path: 'worklist', element: <ResultWorklistPage /> }],
      },
      {
        element: <RequirePermission permission="LAB.ALERT.ACKNOWLEDGE" />,
        children: [{ path: 'critical-alerts', element: <CriticalAlertsPage /> }],
      },
      {
        element: <RequirePermission permission="LAB.TEST.VIEW" />,
        children: [{ path: 'tests', element: <LabTestsPage /> }],
      },
      {
        element: <RequirePermission permission="LAB.PROFILE.VIEW" />,
        children: [{ path: 'profiles', element: <TestProfilesPage /> }],
      },
      {
        element: <RequirePermission permission="LAB.REPORT.VIEW" />,
        children: [
          { path: 'tat-report', element: <TatReportPage /> },
          { path: 'unbilled-report', element: <UnbilledOrdersReportPage /> },
        ],
      },
    ],
  },
  {
    path: 'pharmacy',
    children: [
      {
        element: <RequirePermission permission="PHARMACY.SALE.CREATE" />,
        children: [{ path: 'sales/new', element: <PosSalePage /> }],
      },
      {
        element: <RequirePermission permission="PHARMACY.SALE.VIEW" />,
        children: [
          { index: true, element: <SalesListPage /> },
          { path: 'sales', element: <SalesListPage /> },
          { path: 'sales/:id', element: <SaleDetailPage /> },
        ],
      },
      {
        element: <RequirePermission permission="PHARMACY.PURCHASEREQUEST.VIEW" />,
        children: [{ path: 'purchase-requests', element: <PurchaseRequestsPage /> }],
      },
      {
        element: <RequirePermission permission="PHARMACY.PURCHASEORDER.VIEW" />,
        children: [{ path: 'purchase-orders', element: <PurchaseOrdersPage /> }],
      },
      {
        element: <RequirePermission permission="PHARMACY.GRN.VIEW" />,
        children: [{ path: 'grn', element: <GrnPage /> }],
      },
      {
        element: <RequirePermission permission="PHARMACY.PURCHASERETURN.VIEW" />,
        children: [{ path: 'purchase-returns', element: <PurchaseReturnsPage /> }],
      },
      {
        element: <RequirePermission permission="PHARMACY.STOCK.VIEW" />,
        children: [{ path: 'stock', element: <StockExplorerPage /> }],
      },
      {
        element: <RequirePermission permission="PHARMACY.ISSUE.VIEW" />,
        children: [{ path: 'issues', element: <IssuesPage /> }],
      },
      {
        element: <RequirePermission permission="PHARMACY.INDENT.VIEW" />,
        children: [{ path: 'indents', element: <IndentsPage /> }],
      },
      {
        element: <RequirePermission permission="PHARMACY.STOCK.ADJUST" />,
        children: [{ path: 'adjustments', element: <StockAdjustmentsPage /> }],
      },
      {
        element: <RequirePermission permission="PHARMACY.STOCK.TRANSFER" />,
        children: [{ path: 'transfers', element: <StockTransfersPage /> }],
      },
      {
        element: <RequirePermission permission="PHARMACY.STOCK.AUDIT" />,
        children: [{ path: 'audits', element: <StockAuditsPage /> }],
      },
      {
        element: <RequirePermission permission="PHARMACY.REPORT.VIEW" />,
        children: [
          { path: 'alerts', element: <AlertsPage /> },
          { path: 'consumption-report', element: <ConsumptionReportPage /> },
        ],
      },
      {
        element: <RequirePermission permission="PHARMACY.STORE.VIEW" />,
        children: [{ path: 'stores', element: <StoresPage /> }],
      },
    ],
  },
  {
    path: 'ipd',
    children: [
      {
        element: <RequirePermission permission="IPD.ADMISSION.VIEW" />,
        children: [
          { index: true, element: <AdmissionsPage /> },
          { path: 'admissions', element: <AdmissionsPage /> },
          { path: 'admissions/:id', element: <AdmissionDetailPage /> },
          { path: 'report', element: <IpReportPage /> },
        ],
      },
      {
        element: <RequirePermission permission="IPD.ADMISSION.CREATE" />,
        children: [{ path: 'convert', element: <ConvertToIpPage /> }],
      },
      {
        element: <RequirePermission permission="IPD.ADMISSIONREQUEST.VIEW" />,
        children: [
          { path: 'admission-requests', element: <AdmissionRequestsPage /> },
          { path: 'admission-requests/report', element: <AdmissionRequestConversionReportPage /> },
        ],
      },
      {
        element: <RequirePermission permission="IPD.BED.VIEW" />,
        children: [
          { path: 'bed-board', element: <BedBoardPage /> },
          { path: 'ward-occupancy-report', element: <WardOccupancyReportPage /> },
        ],
      },
    ],
  },
  {
    path: 'nursing',
    children: [
      {
        element: <RequirePermission permission="NURSING.HANDOVER.VIEW" />,
        children: [{ path: 'handovers', element: <ShiftHandoversPage /> }],
      },
      {
        element: <RequirePermission permission="NURSING.INCIDENT.VIEW" />,
        children: [{ path: 'incidents', element: <IncidentsPage /> }],
      },
    ],
  },
  {
    path: 'rad',
    children: [
      {
        element: <RequirePermission permission="RAD.ORDER.VIEW" />,
        children: [
          { index: true, element: <RadOrdersPage /> },
          { path: 'orders', element: <RadOrdersPage /> },
          { path: 'orders/:id', element: <RadOrderDetailPage /> },
        ],
      },
      {
        element: <RequirePermission permission="RAD.SCHEDULE.MANAGE" />,
        children: [{ path: 'scheduling', element: <SchedulingWorklistPage /> }],
      },
      {
        element: <RequirePermission permission="RAD.CAPTURE.MANAGE" />,
        children: [{ path: 'capture', element: <CaptureWorklistPage /> }],
      },
      {
        element: <RequirePermission permission="RAD.REPORT.CREATE" />,
        children: [{ path: 'reporting', element: <ReportingWorklistPage /> }],
      },
      {
        element: <RequirePermission permission="RAD.TEST.VIEW" />,
        children: [{ path: 'tests', element: <RadiologyTestsPage /> }],
      },
      {
        element: <RequirePermission permission="RAD.TEMPLATE.VIEW" />,
        children: [{ path: 'templates', element: <RadReportTemplatesPage /> }],
      },
      {
        element: <RequirePermission permission="RAD.REPORT.VIEW" />,
        children: [
          { path: 'tat-report', element: <RadTatReportPage /> },
          { path: 'unbilled-report', element: <RadUnbilledOrdersReportPage /> },
        ],
      },
    ],
  },
  {
    path: 'ot',
    children: [
      {
        element: <RequirePermission permission="OT.BOOKING.VIEW" />,
        children: [
          { index: true, element: <OtBookingsPage /> },
          { path: 'bookings', element: <OtBookingsPage /> },
          { path: 'bookings/:id', element: <OtBookingDetailPage /> },
        ],
      },
      {
        element: <RequirePermission permission="OT.THEATRE.VIEW" />,
        children: [{ path: 'theatres', element: <OtTheatresPage /> }],
      },
      {
        element: <RequirePermission permission="OT.REGISTER.VIEW" />,
        children: [{ path: 'register', element: <OtRegisterPage /> }],
      },
    ],
  },
  {
    path: 'icu',
    children: [
      {
        element: <RequirePermission permission="ICU.DASHBOARD.VIEW" />,
        children: [
          { index: true, element: <IcuDashboardPage /> },
          { path: 'dashboard', element: <IcuDashboardPage /> },
        ],
      },
      {
        element: <RequirePermission permission="ICU.ALERT.VIEW" />,
        children: [{ path: 'alerts', element: <IcuAlertsPage /> }],
      },
      {
        element: <RequirePermission permission="ICU.RULE.VIEW" />,
        children: [{ path: 'alert-rules', element: <IcuAlertRulesPage /> }],
      },
    ],
  },
  {
    path: 'emergency',
    children: [
      {
        element: <RequirePermission permission="EMERGENCY.VISIT.VIEW" />,
        children: [
          { index: true, element: <ErQueuePage /> },
          { path: 'queue', element: <ErQueuePage /> },
          { path: 'visits/:id', element: <ErVisitDetailPage /> },
          { path: 'reports', element: <ErReportPage /> },
        ],
      },
    ],
  },
  {
    path: 'mrd',
    children: [
      {
        element: <RequirePermission permission="MRD.FILE.VIEW" />,
        children: [
          { index: true, element: <MrdRecordFilesPage /> },
          { path: 'files', element: <MrdRecordFilesPage /> },
          { path: 'files/:id', element: <MrdRecordFileDetailPage /> },
          { path: 'requests', element: <MrdFileRequestsPage /> },
        ],
      },
      {
        element: <RequirePermission permission="MRD.CODING.VIEW" />,
        children: [{ path: 'coding-worklist', element: <MrdCodingWorklistPage /> }],
      },
      {
        element: <RequirePermission permission="MRD.AUDIT.VIEW" />,
        children: [{ path: 'audits', element: <MrdAuditsPage /> }],
      },
      {
        element: <RequirePermission permission="MRD.RETENTION.VIEW" />,
        children: [{ path: 'retention', element: <RetentionDestructionPage /> }],
      },
      {
        element: <RequirePermission permission="MRD.REPORT.VIEW" />,
        children: [{ path: 'reports', element: <MrdReportsPage /> }],
      },
    ],
  },
  {
    path: 'finance',
    children: [
      {
        element: <RequirePermission permission="FINANCE.VOUCHER.VIEW" />,
        children: [
          { index: true, element: <VouchersPage /> },
          { path: 'vouchers', element: <VouchersPage /> },
        ],
      },
      {
        element: <RequirePermission permission="FINANCE.ACCOUNT.VIEW" />,
        children: [{ path: 'accounts', element: <FinanceAccountsPage /> }],
      },
      {
        element: <RequirePermission permission="FINANCE.LEDGER.VIEW" />,
        children: [{ path: 'ledger/:accountId', element: <AccountLedgerPage /> }],
      },
      {
        element: <RequirePermission permission="FINANCE.PAYABLE.VIEW" />,
        children: [{ path: 'payables', element: <PayablesPage /> }],
      },
      {
        element: <RequirePermission permission="FINANCE.RECEIVABLE.VIEW" />,
        children: [{ path: 'receivables', element: <ReceivablesPage /> }],
      },
      {
        element: <RequirePermission permission="FINANCE.DAYCLOSECONSOLE.VIEW" />,
        children: [{ path: 'day-close-console', element: <DayCloseConsolePage /> }],
      },
      {
        element: <RequirePermission permission="FINANCE.REPORT.VIEW" />,
        children: [{ path: 'reports', element: <FinanceReportsPage /> }],
      },
    ],
  },
  {
    path: 'biomed',
    children: [
      {
        element: <RequirePermission permission="BIOMED.EQUIPMENT.VIEW" />,
        children: [
          { index: true, element: <EquipmentListPage /> },
          { path: 'equipment', element: <EquipmentListPage /> },
          { path: 'equipment/:id', element: <EquipmentDetailPage /> },
        ],
      },
      {
        element: <RequirePermission permission="BIOMED.CONTRACT.VIEW" />,
        children: [{ path: 'contracts', element: <AmcContractsPage /> }],
      },
      {
        element: <RequirePermission permission="BIOMED.PM.VIEW" />,
        children: [{ path: 'pm-schedules', element: <PmSchedulesPage /> }],
      },
      {
        element: <RequirePermission permission="BIOMED.WORKORDER.VIEW" />,
        children: [{ path: 'work-orders', element: <WorkOrdersPage /> }],
      },
      {
        element: <RequirePermission permission="BIOMED.COMPLAINT.VIEW" />,
        children: [{ path: 'complaints', element: <ComplaintsPage /> }],
      },
      {
        element: <RequirePermission permission="BIOMED.CALIBRATION.VIEW" />,
        children: [{ path: 'calibrations', element: <CalibrationsPage /> }],
      },
      {
        element: <RequirePermission permission="BIOMED.REPORT.VIEW" />,
        children: [{ path: 'reports', element: <BiomedReportsPage /> }],
      },
    ],
  },
  {
    path: 'quality',
    children: [
      {
        element: <RequirePermission permission="QUALITY.INDICATOR.VIEW" />,
        children: [{ index: true, element: <IndicatorsPage /> }, { path: 'indicators', element: <IndicatorsPage /> }],
      },
      {
        element: <RequirePermission permission="QUALITY.AUDIT.VIEW" />,
        children: [{ path: 'audits', element: <AuditsPage /> }],
      },
      {
        element: <RequirePermission permission="NURSING.INCIDENT.VIEW" />,
        children: [{ path: 'incidents', element: <IncidentRegisterPage /> }],
      },
      {
        element: <RequirePermission permission="QUALITY.NONCONFORMANCE.VIEW" />,
        children: [{ path: 'non-conformances', element: <NonConformancesPage /> }],
      },
      {
        element: <RequirePermission permission="QUALITY.CAPA.VIEW" />,
        children: [{ path: 'capas', element: <CapaTrackerPage /> }],
      },
      {
        element: <RequirePermission permission="QUALITY.RISK.VIEW" />,
        children: [{ path: 'risks', element: <RiskRegisterPage /> }],
      },
      {
        element: <RequirePermission permission="QUALITY.COMMITTEE.VIEW" />,
        children: [
          { path: 'committee-meetings', element: <CommitteeMeetingsPage /> },
          { path: 'committees', element: <CommitteesPage /> },
          { path: 'nabh-standards', element: <NabhStandardsPage /> },
        ],
      },
      {
        element: <RequirePermission permission="QUALITY.EVIDENCE.VIEW" />,
        children: [{ path: 'evidence', element: <NabhEvidencePage /> }],
      },
      {
        element: <RequirePermission permission="QUALITY.DASHBOARD.VIEW" />,
        children: [{ path: 'dashboard', element: <QualityDashboardPage /> }],
      },
    ],
  },
  {
    path: 'insurance',
    children: [
      {
        element: <RequirePermission permission="INSURANCE.PREAUTH.VIEW" />,
        children: [{ index: true, element: <PreAuthWorklistPage /> }, { path: 'pre-auth', element: <PreAuthWorklistPage /> }],
      },
      {
        element: <RequirePermission permission="INSURANCE.CLAIM.VIEW" />,
        children: [{ path: 'claims', element: <ClaimsWorklistPage /> }],
      },
    ],
  },
  {
    path: 'doctor-portal',
    children: [
      {
        element: <RequirePermission permission="DOCTORPORTAL.DASHBOARD.VIEW" />,
        children: [{ index: true, element: <DoctorDashboardPage /> }],
      },
      {
        element: <RequirePermission permission="DOCTORPORTAL.QUEUE.VIEW" />,
        children: [{ path: 'queue', element: <MyQueuePage /> }],
      },
      {
        element: <RequirePermission permission="DOCTORPORTAL.IPROUNDS.VIEW" />,
        children: [{ path: 'inpatients', element: <MyInpatientsPage /> }],
      },
      {
        element: <RequirePermission permission="DOCTORPORTAL.ORDERS.VIEW" />,
        children: [{ path: 'orders', element: <MyOrdersResultsPage /> }],
      },
      {
        element: <RequirePermission permission="DOCTORPORTAL.DISCHARGE.VIEW" />,
        children: [{ path: 'discharge-approvals', element: <DischargeApprovalsPage /> }],
      },
      {
        element: <RequirePermission permission="DOCTORPORTAL.OPINION.VIEW" />,
        children: [{ path: 'opinion-requests', element: <OpinionRequestsPage /> }],
      },
    ],
  },
  {
    path: 'reports',
    children: [
      {
        element: <RequirePermission permission="DASHBOARD.MANAGEMENT.VIEW" />,
        children: [{ path: 'management', element: <ManagementDashboardPage /> }],
      },
      {
        element: <RequirePermission permission="DASHBOARD.OP.VIEW" />,
        children: [{ path: 'op', element: <OpDashboardPage /> }],
      },
      {
        element: <RequirePermission permission="DASHBOARD.IP.VIEW" />,
        children: [{ path: 'ip', element: <IpDashboardPage /> }],
      },
      {
        element: <RequirePermission permission="DASHBOARD.BILLING.VIEW" />,
        children: [{ path: 'billing', element: <BillingDashboardPage /> }],
      },
      {
        element: <RequirePermission permission="DASHBOARD.REVENUE.VIEW" />,
        children: [{ path: 'revenue', element: <RevenueDashboardPage /> }],
      },
      {
        element: <RequirePermission permission="REPORT.CATALOG.VIEW" />,
        children: [{ path: 'custom', element: <CustomReportsPage /> }],
      },
      {
        element: <RequirePermission permission="REPORT.SUBSCRIPTION.MANAGE" />,
        children: [{ path: 'subscriptions', element: <ReportSubscriptionsPage /> }],
      },
      {
        element: <RequirePermission permission="REPORT.CENSUS.VIEW" />,
        children: [{ path: 'census', element: <CensusReportPage /> }],
      },
    ],
  },
  {
    path: 'masters',
    children: [
      {
        element: <RequirePermission permission="MASTER.DEPARTMENT.VIEW" />,
        children: [{ path: 'departments', element: <DepartmentsPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.SPECIALTY.VIEW" />,
        children: [{ path: 'specialties', element: <SpecialtiesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.PATIENTCATEGORY.VIEW" />,
        children: [{ path: 'patient-categories', element: <PatientCategoriesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.DOCTOR.VIEW" />,
        children: [{ path: 'doctors', element: <DoctorsPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.EMPLOYEE.VIEW" />,
        children: [{ path: 'employees', element: <EmployeesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.SERVICE.VIEW" />,
        children: [{ path: 'services', element: <ServicesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.TARIFF.VIEW" />,
        children: [{ path: 'tariffs', element: <TariffsPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.PACKAGE.VIEW" />,
        children: [{ path: 'packages', element: <PackagesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.LOCATION.VIEW" />,
        children: [{ path: 'locations', element: <LocationsPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.VENDOR.VIEW" />,
        children: [{ path: 'vendors', element: <VendorsPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.ITEM.VIEW" />,
        children: [{ path: 'items', element: <ItemsPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.DRUG.VIEW" />,
        children: [{ path: 'drugs', element: <DrugsPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.TAX.VIEW" />,
        children: [{ path: 'taxes', element: <TaxesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.DISCOUNTTYPE.VIEW" />,
        children: [{ path: 'discount-types', element: <DiscountTypesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.REFERRER.VIEW" />,
        children: [{ path: 'referrers', element: <ReferrersPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.ICD.VIEW" />,
        children: [{ path: 'icd-codes', element: <IcdCodesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.CORPORATE.VIEW" />,
        children: [{ path: 'corporates', element: <CorporatesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.INSURANCE.VIEW" />,
        children: [{ path: 'insurance', element: <InsurancePage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.UOM.VIEW" />,
        children: [{ path: 'uoms', element: <UomsPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.SAMPLETYPE.VIEW" />,
        children: [{ path: 'sample-types', element: <SampleTypesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.MODALITY.VIEW" />,
        children: [{ path: 'modalities', element: <ModalitiesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.CONSENTTEMPLATE.VIEW" />,
        children: [{ path: 'consent-templates', element: <ConsentTemplatesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.RECORDDOCUMENTTYPE.VIEW" />,
        children: [{ path: 'document-types', element: <DocumentTypesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.BILLESTIMATETEMPLATE.VIEW" />,
        children: [{ path: 'bill-estimate-templates', element: <BillEstimateTemplatesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.DOCTORPAYOUTRULE.VIEW" />,
        children: [{ path: 'doctor-payout-rules', element: <DoctorPayoutRulesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.APPOINTMENTTYPE.VIEW" />,
        children: [{ path: 'appointment-types', element: <AppointmentTypesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.CAMP.VIEW" />,
        children: [{ path: 'camps', element: <CampsPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.STATE.VIEW" />,
        children: [{ path: 'states', element: <StatesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.CITY.VIEW" />,
        children: [{ path: 'cities', element: <CitiesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.AREA.VIEW" />,
        children: [{ path: 'areas', element: <AreasPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.NATIONALITY.VIEW" />,
        children: [{ path: 'nationalities', element: <NationalitiesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.RELIGION.VIEW" />,
        children: [{ path: 'religions', element: <ReligionsPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.LANGUAGE.VIEW" />,
        children: [{ path: 'languages', element: <LanguagesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.SPECIALLYABLEDCATEGORY.VIEW" />,
        children: [{ path: 'specially-abled-categories', element: <SpeciallyAbledCategoriesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.PATIENTHCTAG.VIEW" />,
        children: [{ path: 'patient-hc-tags', element: <PatientHcTagsPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.PATIENTCASETYPE.VIEW" />,
        children: [{ path: 'patient-case-types', element: <PatientCaseTypesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.MLCTYPE.VIEW" />,
        children: [{ path: 'mlc-types', element: <MlcTypesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.MHCDOCTOR.VIEW" />,
        children: [{ path: 'mhc-doctors', element: <MhcDoctorsPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.MHCPACKAGE.VIEW" />,
        children: [{ path: 'mhc-packages', element: <MhcPackagesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.LOYALTYPROGRAM.VIEW" />,
        children: [{ path: 'loyalty-programs', element: <LoyaltyProgramsPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.UNIT.VIEW" />,
        children: [{ path: 'units', element: <UnitsPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.LOYALTYCARDCATEGORY.VIEW" />,
        children: [{ path: 'loyalty-card-categories', element: <LoyaltyCardCategoriesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.MEMBERRELATIONTAG.VIEW" />,
        children: [{ path: 'member-relation-tags', element: <MemberRelationTagsPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.SALUTATION.VIEW" />,
        children: [{ path: 'salutations', element: <SalutationsPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.MARITALSTATUS.VIEW" />,
        children: [{ path: 'marital-statuses', element: <MaritalStatusesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.IDENTITYDOCUMENTTYPE.VIEW" />,
        children: [{ path: 'identity-document-types', element: <IdentityDocumentTypesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.VISITCLASS.VIEW" />,
        children: [{ path: 'visit-classes', element: <VisitClassesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.VISITSESSION.VIEW" />,
        children: [{ path: 'visit-sessions', element: <VisitSessionsPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.CLAIMDOCUMENTTYPE.VIEW" />,
        children: [{ path: 'claim-document-types', element: <ClaimDocumentTypesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.CLAIMDENIALREASON.VIEW" />,
        children: [{ path: 'claim-denial-reasons', element: <ClaimDenialReasonsPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.LOYALTYCARDTYPE.VIEW" />,
        children: [{ path: 'loyalty-card-types', element: <LoyaltyCardTypesPage /> }],
      },
      {
        element: <RequirePermission permission="MASTER.LOYALTYCARDPRICE.VIEW" />,
        children: [{ path: 'loyalty-card-prices', element: <LoyaltyCardPricesPage /> }],
      },
    ],
  },
  {
    path: 'admin',
    children: [
      {
        element: <RequirePermission permission="ADMIN.USER.VIEW" />,
        children: [{ path: 'users', element: <UsersPage /> }],
      },
      {
        element: <RequirePermission permission="ADMIN.ROLE.VIEW" />,
        children: [{ path: 'roles', element: <RolesPage /> }],
      },
      {
        element: <RequirePermission permission="ADMIN.MENU.VIEW" />,
        children: [{ path: 'menus', element: <MenusPage /> }],
      },
      {
        element: <RequirePermission permission="ADMIN.BRANCH.VIEW" />,
        children: [{ path: 'branches', element: <BranchesPage /> }],
      },
      {
        element: <RequirePermission permission="ADMIN.DESIGNATION.VIEW" />,
        children: [{ path: 'designations', element: <DesignationsPage /> }],
      },
      {
        element: <RequirePermission permission="ADMIN.SETTING.VIEW" />,
        children: [
          { path: 'settings', element: <SettingsPage /> },
          { path: 'abdm', element: <AbdmAdminPage /> },
        ],
      },
      {
        element: <RequirePermission permission="ADMIN.SESSION.VIEW" />,
        children: [{ path: 'sessions', element: <SessionsPage /> }],
      },
      {
        element: <RequirePermission permission="ADMIN.AUDIT.VIEW" />,
        children: [{ path: 'audit-logs', element: <AuditLogsPage /> }],
      },
      {
        element: <RequirePermission permission="ADMIN.ACTIVITY.VIEW" />,
        children: [{ path: 'activity-logs', element: <ActivityLogsPage /> }],
      },
    ],
  },
]
