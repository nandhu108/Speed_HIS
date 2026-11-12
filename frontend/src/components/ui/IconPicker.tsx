import { useState } from 'react'
import { MENU_ICONS, MenuIconFor } from './menuIcons'

const ICON_NAMES = Object.keys(MENU_ICONS).sort()

/** Visual picker over the curated menu-icon set — every choice is guaranteed to render in the sidebar (no free-text typos). */
export function IconPicker({ value, onChange }: { value: string | null; onChange: (name: string | null) => void }) {
  const [open, setOpen] = useState(false)

  return (
    <div className="relative">
      <span className="mb-1.5 block text-[10.5px] font-bold tracking-wider text-ink-faint uppercase">Icon</span>
      <button
        type="button"
        onClick={() => setOpen((o) => !o)}
        className="flex items-center gap-2 rounded-shell-sm border border-border-soft bg-panel-2 px-3 py-2 text-sm text-ink hover:border-border-strong"
      >
        <MenuIconFor name={value} />
        {value ?? 'No icon'}
      </button>

      {open && (
        <>
          <div className="fixed inset-0 z-10" onClick={() => setOpen(false)} />
          <div className="absolute z-20 mt-1.5 grid max-h-56 w-64 grid-cols-6 gap-1 overflow-y-auto rounded-shell-sm border border-border-soft bg-panel-solid p-2 shadow-shell-lg">
            <button
              type="button"
              title="No icon"
              onClick={() => {
                onChange(null)
                setOpen(false)
              }}
              className={`flex size-9 items-center justify-center rounded-shell-sm text-ink-faint hover:bg-panel-2 ${
                value === null ? 'ring-2 ring-accent-2/60' : ''
              }`}
            >
              <span className="size-1.5 rounded-full bg-current opacity-50" aria-hidden />
            </button>
            {ICON_NAMES.map((name) => (
              <button
                key={name}
                type="button"
                title={name}
                onClick={() => {
                  onChange(name)
                  setOpen(false)
                }}
                className={`flex size-9 items-center justify-center rounded-shell-sm text-ink hover:bg-panel-2 ${
                  value === name ? 'ring-2 ring-accent-2/60' : ''
                }`}
              >
                <MenuIconFor name={name} />
              </button>
            ))}
          </div>
        </>
      )}
    </div>
  )
}
