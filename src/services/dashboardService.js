import api from './api'
export const getDashboard = (fecha) => api.get('/api/dashboard', { params: fecha ? { fecha } : {} }).then((r) => r.data)
