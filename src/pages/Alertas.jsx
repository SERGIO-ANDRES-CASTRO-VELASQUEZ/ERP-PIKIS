import { useState } from 'react'
import toast from 'react-hot-toast'
import { RefreshCw, CheckCheck, Plus, Send, Trash2 } from 'lucide-react'
import * as al from '../services/alertasService'
import { errMsg } from '../services/api'
import { fechaHora } from '../utils/formatters'
import { Modal, Field, inputCls, btnPrimary, btnGhost, Badge, Tabs, Table, Spinner, ErrorBox, PageHeader, useFetch, useAccion } from '../components/common/ui'

const SEV = { Critica: 'red', Advertencia: 'yellow', Info: 'blue' }

function TabAlertas() {
  const [soloNoLeidas, setSoloNoLeidas] = useState(true)
  const { data, loading, error, reload } = useFetch(() => al.getAlertas(soloNoLeidas ? { soloNoLeidas: true } : {}), [soloNoLeidas])
  const { busy, ejecutar } = useAccion()
  return (
    <>
      <div className="mb-3 flex flex-wrap items-center gap-3">
        <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={soloNoLeidas} onChange={(e) => setSoloNoLeidas(e.target.checked)} /> Solo no leídas</label>
        <button className={btnGhost} disabled={busy} onClick={() => ejecutar(async () => { await al.revisarAhora(); reload() }, 'Revisión completada')}><RefreshCw size={15} /> Revisar ahora</button>
        <button className={btnGhost} disabled={busy} onClick={() => ejecutar(async () => { await al.marcarTodas(); reload() }, 'Todas marcadas como leídas')}><CheckCheck size={15} /> Marcar todas leídas</button>
      </div>
      {error ? <ErrorBox msg={error} onRetry={reload} /> : loading && !data ? <Spinner /> : (
        <div className="space-y-2">
          {(data ?? []).length === 0 && <p className="rounded-xl border bg-white p-6 text-center text-sm text-gray-500">No hay alertas.</p>}
          {(data ?? []).map((a) => (
            <div key={a.id} className={'flex items-start gap-3 rounded-xl border bg-white p-4 ' + (a.leida ? 'opacity-60' : '')}>
              <Badge tone={SEV[a.severidad]}>{a.severidad}</Badge>
              <div className="min-w-0 flex-1">
                <p className="font-medium">{a.titulo} {!a.activa && <span className="text-xs font-normal text-gray-400">(resuelta)</span>}</p>
                <p className="whitespace-pre-line text-sm text-gray-600">{a.mensaje}</p>
                <p className="mt-1 text-xs text-gray-400">{fechaHora(a.fecha)}</p>
              </div>
              {!a.leida && <button className={btnGhost} onClick={() => al.marcarLeida(a.id).then(reload).catch((e) => toast.error(errMsg(e)))}>Leída</button>}
            </div>
          ))}
        </div>
      )}
    </>
  )
}

function DestinatarioForm({ dest, onClose, onDone }) {
  const [f, setF] = useState({ nombre: dest?.nombre ?? '', telefono: dest?.telefono ?? '', apiKey: '', activo: dest?.activo ?? true })
  const { busy, ejecutar } = useAccion()
  return (
    <Modal title={dest ? 'Editar destinatario' : 'Nuevo destinatario WhatsApp'} onClose={onClose}>
      <form className="space-y-3" onSubmit={(e) => { e.preventDefault()
        ejecutar(async () => {
          if (dest) await al.actualizarDestinatario(dest.id, { nombre: f.nombre, telefono: f.telefono, apiKey: f.apiKey || null, activo: f.activo })
          else await al.crearDestinatario({ nombre: f.nombre, telefono: f.telefono, apiKey: f.apiKey || null })
          onDone(); onClose()
        }, 'Guardado') }}>
        <Field label="Nombre"><input required className={inputCls} value={f.nombre} onChange={(e) => setF({ ...f, nombre: e.target.value })} /></Field>
        <Field label="Teléfono (con indicativo o 10 dígitos)"><input required className={inputCls} value={f.telefono} onChange={(e) => setF({ ...f, telefono: e.target.value })} /></Field>
        <Field label={dest ? 'API key de CallMeBot (vacío = no cambiar)' : 'API key de CallMeBot'}><input className={inputCls} value={f.apiKey} onChange={(e) => setF({ ...f, apiKey: e.target.value })} /></Field>
        {dest && <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={f.activo} onChange={(e) => setF({ ...f, activo: e.target.checked })} /> Activo</label>}
        <details className="rounded-lg bg-gray-50 p-3 text-xs text-gray-600">
          <summary className="cursor-pointer font-medium">¿Cómo obtengo la API key?</summary>
          <ol className="mt-2 list-decimal space-y-1 pl-4">
            <li>Guarda el contacto del bot de CallMeBot (verifica el número vigente en callmebot.com).</li>
            <li>Envíale por WhatsApp: <i>I allow callmebot to send me messages</i>.</li>
            <li>Espera la respuesta con tu APIKEY y pégala aquí.</li>
          </ol>
        </details>
        <div className="flex justify-end"><button className={btnPrimary} disabled={busy}>Guardar</button></div>
      </form>
    </Modal>
  )
}

