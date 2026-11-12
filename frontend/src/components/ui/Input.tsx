import { forwardRef, useId, type InputHTMLAttributes } from 'react'

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
  /** Extra classes applied to the <input> itself, e.g. to make room for a leading icon. */
  inputClassName?: string
}

export const Input = forwardRef<HTMLInputElement, InputProps>(function Input(
  { label, error, id, className = '', inputClassName = '', ...rest },
  ref,
) {
  const autoId = useId()
  const inputId = id ?? autoId
  return (
    <div className={className}>
      {label && (
        <label
          htmlFor={inputId}
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
      <input
        ref={ref}
        id={inputId}
        aria-invalid={!!error}
        className={`w-full rounded-shell-sm border bg-panel-2 px-3 py-2 text-sm text-ink outline-none transition-colors placeholder:text-ink-faint focus:ring-2 ${
          error
            ? 'border-crit focus:border-crit focus:ring-crit/10'
            : 'border-border-soft focus:border-accent-2 focus:ring-accent-2/15'
        } ${inputClassName}`}
        {...rest}
      />
      {error && (
        <p role="alert" className="mt-1 text-xs font-medium text-crit">
          {error}
        </p>
      )}
    </div>
  )
})
