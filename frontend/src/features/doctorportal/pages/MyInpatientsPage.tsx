import { useNavigate } from 'react-router-dom'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { useMyInpatients } from '../api'

/** IP Rounds (plan §22: "my inpatients, quick progress notes + orders") — the list is new, but each row opens IPD's existing AdmissionDetailPage outright (progress notes, vitals, doctor orders are all already there). */
export function MyInpatientsPage() {
  const navigate = useNavigate()
  const { data: admissions = [], isLoading } = useMyInpatients()

  return (
    <>
      <PageHeader title="My Inpatients" subtitle="Active admissions where you are a consultant" />
      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Admission</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Patient</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Bed</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Admitted</th>
            </tr>
          </thead>
          <tbody>
            {admissions.map((a) => (
              <tr
                key={a.id}
                className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50"
                onClick={() => navigate(`/ipd/admissions/${a.id}`)}
              >
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{a.admissionNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">
                  {a.patientName} <span className="font-mono text-xs text-slate-400">({a.patientUhid})</span>
                </td>
                <td className="px-4 py-2.5 text-slate-700">{a.wardName} / {a.roomName} / {a.bedCode}</td>
                <td className="px-4 py-2.5 text-slate-500">{new Date(a.admissionDate).toLocaleDateString()}</td>
              </tr>
            ))}
            {!isLoading && admissions.length === 0 && (
              <tr><td colSpan={4} className="px-4 py-10 text-center text-slate-400">No active inpatients under your care.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  )
}
