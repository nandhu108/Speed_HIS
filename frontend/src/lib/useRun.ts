import { useCallback } from 'react'
import { errorMessage } from '../api/axios'
import { useToast } from '../components/ui/Toast'

/**
 * Runs a mutation and surfaces a server rejection as an error toast.
 *
 * There is no global `MutationCache.onError` (see `lib/queryClient.ts`), so an unwrapped
 * `mutateAsync` in a click handler rejects into nothing: the row never changes and the user is
 * told nothing. This is the same shape as the local `run()` helpers OT's booking detail page
 * already uses, lifted so screens without their own error banner can share it.
 */
export function useRun() {
  const toast = useToast()
  return useCallback(
    async (fn: () => Promise<unknown>) => {
      try {
        await fn()
      } catch (err) {
        toast.error(errorMessage(err))
      }
    },
    [toast],
  )
}
