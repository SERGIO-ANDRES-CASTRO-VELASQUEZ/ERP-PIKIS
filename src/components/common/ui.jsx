import { useEffect, useState, useCallback } from 'react'
import { X, Loader2 } from 'lucide-react'
import clsx from 'clsx'
import toast from 'react-hot-toast'
import { errMsg } from '../../services/api'

export function Modal({ title, onClose, children, wide }) {
  useEffect(() => {
    const h = (e) => e.key === 'Escape' && onClose()
    window.addEventListener('keydown', h)
    return () => window.removeEventListener('keydown', h)
  }, [onClose])
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4" onMouseDown={onClose}>
      <div className={clsx('w-full rounded-xl bg-white shadow-xl', wide ? 'max-w-3xl' : 'max-w-md')} onMouseDown={(e) => e.stopPropagation()}>
        <div className="flex items-center justify-between border-b px-5 py-3">
          <h3 className="font-semibold text-gray-900">{title}</h3>
          <button onClick={onClose} aria-label="Cerrar" className="rounded p-1 hover:bg-gray-100"><X size={18} /></button>
        </div>
        <div className="max-h-[75vh] overflow-y-auto p-5">{children}</div>
      </div>
    </div>
  )
}

export const Field = ({ label, children }) => (
  <label className="block text-sm">
    <span className="mb-1 block font-medium text-gray-700">{label}</span>
    {children}
  </label>
)

export const inputCls = 'w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-pikis-blue focus:outline-none focus:ring-2 focus:ring-pikis-blue/20'
export const btnPrimary = 'inline-flex items-center justify-center gap-2 rounded-lg bg-pikis-blue px-4 py-2 text-sm font-medium text-white hover:bg-primary-700 disabled:cursor-not-allowed disabled:opacity-50'
export const btnGhost = 'inline-flex items-center justify-center gap-2 rounded-lg border border-gray-300 px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50'
export const btnDanger = 'inline-flex items-center justify-center gap-2 rounded-lg bg-pikis-red px-4 py-2 text-sm font-medium text-white hover:opacity-90 disabled:opacity-50'

const tones = { green: 'bg-green-100 text-green-800', red: 'bg-red-100 text-red-800', yellow: 'bg-yellow-100 text-yellow-800', gray: 'bg-gray-100 text-gray-700', blue: 'bg-blue-100 text-blue-800' }
export const Badge = ({ tone = 'gray', children }) => (
  <span className={clsx('whitespace-nowrap rounded-full px-2 py-0.5 text-xs font-medium', tones[tone])}>{children}</span>
)

export function Tabs({ tabs, value, onChange }) {
  return (
    <div className="mb-5 flex gap-1 overflow-x-auto border-b">
      {tabs.map((t) => (
        <button key={t.id} onClick={() => onChange(t.id)}
          className={clsx('-mb-px whitespace-nowrap border-b-2 px-4 py-2 text-sm font-medium', value === t.id ? 'border-pikis-blue text-pikis-blue' : 'border-transparent text-gray-500 hover:text-gray-800')}>
          {t.label}
        </button>
      ))}
    </div>
  )
}

export const Table = ({ head, children, empty }) => (
  <div className="overflow-x-auto rounded-xl border bg-white">
    <table className="w-full text-left text-sm">
      <thead className="bg-gray-50 text-gray-600"><tr>{head.map((h, i) => <th key={i} className="whitespace-nowrap px-4 py-2.5 font-medium">{h}</th>)}</tr></thead>
      <tbody className="divide-y">{children}</tbody>
    </table>
    {empty && <p className="p-6 text-center text-sm text-gray-500">{empty}</p>}
  </div>
)

export const Kpi = ({ label, value, tone = 'text-gray-900', sub }) => (
  <div className="rounded-xl border bg-white p-4">
    <p className="text-xs text-gray-500">{label}</p>
    <p className={clsx('mt-1 text-xl font-bold', tone)}>{value}</p>
    {sub && <p className="mt-0.5 text-xs text-gray-400">{sub}</p>}
  </div>
)

export const Spinner = () => <div className="flex justify-center p-10 text-gray-400"><Loader2 className="animate-spin" /></div>

export const PageHeader = ({ title, subtitle, children }) => (
  <div className="mb-5 flex flex-wrap items-end justify-between gap-3">
    <div>
      <h1 className="text-2xl font-bold text-gray-900">{title}</h1>
      {subtitle && <p className="text-sm text-gray-500">{subtitle}</p>}
    </div>
    <div className="flex flex-wrap items-center gap-2">{children}</div>
  </div>
)

/** Carga datos con loading/error. `fn` debe devolver una promesa; `deps` re-dispara la carga. */
export function useFetch(fn, deps = []) {
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  // eslint-disable-next-line react-hooks/exhaustive-deps
  const run = useCallback(async () => {
    setLoading(true); setError(null)
    try { setData(await fn()) } catch (e) { setError(errMsg(e)) } finally { setLoading(false) }
  }, deps)
  useEffect(() => { run() }, [run])
  return { data, loading, error, reload: run, setData }
}

/** Ejecuta una acción async mostrando toast de error y bloqueando doble clic. */
export function useAccion() {
  const [busy, setBusy] = useState(false)
  const ejecutar = async (fn, okMsg) => {
    if (busy) return
    setBusy(true)
    try { const r = await fn(); if (okMsg) toast.success(okMsg); return r }
    catch (e) { toast.error(errMsg(e)) }
    finally { setBusy(false) }
  }
  return { busy, ejecutar }
}

export const ErrorBox = ({ msg, onRetry }) => (
  <div className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">
    {msg} {onRetry && <button className="ml-2 underline" onClick={onRetry}>Reintentar</button>}
  </div>
)
