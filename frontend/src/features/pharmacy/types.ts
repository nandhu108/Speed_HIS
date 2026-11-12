export type PurchaseRequestStatus = 1 | 2 | 3 | 4 | 5 // Draft, Submitted, Approved, Rejected, Converted
export type PurchaseOrderStatus = 1 | 2 | 3 | 4 | 5 // Draft, Approved, PartiallyReceived, Received, Cancelled
export type DocumentStatus = 1 | 2 | 3 // Draft, Posted, Cancelled
export type PharmacySaleStatus = 1 | 2 // Completed, Cancelled
export type IssueType = 1 | 2 | 3 // Department, Emergency, Ip
export type StockAdjustmentStatus = 1 | 2 | 3 // Pending, Approved, Rejected
export type StockTransferStatus = 1 | 2 | 3 | 4 // Pending, Dispatched, Received, Cancelled
export type StockAuditStatus = 1 | 2 // InProgress, Completed
export type StockMovementType = 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10
export type StoreType = 1 | 2 | 3 | 4 | 5 | 6 // Pharmacy, General, Linen, Stationery, Consumables, BiomedicalSpares
export type IndentStatus = 1 | 2 | 3 | 4 // Requested, Approved, Rejected, Issued

export const PURCHASE_REQUEST_STATUS_LABELS: Record<PurchaseRequestStatus, string> = {
  1: 'Draft', 2: 'Submitted', 3: 'Approved', 4: 'Rejected', 5: 'Converted',
}
export const PURCHASE_ORDER_STATUS_LABELS: Record<PurchaseOrderStatus, string> = {
  1: 'Draft', 2: 'Approved', 3: 'Partially Received', 4: 'Received', 5: 'Cancelled',
}
export const DOCUMENT_STATUS_LABELS: Record<DocumentStatus, string> = { 1: 'Draft', 2: 'Posted', 3: 'Cancelled' }
export const SALE_STATUS_LABELS: Record<PharmacySaleStatus, string> = { 1: 'Completed', 2: 'Cancelled' }
export const ISSUE_TYPE_LABELS: Record<IssueType, string> = { 1: 'Department', 2: 'Emergency', 3: 'IP' }
export const ADJUSTMENT_STATUS_LABELS: Record<StockAdjustmentStatus, string> = { 1: 'Pending', 2: 'Approved', 3: 'Rejected' }
export const TRANSFER_STATUS_LABELS: Record<StockTransferStatus, string> = {
  1: 'Pending', 2: 'Dispatched', 3: 'Received', 4: 'Cancelled',
}
export const AUDIT_STATUS_LABELS: Record<StockAuditStatus, string> = { 1: 'In Progress', 2: 'Completed' }
export const MOVEMENT_TYPE_LABELS: Record<StockMovementType, string> = {
  1: 'Purchase In', 2: 'Purchase Return Out', 3: 'Sale Out', 4: 'Sale Return In',
  5: 'Issue Out', 6: 'Issue Return In', 7: 'Adjustment In', 8: 'Adjustment Out',
  9: 'Transfer Out', 10: 'Transfer In',
}
export const STORE_TYPE_LABELS: Record<StoreType, string> = {
  1: 'Pharmacy', 2: 'General', 3: 'Linen', 4: 'Stationery', 5: 'Consumables', 6: 'Biomedical Spares',
}
export const INDENT_STATUS_LABELS: Record<IndentStatus, string> = {
  1: 'Requested', 2: 'Approved', 3: 'Rejected', 4: 'Issued',
}

/// Every purchase/stock line carries exactly one of drugId/itemId (Phase 13's "genuinely widen
/// the pharmacy engine" — see backend Batch entity doc comment) plus a resolved display name.
export interface ProductRef {
  drugId: number | null
  itemId: number | null
  productName: string
}

