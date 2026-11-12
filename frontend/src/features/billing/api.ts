import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse, PagedQuery, PagedResult } from '../../api/types/api'
import type { ReportResult } from '../../components/ui/ReportResultTable'
import type {
  Advance,
  AddBillNotePayload,
  AddManualBillItemPayload,
  ApplyDiscountPayload,
  BillDetail,
  BillEstimateDetail,
  BillEstimateListItem,
  BillingReportExtraFilters,
  BillingReportRange,
  BillListItem,
  BillNote,
  BillStatus,
  BillType,
  CancelBillPayload,
  CashierShift,
  CashierShiftStatus,
  CloseCashierShiftPayload,
  CloseDayPayload,
  CollectAdvancePayload,
  CollectPaymentPayload,
  ConcessionSearchResult,
  CreateBillEstimatePayload,
  CreditNote,
  DailyCollection,
  DailyCollectionMode,
  DayClose,
  DecideCreditNotePayload,
  DecideDiscountPayload,
  DecideRefundPayload,
  DiscountApproval,
  DrawAdvanceOnBillPayload,
  GenerateOpBillPayload,
  MakeCreditBillPayload,
  OnlinePaymentRequest,
  OnlinePaymentRequestStatus,
  OpBillingSearchResult,
  OpenCashierShiftPayload,
  ParkBillPayload,
  PaymentMode,
  Refund,
  RequestAdvanceRefundPayload,
  RequestCreditNotePayload,
  RequestRefundPayload,
  SavePaymentModePayload,
  TransferAdvancePayload,
  UpdateBillItemPayload,
} from './types'

export interface BillQuery extends PagedQuery {
  patientId?: number
  fromDate?: string
  toDate?: string
  status?: BillStatus
  billType?: BillType
  corporateId?: number
  billedByUserId?: number
}

export interface OpBillingSearchQuery extends PagedQuery {
  fromDate?: string
  toDate?: string
  patientName?: string
  doctorName?: string
  regNo?: string
  mobile?: string
  gender?: number
  ipNumber?: string
  mrdOnly?: boolean
  emrOnly?: boolean
  packageOnly?: boolean
}

/** "OP Bill Concession Search" — tokenNo is the OPD visit's daily token number (legacy "OT No"). */
export interface ConcessionSearchQuery extends PagedQuery {
  fromDate?: string
  toDate?: string
  regNo?: string
  patientName?: string
  tokenNo?: number
  billNo?: string
}

const b = endpoints.billing

async function get<T>(url: string, params?: object): Promise<T> {
  const { data } = await api.get<ApiResponse<T>>(url, { params })
  return data.data!
}

function useInvalidating<TArgs, TResult = unknown>(fn: (args: TArgs) => Promise<TResult>, keys: string[][]) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: fn,
    onSuccess: () => keys.forEach((key) => queryClient.invalidateQueries({ queryKey: key })),
  })
}

const billKeys = [['billing', 'bills']]
const billDetailKeys = (id: number) => [
  ['billing', 'bills', 'detail', String(id)],
  ['billing', 'bills'],
  ['billing', 'discount-approvals'],
  ['billing', 'refunds'],
]

// ── Bills ────────────────────────────────────────────────────────────────
export const useBills = (query: BillQuery) =>
  useQuery({
    queryKey: ['billing', 'bills', 'list', query],
    queryFn: () => get<PagedResult<BillListItem>>(b.bills, query),
  })

export const useBill = (id: number | null) =>
  useQuery({
    queryKey: ['billing', 'bills', 'detail', id],
    queryFn: () => get<BillDetail>(`${b.bills}/${id}`),
    enabled: id !== null,
  })

/** OP Billing's own patient search (legacy "OP Bill Payment" search) — visits, not patient master records. */
export const useOpBillingSearch = (query: OpBillingSearchQuery) =>
  useQuery({
    queryKey: ['billing', 'op-search', query],
    queryFn: () => get<PagedResult<OpBillingSearchResult>>(b.opSearch, query),
  })

export const useGenerateOpBill = () =>
  useInvalidating(async (payload: GenerateOpBillPayload) => {
    const { data } = await api.post<ApiResponse<BillDetail>>(`${b.bills}/generate-op`, payload)
    return data.data!
  }, billKeys)

export const useGenerateEmergencyBill = () =>
  useInvalidating(async (emergencyVisitId: number) => {
    const { data } = await api.post<ApiResponse<BillDetail>>(`${b.bills}/generate-emergency/${emergencyVisitId}`)
    return data.data!
  }, billKeys)

export const useAddBillItem = (billId: number) =>
  useInvalidating((payload: AddManualBillItemPayload) => api.post(`${b.bills}/${billId}/items`, payload), billDetailKeys(billId))

