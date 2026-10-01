import { useEffect, useMemo, useRef, useState } from 'react'
import toast from 'react-hot-toast'
import { Search, Plus, Minus, Trash2, Wallet } from 'lucide-react'
import { useCartStore } from '../store/cartStore'
import { useCajaStore } from '../store/cajaStore'
import { getProductos, getProductoPorCodigo } from '../services/inventarioService'
import { registrarVenta } from '../services/ventasService'
import { errMsg } from '../services/api'
import { money } from '../utils/formatters'
import { inputCls, btnPrimary, btnGhost, Spinner, Field } from '../components/common/ui'
import PaymentModal from '../components/pos/PaymentModal'
import TicketModal from '../components/pos/TicketModal'

function AbrirCaja() {
  const abrir = useCajaStore((s) => s.abrir)
  const [monto, setMonto] = useState('')
  const [busy, setBusy] = useState(false)
  const submit = async (e) => {
    e.preventDefault(); setBusy(true)
    try { await abrir(monto || 0); toast.success('Caja abierta') } catch (err) { toast.error(errMsg(err)) } finally { setBusy(false) }
  }
  return (
    <form onSubmit={submit} className="mx-auto mt-16 max-w-sm space-y-4 rounded-2xl border bg-white p-6 text-center">
      <Wallet className="mx-auto text-pikis-blue" size={36} />
      <h2 className="text-lg font-bold">Abre tu caja para vender</h2>
      <p className="text-sm text-gray-500">Sin caja abierta no se pueden registrar ventas.</p>
      <Field label="Efectivo base en caja"><input autoFocus type="number" min="0" required className={inputCls} value={monto} onChange={(e) => setMonto(e.target.value)} /></Field>
      <button className={btnPrimary + ' w-full'} disabled={busy}>Abrir caja</button>
    </form>
  )
}

export default function POS() {
  const { turno, cargado, refrescar } = useCajaStore()
  const { items, add, setCantidad, remove, clear, total } = useCartStore()
  const [productos, setProductos] = useState([])
  const [q, setQ] = useState('')
  const [loading, setLoading] = useState(true)
  const [cobrando, setCobrando] = useState(false)
  const [busy, setBusy] = useState(false)
  const [resultado, setResultado] = useState(null)
  const inputRef = useRef(null)

  const cargarProductos = () => getProductos().then(setProductos).catch((e) => toast.error(errMsg(e))).finally(() => setLoading(false))
  useEffect(() => { refrescar().catch((e) => toast.error(errMsg(e))); cargarProductos() }, []) // eslint-disable-line

  const filtrados = useMemo(() => {
    const t = q.trim().toLowerCase()
    if (!t) return productos.slice(0, 60)
    return productos.filter((p) => p.nombre.toLowerCase().includes(t) || (p.codigoBarras ?? '').includes(t)).slice(0, 60)
  }, [productos, q])

  const agregar = (p) => { if (!add(p)) toast.error(`Sin stock suficiente de ${p.nombre}`) }

  // Los lectores USB escriben el código y envían Enter
  const onEnter = async (e) => {
    if (e.key !== 'Enter' || !q.trim()) return
    const codigo = q.trim()
    let p = productos.find((x) => x.codigoBarras === codigo)
    if (!p) { try { p = await getProductoPorCodigo(codigo) } catch { /* 404 */ } }
    if (p) { agregar(p); setQ('') } else if (filtrados.length === 1) { agregar(filtrados[0]); setQ('') } else toast.error('Producto no encontrado')
  }

  const cobrar = async (pago) => {
    setBusy(true)
    try {
      const r = await registrarVenta({ ...pago, items: items.map((i) => ({ productoId: i.productoId, cantidad: i.cantidad })) })
      clear(); setCobrando(false); setResultado(r)
      refrescar(); cargarProductos()
    } catch (e) { toast.error(errMsg(e)) } finally { setBusy(false) }
  }

  if (!cargado) return <Spinner />
  if (!turno) return <AbrirCaja />

  return (
    <div className="grid gap-4 lg:grid-cols-5">
      <section className="lg:col-span-3">
        <div className="relative mb-3">
          <Search className="absolute left-3 top-2.5 text-gray-400" size={18} />
          <input ref={inputRef} autoFocus className={inputCls + ' pl-10'} placeholder="Escanea el código o busca por nombre…" value={q} onChange={(e) => setQ(e.target.value)} onKeyDown={onEnter} />
        </div>
        {loading ? <Spinner /> : (
          <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
            {filtrados.map((p) => (
              <button key={p.id} disabled={p.stockActual < 1} onClick={() => agregar(p)}
                className="rounded-xl border bg-white p-3 text-left hover:border-pikis-blue disabled:opacity-40">
                <p className="line-clamp-2 text-sm font-medium">{p.nombre}</p>
                <p className="mt-1 font-bold text-pikis-blue">{money(p.precioVenta)}</p>
                <p className={'text-xs ' + (p.requiereReabastecimiento ? 'text-pikis-red' : 'text-gray-400')}>{p.stockActual < 1 ? 'Agotado' : `Stock ${p.stockActual}`}</p>
              </button>
            ))}
            {filtrados.length === 0 && <p className="col-span-full p-6 text-center text-sm text-gray-500">Sin resultados.</p>}
          </div>
        )}
      </section>

      <aside className="lg:col-span-2">
        <div className="sticky top-4 rounded-xl border bg-white p-4">
          <div className="mb-3 flex items-center justify-between">
            <h2 className="font-semibold">Venta actual</h2>
            {items.length > 0 && <button className="text-xs text-gray-500 underline" onClick={clear}>Vaciar</button>}
          </div>
          <div className="max-h-[50vh] space-y-2 overflow-y-auto">
            {items.length === 0 && <p className="py-8 text-center text-sm text-gray-400">Agrega productos para empezar.</p>}
            {items.map((i) => (
              <div key={i.productoId} className="flex items-center gap-2 rounded-lg border p-2 text-sm">
                <div className="min-w-0 flex-1">
                  <p className="truncate font-medium">{i.nombre}</p>
                  <p className="text-xs text-gray-500">{money(i.precio)} c/u</p>
                </div>
                <button className="rounded border p-1" onClick={() => setCantidad(i.productoId, i.cantidad - 1)} aria-label="Menos"><Minus size={14} /></button>
                <span className="w-6 text-center">{i.cantidad}</span>
                <button className="rounded border p-1" onClick={() => setCantidad(i.productoId, i.cantidad + 1)} aria-label="Más"><Plus size={14} /></button>
                <span className="w-20 text-right font-semibold">{money(i.precio * i.cantidad)}</span>
                <button className="text-gray-400 hover:text-pikis-red" onClick={() => remove(i.productoId)} aria-label="Quitar"><Trash2 size={16} /></button>
              </div>
            ))}
          </div>
          <div className="mt-4 flex items-center justify-between border-t pt-3">
            <span className="text-gray-600">Total</span>
            <span className="text-2xl font-bold">{money(total())}</span>
          </div>
          <button className={btnPrimary + ' mt-3 w-full py-3 text-base'} disabled={!items.length} onClick={() => setCobrando(true)}>Cobrar</button>
        </div>
      </aside>

      {cobrando && <PaymentModal total={total()} busy={busy} onConfirm={cobrar} onClose={() => setCobrando(false)} />}
      {resultado && <TicketModal venta={resultado.venta} ticketImpreso={resultado.ticketImpreso} errorImpresion={resultado.errorImpresion} onClose={() => { setResultado(null); inputRef.current?.focus() }} />}
    </div>
  )
}
