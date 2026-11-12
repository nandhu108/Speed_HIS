import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { useAuth } from '../../auth/AuthContext'
import { useCollectSample, useCollectionWorklist, useReceiveSample, useRejectSample, useSamplesAwaitingReceipt } from '../api'
import { PRIORITY_LABELS, type CollectionWorklistItem, type Sample } from '../types'

export function CollectionWorklistPage() {
  const { hasPermission } = useAuth()
  const { data: worklist = [], isLoading: loadingWorklist } = useCollectionWorklist()
  const { data: samples = [], isLoading: loadingSamples } = useSamplesAwaitingReceipt()
  const receive = useReceiveSample()
  const reject = useRejectSample()

  const [rejecting, setRejecting] = useState<Sample | null>(null)
  const [error, setError] = useState<string | null>(null)

  const canCollect = hasPermission('LAB.SAMPLE.COLLECT')
  const canReceive = hasPermission('LAB.SAMPLE.RECEIVE')
  const canReject = hasPermission('LAB.SAMPLE.REJECT')

  // Group by (labOrderId, sampleTypeId) so a multi-select only spans items that can share one draw.
  const groups = new Map<string, CollectionWorklistItem[]>()
  for (const item of worklist) {
    const key = `${item.labOrderId}:${item.sampleTypeId}`
    groups.set(key, [...(groups.get(key) ?? []), item])
  }

  return (
    <>
      <PageHeader title="Collection Worklist" subtitle="Collect samples against ordered tests, then acknowledge receipt in the lab" />

      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <div className="mb-6 rounded-lg border border-slate-200 bg-white">
        <div className="border-b border-slate-200 px-4 py-3">
          <h2 className="text-sm font-semibold text-slate-800">Awaiting collection</h2>
        </div>
        {loadingWorklist && <Spinner />}
        {Array.from(groups.entries()).map(([key, items]) => (
          <CollectionGroup key={key} items={items} canCollect={canCollect} onError={setError} />
        ))}
        {!loadingWorklist && groups.size === 0 && <p className="px-4 py-10 text-center text-slate-400">Nothing awaiting collection.</p>}
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        <div className="border-b border-slate-200 px-4 py-3">
          <h2 className="text-sm font-semibold text-slate-800">Awaiting receipt in lab</h2>
        </div>
        {loadingSamples && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Sample No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Order No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Type</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Collected</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {samples.map((s) => (
              <tr key={s.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{s.sampleNumber}</td>
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{s.orderNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{s.sampleTypeName}</td>
                <td className="px-4 py-2.5 text-slate-700">{new Date(s.collectedAt).toLocaleString()}</td>
                <td className="px-4 py-2.5 text-right">
                  <div className="flex justify-end gap-1">
                    {canReceive && (
                      <Button
                        variant="secondary"
                        onClick={async () => {
                          try {
                            await receive.mutateAsync(s.id)
                          } catch (err) {
                            setError(errorMessage(err))
                          }
                        }}
                      >
                        Receive
                      </Button>
                    )}
                    {canReject && (
                      <Button variant="ghost" className="text-red-600" onClick={() => setRejecting(s)}>
                        Reject
                      </Button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
            {!loadingSamples && samples.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-10 text-center text-slate-400">
                  Nothing awaiting receipt.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {rejecting && (
        <RejectModal
          sample={rejecting}
          onClose={() => setRejecting(null)}
          onSubmit={async (reason) => {
            try {
              await reject.mutateAsync({ sampleId: rejecting.id, reason })
              setRejecting(null)
            } catch (err) {
              setError(errorMessage(err))
            }
          }}
          loading={reject.isPending}
        />
      )}
    </>
  )
}

function CollectionGroup({
  items,
  canCollect,
  onError,
}: {
  items: CollectionWorklistItem[]
  canCollect: boolean
  onError: (msg: string) => void
}) {
  const [selected, setSelected] = useState<Set<number>>(new Set(items.map((i) => i.labOrderItemId)))
  const collect = useCollectSample()
  const first = items[0]

  const toggle = (id: number) => {
    const next = new Set(selected)
    if (next.has(id)) next.delete(id)
    else next.add(id)
    setSelected(next)
  }

  const submit = async () => {
    if (selected.size === 0) return
    try {
      await collect.mutateAsync({
        labOrderId: first.labOrderId,
        sampleTypeId: first.sampleTypeId,
        labOrderItemIds: Array.from(selected),
      })
    } catch (err) {
      onError(errorMessage(err))
    }
  }

  return (
    <div className="border-b border-slate-100 px-4 py-3 last:border-0">
      <div className="mb-2 flex items-center justify-between">
        <div>
          <span className="font-mono text-xs text-slate-500">{first.orderNumber}</span>{' '}
          <span className="font-medium text-slate-700">{first.patientName}</span>{' '}
          <span className="text-xs text-slate-400">({first.patientUhid})</span>{' '}
          <span className="ml-2 rounded bg-slate-100 px-2 py-0.5 text-xs text-slate-600">{first.sampleTypeName}</span>{' '}
          <span className="text-xs text-slate-400">{PRIORITY_LABELS[first.priority]}</span>
        </div>
        {canCollect && (
          <Button onClick={submit} loading={collect.isPending} disabled={selected.size === 0}>
            Collect sample
          </Button>
        )}
      </div>
      <div className="flex flex-wrap gap-3">
        {items.map((item) => (
          <label key={item.labOrderItemId} className="flex items-center gap-2 text-sm text-slate-700">
            <input type="checkbox" checked={selected.has(item.labOrderItemId)} onChange={() => toggle(item.labOrderItemId)} />
            {item.labTestName}
          </label>
        ))}
      </div>
    </div>
  )
}

function RejectModal({
  sample,
  onClose,
  onSubmit,
  loading,
}: {
  sample: Sample
  onClose: () => void
  onSubmit: (reason: string) => void
  loading: boolean
}) {
  const [reason, setReason] = useState('')
  return (
    <Modal
      open
      title={`Reject sample ${sample.sampleNumber}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Back
          </Button>
          <Button variant="danger" onClick={() => onSubmit(reason)} loading={loading} disabled={!reason.trim()}>
            Reject
          </Button>
        </>
      }
    >
      <p className="mb-3 text-sm text-slate-600">The affected tests will bounce back to Ordered, awaiting recollection.</p>
      <Input label="Reason" value={reason} onChange={(e) => setReason(e.target.value)} />
    </Modal>
  )
}
