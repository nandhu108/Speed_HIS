import { useState, useEffect } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import {
  useAbdmAdminStatus,
  useDataTransfers,
  useTestAbdmConnection,
  useToggleAbdmMilestone,
  useUpdateAbdmConfig,
} from '../../abdm/api'
import { ABDM_HI_TYPE_LABELS, type DataTransferLog } from '../../abdm/types'

export function AbdmAdminPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('ADMIN.ABDM.MANAGE') || hasPermission('ADMIN.SETTING.UPDATE')
  const { data: status, isLoading, refetch } = useAbdmAdminStatus()
  const { data: transfers = [] } = useDataTransfers()
  const toggleMilestone = useToggleAbdmMilestone()
  const updateConfig = useUpdateAbdmConfig()
  const testConnection = useTestAbdmConnection()

  const [form, setForm] = useState({
    gatewayUrl: '',
    clientId: '',
    clientSecret: '',
    hipId: '',
    hiuId: '',
  })
  const [testResult, setTestResult] = useState<{ online: boolean; message: string; gatewayUrl: string } | null>(null)

  useEffect(() => {
    if (status) {
      setForm({
        gatewayUrl: status.gatewayUrl ?? '',
        clientId: status.clientId ?? '',
        clientSecret: '',
        hipId: status.hipId ?? '',
        hiuId: status.hiuId ?? '',
      })
    }
  }, [status])

  if (isLoading || !status) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Spinner />
      </div>
    )
  }

  const handleToggle = async (milestone: 'master' | 'm1' | 'm2' | 'm3', currentValue: boolean) => {
    try {
      await toggleMilestone.mutateAsync({ milestone, enabled: !currentValue })
    } catch (err) {
      window.alert(errorMessage(err))
    }
  }

  const handleSaveConfig = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      await updateConfig.mutateAsync({
        masterEnabled: status.masterEnabled,
        m1Enabled: status.m1Enabled,
        m2Enabled: status.m2Enabled,
        m3Enabled: status.m3Enabled,
        gatewayUrl: form.gatewayUrl,
        clientId: form.clientId,
        clientSecret: form.clientSecret || undefined,
        hipId: form.hipId,
        hiuId: form.hiuId,
      })
      window.alert('ABDM Configuration saved successfully!')
      void refetch()
    } catch (err) {
      window.alert(errorMessage(err))
    }
  }

  const handleTestProbe = async () => {
    try {
      const res = await testConnection.mutateAsync()
      setTestResult(res)
    } catch (err) {
      setTestResult({
        online: false,
        message: errorMessage(err),
        gatewayUrl: form.gatewayUrl,
      })
    }
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Ayushman Bharat Digital Mission (ABDM)"
        subtitle="Manage national digital health ecosystem integration, M1/M2/M3 milestones and facility keys"
      />

      {/* ── Master Switch Hero ── */}
      <div className={`rounded-xl border p-6 transition-all ${
        status.masterEnabled
          ? 'border-emerald-300 bg-gradient-to-r from-emerald-50 via-teal-50 to-white shadow-sm'
          : 'border-slate-300 bg-gradient-to-r from-slate-100 to-white'
      }`}>
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="space-y-1">
            <div className="flex items-center gap-2">
              <span className="flex h-3 w-3 relative">
                <span className={`absolute inline-flex h-full w-full rounded-full opacity-75 animate-ping ${
                  status.masterEnabled ? 'bg-emerald-400' : 'bg-slate-400'
                }`} />
                <span className={`relative inline-flex h-3 w-3 rounded-full ${
                  status.masterEnabled ? 'bg-emerald-500' : 'bg-slate-500'
                }`} />
              </span>
              <h2 className="text-lg font-bold text-slate-800">
                ABDM Integration Master Switch
              </h2>
              <StatusBadge tone={status.masterEnabled ? 'success' : 'neutral'}>
                {status.masterEnabled ? 'ACTIVE & ONLINE' : 'DISABLED'}
              </StatusBadge>
            </div>
            <p className="text-sm text-slate-600 max-w-2xl">
              Enable or disable all Ayushman Bharat Digital Mission (ABDM) services across Prixo HIS.
              When disabled, all external M1, M2, and M3 network interactions are halted.
            </p>
          </div>

          <div className="flex items-center gap-3">
            <Button
              variant={status.masterEnabled ? 'secondary' : 'primary'}
              disabled={!canManage || toggleMilestone.isPending}
              loading={toggleMilestone.isPending}
              onClick={() => handleToggle('master', status.masterEnabled)}
            >
              {status.masterEnabled ? 'Disable ABDM' : 'Enable ABDM'}
            </Button>
          </div>
        </div>
      </div>

      {/* ── Milestones Grid (M1, M2, M3) ── */}
      <div className="grid gap-5 md:grid-cols-3">
        {/* Milestone 1: ABHA & Identity */}
        <div className="flex flex-col justify-between rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <span className="rounded-md bg-blue-100 px-2 py-0.5 text-xs font-bold text-blue-700">
                Milestone 1 (M1)
              </span>
              <StatusBadge tone={status.m1Enabled && status.masterEnabled ? 'success' : 'neutral'}>
                {status.m1Enabled && status.masterEnabled ? 'Enabled' : 'Disabled'}
              </StatusBadge>
            </div>
            <h3 className="font-semibold text-slate-800">ABHA & Patient Identity</h3>
            <p className="text-xs text-slate-500 leading-relaxed">
              Create and verify Ayushman Bharat Health Accounts (ABHA) via Aadhaar OTP, Mobile OTP, or direct counter QR Scan &amp; Share.
            </p>
            <div className="rounded-lg bg-slate-50 p-3">
              <p className="text-xs font-medium text-slate-500">Linked Patient ABHAs</p>
              <p className="text-2xl font-bold text-slate-800">{status.totalAbhaLinked}</p>
            </div>
          </div>

          <div className="mt-4 pt-3 border-t border-slate-100 flex items-center justify-between">
            <span className="text-xs text-slate-500">M1 Control</span>
            <Button
              className="px-2.5 py-1 text-xs"
              variant={status.m1Enabled ? 'secondary' : 'primary'}
              disabled={!canManage || toggleMilestone.isPending}
              onClick={() => handleToggle('m1', status.m1Enabled)}
            >
              {status.m1Enabled ? 'Disable M1' : 'Enable M1'}
            </Button>
          </div>
        </div>

        {/* Milestone 2: Health Information Provider (HIP) */}
        <div className="flex flex-col justify-between rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <span className="rounded-md bg-indigo-100 px-2 py-0.5 text-xs font-bold text-indigo-700">
                Milestone 2 (M2)
              </span>
              <StatusBadge tone={status.m2Enabled && status.masterEnabled ? 'success' : 'neutral'}>
                {status.m2Enabled && status.masterEnabled ? 'Enabled' : 'Disabled'}
              </StatusBadge>
            </div>
            <h3 className="font-semibold text-slate-800">Health Information Provider (HIP)</h3>
            <p className="text-xs text-slate-500 leading-relaxed">
              Link hospital encounters (OPD visits, IPD admissions, lab diagnostic reports) into care contexts and share NRCeS FHIR R4 bundles upon patient consent.
            </p>
            <div className="grid grid-cols-2 gap-2">
              <div className="rounded-lg bg-slate-50 p-3">
                <p className="text-xs font-medium text-slate-500">Linked Contexts</p>
                <p className="text-xl font-bold text-slate-800">{status.totalCareContextsLinked}</p>
              </div>
              <div className="rounded-lg bg-slate-50 p-3">
                <p className="text-xs font-medium text-slate-500">Data Shared</p>
                <p className="text-xl font-bold text-slate-800">{status.totalDataTransfers}</p>
              </div>
            </div>
          </div>

          <div className="mt-4 pt-3 border-t border-slate-100 flex items-center justify-between">
            <span className="text-xs text-slate-500">M2 Control</span>
            <Button
              className="px-2.5 py-1 text-xs"
              variant={status.m2Enabled ? 'secondary' : 'primary'}
              disabled={!canManage || toggleMilestone.isPending}
              onClick={() => handleToggle('m2', status.m2Enabled)}
            >
              {status.m2Enabled ? 'Disable M2' : 'Enable M2'}
            </Button>
          </div>
        </div>

        {/* Milestone 3: Health Information User (HIU) */}
        <div className="flex flex-col justify-between rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <span className="rounded-md bg-purple-100 px-2 py-0.5 text-xs font-bold text-purple-700">
                Milestone 3 (M3)
              </span>
              <StatusBadge tone={status.m3Enabled && status.masterEnabled ? 'success' : 'neutral'}>
                {status.m3Enabled && status.masterEnabled ? 'Enabled' : 'Disabled'}
              </StatusBadge>
            </div>
            <h3 className="font-semibold text-slate-800">Health Information User (HIU)</h3>
            <p className="text-xs text-slate-500 leading-relaxed">
              Empower clinicians to request patient consent and pull longitudinal health records from across India (AIIMS, Apollo, Max, etc.) into doctor consultation.
            </p>
            <div className="grid grid-cols-2 gap-2">
              <div className="rounded-lg bg-slate-50 p-3">
                <p className="text-xs font-medium text-slate-500">Consent Requests</p>
                <p className="text-xl font-bold text-slate-800">{status.totalConsentRequests}</p>
              </div>
              <div className="rounded-lg bg-slate-50 p-3">
                <p className="text-xs font-medium text-slate-500">Records Fetched</p>
                <p className="text-xl font-bold text-slate-800">{status.totalExternalRecords}</p>
              </div>
            </div>
          </div>

          <div className="mt-4 pt-3 border-t border-slate-100 flex items-center justify-between">
            <span className="text-xs text-slate-500">M3 Control</span>
            <Button
              className="px-2.5 py-1 text-xs"
              variant={status.m3Enabled ? 'secondary' : 'primary'}
              disabled={!canManage || toggleMilestone.isPending}
              onClick={() => handleToggle('m3', status.m3Enabled)}
            >
              {status.m3Enabled ? 'Disable M3' : 'Enable M3'}
            </Button>
          </div>
        </div>
      </div>

      {/* ── Configuration & Credentials Form ── */}
      <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex items-center justify-between border-b border-slate-100 pb-4 mb-4">
          <div>
            <h3 className="text-base font-semibold text-slate-800">ABDM Gateway &amp; Facility Registry Settings</h3>
            <p className="text-xs text-slate-500">Configure your registered ABDM Sandbox / Production credentials</p>
          </div>
          <Button
            type="button"
            variant="secondary"
            className="px-2.5 py-1 text-xs"
            loading={testConnection.isPending}
            onClick={handleTestProbe}
          >
            Probe Gateway Connection
          </Button>
        </div>

        {testResult && (
          <div className={`mb-4 rounded-lg p-3 text-sm ${
            testResult.online ? 'bg-emerald-50 text-emerald-800 border border-emerald-200' : 'bg-red-50 text-red-800 border border-red-200'
          }`}>
            <p className="font-semibold">{testResult.online ? '✓ Connection Successful' : '✗ Connection Failed'}</p>
            <p className="text-xs mt-0.5">{testResult.message} ({testResult.gatewayUrl})</p>
          </div>
        )}

        <form onSubmit={handleSaveConfig} className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <label className="block text-xs font-semibold text-slate-700">ABDM Gateway Base URL</label>
              <input
                className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-primary-500 focus:outline-none"
                placeholder="https://dev.abdm.gov.in/gateway"
                disabled={!canManage}
                value={form.gatewayUrl}
                onChange={(e) => setForm((f) => ({ ...f, gatewayUrl: e.target.value }))}
              />
              <p className="mt-1 text-xs text-slate-400">Sandbox or Production Gateway URL</p>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700">ABDM Client ID</label>
              <input
                className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-primary-500 focus:outline-none"
                placeholder="e.g. SBX_PRIXO_DEV"
                disabled={!canManage}
                value={form.clientId}
                onChange={(e) => setForm((f) => ({ ...f, clientId: e.target.value }))}
              />
              <p className="mt-1 text-xs text-slate-400">Assigned by NHA Developer Portal</p>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700">Facility HIP ID (Health Facility Registry)</label>
              <input
                className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-primary-500 focus:outline-none"
                placeholder="e.g. IN2910000123"
                disabled={!canManage}
                value={form.hipId}
                onChange={(e) => setForm((f) => ({ ...f, hipId: e.target.value }))}
              />
              <p className="mt-1 text-xs text-slate-400">Unique facility identifier used in Milestone 2</p>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700">Facility HIU ID</label>
              <input
                className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-primary-500 focus:outline-none"
                placeholder="e.g. IN2910000123"
                disabled={!canManage}
                value={form.hiuId}
                onChange={(e) => setForm((f) => ({ ...f, hiuId: e.target.value }))}
              />
              <p className="mt-1 text-xs text-slate-400">Unique consumer identifier used in Milestone 3</p>
            </div>

            <div className="sm:col-span-2">
              <label className="block text-xs font-semibold text-slate-700">ABDM Client Secret</label>
              <input
                type="password"
                className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-primary-500 focus:outline-none"
                placeholder="Leave blank to keep current secret"
                disabled={!canManage}
                value={form.clientSecret}
                onChange={(e) => setForm((f) => ({ ...f, clientSecret: e.target.value }))}
              />
              <p className="mt-1 text-xs text-slate-400">Kept encrypted in system configuration</p>
            </div>
          </div>

          {canManage && (
            <div className="flex justify-end pt-2">
              <Button type="submit" loading={updateConfig.isPending}>
                Save ABDM Configuration
              </Button>
            </div>
          )}
        </form>
      </div>

      {/* ── Recent FHIR Data Transfers Feed ── */}
      <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
        <h3 className="text-base font-semibold text-slate-800 mb-3">Recent ABDM Ecosystem Health Data Transfers</h3>
        {transfers.length === 0 ? (
          <p className="text-xs text-slate-400 py-4 text-center">No health data transfer events recorded yet.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-slate-50 text-slate-600 border-b border-slate-200">
                <tr>
                  <th className="py-2.5 px-3">Transaction ID</th>
                  <th className="py-2.5 px-3">Encounter Reference</th>
                  <th className="py-2.5 px-3">HI Type</th>
                  <th className="py-2.5 px-3">Recipient HIU</th>
                  <th className="py-2.5 px-3">Transfer Date</th>
                  <th className="py-2.5 px-3">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 text-slate-700">
                {transfers.slice(0, 10).map((t: DataTransferLog) => (
                  <tr key={t.id}>
                    <td className="py-2.5 px-3 font-mono text-[11px]">{t.transactionId}</td>
                    <td className="py-2.5 px-3 font-semibold">{t.careContextReference}</td>
                    <td className="py-2.5 px-3">
                      <span className="rounded bg-slate-100 px-1.5 py-0.5 font-medium">
                        {ABDM_HI_TYPE_LABELS[t.hiType as keyof typeof ABDM_HI_TYPE_LABELS] ?? t.hiType}
                      </span>
                    </td>
                    <td className="py-2.5 px-3">{t.recipientHiuId}</td>
                    <td className="py-2.5 px-3">{new Date(t.transferDate).toLocaleString()}</td>
                    <td className="py-2.5 px-3">
                      <StatusBadge tone="success">Transferred</StatusBadge>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}