// ── Masters ──────────────────────────────────────────────────────────────
export interface Store {
  id: number
  code: string
  name: string
  storeType: StoreType
  isDefault: boolean
  isActive: boolean
}
export interface SaveStorePayload {
  id?: number
  code: string
  name: string
  storeType: StoreType
  isDefault: boolean
  isActive: boolean
}

export interface Rack {
  id: number
  storeId: number
  storeName: string
  code: string
  name: string
}
export interface SaveRackPayload {
  storeId: number
  code: string
  name: string
}

// ── Purchase requests ────────────────────────────────────────────────────
export interface PurchaseRequestItem extends ProductRef {
  id: number
  requestedQuantity: number
}
export interface CreatePurchaseRequestItemPayload {
  drugId: number | null
  itemId: number | null
  requestedQuantity: number
}
export interface CreatePurchaseRequestPayload {
  notes: string | null
  items: CreatePurchaseRequestItemPayload[]
}
export interface PurchaseRequestListItem {
  id: number
  requestNumber: string
  requestDate: string
  status: PurchaseRequestStatus
  itemCount: number
}
export interface PurchaseRequestDetail {
  id: number
  requestNumber: string
  requestDate: string
  status: PurchaseRequestStatus
  notes: string | null
  items: PurchaseRequestItem[]
}

// ── Purchase orders ──────────────────────────────────────────────────────
export interface PurchaseOrderItem extends ProductRef {
  id: number
  orderedQuantity: number
  receivedQuantity: number
  rate: number
  taxPercent: number
}
export interface CreatePurchaseOrderItemPayload {
  drugId: number | null
  itemId: number | null
  orderedQuantity: number
  rate: number
  taxPercent: number
}
export interface CreatePurchaseOrderPayload {
  vendorId: number
  purchaseRequestId: number | null
  notes: string | null
  items: CreatePurchaseOrderItemPayload[]
}
export interface PurchaseOrderListItem {
  id: number
  poNumber: string
  orderDate: string
  vendorId: number
  vendorName: string
  status: PurchaseOrderStatus
  totalAmount: number
}
export interface PurchaseOrderDetail {
  id: number
  poNumber: string
  orderDate: string
  vendorId: number
  vendorName: string
  purchaseRequestId: number | null
  status: PurchaseOrderStatus
  notes: string | null
  items: PurchaseOrderItem[]
}

// ── GRN ──────────────────────────────────────────────────────────────────
export interface GrnItem extends ProductRef {
  id: number
  batchNumber: string
  expiryDate: string | null
  mrp: number | null
  purchaseRate: number
  receivedQuantity: number
  freeQuantity: number
  taxPercent: number
  amount: number
}
export interface CreateGrnItemPayload {
  drugId: number | null
  itemId: number | null
  batchNumber: string
  expiryDate: string | null
  mrp: number | null
  purchaseRate: number
  receivedQuantity: number
  freeQuantity: number
  taxPercent: number
}
export interface CreateGrnPayload {
  purchaseOrderId: number | null
  vendorId: number
  storeId: number
  supplierInvoiceNumber: string | null
  supplierInvoiceDate: string | null
  items: CreateGrnItemPayload[]
}
export interface GrnListItem {
  id: number
  grnNumber: string
  receivedDate: string
  vendorId: number
  vendorName: string
  status: DocumentStatus
  totalAmount: number
}
export interface GrnDetail {
  id: number
  grnNumber: string
  receivedDate: string
  purchaseOrderId: number | null
  vendorId: number
  vendorName: string
  storeId: number
  storeName: string
  supplierInvoiceNumber: string | null
  supplierInvoiceDate: string | null
  status: DocumentStatus
  totalAmount: number
  items: GrnItem[]
}

