import { useEffect, useState } from 'react'
import toast from 'react-hot-toast'
import { Wallet, ArrowDownCircle, Lock } from 'lucide-react'
import { useCajaStore } from '../store/cajaStore'
import { useAuthStore } from '../store/authStore'
import { getTurnos, getTurno } from '../services/cajaService'
import { errMsg } from '../services/api'
import { money, fechaHora, hace, dia } from '../utils/formatters'
import { Modal, Field, inputCls, btnPrimary, btnGhost, btnDanger, Badge, Tabs, Table, Kpi, Spinner, PageHeader, useFetch, useAccion } from '../components/common/ui'

function DetalleTurno({ t }) {
  return (
    <>
      <div className="mb-4 grid grid-cols-2 gap-3 sm:grid-cols-4">
        <Kpi label="Base inicial" value={money(t.montoInicial)} />
        <Kpi label="Ventas efectivo" value={money(t.ventasEfectivo)} />
        <Kpi label="Ventas Nequi" value={money(t.ventasNequi)} />
        <Kpi label="Ventas Daviplata" value={money(t.ventasDaviplata)} />
        <Kpi label="Ventas fiado" value={money(t.ventasCredito)} />
        <Kpi label="Ingresos (abonos)" value={money(t.ingresos)} />
        <Kpi label="Egresos (gastos)" value={money(t.egresos)} />
        <Kpi label="Efectivo esperado" value={money(t.efectivoEsperado)} tone="text-pikis-blue" />
      </div>
      {t.diferencia != null && (
        <p className="mb-4 text-sm">Contado: <b>{money(t.montoContado)}</b> — <Badge tone={t.diferencia === 0 ? 'green' : 'red'}>{t.diferencia === 0 ? 'Cuadra' : (t.diferencia > 0 ? 'Sobrante ' : 'Faltante ') + money(Math.abs(t.diferencia))}</Badge></p>
      )}
      <Table head={['Hora', 'Tipo', 'Monto', 'Descripción']} empty={t.movimientos.length ? null : 'Sin movimientos.'}>
        {t.movimientos.map((m) => (
          <tr key={m.id}><td className="px-4 py-2">{fechaHora(m.fecha)}</td><td className="px-4 py-2">{m.tipo}</td><td className="px-4 py-2">{money(m.monto)}</td><td className="px-4 py-2">{m.descripcion}{m.cliente ? ` — ${m.cliente}` : ''}</td></tr>
        ))}
      </Table>
    </>
  )
}

function MiCaja() {
  const { turno, cargado, refrescar, abrir, egreso, cerrar } = useCajaStore()
  const { busy, ejecutar } = useAccion()
  const [modal, setModal] = useState(null) // 'gasto' | 'cierre'
  const [f, setF] = useState({ monto: '', desc: '' })
  const [cierre, setCierre] = useState(null)

  useEffect(() => { refrescar().catch((e) => toast.error(errMsg(e))) }, []) // eslint-disable-line
  if (!cargado) return <Spinner />

  if (!turno) return (
    <div className="mx-auto max-w-sm space-y-3 rounded-2xl border bg-white p-6 text-center">
      {cierre && (
        <div className="rounded-lg bg-gray-50 p-3 text-sm">
          Último cierre: esperado {money(cierre.efectivoEsperado)}, contado {money(cierre.montoContado)} →{' '}
          <Badge tone={cierre.diferencia === 0 ? 'green' : 'red'}>{cierre.diferencia === 0 ? 'Cuadra' : (cierre.diferencia > 0 ? 'Sobrante ' : 'Faltante ') + money(Math.abs(cierre.diferencia))}</Badge>
        </div>
      )}
      <Wallet className="mx-auto text-pikis-blue" size={34} />
      <p className="font-medium">No tienes caja abierta</p>
      <Field label="Efectivo base"><input type="number" min="0" className={inputCls} value={f.monto} onChange={(e) => setF({ ...f, monto: e.target.value })} /></Field>
      <button className={btnPrimary + ' w-full'} disabled={busy} onClick={() => ejecutar(async () => { await abrir(f.monto || 0); setCierre(null) }, 'Caja abierta')}>Abrir caja</button>
    </div>
  )

  return (
    <>
      <div className="mb-4 flex flex-wrap items-center justify-between gap-2">
        <p className="text-sm text-gray-500">Abierta desde {fechaHora(turno.fechaApertura)} · {turno.cajero}</p>
        <div className="flex gap-2">
          <button className={btnGhost} onClick={() => { setF({ monto: '', desc: '' }); setModal('gasto') }}><ArrowDownCircle size={16} /> Registrar gasto</button>
          <button className={btnDanger} onClick={() => { setF({ monto: '', desc: '' }); setModal('cierre') }}><Lock size={16} /> Cerrar caja</button>
        </div>
      </div>
      <DetalleTurno t={turno} />
      {modal === 'gasto' && (
        <Modal title="Registrar gasto" onClose={() => setModal(null)}>
          <form className="space-y-3" onSubmit={(e) => { e.preventDefault(); ejecutar(async () => { await egreso(f.monto, f.desc); setModal(null) }, 'Gasto registrado') }}>
            <Field label="Monto"><input required autoFocus type="number" min="1" className={inputCls} value={f.monto} onChange={(e) => setF({ ...f, monto: e.target.value })} /></Field>
            <Field label="Descripción"><input required maxLength={250} className={inputCls} value={f.desc} onChange={(e) => setF({ ...f, desc: e.target.value })} /></Field>
            <div className="flex justify-end"><button className={btnPrimary} disabled={busy}>Guardar</button></div>
          </form>
        </Modal>
      )}
      {modal === 'cierre' && (
        <Modal title="Cerrar caja" onClose={() => setModal(null)}>
          <form className="space-y-3" onSubmit={(e) => { e.preventDefault(); ejecutar(async () => { setCierre(await cerrar(f.monto)); setModal(null) }, 'Caja cerrada') }}>
            <p className="text-sm text-gray-600">Efectivo esperado: <b>{money(turno.efectivoEsperado)}</b></p>
            <Field label="Efectivo contado"><input required autoFocus type="number" min="0" className={inputCls} value={f.monto} onChange={(e) => setF({ ...f, monto: e.target.value })} /></Field>
            <div className="flex justify-end"><button className={btnDanger} disabled={busy}>Confirmar cierre</button></div>
          </form>
        </Modal>
      )}
    </>
  )
}

