import { useState } from 'react'
import toast from 'react-hot-toast'
import { Plus, Pencil, PackagePlus, PackageMinus, History, Trash2, Search } from 'lucide-react'
import * as inv from '../services/inventarioService'
import { errMsg } from '../services/api'
import { money, fechaHora } from '../utils/formatters'
import { Modal, Field, inputCls, btnPrimary, btnGhost, Badge, Table, Spinner, ErrorBox, PageHeader, useFetch, useAccion } from '../components/common/ui'
import AjusteModal from '../components/inventario/AjusteModal'

function ProductoForm({ producto, categorias, onClose, onDone, recargarCategorias }) {
  const edit = !!producto
  const [f, setF] = useState({
    codigoBarras: producto?.codigoBarras ?? '', nombre: producto?.nombre ?? '', categoriaId: producto?.categoriaId ?? categorias[0]?.id ?? '',
    precioCompra: producto?.precioCompra ?? '', precioVenta: producto?.precioVenta ?? '', stockInicial: 0, stockMinimo: producto?.stockMinimo ?? 5,
  })
  const [nuevaCat, setNuevaCat] = useState('')
  const { busy, ejecutar } = useAccion()
  const set = (k) => (e) => setF({ ...f, [k]: e.target.value })

  const crearCat = () => ejecutar(async () => {
    const c = await inv.crearCategoria(nuevaCat.trim()); await recargarCategorias(); setF((x) => ({ ...x, categoriaId: c.id })); setNuevaCat('')
  }, 'Categoría creada')

  const submit = (e) => {
    e.preventDefault()
    if (Number(f.precioVenta) < Number(f.precioCompra)) return toast.error('El precio de venta no puede ser menor al de compra')
    const base = { codigoBarras: f.codigoBarras.trim() || null, nombre: f.nombre.trim(), categoriaId: Number(f.categoriaId), precioCompra: Number(f.precioCompra), precioVenta: Number(f.precioVenta), stockMinimo: Number(f.stockMinimo) }
    ejecutar(async () => {
      if (edit) await inv.actualizarProducto(producto.id, base); else await inv.crearProducto({ ...base, stockInicial: Number(f.stockInicial) })
      onDone(); onClose()
    }, edit ? 'Producto actualizado' : 'Producto creado')
  }

  return (
    <Modal title={edit ? 'Editar producto' : 'Nuevo producto'} onClose={onClose}>
      <form onSubmit={submit} className="space-y-3">
        <Field label="Nombre"><input required maxLength={150} className={inputCls} value={f.nombre} onChange={set('nombre')} /></Field>
        <Field label="Código de barras (opcional)"><input maxLength={50} className={inputCls} value={f.codigoBarras} onChange={set('codigoBarras')} /></Field>
        <Field label="Categoría">
          <select required className={inputCls} value={f.categoriaId} onChange={set('categoriaId')}>{categorias.map((c) => <option key={c.id} value={c.id}>{c.nombre}</option>)}</select>
        </Field>
        <div className="flex gap-2">
          <input className={inputCls} placeholder="Nueva categoría…" value={nuevaCat} onChange={(e) => setNuevaCat(e.target.value)} />
          <button type="button" className={btnGhost} disabled={!nuevaCat.trim() || busy} onClick={crearCat}>Crear</button>
        </div>
        <div className="grid grid-cols-2 gap-3">
          <Field label="Precio de compra"><input required type="number" min="0" className={inputCls} value={f.precioCompra} onChange={set('precioCompra')} /></Field>
          <Field label="Precio de venta"><input required type="number" min="0" className={inputCls} value={f.precioVenta} onChange={set('precioVenta')} /></Field>
          {!edit && <Field label="Stock inicial"><input type="number" min="0" className={inputCls} value={f.stockInicial} onChange={set('stockInicial')} /></Field>}
          <Field label="Stock mínimo"><input type="number" min="0" className={inputCls} value={f.stockMinimo} onChange={set('stockMinimo')} /></Field>
        </div>
        {edit && <p className="text-xs text-gray-500">El stock no se edita aquí: usa Entrada o Merma/Ajuste para que quede trazabilidad.</p>}
        <div className="flex justify-end gap-2"><button type="button" className={btnGhost} onClick={onClose}>Cancelar</button><button className={btnPrimary} disabled={busy}>Guardar</button></div>
      </form>
    </Modal>
  )
}

function EntradaModal({ producto, onClose, onDone }) {
  const [f, setF] = useState({ cantidad: 1, costoUnitario: producto.precioCompra, motivo: '' })
  const { busy, ejecutar } = useAccion()
  return (
    <Modal title={`Entrada de mercancía — ${producto.nombre}`} onClose={onClose}>
      <form className="space-y-3" onSubmit={(e) => { e.preventDefault(); ejecutar(async () => { await inv.registrarEntrada(producto.id, { cantidad: Number(f.cantidad), costoUnitario: Number(f.costoUnitario), motivo: f.motivo.trim() || null }); onDone(); onClose() }, 'Entrada registrada') }}>
        <Field label="Cantidad"><input required autoFocus type="number" min="1" className={inputCls} value={f.cantidad} onChange={(e) => setF({ ...f, cantidad: e.target.value })} /></Field>
        <Field label="Costo unitario"><input type="number" min="0" className={inputCls} value={f.costoUnitario} onChange={(e) => setF({ ...f, costoUnitario: e.target.value })} /></Field>
        <Field label="Motivo / factura del proveedor"><input maxLength={250} className={inputCls} value={f.motivo} onChange={(e) => setF({ ...f, motivo: e.target.value })} /></Field>
        <div className="flex justify-end"><button className={btnPrimary} disabled={busy}>Registrar entrada</button></div>
      </form>
    </Modal>
  )
}

