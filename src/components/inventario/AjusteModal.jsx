import { useState } from 'react'
import { Modal, Field, inputCls, btnPrimary, btnGhost, useAccion } from '../common/ui'
import { registrarAjuste } from '../../services/inventarioService'
import { money } from '../../utils/formatters'

// El backend guarda un ajuste con (diferencia, motivo). El tipo de merma se codifica como prefijo "[Tipo] ".
export const TIPOS_MERMA = {
  averia:       { label: 'Avería / daño',      signo: -1, prefijo: '[Avería]',       tone: 'red' },
  vencimiento:  { label: 'Vencimiento',        signo: -1, prefijo: '[Vencimiento]',  tone: 'yellow' },
  consumo:      { label: 'Consumo interno',    signo: -1, prefijo: '[Consumo]',      tone: 'gray' },
  perdida:      { label: 'Pérdida / faltante', signo: -1, prefijo: '[Pérdida]',      tone: 'red' },
  ajuste_menos: { label: 'Ajuste manual (−)',  signo: -1, prefijo: '[Ajuste-]',      tone: 'blue' },
  ajuste_mas:   { label: 'Ajuste manual (+)',  signo: 1,  prefijo: '[Ajuste+]',      tone: 'green' },
}
export const parseMotivo = (motivo = '') => {
  for (const [k, t] of Object.entries(TIPOS_MERMA)) if (motivo.startsWith(t.prefijo)) return { tipo: k, texto: motivo.slice(t.prefijo.length).trim() }
  return { tipo: null, texto: motivo }
}

/** `producto` fijo, o `productos` para elegir. */
export default function AjusteModal({ producto, productos = [], onClose, onDone }) {
  const [f, setF] = useState({ tipo: 'averia', productoId: producto?.id ?? '', cantidad: 1, motivo: '' })
  const { busy, ejecutar } = useAccion()
  const p = producto ?? productos.find((x) => String(x.id) === String(f.productoId))
  const t = TIPOS_MERMA[f.tipo]

  const submit = (e) => {
    e.preventDefault()
    if (!p) return
    ejecutar(async () => {
      await registrarAjuste(p.id, { diferencia: t.signo * Number(f.cantidad), motivo: `${t.prefijo} ${f.motivo.trim()}`.slice(0, 250) })
      onDone?.(); onClose()
    }, 'Ajuste registrado')
  }

  return (
    <Modal title="Registrar merma o ajuste" onClose={onClose}>
      <form onSubmit={submit} className="space-y-3">
        {producto ? <p className="text-sm"><b>{producto.nombre}</b> — stock actual {producto.stockActual}</p> : (
          <Field label="Producto">
            <select required className={inputCls} value={f.productoId} onChange={(e) => setF({ ...f, productoId: e.target.value })}>
              <option value="">Selecciona…</option>
              {productos.map((x) => <option key={x.id} value={x.id}>{x.nombre} (stock {x.stockActual})</option>)}
            </select>
          </Field>
        )}
        <Field label="Tipo">
          <select className={inputCls} value={f.tipo} onChange={(e) => setF({ ...f, tipo: e.target.value })}>
            {Object.entries(TIPOS_MERMA).map(([k, x]) => <option key={k} value={k}>{x.label}</option>)}
          </select>
        </Field>
        <Field label="Cantidad"><input required type="number" min="1" className={inputCls} value={f.cantidad} onChange={(e) => setF({ ...f, cantidad: e.target.value })} /></Field>
        <Field label="Motivo (queda como auditoría)"><textarea required rows={2} maxLength={200} className={inputCls} value={f.motivo} onChange={(e) => setF({ ...f, motivo: e.target.value })} /></Field>
        {p && <p className="text-xs text-gray-500">Stock resultante: {p.stockActual + t.signo * (Number(f.cantidad) || 0)} · Impacto a costo: {money(p.precioCompra * (Number(f.cantidad) || 0))}</p>}
        <div className="flex justify-end gap-2"><button type="button" className={btnGhost} onClick={onClose}>Cancelar</button><button className={btnPrimary} disabled={busy || !p}>Guardar</button></div>
      </form>
    </Modal>
  )
}
