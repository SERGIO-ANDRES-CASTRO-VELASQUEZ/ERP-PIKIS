import { create } from 'zustand'
import * as caja from '../services/cajaService'

export const useCajaStore = create((set) => ({
  turno: null,       // TurnoCajaDto | null
  cargado: false,
  refrescar: async () => { const t = await caja.getTurnoActual(); set({ turno: t, cargado: true }); return t },
  abrir: async (monto) => { const t = await caja.abrirCaja(monto); set({ turno: t, cargado: true }); return t },
  egreso: async (monto, desc) => { const t = await caja.registrarEgreso(monto, desc); set({ turno: t }); return t },
  cerrar: async (contado) => { const t = await caja.cerrarCaja(contado); set({ turno: null }); return t },
  reset: () => set({ turno: null, cargado: false }),
}))
