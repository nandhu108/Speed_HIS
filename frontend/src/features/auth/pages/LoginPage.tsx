import { zodResolver } from '@hookform/resolvers/zod'
import { HeartPulse, LockKeyhole, Moon, ShieldCheck, Sun, UserRound } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { outerNavigate, useOuterPath } from '../../../lib/outerHistory'
import { useTheme } from '../../../lib/ThemeContext'
import { useAuth } from '../AuthContext'

const schema = z.object({
  username: z.string().min(1, 'Username is required'),
  password: z.string().min(1, 'Password is required'),
})
type FormValues = z.infer<typeof schema>

/** The `?from=` deep-link param, if the user was bounced here from a protected route — null otherwise. */
function fromParam(outerPath: string): string | null {
  const query = new URLSearchParams(outerPath.split('?')[1] ?? '')
  return query.get('from')
}

export function LoginPage() {
  const { status, user, login } = useAuth()
  const { theme, toggleTheme } = useTheme()
  const outerPath = useOuterPath()
  const [serverError, setServerError] = useState<string | null>(null)

  useEffect(() => {
    // A specific deep link always wins; otherwise land on this user's configured default
    // page (role, then branch) once, right now — not on every later visit to "/".
    if (status === 'authenticated') {
      outerNavigate(fromParam(outerPath) ?? user?.defaultLandingPath ?? '/', { replace: true })
    }
  }, [status, outerPath, user])

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  const onSubmit = handleSubmit(async (values) => {
    setServerError(null)
    try {
      const loggedInUser = await login(values)
      outerNavigate(
        loggedInUser.mustChangePassword ? '/change-password' : (fromParam(outerPath) ?? loggedInUser.defaultLandingPath ?? '/'),
        { replace: true },
      )
    } catch (error) {
      setServerError(errorMessage(error))
    }
  })

  return (
    <div className="relative flex min-h-screen bg-shell-bg">
      <button
        type="button"
        onClick={toggleTheme}
        title="Toggle theme"
        className="absolute top-5 right-5 z-10 flex size-10 items-center justify-center rounded-shell-sm border border-border-soft bg-panel text-ink-muted hover:text-ink"
      >
        {theme === 'dark' ? <Sun className="size-4" aria-hidden /> : <Moon className="size-4" aria-hidden />}
      </button>

      <div
        className="relative hidden flex-1 flex-col justify-center overflow-hidden px-16 lg:flex"
        /* Navy-only ramp. The previous stop list ended on #0e5f6b (teal) — a leftover from the
           pre-2026-08 teal/indigo palette that clashed with the navy shell it now sits beside. */
        style={{ background: 'linear-gradient(150deg, #0b1c4a 0%, #16307e 55%, #1e40af 100%)' }}
      >
        <div
          className="pointer-events-none absolute inset-0"
          style={{ background: 'radial-gradient(ellipse at 22% 40%, rgb(255 255 255 / 0.07), transparent 55%)' }}
          aria-hidden
        />
        <div className="relative z-10 max-w-md">
          <div className="mb-11 flex items-center gap-3">
            <span className="flex size-11 items-center justify-center rounded-shell-sm border border-white/25 bg-white/15">
              <HeartPulse className="size-6 text-white" aria-hidden />
            </span>
            <div>
              <div className="font-display text-xl font-bold tracking-wide text-white">Prixo</div>
              <div className="text-[11px] tracking-[0.14em] text-white/55">HOSPITAL INFORMATION SYSTEM</div>
            </div>
          </div>
          <h1 className="font-display mb-5 text-[46px] leading-[1.05] font-bold tracking-tight text-white">
            Unified care,
            <br />
            {/* Was a teal→blue clipped-background gradient (#5eead4), the last visible trace of the
                old palette. A solid light blue gives the same emphasis against the white heading
                without reintroducing teal — and without the clipped-text fallback risk. */}
            <span className="text-[#93c5fd]">one platform</span>
          </h1>
          <p className="max-w-sm text-[15px] leading-relaxed text-white/65">
            Registration, OPD, IPD, diagnostics, pharmacy, billing and every clinical workflow your hospital runs on
            — in a single connected workspace.
          </p>
        </div>
      </div>

      <div className="flex flex-1 items-center justify-center px-6 py-12 lg:w-[480px] lg:flex-none">
        <div className="w-full max-w-sm rounded-shell border border-border-soft bg-panel p-8 shadow-shell-lg">
          <div className="mb-6 flex items-center gap-2.5 lg:hidden">
            <span className="flex size-9 items-center justify-center rounded-shell-sm bg-accent-2">
              <HeartPulse className="size-5 text-white" aria-hidden />
            </span>
            <span className="font-display text-lg font-bold text-ink">Prixo</span>
          </div>

          <h2 className="font-display text-2xl font-bold text-ink">Welcome back</h2>
          <p className="mt-1 mb-6 text-[13.5px] text-ink-muted">Sign in to your hospital dashboard</p>

          <form onSubmit={onSubmit} className="space-y-4">
            {serverError && (
              <p className="rounded-shell-sm bg-crit/10 px-3 py-2 text-sm text-crit" role="alert">
                {serverError}
              </p>
            )}

            <div>
              <label htmlFor="username" className="mb-1.5 block text-[10.5px] font-bold tracking-wider text-ink-faint uppercase">
                Username
              </label>
              <div className="relative">
                <UserRound className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-ink-faint" aria-hidden />
                <Input
                  id="username"
                  autoComplete="username"
                  autoFocus
                  inputClassName="pl-9"
                  {...register('username')}
                  error={errors.username?.message}
                />
              </div>
            </div>

            <div>
              <label htmlFor="password" className="mb-1.5 block text-[10.5px] font-bold tracking-wider text-ink-faint uppercase">
                Password
              </label>
              <div className="relative">
                <LockKeyhole className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-ink-faint" aria-hidden />
                <Input
                  id="password"
                  type="password"
                  autoComplete="current-password"
                  inputClassName="pl-9"
                  {...register('password')}
                  error={errors.password?.message}
                />
              </div>
            </div>

            <Button type="submit" loading={isSubmitting} className="w-full justify-center">
              Sign in
            </Button>
          </form>

          <p className="mt-6 flex items-center justify-center gap-1.5 text-[11px] text-ink-faint">
            <ShieldCheck className="size-3.5" aria-hidden />
            Role-based access · audit-logged sign-in
          </p>
        </div>
      </div>
    </div>
  )
}