function Historial() {
  const [desde, setDesde] = useState(hace(30))
  const [hasta, setHasta] = useState(dia())
  const { data, loading, error } = useFetch(() => getTurnos(desde, hasta), [desde, hasta])
  const [det, setDet] = useState(null)
  const abrir = (id) => getTurno(id).then(setDet).catch((e) => toast.error(errMsg(e)))
  return (
    <>
      <div className="mb-3 flex gap-2">
        <input type="date" className={inputCls + ' !w-auto'} value={desde} onChange={(e) => setDesde(e.target.value)} />
        <input type="date" className={inputCls + ' !w-auto'} value={hasta} onChange={(e) => setHasta(e.target.value)} />
      </div>
      {loading ? <Spinner /> : error ? <p className="text-red-600">{error}</p> : (
        <Table head={['Cajero', 'Apertura', 'Cierre', 'Base', 'Esperado', 'Contado', 'Diferencia', '']} empty={data?.length ? null : 'Sin turnos en el rango.'}>
          {data.map((t) => (
            <tr key={t.id}>
              <td className="px-4 py-2 font-medium">{t.cajero}</td><td className="px-4 py-2">{fechaHora(t.fechaApertura)}</td>
              <td className="px-4 py-2">{t.fechaCierre ? fechaHora(t.fechaCierre) : <Badge tone="green">Abierto</Badge>}</td>
              <td className="px-4 py-2">{money(t.montoInicial)}</td><td className="px-4 py-2">{t.montoEsperado != null ? money(t.montoEsperado) : '—'}</td>
              <td className="px-4 py-2">{t.montoContado != null ? money(t.montoContado) : '—'}</td>
              <td className="px-4 py-2">{t.diferencia == null ? '—' : <Badge tone={t.diferencia === 0 ? 'green' : 'red'}>{money(t.diferencia)}</Badge>}</td>
              <td className="px-4 py-2 text-right"><button className={btnGhost} onClick={() => abrir(t.id)}>Detalle</button></td>
            </tr>
          ))}
        </Table>
      )}
      {det && <Modal wide title={`Turno de ${det.cajero}`} onClose={() => setDet(null)}><DetalleTurno t={det} /></Modal>}
    </>
  )
}

export default function Caja() {
  const admin = useAuthStore((s) => s.user.rol === 'Administrador')
  const [tab, setTab] = useState('mia')
  return (
    <div>
      <PageHeader title="Caja" subtitle="Apertura, gastos y cierre de tu turno." />
      {admin && <Tabs value={tab} onChange={setTab} tabs={[{ id: 'mia', label: 'Mi caja' }, { id: 'hist', label: 'Historial de turnos' }]} />}
      {tab === 'mia' ? <MiCaja /> : <Historial />}
    </div>
  )
}
