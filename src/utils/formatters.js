import { format } from 'date-fns'

const cop = new Intl.NumberFormat('es-CO', { style: 'currency', currency: 'COP', maximumFractionDigits: 0 })
export const money = (n) => cop.format(Number(n) || 0)
export const fechaHora = (iso) => (iso ? new Date(iso).toLocaleString('es-CO', { dateStyle: 'short', timeStyle: 'short' }) : '—')
export const fecha = (iso) => (iso ? new Date(iso).toLocaleDateString('es-CO') : '—')
/** Para DateOnly del backend ('yyyy-MM-dd'): new Date('2026-10-15') sería UTC y mostraría el día anterior. */
export const fechaDia = (ymd) => (ymd ? new Date(`${ymd}T00:00:00`).toLocaleDateString('es-CO') : '—')
/** yyyy-MM-dd en hora local (formato que espera el backend en filtros). */
export const dia = (d = new Date()) => format(d, 'yyyy-MM-dd')
export const hace = (dias) => { const d = new Date(); d.setDate(d.getDate() - dias); return dia(d) }
export const TIPOS_PAGO = { Efectivo: 'Efectivo', Nequi: 'Nequi', Daviplata: 'Daviplata', Credito: 'Crédito (fiado)' }