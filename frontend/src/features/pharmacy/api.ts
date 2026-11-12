import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse, PagedQuery, PagedResult } from '../../api/types/api'
import type {
  Batch,
  BatchAvailability,
  CreateGrnPayload,
  CreateIndentPayload,
  CreateIssuePayload,
  CreateIssueReturnPayload,
  CreatePurchaseOrderPayload,
  CreatePurchaseRequestPayload,
  CreatePurchaseReturnPayload,
  CreateSalePayload,
  CreateSalesReturnPayload,
  CreateStockAdjustmentPayload,
  CreateStockTransferPayload,
  DecideIndentPayload,
  DocumentStatus,
  GrnDetail,
  GrnListItem,
  IndentDetail,
  IndentListItem,
  IndentStatus,
  IssueDetail,
  IssueListItem,
  IssueReturnListItem,
  IssueType,
  LowStockProduct,
  NearExpiryBatch,
  PharmacySaleDetail,
  PharmacySaleListItem,
  PharmacySaleStatus,
  ProductConsumptionRow,
  PurchaseOrderDetail,
  PurchaseOrderListItem,
  PurchaseOrderStatus,
  PurchaseRequestDetail,
  PurchaseRequestListItem,
  PurchaseRequestStatus,
  PurchaseReturnDetail,
  PurchaseReturnListItem,
  Rack,
  SalesReturnListItem,
  SaveRackPayload,
  SaveStorePayload,
  StockAdjustmentDetail,
  StockAdjustmentListItem,
  StockAuditDetail,
  StockAuditListItem,
  StockLedgerEntry,
  StockTransferDetail,
  StockTransferListItem,
  Store,
} from './types'

const p = endpoints.pharmacy

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

// ── Stores / racks ───────────────────────────────────────────────────────
export const useStores = () => useQuery({ queryKey: ['pharmacy', 'stores'], queryFn: () => get<Store[]>(p.stores) })

export const useSaveStore = () =>
  useInvalidating(
    ({ id, ...payload }: SaveStorePayload) => (id ? api.put(`${p.stores}/${id}`, payload) : api.post(p.stores, payload)),
    [['pharmacy', 'stores']],
  )

export const useRacks = (storeId?: number) =>
  useQuery({ queryKey: ['pharmacy', 'racks', storeId], queryFn: () => get<Rack[]>(`${p.stores}/racks`, { storeId }) })

export const useSaveRack = () =>
  useInvalidating((payload: SaveRackPayload) => api.post(`${p.stores}/racks`, payload), [['pharmacy', 'racks']])

// ── Purchase requests ────────────────────────────────────────────────────
export interface PurchaseRequestFilter extends PagedQuery {
  status?: PurchaseRequestStatus
}
export const usePurchaseRequests = (query: PurchaseRequestFilter) =>
  useQuery({
    queryKey: ['pharmacy', 'purchase-requests', query],
    queryFn: () => get<PagedResult<PurchaseRequestListItem>>(p.purchaseRequests, query),
  })
export const usePurchaseRequest = (id: number | null) =>
  useQuery({
    queryKey: ['pharmacy', 'purchase-requests', 'detail', id],
    queryFn: () => get<PurchaseRequestDetail>(`${p.purchaseRequests}/${id}`),
    enabled: id !== null,
  })
export const useCreatePurchaseRequest = () =>
  useInvalidating((payload: CreatePurchaseRequestPayload) => api.post(p.purchaseRequests, payload), [['pharmacy', 'purchase-requests']])
export const useDecidePurchaseRequest = () =>
  useInvalidating(
    ({ id, approve }: { id: number; approve: boolean }) => api.post(`${p.purchaseRequests}/${id}/decide`, { approve }),
    [['pharmacy', 'purchase-requests']],
  )

// ── Purchase orders ──────────────────────────────────────────────────────
export interface PurchaseOrderFilter extends PagedQuery {
  status?: PurchaseOrderStatus
}
export const usePurchaseOrders = (query: PurchaseOrderFilter) =>
  useQuery({
    queryKey: ['pharmacy', 'purchase-orders', query],
    queryFn: () => get<PagedResult<PurchaseOrderListItem>>(p.purchaseOrders, query),
  })
