import { useState } from 'react'
import { PageHeader } from '../../../components/ui/PageHeader'
import { PatientPicker } from '../../appointments/components/PatientPicker'
import { PatientPhotoCapture } from '../components/PatientPhotoCapture'
import type { PatientListItem } from '../types'

/**
 * Standalone "Photo Capture" screen — search/select a patient, then capture or upload their
 * photo via the shared `PatientPhotoCapture` widget (same one embedded in the patient detail
 * page and the IP admission / OP registration flows).
 */
export function PhotoCapturePage() {
  const [patient, setPatient] = useState<PatientListItem | null>(null)

  return (
    <>
      <PageHeader title="Photo Capture" subtitle="Find a patient and capture or upload their photo" />

      <div className="max-w-2xl space-y-4">
        <PatientPicker selected={patient} onSelect={setPatient} />

        {patient && (
          <div className="rounded-shell border border-border-soft bg-panel p-5 shadow-shell-sm">
            <PatientPhotoCapture patientId={patient.id} personLabel={patient.fullName} />
          </div>
        )}
      </div>
    </>
  )
}
