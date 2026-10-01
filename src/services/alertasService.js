import api from './api'
const d = (r) => r.data
export const getResumen = () => api.get('/api/alertas/resumen').then(d)
export const getAlertas = (params) => api.get('/api/alertas', { params }).then(d)
export const marcarLeida = (id) => api.put(`/api/alertas/${id}/leida`)
export const marcarTodas = () => api.put('/api/alertas/leer-todas')
export const revisarAhora = () => api.post('/api/alertas/revisar')
export const getEstadoWhatsApp = () => api.get('/api/alertas/whatsapp/estado').then(d)
export const getNotificaciones = (estado) => api.get('/api/alertas/whatsapp/notificaciones', { params: estado ? { estado } : {} }).then(d)
export const getDestinatarios = () => api.get('/api/alertas/destinatarios').then(d)
export const crearDestinatario = (b) => api.post('/api/alertas/destinatarios', b).then(d)
export const actualizarDestinatario = (id, b) => api.put(`/api/alertas/destinatarios/${id}`, b).then(d)
export const eliminarDestinatario = (id) => api.delete(`/api/alertas/destinatarios/${id}`)
export const probarDestinatario = (id) => api.post(`/api/alertas/destinatarios/${id}/prueba`)
