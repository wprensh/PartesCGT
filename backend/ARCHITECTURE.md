# Arquitectura del backend

API REST en .NET 9 organizada por **features (slices verticales)**, igual que el frontend: todo lo de una
funcionalidad vive junto (controlador, servicio, DTOs), y las reglas de negocio viven en el dominio.

```
TiendaVirtual.Api/
├─ Program.cs                 Solo compone módulos (~15 líneas).
├─ Common/                    Transversal, sin reglas de negocio.
│  ├─ Results/                Result<T> y Error: fallos de negocio sin excepciones.
│  ├─ Web/                    ApiControllerBase (Error → ProblemDetails) y configuración HTTP.
│  ├─ Security/  RateLimiting/
├─ Domain/                    Entidades con sus reglas. Sin EF ni ASP.NET.
│  ├─ Catalog/                Product (stock, atributos), Category + CategoryHierarchy, Review.
│  └─ Orders/                 Order.Place: arma el pedido y descuenta stock.
├─ Infrastructure/
│  └─ Persistence/            AppDbContext, una IEntityTypeConfiguration por entidad, SeedData, inicialización.
│     └─ Migrations/          Generadas por dotnet-ef. No se editan a mano.
└─ Features/                  Un caso de uso por carpeta.
   ├─ Products/               ProductsController → ProductQueries (lectura) / ProductService (escritura).
   ├─ Reviews/  Categories/  Orders/
   ├─ Assistant/              AssistantService → IChatModel (Claude/ es una implementación intercambiable).
   └─ Auth/                   AdminAuthService → ITokenIssuer (JWT).
TiendaVirtual.Api.Tests/      Pruebas: dominio, servicios (SQLite en memoria), API completa, arquitectura y migraciones.
tools/TiendaVirtual.DataMigration/  Copia única de datos SQLite → SQL Server (ver README).
```

## Base de datos

| | Aplicación | Pruebas automáticas |
|---|---|---|
| Proveedor (`Database:Provider`) | `SqlServer` | `Sqlite` (en memoria / archivo temporal) |
| Esquema | Migraciones, aplicadas al arrancar | `EnsureCreated` |

- Dinero en `decimal(18,2)` (`MoneyPrecision`). Fechas en UTC (`datetime2`) vía `TimeProvider`.
- SQL Server con reintentos ante fallos transitorios (`EnableRetryOnFailure`). Por eso, toda transacción
  manual va dentro de `Database.CreateExecutionStrategy().ExecuteAsync(...)` (ver `OrderService`).
- Pedidos en transacción `Serializable`: dos compras simultáneas no venden la misma última unidad.
- La configuración de la base se lee **al crear el DbContext**, nunca al registrar servicios en `Program.cs`:
  si se leyera antes, las pruebas de integración tomarían la base de `appsettings.json` (pasó una vez y escribieron
  en la base real). `ApiFactory` además reemplaza el DbContext por SQLite y `ApiIsolationTests` lo verifica.
- `MigrationTests` falla si el modelo cambió y falta la migración.

## Control de acceso del panel

- **Usuarios** (`AdminUsers`) con contraseña hasheada (PBKDF2, `PasswordHasher` de ASP.NET Identity) y **un rol**.
- **Roles editables** (`Roles` + `RolePermissions`) sobre un **catálogo fijo de permisos** (`Domain/Access/Permissions.cs`):
  `categories.view/manage`, `products.view/manage`, `suppliers.view/manage`, `users.manage`. Gestionar ⇒ ver.
- El rol **Administrador** (Id 1, de sistema) tiene siempre todos los permisos, incluso los que se agreguen después.
- Endpoints protegidos con `[HasPermission(Permissions.X)]` (una política por permiso).
- El token solo lleva identidad y un **sello de seguridad**. En cada petición (`AuthSetup.AttachCurrentPermissionsAsync`)
  se verifica que el usuario siga activo y con el mismo sello, y se cargan sus permisos **vigentes**: un cambio de
  rol, de permisos, una desactivación o un cambio de contraseña aplican en la siguiente petición.
