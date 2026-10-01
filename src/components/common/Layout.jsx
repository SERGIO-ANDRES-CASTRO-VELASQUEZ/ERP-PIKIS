import { useEffect, useState } from 'react'
import { NavLink, Outlet, useNavigate, Link } from 'react-router-dom'
import { LayoutDashboard, ShoppingCart, Wallet, Boxes, PackageMinus, Truck, Users, UserCog, Bell, LogOut, Menu, KeyRound } from 'lucide-react'
import clsx from 'clsx'
import toast from 'react-hot-toast'
import { useAuthStore } from '../../store/authStore'
import { useCajaStore } from '../../store/cajaStore'
import { getResumen } from '../../services/alertasService'
import { cambiarPassword } from '../../services/authService'
import { Modal, Field, inputCls, btnPrimary, useAccion } from './ui'

const NAV = [
  { to: '/dashboard',   label: 'Dashboard',   icon: LayoutDashboard, admin: true },
  { to: '/pos',         label: 'Punto de venta', icon: ShoppingCart },
  { to: '/caja',        label: 'Caja',        icon: Wallet },
  { to: '/inventario',  label: 'Inventario',  icon: Boxes, admin: true },
  { to: '/mermas',      label: 'Mermas',      icon: PackageMinus, admin: true },
  { to: '/proveedores', label: 'Proveedores', icon: Truck, admin: true },
  { to: '/clientes',    label: 'Clientes',    icon: Users },
  { to: '/empleados',   label: 'Empleados',   icon: UserCog, admin: true },
  { to: '/alertas',     label: 'Alertas',     icon: Bell, admin: true },
]

function Campana() {
  const [r, setR] = useState(null)
  useEffect(() => {
    let vivo = true
    const cargar = () => getResumen().then((x) => vivo && setR(x)).catch(() => {})
    cargar()
    const t = setInterval(cargar, 45000) // el backend no tiene websockets: polling
    return () => { vivo = false; clearInterval(t) }
  }, [])
  return (
    <Link to="/alertas" className="relative rounded-lg p-2 text-gray-600 hover:bg-gray-100" aria-label="Alertas">
      <Bell size={20} />
      {r?.noLeidas > 0 && (
        <span className={clsx('absolute -right-0.5 -top-0.5 min-w-[18px] rounded-full px-1 text-center text-[11px] font-bold text-white', r.criticas > 0 ? 'bg-pikis-red' : 'bg-pikis-yellow')}>
          {r.noLeidas}
        </span>
      )}
    </Link>
  )
}

function CambiarPassword({ onClose }) {
  const [f, setF] = useState({ actual: '', nueva: '', repetir: '' })
  const { busy, ejecutar } = useAccion()
  const submit = (e) => {
    e.preventDefault()
    if (f.nueva !== f.repetir) return toast.error('Las contraseñas nuevas no coinciden')
    ejecutar(async () => { await cambiarPassword(f.actual, f.nueva); onClose() }, 'Contraseña actualizada')
  }
  return (
    <Modal title="Cambiar mi contraseña" onClose={onClose}>
      <form className="space-y-3" onSubmit={submit}>
        <Field label="Contraseña actual"><input required autoFocus type="password" className={inputCls} value={f.actual} onChange={(e) => setF({ ...f, actual: e.target.value })} autoComplete="current-password" /></Field>
        <Field label="Nueva contraseña (mín. 6)"><input required type="password" minLength={6} maxLength={100} className={inputCls} value={f.nueva} onChange={(e) => setF({ ...f, nueva: e.target.value })} autoComplete="new-password" /></Field>
        <Field label="Repetir nueva contraseña"><input required type="password" minLength={6} className={inputCls} value={f.repetir} onChange={(e) => setF({ ...f, repetir: e.target.value })} autoComplete="new-password" /></Field>
        <div className="flex justify-end"><button className={btnPrimary} disabled={busy}>Guardar</button></div>
      </form>
    </Modal>
  )
}

export default function Layout() {
  const { user, logout, refrescarPerfil } = useAuthStore()
  const resetCaja = useCajaStore((s) => s.reset)
  const nav = useNavigate()
  const [abierto, setAbierto] = useState(false)
  const [pwd, setPwd] = useState(false)
  useEffect(() => { refrescarPerfil().catch(() => {}) }, []) // eslint-disable-line
  const admin = user.rol === 'Administrador'
  const items = NAV.filter((n) => !n.admin || admin)
  const salir = () => { resetCaja(); logout(); nav('/login') }

  return (
    <div className="flex min-h-screen">
      <aside className={clsx('fixed inset-y-0 left-0 z-40 w-60 bg-pikis-sidebar text-gray-300 transition-transform md:static md:translate-x-0', abierto ? 'translate-x-0' : '-translate-x-full')}>
        <div className="px-5 py-5 text-lg font-bold text-white">ERP PIKIS</div>
        <nav className="space-y-1 px-3">
          {items.map(({ to, label, icon: Icon }) => (
            <NavLink key={to} to={to} onClick={() => setAbierto(false)}
              className={({ isActive }) => clsx('flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium', isActive ? 'bg-pikis-blue text-white' : 'hover:bg-white/10')}>
              <Icon size={18} />{label}
            </NavLink>
          ))}
        </nav>
      </aside>
      {abierto && <div className="fixed inset-0 z-30 bg-black/40 md:hidden" onClick={() => setAbierto(false)} />}

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex items-center justify-between border-b bg-white px-4 py-2.5">
          <button className="rounded p-2 hover:bg-gray-100 md:hidden" onClick={() => setAbierto(true)} aria-label="Menú"><Menu size={20} /></button>
          <div className="hidden md:block" />
          <div className="flex items-center gap-3">
            {admin && <Campana />}
            <div className="text-right text-sm leading-tight">
              <p className="font-medium">{user.nombre}</p>
              <p className="text-xs text-gray-500">{user.rol}</p>
            </div>
            <button onClick={() => setPwd(true)} title="Cambiar mi contraseña" className="rounded-lg p-2 text-gray-600 hover:bg-gray-100" aria-label="Cambiar contraseña"><KeyRound size={18} /></button>
            <button onClick={salir} className="rounded-lg p-2 text-gray-600 hover:bg-gray-100" aria-label="Cerrar sesión"><LogOut size={18} /></button>
          </div>
        </header>
        <main className="min-w-0 flex-1 p-4 md:p-6"><Outlet /></main>
      </div>
      {pwd && <CambiarPassword onClose={() => setPwd(false)} />}
    </div>
  )
}
