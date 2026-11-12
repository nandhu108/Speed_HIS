import { useRef, useState } from 'react'
import { endpoints } from '../../../api/endpoints'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { useDoctors, useSimpleMaster } from '../../masters/api'
import { useImportCampVisits } from '../api'
import type { BulkImportResult } from '../types'

/** "Camp Bulk Import" — pick one camp + doctor for the whole batch, then upload a CSV of UHIDs (one per line, optional "uhid" header). */
export function CampBulkImportPage() {
  const [campId, setCampId] = useState('')
  const [doctorId, setDoctorId] = useState('')
  const [result, setResult] = useState<BulkImportResult | null>(null)
  const [error, setError] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const { data: camps = [] } = useSimpleMaster('camps', endpoints.masters.camps)
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const importVisits = useImportCampVisits()

  const ready = !!campId && !!doctorId

  const onFilePicked = async (file: File | undefined) => {
    if (!file || !ready) return
    setError(null)
    try {
      const outcome = await importVisits.mutateAsync({ campId: Number(campId), doctorId: Number(doctorId), file })
      setResult(outcome)
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  return (
    <>
      <PageHeader title="Camp Bulk Import" subtitle="Register a batch of already-registered patients into one camp/doctor from a CSV of UHIDs" />

      {error && <p className="mb-3 rounded-shell-sm bg-crit/10 px-3 py-2 text-sm text-crit">{error}</p>}

      <div className="max-w-xl space-y-4 rounded-shell border border-border-soft bg-panel p-4 shadow-shell-sm">
        <div className="grid gap-3 sm:grid-cols-2">
          <Select
            label="Camp"
            placeholder="Select a camp…"
            options={camps.map((c) => ({ value: c.id, label: c.name }))}
            value={campId}
            onChange={(e) => setCampId(e.target.value)}
          />
          <Select
            label="Doctor"
            placeholder="Select a doctor…"
            options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
            value={doctorId}
            onChange={(e) => setDoctorId(e.target.value)}
          />
        </div>

        <div>
          <p className="mb-2 text-xs text-ink-faint">
            CSV: one UHID per line, with an optional "uhid" header row. Every row is registered against the camp and
            doctor selected above.
          </p>
          <input
            ref={fileInputRef}
            type="file"
            accept=".csv,text/csv"
            className="hidden"
            onChange={(e) => onFilePicked(e.target.files?.[0])}
          />
          <Button
            disabled={!ready}
            loading={importVisits.isPending}
            onClick={() => fileInputRef.current?.click()}
          >
            Import CSV
          </Button>
        </div>
      </div>

      {result && (
        <Modal
          open
          title="Import complete"
          onClose={() => setResult(null)}
          footer={<Button onClick={() => setResult(null)}>Close</Button>}
        >
          <div className="space-y-2 text-sm text-ink-muted">
            <p>
              Registered <strong>{result.inserted}</strong>, skipped <strong>{result.skipped}</strong>.
            </p>
            {result.errors.length > 0 && (
              <div className="max-h-48 overflow-y-auto rounded-shell-sm bg-crit/10 p-3">
                <p className="mb-1 font-medium text-crit">Errors</p>
                <ul className="list-inside list-disc space-y-0.5 text-crit">
                  {result.errors.map((message, index) => (
                    <li key={index}>{message}</li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        </Modal>
      )}
    </>
  )
}
