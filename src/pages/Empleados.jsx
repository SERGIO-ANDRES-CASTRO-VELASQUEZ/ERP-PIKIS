import { useState } from 'react'
import toast from 'react-hot-toast'
import { UserPlus, FileText } from 'lucide-react'
import * as us from '../services/usuariosService'
import { getTurnos, getTurno } from '../services/cajaService'
import { getVentas, abrirTicketPdf } from '../services/ventasService'
import { errMsg } from '../services/api'
import { money, fechaHora, hace, dia } from '../utils/formatters'
import { Modal, Field, inputCls, btnPrimary, btnGhost, Badge, Tabs, Table, Kpi, Spinner, ErrorBox, PageHeader, useFetch, useAccion } from '../components/common/ui'

function UsuarioForm({ onClose, onDone }) {
  const [f, setF] = useState({ nombre: '', username: '', password: '', rol: 'Cajero' })
  const { busy, ejecutar } = useAccion()
  const set = (k) => (e) => setF({ ...f, [k]: e.target.value })
  return (
    <Modal title="Nuevo usuario" onClose={onClose}>
      <form className="space-y-3" onSubmit={(e) => { e.preventDefault(); ejecutar(async () => { await us.crearUsuario({ ...f, username: f.username.trim() }); onDone(); onClose() }, 'Usuario creado') }}>
        <Field label="Nombre completo"><input required maxLength={120} className={inputCls} value={f.nombre} onChange={set('nombre')} /></Field>
        <Field label="Usuario (mín. 3 caracteres)"><input required minLength={3} maxLength={50} className={inputCls} value={f.username} onChange={set('username')} /></Field>
        <Field label="Contraseña (mín. 6 caracteres)"><input required type="password" minLength={6} className={inputCls} value={f.password} onChange={set('password')} autoComplete="new-password" /></Field>
        <Field label="Rol"><select className={inputCls} value={f.rol} onChange={set('rol')}><option value="Cajero">Cajero (POS, caja, clientes)</option><option value="Administrador">Administrador (acceso total)</option></select></Field>
        <div className="flex justify-end gap-2"><button type="button" className={btnGhost} onClick={onClose}>Cancelar</button><button className={btnPrimary} disabled={busy}>Crear</button></div>
      </form>
    </Modal>
  )
}

function TabUsuarios() {
  const { data, loading, error, reload } = useFetch(us.getUsuarios, [])
  const [nuevo, setNuevo] = useState(false)
  const toggle = (u) => (u.activo ? us.desactivarUsuario(u.id) : us.activarUsuario(u.id)).then(reload).catch((e) => toast.error(errMsg(e)))
  return (
    <>
      <div className="mb-3 flex justify-end"><button className={btnPrimary} onClick={() => setNuevo(true)}><UserPlus size={16} /> Nuevo usuario</button></div>
      {error ? <ErrorBox msg={error} onRetry={reload} /> : loading && !data ? <Spinner /> : (
        <Table head={['Nombre', 'Usuario', 'Rol', 'Estado', '']}>
          {(data ?? []).map((u) => (
            <tr key={u.id}>
              <td className="px-4 py-2.5 font-medium">{u.nombre}</td><td className="px-4 py-2.5">{u.username}</td>
              <td className="px-4 py-2.5"><Badge tone={u.rol === 'Administrador' ? 'blue' : 'gray'}>{u.rol}</Badge></td>
              <td className="px-4 py-2.5"><Badge tone={u.activo ? 'green' : 'red'}>{u.activo ? 'Activo' : 'Inactivo'}</Badge></td>
              <td className="px-4 py-2.5 text-right"><button className={btnGhost} onClick={() => toggle(u)}>{u.activo ? 'Desactivar' : 'Activar'}</button></td>
            </tr>
          ))}
        </Table>
      )}
      {nuevo && <UsuarioForm onClose={() => setNuevo(false)} onDone={reload} />}
    </>
  )
}

function Rango({ desde, hasta, setDesde, setHasta }) {
  return (
    <div className="mb-3 flex gap-2">
      <input type="date" className={inputCls + ' !w-auto'} value={desde} onChange={(e) => setDesde(e.target.value)} />
      <input type="date" className={inputCls + ' !w-auto'} value={hasta} onChange={(e) => setHasta(e.target.value)} />
    </div>
  )
}

