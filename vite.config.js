import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// El backend .NET corre en http://localhost:5105 (ver launchSettings.json).
// Con el proxy, el front llama a /api/... y no hay problemas de CORS.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      '/api': { target: 'http://localhost:5105', changeOrigin: true },
    },
  },
})
