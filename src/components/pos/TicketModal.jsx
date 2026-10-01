import { useState } from 'react'
import toast from 'react-hot-toast'
import { FileText, Printer } from 'lucide-react'
import { money, fechaHora, TIPOS_PAGO } from '../../utils/formatters'
import { Modal, btnPrimary, btnGhost } from '../common/ui'
import { abrirTicketPdf, reimprimir } from '../../services/ventasService'
import { errMsg } from '../../services/api'

/** Vista previa del tiquete a partir de VentaDto del backend. */
export function Ticket({ venta }) {
  return (
    <div className="mx-auto w-[290px] bg-white p-3 font-mono text-[12px] leading-snug text-black">
      <div className="text-center">
        <p className="text-base font-bold">TIENDA PIKIS</p>
        <p>Tiquete de venta #{venta.id}</p>
        <p>{fechaHora(venta.fecha)}</p>
        <p>Cajero: {venta.cajero}</p>
      </div>
      <hr className="my-2 border-dashed border-black" />
      {venta.detalles.map((d, i) => (
        <div key={i} className="mb-1">
          <p>{d.producto}</p>
          <p className="flex justify-between"><span>{d.cantidad} x {money(d.precioUnitario)}</span><span>{money(d.subtotal)}</span></p>
        </div>
      ))}
      <hr className="my-2 border-dashed border-black" />
      <p className="flex justify-between text-sm font-bold"><span>TOTAL</span><span>{money(venta.total)}</span></p>
      <p className="flex justify-between"><span>Pago</span><span>{TIPOS_PAGO[venta.tipoPago] ?? venta.tipoPago}</span></p>
      {venta.montoRecibido != null && <>
        <p className="flex justify-between"><span>Recibido</span><span>{money(venta.montoRecibido)}</span></p>
        <p className="flex justify-between"><span>Cambio</span><span>{money(venta.cambio)}</span></p>
      </>}
      {venta.cliente && <p>Cliente: {venta.cliente}</p>}
      <p className="mt-3 text-center">¡Gracias por su compra!</p>
    </div>
  )
}

/** `resultado` = VentaRegistradaDto ({venta, ticketImpreso, errorImpresion}) o solo una venta. */
export default function TicketModal({ venta, ticketImpreso, errorImpresion, onClose }) {
  const [busy, setBusy] = useState(false)
  const run = async (fn, ok) => {
    setBusy(true)
    try { await fn(); if (ok) toast.success(ok) } catch (e) { toast.error(errMsg(e)) } finally { setBusy(false) }
  }
  return (
    <Modal title={`Venta #${venta.id} registrada`} onClose={onClose}>
      {errorImpresion && (
        <p className="mb-3 rounded-lg bg-yellow-50 p-3 text-sm text-yellow-800">
          La venta se guardó, pero el tiquete no se imprimió: {errorImpresion} Revisa la impresora y usa “Imprimir en térmica”.
        </p>
      )}
      {ticketImpreso && <p className="mb-3 rounded-lg bg-green-50 p-2 text-center text-sm text-green-700">Tiquete enviado a la impresora.</p>}
      <div className="rounded border bg-gray-50 p-2"><Ticket venta={venta} /></div>
      <div className="mt-4 flex flex-wrap justify-end gap-2">
        <button className={btnGhost} disabled={busy} onClick={() => run(() => abrirTicketPdf(venta.id, 80))}><FileText size={16} /> Ver PDF</button>
        <button className={btnGhost} disabled={busy} onClick={() => run(() => reimprimir(venta.id), 'Reimpreso')}><Printer size={16} /> Imprimir en térmica</button>
        <button className={btnPrimary} onClick={onClose}>Nueva venta</button>
      </div>
    </Modal>
  )
}
