interface TabsProps<T extends string> {
  tabs: readonly { key: T; label: string }[]
  active: T
  onChange: (key: T) => void
}

/** Simple underline tab bar used by multi-section master pages. */
export function Tabs<T extends string>({ tabs, active, onChange }: TabsProps<T>) {
  return (
    <div className="mb-4 flex gap-1 border-b border-slate-200">
      {tabs.map((tab) => (
        <button
          key={tab.key}
          type="button"
          onClick={() => onChange(tab.key)}
          className={`-mb-px border-b-2 px-4 py-2 text-sm font-medium transition-colors ${
            active === tab.key
              ? 'border-primary-600 text-primary-700'
              : 'border-transparent text-slate-500 hover:text-slate-700'
          }`}
        >
          {tab.label}
        </button>
      ))}
    </div>
  )
}
