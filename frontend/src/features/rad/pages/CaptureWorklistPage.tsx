import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useCaptureContrastConsent, useCaptureWorklist, useMarkImageCaptured } from '../api'
import { PRIORITY_LABELS, type CaptureWorklistItem } from '../types'

export function CaptureWorklistPage() {
  const { hasPermission } = useAuth()
  const canCapture = hasPermission('RAD.CAPTURE.MANAGE')
  const canCaptureConsent = hasPermission('RAD.CONTRASTCONSENT.CAPTURE')
  const { data: worklist = [], isLoading } = useCaptureWorklist()
  const markCaptured = useMarkImageCaptured()
  const [consenting, setConsenting] = useState<CaptureWorklistItem | null>(null)
  const [error, setError] = useState<string | null>(null)

  return (
    <>
      <PageHeader title="Capture Worklist" subtitle="Technician worklist — record image capture per scheduled test" />

      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Order No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Patient</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Test</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Scheduled</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Priority</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {worklist.map((item) => (
              <tr key={item.radOrderItemId} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{item.orderNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">
                  {item.patientName} ({item.patientUhid})
                </td>
                <td className="px-4 py-2.5 text-slate-700">
                  {item.radiologyTestName} <span className="text-xs text-slate-400">({item.modalityName})</span>
                  {item.requiresContrast && (
                    <span className="ml-2">
                      <StatusBadge tone={item.hasContrastConsent ? 'success' : 'warning'}>
                        {item.hasContrastConsent ? 'Consented' : 'Consent needed'}
                      </StatusBadge>
                    </span>
                  )}
                </td>
                <td className="px-4 py-2.5 text-slate-700">{new Date(item.scheduledAt).toLocaleString()}</td>
                <td className="px-4 py-2.5 text-slate-700">{PRIORITY_LABELS[item.priority]}</td>
                <td className="px-4 py-2.5 text-right">
                  <div className="flex justify-end gap-1">
                    {item.requiresContrast && !item.hasContrastConsent && canCaptureConsent && (
                      <Button variant="secondary" onClick={() => setConsenting(item)}>
                        Capture consent
                      </Button>
                    )}
                    {canCapture && (
                      <Button
                        onClick={async () => {
                          try {
                            await markCaptured.mutateAsync(item.radOrderItemId)
                          } catch (err) {
                            setError(errorMessage(err))
                          }
                        }}
                        disabled={item.requiresContrast && !item.hasContrastConsent}
                      >
                        Mark captured
                      </Button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
            {!isLoading && worklist.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-10 text-center text-slate-400">
                  Nothing awaiting image capture.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {consenting && <ConsentModal item={consenting} onClose={() => setConsenting(null)} onError={setError} />}
    </>
  )
}

function ConsentModal({
  item,
  onClose,
  onError,
}: {
  item: CaptureWorklistItem
  onClose: () => void
  onError: (msg: string) => void
}) {
  const capture = useCaptureContrastConsent()
  const [signedByName, setSignedByName] = useState(item.patientName)
  const [relationship, setRelationship] = useState('')

  const submit = async () => {
    if (!signedByName.trim()) return
    try {
      await capture.mutateAsync({ itemId: item.radOrderItemId, signedByName, relationshipToPatient: relationship || null })
      onClose()
    } catch (err) {
      onError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={`Contrast consent — ${item.radiologyTestName}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={capture.isPending} disabled={!signedByName.trim()}>
            Record consent
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        <Input label="Signed by" value={signedByName} onChange={(e) => setSignedByName(e.target.value)} />
        <Input label="Relationship to patient (if not self)" value={relationship} onChange={(e) => setRelationship(e.target.value)} />
      </div>
    </Modal>
  )
}
