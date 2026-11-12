import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Select } from '../../../components/ui/Select'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { endpoints } from '../../../api/endpoints'
import { useSimpleMaster } from '../../masters/api'
import { useAuth } from '../../auth/AuthContext'
import {
  downloadScannedDocumentUrl,
  useCreateRecordFile,
  useFileAudit,
  useRecordFiles,
  useSaveFileAudit,
  useScannedDocuments,
  useUploadScannedDocument,
} from '../api'

/// Embedded in IPD's AdmissionDetailPage as an "MRD" tab — same pattern as ICU's AdmissionIcuTab:
/// its own file so the already-huge host file only needs one import + one render line.
export function AdmissionMrdTab({ admissionId, setError }: { admissionId: number; setError: (e: string | null) => void }) {
  const run = async (fn: () => Promise<unknown>) => {
    setError(null)
    try {
      await fn()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="space-y-4">
      <RecordFileCard admissionId={admissionId} run={run} />
      <DocumentsCard admissionId={admissionId} run={run} />
      <FileAuditCard admissionId={admissionId} run={run} />
    </div>
  )
}

function RecordFileCard({ admissionId, run }: { admissionId: number; run: (fn: () => Promise<unknown>) => Promise<void> }) {
  const { hasPermission } = useAuth()
  const { data: files = [], isLoading } = useRecordFiles({ admissionId })
  const file = files[0] ?? null
  const create = useCreateRecordFile()

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <h3 className="mb-2 text-sm font-semibold text-slate-700">Medical Record File</h3>
      {isLoading ? null : file ? (
        <div className="flex flex-wrap items-center gap-4 text-sm">
          <span className="font-mono text-slate-600">{file.fileNumber}</span>
          <StatusBadge tone={file.status === 1 ? 'success' : 'neutral'}>{file.status === 1 ? 'Active' : 'Destroyed'}</StatusBadge>
          <span className="text-slate-500">Location: {file.currentLocation}</span>
          <a href={`/mrd/files/${file.id}`} className="text-primary-600 hover:underline">View file →</a>
        </div>
      ) : (
        <div className="flex items-center gap-3">
          <p className="text-sm text-slate-500">No medical record file opened for this admission yet.</p>
          {hasPermission('MRD.FILE.CREATE') && (
            <Button variant="secondary" loading={create.isPending} onClick={() => run(() => create.mutateAsync(admissionId))}>
              Open record file
            </Button>
          )}
        </div>
      )}
    </div>
  )
}

function DocumentsCard({ admissionId, run }: { admissionId: number; run: (fn: () => Promise<unknown>) => Promise<void> }) {
  const { hasPermission } = useAuth()
  const canUpload = hasPermission('MRD.DOCUMENT.UPLOAD')
  const { data: documents = [] } = useScannedDocuments(admissionId)
  const { data: documentTypes = [] } = useSimpleMaster('document-types', endpoints.masters.documentTypes)
  const upload = useUploadScannedDocument(admissionId)

  const [documentTypeId, setDocumentTypeId] = useState('')
  const [description, setDescription] = useState('')
  const [file, setFile] = useState<File | null>(null)

  const submit = () =>
    run(() => upload.mutateAsync({ documentTypeId: Number(documentTypeId), description: description || null, file: file! })).then(() => {
      setDocumentTypeId('')
      setDescription('')
      setFile(null)
    })

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <h3 className="mb-2 text-sm font-semibold text-slate-700">Scanned Documents</h3>

      <table className="mb-3 w-full text-xs">
        <tbody>
          {documents.map((d) => (
            <tr key={d.id} className="border-t border-slate-100">
              <td className="py-1.5 text-slate-500">{new Date(d.uploadedAt).toLocaleDateString()}</td>
              <td className="py-1.5">{d.documentTypeName}</td>
              <td className="py-1.5">
                <a href={downloadScannedDocumentUrl(admissionId, d.id)} target="_blank" rel="noreferrer" className="text-primary-600 hover:underline">
                  {d.fileName}
                </a>
              </td>
              <td className="py-1.5 text-slate-500">{d.description ?? ''}</td>
            </tr>
          ))}
          {documents.length === 0 && (
            <tr>
              <td className="py-3 text-center text-slate-400">No documents uploaded.</td>
            </tr>
          )}
        </tbody>
      </table>

      {canUpload && (
        <div className="flex flex-wrap items-end gap-2 border-t border-slate-100 pt-3">
          <Select
            label="Document type"
            value={documentTypeId}
            onChange={(e) => setDocumentTypeId(e.target.value)}
            placeholder="Select type"
            options={documentTypes.map((t) => ({ value: t.id, label: t.name }))}
            className="w-48"
          />
          <Input label="Description (optional)" value={description} onChange={(e) => setDescription(e.target.value)} />
          <input type="file" onChange={(e) => setFile(e.target.files?.[0] ?? null)} className="text-sm" />
          <Button onClick={submit} loading={upload.isPending} disabled={!documentTypeId || !file}>Upload</Button>
        </div>
      )}
    </div>
  )
}

