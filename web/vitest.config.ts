import { defineConfig } from 'vitest/config'

export default defineConfig({
  test: {
    server: {
      deps: {
        inline: ['@podvysotskyi/l2-runtime']
      }
    }
  }
})
