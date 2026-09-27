# Arquitectura del frontend

Organizado por **features (slices verticales)**: cada funcionalidad tiene dentro todo lo que necesita,
desde la llamada HTTP hasta la pantalla. Agregar una funcionalidad es agregar una carpeta, no tocar diez.

```
src/app/
├─ core/        Transversal y sin reglas de negocio: auth, errores HTTP, toasts.
├─ shared/      Reutilizable y sin estado: <app-icon>, pipe | cop, paginación de tablas, utilidades de texto y almacenamiento.
├─ layout/      El cascarón: header y footer. Puede componer features.
└─ features/
   ├─ catalog/    Productos, categorías, filtros, menú de categorías.
   ├─ kits/       Kits por objetivo.
   ├─ home/       Página de inicio: compone portada + kits + catálogo.
   ├─ cart/       Carrito y pedidos.
   ├─ assistant/  Chat con Claude.
   └─ admin/      Panel de administración.
```

## Dentro de una feature

| Carpeta   | Contiene                                                          | Depende de Angular |
|-----------|-------------------------------------------------------------------|--------------------|
| `domain/` | Modelos y reglas puras (filtros, orden, kits). Fáciles de probar. | No                 |
| `data/`   | Clientes HTTP (`*.api.ts`) y stores globales (`*.store.ts`).       | Sí                 |
| `state/`  | Stores de componente (se proveen en `providers: []`).              | Sí                 |
| `ui/`     | Componentes reutilizables de la feature.                          | Sí                 |
| `pages/`  | Componentes enrutados.                                            | Sí                 |
| `index.ts`| **API pública**: lo único que otras features pueden importar.     | —                  |
| `*.routes.ts` | Rutas de la feature, cargadas bajo demanda desde `app.routes.ts`. | —              |

## Reglas

1. **Importa otras features solo desde su `index.ts`**: `import { CartStore } from '@features/cart'`,
   nunca `'@features/cart/data/cart.store'`. Si necesitas algo que no está exportado, decide si debe ser público.
2. **Dependencias hacia abajo**: `features` → `shared`/`core`. `shared` y `core` nunca importan features.
3. **Componentes presentacionales**: `catalog` y `kits` no conocen el carrito ni el asistente; emiten eventos
   (`(add)`, `(askAssistant)`) y la página (`home`) decide qué hacer. Así se pueden reutilizar en otra pantalla.
4. **Reglas de negocio en `domain/`** como funciones puras; los componentes solo las llaman.
5. **Estado con signals**. Estado global en stores `providedIn: 'root'`; estado de una pantalla en un store
   provisto por el componente (ver `ProductBrowserStore`).
6. **Componentes**: `ChangeDetectionStrategy.OnPush`, `input()`/`output()`/`model()`, plantilla y estilos en
   archivos `.html`/`.css` propios (las muy cortas pueden ir en línea).
7. **Sin números mágicos**: constantes con nombre (`LOW_STOCK`, `MAX_ATTRIBUTES`, `CHAT_CONTEXT_SIZE`…).
8. **Iconos** con `<app-icon name="…">`. El nombre está tipado: si agregas uno, súmalo a
   `shared/ui/icon/icon-names.ts` **y** a `icon_names` en `src/index.html`.
9. **Precios** con el pipe `| cop`; errores HTTP con `describeHttpError()`; avisos con `ToastService`.
10. **Tablas del panel** con Angular Material 19 (`MatTable` + `MatPaginator`), máximo 10 filas por página
    (`PAGE_SIZE`). La paginación es en el cliente con `paginatedList()` de `shared/table/`: recibe la lista ya
    filtrada y expone `rows()`, `pageIndex()` y `onPage()`; llama a `reset()` al cambiar un filtro. Los textos
    del paginador están en español (`provideSpanishPaginator()` en `app.config.ts`). El catálogo de la tienda
    usa el mismo helper con 9 tarjetas por página (`CATALOG_PAGE_SIZE`, cuadrícula de 3 × 3) y los colores salen de
    las variables de la tienda (bloque "Angular Material" en `styles.css`), no de un tema Material aparte.
11. **Detalle de producto** con `MatDialog`: `inject(ProductDetailDialog).open(producto)` (exportado en
    `@features/catalog`) abre `ProductDetailDialogComponent` para cualquier producto. El diálogo es presentacional:
    se cierra con `'add'` si el cliente pide agregarlo y quien lo abrió decide qué hacer (la tarjeta lo emite
    como `(add)`). Sus colores van en `.product-detail-panel` de `styles.css`, porque el overlay vive fuera del componente.

## Calidad: pruebas y lint

| Comando              | Qué hace                                                                  |
|----------------------|---------------------------------------------------------------------------|
| `npm test`           | Pruebas unitarias (Vitest) de `domain/`, `shared/utils/` y `shared/table/`. Tardan ~1 s.  |
| `npm run test:watch` | Igual, re-ejecutando al guardar.                                          |
| `npm run lint`       | ESLint: Angular, accesibilidad de plantillas y **reglas de arquitectura**. |
| `npm run check`      | Lint + pruebas + build. Córrelo antes de subir cambios.                   |

- Las pruebas van junto al código (`facets.ts` → `facets.spec.ts`). Para datos de prueba usa `aProduct()`
  desde `@features/catalog/testing`.
- `eslint-plugin-boundaries` hace cumplir las reglas 1 y 2 de arriba: importar el interior de otra feature,
  o que `shared`/`core` dependan de una feature, es un error de lint con un mensaje que lo explica.

## Seguridad con Angular 19

El proyecto se mantiene en **Angular 19.2.25**, la última versión 19. Esta versión terminó su soporte LTS en mayo de 2026
y tiene vulnerabilidades publicadas **que no se van a parchear en la 19**. Hoy ninguna afecta a la tienda, porque no
usa las funciones vulnerables. Para que siga así:

| No usar (mientras sigamos en v19)                               | Por qué                                  | Bloqueado por lint |
|-----------------------------------------------------------------|------------------------------------------|--------------------|
| SSR / hidratación (`provideClientHydration`, `@angular/ssr`, `withHttpTransferCache`) | Fuga de datos y envenenamiento de caché | Sí |
| i18n de Angular (`$localize`, `@angular/localize`)              | XSS                                      | Sí (atributo `i18n` en plantillas: revisar a mano) |
| `DatePipe` / `formatDate` (usar `Intl.DateTimeFormat`)          | Denegación de servicio                   | Sí |
| `bypassSecurityTrust*`                                          | Desactiva la sanitización                | Sí |
| `[innerHTML]`, enlaces de dos vías `[(…)]` o host bindings a `href`/`src` con datos del usuario o de la API | Bypass de sanitización (XSS) | No: **revisar en code review** |
| `<img [src]>` con una URL que viene de la API sin pasar por `safeImageUrl()` | Defensa en profundidad | No: **revisar en code review** |

Las imágenes de producto son el único `[src]` con datos de la API: el backend solo acepta URL `http(s)` o
archivos subidos, y el frontend las filtra otra vez con `safeImageUrl()` antes de mostrarlas.

Si algún día se necesita alguna de estas funciones, esa es la señal para actualizar Angular primero.
`npm audit --omit=dev` seguirá mostrando estas alertas: son esperadas mientras no se actualice.

## Alias de importación

`@core/*`, `@shared/*` y `@features/*` (definidos en `tsconfig.json`).