export const usePurchaseOrder = (id: number | null) =>
  useQuery({
    queryKey: ['pharmacy', 'purchase-orders', 'detail', id],
    queryFn: () => get<PurchaseOrderDetail>(`${p.purchaseOrders}/${id}`),
    enabled: id !== null,
  })
export const useCreatePurchaseOrder = () =>
  useInvalidating((payload: CreatePurchaseOrderPayload) => api.post(p.purchaseOrders, payload), [['pharmacy', 'purchase-orders']])
export const useApprovePurchaseOrder = () =>
  useInvalidating((id: number) => api.post(`${p.purchaseOrders}/${id}/approve`), [['pharmacy', 'purchase-orders']])

// ── GRN ──────────────────────────────────────────────────────────────────
export interface GrnFilter extends PagedQuery {
  status?: DocumentStatus
}
export const useGrns = (query: GrnFilter) =>
  useQuery({ queryKey: ['pharmacy', 'grn', query], queryFn: () => get<PagedResult<GrnListItem>>(p.grn, query) })
export const useGrn = (id: number | null) =>
  useQuery({ queryKey: ['pharmacy', 'grn', 'detail', id], queryFn: () => get<GrnDetail>(`${p.grn}/${id}`), enabled: id !== null })
export const useCreateGrn = () =>
  useInvalidating(async (payload: CreateGrnPayload) => {
    const { data } = await api.post<ApiResponse<number>>(p.grn, payload)
    return data.data!
  }, [['pharmacy', 'grn']])
export const usePostGrn = () =>
  useInvalidating((id: number) => api.post(`${p.grn}/${id}/post`), [['pharmacy', 'grn'], ['pharmacy', 'stock']])

// ── Purchase returns ─────────────────────────────────────────────────────
export const usePurchaseReturns = () =>
  useQuery({ queryKey: ['pharmacy', 'purchase-returns'], queryFn: () => get<PurchaseReturnListItem[]>(p.purchaseReturns) })
export const usePurchaseReturn = (id: number | null) =>
  useQuery({
    queryKey: ['pharmacy', 'purchase-returns', 'detail', id],
    queryFn: () => get<PurchaseReturnDetail>(`${p.purchaseReturns}/${id}`),
    enabled: id !== null,
  })
export const useCreatePurchaseReturn = () =>
  useInvalidating((payload: CreatePurchaseReturnPayload) => api.post(p.purchaseReturns, payload), [
    ['pharmacy', 'purchase-returns'],
    ['pharmacy', 'stock'],
  ])

// ── Sales ────────────────────────────────────────────────────────────────
export const useBatchAvailability = (drugId: number | null, storeId: number | null) =>
  useQuery({
    queryKey: ['pharmacy', 'sales', 'batch-availability', drugId, storeId],
    queryFn: () => get<BatchAvailability[]>(`${p.sales}/batch-availability`, { drugId, storeId }),
    enabled: drugId !== null && storeId !== null,
  })

export interface PharmacySaleFilter extends PagedQuery {
  fromDate?: string
  toDate?: string
  status?: PharmacySaleStatus
}
export const usePharmacySales = (query: PharmacySaleFilter) =>
  useQuery({ queryKey: ['pharmacy', 'sales', 'list', query], queryFn: () => get<PagedResult<PharmacySaleListItem>>(p.sales, query) })
export const usePharmacySale = (id: number | null) =>
  useQuery({
    queryKey: ['pharmacy', 'sales', 'detail', id],
    queryFn: () => get<PharmacySaleDetail>(`${p.sales}/${id}`),
    enabled: id !== null,
  })
export const useCreateSale = () =>
  useInvalidating(async (payload: CreateSalePayload) => {
    const { data } = await api.post<ApiResponse<PharmacySaleDetail>>(p.sales, payload)
    return data.data!
  }, [['pharmacy', 'sales'], ['pharmacy', 'stock']])
export const useCancelSale = () =>
  useInvalidating(
    ({ id, reason }: { id: number; reason: string }) => api.post(`${p.sales}/${id}/cancel`, { reason }),
    [['pharmacy', 'sales'], ['pharmacy', 'stock']],
  )

