// OJO: el backend actual NO tiene tablas de proveedores/facturas/pagos.
// Esta información se guarda en el navegador (localStorage). Las ENTRADAS de mercancía sí van al backend.
import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import { uid } from '../utils/formatters'

export const estadoFactura = (f) => {
  if (f.pagado >= f.total) return 'pagada'
  if (f.vence && new Date(f.vence) < new Date(new Date().toDateString())) return 'vencida'
  return f.pagado > 0 ? 'parcial' : 'pendiente'
}

export const useProveedores = create(
  persist(
    (set, get) => ({
      proveedores: [], facturas: [], pagos: [],
      crearProveedor: (p) => set({ proveedores: [...get().proveedores, { ...p, id: uid() }] }),
      crearFactura: (f) => set({ facturas: [{ ...f, id: uid(), total: Number(f.total), pagado: 0 }, ...get().facturas] }),
      registrarPago: ({ facturaId, monto, metodo, referencia, usuario }) => {
        const f = get().facturas.find((x) => x.id === facturaId)
        if (!f) return
        const m = Math.min(Number(monto), f.total - f.pagado)
        set({
          pagos: [{ id: uid(), facturaId, proveedorId: f.proveedorId, fecha: new Date().toISOString(), monto: m, metodo, referencia, usuario }, ...get().pagos],
          facturas: get().facturas.map((x) => (x.id === facturaId ? { ...x, pagado: x.pagado + m } : x)),
        })
      },
    }),
    { name: 'pikis-proveedores' }
  )
)
