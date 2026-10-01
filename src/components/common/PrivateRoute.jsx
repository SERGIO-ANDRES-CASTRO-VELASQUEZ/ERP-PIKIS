import { Navigate } from 'react-router-dom'
import { useAuthStore } from '../../store/authStore'

export default function PrivateRoute({ roles, children }) {
  const { token, user } = useAuthStore()
  if (!token || !user) return <Navigate to="/login" replace />
  if (roles && !roles.includes(user.rol)) return <Navigate to={user.rol === 'Administrador' ? '/dashboard' : '/pos'} replace />
  return children
}