/** "Pharmacy Pay Type Modify" — correct a completed sale's payment mode. */
export const useModifyPayType = () =>
  useInvalidating(
    ({ id, paymentModeId }: { id: number; paymentModeId: number | null }) =>
      api.put(`${p.sales}/${id}/pay-type`, { paymentModeId }),
    [['pharmacy', 'sales']],
  )

export async function openSaleInvoice(id: number) {
  const { data } = await api.get(`${p.sales}/${id}/invoice`, { responseType: 'blob' })
  const url = URL.createObjectURL(data as Blob)
  window.open(url, '_blank', 'noopener,noreferrer')
  setTimeout(() => URL.revokeObjectURL(url), 60_000)
}

export const useSalesReturns = () =>
  useQuery({ queryKey: ['pharmacy', 'sales-returns'], queryFn: () => get<SalesReturnListItem[]>(p.salesReturns) })
export const useCreateSalesReturn = () =>
  useInvalidating((payload: CreateSalesReturnPayload) => api.post(p.salesReturns, payload), [
    ['pharmacy', 'sales-returns'],
    ['pharmacy', 'sales'],
    ['pharmacy', 'stock'],
  ])

// ── Issues ───────────────────────────────────────────────────────────────
export const useIssues = (issueType?: IssueType) =>
  useQuery({ queryKey: ['pharmacy', 'issues', issueType], queryFn: () => get<IssueListItem[]>(p.issues, { issueType }) })
export const useIssue = (id: number | null) =>
  useQuery({ queryKey: ['pharmacy', 'issues', 'detail', id], queryFn: () => get<IssueDetail>(`${p.issues}/${id}`), enabled: id !== null })
export const useCreateIssue = () =>
  useInvalidating((payload: CreateIssuePayload) => api.post(p.issues, payload), [['pharmacy', 'issues'], ['pharmacy', 'stock']])

export const useIssueReturns = () =>
  useQuery({ queryKey: ['pharmacy', 'issue-returns'], queryFn: () => get<IssueReturnListItem[]>(p.issueReturns) })
export const useCreateIssueReturn = () =>
  useInvalidating((payload: CreateIssueReturnPayload) => api.post(p.issueReturns, payload), [
    ['pharmacy', 'issue-returns'],
    ['pharmacy', 'stock'],
  ])

// ── Stock explorer / alerts / reports ────────────────────────────────────
export const useBatches = (drugId?: number, itemId?: number, storeId?: number, onlyInStock = true) =>
  useQuery({
    queryKey: ['pharmacy', 'stock', 'batches', drugId, itemId, storeId, onlyInStock],
    queryFn: () => get<Batch[]>(`${p.stock}/batches`, { drugId, itemId, storeId, onlyInStock }),
  })
export const useBatchLedger = (batchId: number | null) =>
  useQuery({
    queryKey: ['pharmacy', 'stock', 'ledger', batchId],
    queryFn: () => get<StockLedgerEntry[]>(`${p.stock}/batches/${batchId}/ledger`),
    enabled: batchId !== null,
  })
export const useNearExpiry = (withinDays: number) =>
  useQuery({
    queryKey: ['pharmacy', 'stock', 'near-expiry', withinDays],
    queryFn: () => get<NearExpiryBatch[]>(`${p.stock}/alerts/near-expiry`, { withinDays }),
  })
export const useLowStock = () =>
  useQuery({ queryKey: ['pharmacy', 'stock', 'low-stock'], queryFn: () => get<LowStockProduct[]>(`${p.stock}/alerts/low-stock`) })
export const useConsumptionReport = (fromDate: string, toDate: string, enabled: boolean) =>
  useQuery({
    queryKey: ['pharmacy', 'stock', 'consumption-report', fromDate, toDate],
    queryFn: () => get<ProductConsumptionRow[]>(`${p.stock}/consumption-report`, { fromDate, toDate }),
    enabled,
  })

