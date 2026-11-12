import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button } from '../../../components/ui/Button'
import { DateRangeFilter, defaultDateRange } from '../../../components/ui/DateRangeFilter'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useEmergencyQueue } from '../../er/api'
import type { EmergencyVisitListItem } from '../../er/types'
import { useOpVisits } from '../../opd/api'
import { OP_VISIT_STATUSES } from '../../opd/types'
import type { OpVisitListItem } from '../../opd/types'
import { AdmitPatientModal, type AdmitFromPrefill } from '../components/AdmitPatientModal'

/**
 * A single launchpad for ER→IP and OP→IP conversion — a convenience list on top of the
 * "Admit to IP" buttons already on the OP consultation workspace and an ER visit's Assessment
 * tab, for staff who'd rather browse admit-ready visits than open each one individually.
 * "Convert" opens the Admit form in place (no navigation) — this page stays put until the
 * admission is actually saved.
 */
export function ConvertToIpPage() {
  const navigate = useNavigate()
  const [admitting, setAdmitting] = useState<AdmitFromPrefill | null>(null)

  return (
    <>
      <PageHeader title="Convert to IP" subtitle="OP and ER visits ready to be admitted as inpatients" />
      <div className="space-y-6">
        <OpVisitsToConvert onConvert={setAdmitting} />
        <ErVisitsToConvert onConvert={setAdmitting} />
      </div>
      {admitting && (
        <AdmitPatientModal
          prefill={admitting}
          onClose={() => setAdmitting(null)}
          onAdmitted={(id) => navigate(`/ipd/admissions/${id}`)}
        />
      )}
    </>
  )
}

function OpVisitsToConvert({ onConvert }: { onConvert: (prefill: AdmitFromPrefill) => void }) {
  const { data, isLoading } = useOpVisits({ page: 1, pageSize: 20, status: OP_VISIT_STATUSES.Completed })
  const pending = (data?.items ?? []).filter((v) => !v.alreadyAdmitted)

  const convert = (v: OpVisitListItem) =>
    onConvert({ patientId: v.patientId, patientName: v.patientName, patientUhid: v.patientUhid, opVisitId: v.id })

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <h3 className="mb-1 text-sm font-semibold text-slate-800">From OP Visits</h3>
      <p className="mb-3 text-xs text-slate-500">Recently completed consultations not yet admitted.</p>
      {isLoading && <Spinner />}
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-3 py-2 font-medium text-slate-600">Visit No</th>
              <th className="px-3 py-2 font-medium text-slate-600">Patient</th>
              <th className="px-3 py-2 font-medium text-slate-600">Doctor</th>
              <th className="px-3 py-2 font-medium text-slate-600">Date</th>
              <th className="px-3 py-2" />
            </tr>
          </thead>
          <tbody>
            {pending.map((v) => (
              <tr key={v.id} className="border-b border-slate-100 last:border-0">
                <td className="px-3 py-2 font-mono text-xs text-slate-500">{v.visitNumber}</td>
                <td className="px-3 py-2 text-slate-700">
                  {v.patientName} <span className="font-mono text-xs text-slate-400">({v.patientUhid})</span>
                </td>
                <td className="px-3 py-2 text-slate-700">{v.doctorName}</td>
                <td className="px-3 py-2 text-slate-700">{v.visitDate}</td>
                <td className="px-3 py-2 text-right">
                  <Button onClick={() => convert(v)}>Convert</Button>
                </td>
              </tr>
            ))}
            {!isLoading && pending.length === 0 && (
              <tr><td colSpan={5} className="px-3 py-8 text-center text-slate-400">Nothing waiting to be converted.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}

function ErVisitsToConvert({ onConvert }: { onConvert: (prefill: AdmitFromPrefill) => void }) {
  const [{ fromDate, toDate }, setRange] = useState(defaultDateRange(7))
  const { data = [], isLoading } = useEmergencyQueue({ status: 4, fromDate, toDate })
  const pending = data.filter((v) => v.disposition === 2 && !v.alreadyAdmitted)

  const convert = (v: EmergencyVisitListItem) =>
    onConvert({ patientId: v.patientId, patientName: v.patientName, patientUhid: v.patientUhid, emergencyVisitId: v.id })

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <h3 className="mb-1 text-sm font-semibold text-slate-800">From ER Visits</h3>
      <p className="mb-3 text-xs text-slate-500">Disposed as "Admitted (Converted)" but not yet admitted.</p>
      <DateRangeFilter
        fromDate={fromDate} toDate={toDate}
        onFromDate={(v) => setRange((r) => ({ ...r, fromDate: v }))} onToDate={(v) => setRange((r) => ({ ...r, toDate: v }))}
      />
      {isLoading && <Spinner />}
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-3 py-2 font-medium text-slate-600">Visit No</th>
              <th className="px-3 py-2 font-medium text-slate-600">Patient</th>
              <th className="px-3 py-2 font-medium text-slate-600">Arrived</th>
              <th className="px-3 py-2 font-medium text-slate-600">MLC</th>
              <th className="px-3 py-2" />
            </tr>
          </thead>
          <tbody>
            {pending.map((v) => (
              <tr key={v.id} className="border-b border-slate-100 last:border-0">
                <td className="px-3 py-2 font-mono text-xs text-slate-500">{v.visitNumber}</td>
                <td className="px-3 py-2 text-slate-700">
                  {v.patientName} <span className="font-mono text-xs text-slate-400">({v.patientUhid})</span>
                </td>
                <td className="px-3 py-2 text-slate-700">{new Date(v.arrivedAt).toLocaleString()}</td>
                <td className="px-3 py-2">{v.isMlc && <StatusBadge tone="danger">MLC</StatusBadge>}</td>
                <td className="px-3 py-2 text-right">
                  <Button onClick={() => convert(v)}>Convert</Button>
                </td>
              </tr>
            ))}
            {!isLoading && pending.length === 0 && (
              <tr><td colSpan={5} className="px-3 py-8 text-center text-slate-400">Nothing waiting to be converted.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}
