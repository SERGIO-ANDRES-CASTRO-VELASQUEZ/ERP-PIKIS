import { create } from 'zustand'
import * as auth from '../services/authService'

const leer = () => { try { return JSON.parse(localStorage.getItem('usuario')) } catch { return null } }

export const useAuthStore = create((set, get) => ({
  token: localStorage.getItem('token'),
  user: leer(),            // { id, nombre, username, rol: 'Administrador' | 'Cajero', activo }
  login: async (username, password) => {
    const r = await auth.login(username, password)
    localStorage.setItem('token', r.token)
    localStorage.setItem('usuario', JSON.stringify(r.usuario))
    set({ token: r.token, user: r.usuario })
    return r.usuario
  },
  logout: () => {
    localStorage.removeItem('token'); localStorage.removeItem('usuario')
    set({ token: null, user: null })
  },
  isAdmin: () => get().user?.rol === 'Administrador',
}))