function Kardex({ producto, onClose }) {
  const { data, loading, error } = useFetch(() => inv.getMovimientos(producto.id), [producto.id])
  return (
    <Modal wide title={`Kardex — ${producto.nombre}`} onClose={onClose}>
      {loading ? <Spinner /> : error ? <ErrorBox msg={error} /> : (
        <Table head={['Fecha', 'Tipo', 'Cantidad', 'Costo', 'Motivo', 'Usuario']} empty={data.length ? null : 'Sin movimientos.'}>
          {data.map((m) => (
            <tr key={m.id}><td className="px-4 py-2">{fechaHora(m.fecha)}</td><td className="px-4 py-2"><Badge tone={m.tipo === 'Entrada' ? 'green' : m.tipo === 'Venta' ? 'blue' : 'yellow'}>{m.tipo}</Badge></td>
              <td className="px-4 py-2">{m.cantidad > 0 && m.tipo !== 'Venta' ? '+' : ''}{m.cantidad}</td><td className="px-4 py-2">{m.costoUnitario != null ? money(m.costoUnitario) : '—'}</td>
              <td className="px-4 py-2">{m.motivo ?? '—'}</td><td className="px-4 py-2">{m.usuario}</td></tr>
          ))}
        </Table>
      )}
    </Modal>
  )
}

export default function Inventario() {
  const [q, setQ] = useState('')
  const [soloBajo, setSoloBajo] = useState(false)
  const prods = useFetch(() => inv.getProductos({ ...(q.trim() && { busqueda: q.trim() }), ...(soloBajo && { soloBajoStock: true }) }), [q, soloBajo])
  const cats = useFetch(() => inv.getCategorias(), [])
  const [modal, setModal] = useState(null) // { tipo, producto }
  const cerrar = () => setModal(null)
  const desactivar = async (p) => {
    if (!window.confirm(`¿Desactivar "${p.nombre}"? Dejará de aparecer en el POS.`)) return
    try { await inv.desactivarProducto(p.id); toast.success('Producto desactivado'); prods.reload() } catch (e) { toast.error(errMsg(e)) }
  }
  const lista = prods.data ?? []

  return (
    <div>
      <PageHeader title="Inventario" subtitle="Productos, entradas de mercancía, mermas y kardex.">
        <button className={btnPrimary} disabled={!cats.data} onClick={() => setModal({ tipo: 'producto' })}><Plus size={16} /> Nuevo producto</button>
      </PageHeader>
      <div className="mb-3 flex flex-wrap items-center gap-3">
        <div className="relative w-full max-w-xs"><Search className="absolute left-3 top-2.5 text-gray-400" size={16} /><input className={inputCls + ' pl-9'} placeholder="Buscar por nombre o código" value={q} onChange={(e) => setQ(e.target.value)} /></div>
        <label className="flex items-center gap-2 text-sm text-gray-600"><input type="checkbox" checked={soloBajo} onChange={(e) => setSoloBajo(e.target.checked)} /> Solo por reabastecer</label>
      </div>
      {prods.error ? <ErrorBox msg={prods.error} onRetry={prods.reload} /> : prods.loading && !prods.data ? <Spinner /> : (
        <Table head={['Producto', 'Categoría', 'Compra', 'Venta', 'Margen', 'Stock', '']} empty={lista.length ? null : 'No hay productos.'}>
          {lista.map((p) => (
            <tr key={p.id}>
              <td className="px-4 py-2.5"><p className="font-medium">{p.nombre}</p><p className="text-xs text-gray-400">{p.codigoBarras ?? 'Sin código'}</p></td>
              <td className="px-4 py-2.5">{p.categoria}</td><td className="px-4 py-2.5">{money(p.precioCompra)}</td><td className="px-4 py-2.5">{money(p.precioVenta)}</td>
              <td className="px-4 py-2.5"><Badge tone={p.margenPorcentaje < 10 ? 'red' : 'green'}>{p.margenPorcentaje?.toFixed(1)}%</Badge></td>
              <td className="px-4 py-2.5"><Badge tone={p.stockActual === 0 ? 'red' : p.requiereReabastecimiento ? 'yellow' : 'green'}>{p.stockActual} / mín {p.stockMinimo}</Badge></td>
              <td className="whitespace-nowrap px-4 py-2.5 text-right">
                <button title="Entrada" className={btnGhost} onClick={() => setModal({ tipo: 'entrada', producto: p })}><PackagePlus size={15} /></button>{' '}
                <button title="Merma / ajuste" className={btnGhost} onClick={() => setModal({ tipo: 'ajuste', producto: p })}><PackageMinus size={15} /></button>{' '}
                <button title="Kardex" className={btnGhost} onClick={() => setModal({ tipo: 'kardex', producto: p })}><History size={15} /></button>{' '}
                <button title="Editar" className={btnGhost} onClick={() => setModal({ tipo: 'producto', producto: p })}><Pencil size={15} /></button>{' '}
                <button title="Desactivar" className={btnGhost} onClick={() => desactivar(p)}><Trash2 size={15} /></button>
              </td>
            </tr>
          ))}
        </Table>
      )}
      {modal?.tipo === 'producto' && <ProductoForm producto={modal.producto} categorias={cats.data ?? []} recargarCategorias={cats.reload} onClose={cerrar} onDone={prods.reload} />}
      {modal?.tipo === 'entrada' && <EntradaModal producto={modal.producto} onClose={cerrar} onDone={prods.reload} />}
      {modal?.tipo === 'ajuste' && <AjusteModal producto={modal.producto} onClose={cerrar} onDone={prods.reload} />}
      {modal?.tipo === 'kardex' && <Kardex producto={modal.producto} onClose={cerrar} />}
    </div>
  )
}
