import { useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { errorMessage } from '../../api/axios'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { usePortalAuth } from '../features/auth/PortalAuthContext'

export function PortalLoginPage() {
  const { login } = usePortalAuth()
  const navigate = useNavigate()
  const location = useLocation() as { state?: { from?: string } }
  const [uhidOrMobile, setUhidOrMobile] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  const submit = async () => {
    setError(null)
    setLoading(true)
    try {
      await login({ uhidOrMobile, password })
      navigate(location.state?.from ?? '/portal', { replace: true })
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50">
      <div className="w-full max-w-sm rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
        <h1 className="mb-1 text-xl font-semibold text-slate-800">Patient Portal</h1>
        <p className="mb-4 text-sm text-slate-500">Log in with your UHID or mobile number</p>
        {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <div className="space-y-3">
          <Input label="UHID or mobile number" value={uhidOrMobile} onChange={(e) => setUhidOrMobile(e.target.value)} />
          <Input label="Password" type="password" value={password} onChange={(e) => setPassword(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && submit()} />
          <Button className="w-full" onClick={submit} loading={loading} disabled={!uhidOrMobile || !password}>Log in</Button>
        </div>
        <p className="mt-4 text-center text-sm text-slate-500">
          New here? <Link to="/portal/register" className="text-primary-600 hover:underline">Activate your portal account</Link>
        </p>
      </div>
    </div>
  )
}