function TabWhatsApp() {
  const estado = useFetch(() => al.getEstadoWhatsApp(), [])
  const dests = useFetch(() => al.getDestinatarios(), [])
  const fallidas = useFetch(() => al.getNotificaciones('Fallida'), [])
  const [modal, setModal] = useState(null)
  const accion = (fn, ok) => fn().then(() => { toast.success(ok); dests.reload() }).catch((e) => toast.error(errMsg(e)))
  return (
    <>
      {estado.data && <p className="mb-3 text-sm text-gray-600">Proveedor: <b>{estado.data.proveedor}</b> · {estado.data.habilitado ? <Badge tone="green">Habilitado</Badge> : <Badge tone="red">Deshabilitado</Badge>} · {estado.data.destinatariosActivos} destinatarios activos</p>}
      <div className="mb-3 flex justify-end"><button className={btnPrimary} onClick={() => setModal({})}><Plus size={16} /> Agregar número</button></div>
      {dests.error ? <ErrorBox msg={dests.error} onRetry={dests.reload} /> : (
        <Table head={['Nombre', 'Teléfono', 'API key', 'Estado', '']} empty={(dests.data ?? []).length ? null : 'No hay números registrados.'}>
          {(dests.data ?? []).map((d) => (
            <tr key={d.id}>
              <td className="px-4 py-2.5 font-medium">{d.nombre}</td><td className="px-4 py-2.5">{d.telefono}</td>
              <td className="px-4 py-2.5">{d.tieneApiKey ? <Badge tone="green">Configurada</Badge> : <Badge tone="yellow">Falta</Badge>}</td>
              <td className="px-4 py-2.5">{d.activo ? <Badge tone="green">Activo</Badge> : <Badge>Inactivo</Badge>}</td>
              <td className="space-x-2 whitespace-nowrap px-4 py-2.5 text-right">
                <button className={btnGhost} onClick={() => accion(() => al.probarDestinatario(d.id), 'Mensaje de prueba enviado')}><Send size={14} /> Probar</button>
                <button className={btnGhost} onClick={() => setModal(d)}>Editar</button>
                <button className={btnGhost} onClick={() => window.confirm(`¿Eliminar a ${d.nombre}?`) && accion(() => al.eliminarDestinatario(d.id), 'Eliminado')}><Trash2 size={14} /></button>
              </td>
            </tr>
          ))}
        </Table>
      )}
      <h3 className="mb-2 mt-6 font-semibold">Mensajes que no se pudieron entregar</h3>
      <Table head={['Fecha', 'Destinatario', 'Intentos', 'Error']} empty={(fallidas.data ?? []).length ? null : 'Sin mensajes fallidos.'}>
        {(fallidas.data ?? []).map((n) => <tr key={n.id}><td className="px-4 py-2">{fechaHora(n.fechaCreacion)}</td><td className="px-4 py-2">{n.destinatario}</td><td className="px-4 py-2">{n.intentos}</td><td className="px-4 py-2 text-red-600">{n.ultimoError ?? '—'}</td></tr>)}
      </Table>
      {modal && <DestinatarioForm dest={modal.id ? modal : null} onClose={() => setModal(null)} onDone={() => { dests.reload(); estado.reload() }} />}
    </>
  )
}

export default function Alertas() {
  const [tab, setTab] = useState('alertas')
  return (
    <div>
      <PageHeader title="Alertas" subtitle="Stock, caja, cartera y avisos por WhatsApp." />
      <Tabs value={tab} onChange={setTab} tabs={[{ id: 'alertas', label: 'Alertas' }, { id: 'wa', label: 'WhatsApp' }]} />
      {tab === 'alertas' ? <TabAlertas /> : <TabWhatsApp />}
    </div>
  )
}
