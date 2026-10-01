import { useState } from 'react'
import toast from 'react-hot-toast'
import { Plus, Wallet, Trash2 } from 'lucide-react'
import { useProveedores, estadoFactura } from '../store/proveedoresStore'
import { useAuthStore } from '../store/authStore'
import { useCajaStore } from '../store/cajaStore'
import { getProductos, registrarEntrada } from '../services/inventarioService'
import { registrarEgreso } from '../services/cajaService'
import { errMsg } from '../services/api'
import { money, fecha, dia } from '../utils/formatters'
import { Modal, Field, inputCls, btnPrimary, btnGhost, Badge, Tabs, Table, Kpi, PageHeader, useFetch, useAccion } from '../components/common/ui'

const TONOS = { pagada: 'green', parcial: 'yellow', pendiente: 'gray', vencida: 'red' }

function CompraForm({ proveedores, productos, onClose }) {
  const { crearFactura } = useProveedores()
  const { busy, ejecutar } = useAccion()
  const [f, setF] = useState({ proveedorId: proveedores[0]?.id ?? '', numero: '', fecha: dia(), vence: '', totalManual: '' })
  const [lineas, setLineas] = useState([{ productoId: '', cantidad: 1, costo: '' }])
  const setL = (i, patch) => setLineas(lineas.map((l, j) => (j === i ? { ...l, ...patch } : l)))
  const validas = lineas.filter((l) => l.productoId && Number(l.cantidad) > 0)
  const totalLineas = validas.reduce((s, l) => s + Number(l.cantidad) * Number(l.costo || 0), 0)
  const total = validas.length ? totalLineas : Number(f.totalManual || 0)
  const prov = proveedores.find((p) => p.id === f.proveedorId)

  const submit = (e) => {
    e.preventDefault()
    if (total <= 0) return toast.error('El total de la factura debe ser mayor a 0')
    ejecutar(async () => {
      const motivo = `Fact. ${f.numero} - ${prov.nombre}`.slice(0, 250)
      let ok = 0; let sumaOk = 0; const fallas = []
      for (const l of validas) { // entradas reales al inventario del backend
        try { await registrarEntrada(Number(l.productoId), { cantidad: Number(l.cantidad), costoUnitario: Number(l.costo || 0), motivo }); ok++; sumaOk += Number(l.cantidad) * Number(l.costo || 0) }
        catch (err) { fallas.push(`${productos.find((p) => String(p.id) === String(l.productoId))?.nombre}: ${errMsg(err)}`) }
      }
      if (validas.length && ok === 0) { toast.error(`No se pudo registrar el inventario: ${fallas.join(' | ')}`, { duration: 9000 }); return }
      crearFactura({ proveedorId: f.proveedorId, numero: f.numero, fecha: f.fecha, vence: f.vence || null, total: validas.length ? sumaOk : total })
      if (fallas.length) toast.error(`Algunas líneas no se registraron en inventario: ${fallas.join(' | ')}`, { duration: 9000 })
      onClose()
      toast.success('Compra registrada')
    })
  }

  return (
    <Modal wide title="Registrar compra / factura de proveedor" onClose={onClose}>
      <form onSubmit={submit} className="space-y-3">
        <div className="grid gap-3 sm:grid-cols-2">
          <Field label="Proveedor"><select required className={inputCls} value={f.proveedorId} onChange={(e) => setF({ ...f, proveedorId: e.target.value })}>{proveedores.map((p) => <option key={p.id} value={p.id}>{p.nombre}</option>)}</select></Field>
          <Field label="N° de factura"><input required className={inputCls} value={f.numero} onChange={(e) => setF({ ...f, numero: e.target.value })} /></Field>
          <Field label="Fecha"><input type="date" className={inputCls} value={f.fecha} onChange={(e) => setF({ ...f, fecha: e.target.value })} /></Field>
          <Field label="Vence"><input type="date" className={inputCls} value={f.vence} onChange={(e) => setF({ ...f, vence: e.target.value })} /></Field>
        </div>
        <p className="text-sm font-medium text-gray-700">Mercancía recibida <span className="font-normal text-gray-400">(suma al stock del inventario)</span></p>
        {lineas.map((l, i) => (
          <div key={i} className="grid grid-cols-12 items-center gap-2">
            <select className={inputCls + ' col-span-6'} value={l.productoId} onChange={(e) => { const p = productos.find((x) => String(x.id) === e.target.value); setL(i, { productoId: e.target.value, costo: p?.precioCompra ?? '' }) }}>
              <option value="">Producto…</option>{productos.map((p) => <option key={p.id} value={p.id}>{p.nombre}</option>)}
            </select>
            <input type="number" min="1" className={inputCls + ' col-span-2'} value={l.cantidad} onChange={(e) => setL(i, { cantidad: e.target.value })} aria-label="Cantidad" />
            <input type="number" min="0" className={inputCls + ' col-span-3'} value={l.costo} onChange={(e) => setL(i, { costo: e.target.value })} aria-label="Costo unitario" placeholder="Costo" />
            <button type="button" className="col-span-1 text-gray-400 hover:text-pikis-red" onClick={() => setLineas(lineas.filter((_, j) => j !== i))} aria-label="Quitar"><Trash2 size={16} /></button>
          </div>
        ))}
        <button type="button" className="text-sm text-pikis-blue underline" onClick={() => setLineas([...lineas, { productoId: '', cantidad: 1, costo: '' }])}>+ Agregar producto</button>
        {!validas.length && <Field label="Total de la factura (si no detallas productos)"><input type="number" min="1" className={inputCls} value={f.totalManual} onChange={(e) => setF({ ...f, totalManual: e.target.value })} /></Field>}
        <p className="text-right text-lg font-bold">Total: {money(total)}</p>
        <div className="flex justify-end gap-2"><button type="button" className={btnGhost} onClick={onClose}>Cancelar</button><button className={btnPrimary} disabled={busy}>Registrar</button></div>
      </form>
    </Modal>
  )
}