// ── Purchase returns ─────────────────────────────────────────────────────
export interface PurchaseReturnItem {
  id: number
  batchId: number
  productName: string
  batchNumber: string
  quantity: number
  rate: number
}
export interface CreatePurchaseReturnItemPayload {
  batchId: number
  quantity: number
  rate: number
}
export interface CreatePurchaseReturnPayload {
  grnId: number | null
  vendorId: number
  reason: string
  items: CreatePurchaseReturnItemPayload[]
}
export interface PurchaseReturnListItem {
  id: number
  returnNumber: string
  returnDate: string
  vendorId: number
  vendorName: string
  totalAmount: number
}
export interface PurchaseReturnDetail {
  id: number
  returnNumber: string
  returnDate: string
  grnId: number | null
  vendorId: number
  vendorName: string
  reason: string
  totalAmount: number
  items: PurchaseReturnItem[]
}

// ── Sales (drug-only — OP pharmacy POS doesn't dispense general items) ────
export interface BatchAvailability {
  batchId: number
  batchNumber: string
  expiryDate: string
  mrp: number
  available: number
}
export interface SaleLinePayload {
  drugId: number
  quantity: number
  discountPercent: number
}
export interface CreateSalePayload {
  storeId: number
  patientId: number | null
  prescriptionId: number | null
  paymentModeId: number | null
  items: SaleLinePayload[]
}
export interface SaleItem {
  id: number
  batchId: number
  drugName: string
  batchNumber: string
  expiryDate: string
  quantity: number
  rate: number
  discountPercent: number
  discountAmount: number
  taxPercent: number
  taxAmount: number
  totalAmount: number
}
export interface PharmacySaleListItem {
  id: number
  saleNumber: string
  saleDate: string
  patientName: string | null
  status: PharmacySaleStatus
  totalAmount: number
}
export interface PharmacySaleDetail {
  id: number
  saleNumber: string
  saleDate: string
  storeId: number
  storeName: string
  patientId: number | null
  patientName: string | null
  status: PharmacySaleStatus
  subTotal: number
  discountAmount: number
  taxAmount: number
  totalAmount: number
  paymentModeId: number | null
  paymentModeName: string | null
  cancelReason: string | null
  items: SaleItem[]
}
export interface CreateSalesReturnItemPayload {
  pharmacySaleItemId: number
  quantity: number
}
export interface CreateSalesReturnPayload {
  pharmacySaleId: number
  reason: string
  items: CreateSalesReturnItemPayload[]
}
export interface SalesReturnListItem {
  id: number
  returnNumber: string
  returnDate: string
  saleNumber: string
  totalAmount: number
}

// ── Issues ───────────────────────────────────────────────────────────────
export interface CreateIssueItemPayload {
  batchId: number
  quantity: number
}
export interface CreateIssuePayload {
  storeId: number
  issueType: IssueType
  departmentId: number | null
  patientId: number | null
  admissionId: number | null
  indentId: number | null
  notes: string | null
  items: CreateIssueItemPayload[]
}
export interface IssueItem {
  id: number
  batchId: number
  productName: string
  batchNumber: string
  quantity: number
}
export interface IssueListItem {
  id: number
  issueNumber: string
  issueDate: string
  issueType: IssueType
  departmentName: string | null
  admissionId: number | null
  admissionNumber: string | null
  indentId: number | null
  itemCount: number
}
export interface IssueDetail {
  id: number
  issueNumber: string
  issueDate: string
  issueType: IssueType
  departmentId: number | null
  departmentName: string | null
  patientId: number | null
  patientName: string | null
  admissionId: number | null
  admissionNumber: string | null
  indentId: number | null
  notes: string | null
  items: IssueItem[]
}
export interface CreateIssueReturnItemPayload {
  issueItemId: number
  quantity: number
}
export interface CreateIssueReturnPayload {
  issueId: number
  reason: string
  items: CreateIssueReturnItemPayload[]
}
export interface IssueReturnListItem {
  id: number
  returnNumber: string
  returnDate: string
  issueNumber: string
}

