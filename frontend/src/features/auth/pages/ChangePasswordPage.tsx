import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { changePasswordRequest } from '../api'
import { useAuth } from '../AuthContext'

const schema = z
  .object({
    currentPassword: z.string().min(1, 'Current password is required'),
    newPassword: z.string().min(1, 'New password is required'),
    confirmPassword: z.string().min(1, 'Confirm the new password'),
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    message: 'Passwords do not match',
    path: ['confirmPassword'],
  })
type FormValues = z.infer<typeof schema>

export function ChangePasswordPage() {
  const { user, refreshUser } = useAuth()
  const navigate = useNavigate()
  const [serverError, setServerError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  const forced = user?.mustChangePassword ?? false

  const onSubmit = handleSubmit(async (values) => {
    setServerError(null)
    try {
      await changePasswordRequest(values.currentPassword, values.newPassword)
      await refreshUser()
      navigate('/', { replace: true })
    } catch (error) {
      setServerError(errorMessage(error))
    }
  })

  return (
    <div className="mx-auto max-w-md">
      <form
        onSubmit={onSubmit}
        className="space-y-4 rounded-shell border border-border-soft bg-panel p-6 shadow-shell-sm"
      >
        <div>
          <h1 className="font-display text-base font-bold text-ink">Change password</h1>
          {forced && (
            <p className="mt-2 rounded-shell-sm bg-warn/10 px-3 py-2 text-sm text-warn">
              You must change your password before continuing.
            </p>
          )}
        </div>

        {serverError && (
          <p className="rounded-shell-sm bg-crit/10 px-3 py-2 text-sm text-crit" role="alert">
            {serverError}
          </p>
        )}

        <Input
          label="Current password"
          type="password"
          autoComplete="current-password"
          {...register('currentPassword')}
          error={errors.currentPassword?.message}
        />
        <Input
          label="New password"
          type="password"
          autoComplete="new-password"
          {...register('newPassword')}
          error={errors.newPassword?.message}
        />
        <Input
          label="Confirm new password"
          type="password"
          autoComplete="new-password"
          {...register('confirmPassword')}
          error={errors.confirmPassword?.message}
        />

        <Button type="submit" loading={isSubmitting} className="w-full justify-center">
          Change password
        </Button>
      </form>
    </div>
  )
}