export default function Proveedores() {
  const { proveedores, facturas, pagos, crearProveedor, registrarPago } = useProveedores()
  const user = useAuthStore((s) => s.user)
  const refrescarCaja = useCajaStore((s) => s.refrescar)
  const productos = useFetch(() => getProductos(), [])
  const { busy, ejecutar } = useAccion()
  const [tab, setTab] = useState('proveedores')
  const [modal, setModal] = useState(null) // 'proveedor' | 'compra' | factura
  const [f, setF] = useState({})
  const set = (k) => (e) => setF({ ...f, [k]: e.target.value })
  const nombre = (id) => proveedores.find((p) => p.id === id)?.nombre ?? '—'
  const saldo = (pid) => facturas.filter((x) => x.proveedorId === pid).reduce((s, x) => s + x.total - x.pagado, 0)
  const porPagar = facturas.reduce((s, x) => s + x.total - x.pagado, 0)

  const submitSimple = (e) => {
    e.preventDefault()
    if (modal === 'proveedor') { crearProveedor(f); toast.success('Proveedor creado'); return setModal(null) }
    const x = modal; const monto = Number(f.monto); const pend = x.total - x.pagado
    if (monto <= 0 || monto > pend) return toast.error(`El monto debe estar entre 1 y ${money(pend)}`)
    ejecutar(async () => {
      // Pago en efectivo → sale de la caja del turno (egreso real en el backend)
      if (f.metodo === 'efectivo' && f.descontarCaja) { await registrarEgreso(monto, `Pago factura ${x.numero} - ${nombre(x.proveedorId)}`.slice(0, 250)); refrescarCaja().catch(() => {}) }
      registrarPago({ facturaId: x.id, monto, metodo: f.metodo, referencia: f.referencia, usuario: user?.nombre })
      setModal(null)
    }, 'Pago registrado')
  }

  return (
    <div>
      <PageHeader title="Proveedores" subtitle="Compras, cuentas por pagar y pagos.">
        <button className={btnGhost} onClick={() => { setF({}); setModal('proveedor') }}><Plus size={16} /> Proveedor</button>
        <button className={btnPrimary} disabled={!proveedores.length || !productos.data} onClick={() => setModal('compra')}><Plus size={16} /> Registrar compra</button>
      </PageHeader>
      <p className="mb-4 rounded-lg bg-yellow-50 p-3 text-xs text-yellow-800">
        Las <b>entradas de mercancía</b> sí se guardan en el servidor (suben el stock). Los <b>proveedores, facturas y pagos</b> se guardan solo en este navegador porque el backend aún no tiene ese módulo.
      </p>
      <div className="mb-5 grid gap-3 sm:grid-cols-3">
        <Kpi label="Por pagar" value={money(porPagar)} />
        <Kpi label="Facturas vencidas" value={facturas.filter((x) => estadoFactura(x) === 'vencida').length} />
        <Kpi label="Proveedores" value={proveedores.length} />
      </div>
      <Tabs value={tab} onChange={setTab} tabs={[{ id: 'proveedores', label: 'Proveedores' }, { id: 'facturas', label: 'Facturas' }, { id: 'pagos', label: 'Pagos realizados' }]} />

      {tab === 'proveedores' && (
        <Table head={['Proveedor', 'NIT', 'Contacto', 'Teléfono', 'Saldo pendiente']} empty={proveedores.length ? null : 'Crea tu primer proveedor para registrar compras.'}>
          {proveedores.map((p) => <tr key={p.id}><td className="px-4 py-2.5 font-medium">{p.nombre}</td><td className="px-4 py-2.5">{p.nit || '—'}</td><td className="px-4 py-2.5">{p.contacto || '—'}</td><td className="px-4 py-2.5">{p.telefono || '—'}</td><td className="px-4 py-2.5 font-semibold">{money(saldo(p.id))}</td></tr>)}
        </Table>
      )}
      {tab === 'facturas' && (
        <Table head={['N° factura', 'Proveedor', 'Fecha', 'Vence', 'Total', 'Pagado', 'Estado', '']} empty={facturas.length ? null : 'Sin facturas registradas.'}>
          {facturas.map((x) => { const est = estadoFactura(x); return (
            <tr key={x.id}><td className="px-4 py-2.5 font-medium">{x.numero}</td><td className="px-4 py-2.5">{nombre(x.proveedorId)}</td><td className="px-4 py-2.5">{fecha(x.fecha)}</td><td className="px-4 py-2.5">{fecha(x.vence)}</td>
              <td className="px-4 py-2.5">{money(x.total)}</td><td className="px-4 py-2.5">{money(x.pagado)}</td><td className="px-4 py-2.5"><Badge tone={TONOS[est]}>{est}</Badge></td>
              <td className="px-4 py-2.5 text-right">{est !== 'pagada' && <button className={btnGhost} onClick={() => { setF({ monto: x.total - x.pagado, metodo: 'efectivo', descontarCaja: true }); setModal(x) }}><Wallet size={14} /> Pagar</button>}</td></tr>
          ) })}
        </Table>
      )}
      {tab === 'pagos' && (
        <Table head={['Fecha', 'Proveedor', 'Factura', 'Monto', 'Método', 'Referencia', 'Registró']} empty={pagos.length ? null : 'Sin pagos registrados.'}>
          {pagos.map((p) => <tr key={p.id}><td className="px-4 py-2.5">{fecha(p.fecha)}</td><td className="px-4 py-2.5">{nombre(p.proveedorId)}</td><td className="px-4 py-2.5">{facturas.find((x) => x.id === p.facturaId)?.numero}</td><td className="px-4 py-2.5 font-semibold">{money(p.monto)}</td><td className="px-4 py-2.5 capitalize">{p.metodo}</td><td className="px-4 py-2.5">{p.referencia || '—'}</td><td className="px-4 py-2.5">{p.usuario || '—'}</td></tr>)}
        </Table>
      )}

      {modal === 'compra' && <CompraForm proveedores={proveedores} productos={productos.data ?? []} onClose={() => setModal(null)} />}
      {(modal === 'proveedor' || (modal && typeof modal === 'object')) && (
        <Modal title={modal === 'proveedor' ? 'Nuevo proveedor' : `Pago a factura ${modal.numero}`} onClose={() => setModal(null)}>
          <form onSubmit={submitSimple} className="space-y-3">
            {modal === 'proveedor' ? <>
              <Field label="Nombre / Razón social"><input required className={inputCls} onChange={set('nombre')} /></Field>
              <Field label="NIT / Cédula"><input className={inputCls} onChange={set('nit')} /></Field>
              <Field label="Contacto"><input className={inputCls} onChange={set('contacto')} /></Field>
              <Field label="Teléfono"><input className={inputCls} onChange={set('telefono')} /></Field>
            </> : <>
              <p className="text-sm text-gray-600">Saldo pendiente: <b>{money(modal.total - modal.pagado)}</b></p>
              <Field label="Monto"><input required type="number" min="1" className={inputCls} value={f.monto} onChange={set('monto')} /></Field>
              <Field label="Método"><select className={inputCls} value={f.metodo} onChange={set('metodo')}><option value="efectivo">Efectivo</option><option value="nequi">Nequi</option><option value="daviplata">Daviplata</option><option value="transferencia">Transferencia</option></select></Field>
              {f.metodo === 'efectivo' && <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={!!f.descontarCaja} onChange={(e) => setF({ ...f, descontarCaja: e.target.checked })} /> Registrar como gasto de mi caja abierta</label>}
              <Field label="Referencia (opcional)"><input className={inputCls} onChange={set('referencia')} /></Field>
            </>}
            <div className="flex justify-end gap-2"><button type="button" className={btnGhost} onClick={() => setModal(null)}>Cancelar</button><button className={btnPrimary} disabled={busy}>Guardar</button></div>
          </form>
        </Modal>
      )}
    </div>
  )
}
