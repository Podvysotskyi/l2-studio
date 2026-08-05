export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  css: ['~/assets/css/main.css'],
  devtools: { enabled: true },
  icon: {
    serverBundle: {
      collections: ['lucide']
    }
  },
  modules: ['@nuxt/ui', '@pinia/nuxt'],
  runtimeConfig: {
    public: {
      apiBase:
        process.env.NUXT_PUBLIC_STUDIO_API_BASE ?? 'http://localhost:5101'
    }
  },
  typescript: { typeCheck: true }
})
