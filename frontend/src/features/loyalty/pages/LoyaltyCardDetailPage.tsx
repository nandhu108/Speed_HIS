import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { PatientPicker } from '../../appointments/components/PatientPicker'
import { useAuth } from '../../auth/AuthContext'
import { useLoyaltyCardTypes, useSimpleMaster } from '../../masters/api'
import { endpoints } from '../../../api/endpoints'
import type { PatientListItem } from '../../patients/types'
import {
  openLoyaltyCardPdf,
  printLoyaltyCardDirect,
  useAddDependent,
  useDeactivateLoyaltyCard,
  useDuplicateIssue,
  useLoyaltyCard,
  useRemoveDependent,
  useRenewLoyaltyCard,
} from '../api'
import { LOYALTY_CARD_STATUS_LABELS, type LoyaltyCardStatus } from '../types'

const statusTone: Record<LoyaltyCardStatus, 'success' | 'danger'> = {
  1: 'success',
  2: 'danger',
}

export function LoyaltyCardDetailPage() {
  const { id } = useParams<{ id: string }>()
  const cardId = Number(id)
  const { hasPermission } = useAuth()
  const { data: card, isLoading } = useLoyaltyCard(cardId)
  const [renewing, setRenewing] = useState(false)
  const [deactivating, setDeactivating] = useState(false)
  const [addingDependent, setAddingDependent] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const duplicate = useDuplicateIssue(cardId)
  const removeDependent = useRemoveDependent(cardId)

  const canManage = hasPermission('LOYALTY.CARD.MANAGE')
  const canDeactivate = hasPermission('LOYALTY.CARD.DEACTIVATE')
  const isActive = card?.status === 1

  if (isLoading || !card) {
    return (
      <>
        <PageHeader title="Loyalty Card" subtitle="Loading…" backTo="/loyalty" />
        <Spinner />
      </>
    )
  }

  const doDuplicate = async () => {
    setError(null)
    try {
      await duplicate.mutateAsync(undefined as never)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader
        title={`Loyalty Card ${card.cardNumber}`}
        subtitle={`${card.patientName} (${card.patientUhid}) — ${card.loyaltyCardTypeName}`}
        backTo="/loyalty"
        actions={
          <div className="flex flex-wrap gap-2">
            <Button variant="secondary" onClick={() => openLoyaltyCardPdf(card.id)}>
              Print Preview
            </Button>
            <Button variant="secondary" onClick={() => printLoyaltyCardDirect(card.id)}>
              Print Direct
            </Button>
            {canManage && isActive && (
              <>
                <Button variant="secondary" onClick={doDuplicate} loading={duplicate.isPending}>
                  Duplicate Issue
                </Button>
                <Button variant="secondary" onClick={() => setRenewing(true)}>
                  Renew / Upgrade
                </Button>
              </>
            )}
            {canDeactivate && isActive && (
              <Button variant="danger" onClick={() => setDeactivating(true)}>
                Deactivate
              </Button>
            )}
          </div>
        }
      />

      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <div className="mb-6 grid grid-cols-2 gap-4 rounded-lg border border-slate-200 bg-white p-4 sm:grid-cols-4">
        <Field label="Status">
          <StatusBadge tone={statusTone[card.status]}>{LOYALTY_CARD_STATUS_LABELS[card.status]}</StatusBadge>
        </Field>
        <Field label="Category">{card.categoryName ?? '—'}</Field>
        <Field label="Issued">{card.issuedDate}</Field>
        <Field label="Expires">{card.expiryDate}</Field>
        <Field label="Duration">{`${card.durationMonths} month(s)`}</Field>
        <Field label="Amount">{card.amount.toFixed(2)}</Field>
        <Field label="Member limit">{`${card.members.length} / ${card.memberLimit}`}</Field>
        <Field label="Duplicates issued">{card.duplicateIssueCount}</Field>
        {!isActive && (
          <>
            <Field label="Deactivated">{card.deactivatedAt ? new Date(card.deactivatedAt).toLocaleString() : '—'}</Field>
            <Field label="Reason">{card.deactivateReason ?? '—'}</Field>
          </>
        )}
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3">
          <h2 className="text-sm font-semibold text-slate-700">Dependents</h2>
          {canManage && isActive && card.members.length < card.memberLimit && (
            <Button variant="secondary" onClick={() => setAddingDependent(true)}>
              Add dependent
            </Button>
          )}
        </div>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Name</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">UHID</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Relation</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Added</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {card.members.map((m) => (
              <tr key={m.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-medium text-slate-700">{m.patientName}</td>
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{m.patientUhid}</td>
                <td className="px-4 py-2.5 text-slate-500">{m.memberRelationTagName}</td>
                <td className="px-4 py-2.5 text-slate-500">{new Date(m.addedAt).toLocaleDateString()}</td>
                <td className="px-4 py-2.5 text-right">
                  {canManage && isActive && (
                    <Button
                      variant="ghost"
                      className="text-red-600"
                      onClick={async () => {
                        if (window.confirm(`Remove '${m.patientName}' from this card?`)) {
                          try {
                            await removeDependent.mutateAsync(m.id)
                          } catch (err) {
                            window.alert(errorMessage(err))
                          }
                        }
                      }}
                    >
                      Remove
                    </Button>
                  )}
                </td>
              </tr>
            ))}
            {card.members.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-slate-400">
                  No dependents added.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {renewing && <RenewModal cardId={card.id} currentTypeId={card.loyaltyCardTypeId} onClose={() => setRenewing(false)} />}
      {deactivating && <DeactivateModal cardId={card.id} onClose={() => setDeactivating(false)} />}
      {addingDependent && <AddDependentModal cardId={card.id} onClose={() => setAddingDependent(false)} />}
    </>
  )
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <div className="text-xs font-medium uppercase tracking-wide text-slate-400">{label}</div>
      <div className="mt-0.5 text-sm text-slate-700">{children}</div>
    </div>
  )
}

