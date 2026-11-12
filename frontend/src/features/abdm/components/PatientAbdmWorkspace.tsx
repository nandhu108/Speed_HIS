import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Modal } from '../../../components/ui/Modal'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import {
  useCareContexts,
  useCheckConsentStatus,
  useCreateConsentRequest,
  useExternalHealthRecords,
  useFetchExternalRecords,
  useLinkCareContexts,
  useScanAndShareAbha,
  useSyncCareContexts,
  useConsentRequests,
} from '../api'
import {
  ABDM_CARE_CONTEXT_LABELS,
  ABDM_CONSENT_STATUS_LABELS,
  ABDM_HI_TYPE_LABELS,
  ABDM_HI_TYPES,
  ABDM_METHOD_LABELS,
  ABDM_VERIFICATION_METHODS,
  ABHA_STATUS_LABELS,
  type AbdmVerificationMethod,
  type CareContext,
  type ConsentRequest,
  type ExternalHealthRecord,
} from '../types'
import { useAbhaProfile, useCompleteAbha, useInitiateAbha, useUnlinkAbha } from '../../patients/api'

interface PatientAbdmWorkspaceProps {
  patientId: number
  patientUhid: string
  canManage: boolean
}

export function PatientAbdmWorkspace({ patientId, patientUhid: _patientUhid, canManage }: PatientAbdmWorkspaceProps) {
  const [activeTab, setActiveTab] = useState<'m1' | 'm2' | 'm3'>('m1')
  const { data: abha, isLoading: loadingAbha } = useAbhaProfile(patientId)

  return (
    <div className="space-y-4">
      {/* ── Sub Navigation ── */}
      <div className="flex border-b border-slate-200">
        <button
          type="button"
          className={`border-b-2 px-4 py-2.5 text-sm font-medium transition-colors ${
            activeTab === 'm1'
              ? 'border-primary-600 text-primary-600'
              : 'border-transparent text-slate-500 hover:text-slate-700'
          }`}
          onClick={() => setActiveTab('m1')}
        >
          Milestone 1: ABHA &amp; Identity
        </button>
        <button
          type="button"
          className={`border-b-2 px-4 py-2.5 text-sm font-medium transition-colors ${
            activeTab === 'm2'
              ? 'border-primary-600 text-primary-600'
              : 'border-transparent text-slate-500 hover:text-slate-700'
          }`}
          onClick={() => setActiveTab('m2')}
        >
          Milestone 2: Care Contexts (HIP)
        </button>
        <button
          type="button"
          className={`border-b-2 px-4 py-2.5 text-sm font-medium transition-colors ${
            activeTab === 'm3'
              ? 'border-primary-600 text-primary-600'
              : 'border-transparent text-slate-500 hover:text-slate-700'
          }`}
          onClick={() => setActiveTab('m3')}
        >
          Milestone 3: External Health Records (HIU)
        </button>
      </div>

      {/* ── Tab Panes ── */}
      {activeTab === 'm1' && (
        <M1AbhaSection patientId={patientId} abha={abha} isLoading={loadingAbha} canManage={canManage} />
      )}
      {activeTab === 'm2' && (
        <M2HipCareContextSection patientId={patientId} hasAbha={!!abha} canManage={canManage} />
      )}
      {activeTab === 'm3' && (
        <M3HiuRecordsSection patientId={patientId} hasAbha={!!abha} canManage={canManage} />
      )}
    </div>
  )
}

// ─────────────────────────────────────────────────────────────────────────────
// Milestone 1 (M1) — ABHA Identity Section
// ─────────────────────────────────────────────────────────────────────────────