export const useUpdateBillItem = (billId: number) =>
  useInvalidating(
    ({ itemId, ...payload }: UpdateBillItemPayload & { itemId: number }) =>
      api.put(`${b.bills}/${billId}/items/${itemId}`, payload),
    billDetailKeys(billId),
  )

export const useRemoveBillItem = (billId: number) =>
  useInvalidating((itemId: number) => api.delete(`${b.bills}/${billId}/items/${itemId}`), billDetailKeys(billId))

export const useApplyItemDiscount = (billId: number) =>
  useInvalidating(
    ({ itemId, ...payload }: ApplyDiscountPayload & { itemId: number }) =>
      api.post(`${b.bills}/${billId}/items/${itemId}/discount`, payload),
    billDetailKeys(billId),
  )

export const useApplyBillDiscount = (billId: number) =>
  useInvalidating((payload: ApplyDiscountPayload) => api.post(`${b.bills}/${billId}/discount`, payload), billDetailKeys(billId))

export const useCollectPayment = (billId: number) =>
  useInvalidating((payload: CollectPaymentPayload) => api.post(`${b.bills}/${billId}/payments`, payload), billDetailKeys(billId))

export const useCancelBill = (billId: number) =>
  useInvalidating((payload: CancelBillPayload) => api.post(`${b.bills}/${billId}/cancel`, payload), billDetailKeys(billId))

export const useMakeCreditBill = (billId: number) =>
  useInvalidating((payload: MakeCreditBillPayload) => api.post(`${b.bills}/${billId}/credit`, payload), billDetailKeys(billId))

export const useRequestRefund = (billId: number) =>
  useInvalidating((payload: RequestRefundPayload) => api.post(`${b.bills}/${billId}/refunds`, payload), billDetailKeys(billId))

export const useDrawAdvanceOnBill = (billId: number) =>
  useInvalidating(
    (payload: DrawAdvanceOnBillPayload) => api.post(`${b.bills}/${billId}/advances/draw`, payload),
    [...billDetailKeys(billId), ['billing', 'advances']],
  )

export const useParkBill = (billId: number) =>
  useInvalidating((payload: ParkBillPayload) => api.post(`${b.bills}/${billId}/park`, payload), billDetailKeys(billId))

export const useUnparkBill = (billId: number) =>
  useInvalidating(() => api.post(`${b.bills}/${billId}/unpark`), billDetailKeys(billId))

// ── Bill notes ───────────────────────────────────────────────────────────
export const useBillNotes = (billId: number | null) =>
  useQuery({
    queryKey: ['billing', 'bills', 'notes', billId],
    queryFn: () => get<BillNote[]>(`${b.bills}/${billId}/notes`),
    enabled: billId !== null,
  })

export const useAddBillNote = (billId: number) =>
  useInvalidating(
    (payload: AddBillNotePayload) => api.post(`${b.bills}/${billId}/notes`, payload),
    [['billing', 'bills', 'notes', String(billId)]],
  )

// ── Credit notes ─────────────────────────────────────────────────────────
export const useRequestCreditNote = (billId: number) =>
  useInvalidating(
    (payload: RequestCreditNotePayload) => api.post(`${b.bills}/${billId}/credit-notes`, payload),
    [...billDetailKeys(billId), ['billing', 'credit-notes']],
  )

export const usePendingCreditNotes = () =>
  useQuery({ queryKey: ['billing', 'credit-notes'], queryFn: () => get<CreditNote[]>(b.creditNotes) })

export const useDecideCreditNote = () =>
  useInvalidating(
    ({ id, ...payload }: DecideCreditNotePayload & { id: number }) => api.post(`${b.creditNotes}/${id}/decide`, payload),
    [['billing', 'credit-notes'], ['billing', 'bills']],
  )

async function fetchBlob(url: string): Promise<Blob> {
  const { data } = await api.get(url, { responseType: 'blob' })
  return data as Blob
}

export async function openBillReceipt(billId: number, paymentId?: number) {
  const blob = await fetchBlob(`${b.bills}/${billId}/receipt${paymentId ? `?paymentId=${paymentId}` : ''}`)
  const url = URL.createObjectURL(blob)
  window.open(url, '_blank', 'noopener,noreferrer')
  setTimeout(() => URL.revokeObjectURL(url), 60_000)
}

export type ReportExportFormat = 'csv' | 'excel' | 'pdf'

async function downloadFile(url: string, params: object, filename: string) {
  const { data } = await api.get(url, { params, responseType: 'blob' })
  const objectUrl = URL.createObjectURL(data as Blob)
  const link = document.createElement('a')
  link.href = objectUrl
  link.download = filename
  document.body.appendChild(link)
  link.click()
  link.remove()
  setTimeout(() => URL.revokeObjectURL(objectUrl), 60_000)
}

function extensionFor(format: ReportExportFormat) {
  return format === 'excel' ? 'xlsx' : format
}

