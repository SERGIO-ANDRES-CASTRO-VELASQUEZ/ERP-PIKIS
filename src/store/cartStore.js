import { create } from 'zustand'

// items: { productoId, nombre, precio, stock, cantidad } — el total real lo calcula el backend.
export const useCartStore = create((set, get) => ({
  items: [],
  add: (p) => {
    const items = get().items
    const ex = items.find((i) => i.productoId === p.id)
    if (ex) {
      if (ex.cantidad >= p.stockActual) return false
      set({ items: items.map((i) => (i.productoId === p.id ? { ...i, cantidad: i.cantidad + 1 } : i)) })
    } else {
      if (p.stockActual < 1) return false
      set({ items: [...items, { productoId: p.id, nombre: p.nombre, precio: p.precioVenta, stock: p.stockActual, cantidad: 1 }] })
    }
    return true
  },
  setCantidad: (id, cantidad) =>
    set({ items: get().items.map((i) => (i.productoId === id ? { ...i, cantidad: Math.max(1, Math.min(i.stock, cantidad)) } : i)) }),
  remove: (id) => set({ items: get().items.filter((i) => i.productoId !== id) }),
  clear: () => set({ items: [] }),
  total: () => get().items.reduce((s, i) => s + i.precio * i.cantidad, 0),
}))
