import api from './api'
export const login = (username, password) => api.post('/api/auth/login', { username, password }).then((r) => r.data)
export const perfil = () => api.get('/api/auth/perfil').then((r) => r.data)
export const cambiarPassword = (passwordActual, passwordNueva) => api.post('/api/auth/cambiar-password', { passwordActual, passwordNueva })
