import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useDeleteDoctorPayoutRule, useDoctorPayoutRules, useDoctors, useSaveDoctorPayoutRule, useSimpleMaster } from '../api'
import { endpoints } from '../../../api/endpoints'
import { PAYOUT_TYPE_LABELS, type DoctorPayoutRule, type PayoutType } from '../types'

interface EditorState {
  id?: number
  doctorId: string
  serviceCategoryId: string
  payoutType: PayoutType
  value: string
  isActive: boolean
}

export function DoctorPayoutRulesPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('MASTER.DOCTORPAYOUTRULE.MANAGE')
  const { data: rows = [], isLoading } = useDoctorPayoutRules()
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 500 })
  const { data: serviceCategories = [] } = useSimpleMaster('service-categories', endpoints.masters.serviceCategories)
  const save = useSaveDoctorPayoutRule()
  const remove = useDeleteDoctorPayoutRule()

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [error, setError] = useState<string | null>(null)

  const openEditor = (row: DoctorPayoutRule | null) => {
    setError(null)
    setEditor(
      row
        ? {
            id: row.id,
            doctorId: String(row.doctorId),
            serviceCategoryId: String(row.serviceCategoryId),
            payoutType: row.payoutType,
            value: String(row.value),
            isActive: row.isActive,
          }
        : { doctorId: '', serviceCategoryId: '', payoutType: 1, value: '', isActive: true },
    )
  }

  const submit = async () => {
    if (!editor) return
    try {
      await save.mutateAsync({
        id: editor.id,
        doctorId: Number(editor.doctorId),
        serviceCategoryId: Number(editor.serviceCategoryId),
        payoutType: editor.payoutType,
        value: Number(editor.value),
        isActive: editor.isActive,
      })
      setEditor(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader
        title="Doctor Payout Rules"
        subtitle="Fee/commission share per doctor and service category — feeds the Doctors Payout report"
        actions={canManage && <Button onClick={() => openEditor(null)}>New payout rule</Button>}
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Doctor</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Service Category</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Type</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Value</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-medium text-slate-700">{row.doctorName}</td>
                <td className="px-4 py-2.5 text-slate-700">{row.serviceCategoryName}</td>
                <td className="px-4 py-2.5 text-slate-700">{PAYOUT_TYPE_LABELS[row.payoutType]}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">
                  {row.payoutType === 1 ? `${row.value}%` : row.value.toFixed(2)}
                </td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={row.isActive ? 'success' : 'neutral'}>
                    {row.isActive ? 'Active' : 'Inactive'}
                  </StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-right">
                  {canManage && (
                    <div className="flex justify-end gap-1">
                      <Button variant="ghost" onClick={() => openEditor(row)}>
                        Edit
                      </Button>
                      <Button
                        variant="ghost"
                        className="text-red-600"
                        onClick={async () => {
                          if (window.confirm(`Delete this payout rule for '${row.doctorName}'?`)) {
                            try {
                              await remove.mutateAsync(row.id)
                            } catch (err) {
                              window.alert(errorMessage(err))
                            }
                          }
                        }}
                      >
                        Delete
                      </Button>
                    </div>
                  )}
                </td>
              </tr>
            ))}
            {!isLoading && rows.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-10 text-center text-slate-400">
                  No payout rules yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit payout rule' : 'New payout rule'}
          onClose={() => setEditor(null)}
          footer={
            <>
              <Button variant="secondary" onClick={() => setEditor(null)}>
                Cancel
              </Button>
              <Button onClick={submit} loading={save.isPending}>
                Save
              </Button>
            </>
          }
        >
          <div className="space-y-3">
            {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
            <Select
              label="Doctor"
              placeholder="Select…"
              value={editor.doctorId}
              onChange={(e) => setEditor({ ...editor, doctorId: e.target.value })}
              options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
            />
            <Select
              label="Service Category"
              placeholder="Select…"
              value={editor.serviceCategoryId}
              onChange={(e) => setEditor({ ...editor, serviceCategoryId: e.target.value })}
              options={serviceCategories.filter((c) => c.isActive).map((c) => ({ value: c.id, label: c.name }))}
            />
            <div className="grid grid-cols-2 gap-3">
              <Select
                label="Payout type"
                value={String(editor.payoutType)}
                onChange={(e) => setEditor({ ...editor, payoutType: Number(e.target.value) as PayoutType })}
                options={[
                  { value: '1', label: 'Percent' },
                  { value: '2', label: 'Flat' },
                ]}
              />
              <Input
                label={editor.payoutType === 1 ? 'Percent' : 'Flat amount'}
                type="number"
                min={0}
                max={editor.payoutType === 1 ? 100 : undefined}
                step="0.01"
                value={editor.value}
                onChange={(e) => setEditor({ ...editor, value: e.target.value })}
              />
            </div>
            <label className="flex items-center gap-2 text-sm text-slate-700">
              <input
                type="checkbox"
                checked={editor.isActive}
                onChange={(e) => setEditor({ ...editor, isActive: e.target.checked })}
              />
              Active
            </label>
          </div>
        </Modal>
      )}
    </>
  )
}
