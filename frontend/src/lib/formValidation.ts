/**
 * Client-side mirrors of the backend FluentValidation rules (see the matching
 * `*Validators.cs`). These exist so staff can see what is mandatory *before* they
 * submit a long form — the API is still the gate, per the plan's RBAC/validation
 * conventions. Keep a rule here in step with its server-side counterpart.
 */

/** Per-field messages, keyed by the form-state key the message belongs to. */
export type FieldErrors<K extends string = string> = Partial<Record<K, string>>

/** Mirrors `Matches(@"^[0-9+\-\s]{7,20}$")` on the patient/contact phone rules. */
export const MOBILE_PATTERN = /^[0-9+\-\s]{7,20}$/

/** Deliberately permissive — the server's `EmailAddress()` is the real check. */
export const EMAIL_PATTERN = /^[^@\s]+@[^@\s]+\.[^@\s]+$/

export function isBlank(value: string | null | undefined): boolean {
  return !value || value.trim().length === 0
}

/** Mirrors `NotEmpty()` on a string rule. */
export function requiredText(value: string | null | undefined, label: string): string | undefined {
  return isBlank(value) ? `${label} is required.` : undefined
}

/**
 * Mirrors `GreaterThan(0)` on the FK rules — our selects hold '' when nothing is
 * chosen, so a blank and a 0 are the same "not selected" case.
 */
export function requiredSelect(value: string | number | null | undefined, label: string): string | undefined {
  if (value === null || value === undefined || value === '') return `${label} is required.`
  return Number(value) > 0 ? undefined : `${label} is required.`
}

/** Mirrors `GreaterThan(0)` on a money/quantity rule. */
export function requiredPositiveAmount(value: string | number | null | undefined, label: string): string | undefined {
  if (value === null || value === undefined || value === '') return `${label} is required.`
  const parsed = Number(value)
  if (Number.isNaN(parsed)) return `Enter a valid ${label.toLowerCase()}.`
  return parsed > 0 ? undefined : `${label} must be greater than zero.`
}

export function mobileError(value: string | null | undefined, label = 'Mobile'): string | undefined {
  if (isBlank(value)) return `${label} is required.`
  return MOBILE_PATTERN.test(value!.trim()) ? undefined : 'Enter a valid mobile number.'
}

/** For the optional phone fields, which only validate their format when filled. */
export function optionalMobileError(value: string | null | undefined): string | undefined {
  if (isBlank(value)) return undefined
  return MOBILE_PATTERN.test(value!.trim()) ? undefined : 'Enter a valid mobile number.'
}

export function optionalEmailError(value: string | null | undefined): string | undefined {
  if (isBlank(value)) return undefined
  return EMAIL_PATTERN.test(value!.trim()) ? undefined : 'Enter a valid email address.'
}

/** Mirrors `MaximumLength(n)`, so an over-long value is caught before the round trip. */
export function maxLengthError(value: string | null | undefined, max: number, label: string): string | undefined {
  if (isBlank(value)) return undefined
  return value!.trim().length > max ? `${label} cannot exceed ${max} characters.` : undefined
}

export function hasErrors(errors: FieldErrors): boolean {
  return Object.values(errors).some(Boolean)
}

/** Drops the undefined entries so `hasErrors` and the field lookups stay honest. */
export function compactErrors<K extends string>(errors: FieldErrors<K>): FieldErrors<K> {
  const result: FieldErrors<K> = {}
  for (const [key, message] of Object.entries(errors) as [K, string | undefined][]) {
    if (message) result[key] = message
  }
  return result
}
