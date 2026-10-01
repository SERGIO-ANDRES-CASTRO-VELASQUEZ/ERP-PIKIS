import api from './api'
const d = (r) => r.data
export const getTurnoActual = async () => {
  const r = await api.get('/api/caja/turno-actual')
  return r.status === 204 || !r.data ? null : r.data   // 204 = sin caja abierta
}
export const abrirCaja = (montoInicial) => api.post('/api/caja/apertura', { montoInicial: Number(montoInicial) }).then(d)
export const registrarEgreso = (monto, descripcion) => api.post('/api/caja/egresos', { monto: Number(monto), descripcion }).then(d)
export const cerrarCaja = (montoContado) => api.post('/api/caja/cierre', { montoContado: Number(montoContado) }).then(d)
export const getTurnos = (desde, hasta) => api.get('/api/caja/turnos', { params: { desde, hasta } }).then(d)
export const getTurno = (id) => api.get(`/api/caja/turnos/${id}`).then(d)
