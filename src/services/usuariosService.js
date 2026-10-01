import api from './api'
const d = (r) => r.data
export const getUsuarios = () => api.get('/api/usuarios').then(d)
export const crearUsuario = (b) => api.post('/api/usuarios', b).then(d)
export const actualizarUsuario = (id, b) => api.put(`/api/usuarios/${id}`, b).then(d)            // { nombre, rol }
export const restablecerPassword = (id, passwordNueva) => api.put(`/api/usuarios/${id}/password`, { passwordNueva })
export const activarUsuario = (id) => api.put(`/api/usuarios/${id}/activar`)
export const desactivarUsuario = (id) => api.put(`/api/usuarios/${id}/desactivar`)
