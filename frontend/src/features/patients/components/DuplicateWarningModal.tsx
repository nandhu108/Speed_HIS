import { useNavigate } from 'react-router-dom'
import { Button } from '../../../components/ui/Button'
import { Modal } from '../../../components/ui/Modal'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { GENDER_LABELS, type DuplicateCandidate } from '../types'

interface DuplicateWarningModalProps {
  candidates: DuplicateCandidate[]
  onProceed: () => void
  onCancel: () => void
  loading?: boolean
}

/** Shown before saving when CheckDuplicates finds a possible match — a warning, not a hard block. */
export function DuplicateWarningModal({ candidates, onProceed, onCancel, loading }: DuplicateWarningModalProps) {
  const navigate = useNavigate()

  return (
    <Modal
      open
      title="Possible duplicate patients found"
      onClose={onCancel}
      footer={
        <>
          <Button variant="secondary" onClick={onCancel}>
            Go back and edit
          </Button>
          <Button variant="danger" onClick={onProceed} loading={loading}>
            Register anyway
          </Button>
        </>
      }
    >
      <p className="mb-3 text-sm text-slate-600">
        These existing patients closely match the details entered. Check whether this person is already registered
        before creating a new record.
      </p>
      <div className="space-y-2">
        {candidates.map((candidate) => (
          <div
            key={candidate.id}
            className="flex items-center justify-between rounded-md border border-amber-200 bg-amber-50 px-3 py-2"
          >
            <div>
              <p className="text-sm font-medium text-slate-800">
                {candidate.fullName} <span className="font-mono text-xs text-slate-500">({candidate.uhid})</span>
              </p>
              <p className="text-xs text-slate-500">
                {GENDER_LABELS[candidate.gender]} · {candidate.dateOfBirth ?? 'DOB unknown'} · {candidate.mobile}
              </p>
              <p className="mt-0.5 text-xs text-amber-700">{candidate.matchReasons.join(', ')}</p>
            </div>
            <div className="flex items-center gap-2">
              <StatusBadge tone="warning">Match {candidate.matchScore}%</StatusBadge>
              <Button variant="secondary" onClick={() => navigate(`/patients/${candidate.id}`)}>
                View
              </Button>
            </div>
          </div>
        ))}
      </div>
    </Modal>
  )
}
