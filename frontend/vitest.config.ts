import { fileURLToPath } from 'node:url';
import { defineConfig } from 'vitest/config';

const src = (path: string) => fileURLToPath(new URL(`./src/app/${path}`, import.meta.url));

/**
 * Pruebas unitarias de la lógica pura (carpetas domain/ y shared/utils/).
 * No necesitan navegador ni TestBed: se ejecutan en Node en milisegundos.
 */
export default defineConfig({
  resolve: {
    // Los mismos alias de tsconfig.json.
    alias: { '@core': src('core'), '@shared': src('shared'), '@features': src('features') }
  },
  test: {
    include: ['src/**/*.spec.ts'],
    environment: 'node'
  }
});
