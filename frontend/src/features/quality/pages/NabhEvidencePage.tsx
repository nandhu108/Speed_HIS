import { useState } from 'react'
import { endpoints } from '../../../api/endpoints'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { useAuth } from '../../auth/AuthContext'
import { useSimpleMaster } from '../../masters/api'
import { evidenceFileUrl, useEvidence, useUploadEvidence } from '../api'

export function NabhEvidencePage() {
  const { hasPermission } = useAuth()
  const canUpload = hasPermission('QUALITY.EVIDENCE.UPLOAD')
  const { data: evidence = [], isLoading } = useEvidence()
  const [uploading, setUploading] = useState(false)

  return (
    <>
      <PageHeader
        title="NABH Evidence"
        subtitle="Chapter/standard-wise document repository — manage standards under Quality / NABH > NABH Standards"
        actions={canUpload && <Button onClick={() => setUploading(true)}>Upload evidence</Button>}
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Standard</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Title</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Uploaded by</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Uploaded at</th>
              <th className="px-4 py-2.5 font-medium text-slate-600"></th>
            </tr>
          </thead>
          <tbody>
            {evidence.map((e) => (
              <tr key={e.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{e.standardCode}</td>
                <td className="px-4 py-2.5 text-slate-700">{e.title}</td>
                <td className="px-4 py-2.5 text-slate-700">{e.uploadedByName}</td>
                <td className="px-4 py-2.5 text-slate-700">{new Date(e.uploadedAt).toLocaleString()}</td>
                <td className="px-4 py-2.5 text-right">
                  <a href={evidenceFileUrl(e.id)} target="_blank" rel="noreferrer" className="text-sm text-primary-600 hover:underline">Download</a>
                </td>
              </tr>
            ))}
            {!isLoading && evidence.length === 0 && (
              <tr><td colSpan={5} className="px-4 py-10 text-center text-slate-400">No evidence uploaded yet.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {uploading && <UploadModal onClose={() => setUploading(false)} />}
    </>
  )
}

function UploadModal({ onClose }: { onClose: () => void }) {
  const upload = useUploadEvidence()
  const { data: standards = [] } = useSimpleMaster('quality-nabh-standards', endpoints.quality.nabhStandards)
  const [standardId, setStandardId] = useState('')
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [file, setFile] = useState<File | null>(null)
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    if (!standardId || !title.trim() || !file) { setError('Standard, title and file are required.'); return }
    try {
      await upload.mutateAsync({ standardId: Number(standardId), title, description: description || null, file })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal open title="Upload NABH evidence" onClose={onClose} footer={<><Button variant="secondary" onClick={onClose}>Cancel</Button><Button onClick={submit} loading={upload.isPending}>Upload</Button></>}>
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <Select label="NABH standard" placeholder="Select standard" value={standardId} onChange={(e) => setStandardId(e.target.value)} options={standards.map((s) => ({ value: s.id, label: `${s.code} — ${s.name}` }))} />
        <Input label="Title" value={title} onChange={(e) => setTitle(e.target.value)} />
        <Input label="Description" value={description} onChange={(e) => setDescription(e.target.value)} />
        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700">File</label>
          <input type="file" onChange={(e) => setFile(e.target.files?.[0] ?? null)} className="block w-full text-sm text-slate-600" />
        </div>
      </div>
    </Modal>
  )
}
