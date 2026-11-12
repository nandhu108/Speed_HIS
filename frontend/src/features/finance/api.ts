import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse } from '../../api/types/api'
import type {
  AccountDto,
  AccountLedgerDto,
  AgingRowDto,
  CashBankCollectionDto,
  DayCloseConsoleRowDto,
  GstReportDto,
  OutstandingReportDto,
  PayableDto,
  PayableStatus,
  PendingCreditBillDto,
  PendingGrnDto,
  PostVoucherPayload,
  ReceivableDto,
  ReceivableStatus,
  RevenueReportDto,
  SaveAccountPayload,
  TrialBalanceDto,
  VoucherDto,
  VoucherStatus,
  VoucherType,
} from './types'

const p = endpoints.finance

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

// ── Accounts ─────────────────────────────────────────────────────────────────
export const useAccounts = () => useQuery({ queryKey: ['finance', 'accounts'], queryFn: () => get<AccountDto[]>(p.accounts) })

export const useSaveAccount = () =>
  useInvalidating(
    ({ id, ...payload }: SaveAccountPayload & { id?: number }) =>
      id ? api.put(`${p.accounts}/${id}`, payload) : api.post(p.accounts, payload),
    [['finance', 'accounts']],
  )

export const useDeleteAccount = () => useInvalidating((id: number) => api.delete(`${p.accounts}/${id}`), [['finance', 'accounts']])

// ── Vouchers / Ledger ────────────────────────────────────────────────────────
export const useVouchers = (filter: { voucherType?: VoucherType; status?: VoucherStatus; fromDate?: string; toDate?: string }) =>
  useQuery({ queryKey: ['finance', 'vouchers', filter], queryFn: () => get<VoucherDto[]>(p.vouchers, filter) })

export const usePostVoucher = () =>
  useInvalidating(async (payload: PostVoucherPayload) => {
    const { data } = await api.post<ApiResponse<VoucherDto>>(p.vouchers, payload)
    return data.data!
  }, [['finance', 'vouchers']])

export const useCancelVoucher = () =>
  useInvalidating(({ id, reason }: { id: number; reason: string }) => api.post(`${p.vouchers}/${id}/cancel`, { reason }), [
    ['finance', 'vouchers'],
    ['finance', 'ledger'],
  ])

export const useAccountLedger = (accountId: number | null, range: { fromDate?: string; toDate?: string }) =>
  useQuery({
    queryKey: ['finance', 'ledger', accountId, range],
    queryFn: () => get<AccountLedgerDto>(`${p.ledger}/${accountId}`, range),
    enabled: accountId !== null,
  })

// ── Payables ─────────────────────────────────────────────────────────────────
export const usePayables = (status?: PayableStatus) =>
  useQuery({ queryKey: ['finance', 'payables', status], queryFn: () => get<PayableDto[]>(p.payables, { status }) })

export const usePendingGrns = () =>
  useQuery({ queryKey: ['finance', 'payables', 'pending-grns'], queryFn: () => get<PendingGrnDto[]>(`${p.payables}/pending-grns`) })

export const usePayableAging = (asOf: string) =>
  useQuery({ queryKey: ['finance', 'payables', 'aging', asOf], queryFn: () => get<AgingRowDto[]>(`${p.payables}/aging`, { asOf }) })

const payableKeys = [['finance', 'payables'], ['finance', 'ledger']]

export const useCreatePayable = () =>
  useInvalidating((request: { grnId: number; dueDate: string | null }) => api.post(p.payables, request), payableKeys)

export const usePayPayable = () =>
  useInvalidating(
    ({ id, ...payload }: { id: number; amount: number; cashAccountId: number; date: string; referenceNumber: string | null }) =>
      api.post(`${p.payables}/${id}/pay`, payload),
    payableKeys,
  )

// ── Receivables ──────────────────────────────────────────────────────────────
export const useReceivables = (status?: ReceivableStatus) =>
  useQuery({ queryKey: ['finance', 'receivables', status], queryFn: () => get<ReceivableDto[]>(p.receivables, { status }) })

export const usePendingCreditBills = () =>
  useQuery({ queryKey: ['finance', 'receivables', 'pending-bills'], queryFn: () => get<PendingCreditBillDto[]>(`${p.receivables}/pending-bills`) })

export const useReceivableAging = (asOf: string) =>
  useQuery({ queryKey: ['finance', 'receivables', 'aging', asOf], queryFn: () => get<AgingRowDto[]>(`${p.receivables}/aging`, { asOf }) })

const receivableKeys = [['finance', 'receivables'], ['finance', 'ledger']]

export const useCreateReceivable = () => useInvalidating((request: { billId: number }) => api.post(p.receivables, request), receivableKeys)

export const useCollectReceivable = () =>
  useInvalidating(
    ({ id, ...payload }: { id: number; amount: number; cashAccountId: number; date: string; referenceNumber: string | null }) =>
      api.post(`${p.receivables}/${id}/collect`, payload),
    receivableKeys,
  )

// ── Reports ───────────────────────────────────────────────────────────────────
export const useCashBankCollection = (range: { fromDate?: string; toDate?: string }) =>
  useQuery({ queryKey: ['finance', 'reports', 'cash-bank', range], queryFn: () => get<CashBankCollectionDto>(`${p.reports}/cash-bank-collection`, range) })

export const useRevenueReport = (range: { fromDate?: string; toDate?: string }) =>
  useQuery({ queryKey: ['finance', 'reports', 'revenue', range], queryFn: () => get<RevenueReportDto>(`${p.reports}/revenue`, range) })

export const useGstReport = (range: { fromDate?: string; toDate?: string }) =>
  useQuery({ queryKey: ['finance', 'reports', 'gst', range], queryFn: () => get<GstReportDto>(`${p.reports}/gst`, range) })

export const useOutstandingReport = () =>
  useQuery({ queryKey: ['finance', 'reports', 'outstanding'], queryFn: () => get<OutstandingReportDto>(`${p.reports}/outstanding`) })

export const useTrialBalance = (asOfDate: string) =>
  useQuery({ queryKey: ['finance', 'reports', 'trial-balance', asOfDate], queryFn: () => get<TrialBalanceDto>(`${p.reports}/trial-balance`, { asOfDate }) })

export const useDayCloseConsole = (date: string) =>
  useQuery({ queryKey: ['finance', 'reports', 'day-close-console', date], queryFn: () => get<DayCloseConsoleRowDto[]>(`${p.reports}/day-close-console`, { date }) })
