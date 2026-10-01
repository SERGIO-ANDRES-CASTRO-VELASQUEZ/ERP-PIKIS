import { useMemo, useState } from 'react'
import { PackageMinus } from 'lucide-react'
import { getProductos, getMovimientosGlobales } from '../services/inventarioService'
import { money, fechaHora, dia } from '../utils/formatters'
import AjusteModal, { TIPOS_MERMA, parseMotivo } from '../components/inventario/AjusteModal'
import { inputCls, btnPrimary, Badge, Table, Kpi, Spinner, ErrorBox, PageHeader, useFetch } from '../components/common/ui'

// mes = 'yyyy-MM' → primer y último día del mes (hora local, como espera el backend)
async function cargarAjustes(mes) {
  const [y, m] = mes.split('-').map(Number)
  const [productos, ajustes] = await Promise.all([
    getProductos(),
    getMovimientosGlobales({ tipo: 'Ajuste', desde: dia(new Date(y, m - 1, 1)), hasta: dia(new Date(y, m, 0)) }),
  ])
  const costoActual = Object.fromEntries(productos.map((p) => [p.id, p.precioCompra]))
  return { productos, ajustes: ajustes.map((a) => ({ ...a, costo: a.costoUnitario ?? costoActual[a.productoId] ?? 0 })) }
}

export default function Mermas() {
  const [mes, setMes] = useState(() => dia().slice(0, 7))
  const { data, loading, error, reload } = useFetch(() => cargarAjustes(mes), [mes])
  const [abierto, setAbierto] = useState(false)

  const delMes = useMemo(() => (data?.ajustes ?? []).map((a) => ({ ...a, ...parseMotivo(a.motivo ?? '') })), [data])
  const perdidas = delMes.filter((a) => a.cantidad < 0)
  const costoTotal = perdidas.reduce((s, a) => s + Math.abs(a.cantidad) * a.costo, 0)
  const porTipo = Object.entries(TIPOS_MERMA).map(([k, t]) => {
    const rs = delMes.filter((a) => a.tipo === k && a.cantidad < 0)
    return { k, t, n: rs.length, costo: rs.reduce((s, a) => s + Math.abs(a.cantidad) * a.costo, 0) }
  }).filter((x) => x.n)
  const max = Math.max(1, ...porTipo.map((x) => x.costo))

  return (
    <div>
      <PageHeader title="Mermas y ajustes" subtitle="Averías, vencimientos y ajustes con motivo y responsable (pérdidas hormiga).">
        <input type="month" className={inputCls + ' !w-auto'} max={dia().slice(0, 7)} value={mes} onChange={(e) => e.target.value && setMes(e.target.value)} />
        <button className={btnPrimary} disabled={!data} onClick={() => setAbierto(true)}><PackageMinus size={16} /> Registrar</button>
      </PageHeader>
      {error ? <ErrorBox msg={error} onRetry={reload} /> : loading && !data ? <Spinner /> : (
        <>
          <div className="mb-5 grid gap-4 lg:grid-cols-3">
            <Kpi label="Pérdida del mes (a costo)" value={money(costoTotal)} tone="text-pikis-red" sub={`${perdidas.length} registros`} />
            <div className="rounded-xl border bg-white p-4 lg:col-span-2">
              <p className="mb-2 text-xs text-gray-500">Pérdida por tipo</p>
              {porTipo.length === 0 && <p className="text-sm text-gray-400">Sin registros este mes.</p>}
              {porTipo.map((x) => (
                <div key={x.k} className="mb-1.5 flex items-center gap-3 text-sm">
                  <span className="w-40 shrink-0">{x.t.label}</span>
                  <div className="h-2 flex-1 rounded bg-gray-100"><div className="h-2 rounded bg-pikis-red" style={{ width: `${(x.costo / max) * 100}%` }} /></div>
                  <span className="w-24 text-right">{money(x.costo)}</span>
                </div>
              ))}
            </div>
          </div>
          <Table head={['Fecha', 'Producto', 'Tipo', 'Cant.', 'Costo', 'Motivo', 'Registró']} empty={delMes.length ? null : 'No hay ajustes en este mes.'}>
            {delMes.map((a) => (
              <tr key={a.id}>
                <td className="px-4 py-2.5">{fechaHora(a.fecha)}</td><td className="px-4 py-2.5 font-medium">{a.producto}</td>
                <td className="px-4 py-2.5">{a.tipo ? <Badge tone={TIPOS_MERMA[a.tipo].tone}>{TIPOS_MERMA[a.tipo].label}</Badge> : <Badge>Ajuste</Badge>}</td>
                <td className="px-4 py-2.5">{a.cantidad > 0 ? '+' : ''}{a.cantidad}</td><td className="px-4 py-2.5">{money(Math.abs(a.cantidad) * a.costo)}</td>
                <td className="px-4 py-2.5">{a.texto}</td><td className="px-4 py-2.5">{a.usuario}</td>
              </tr>
            ))}
          </Table>
        </>
      )}
      {abierto && <AjusteModal productos={data.productos} onClose={() => setAbierto(false)} onDone={reload} />}
    </div>
  )
}
