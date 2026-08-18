import path from "node:path"
import { defineConfig } from "vite"
import react from "@vitejs/plugin-react"
import tailwindcss from "@tailwindcss/vite"

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      "@": path.resolve(import.meta.dirname, "./src"),
    },
  },
  server: {
    proxy: {
      "/api": {
        // IPv4 explícito: em alguns setups de WSL (rede mirrored), "localhost" tenta
        // IPv6 primeiro e demora ~20s até cair para IPv4 antes de conectar.
        target: process.env.VITE_API_BASE_URL ?? "http://127.0.0.1:5166",
        changeOrigin: true,
      },
      "/webhooks": {
        // IPv4 explícito: em alguns setups de WSL (rede mirrored), "localhost" tenta
        // IPv6 primeiro e demora ~20s até cair para IPv4 antes de conectar.
        target: process.env.VITE_API_BASE_URL ?? "http://127.0.0.1:5166",
        changeOrigin: true,
      },
    },
  },
})
