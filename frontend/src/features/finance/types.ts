export type AccountType = 1 | 2 | 3 | 4 | 5 // Asset, Liability, Equity, Income, Expense
export type VoucherType = 1 | 2 | 3 // Journal, Payment, Receipt
export type VoucherStatus = 1 | 2 // Posted, Cancelled
export type PayableStatus = 1 | 2 | 3 // Open, PartiallyPaid, Paid
export type ReceivableStatus = 1 | 2 | 3 // Open, PartiallyCollected, Collected

export const ACCOUNT_TYPE_LABELS: Record<AccountType, string> = {
  1: 'Asset', 2: 'Liability', 3: 'Equity', 4: 'Income', 5: 'Expense',
}
export const VOUCHER_TYPE_LABELS: Record<VoucherType, string> = { 1: 'Journal', 2: 'Payment', 3: 'Receipt' }
export const VOUCHER_STATUS_LABELS: Record<VoucherStatus, string> = { 1: 'Posted', 2: 'Cancelled' }
export const PAYABLE_STATUS_LABELS: Record<PayableStatus, string> = { 1: 'Open', 2: 'Partially Paid', 3: 'Paid' }
export const RECEIVABLE_STATUS_LABELS: Record<ReceivableStatus, string> = { 1: 'Open', 2: 'Partially Collected', 3: 'Collected' }

// ── Accounts ─────────────────────────────────────────────────────────────────
export interface AccountDto {
  id: number
  code: string
  name: string
  accountType: AccountType
  parentAccountId: number | null
  parentAccountName: string | null
  isSystemAccount: boolean
  description: string | null
  isActive: boolean
}

export interface SaveAccountPayload {
  code: string
  name: string
  accountType: AccountType
  parentAccountId: number | null
  description: string | null
}

// ── Vouchers / Ledger ────────────────────────────────────────────────────────
export interface VoucherLineDto {
  accountId: number
  accountCode: string
  accountName: string
  debitAmount: number
  creditAmount: number
  narration: string | null
}

export interface VoucherDto {
  id: number
  voucherNumber: string
  voucherType: VoucherType
  voucherDate: string
  narration: string
  status: VoucherStatus
  postedByName: string
  postedAt: string
  reversalOfVoucherId: number | null
  cancelReason: string | null
  sourceReference: string | null
  lines: VoucherLineDto[]
}

export interface PostVoucherLinePayload {
  accountId: number
  debitAmount: number
  creditAmount: number
  narration: string | null
}

export interface PostVoucherPayload {
  voucherType: VoucherType
  voucherDate: string
  narration: string
  lines: PostVoucherLinePayload[]
}

export interface LedgerEntryRowDto {
  id: number
  entryDate: string
  voucherNumber: string
  narration: string
  debitAmount: number
  creditAmount: number
  runningBalance: number
}

export interface AccountLedgerDto {
  accountId: number
  accountCode: string
  accountName: string
  openingBalance: number
  entries: LedgerEntryRowDto[]
  closingBalance: number
}

// ── Payables ─────────────────────────────────────────────────────────────────
export interface PendingGrnDto {
  grnId: number
  grnNumber: string
  vendorId: number
  vendorName: string
  receivedDate: string
  supplierInvoiceNumber: string | null
  totalAmount: number
}

export interface PayableDto {
  id: number
  vendorId: number
  vendorName: string
  grnId: number
  grnNumber: string
  invoiceNumber: string
  invoiceDate: string
  invoiceAmount: number
  paidAmount: number
  balanceAmount: number
  dueDate: string | null
  status: PayableStatus
}

export interface AgingRowDto {
  entityName: string
  bucket: string
  balanceAmount: number
}

// ── Receivables ──────────────────────────────────────────────────────────────
export interface PendingCreditBillDto {
  billId: number
  billNumber: string
  corporateId: number
  corporateName: string
  billDate: string
  totalAmount: number
}

export interface ReceivableDto {
  id: number
  corporateId: number
  corporateName: string
  billId: number
  billNumber: string
  billDate: string
  billAmount: number
  collectedAmount: number
  balanceAmount: number
  status: ReceivableStatus
}

// ── Reports ───────────────────────────────────────────────────────────────────
export interface CashBankCollectionRowDto {
  paymentModeId: number
  paymentModeName: string
  amount: number
  paymentCount: number
}
export interface CashBankCollectionDto {
  byMode: CashBankCollectionRowDto[]
  total: number
}

export interface RevenueByDepartmentRowDto {
  departmentId: number | null
  departmentName: string
  amount: number
}
export interface RevenueByDoctorRowDto {
  doctorId: number | null
  doctorName: string
  amount: number
}
export interface RevenueReportDto {
  byDepartment: RevenueByDepartmentRowDto[]
  byDoctor: RevenueByDoctorRowDto[]
}

export interface GstOutwardRowDto {
  source: string
  taxPercent: number
  taxableAmount: number
  taxAmount: number
}
export interface HsnSummaryRowDto {
  hsnCode: string
  quantity: number
  taxableAmount: number
  taxAmount: number
}
export interface GstReportDto {
  outwardRegister: GstOutwardRowDto[]
  hsnSummary: HsnSummaryRowDto[]
}

export interface OutstandingBillRowDto {
  billId: number
  billNumber: string
  billDate: string
  patientName: string
  isCreditBill: boolean
  corporateName: string | null
  totalAmount: number
  balanceAmount: number
}
export interface OutstandingReportDto {
  bills: OutstandingBillRowDto[]
  totalOutstanding: number
}

export interface TrialBalanceRowDto {
  accountId: number
  accountCode: string
  accountName: string
  accountType: string
  debitTotal: number
  creditTotal: number
}
export interface TrialBalanceDto {
  asOfDate: string
  rows: TrialBalanceRowDto[]
  totalDebit: number
  totalCredit: number
}

export interface DayCloseConsoleRowDto {
  branchId: number
  branchName: string
  isClosed: boolean
  systemTotal: number | null
  declaredTotal: number | null
  variance: number | null
}
