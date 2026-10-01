import api from './api'
const d = (r) => r.data
export const registrarVenta = (b) => api.post('/api/ventas', b).then(d)          // → { venta, ticketImpreso, errorImpresion }
export const getVentas = (params) => api.get('/api/ventas', { params }).then(d)   // resumen: desde, hasta, usuarioId
export const getVenta = (id) => api.get(`/api/ventas/${id}`).then(d)
export const reimprimir = (id) => api.post(`/api/ventas/${id}/imprimir`)
export const getTicketPdf = (id, ancho = 80) =>
  api.get(`/api/ventas/${id}/ticket/pdf`, { params: { ancho }, responseType: 'blob' }).then(d)

/** Abre el PDF del tiquete en una pestaña nueva (requiere token, por eso se baja como blob). */
export async function abrirTicketPdf(id, ancho = 80) {
  const blob = await getTicketPdf(id, ancho)
  window.open(URL.createObjectURL(blob), '_blank')
}
