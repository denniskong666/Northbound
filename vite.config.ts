import { defineConfig } from 'vite';

// 向北 Northbound - Vite 配置
export default defineConfig({
  base: './',
  server: {
    open: true,
    port: 5173
  },
  build: {
    target: 'es2020',
    outDir: 'dist'
  }
});
