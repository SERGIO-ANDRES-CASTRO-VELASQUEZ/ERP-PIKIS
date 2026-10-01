import { useState } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'
import { errMsg } from '../services/api'
import { Field, inputCls, btnPrimary } from '../components/common/ui'

export default function Login() {
  const { user, token, login } = useAuthStore()
  const nav = useNavigate()
  const [f, setF] = useState({ username: '', password: '' })
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  if (token && user) return <Navigate to={user.rol === 'Administrador' ? '/dashboard' : '/pos'} replace />

  const submit = async (e) => {
    e.preventDefault(); setError(''); setBusy(true)
    try {
      const u = await login(f.username.trim(), f.password)
      nav(u.rol === 'Administrador' ? '/dashboard' : '/pos', { replace: true })
    } catch (err) { setError(errMsg(err)) } finally { setBusy(false) }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-pikis-dark p-4">
      <form onSubmit={submit} className="w-full max-w-sm space-y-4 rounded-2xl bg-white p-7 shadow-xl">
        <div className="text-center">
          <h1 className="text-2xl font-extrabold text-pikis-blue">ERP PIKIS</h1>
          <p className="text-sm text-gray-500">Inicia sesión para continuar</p>
        </div>
        <Field label="Usuario"><input autoFocus required className={inputCls} value={f.username} onChange={(e) => setF({ ...f, username: e.target.value })} autoComplete="username" /></Field>
        <Field label="Contraseña"><input required type="password" className={inputCls} value={f.password} onChange={(e) => setF({ ...f, password: e.target.value })} autoComplete="current-password" /></Field>
        {error && <p className="rounded-lg bg-red-50 p-2 text-sm text-red-700">{error}</p>}
        <button className={btnPrimary + ' w-full'} disabled={busy}>{busy ? 'Ingresando…' : 'Ingresar'}</button>
      </form>
    </div>
  )
}
