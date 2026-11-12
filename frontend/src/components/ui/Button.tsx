import type { ButtonHTMLAttributes } from 'react'

type Variant = 'primary' | 'secondary' | 'danger' | 'ghost'

/* Flat corporate palette (2026-08-11): a solid navy fill that darkens on hover, rather than the
   previous accent-2→indigo gradient. Under the new tokens those two stops are blue-600 and
   blue-800 — close enough that the gradient read as a muddy smear instead of a deliberate one,
   so the hue step now does real work as the hover state. */
const variants: Record<Variant, string> = {
  primary: 'bg-accent-2 text-white shadow-shell-sm hover:bg-indigo disabled:hover:bg-accent-2',
  // Secondary previously changed only its border on hover, which was easy to miss on a dense
  // toolbar; it now shifts its fill like the others so it reads as clickable.
  secondary: 'bg-panel-2 text-ink border border-border-soft hover:border-border-strong hover:bg-panel',
  danger: 'bg-crit text-white hover:brightness-110',
  ghost: 'text-ink-muted hover:bg-panel-2 hover:text-ink',
}

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant
  loading?: boolean
}

export function Button({ variant = 'primary', loading, disabled, children, className = '', ...rest }: ButtonProps) {
  return (
    <button
      disabled={disabled || loading}
      // `aria-busy` + a still-rendered label: a screen reader announces the pending state, and
      // sighted users keep the button's width instead of it collapsing to a bare spinner.
      aria-busy={loading || undefined}
      className={`inline-flex items-center justify-center gap-2 rounded-shell-sm px-4 py-2 text-sm font-semibold transition-all select-none active:translate-y-px disabled:cursor-not-allowed disabled:opacity-60 disabled:active:translate-y-0 ${variants[variant]} ${className}`}
      {...rest}
    >
      {loading && (
        <span className="size-4 shrink-0 animate-spin rounded-full border-2 border-current border-t-transparent" aria-hidden />
      )}
      {children}
    </button>
  )
}