- Protecciones: nadie cambia su propio rol ni se desactiva/elimina; siempre queda un usuario activo con
  `users.manage`; un rol con usuarios no se borra.
- **Primer administrador**: si no hay usuarios, `AdminBootstrapper` lo crea desde `Admin:Email` / `Admin:Password`
  (user-secrets). Después se ignora. Alternativa en desarrollo: `POST /api/auth/setup` `{email, fullName, password}`,
  que solo responde en `Development`, desde localhost y con la tabla de usuarios vacía (si no, 403/409).
- **Agregar un permiso**: constante + entrada en `Permissions.All` (backend) y en `core/auth/permissions.ts` (frontend).

## Proveedores

`Suppliers` (NIT único) y `ProductSuppliers` (producto × proveedor con costo, código del proveedor y un preferido).
Los costos solo los ven quienes tienen `suppliers.view` y se asignan con `suppliers.manage`, por eso van en
endpoints aparte del producto (`/api/products/{id}/suppliers`). Un proveedor que surte productos no se borra: se desactiva.

## Imágenes de producto

- En la base solo se guarda la **URL** (`Products.ImageUrl`, `nvarchar(500)`); el archivo va en disco.
  Guardar binarios en SQL Server la engorda y hace más lentos los respaldos.
- `POST /api/products/images` (admin) recibe JPG/PNG/WebP de hasta 2 MB. El formato se detecta **por el
  contenido** (`ImageFormat`), no por la extensión. Se guarda con nombre aleatorio vía `IFileStorage`.
- `Storage:Provider` elige el almacén:
  - `Local` (por defecto): `LocalFileStorage` escribe en `Storage:RootPath` (`App_Data/files`, fuera del repositorio).
    Al desplegar, esa carpeta debe persistir entre versiones.
  - `AzureBlob`: `AzureBlobFileStorage` guarda en un contenedor **privado** (`Storage:AzureBlob:Container`, se crea
    al arrancar) con la cadena `Storage:AzureBlob:ConnectionString` (secreta: user-secrets o variable de entorno).
- En ambos casos las URL son `/api/files/{carpeta}/{guid}.{ext}` (`StoredFileUrl`) y la API las sirve con
  `X-Content-Type-Options: nosniff` y caché larga (los nombres nunca se repiten). Cambiar de almacén no cambia
  las URL guardadas en la base (hay que copiar los archivos). Solo se reconoce ese formato exacto, así que no
  sirve para leer ni borrar otros archivos.
- El proveedor se elige al **resolver** el servicio, no al registrarlo: las pruebas fuerzan `Local` en una carpeta
  temporal y nunca usan el almacén real.
- `ImageUrl` acepta solo imágenes subidas o URL `http(s)` absolutas (`ProductImage.IsExternalUrl`).
- Al reemplazar la imagen o borrar el producto, el archivo anterior se borra si ningún otro producto lo usa.

## Despliegue (Docker y Render)

- `backend/Dockerfile` (contexto: `backend/`): compila con `sdk:9.0` y ejecuta con `aspnet:9.0` como usuario sin
  privilegios, en el puerto 8080. Copia `.editorconfig` porque los avisos del analizador son errores.
- `render.yaml` (raíz del repositorio) define el servicio de Render. Los secretos (`ConnectionStrings__Default`,
  `Admin__*`, `Storage__AzureBlob__ConnectionString`, `Claude__ApiKey`) se cargan en Render; `Jwt__Key` la genera Render.
- `ReverseProxy:Enabled` (activado en Render) toma la IP del cliente de `X-Forwarded-For`, para que el límite de
  intentos sea por cliente. Apagado por defecto: sin proxy, cualquiera podría falsear esa cabecera.
  `ReverseProxy:ForwardLimit` (1) es cuántos proxies hay delante de la API.
