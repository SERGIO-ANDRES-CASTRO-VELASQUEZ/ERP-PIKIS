import api from './api'
const d = (r) => r.data
export const getUsuarios = () => api.get('/api/usuarios').then(d)
export const crearUsuario = (b) => api.post('/api/usuarios', b).then(d)
export const activarUsuario = (id) => api.put(`/api/usuarios/${id}/activar`)
export const desactivarUsuario = (id) => api.put(`/api/usuarios/${id}/desactivar`)
