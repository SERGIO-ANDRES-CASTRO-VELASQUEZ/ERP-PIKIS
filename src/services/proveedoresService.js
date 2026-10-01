import api from './api'
const d = (r) => r.data
export const getProveedores = (incluirInactivos = false) => api.get('/api/proveedores', { params: incluirInactivos ? { incluirInactivos } : {} }).then(d)
export const crearProveedor = (b) => api.post('/api/proveedores', b).then(d)
export const actualizarProveedor = (id, b) => api.put(`/api/proveedores/${id}`, b).then(d)
export const getFacturas = (params) => api.get('/api/proveedores/facturas', { params }).then(d)
export const getFactura = (id) => api.get(`/api/proveedores/facturas/${id}`).then(d)
// items: [{ productoId, cantidad, costoUnitario }] → suben el stock en la misma transacción. Sin items se usa `total`.
export const registrarFactura = (b) => api.post('/api/proveedores/facturas', b).then(d)
// metodo: 'Efectivo' | 'Nequi' | 'Daviplata' | 'Transferencia'. descontarDeCaja solo aplica a Efectivo.
export const registrarPago = (facturaId, b) => api.post(`/api/proveedores/facturas/${facturaId}/pagos`, b).then(d)
export const getPagos = (desde, hasta) => api.get('/api/proveedores/pagos', { params: { desde, hasta } }).then(d)