// ── Indents ──────────────────────────────────────────────────────────────
export interface IndentItem extends ProductRef {
  id: number
  requestedQuantity: number
  approvedQuantity: number | null
}
export interface CreateIndentItemPayload {
  drugId: number | null
  itemId: number | null
  requestedQuantity: number
}
export interface CreateIndentPayload {
  storeId: number
  departmentId: number
  notes: string | null
  items: CreateIndentItemPayload[]
}
export interface DecideIndentItemPayload {
  indentItemId: number
  approvedQuantity: number
}
export interface DecideIndentPayload {
  approve: boolean
  rejectionReason: string | null
  items: DecideIndentItemPayload[] | null
}
export interface IndentListItem {
  id: number
  indentNumber: string
  indentDate: string
  storeName: string
  departmentName: string
  status: IndentStatus
  itemCount: number
}
export interface IndentDetail {
  id: number
  indentNumber: string
  indentDate: string
  storeId: number
  storeName: string
  departmentId: number
  departmentName: string
  status: IndentStatus
  notes: string | null
  rejectionReason: string | null
  items: IndentItem[]
}

// ── Stock ────────────────────────────────────────────────────────────────
export interface Batch extends ProductRef {
  id: number
  storeId: number
  storeName: string
  batchNumber: string
  expiryDate: string | null
  mrp: number | null
  purchaseRate: number
  quantityOnHand: number
}
export interface StockLedgerEntry {
  id: number
  movementType: StockMovementType
  quantity: number
  runningBalance: number
  referenceType: string
  referenceId: number
  movementAt: string
  notes: string | null
}

export interface CreateStockAdjustmentItemPayload {
  batchId: number
  quantityChange: number
}
export interface CreateStockAdjustmentPayload {
  storeId: number
  reason: string
  items: CreateStockAdjustmentItemPayload[]
}
export interface StockAdjustmentItem {
  id: number
  batchId: number
  productName: string
  batchNumber: string
  quantityChange: number
}
export interface StockAdjustmentListItem {
  id: number
  adjustmentNumber: string
  adjustmentDate: string
  storeName: string
  status: StockAdjustmentStatus
}
export interface StockAdjustmentDetail {
  id: number
  adjustmentNumber: string
  adjustmentDate: string
  storeId: number
  storeName: string
  reason: string
  status: StockAdjustmentStatus
  rejectionReason: string | null
  items: StockAdjustmentItem[]
}

export interface CreateStockTransferItemPayload {
  batchId: number
  quantity: number
}
export interface CreateStockTransferPayload {
  fromStoreId: number
  toBranchId: number
  toStoreId: number
  items: CreateStockTransferItemPayload[]
}
export interface StockTransferItem {
  id: number
  batchId: number
  productName: string
  batchNumber: string
  quantity: number
}
export interface StockTransferListItem {
  id: number
  transferNumber: string
  transferDate: string
  fromStoreName: string
  toStoreName: string
  status: StockTransferStatus
}
export interface StockTransferDetail {
  id: number
  transferNumber: string
  transferDate: string
  fromStoreId: number
  fromStoreName: string
  toBranchId: number
  toBranchName: string
  toStoreId: number
  toStoreName: string
  status: StockTransferStatus
  items: StockTransferItem[]
}

export interface StockAuditItem {
  id: number
  batchId: number
  productName: string
  batchNumber: string
  systemQuantity: number
  countedQuantity: number
  variance: number
}
export interface StockAuditListItem {
  id: number
  auditNumber: string
  auditDate: string
  storeName: string
  status: StockAuditStatus
}
export interface StockAuditDetail {
  id: number
  auditNumber: string
  auditDate: string
  storeId: number
  storeName: string
  status: StockAuditStatus
  items: StockAuditItem[]
}

export interface NearExpiryBatch {
  batchId: number
  productName: string
  storeName: string
  batchNumber: string
  expiryDate: string
  quantityOnHand: number
  daysToExpiry: number
}
export interface LowStockProduct {
  drugId: number | null
  itemId: number | null
  productName: string
  reorderLevel: number
  totalOnHand: number
}
export interface ProductConsumptionRow {
  drugId: number | null
  itemId: number | null
  productName: string
  departmentName: string | null
  quantitySold: number
  quantityIssued: number
}
