import { useState } from 'react'
import { Banknote, Smartphone, HandCoins } from 'lucide-react'
import clsx from 'clsx'
import { money } from '../../utils/formatters'
import { Modal, Field, inputCls, btnPrimary, useFetch } from '../common/ui'
import { getClientes } from '../../services/clientesService'

const METODOS = [
  { id: 'Efectivo',  label: 'Efectivo',  icon: Banknote },
  { id: 'Nequi',     label: 'Nequi',     icon: Smartphone },
  { id: 'Daviplata', label: 'Daviplata', icon: Smartphone },
  { id: 'Credito',   label: 'Fiar',      icon: HandCoins },
]

/** onConfirm({ tipoPago, montoRecibido, clienteId, imprimir }) → promesa */
export default function PaymentModal({ total, onConfirm, onClose, busy }) {
  const [tipoPago, setTipoPago] = useState('Efectivo')
  const [recibido, setRecibido] = useState('')
  const [clienteId, setClienteId] = useState('')
  const [imprimir, setImprimir] = useState(localStorage.getItem('imprimirTermica') === '1')
  const clientes = useFetch(() => getClientes(), [])

  const rec = Number(recibido) || 0
  const cambio = Math.max(0, rec - total)
  const cliente = (clientes.data ?? []).find((c) => String(c.id) === String(clienteId))
  const excede = tipoPago === 'Credito' && cliente && total > cliente.creditoDisponible
  const sugeridos = [...new Set([total, Math.ceil(total / 1000) * 1000, Math.ceil(total / 5000) * 5000, Math.ceil(total / 10000) * 10000, Math.ceil(total / 50000) * 50000])]
  const valido = tipoPago === 'Efectivo' ? rec >= total : tipoPago === 'Credito' ? !!cliente && !excede : true

  const confirmar = () => {
    localStorage.setItem('imprimirTermica', imprimir ? '1' : '0')
    onConfirm({
      tipoPago,
      montoRecibido: tipoPago === 'Efectivo' && rec > 0 ? rec : null,
      clienteId: tipoPago === 'Credito' ? Number(clienteId) : null,
      imprimir,
    })
  }

  return (
    <Modal title="Cobrar venta" onClose={onClose}>
      <p className="mb-4 text-center text-3xl font-bold">{money(total)}</p>
      <div className="mb-4 grid grid-cols-4 gap-2">
        {METODOS.map(({ id, label, icon: Icon }) => (
          <button key={id} onClick={() => setTipoPago(id)}
            className={clsx('flex flex-col items-center gap-1 rounded-lg border p-3 text-xs font-medium', tipoPago === id ? 'border-pikis-blue bg-primary-50 text-pikis-blue' : 'border-gray-200 text-gray-600 hover:bg-gray-50')}>
            <Icon size={20} />{label}
          </button>
        ))}
      </div>

      {tipoPago === 'Efectivo' && (
        <div className="space-y-3">
          <Field label="Efectivo recibido"><input autoFocus type="number" min="0" className={inputCls} value={recibido} onChange={(e) => setRecibido(e.target.value)} /></Field>
          <div className="flex flex-wrap gap-2">
            {sugeridos.map((s) => <button key={s} className="rounded-full border px-3 py-1 text-xs hover:bg-gray-50" onClick={() => setRecibido(String(s))}>{money(s)}</button>)}
          </div>
          <p className="text-sm">Cambio a devolver: <b className={cambio ? 'text-pikis-green' : ''}>{money(cambio)}</b></p>
        </div>
      )}

      {(tipoPago === 'Nequi' || tipoPago === 'Daviplata') && (
        <p className="rounded-lg bg-blue-50 p-3 text-sm text-blue-800">Confirma que el pago de <b>{money(total)}</b> ya llegó a {tipoPago} antes de registrar la venta.</p>
      )}

      {tipoPago === 'Credito' && (
        <div className="space-y-2">
          <Field label="Cliente">
            <select className={inputCls} value={clienteId} onChange={(e) => setClienteId(e.target.value)}>
              <option value="">Selecciona un cliente…</option>
              {(clientes.data ?? []).map((c) => <option key={c.id} value={c.id}>{c.nombre} — disponible {money(c.creditoDisponible)}</option>)}
            </select>
          </Field>
          {excede && <p className="text-sm text-red-600">Supera el crédito disponible ({money(cliente.creditoDisponible)}).</p>}
        </div>
      )}

      <label className="mt-4 flex items-center gap-2 text-sm text-gray-600">
        <input type="checkbox" checked={imprimir} onChange={(e) => setImprimir(e.target.checked)} /> Imprimir tiquete en la térmica
      </label>
      <button className={btnPrimary + ' mt-4 w-full'} disabled={!valido || busy} onClick={confirmar}>{busy ? 'Registrando…' : 'Confirmar pago'}</button>
    </Modal>
  )
}
