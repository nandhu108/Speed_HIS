import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 3000,
    proxy: {
      // Dev-only: forwards API calls to the local .NET API so no CORS setup is
      // needed in the browser. Production uses the same-origin /api path via nginx.
      '/api': {
        target: process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:5000',
        changeOrigin: true,
      },
      '/health': {
        target: process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
})
