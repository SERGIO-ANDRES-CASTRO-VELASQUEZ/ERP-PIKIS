import { Routes, Route, Navigate } from 'react-router-dom'
import { useAuthStore } from './store/authStore'
import Layout from './components/common/Layout'
import PrivateRoute from './components/common/PrivateRoute'
import Login from './pages/Login'
import Dashboard from './pages/Dashboard'
import POS from './pages/POS'
import Caja from './pages/Caja'
import Inventario from './pages/Inventario'
import Mermas from './pages/Mermas'
import Proveedores from './pages/Proveedores'
import Clientes from './pages/Clientes'
import Empleados from './pages/Empleados'
import Alertas from './pages/Alertas'

const ADMIN = ['Administrador']

export default function App() {
  const user = useAuthStore((s) => s.user)
  const home = user?.rol === 'Administrador' ? '/dashboard' : '/pos'
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route element={<PrivateRoute><Layout /></PrivateRoute>}>
        <Route path="/dashboard"   element={<PrivateRoute roles={ADMIN}><Dashboard /></PrivateRoute>} />
        <Route path="/pos"         element={<POS />} />
        <Route path="/caja"        element={<Caja />} />
        <Route path="/clientes"    element={<Clientes />} />
        <Route path="/inventario"  element={<PrivateRoute roles={ADMIN}><Inventario /></PrivateRoute>} />
        <Route path="/mermas"      element={<PrivateRoute roles={ADMIN}><Mermas /></PrivateRoute>} />
        <Route path="/proveedores" element={<PrivateRoute roles={ADMIN}><Proveedores /></PrivateRoute>} />
        <Route path="/empleados"   element={<PrivateRoute roles={ADMIN}><Empleados /></PrivateRoute>} />
        <Route path="/alertas"     element={<PrivateRoute roles={ADMIN}><Alertas /></PrivateRoute>} />
      </Route>
      <Route path="*" element={<Navigate to={user ? home : '/login'} replace />} />
    </Routes>
  )
}
