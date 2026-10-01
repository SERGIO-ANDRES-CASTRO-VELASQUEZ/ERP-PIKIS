import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

// El backend .NET corre en http://localhost:5105 (ver launchSettings.json).
// Con el proxy, el front llama a /api/... y no hay problemas de CORS.
// Si la API está en otro equipo, define VITE_PROXY_TARGET en .env (p. ej. http://192.168.1.20:5105).
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  return {
    plugins: [react()],
    server: {
      port: 3000,
      proxy: {
        '/api': { target: env.VITE_PROXY_TARGET || 'http://localhost:5105', changeOrigin: true },
      },
    },
  }
})
