import { useState } from 'react'
import { errorMessage } from '../../api/axios'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { PageHeader } from '../../components/ui/PageHeader'
import { Spinner } from '../../components/ui/Spinner'
import { useMyFeedback, useSubmitFeedback } from '../features/feedback/api'

export function PortalFeedbackPage() {
  const { data: feedback = [], isLoading } = useMyFeedback()
  const submit = useSubmitFeedback()
  const [opVisitId, setOpVisitId] = useState('')
  const [rating, setRating] = useState(5)
  const [comments, setComments] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)

  const submitFeedback = async () => {
    setError(null)
    setMessage(null)
    try {
      await submit.mutateAsync({ opVisitId: opVisitId ? Number(opVisitId) : null, admissionId: null, rating, comments: comments || null })
      setMessage('Thank you for your feedback!')
      setOpVisitId('')
      setComments('')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader title="Feedback" subtitle="Rate your recent visit or admission" />
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      {message && <p className="mb-3 rounded-md bg-green-50 px-3 py-2 text-sm text-green-700">{message}</p>}

      <div className="mb-6 rounded-lg border border-slate-200 bg-white p-4">
        <div className="mb-3 grid grid-cols-2 gap-2">
          <Input label="Visit ID (from your appointment/receipt)" value={opVisitId} onChange={(e) => setOpVisitId(e.target.value)} />
          <label className="block">
            <span className="mb-1 block text-sm font-medium text-slate-700">Rating</span>
            <div className="flex gap-1">
              {[1, 2, 3, 4, 5].map((n) => (
                <button
                  key={n}
                  onClick={() => setRating(n)}
                  className={`h-9 w-9 rounded-md border text-sm ${rating >= n ? 'border-amber-400 bg-amber-50 text-amber-600' : 'border-slate-200 text-slate-400'}`}
                >
                  ★
                </button>
              ))}
            </div>
          </label>
        </div>
        <Input label="Comments (optional)" value={comments} onChange={(e) => setComments(e.target.value)} />
        <Button className="mt-3" onClick={submitFeedback} loading={submit.isPending} disabled={!opVisitId}>Submit feedback</Button>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <tbody>
            {feedback.map((f) => (
              <tr key={f.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-slate-700">{new Date(f.submittedAt).toLocaleDateString()}</td>
                <td className="px-4 py-2.5 text-amber-600">{'★'.repeat(f.rating)}</td>
                <td className="px-4 py-2.5 text-slate-500">{f.comments ?? ''}</td>
              </tr>
            ))}
            {!isLoading && feedback.length === 0 && <tr><td className="px-4 py-10 text-center text-slate-400">No feedback submitted yet.</td></tr>}
          </tbody>
        </table>
      </div>
    </>
  )
}
