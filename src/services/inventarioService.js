import api from './api'
const d = (r) => r.data
export const getProductos = (params) => api.get('/api/productos', { params }).then(d)
export const getProductoPorCodigo = (codigo) => api.get(`/api/productos/codigo/${encodeURIComponent(codigo)}`).then(d)
export const crearProducto = (b) => api.post('/api/productos', b).then(d)
export const actualizarProducto = (id, b) => api.put(`/api/productos/${id}`, b).then(d)
export const desactivarProducto = (id) => api.delete(`/api/productos/${id}`)
export const registrarEntrada = (id, b) => api.post(`/api/productos/${id}/entradas`, b).then(d)
export const registrarAjuste = (id, b) => api.post(`/api/productos/${id}/ajustes`, b).then(d)
export const getMovimientos = (id) => api.get(`/api/productos/${id}/movimientos`).then(d)
export const getCategorias = () => api.get('/api/categorias').then(d)
export const crearCategoria = (nombre) => api.post('/api/categorias', { nombre }).then(d)
