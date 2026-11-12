import { useEffect, useRef, type ReactNode } from 'react'
import { X } from 'lucide-react'
import { Button } from './Button'

/** Dialog widths. `lg` is the historical default every existing caller gets. */
type ModalSize = 'sm' | 'lg' | 'xl' | '2xl' | '4xl'

const sizes: Record<ModalSize, string> = {
  sm: 'max-w-md',
  lg: 'max-w-lg',
  xl: 'max-w-2xl',
  '2xl': 'max-w-4xl',
  '4xl': 'max-w-6xl',
}

interface ModalProps {
  open: boolean
  title: string
  onClose: () => void
  children: ReactNode
  footer?: ReactNode
  size?: ModalSize
}

export function Modal({ open, title, onClose, children, footer, size = 'lg' }: ModalProps) {
  const panelRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) return
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose()
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [open, onClose])

  // Lock the page behind the dialog: without this the wheel/trackpad scrolls the list
  // underneath while the dialog stays put, which reads as a frozen dialog.
  useEffect(() => {
    if (!open) return
    const previous = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    return () => {
      document.body.style.overflow = previous
    }
  }, [open])

  // Move focus into the dialog on open so keyboard users don't have to tab back through the
  // whole page behind it, and so Escape/Tab land on the dialog straight away. Prefers the
  // first real field over the close button — the caret should start where typing starts.
  useEffect(() => {
    if (!open) return
    const panel = panelRef.current
    if (!panel) return
    const field = panel.querySelector<HTMLElement>(
      'input:not([type="hidden"]):not([disabled]), select:not([disabled]), textarea:not([disabled])',
    )
    ;(field ?? panel).focus({ preventScroll: true })
  }, [open])

  if (!open) return null

  return (
    <div
      className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/50 p-4 backdrop-blur-sm sm:items-center"
      onMouseDown={(e) => e.target === e.currentTarget && onClose()}
      role="dialog"
      aria-modal="true"
      aria-label={title}
    >
      {/* max-h + an internally scrolling body: a tall form (registration, admission, GRN) used
          to run past the bottom of a laptop screen with its footer buttons unreachable, because
          the panel had no height cap and the overlay itself didn't scroll. Header and footer now
          stay pinned while only the body scrolls. */}
      <div
        ref={panelRef}
        tabIndex={-1}
        className={`my-auto flex max-h-[calc(100dvh-2rem)] w-full flex-col rounded-shell border border-border-soft bg-panel-solid shadow-shell-lg outline-none ${sizes[size]}`}
      >
        <div className="flex shrink-0 items-center justify-between gap-3 border-b border-border-soft px-5 py-3.5">
          <h2 className="font-display text-base font-semibold text-ink">{title}</h2>
          <Button variant="ghost" aria-label="Close" onClick={onClose} className="!p-1.5">
            <X className="size-4" aria-hidden />
          </Button>
        </div>
        <div className="min-h-0 flex-1 overflow-y-auto px-5 py-4">{children}</div>
        {footer && (
          <div className="flex shrink-0 flex-wrap justify-end gap-2 border-t border-border-soft px-5 py-3">{footer}</div>
        )}
      </div>
    </div>
  )
}
