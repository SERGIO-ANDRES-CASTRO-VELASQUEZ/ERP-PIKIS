import api from './api'
const d = (r) => r.data
export const getClientes = (busqueda) => api.get('/api/clientes', { params: busqueda ? { busqueda } : {} }).then(d)
export const crearCliente = (b) => api.post('/api/clientes', b).then(d)
export const actualizarCliente = (id, b) => api.put(`/api/clientes/${id}`, b).then(d)
export const registrarAbono = (id, monto, nota) => api.post(`/api/clientes/${id}/abonos`, { monto: Number(monto), nota: nota || null }).then(d)
