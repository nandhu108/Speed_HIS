import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { errorMessage } from '../../api/axios'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { portalRegister } from '../features/auth/api'

export function PortalRegisterPage() {
  const navigate = useNavigate()
  const [uhid, setUhid] = useState('')
  const [mobile, setMobile] = useState('')
  const [dateOfBirth, setDateOfBirth] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)
  const [loading, setLoading] = useState(false)

  const submit = async () => {
    setError(null)
    setLoading(true)
    try {
      await portalRegister({ uhid, mobile, dateOfBirth, password })
      setSuccess(true)
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setLoading(false)
    }
  }

  if (success) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50">
        <div className="w-full max-w-sm rounded-lg border border-slate-200 bg-white p-6 text-center shadow-sm">
          <p className="mb-4 text-sm text-slate-700">Portal account activated. You can now log in.</p>
          <Button className="w-full" onClick={() => navigate('/portal/login')}>Go to login</Button>
        </div>
      </div>
    )
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50">
      <div className="w-full max-w-sm rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
        <h1 className="mb-1 text-xl font-semibold text-slate-800">Activate Portal Access</h1>
        <p className="mb-4 text-sm text-slate-500">Match your details exactly as registered at the hospital</p>
        {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <div className="space-y-3">
          <Input label="UHID" value={uhid} onChange={(e) => setUhid(e.target.value)} />
          <Input label="Mobile number" value={mobile} onChange={(e) => setMobile(e.target.value)} />
          <Input label="Date of birth" type="date" value={dateOfBirth} onChange={(e) => setDateOfBirth(e.target.value)} />
          <Input label="Choose a password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} />
          <Button className="w-full" onClick={submit} loading={loading} disabled={!uhid || !mobile || !dateOfBirth || !password}>
            Activate account
          </Button>
        </div>
        <p className="mt-4 text-center text-sm text-slate-500">
          Already activated? <Link to="/portal/login" className="text-primary-600 hover:underline">Log in</Link>
        </p>
      </div>
    </div>
  )
}