// Turnos = aperturas/cierres de caja de cada cajero (asistencia + cuadre)
function TabTurnos() {
  const [desde, setDesde] = useState(hace(7)); const [hasta, setHasta] = useState(dia())
  const { data, loading, error } = useFetch(() => getTurnos(desde, hasta), [desde, hasta])
  const [det, setDet] = useState(null)
  return (
    <>
      <Rango {...{ desde, hasta, setDesde, setHasta }} />
      {error ? <ErrorBox msg={error} /> : loading ? <Spinner /> : (
        <Table head={['Cajero', 'Entrada (apertura)', 'Salida (cierre)', 'Esperado', 'Contado', 'Diferencia', '']} empty={data.length ? null : 'Sin turnos en el rango.'}>
          {data.map((t) => (
            <tr key={t.id}>
              <td className="px-4 py-2.5 font-medium">{t.cajero}</td><td className="px-4 py-2.5">{fechaHora(t.fechaApertura)}</td>
              <td className="px-4 py-2.5">{t.fechaCierre ? fechaHora(t.fechaCierre) : <Badge tone="green">En turno</Badge>}</td>
              <td className="px-4 py-2.5">{t.montoEsperado != null ? money(t.montoEsperado) : '—'}</td><td className="px-4 py-2.5">{t.montoContado != null ? money(t.montoContado) : '—'}</td>
              <td className="px-4 py-2.5">{t.diferencia == null ? '—' : <Badge tone={t.diferencia === 0 ? 'green' : 'red'}>{t.diferencia === 0 ? 'Cuadra' : (t.diferencia > 0 ? 'Sobrante ' : 'Faltante ') + money(Math.abs(t.diferencia))}</Badge>}</td>
              <td className="px-4 py-2.5 text-right"><button className={btnGhost} onClick={() => getTurno(t.id).then(setDet).catch((e) => toast.error(errMsg(e)))}>Detalle</button></td>
            </tr>
          ))}
        </Table>
      )}
      {det && (
        <Modal wide title={`Turno de ${det.cajero}`} onClose={() => setDet(null)}>
          <div className="mb-3 grid grid-cols-2 gap-3 sm:grid-cols-4">
            <Kpi label="Efectivo" value={money(det.ventasEfectivo)} /><Kpi label="Nequi" value={money(det.ventasNequi)} /><Kpi label="Daviplata" value={money(det.ventasDaviplata)} /><Kpi label="Fiado" value={money(det.ventasCredito)} />
          </div>
          <Table head={['Hora', 'Tipo', 'Monto', 'Descripción']}>
            {det.movimientos.map((m) => <tr key={m.id}><td className="px-4 py-2">{fechaHora(m.fecha)}</td><td className="px-4 py-2">{m.tipo}</td><td className="px-4 py-2">{money(m.monto)}</td><td className="px-4 py-2">{m.descripcion}</td></tr>)}
          </Table>
        </Modal>
      )}
    </>
  )
}

// Auditoría interna: ventas agrupadas por cajero (el backend ya devuelve todas las ventas al administrador)
function TabVentasCajero() {
  const [desde, setDesde] = useState(dia()); const [hasta, setHasta] = useState(dia())
  const { data, loading, error } = useFetch(() => getVentas({ desde, hasta }), [desde, hasta])
  const [sel, setSel] = useState(null)
  const grupos = Object.values((data ?? []).reduce((acc, v) => {
    const g = (acc[v.cajero] ??= { cajero: v.cajero, n: 0, total: 0, Efectivo: 0, Nequi: 0, Daviplata: 0, Credito: 0, ventas: [] })
    g.n++; g.total += v.total; g[v.tipoPago] = (g[v.tipoPago] ?? 0) + v.total; g.ventas.push(v); return acc
  }, {})).sort((a, b) => b.total - a.total)
  const total = grupos.reduce((s, g) => s + g.total, 0)
  return (
    <>
      <Rango {...{ desde, hasta, setDesde, setHasta }} />
      {error ? <ErrorBox msg={error} /> : loading ? <Spinner /> : (
        <>
          <div className="mb-4 grid grid-cols-2 gap-3 sm:max-w-md"><Kpi label="Ventas del período" value={money(total)} /><Kpi label="Transacciones" value={data.length} /></div>
          <Table head={['Cajero', 'Ventas', 'Total', 'Ticket prom.', 'Efectivo', 'Nequi', 'Daviplata', 'Fiado', '']} empty={grupos.length ? null : 'Sin ventas en el rango.'}>
            {grupos.map((g) => (
              <tr key={g.cajero}>
                <td className="px-4 py-2.5 font-medium">{g.cajero}</td><td className="px-4 py-2.5">{g.n}</td><td className="px-4 py-2.5 font-semibold">{money(g.total)}</td><td className="px-4 py-2.5">{money(g.total / g.n)}</td>
                <td className="px-4 py-2.5">{money(g.Efectivo)}</td><td className="px-4 py-2.5">{money(g.Nequi)}</td><td className="px-4 py-2.5">{money(g.Daviplata)}</td><td className="px-4 py-2.5">{money(g.Credito)}</td>
                <td className="px-4 py-2.5 text-right"><button className={btnGhost} onClick={() => setSel(g)}>Ver ventas</button></td>
              </tr>
            ))}
          </Table>
        </>
      )}
      {sel && (
        <Modal wide title={`Ventas de ${sel.cajero}`} onClose={() => setSel(null)}>
          <Table head={['#', 'Fecha', 'Pago', 'Cliente', 'Total', 'Tiquete']}>
            {sel.ventas.map((v) => (
              <tr key={v.id}><td className="px-4 py-2">{v.id}</td><td className="px-4 py-2">{fechaHora(v.fecha)}</td><td className="px-4 py-2">{v.tipoPago}</td><td className="px-4 py-2">{v.cliente ?? '—'}</td><td className="px-4 py-2">{money(v.total)}</td>
                <td className="px-4 py-2"><button className={btnGhost} onClick={() => abrirTicketPdf(v.id).catch((e) => toast.error(errMsg(e)))}><FileText size={14} /></button></td></tr>
            ))}
          </Table>
        </Modal>
      )}
    </>
  )
}

export default function Empleados() {
  const [tab, setTab] = useState('usuarios')
  return (
    <div>
      <PageHeader title="Empleados" subtitle="Usuarios, turnos de caja (asistencia) y ventas por cajero." />
      <Tabs value={tab} onChange={setTab} tabs={[{ id: 'usuarios', label: 'Usuarios' }, { id: 'turnos', label: 'Turnos y asistencia' }, { id: 'ventas', label: 'Ventas por cajero' }]} />
      {tab === 'usuarios' && <TabUsuarios />}
      {tab === 'turnos' && <TabTurnos />}
      {tab === 'ventas' && <TabVentasCajero />}
    </div>
  )
}