function M1AbhaSection({
  patientId,
  abha,
  isLoading,
  canManage,
}: {
  patientId: number
  abha: any
  isLoading: boolean
  canManage: boolean
}) {
  const [linking, setLinking] = useState(false)
  const unlink = useUnlinkAbha(patientId)

  const doUnlink = async () => {
    const reason = window.prompt('Reason for unlinking this ABHA from patient?')
    if (!reason?.trim()) return
    try {
      await unlink.mutateAsync(reason.trim())
    } catch (err) {
      window.alert(errorMessage(err))
    }
  }

  if (isLoading) return <Spinner />

  return (
    <div className="rounded-xl border border-slate-200 bg-white p-6 space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-base font-semibold text-slate-800">Ayushman Bharat Health Account (ABHA)</h3>
          <p className="text-xs text-slate-500">Milestone 1 — National digital identity verification and linkage</p>
        </div>
        {canManage && !abha && (
          <Button onClick={() => setLinking(true)}>
            Link / Create ABHA
          </Button>
        )}
      </div>

      {abha ? (
        <div className="rounded-xl border border-emerald-200 bg-emerald-50/50 p-5 space-y-3">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <div className="flex items-center gap-3">
              <span className="text-xl font-bold font-mono text-emerald-900 tracking-wide">
                {abha.abhaNumber}
              </span>
              <StatusBadge tone={abha.kycVerified ? 'success' : 'neutral'}>
                {abha.kycVerified ? '✓ KYC Verified' : 'Unverified'}
              </StatusBadge>
              <StatusBadge tone="info">
                {ABHA_STATUS_LABELS[abha.status as keyof typeof ABHA_STATUS_LABELS] ?? 'Linked'}
              </StatusBadge>
            </div>
            {canManage && (
              <Button className="px-2.5 py-1 text-xs" variant="danger" onClick={doUnlink}>
                Unlink ABHA
              </Button>
            )}
          </div>

          <div className="grid grid-cols-2 gap-4 text-xs sm:grid-cols-4 pt-2 border-t border-emerald-100">
            <div>
              <p className="text-slate-500 font-medium">ABHA Address (PHR)</p>
              <p className="font-semibold text-slate-800 mt-0.5">{abha.abhaAddress ?? '—'}</p>
            </div>
            <div>
              <p className="text-slate-500 font-medium">Linked Holder Name</p>
              <p className="font-semibold text-slate-800 mt-0.5">{abha.linkedName ?? '—'}</p>
            </div>
            <div>
              <p className="text-slate-500 font-medium">Gender &amp; YOB</p>
              <p className="font-semibold text-slate-800 mt-0.5">
                {abha.linkedGender ?? '—'} {abha.linkedYearOfBirth ? `(${abha.linkedYearOfBirth})` : ''}
              </p>
            </div>
            <div>
              <p className="text-slate-500 font-medium">Linked Date</p>
              <p className="font-semibold text-slate-800 mt-0.5">
                {abha.verifiedAt ? new Date(abha.verifiedAt).toLocaleDateString() : '—'}
              </p>
            </div>
          </div>
        </div>
      ) : (
        <div className="rounded-xl border border-dashed border-slate-300 p-8 text-center space-y-2">
          <p className="text-sm font-medium text-slate-700">No ABHA linked to this patient</p>
          <p className="text-xs text-slate-500 max-w-md mx-auto">
            Linking an ABHA binds the patient to India's national health data network, enabling care-context sharing (M2) and longitudinal records retrieval (M3).
          </p>
        </div>
      )}

      {linking && <LinkAbhaModal patientId={patientId} onClose={() => setLinking(false)} />}
    </div>
  )
}

