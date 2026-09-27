# Partes CTG — Frontend

Tienda virtual de repuestos y mejoras para computador, hecha con **Angular 19** (componentes standalone,
signals y Angular Material). La API está en un repositorio aparte (.NET 9).

## Requisitos

- Node.js 20 o superior

## Uso

```bash
npm install
npm start          # http://localhost:4200 (las llamadas a /api van al destino de proxy.conf.json)
npm run check      # lint + pruebas + build
```

`proxy.conf.json` define a qué API se envían las peticiones `/api` durante el desarrollo
(la API local es `http://localhost:5080`).

## Despliegue en Vercel

`vercel.json` compila el proyecto (`dist/tienda-virtual-web/browser`) y **reenvía `/api/*` a la API en Render**
(`https://partescgt.onrender.com`). Así el código sigue llamando a `/api`, las imágenes (`/api/files/...`)
funcionan y no hace falta CORS. Si la API cambia de dirección, actualiza el `destination` de esa regla.
La segunda regla envía cualquier otra ruta a `index.html`, para que funcionen las URL de Angular al recargar.

## Estructura

Arquitectura por features (`core`, `shared`, `layout`, `features`). Las convenciones, las reglas de
dependencias y las decisiones de seguridad están en [ARCHITECTURE.md](ARCHITECTURE.md).
