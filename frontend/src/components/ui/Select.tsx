import { forwardRef, useId, type SelectHTMLAttributes } from 'react'

export interface SelectOption {
  value: string | number
  label: string
}

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label?: string
  error?: string
  options: SelectOption[]
  placeholder?: string
}

export const Select = forwardRef<HTMLSelectElement, SelectProps>(function Select(
  { label, error, options, placeholder, id, className = '', ...rest },
  ref,
) {
  const autoId = useId()
  const selectId = id ?? autoId
  return (
    <div className={className}>
      {label && (
        <label
          htmlFor={selectId}
          className="mb-1.5 block text-[11.5px] font-bold tracking-wide text-ink-muted uppercase"
        >
          {label}
          {rest.required && (
            <span className="ml-0.5 text-crit" title="Required">
              *
            </span>
          )}
        </label>
      )}
      <select
        ref={ref}
        id={selectId}
        aria-invalid={!!error}
        className={`w-full rounded-shell-sm border bg-panel-solid px-3 py-2 text-sm text-ink outline-none transition-colors focus:ring-2 ${
          error
            ? 'border-crit focus:border-crit focus:ring-crit/10'
            : 'border-border-soft focus:border-accent-2 focus:ring-accent-2/15'
        }`}
        {...rest}
      >
        {placeholder && <option value="">{placeholder}</option>}
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
      {error && (
        <p role="alert" className="mt-1 text-xs font-medium text-crit">
          {error}
        </p>
      )}
    </div>
  )
})
