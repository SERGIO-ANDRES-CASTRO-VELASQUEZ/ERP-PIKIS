import { useState } from 'react'
import { Plus, Pencil, HandCoins, Search } from 'lucide-react'
import { useAuthStore } from '../store/authStore'
import * as cs from '../services/clientesService'
import { money } from '../utils/formatters'
import { Modal, Field, inputCls, btnPrimary, btnGhost, Table, Spinner, ErrorBox, PageHeader, Kpi, useFetch, useAccion } from '../components/common/ui'

function ClienteForm({ cliente, onClose, onDone }) {
  const [f, setF] = useState({ nombre: cliente?.nombre ?? '', telefono: cliente?.telefono ?? '', limiteCredito: cliente?.limiteCredito ?? 0 })
  const { busy, ejecutar } = useAccion()
  return (
    <Modal title={cliente ? 'Editar cliente' : 'Nuevo cliente'} onClose={onClose}>
      <form className="space-y-3" onSubmit={(e) => { e.preventDefault(); const b = { nombre: f.nombre.trim(), telefono: f.telefono.trim() || null, limiteCredito: Number(f.limiteCredito) }
        ejecutar(async () => { cliente ? await cs.actualizarCliente(cliente.id, b) : await cs.crearCliente(b); onDone(); onClose() }, 'Cliente guardado') }}>
        <Field label="Nombre"><input required autoFocus maxLength={120} className={inputCls} value={f.nombre} onChange={(e) => setF({ ...f, nombre: e.target.value })} /></Field>
        <Field label="Teléfono"><input maxLength={20} className={inputCls} value={f.telefono} onChange={(e) => setF({ ...f, telefono: e.target.value })} /></Field>
        <Field label="Límite de crédito"><input required type="number" min="0" className={inputCls} value={f.limiteCredito} onChange={(e) => setF({ ...f, limiteCredito: e.target.value })} /></Field>
        <div className="flex justify-end"><button className={btnPrimary} disabled={busy}>Guardar</button></div>
      </form>
    </Modal>
  )
}

function AbonoModal({ cliente, onClose, onDone }) {
  const [f, setF] = useState({ monto: '', nota: '' })
  const { busy, ejecutar } = useAccion()
  return (
    <Modal title={`Abono — ${cliente.nombre}`} onClose={onClose}>
      <form className="space-y-3" onSubmit={(e) => { e.preventDefault(); ejecutar(async () => { await cs.registrarAbono(cliente.id, f.monto, f.nota); onDone(); onClose() }, 'Abono registrado') }}>
        <p className="text-sm text-gray-600">Deuda actual: <b>{money(cliente.saldoActual)}</b></p>
        <Field label="Monto recibido (efectivo)"><input required autoFocus type="number" min="1" max={cliente.saldoActual} className={inputCls} value={f.monto} onChange={(e) => setF({ ...f, monto: e.target.value })} /></Field>
        <Field label="Nota (opcional)"><input maxLength={250} className={inputCls} value={f.nota} onChange={(e) => setF({ ...f, nota: e.target.value })} /></Field>
        <p className="text-xs text-gray-500">Requiere tener la caja abierta: el abono entra al efectivo del turno.</p>
        <div className="flex justify-end"><button className={btnPrimary} disabled={busy}>Registrar abono</button></div>
      </form>
    </Modal>
  )
}

export default function Clientes() {
  const admin = useAuthStore((s) => s.user.rol === 'Administrador')
  const [q, setQ] = useState('')
  const { data, loading, error, reload } = useFetch(() => cs.getClientes(q.trim()), [q])
  const [modal, setModal] = useState(null)
  const lista = data ?? []
  const cartera = lista.reduce((s, c) => s + c.saldoActual, 0)

  return (
    <div>
      <PageHeader title="Clientes y fiado" subtitle="Cartera de vecinos, límites de crédito y abonos.">
        {admin && <button className={btnPrimary} onClick={() => setModal({ tipo: 'form' })}><Plus size={16} /> Nuevo cliente</button>}
      </PageHeader>
      <div className="mb-4 grid gap-3 sm:grid-cols-3">
        <Kpi label="Cartera por cobrar" value={money(cartera)} tone="text-pikis-red" />
        <Kpi label="Clientes" value={lista.length} />
        <Kpi label="Con deuda" value={lista.filter((c) => c.saldoActual > 0).length} />
      </div>
      <div className="relative mb-3 max-w-xs"><Search className="absolute left-3 top-2.5 text-gray-400" size={16} /><input className={inputCls + ' pl-9'} placeholder="Buscar cliente" value={q} onChange={(e) => setQ(e.target.value)} /></div>
      {error ? <ErrorBox msg={error} onRetry={reload} /> : loading && !data ? <Spinner /> : (
        <Table head={['Cliente', 'Teléfono', 'Límite', 'Debe', 'Disponible', '']} empty={lista.length ? null : 'No hay clientes.'}>
          {lista.map((c) => {
            const pct = c.limiteCredito ? Math.min(100, (c.saldoActual / c.limiteCredito) * 100) : 0
            return (
              <tr key={c.id}>
                <td className="px-4 py-2.5 font-medium">{c.nombre}</td><td className="px-4 py-2.5">{c.telefono ?? '—'}</td><td className="px-4 py-2.5">{money(c.limiteCredito)}</td>
                <td className="px-4 py-2.5"><p className="font-semibold">{money(c.saldoActual)}</p><div className="mt-1 h-1.5 w-24 rounded bg-gray-100"><div className={'h-1.5 rounded ' + (pct >= 80 ? 'bg-pikis-red' : 'bg-pikis-green')} style={{ width: `${pct}%` }} /></div></td>
                <td className="px-4 py-2.5">{money(c.creditoDisponible)}</td>
                <td className="space-x-2 whitespace-nowrap px-4 py-2.5 text-right">
                  <button className={btnGhost} disabled={c.saldoActual <= 0} onClick={() => setModal({ tipo: 'abono', c })}><HandCoins size={15} /> Abonar</button>
                  {admin && <button className={btnGhost} onClick={() => setModal({ tipo: 'form', c })}><Pencil size={15} /></button>}
                </td>
              </tr>
            )
          })}
        </Table>
      )}
      {modal?.tipo === 'form' && <ClienteForm cliente={modal.c} onClose={() => setModal(null)} onDone={reload} />}
      {modal?.tipo === 'abono' && <AbonoModal cliente={modal.c} onClose={() => setModal(null)} onDone={reload} />}
    </div>
  )
}
