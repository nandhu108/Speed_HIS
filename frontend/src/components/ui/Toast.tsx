import { createContext, useCallback, useContext, useEffect, useRef, useState, type ReactNode } from 'react'
import { CheckCircle2, X, XCircle } from 'lucide-react'

type ToastTone = 'success' | 'error'
interface ToastItem {
  id: number
  tone: ToastTone
  message: string
}

interface ToastContextValue {
  success: (message: string) => void
  error: (message: string) => void
}

const ToastContext = createContext<ToastContextValue | null>(null)

const toneStyles: Record<ToastTone, string> = {
  success: 'border-ok/40 text-ok',
  error: 'border-crit/40 text-crit',
}

/** Errors are usually a validation message or a server reason the user has to act on, so they
 * stay up long enough to actually read; successes are just confirmation and clear quickly. */
const toneDuration: Record<ToastTone, number> = {
  success: 3500,
  error: 8000,
}

let nextId = 1

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([])
  const timers = useRef(new Map<number, ReturnType<typeof setTimeout>>())

  const dismiss = useCallback((id: number) => {
    const timer = timers.current.get(id)
    if (timer) {
      clearTimeout(timer)
      timers.current.delete(id)
    }
    setToasts((prev) => prev.filter((t) => t.id !== id))
  }, [])

  const push = useCallback(
    (tone: ToastTone, message: string) => {
      const id = nextId++
      // Cap the stack: a failing bulk action could otherwise fire a dozen toasts and bury the
      // screen. Newest wins, oldest drops off.
      setToasts((prev) => [...prev, { id, tone, message }].slice(-4))
      timers.current.set(
        id,
        setTimeout(() => dismiss(id), toneDuration[tone]),
      )
    },
    [dismiss],
  )

  useEffect(() => {
    const pending = timers.current
    return () => {
      pending.forEach(clearTimeout)
      pending.clear()
    }
  }, [])

  const value: ToastContextValue = {
    success: (message) => push('success', message),
    error: (message) => push('error', message),
  }

  return (
    <ToastContext.Provider value={value}>
      {children}
      {/* Bottom-right rather than top-center: at top-center a toast sat directly over the page
          header and tab strip, hiding the very controls the user had just clicked. */}
      <div className="pointer-events-none fixed right-4 bottom-4 z-[100] flex w-full max-w-sm flex-col items-end gap-2">
        {toasts.map((t) => (
          <div
            key={t.id}
            role={t.tone === 'error' ? 'alert' : 'status'}
            className={`pointer-events-auto flex w-full items-start gap-2 rounded-shell-sm border bg-panel px-4 py-2.5 text-sm font-semibold shadow-shell-lg ${toneStyles[t.tone]}`}
          >
            {t.tone === 'success' ? (
              <CheckCircle2 className="mt-0.5 size-4 shrink-0" aria-hidden />
            ) : (
              <XCircle className="mt-0.5 size-4 shrink-0" aria-hidden />
            )}
            {/* Wraps instead of clipping — server errors are often a full sentence. */}
            <span className="min-w-0 flex-1 break-words text-ink">{t.message}</span>
            <button
              type="button"
              onClick={() => dismiss(t.id)}
              aria-label="Dismiss notification"
              className="-mr-1 shrink-0 rounded p-0.5 text-ink-faint hover:bg-panel-2 hover:text-ink"
            >
              <X className="size-3.5" aria-hidden />
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}

/** Fire-and-forget success/error toasts — throws if used outside `ToastProvider` (mounted once at the app root). */
export function useToast(): ToastContextValue {
  const ctx = useContext(ToastContext)
  if (!ctx) throw new Error('useToast must be used within a ToastProvider')
  return ctx
}
