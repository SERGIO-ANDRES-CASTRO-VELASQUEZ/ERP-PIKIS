// Cliente Axios → backend .NET (JWT en Authorization). Ver FRONTEND.md del backend.
import axios from 'axios'

const api = axios.create({ baseURL: import.meta.env.VITE_API_URL ?? '' })

api.interceptors.request.use((cfg) => {
  const token = localStorage.getItem('token')
  if (token) cfg.headers.Authorization = `Bearer ${token}`
  return cfg
})

api.interceptors.response.use(
  (r) => r,
  (err) => {
    const { response, config } = err
    // Sesión vencida → login (no aplica al propio login)
    if (response?.status === 401 && !config?.url?.includes('/api/auth/login')) {
      localStorage.removeItem('token')
      localStorage.removeItem('usuario')
      if (window.location.pathname !== '/login') window.location.href = '/login'
    }
    // 409 = otro usuario modificó el mismo registro: se reintenta una vez
    if (response?.status === 409 && config && !config._reintentado) {
      config._reintentado = true
      return api(config)
    }
    return Promise.reject(err)
  }
)

/** Mensaje en español apto para mostrar al usuario. */
export function errMsg(err) {
  const r = err?.response
  if (!r) return 'No hay conexión con el servidor. Verifica que el backend esté encendido.'
  const b = r.data
  if (b?.errors) return Object.values(b.errors).flat().join(' ')
  if (b?.detail) return b.detail
  if (r.status === 401) return 'Su sesión expiró. Ingrese de nuevo.'
  if (r.status === 403) return 'No tiene permiso para esta acción.'
  if (r.status === 503) return 'La impresora no responde. Revise la impresora.'
  return 'Error inesperado. Intente de nuevo.'
}

export default api