/** "OP Bill Report" / "IP Bill Generated Report" export — same filters as useBills. */
export async function downloadBillReport(query: BillQuery, format: ReportExportFormat) {
  await downloadFile(`${b.bills}/export`, { ...query, format }, `bill-report.${extensionFor(format)}`)
}

/** Export for any BillingReportsPage tab (ageing, concession, refund, advance, etc.). */
export async function downloadBillingReport(
  key: string,
  format: ReportExportFormat,
  range: { fromDate?: string; toDate?: string },
  date?: string,
  extra?: BillingReportExtraFilters,
) {
  await downloadFile(`${b.reports}/${key}/export`, { format, ...range, date, ...extra }, `${key}.${extensionFor(format)}`)
}

// ── Discount approvals ───────────────────────────────────────────────────
export const usePendingDiscountApprovals = () =>
  useQuery({
    queryKey: ['billing', 'discount-approvals'],
    queryFn: () => get<DiscountApproval[]>(b.discountApprovals),
  })

export const useDecideDiscount = () =>
  useInvalidating(
    ({ id, ...payload }: DecideDiscountPayload & { id: number }) =>
      api.post(`${b.discountApprovals}/${id}/decide`, payload),
    [['billing', 'discount-approvals'], ['billing', 'bills']],
  )

// ── Refunds ──────────────────────────────────────────────────────────────
export const usePendingRefunds = () =>
  useQuery({ queryKey: ['billing', 'refunds'], queryFn: () => get<Refund[]>(b.refunds) })

export const useDecideRefund = () =>
  useInvalidating(
    ({ id, ...payload }: DecideRefundPayload & { id: number }) => api.post(`${b.refunds}/${id}/decide`, payload),
    [['billing', 'refunds'], ['billing', 'refunds', 'issuable'], ['billing', 'bills']],
  )

/** Approved-but-not-yet-paid-out refunds — the Issue queue. */
export const useIssuableRefunds = () =>
  useQuery({ queryKey: ['billing', 'refunds', 'issuable'], queryFn: () => get<Refund[]>(`${b.refunds}/issuable`) })

// ── Online payment requests (Portal "Pay Online" follow-up worklist) ─────
export const useOnlinePaymentRequests = (status?: OnlinePaymentRequestStatus) =>
  useQuery({
    queryKey: ['billing', 'online-payment-requests', status],
    queryFn: () => get<OnlinePaymentRequest[]>(b.onlinePaymentRequests, { status }),
  })
export const useCompleteOnlinePaymentRequest = () =>
  useInvalidating((id: number) => api.post(`${b.onlinePaymentRequests}/${id}/complete`), [['billing', 'online-payment-requests']])
export const useCancelOnlinePaymentRequest = () =>
  useInvalidating((id: number) => api.post(`${b.onlinePaymentRequests}/${id}/cancel`), [['billing', 'online-payment-requests']])

export const useIssueRefund = () =>
  useInvalidating(
    (id: number) => api.post(`${b.refunds}/${id}/issue`),
    [['billing', 'refunds'], ['billing', 'refunds', 'issuable'], ['billing', 'bills'], ['billing', 'advances']],
  )

// ── Advances ─────────────────────────────────────────────────────────────
export const useCollectAdvance = () =>
  useInvalidating((payload: CollectAdvancePayload) => api.post(b.advances, payload), [['billing', 'advances']])

export const useRequestAdvanceRefund = (advanceId: number) =>
  useInvalidating(
    (payload: RequestAdvanceRefundPayload) => api.post(`${b.advances}/${advanceId}/refunds`, payload),
    [['billing', 'advances'], ['billing', 'refunds']],
  )

export const useTransferAdvance = (advanceId: number) =>
  useInvalidating(
    (payload: TransferAdvancePayload) => api.post(`${b.advances}/${advanceId}/transfer`, payload),
    [['billing', 'advances']],
  )

export const useAdvancesByPatient = (patientId: number | null) =>
  useQuery({
    queryKey: ['billing', 'advances', patientId],
    queryFn: () => get<Advance[]>(`${b.advances}/patient/${patientId}`),
    enabled: patientId !== null,
  })

// ── Day close ────────────────────────────────────────────────────────────
export const useDailyCollection = (date: string) =>
  useQuery({
    queryKey: ['billing', 'day-close', 'collection', date],
    queryFn: () => get<DailyCollection>(`${b.dayClose}/collection`, { date }),
  })

export const useDayClose = (date: string) =>
  useQuery({
    queryKey: ['billing', 'day-close', date],
    queryFn: () => get<DayClose | null>(b.dayClose, { date }),
  })

export const useCloseDay = () =>
  useInvalidating(
    (payload: CloseDayPayload) => api.post(`${b.dayClose}/close`, payload),
    [['billing', 'day-close']],
  )

