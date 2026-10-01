import { useState } from 'react'
import toast from 'react-hot-toast'
import { Plus, Wallet, Trash2, Pencil, Eye } from 'lucide-react'
import * as pv from '../services/proveedoresService'
import { useCajaStore } from '../store/cajaStore'
import { getProductos } from '../services/inventarioService'
import { money, fechaDia, fechaHora, dia, hace } from '../utils/formatters'
import { Modal, Field, inputCls, btnPrimary, btnGhost, Badge, Tabs, Table, Kpi, Spinner, ErrorBox, PageHeader, useFetch, useAccion } from '../components/common/ui'

const TONOS = { Pagada: 'green', Parcial: 'yellow', Pendiente: 'gray', Vencida: 'red' }
const METODOS = ['Efectivo', 'Nequi', 'Daviplata', 'Transferencia']

function ProveedorForm({ proveedor, onClose, onDone }) {
  const [f, setF] = useState({ nombre: proveedor?.nombre ?? '', nit: proveedor?.nit ?? '', contacto: proveedor?.contacto ?? '', telefono: proveedor?.telefono ?? '', activo: proveedor?.activo ?? true })
  const { busy, ejecutar } = useAccion()
  const set = (k) => (e) => setF({ ...f, [k]: e.target.value })
  const submit = (e) => {
    e.preventDefault()
    const b = { nombre: f.nombre.trim(), nit: f.nit.trim() || null, contacto: f.contacto.trim() || null, telefono: f.telefono.trim() || null, activo: f.activo }
    ejecutar(async () => { proveedor ? await pv.actualizarProveedor(proveedor.id, b) : await pv.crearProveedor(b); onDone(); onClose() }, 'Proveedor guardado')
  }
  return (
    <Modal title={proveedor ? 'Editar proveedor' : 'Nuevo proveedor'} onClose={onClose}>
      <form onSubmit={submit} className="space-y-3">
        <Field label="Nombre / Razón social"><input required autoFocus maxLength={150} className={inputCls} value={f.nombre} onChange={set('nombre')} /></Field>
        <Field label="NIT / Cédula"><input maxLength={30} className={inputCls} value={f.nit} onChange={set('nit')} /></Field>
        <Field label="Contacto"><input maxLength={120} className={inputCls} value={f.contacto} onChange={set('contacto')} /></Field>
        <Field label="Teléfono"><input maxLength={20} className={inputCls} value={f.telefono} onChange={set('telefono')} /></Field>
        {proveedor && <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={f.activo} onChange={(e) => setF({ ...f, activo: e.target.checked })} /> Activo</label>}
        <div className="flex justify-end gap-2"><button type="button" className={btnGhost} onClick={onClose}>Cancelar</button><button className={btnPrimary} disabled={busy}>Guardar</button></div>
      </form>
    </Modal>
  )
}

function CompraForm({ proveedores, productos, onClose, onDone }) {
  const { busy, ejecutar } = useAccion()
  const [f, setF] = useState({ proveedorId: proveedores[0]?.id ?? '', numero: '', fecha: dia(), vence: '', totalManual: '' })
  const [lineas, setLineas] = useState([{ productoId: '', cantidad: 1, costo: '' }])
  const setL = (i, patch) => setLineas(lineas.map((l, j) => (j === i ? { ...l, ...patch } : l)))
  const validas = lineas.filter((l) => l.productoId && Number(l.cantidad) > 0)
  const total = validas.length ? validas.reduce((s, l) => s + Number(l.cantidad) * Number(l.costo || 0), 0) : Number(f.totalManual || 0)

  const submit = (e) => {
    e.preventDefault()
    if (total <= 0) return toast.error('El total de la factura debe ser mayor a 0')
    ejecutar(async () => {
      await pv.registrarFactura({
        proveedorId: Number(f.proveedorId),
        numero: f.numero.trim(),
        fecha: f.fecha,
        vence: f.vence || null,
        items: validas.map((l) => ({ productoId: Number(l.productoId), cantidad: Number(l.cantidad), costoUnitario: Number(l.costo || 0) })),
        total: validas.length ? null : total,
      })
      onDone(); onClose()
    }, 'Compra registrada')
  }

  return (
    <Modal wide title="Registrar compra / factura de proveedor" onClose={onClose}>
      <form onSubmit={submit} className="space-y-3">
        <div className="grid gap-3 sm:grid-cols-2">
          <Field label="Proveedor"><select required className={inputCls} value={f.proveedorId} onChange={(e) => setF({ ...f, proveedorId: e.target.value })}>{proveedores.map((p) => <option key={p.id} value={p.id}>{p.nombre}</option>)}</select></Field>
          <Field label="N° de factura"><input required maxLength={50} className={inputCls} value={f.numero} onChange={(e) => setF({ ...f, numero: e.target.value })} /></Field>
          <Field label="Fecha"><input required type="date" max={dia()} className={inputCls} value={f.fecha} onChange={(e) => setF({ ...f, fecha: e.target.value })} /></Field>
          <Field label="Vence (opcional)"><input type="date" min={f.fecha} className={inputCls} value={f.vence} onChange={(e) => setF({ ...f, vence: e.target.value })} /></Field>
        </div>
        <p className="text-sm font-medium text-gray-700">Mercancía recibida <span className="font-normal text-gray-400">(suma al stock y actualiza el costo del producto)</span></p>
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
        {!validas.length && <Field label="Total de la factura (si no detallas productos: fletes, servicios…)"><input type="number" min="1" className={inputCls} value={f.totalManual} onChange={(e) => setF({ ...f, totalManual: e.target.value })} /></Field>}
        <p className="text-right text-lg font-bold">Total: {money(total)}</p>
        <div className="flex justify-end gap-2"><button type="button" className={btnGhost} onClick={onClose}>Cancelar</button><button className={btnPrimary} disabled={busy}>Registrar</button></div>
      </form>
    </Modal>
  )
}