- `GET /api/health`: chequeo de Render. Solo indica que el proceso responde; no consulta la base.

## Flujo de una petición

```
HTTP → Controller (delgado) → Service / Queries → Domain (reglas) → AppDbContext
                  ↑                    │
                  └── Result<T> ◄──────┘   ApiControllerBase traduce Error → 400/401/404/409/503
```

## Principios SOLID aplicados

| Principio | Dónde |
|---|---|
| **S**: una responsabilidad | Controladores solo traducen HTTP; servicios orquestan; el dominio decide. `Program.cs` delega en `*Setup`. Lectura (`ProductQueries`) separada de escritura (`ProductService`). |
| **O**: abierto/cerrado | Un tipo de error nuevo solo toca `ApiControllerBase`. Una política de rate limiting nueva es una línea en `RateLimitPolicies`. Una entidad nueva es una clase de configuración: `ApplyConfigurationsFromAssembly` la encuentra. |
| **L**: sustitución | Cualquier `IChatModel` o `ITokenIssuer` sirve sin cambiar a quien lo usa (las pruebas usan dobles). |
| **I**: interfaces pequeñas | `IChatModel` (un método), `ITokenIssuer` (un método). |
| **D**: inversión de dependencias | `AssistantService` depende de `IChatModel`, no de `HttpClient` ni de Anthropic. Las fechas vienen de `TimeProvider`, no de `DateTime.UtcNow`. |

**Interfaces solo en las fronteras** (servicios externos, reloj). Los servicios internos son clases concretas:
una interfaz por servicio sin una segunda implementación es ceremonia y no aporta. Se prueban con SQLite real.

## Reglas

1. **Controladores delgados**: heredan de `ApiControllerBase`, no usan `AppDbContext` y devuelven
   `OkOrFailure(...)` / `NoContentOrFailure(...)` / `Failure(...)`.
2. **Errores de negocio con `Result<T>`**, no con excepciones. Las excepciones son para lo inesperado
   (bugs, caídas); el middleware las convierte en 500.
3. **Reglas en el dominio**: si una validación depende solo de los datos (stock, jerarquía, normalización),
   va en la entidad o en una clase estática del dominio, con prueba unitaria.
4. **DTOs, nunca entidades**, en las respuestas. Las proyecciones (`ProductMapping.ToDto`) se traducen a SQL.
5. **Sin números mágicos**: `Product.MaxAttributes`, `Review.MaxRating`, `AssistantService.MaxHistoryMessages`…
6. **Mensajes al cliente en español** y en un solo lugar (el `Error` que los genera).

Las reglas 1, 3 (dominio sin EF/ASP.NET) y la separación de capas las verifican `ArchitectureTests`.

## Calidad

| Comando (desde `backend/TiendaVirtual.Api`) | Qué hace |
|---|---|
| `dotnet build` | Compila con los analizadores recomendados de .NET; **cualquier aviso es error**. Excepciones justificadas en `backend/.editorconfig`. |
| `dotnet test TiendaVirtual.Api.slnx` | 60 pruebas en segundos: dominio, servicios, API completa por HTTP, arquitectura y migraciones. No necesitan SQL Server. |

## Agregar una feature

1. Carpeta `Features/<Nombre>/` con `<Nombre>Controller`, `<Nombre>Service`, `<Nombre>Dtos`.
2. Si hay reglas de negocio nuevas, van en `Domain/` con sus pruebas.
3. Registrar el servicio en `FeatureSetup` (o un `<Nombre>Setup` si necesita configuración propia).
4. Si agregas entidades o cambias columnas: su `IEntityTypeConfiguration` en
   `Infrastructure/Persistence/Configurations/` y una migración nueva
   (`dotnet tool run dotnet-ef migrations add <Nombre> -o Infrastructure/Persistence/Migrations`).
