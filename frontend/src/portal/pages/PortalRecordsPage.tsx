import { useState } from 'react'
import { errorMessage } from '../../api/axios'
import { Button } from '../../components/ui/Button'
import { PageHeader } from '../../components/ui/PageHeader'
import { Spinner } from '../../components/ui/Spinner'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { useBills, useDischargeSummaries, useLabOrders, usePrescriptions, useRadOrders, useRequestOnlinePayment, downloadPortalPdf } from '../features/records/api'

type Tab = 'lab' | 'rad' | 'prescriptions' | 'bills' | 'discharge'

export function PortalRecordsPage() {
  const [tab, setTab] = useState<Tab>('lab')
  const tabs: { key: Tab; label: string }[] = [
    { key: 'lab', label: 'Lab Reports' },
    { key: 'rad', label: 'Radiology Reports' },
    { key: 'prescriptions', label: 'Prescriptions' },
    { key: 'bills', label: 'Bills & Receipts' },
    { key: 'discharge', label: 'Discharge Summaries' },
  ]

  return (
    <>
      <PageHeader title="My Records" subtitle="Approved reports, prescriptions, bills and discharge summaries" />
      <div className="mb-4 flex flex-wrap gap-1 border-b border-slate-200">
        {tabs.map((t) => (
          <button
            key={t.key}
            onClick={() => setTab(t.key)}
            className={`border-b-2 px-3 py-2 text-sm font-medium ${tab === t.key ? 'border-primary-600 text-primary-700' : 'border-transparent text-slate-500 hover:text-slate-700'}`}
          >
            {t.label}
          </button>
        ))}
      </div>
      {tab === 'lab' && <LabTab />}
      {tab === 'rad' && <RadTab />}
      {tab === 'prescriptions' && <PrescriptionsTab />}
      {tab === 'bills' && <BillsTab />}
      {tab === 'discharge' && <DischargeTab />}
    </>
  )
}

function LabTab() {
  const { data: orders = [], isLoading } = useLabOrders()
  return (
    <div className="rounded-lg border border-slate-200 bg-white">
      {isLoading && <Spinner />}
      <table className="w-full text-sm">
        <tbody>
          {orders.map((o) => (
            <tr key={o.id} className="border-b border-slate-100 last:border-0">
              <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{o.orderNumber}</td>
              <td className="px-4 py-2.5 text-slate-700">{o.orderDate}</td>
              <td className="px-4 py-2.5">
                {o.hasApprovedReport ? (
                  <Button variant="secondary" onClick={() => downloadPortalPdf(`/portal/lab-reports/${o.id}/pdf`, `lab-report-${o.orderNumber}.pdf`)}>Download</Button>
                ) : <StatusBadge tone="warning">Pending</StatusBadge>}
              </td>
            </tr>
          ))}
          {!isLoading && orders.length === 0 && <tr><td className="px-4 py-10 text-center text-slate-400">No lab orders found.</td></tr>}
        </tbody>
      </table>
    </div>
  )
}

function RadTab() {
  const { data: items = [], isLoading } = useRadOrders()
  return (
    <div className="rounded-lg border border-slate-200 bg-white">
      {isLoading && <Spinner />}
      <table className="w-full text-sm">
        <tbody>
          {items.map((i) => (
            <tr key={i.id} className="border-b border-slate-100 last:border-0">
              <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{i.accessionNumber}</td>
              <td className="px-4 py-2.5 text-slate-700">{i.testName}</td>
              <td className="px-4 py-2.5 text-slate-700">{i.orderDate}</td>
              <td className="px-4 py-2.5">
                {i.isApproved ? (
                  <Button variant="secondary" onClick={() => downloadPortalPdf(`/portal/rad-reports/${i.id}/pdf`, `rad-report-${i.accessionNumber}.pdf`)}>Download</Button>
                ) : <StatusBadge tone="warning">Pending</StatusBadge>}
              </td>
            </tr>
          ))}
          {!isLoading && items.length === 0 && <tr><td className="px-4 py-10 text-center text-slate-400">No radiology orders found.</td></tr>}
        </tbody>
      </table>
    </div>
  )
}

function PrescriptionsTab() {
  const { data: prescriptions = [], isLoading } = usePrescriptions()
  return (
    <div className="space-y-3">
      {isLoading && <Spinner />}
      {prescriptions.map((p) => (
        <div key={p.id} className="rounded-lg border border-slate-200 bg-white p-4">
          <div className="mb-2 flex justify-between text-sm text-slate-600">
            <span>{p.visitDate}</span>
            <span>Dr. {p.doctorName}</span>
          </div>
          <ul className="list-inside list-disc text-sm text-slate-700">
            {p.items.map((item, i) => <li key={i}>{item}</li>)}
          </ul>
        </div>
      ))}
      {!isLoading && prescriptions.length === 0 && <p className="py-10 text-center text-slate-400">No prescriptions found.</p>}
    </div>
  )
}

function BillsTab() {
  const { data: bills = [], isLoading } = useBills()
  const requestPayment = useRequestOnlinePayment()
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const payOnline = async (billId: number) => {
    setError(null)
    setMessage(null)
    try {
      await requestPayment.mutateAsync(billId)
      setMessage('Online payment requested — our billing team will contact you to complete it.')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div>
      {message && <p className="mb-3 rounded-md bg-green-50 px-3 py-2 text-sm text-green-700">{message}</p>}
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <tbody>
            {bills.map((b) => (
              <tr key={b.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{b.billNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{b.billDate}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{b.totalAmount.toFixed(2)}</td>
                <td className="px-4 py-2.5 text-slate-700">{b.status}</td>
                <td className="px-4 py-2.5 space-x-2">
                  <Button variant="secondary" onClick={() => downloadPortalPdf(`/portal/bills/${b.id}/receipt`, `receipt-${b.billNumber}.pdf`)}>Receipt</Button>
                  {b.balanceAmount > 0 && (
                    <Button variant="secondary" disabled={requestPayment.isPending} onClick={() => payOnline(b.id)}>
                      Pay Online
                    </Button>
                  )}
                </td>
              </tr>
            ))}
            {!isLoading && bills.length === 0 && <tr><td className="px-4 py-10 text-center text-slate-400">No bills found.</td></tr>}
          </tbody>
        </table>
      </div>
    </div>
  )
}

function DischargeTab() {
  const { data: summaries = [], isLoading } = useDischargeSummaries()
  return (
    <div className="rounded-lg border border-slate-200 bg-white">
      {isLoading && <Spinner />}
      <table className="w-full text-sm">
        <tbody>
          {summaries.map((s) => (
            <tr key={s.admissionId} className="border-b border-slate-100 last:border-0">
              <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{s.admissionNumber}</td>
              <td className="px-4 py-2.5 text-slate-700">{new Date(s.dischargedAt).toLocaleDateString()}</td>
              <td className="px-4 py-2.5 text-slate-700">{s.finalDiagnosis}</td>
              <td className="px-4 py-2.5">
                <Button variant="secondary" onClick={() => downloadPortalPdf(`/portal/discharge-summaries/${s.admissionId}/pdf`, `discharge-summary-${s.admissionNumber}.pdf`)}>Download</Button>
              </td>
            </tr>
          ))}
          {!isLoading && summaries.length === 0 && <tr><td className="px-4 py-10 text-center text-slate-400">No discharge summaries found.</td></tr>}
        </tbody>
      </table>
    </div>
  )
}
