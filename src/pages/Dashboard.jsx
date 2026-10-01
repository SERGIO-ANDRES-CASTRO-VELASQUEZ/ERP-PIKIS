import { useState } from 'react'
import { ResponsiveContainer, BarChart, Bar, XAxis, YAxis, Tooltip, CartesianGrid } from 'recharts'
import { getDashboard } from '../services/dashboardService'
import { money, dia } from '../utils/formatters'
import { Kpi, Spinner, ErrorBox, PageHeader, inputCls, Table, useFetch } from '../components/common/ui'

export default function Dashboard() {
  const [fecha, setFecha] = useState(dia())
  const { data: d, loading, error, reload } = useFetch(() => getDashboard(fecha), [fecha])

  return (
    <div>
      <PageHeader title="Dashboard financiero" subtitle="Resumen del día y de la última semana.">
        <input type="date" className={inputCls + ' !w-auto'} max={dia()} value={fecha} onChange={(e) => e.target.value && setFecha(e.target.value)} />
        <button className="text-sm text-pikis-blue underline" onClick={reload}>Actualizar</button>
      </PageHeader>
      {error ? <ErrorBox msg={error} onRetry={reload} /> : loading && !d ? <Spinner /> : d && (
        <>
          <div className="mb-5 grid grid-cols-2 gap-3 lg:grid-cols-4">
            <Kpi label="Ventas del día" value={money(d.ventasDelDia)} sub={`${d.numeroVentasDelDia} ventas`} tone="text-pikis-blue" />
            <Kpi label="Ganancia bruta del día" value={money(d.utilidadBrutaDelDia)} tone="text-pikis-green" />
            <Kpi label="Cartera por cobrar" value={money(d.carteraTotal)} tone="text-pikis-red" />
            <Kpi label="Valor del inventario" value={money(d.valorInventario)} />
            <Kpi label="Ventas de contado" value={money(d.ventasContado)} />
            <Kpi label="Ventas a crédito" value={money(d.ventasCredito)} />
            <Kpi label="Abonos de cartera" value={money(d.abonosCartera)} />
            <Kpi label="Gastos del día" value={money(d.egresosDelDia)} />
          </div>
          <div className="grid gap-4 lg:grid-cols-3">
            <div className="rounded-xl border bg-white p-4 lg:col-span-2">
              <p className="mb-3 font-semibold">Ventas últimos 7 días</p>
              <div className="h-64">
                <ResponsiveContainer>
                  <BarChart data={d.ventasUltimos7Dias.map((x) => ({ ...x, label: x.dia.slice(5) }))}>
                    <CartesianGrid strokeDasharray="3 3" vertical={false} />
                    <XAxis dataKey="label" /><YAxis tickFormatter={(v) => (v >= 1000 ? `${v / 1000}k` : v)} width={40} />
                    <Tooltip formatter={(v) => money(v)} labelFormatter={(l) => `Día ${l}`} />
                    <Bar dataKey="total" name="Ventas" fill="#3B5BDB" radius={[4, 4, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </div>
            </div>
            <div className="rounded-xl border bg-white p-4">
              <p className="mb-3 font-semibold">Más vendidos (7 días)</p>
              {d.masVendidos7Dias.length === 0 && <p className="text-sm text-gray-400">Sin ventas.</p>}
              <ol className="space-y-2 text-sm">
                {d.masVendidos7Dias.map((p) => <li key={p.productoId} className="flex justify-between gap-2"><span className="truncate">{p.nombre}</span><span className="shrink-0 text-gray-500">{p.cantidad} u · {money(p.total)}</span></li>)}
              </ol>
            </div>
          </div>
          <h2 className="mb-2 mt-6 font-semibold">Productos por reabastecer</h2>
          <Table head={['Producto', 'Stock', 'Mínimo']} empty={d.alertasStock.length ? null : 'Todo el inventario está por encima del mínimo.'}>
            {d.alertasStock.map((a) => <tr key={a.productoId}><td className="px-4 py-2 font-medium">{a.nombre}</td><td className="px-4 py-2 text-pikis-red">{a.stockActual}</td><td className="px-4 py-2">{a.stockMinimo}</td></tr>)}
          </Table>
        </>
      )}
    </div>
  )
}