function FileAuditCard({ admissionId, run }: { admissionId: number; run: (fn: () => Promise<unknown>) => Promise<void> }) {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('MRD.AUDIT.MANAGE')
  const { data: audit } = useFileAudit(admissionId)
  const save = useSaveFileAudit(admissionId)

  const [form, setForm] = useState({
    dischargeSummarySigned: audit?.dischargeSummarySigned ?? false,
    consentsPresent: audit?.consentsPresent ?? false,
    progressNotesComplete: audit?.progressNotesComplete ?? false,
    investigationReportsPresent: audit?.investigationReportsPresent ?? false,
    nursingRecordsComplete: audit?.nursingRecordsComplete ?? false,
    missingDocumentsNotes: audit?.missingDocumentsNotes ?? '',
  })
  const [dirty, setDirty] = useState(false)

  const effective = dirty ? form : {
    dischargeSummarySigned: audit?.dischargeSummarySigned ?? false,
    consentsPresent: audit?.consentsPresent ?? false,
    progressNotesComplete: audit?.progressNotesComplete ?? false,
    investigationReportsPresent: audit?.investigationReportsPresent ?? false,
    nursingRecordsComplete: audit?.nursingRecordsComplete ?? false,
    missingDocumentsNotes: audit?.missingDocumentsNotes ?? '',
  }

  const check = (key: keyof typeof form) => (
    <label className="flex items-center gap-2 text-sm text-slate-700">
      <input
        type="checkbox"
        checked={effective[key] as boolean}
        disabled={!canManage}
        onChange={(e) => {
          setDirty(true)
          setForm({ ...effective, [key]: e.target.checked })
        }}
      />
      {CHECKLIST_LABELS[key]}
    </label>
  )

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <div className="mb-2 flex items-center justify-between">
        <h3 className="text-sm font-semibold text-slate-700">Discharge File Audit</h3>
        {audit && <StatusBadge tone={audit.isComplete ? 'success' : 'danger'}>{audit.isComplete ? 'Complete' : 'Incomplete'}</StatusBadge>}
      </div>
      <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
        {check('dischargeSummarySigned')}
        {check('consentsPresent')}
        {check('progressNotesComplete')}
        {check('investigationReportsPresent')}
        {check('nursingRecordsComplete')}
      </div>
      <Input
        label="Missing documents notes"
        className="mt-2"
        value={effective.missingDocumentsNotes ?? ''}
        disabled={!canManage}
        onChange={(e) => {
          setDirty(true)
          setForm({ ...effective, missingDocumentsNotes: e.target.value })
        }}
      />
      {canManage && (
        <Button
          className="mt-3"
          loading={save.isPending}
          onClick={() =>
            run(() => save.mutateAsync({ ...effective, missingDocumentsNotes: effective.missingDocumentsNotes || null })).then(() => setDirty(false))
          }
        >
          Save audit
        </Button>
      )}
      {audit && <p className="mt-2 text-xs text-slate-400">Last audited by {audit.auditedByName} on {new Date(audit.auditedAt).toLocaleString()}</p>}
    </div>
  )
}

const CHECKLIST_LABELS: Record<string, string> = {
  dischargeSummarySigned: 'Discharge summary signed',
  consentsPresent: 'Consents present',
  progressNotesComplete: 'Progress notes complete',
  investigationReportsPresent: 'Investigation reports present',
  nursingRecordsComplete: 'Nursing records complete',
}