// ── Payment modes ────────────────────────────────────────────────────────
export const usePaymentModes = () =>
  useQuery({ queryKey: ['billing', 'payment-modes'], queryFn: () => get<PaymentMode[]>(b.paymentModes) })

export const useSavePaymentMode = () =>
  useInvalidating(
    ({ id, ...payload }: SavePaymentModePayload) =>
      id ? api.put(`${b.paymentModes}/${id}`, payload) : api.post(b.paymentModes, payload),
    [['billing', 'payment-modes']],
  )

export const useDeletePaymentMode = () =>
  useInvalidating((id: number) => api.delete(`${b.paymentModes}/${id}`), [['billing', 'payment-modes']])

// ── Cashier shifts ───────────────────────────────────────────────────────
export const useCashierShifts = (query: PagedQuery & { status?: CashierShiftStatus }) =>
  useQuery({
    queryKey: ['billing', 'cashier-shifts', 'list', query],
    queryFn: () => get<PagedResult<CashierShift>>(b.cashierShifts, query),
  })

export const useMyCashierShift = () =>
  useQuery({
    queryKey: ['billing', 'cashier-shifts', 'mine'],
    queryFn: () => get<CashierShift | null>(`${b.cashierShifts}/mine`),
  })

export const useCashierShiftCollection = (id: number | null) =>
  useQuery({
    queryKey: ['billing', 'cashier-shifts', 'collection', id],
    queryFn: () => get<DailyCollectionMode[]>(`${b.cashierShifts}/${id}/collection`),
    enabled: id !== null,
  })

const cashierShiftKeys = [['billing', 'cashier-shifts', 'list'], ['billing', 'cashier-shifts', 'mine']]

export const useOpenCashierShift = () =>
  useInvalidating((payload: OpenCashierShiftPayload) => api.post(`${b.cashierShifts}/open`, payload), cashierShiftKeys)

export const useCloseCashierShift = (id: number) =>
  useInvalidating((payload: CloseCashierShiftPayload) => api.post(`${b.cashierShifts}/${id}/close`, payload), cashierShiftKeys)

export const useReceiveCashierShift = () =>
  useInvalidating((id: number) => api.post(`${b.cashierShifts}/${id}/receive`), cashierShiftKeys)

// ── Bill estimates ───────────────────────────────────────────────────────
export const useBillEstimates = (query: PagedQuery & { patientId?: number }) =>
  useQuery({
    queryKey: ['billing', 'estimates', 'list', query],
    queryFn: () => get<PagedResult<BillEstimateListItem>>(b.estimates, query),
  })

export const useBillEstimate = (id: number | null) =>
  useQuery({
    queryKey: ['billing', 'estimates', 'detail', id],
    queryFn: () => get<BillEstimateDetail>(`${b.estimates}/${id}`),
    enabled: id !== null,
  })

const estimateKeys = [['billing', 'estimates']]

export const useCreateBillEstimate = () =>
  useInvalidating((payload: CreateBillEstimatePayload) => api.post(b.estimates, payload), estimateKeys)

export const useCopyBillEstimate = () =>
  useInvalidating((id: number) => api.post(`${b.estimates}/${id}/copy`), estimateKeys)

export const useDeleteBillEstimate = () =>
  useInvalidating((id: number) => api.delete(`${b.estimates}/${id}`), estimateKeys)

export async function openBillEstimatePdf(id: number) {
  const blob = await fetchBlob(`${b.estimates}/${id}/pdf`)
  const url = URL.createObjectURL(blob)
  window.open(url, '_blank', 'noopener,noreferrer')
  setTimeout(() => URL.revokeObjectURL(url), 60_000)
}

// ── Billing reports ──────────────────────────────────────────────────────
/**
 * Generic (columns, rows) result — one hook for every plain-list report tab (ageing,
 * ward-wise-outstanding, day-book, cancelled-bills, concession, refund, advance,
 * tariff-list, doctors-payout, user-settlement, service-category-breakup,
 * ip-service-billing-detail), matching the backend's single GetResult dispatcher
 * (BillingReportsController) and rendered by the shared ReportResultTable — same
 * "OP Report" pattern as opd/api.ts's useOpVisitsReport.
 */
export const useBillingReportResult = (key: string, range: BillingReportRange, date?: string, extra?: BillingReportExtraFilters) =>
  useQuery({
    queryKey: ['billing', 'reports', key, range, date, extra],
    queryFn: () => get<ReportResult>(`${b.reports}/${key}`, { ...range, date, ...extra }),
  })

export const useConcessionSearch = (query: ConcessionSearchQuery) =>
  useQuery({
    queryKey: ['billing', 'reports', 'concession', query],
    queryFn: () => get<PagedResult<ConcessionSearchResult>>(`${b.reports}/concession/search`, query),
  })
