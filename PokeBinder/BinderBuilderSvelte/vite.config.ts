import { svelte } from '@sveltejs/vite-plugin-svelte'
import tailwindcss from '@tailwindcss/vite'
import { defineConfig } from 'vite'
import ViteDotNetPlugin from 'vite-dotnet'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    svelte(),
    tailwindcss(),
    ViteDotNetPlugin('src/main.ts', 'app'),
  ],
})