function PagoForm({ factura, onClose, onDone }) {
  const refrescarCaja = useCajaStore((s) => s.refrescar)
  const [f, setF] = useState({ monto: factura.saldo, metodo: 'Efectivo', referencia: '', descontarDeCaja: true })
  const { busy, ejecutar } = useAccion()
  const submit = (e) => {
    e.preventDefault()
    const monto = Number(f.monto)
    if (monto <= 0 || monto > factura.saldo) return toast.error(`El monto debe estar entre 1 y ${money(factura.saldo)}`)
    ejecutar(async () => {
      const descontar = f.metodo === 'Efectivo' && f.descontarDeCaja
      await pv.registrarPago(factura.id, { monto, metodo: f.metodo, referencia: f.referencia.trim() || null, descontarDeCaja: descontar })
      if (descontar) refrescarCaja().catch(() => {})
      onDone(); onClose()
    }, 'Pago registrado')
  }
  return (
    <Modal title={`Pago a factura ${factura.numero} — ${factura.proveedor}`} onClose={onClose}>
      <form onSubmit={submit} className="space-y-3">
        <p className="text-sm text-gray-600">Saldo pendiente: <b>{money(factura.saldo)}</b></p>
        <Field label="Monto"><input required autoFocus type="number" min="1" max={factura.saldo} className={inputCls} value={f.monto} onChange={(e) => setF({ ...f, monto: e.target.value })} /></Field>
        <Field label="Método"><select className={inputCls} value={f.metodo} onChange={(e) => setF({ ...f, metodo: e.target.value })}>{METODOS.map((m) => <option key={m}>{m}</option>)}</select></Field>
        {f.metodo === 'Efectivo' && <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={f.descontarDeCaja} onChange={(e) => setF({ ...f, descontarDeCaja: e.target.checked })} /> Sacar el dinero de mi caja abierta (queda como gasto del turno)</label>}
        <Field label="Referencia (opcional)"><input maxLength={100} className={inputCls} value={f.referencia} onChange={(e) => setF({ ...f, referencia: e.target.value })} /></Field>
        <div className="flex justify-end gap-2"><button type="button" className={btnGhost} onClick={onClose}>Cancelar</button><button className={btnPrimary} disabled={busy}>Guardar pago</button></div>
      </form>
    </Modal>
  )
}