function RenewModal({ cardId, currentTypeId, onClose }: { cardId: number; currentTypeId: number; onClose: () => void }) {
  const renew = useRenewLoyaltyCard(cardId)
  const { data: cardTypes = [] } = useLoyaltyCardTypes()
  const [loyaltyCardTypeId, setLoyaltyCardTypeId] = useState(String(currentTypeId))
  const [durationMonths, setDurationMonths] = useState('12')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await renew.mutateAsync({ loyaltyCardTypeId: Number(loyaltyCardTypeId), durationMonths: Number(durationMonths) })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Renew / Upgrade card"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={renew.isPending}>
            Save
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <Select
          label="Card Type"
          value={loyaltyCardTypeId}
          onChange={(e) => setLoyaltyCardTypeId(e.target.value)}
          options={cardTypes.filter((t) => t.isActive).map((t) => ({ value: t.id, label: t.name }))}
        />
        <Input
          label="Duration (months)"
          type="number"
          min={1}
          value={durationMonths}
          onChange={(e) => setDurationMonths(e.target.value)}
        />
      </div>
    </Modal>
  )
}

function DeactivateModal({ cardId, onClose }: { cardId: number; onClose: () => void }) {
  const deactivate = useDeactivateLoyaltyCard(cardId)
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await deactivate.mutateAsync(reason)
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Deactivate card"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Back
          </Button>
          <Button variant="danger" onClick={submit} loading={deactivate.isPending} disabled={!reason.trim()}>
            Deactivate
          </Button>
        </>
      }
    >
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <Input label="Reason" value={reason} onChange={(e) => setReason(e.target.value)} />
    </Modal>
  )
}

function AddDependentModal({ cardId, onClose }: { cardId: number; onClose: () => void }) {
  const addDependent = useAddDependent(cardId)
  const { data: relationTags = [] } = useSimpleMaster('member-relation-tags', endpoints.masters.memberRelationTags)
  const [patient, setPatient] = useState<PatientListItem | null>(null)
  const [relationTagId, setRelationTagId] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    if (!patient || !relationTagId) return
    setError(null)
    try {
      await addDependent.mutateAsync({ patientId: patient.id, memberRelationTagId: Number(relationTagId) })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Add dependent"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={addDependent.isPending} disabled={!patient || !relationTagId}>
            Add
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <PatientPicker selected={patient} onSelect={setPatient} />
        <Select
          label="Relation"
          placeholder="Select relation…"
          value={relationTagId}
          onChange={(e) => setRelationTagId(e.target.value)}
          options={relationTags.map((t) => ({ value: t.id, label: t.name }))}
        />
      </div>
    </Modal>
  )
}
