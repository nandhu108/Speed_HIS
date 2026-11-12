import { useMemo, useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { useAuth } from '../../auth/AuthContext'
import { useSettings, useUpdateSetting } from '../api'
import type { Setting } from '../types'

export function SettingsPage() {
  const { hasPermission } = useAuth()
  const canUpdate = hasPermission('ADMIN.SETTING.UPDATE')
  const { data: settings = [], isLoading } = useSettings()

  const groups = useMemo(() => {
    const map = new Map<string, Setting[]>()
    for (const setting of settings) {
      const list = map.get(setting.group) ?? []
      list.push(setting)
      map.set(setting.group, list)
    }
    return map
  }, [settings])

  return (
    <>
      <PageHeader title="System Settings" subtitle="Password policy, lockout, sessions and more" />
      {isLoading && <Spinner />}
      <div className="space-y-4">
        {[...groups.entries()].map(([group, items]) => (
          <div key={group} className="rounded-lg border border-slate-200 bg-white">
            <h2 className="border-b border-slate-200 px-4 py-2.5 text-sm font-semibold text-slate-700">{group}</h2>
            <div className="divide-y divide-slate-100">
              {items.map((setting) => (
                <SettingRow key={setting.key} setting={setting} canUpdate={canUpdate} />
              ))}
            </div>
          </div>
        ))}
      </div>
    </>
  )
}

function SettingRow({ setting, canUpdate }: { setting: Setting; canUpdate: boolean }) {
  const [draft, setDraft] = useState(setting.value)
  const updateSetting = useUpdateSetting()
  const dirty = draft !== setting.value
  const isBool = setting.value === 'true' || setting.value === 'false'

  const save = async (value: string) => {
    try {
      await updateSetting.mutateAsync({ key: setting.key, value })
    } catch (err) {
      window.alert(errorMessage(err))
      setDraft(setting.value)
    }
  }

  return (
    <div className="flex items-center justify-between gap-4 px-4 py-2.5">
      <div className="min-w-0">
        <p className="text-sm font-medium text-slate-700">{setting.key}</p>
        {setting.description && <p className="text-xs text-slate-400">{setting.description}</p>}
      </div>
      <div className="flex shrink-0 items-center gap-2">
        {isBool ? (
          <input
            type="checkbox"
            disabled={!canUpdate || updateSetting.isPending}
            checked={draft === 'true'}
            onChange={(e) => {
              const value = e.target.checked ? 'true' : 'false'
              setDraft(value)
              void save(value)
            }}
          />
        ) : (
          <>
            <input
              className="w-40 rounded-md border border-slate-300 px-2 py-1 text-sm disabled:bg-slate-50"
              disabled={!canUpdate}
              value={draft}
              onChange={(e) => setDraft(e.target.value)}
            />
            {dirty && (
              <Button loading={updateSetting.isPending} onClick={() => save(draft)}>
                Save
              </Button>
            )}
          </>
        )}
      </div>
    </div>
  )
}