function DetalleFactura({ id, onClose }) {
  const { data: x, loading, error } = useFetch(() => pv.getFactura(id), [id])
  return (
    <Modal wide title={x ? `Factura ${x.numero} — ${x.proveedor}` : 'Factura'} onClose={onClose}>
      {loading ? <Spinner /> : error ? <ErrorBox msg={error} /> : (
        <>
          <div className="mb-4 grid grid-cols-2 gap-3 sm:grid-cols-4">
            <Kpi label="Total" value={money(x.total)} /><Kpi label="Pagado" value={money(x.pagado)} /><Kpi label="Saldo" value={money(x.saldo)} tone={x.saldo > 0 ? 'text-pikis-red' : 'text-pikis-green'} />
            <Kpi label="Estado" value={<Badge tone={TONOS[x.estado]}>{x.estado}</Badge>} sub={`Fecha ${fechaDia(x.fecha)} · Vence ${fechaDia(x.vence)}`} />
          </div>
          <p className="mb-3 text-xs text-gray-500">Registrada {fechaHora(x.fechaRegistro)} por {x.usuario}</p>
          <h4 className="mb-2 font-semibold">Productos recibidos</h4>
          <Table head={['Producto', 'Cantidad', 'Costo', 'Subtotal']} empty={x.detalles.length ? null : 'Factura sin detalle de productos.'}>
            {x.detalles.map((d, i) => <tr key={i}><td className="px-4 py-2">{d.producto}</td><td className="px-4 py-2">{d.cantidad}</td><td className="px-4 py-2">{money(d.costoUnitario)}</td><td className="px-4 py-2">{money(d.subtotal)}</td></tr>)}
          </Table>
          <h4 className="mb-2 mt-4 font-semibold">Pagos</h4>
          <Table head={['Fecha', 'Monto', 'Método', 'Referencia', 'Registró']} empty={x.pagos.length ? null : 'Sin pagos.'}>
            {x.pagos.map((p) => <tr key={p.id}><td className="px-4 py-2">{fechaHora(p.fecha)}</td><td className="px-4 py-2 font-semibold">{money(p.monto)}</td><td className="px-4 py-2">{p.metodo}{p.salioDeCaja ? ' (caja)' : ''}</td><td className="px-4 py-2">{p.referencia ?? '—'}</td><td className="px-4 py-2">{p.usuario}</td></tr>)}
          </Table>
        </>
      )}
    </Modal>
  )
}

function TabPagos() {
  const [desde, setDesde] = useState(hace(30)); const [hasta, setHasta] = useState(dia())
  const { data, loading, error, reload } = useFetch(() => pv.getPagos(desde, hasta), [desde, hasta])
  const lista = data ?? []
  return (
    <>
      <div className="mb-3 flex flex-wrap items-center gap-2">
        <input type="date" className={inputCls + ' !w-auto'} value={desde} onChange={(e) => setDesde(e.target.value)} />
        <input type="date" className={inputCls + ' !w-auto'} value={hasta} onChange={(e) => setHasta(e.target.value)} />
        <span className="text-sm text-gray-500">Total pagado: <b>{money(lista.reduce((s, p) => s + p.monto, 0))}</b></span>
      </div>
      {error ? <ErrorBox msg={error} onRetry={reload} /> : loading && !data ? <Spinner /> : (
        <Table head={['Fecha', 'Proveedor', 'Factura', 'Monto', 'Método', 'Referencia', 'Registró']} empty={lista.length ? null : 'Sin pagos en el rango.'}>
          {lista.map((p) => <tr key={p.id}><td className="px-4 py-2.5">{fechaHora(p.fecha)}</td><td className="px-4 py-2.5">{p.proveedor}</td><td className="px-4 py-2.5">{p.numeroFactura}</td><td className="px-4 py-2.5 font-semibold">{money(p.monto)}</td><td className="px-4 py-2.5">{p.metodo}{p.salioDeCaja ? ' (caja)' : ''}</td><td className="px-4 py-2.5">{p.referencia || '—'}</td><td className="px-4 py-2.5">{p.usuario}</td></tr>)}
        </Table>
      )}
    </>
  )
}