// ── Stock adjustments ────────────────────────────────────────────────────
export const usePendingAdjustments = () =>
  useQuery({ queryKey: ['pharmacy', 'adjustments'], queryFn: () => get<StockAdjustmentListItem[]>(p.adjustments) })
export const useAdjustment = (id: number | null) =>
  useQuery({
    queryKey: ['pharmacy', 'adjustments', 'detail', id],
    queryFn: () => get<StockAdjustmentDetail>(`${p.adjustments}/${id}`),
    enabled: id !== null,
  })
export const useCreateAdjustment = () =>
  useInvalidating((payload: CreateStockAdjustmentPayload) => api.post(p.adjustments, payload), [['pharmacy', 'adjustments']])
export const useDecideAdjustment = () =>
  useInvalidating(
    ({ id, approve, rejectionReason }: { id: number; approve: boolean; rejectionReason: string | null }) =>
      api.post(`${p.adjustments}/${id}/decide`, { approve, rejectionReason }),
    [['pharmacy', 'adjustments'], ['pharmacy', 'stock']],
  )

// ── Stock transfers ──────────────────────────────────────────────────────
export const useTransfers = () =>
  useQuery({ queryKey: ['pharmacy', 'transfers'], queryFn: () => get<StockTransferListItem[]>(p.transfers) })
export const useTransfer = (id: number | null) =>
  useQuery({
    queryKey: ['pharmacy', 'transfers', 'detail', id],
    queryFn: () => get<StockTransferDetail>(`${p.transfers}/${id}`),
    enabled: id !== null,
  })
export const useCreateTransfer = () =>
  useInvalidating((payload: CreateStockTransferPayload) => api.post(p.transfers, payload), [['pharmacy', 'transfers']])
export const useDispatchTransfer = () =>
  useInvalidating((id: number) => api.post(`${p.transfers}/${id}/dispatch`), [['pharmacy', 'transfers'], ['pharmacy', 'stock']])
export const useReceiveTransfer = () =>
  useInvalidating((id: number) => api.post(`${p.transfers}/${id}/receive`), [['pharmacy', 'transfers'], ['pharmacy', 'stock']])

// ── Stock audits ─────────────────────────────────────────────────────────
export const useAudits = () => useQuery({ queryKey: ['pharmacy', 'audits'], queryFn: () => get<StockAuditListItem[]>(p.audits) })
export const useAudit = (id: number | null) =>
  useQuery({
    queryKey: ['pharmacy', 'audits', 'detail', id],
    queryFn: () => get<StockAuditDetail>(`${p.audits}/${id}`),
    enabled: id !== null,
  })
export const useCreateAudit = () =>
  useInvalidating(async (storeId: number) => {
    const { data } = await api.post<ApiResponse<number>>(p.audits, { storeId })
    return data.data!
  }, [['pharmacy', 'audits']])
export const useRecordAuditCount = (auditId: number) =>
  useInvalidating(
    (payload: { batchId: number; countedQuantity: number }) => api.post(`${p.audits}/${auditId}/counts`, payload),
    [['pharmacy', 'audits', 'detail', String(auditId)]],
  )
export const useCompleteAudit = () =>
  useInvalidating((id: number) => api.post(`${p.audits}/${id}/complete`), [['pharmacy', 'audits'], ['pharmacy', 'stock']])

// ── Indents ──────────────────────────────────────────────────────────────
export const useIndents = (status?: IndentStatus) =>
  useQuery({ queryKey: ['pharmacy', 'indents', status], queryFn: () => get<IndentListItem[]>(p.indents, { status }) })
export const useIndent = (id: number | null) =>
  useQuery({
    queryKey: ['pharmacy', 'indents', 'detail', id],
    queryFn: () => get<IndentDetail>(`${p.indents}/${id}`),
    enabled: id !== null,
  })
export const useCreateIndent = () =>
  useInvalidating((payload: CreateIndentPayload) => api.post(p.indents, payload), [['pharmacy', 'indents']])
export const useDecideIndent = () =>
  useInvalidating(
    ({ id, ...payload }: { id: number } & DecideIndentPayload) => api.post(`${p.indents}/${id}/decide`, payload),
    [['pharmacy', 'indents']],
  )
