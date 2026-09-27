# Tienda virtual: .NET 9 + Angular 19 + Claude

Catálogo, carrito, pedidos y un asistente de compras con Claude que recomienda
productos reales del inventario (con stock y compatibilidades).

## Estructura

```
backend/TiendaVirtual.Api   API REST .NET 9, EF Core + SQL Server, integración con Claude (ver backend/ARCHITECTURE.md)
backend/TiendaVirtual.Api.Tests  Pruebas xUnit: dominio, servicios, API, arquitectura y migraciones
backend/tools/TiendaVirtual.DataMigration  Copia única de datos de la base SQLite anterior a SQL Server
frontend/                   Angular 19 standalone, signals, organizado por features (ver frontend/ARCHITECTURE.md)
```

## 1. Backend

```bash
cd backend/TiendaVirtual.Api
dotnet user-secrets set "Claude:ApiKey" "sk-ant-..."   # nunca en appsettings ni en el frontend
dotnet user-secrets set "Admin:Email" "tu-correo@dominio.com"      # primer administrador: solo se usa
dotnet user-secrets set "Admin:Password" "una-contraseña-larga-2026" # si aún no hay usuarios (10+ caracteres, letras y números)
dotnet user-secrets set "Admin:FullName" "Tu Nombre"
dotnet user-secrets set "Jwt:Key" "cadena-aleatoria-de-al-menos-32-caracteres"
dotnet run --launch-profile http                         # http://localhost:5080
dotnet test TiendaVirtual.Api.slnx                       # pruebas del backend
```

### Base de datos: SQL Server

La API usa **SQL Server** (`.\SQLEXPRESS`, base `TiendaVirtual`, autenticación de Windows) según
`ConnectionStrings:Default` en `appsettings.json`. Para otro servidor, cámbiala ahí o con user-secrets.

- Al arrancar, la API **aplica sola las migraciones pendientes** y, si la base no existe, la crea con
  12 categorías y 10 productos de ejemplo.
- Cambiar el modelo (una entidad o su configuración) exige una migración nueva; una prueba avisa si falta:

```bash
dotnet tool restore                                   # una vez: instala dotnet-ef (versión fijada en backend/dotnet-tools.json)
dotnet tool run dotnet-ef migrations add NombreDelCambio -o Infrastructure/Persistence/Migrations
dotnet tool run dotnet-ef database update             # opcional: la API también la aplica al arrancar
```

**Venir de la base SQLite anterior** (`tienda-v3.db`): con la base de SQL Server recién creada,

```bash
cd backend/tools/TiendaVirtual.DataMigration
dotnet run -- ../../TiendaVirtual.Api/tienda-v3.db "Server=.\SQLEXPRESS;Database=TiendaVirtual;Trusted_Connection=True;TrustServerCertificate=True" --simular
dotnet run -- ../../TiendaVirtual.Api/tienda-v3.db "Server=.\SQLEXPRESS;Database=TiendaVirtual;Trusted_Connection=True;TrustServerCertificate=True"
```

Copia todo conservando los IDs, en una transacción, y verifica conteos y totales. Se niega a correr
si la base destino ya tiene datos propios. `--simular` hace la copia completa y la deshace.

OpenAPI en desarrollo: http://localhost:5080/openapi/v1.json

Endpoints:

| Método | Ruta                          | Uso                                   |
|--------|-------------------------------|---------------------------------------|
| GET    | /api/products?category=&q=    | Catálogo con filtros                  |
| GET    | /api/products/{id}            | Detalle                               |
| GET    | /api/categories               | Categorías con conteo de productos    |
| POST   | /api/auth/login               | Login admin → JWT (5 intentos/min)    |
| GET    | /api/products/admin           | 🔒 Todos, incluidos ocultos           |
| POST/PUT/DELETE | /api/products/{id}   | 🔒 CRUD de productos                  |
| POST/PUT/DELETE | /api/categories/{id} | 🔒 CRUD de categorías                 |
| POST   | /api/orders                   | Crear pedido (valida stock y precios) |
| GET    | /api/orders/{id}              | Consultar pedido                      |
| POST   | /api/assistant/chat           | Asistente Claude (10 req/min por IP)  |

## 2. Frontend

```bash
cd frontend
npm install
npm start          # http://localhost:4200, con proxy /api -> :5080
```

## Panel de administración

Entra en http://localhost:4200/admin. La primera vez, con el administrador de user-secrets (se crea solo al
arrancar la API si no hay usuarios); después, crea los demás usuarios desde el panel.

- **Acceso por roles:** cada usuario tiene un rol y cada rol, sus permisos. Vienen tres: **Administrador**
  (todo), **Catálogo** (categorías y productos; ve proveedores) y **Compras** (proveedores y costos; ve el
  catálogo). En **Roles** se crean o ajustan; en **Usuarios** se crean, se desactivan o se les restablece la
  contraseña. Los cambios aplican de inmediato, sin volver a entrar. El menú solo muestra lo que cada rol puede ver
  y el backend lo exige igual. "Cambiar contraseña" está arriba a la derecha.
- **Proveedores:** razón social, NIT (único), contacto y estado. Al editar uno se ven los productos que surte con
  su costo y margen. Uno que surte productos no se borra: se desactiva.
- **Proveedores de un producto:** en el formulario del producto, sección "Proveedores y costos": varios
  proveedores con su costo, código y uno preferido, con el margen sobre el precio de venta.
- **Categorías:** crear, renombrar (nombre único) y eliminar. Una categoría con productos no se puede borrar.
- **Imágenes:** en el formulario del producto, "Subir imagen" (JPG, PNG o WebP de hasta 2 MB) o pegar una
  URL `https://`. Los archivos subidos quedan en `backend/TiendaVirtual.Api/App_Data/files` (inclúyela en tus respaldos).
- **Productos:** crear, editar, ocultar/mostrar en la tienda y eliminar. Un producto con pedidos
  no se borra (rompería el historial): se oculta desmarcando "Visible en la tienda".
- Los productos ocultos tampoco los recomienda el asistente.

## Cómo funciona el asistente

1. Angular envía el historial del chat (rol + texto) a `/api/assistant/chat`.
2. `AssistantService` arma un system prompt con el catálogo en stock y pide respuesta en JSON:
   `{ "answer": "...", "productIds": [..] }`.
3. El backend valida los IDs contra la BD y devuelve productos reales; la UI los pinta
   como tarjetas con botón "Agregar".

Modelo por defecto: `claude-haiku-4-5-20251001` (rápido y económico). Cámbialo en
`appsettings.json` → `Claude:Model` (p. ej. `claude-sonnet-5` para respuestas más finas).

## Siguientes pasos para producción

- Base en producción: Azure SQL (misma migración); usuario SQL o identidad administrada en lugar de
  autenticación de Windows, y la cadena de conexión en Key Vault.
- Varios administradores: migrar el login de un solo admin a ASP.NET Identity o Entra ID.
- Pasarela de pago (Wompi, ePayco o Mercado Pago para Colombia) con webhooks.
- Imágenes de producto en Azure Blob Storage: una implementación nueva de `IFileStorage`.
- Si el catálogo crece mucho: en vez de mandar todo el inventario en el prompt, usar
  *tool use* de Claude con una herramienta `buscar_productos` que consulte la BD.
- Despliegue: API en Azure App Service / Container Apps, Angular en Azure Static Web Apps,
  API key en Key Vault.