function LinkAbhaModal({ patientId, onClose }: { patientId: number; onClose: () => void }) {
  const initiate = useInitiateAbha(patientId)
  const complete = useCompleteAbha(patientId)
  const scanAndShare = useScanAndShareAbha(patientId)

  const [mode, setMode] = useState<'otp' | 'qr'>('otp')
  const [method, setMethod] = useState<AbdmVerificationMethod>(ABDM_VERIFICATION_METHODS.AadhaarOtp)
  const [identifier, setIdentifier] = useState('')
  const [step, setStep] = useState<'request' | 'verify'>('request')
  const [sessionId, setSessionId] = useState<number | null>(null)
  const [maskedTarget, setMaskedTarget] = useState<string | null>(null)
  const [otp, setOtp] = useState('')
  const [qrText, setQrText] = useState('')
  const [error, setError] = useState<string | null>(null)

  const handleRequestOtp = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    if (!identifier.trim()) {
      setError('Please enter Aadhaar or Mobile or ABHA number.')
      return
    }
    try {
      const res = await initiate.mutateAsync({ method, identifier: identifier.trim() })
      setSessionId(res.sessionId)
      setMaskedTarget(res.maskedTarget)
      setStep('verify')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const handleVerifyOtp = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    if (!sessionId || !otp.trim()) {
      setError('Please enter the OTP.')
      return
    }
    try {
      await complete.mutateAsync({ sessionId, otp: otp.trim() })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const handleScanAndShare = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    if (!qrText.trim()) {
      setError('Please paste or scan ABDM QR code data.')
      return
    }
    try {
      await scanAndShare.mutateAsync(qrText.trim())
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal open onClose={onClose} title="Link Patient ABHA (ABDM M1)">
      <div className="space-y-4">
        {/* Toggle Mode: OTP vs QR Scan */}
        <div className="flex rounded-lg bg-slate-100 p-1">
          <button
            type="button"
            className={`flex-1 rounded-md py-1.5 text-xs font-semibold transition ${
              mode === 'otp' ? 'bg-white shadow text-slate-800' : 'text-slate-600 hover:text-slate-900'
            }`}
            onClick={() => setMode('otp')}
          >
            OTP Verification (Aadhaar / Mobile)
          </button>
          <button
            type="button"
            className={`flex-1 rounded-md py-1.5 text-xs font-semibold transition ${
              mode === 'qr' ? 'bg-white shadow text-slate-800' : 'text-slate-600 hover:text-slate-900'
            }`}
            onClick={() => setMode('qr')}
          >
            Scan &amp; Share QR Code
          </button>
        </div>

        {error && (
          <div className="rounded-md bg-red-50 p-2.5 text-xs text-red-700 border border-red-200">
            {error}
          </div>
        )}

        {mode === 'otp' ? (
          step === 'request' ? (
            <form onSubmit={handleRequestOtp} className="space-y-3">
              <div>
                <label className="block text-xs font-semibold text-slate-700">Verification Method</label>
                <select
                  className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
                  value={method}
                  onChange={(e) => setMethod(Number(e.target.value) as AbdmVerificationMethod)}
                >
                  <option value={ABDM_VERIFICATION_METHODS.AadhaarOtp}>{ABDM_METHOD_LABELS[1]}</option>
                  <option value={ABDM_VERIFICATION_METHODS.MobileOtp}>{ABDM_METHOD_LABELS[2]}</option>
                  <option value={ABDM_VERIFICATION_METHODS.AbhaOtp}>{ABDM_METHOD_LABELS[3]}</option>
                </select>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700">
                  {method === ABDM_VERIFICATION_METHODS.AadhaarOtp
                    ? '12-digit Aadhaar Number'
                    : method === ABDM_VERIFICATION_METHODS.MobileOtp
                    ? '10-digit Mobile Number'
                    : '14-digit ABHA Number or Address'}
                </label>
                <input
                  className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
                  placeholder={method === ABDM_VERIFICATION_METHODS.AadhaarOtp ? 'XXXX XXXX XXXX' : 'Enter identifier'}
                  value={identifier}
                  onChange={(e) => setIdentifier(e.target.value)}
                />
              </div>

              <div className="flex justify-end gap-2 pt-2">
                <Button type="button" variant="secondary" onClick={onClose}>
                  Cancel
                </Button>
                <Button type="submit" loading={initiate.isPending}>
                  Request OTP
                </Button>
              </div>
            </form>
          ) : (
            <form onSubmit={handleVerifyOtp} className="space-y-3">
              <p className="text-xs text-slate-600">
                OTP has been sent to {maskedTarget ?? 'registered mobile'}.
              </p>
              <div>
                <label className="block text-xs font-semibold text-slate-700">Enter OTP</label>
                <input
                  type="text"
                  maxLength={6}
                  className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-center text-lg font-mono tracking-widest"
                  placeholder="123456"
                  value={otp}
                  onChange={(e) => setOtp(e.target.value)}
                />
              </div>
              <div className="flex justify-end gap-2 pt-2">
                <Button type="button" variant="secondary" onClick={() => setStep('request')}>
                  Back
                </Button>
                <Button type="submit" loading={complete.isPending}>
                  Verify &amp; Link ABHA
                </Button>
              </div>
            </form>
          )
        ) : (
          <form onSubmit={handleScanAndShare} className="space-y-3">
            <div>
              <label className="block text-xs font-semibold text-slate-700">Scan &amp; Share QR Code Data</label>
              <textarea
                rows={4}
                className="mt-1 w-full rounded-md border border-slate-300 p-2 text-xs font-mono"
                placeholder='Paste raw QR code text or JSON payload from patient ABHA app...'
                value={qrText}
                onChange={(e) => setQrText(e.target.value)}
              />
              <p className="mt-1 text-[11px] text-slate-400">
                Supports standard ABDM QR payload containing ABHA number/address.
              </p>
            </div>
            <div className="flex justify-end gap-2 pt-2">
              <Button type="button" variant="secondary" onClick={onClose}>
                Cancel
              </Button>
              <Button type="submit" loading={scanAndShare.isPending}>
                Link from QR
              </Button>
            </div>
          </form>
        )}
      </div>
    </Modal>
  )
}

// ─────────────────────────────────────────────────────────────────────────────
// Milestone 2 (M2) — Health Information Provider (HIP) Section
// ─────────────────────────────────────────────────────────────────────────────

function M2HipCareContextSection({
  patientId,
  hasAbha,
  canManage,
}: {
  patientId: number
  hasAbha: boolean
  canManage: boolean
}) {
  const { data: contexts = [], isLoading, refetch } = useCareContexts(patientId)
  const syncContexts = useSyncCareContexts(patientId)
  const linkContexts = useLinkCareContexts(patientId)

  const handleSync = async () => {
    try {
      await syncContexts.mutateAsync()
      void refetch()
    } catch (err) {
      window.alert(errorMessage(err))
    }
  }

  const handleLinkAll = async () => {
    if (!hasAbha) {
      window.alert('Patient must have a linked ABHA before care contexts can be registered with ABDM.')
      return
    }
    try {
      await linkContexts.mutateAsync({ linkAllUnlinked: true })
      void refetch()
    } catch (err) {
      window.alert(errorMessage(err))
    }
  }

  if (isLoading) return <Spinner />

  return (
    <div className="rounded-xl border border-slate-200 bg-white p-6 space-y-4">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h3 className="text-base font-semibold text-slate-800">Care Context Linking (HIP)</h3>
          <p className="text-xs text-slate-500">
            Milestone 2 — Group encounters (OP visits, IP admissions, diagnostic reports) and publish to ABDM
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="secondary" className="px-2.5 py-1 text-xs" loading={syncContexts.isPending} onClick={handleSync}>
            Discover Encounters
          </Button>
          {canManage && (
            <Button className="px-2.5 py-1 text-xs" disabled={!hasAbha || linkContexts.isPending} loading={linkContexts.isPending} onClick={handleLinkAll}>
              Link All to ABDM
            </Button>
          )}
        </div>
      </div>

      {!hasAbha && (
        <div className="rounded-lg bg-amber-50 p-3 text-xs text-amber-800 border border-amber-200">
          ⚠️ Patient does not have a linked ABHA. Care contexts will be registered once ABHA is verified.
        </div>
      )}

      {contexts.length === 0 ? (
        <p className="text-xs text-slate-400 py-6 text-center">No care contexts discovered yet. Click &quot;Discover Encounters&quot; to scan.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="bg-slate-50 text-slate-600 border-b border-slate-200">
              <tr>
                <th className="py-2.5 px-3">Encounter Reference</th>
                <th className="py-2.5 px-3">Type</th>
                <th className="py-2.5 px-3">Encounter Description</th>
                <th className="py-2.5 px-3">Linked Date</th>
                <th className="py-2.5 px-3">ABDM Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 text-slate-700">
              {contexts.map((c: CareContext) => (
                <tr key={c.id}>
                  <td className="py-2.5 px-3 font-mono font-medium">{c.referenceNumber}</td>
                  <td className="py-2.5 px-3">
                    <span className="rounded bg-slate-100 px-1.5 py-0.5">
                      {ABDM_CARE_CONTEXT_LABELS[c.contextType as keyof typeof ABDM_CARE_CONTEXT_LABELS] ?? c.contextType}
                    </span>
                  </td>
                  <td className="py-2.5 px-3">{c.display}</td>
                  <td className="py-2.5 px-3">
                    {c.linkedAt ? new Date(c.linkedAt).toLocaleDateString() : '—'}
                  </td>
                  <td className="py-2.5 px-3">
                    <StatusBadge tone={c.isLinked ? 'success' : 'neutral'}>
                      {c.isLinked ? 'Linked to ABDM' : 'Pending Link'}
                    </StatusBadge>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

// ─────────────────────────────────────────────────────────────────────────────
// Milestone 3 (M3) — Health Information User (HIU) Section
// ─────────────────────────────────────────────────────────────────────────────

function M3HiuRecordsSection({
  patientId,
  hasAbha,
  canManage,
}: {
  patientId: number
  hasAbha: boolean
  canManage: boolean
}) {
  const { data: records = [], isLoading } = useExternalHealthRecords(patientId)
  const { data: consents = [] } = useConsentRequests(patientId)
  const [requesting, setRequesting] = useState(false)
  const [viewingRecord, setViewingRecord] = useState<ExternalHealthRecord | null>(null)
  const checkStatus = useCheckConsentStatus(patientId)
  const fetchRecords = useFetchExternalRecords(patientId)

  const handleCheckAndFetch = async (consentId: number) => {
    try {
      await checkStatus.mutateAsync(consentId)
      await fetchRecords.mutateAsync(consentId)
    } catch (err) {
      window.alert(errorMessage(err))
    }
  }

  if (isLoading) return <Spinner />

  return (
    <div className="space-y-6">
      {/* Records Header Card */}
      <div className="rounded-xl border border-slate-200 bg-white p-6 space-y-4">
        <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h3 className="text-base font-semibold text-slate-800">Longitudinal Health Records (HIU)</h3>
            <p className="text-xs text-slate-500">
              Milestone 3 — View historical consultations, prescriptions, and reports from external healthcare facilities across India
            </p>
          </div>
          {canManage && (
            <Button disabled={!hasAbha} onClick={() => setRequesting(true)}>
              Request Patient Consent
            </Button>
          )}
        </div>

        {!hasAbha && (
          <div className="rounded-lg bg-amber-50 p-3 text-xs text-amber-800 border border-amber-200">
            ⚠️ Patient requires a linked ABHA (Milestone 1) before external health records can be requested.
          </div>
        )}

        {/* External Records List */}
        {records.length === 0 ? (
          <div className="rounded-xl border border-dashed border-slate-200 p-8 text-center space-y-2">
            <p className="text-sm font-medium text-slate-700">No external health records fetched yet</p>
            <p className="text-xs text-slate-400 max-w-md mx-auto">
              Initiate a consent request to the patient&apos;s ABHA PHR app. Once approved, medical records from external hospitals can be pulled securely.
            </p>
          </div>
        ) : (
          <div className="grid gap-4 md:grid-cols-2">
            {records.map((r: ExternalHealthRecord) => (
              <div
                key={r.id}
                className="rounded-lg border border-slate-200 bg-slate-50/50 p-4 space-y-2 hover:bg-slate-50 transition cursor-pointer"
                onClick={() => setViewingRecord(r)}
              >
                <div className="flex items-center justify-between">
                  <span className="rounded bg-primary-100 px-2 py-0.5 text-[11px] font-semibold text-primary-800">
                    {ABDM_HI_TYPE_LABELS[r.hiType as keyof typeof ABDM_HI_TYPE_LABELS] ?? r.hiType}
                  </span>
                  <span className="text-[11px] text-slate-400">
                    {new Date(r.recordDate).toLocaleDateString()}
                  </span>
                </div>
                <h4 className="text-sm font-bold text-slate-800">{r.title}</h4>
                <p className="text-xs font-medium text-slate-600 line-clamp-2">
                  {r.clinicalSummary}
                </p>
                <div className="pt-2 border-t border-slate-200/60 flex items-center justify-between text-[11px] text-slate-500">
                  <span className="truncate max-w-[200px]">📍 {r.sourceFacilityName}</span>
                  <span className="text-primary-600 font-semibold hover:underline">View FHIR &rarr;</span>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Consent Requests Status Table */}
      <div className="rounded-xl border border-slate-200 bg-white p-6 space-y-3">
        <h4 className="text-sm font-semibold text-slate-800">ABDM Consent Requests Log</h4>
        {consents.length === 0 ? (
          <p className="text-xs text-slate-400 py-3">No consent requests initiated for this patient yet.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-slate-50 text-slate-600 border-b border-slate-200">
                <tr>
                  <th className="py-2.5 px-3">Request ID</th>
                  <th className="py-2.5 px-3">Purpose</th>
                  <th className="py-2.5 px-3">Requested HI Types</th>
                  <th className="py-2.5 px-3">Status</th>
                  <th className="py-2.5 px-3">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 text-slate-700">
                {consents.map((c: ConsentRequest) => (
                  <tr key={c.id}>
                    <td className="py-2.5 px-3 font-mono font-medium">{c.consentRequestId}</td>
                    <td className="py-2.5 px-3 font-semibold">{c.purpose}</td>
                    <td className="py-2.5 px-3">{c.hiTypes}</td>
                    <td className="py-2.5 px-3">
                      <StatusBadge tone={c.status === 2 ? 'success' : c.status === 1 ? 'warning' : 'neutral'}>
                        {ABDM_CONSENT_STATUS_LABELS[c.status as keyof typeof ABDM_CONSENT_STATUS_LABELS] ?? c.status}
                      </StatusBadge>
                    </td>
                    <td className="py-2.5 px-3">
                      <Button
                        className="px-2.5 py-1 text-xs"
                        variant="secondary"
                        loading={fetchRecords.isPending || checkStatus.isPending}
                        onClick={() => handleCheckAndFetch(c.id)}
                      >
                        {c.status === 2 ? 'Fetch Records' : 'Check Status & Fetch'}
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* New Consent Request Modal */}
      {requesting && (
        <CreateConsentModal patientId={patientId} onClose={() => setRequesting(false)} />
      )}

      {/* Record Detail Modal */}
      {viewingRecord && (
        <Modal open onClose={() => setViewingRecord(null)} title={viewingRecord.title}>
          <div className="space-y-4">
            <div className="rounded-lg bg-slate-50 p-3 space-y-1 text-xs">
              <p><span className="font-semibold text-slate-700">Source Facility:</span> {viewingRecord.sourceFacilityName} ({viewingRecord.sourceFacilityHipId})</p>
              <p><span className="font-semibold text-slate-700">Authoring Practitioner:</span> {viewingRecord.doctorName ?? 'Staff Physician'}</p>
              <p><span className="font-semibold text-slate-700">Clinical Date:</span> {new Date(viewingRecord.recordDate).toLocaleString()}</p>
            </div>

            <div>
              <p className="text-xs font-semibold text-slate-700 mb-1">Clinical Summary</p>
              <div className="rounded-lg border border-slate-200 bg-white p-3 text-xs text-slate-800 whitespace-pre-wrap leading-relaxed">
                {viewingRecord.clinicalSummary}
              </div>
            </div>

            <div>
              <p className="text-xs font-semibold text-slate-700 mb-1">NRCeS FHIR R4 Document Payload</p>
              <pre className="max-h-48 overflow-auto rounded-lg bg-slate-900 p-3 text-[11px] text-emerald-400 font-mono">
                {(() => {
                  try {
                    return JSON.stringify(JSON.parse(viewingRecord.fhirBundleJson), null, 2)
                  } catch {
                    return viewingRecord.fhirBundleJson
                  }
                })()}
              </pre>
            </div>

            <div className="flex justify-end pt-2">
              <Button onClick={() => setViewingRecord(null)}>Close</Button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  )
}

function CreateConsentModal({ patientId, onClose }: { patientId: number; onClose: () => void }) {
  const createConsent = useCreateConsentRequest(patientId)
  const [purpose, setPurpose] = useState('CAET')
  const [selectedHiTypes, setSelectedHiTypes] = useState<number[]>([
    ABDM_HI_TYPES.OpConsultation,
    ABDM_HI_TYPES.Prescription,
    ABDM_HI_TYPES.DiagnosticReport,
  ])
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    if (selectedHiTypes.length === 0) {
      setError('Please select at least one HI Type.')
      return
    }

    try {
      const now = new Date()
      const oneYearAgo = new Date(now.getTime() - 365 * 24 * 60 * 60 * 1000)
      const expiry = new Date(now.getTime() + 30 * 24 * 60 * 60 * 1000)

      await createConsent.mutateAsync({
        patientId,
        purpose,
        hiTypes: selectedHiTypes,
        dateFrom: oneYearAgo.toISOString(),
        dateTo: now.toISOString(),
        expiryAt: expiry.toISOString(),
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const toggleHiType = (type: number) => {
    setSelectedHiTypes((prev) =>
      prev.includes(type) ? prev.filter((t) => t !== type) : [...prev, type]
    )
  }

  return (
    <Modal open onClose={onClose} title="Raise ABDM Consent Request (HIU)">
      <form onSubmit={handleSubmit} className="space-y-4">
        {error && (
          <div className="rounded-md bg-red-50 p-2 text-xs text-red-700 border border-red-200">
            {error}
          </div>
        )}

        <div>
          <label className="block text-xs font-semibold text-slate-700">Purpose of Access</label>
          <select
            className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
            value={purpose}
            onChange={(e) => setPurpose(e.target.value)}
          >
            <option value="CAET">Care Management (Routine Clinical Consultation)</option>
            <option value="BTG">Break-the-Glass (Emergency Care)</option>
            <option value="PUBHLTH">Public Health Surveillance</option>
          </select>
        </div>

        <div>
          <label className="block text-xs font-semibold text-slate-700 mb-2">Requested Health Information Types</label>
          <div className="grid grid-cols-2 gap-2 text-xs">
            {Object.entries(ABDM_HI_TYPES).map(([name, code]) => (
              <label key={code} className="flex items-center gap-2 rounded border border-slate-200 p-2 cursor-pointer hover:bg-slate-50">
                <input
                  type="checkbox"
                  checked={selectedHiTypes.includes(code)}
                  onChange={() => toggleHiType(code)}
                />
                <span>{ABDM_HI_TYPE_LABELS[code as keyof typeof ABDM_HI_TYPE_LABELS] ?? name}</span>
              </label>
            ))}
          </div>
        </div>

        <div className="rounded-lg bg-blue-50 p-3 text-xs text-blue-800">
          ℹ️ The patient will receive an authorization prompt in their registered ABDM PHR app (e.g. ABHA App, Aarogya Setu).
        </div>

        <div className="flex justify-end gap-2 pt-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" loading={createConsent.isPending}>
            Send Consent Request
          </Button>
        </div>
      </form>
    </Modal>
  )
}