export default function Proveedores() {
  const provs = useFetch(() => pv.getProveedores(true), [])
  const [soloPendientes, setSoloPendientes] = useState(false)
  const facts = useFetch(() => pv.getFacturas(soloPendientes ? { soloPendientes: true } : {}), [soloPendientes])
  const productos = useFetch(() => getProductos(), [])
  const [tab, setTab] = useState('proveedores')
  const [modal, setModal] = useState(null) // { tipo: 'proveedor'|'compra'|'pago'|'detalle', item }
  const cerrar = () => setModal(null)
  const recargar = () => { provs.reload(); facts.reload(); productos.reload() }

  const proveedores = provs.data ?? []
  const activos = proveedores.filter((p) => p.activo)
  const facturas = facts.data ?? []
  const porPagar = proveedores.reduce((s, p) => s + p.saldoPendiente, 0)

  return (
    <div>
      <PageHeader title="Proveedores" subtitle="Compras, cuentas por pagar y pagos.">
        <button className={btnGhost} onClick={() => setModal({ tipo: 'proveedor' })}><Plus size={16} /> Proveedor</button>
        <button className={btnPrimary} disabled={!activos.length || !productos.data} onClick={() => setModal({ tipo: 'compra' })}><Plus size={16} /> Registrar compra</button>
      </PageHeader>
      <div className="mb-5 grid gap-3 sm:grid-cols-3">
        <Kpi label="Por pagar" value={money(porPagar)} tone="text-pikis-red" />
        <Kpi label="Facturas vencidas" value={facturas.filter((x) => x.estado === 'Vencida').length} />
        <Kpi label="Proveedores activos" value={activos.length} />
      </div>
      <Tabs value={tab} onChange={setTab} tabs={[{ id: 'proveedores', label: 'Proveedores' }, { id: 'facturas', label: 'Facturas' }, { id: 'pagos', label: 'Pagos realizados' }]} />

      {tab === 'proveedores' && (provs.error ? <ErrorBox msg={provs.error} onRetry={provs.reload} /> : provs.loading && !provs.data ? <Spinner /> : (
        <Table head={['Proveedor', 'NIT', 'Contacto', 'Teléfono', 'Saldo pendiente', 'Estado', '']} empty={proveedores.length ? null : 'Crea tu primer proveedor para registrar compras.'}>
          {proveedores.map((p) => (
            <tr key={p.id}>
              <td className="px-4 py-2.5 font-medium">{p.nombre}</td><td className="px-4 py-2.5">{p.nit || '—'}</td><td className="px-4 py-2.5">{p.contacto || '—'}</td><td className="px-4 py-2.5">{p.telefono || '—'}</td>
              <td className="px-4 py-2.5 font-semibold">{money(p.saldoPendiente)}</td>
              <td className="px-4 py-2.5">{p.activo ? <Badge tone="green">Activo</Badge> : <Badge>Inactivo</Badge>}</td>
              <td className="px-4 py-2.5 text-right"><button className={btnGhost} onClick={() => setModal({ tipo: 'proveedor', item: p })}><Pencil size={14} /></button></td>
            </tr>
          ))}
        </Table>
      ))}

      {tab === 'facturas' && (
        <>
          <label className="mb-3 flex items-center gap-2 text-sm text-gray-600"><input type="checkbox" checked={soloPendientes} onChange={(e) => setSoloPendientes(e.target.checked)} /> Solo con saldo pendiente</label>
          {facts.error ? <ErrorBox msg={facts.error} onRetry={facts.reload} /> : facts.loading && !facts.data ? <Spinner /> : (
            <Table head={['N° factura', 'Proveedor', 'Fecha', 'Vence', 'Total', 'Pagado', 'Estado', '']} empty={facturas.length ? null : 'Sin facturas registradas.'}>
              {facturas.map((x) => (
                <tr key={x.id}>
                  <td className="px-4 py-2.5 font-medium">{x.numero}</td><td className="px-4 py-2.5">{x.proveedor}</td><td className="px-4 py-2.5">{fechaDia(x.fecha)}</td><td className="px-4 py-2.5">{fechaDia(x.vence)}</td>
                  <td className="px-4 py-2.5">{money(x.total)}</td><td className="px-4 py-2.5">{money(x.pagado)}</td><td className="px-4 py-2.5"><Badge tone={TONOS[x.estado]}>{x.estado}</Badge></td>
                  <td className="space-x-2 whitespace-nowrap px-4 py-2.5 text-right">
                    <button className={btnGhost} onClick={() => setModal({ tipo: 'detalle', item: x })}><Eye size={14} /></button>
                    {x.saldo > 0 && <button className={btnGhost} onClick={() => setModal({ tipo: 'pago', item: x })}><Wallet size={14} /> Pagar</button>}
                  </td>
                </tr>
              ))}
            </Table>
          )}
        </>
      )}

      {tab === 'pagos' && <TabPagos />}

      {modal?.tipo === 'proveedor' && <ProveedorForm proveedor={modal.item} onClose={cerrar} onDone={provs.reload} />}
      {modal?.tipo === 'compra' && <CompraForm proveedores={activos} productos={productos.data ?? []} onClose={cerrar} onDone={recargar} />}
      {modal?.tipo === 'pago' && <PagoForm factura={modal.item} onClose={cerrar} onDone={recargar} />}
      {modal?.tipo === 'detalle' && <DetalleFactura id={modal.item.id} onClose={cerrar} />}
    </div>
  )
}
