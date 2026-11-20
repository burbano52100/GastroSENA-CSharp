# GastroSENA API (C#)

API REST del proyecto **GastroSENA**: reserva de almuerzos en el comedor del SENA, compra de fichas y saldo, y control de entrada por reconocimiento facial. Es la versión en C# del backend, hecha con **ASP.NET Core Minimal API**, **Entity Framework Core** y **PostgreSQL**.

## Qué hace

- **Cuentas**: registro e inicio de sesión de estudiantes con token JWT.
- **Saldo y fichas**: el estudiante recarga saldo y compra fichas. Cada ficha equivale a un almuerzo.
- **Reservas**: reserva un almuerzo para una fecha (consume una ficha). Puede cancelarla con al menos un día de anticipación y recupera la ficha.
- **Entrada al comedor**: el lector de reconocimiento facial envía el documento reconocido y la confianza. La API comprueba el umbral, que el usuario esté activo y que tenga una reserva de hoy sin consumir. Si todo está bien, marca el almuerzo como consumido y registra la entrada.
- **Administración**: reservas del día (para que la cocina sepa cuántos almuerzos preparar), registro de entradas y gestión de usuarios.

## Tecnologías y dónde buscarlas en la documentación oficial

| Tecnología | Para qué se usa aquí | Buscar en la documentación oficial |
|---|---|---|
| ASP.NET Core Minimal APIs | Endpoints, `MapGet`, `MapPost`, `MapGroup` | "Minimal APIs" · learn.microsoft.com/aspnet/core/fundamentals/minimal-apis |
| Route groups | Agrupar rutas bajo `/api/...` | "Minimal APIs route groups" |
| Route constraints | `{id:int}` | "Route constraints" (ASP.NET Core Routing) |
| Results / IResult | Respuestas HTTP (200, 201, 400, 401, 403, 404, 409) | "Results class" · learn.microsoft.com/dotnet/api/microsoft.aspnetcore.http.results |
| Inyección de dependencias | `ServicioJwt`, `GastroSenaDbContext`, `IConfiguration` | "Dependency injection in ASP.NET Core" |
| Configuración | `appsettings.json`, `GetValue<T>` | "Configuration in ASP.NET Core" |
| User secrets | Claves y contraseñas fuera del código | "Safe storage of app secrets in development" |
| Autenticación JWT Bearer | Emisión y validación de tokens | "JwtBearer authentication" · "Authentication in ASP.NET Core" |
| Autorización (roles y políticas) | `RequireAuthorization("Admin")` | "Authorization in ASP.NET Core" · "policy-based authorization" |
| Entity Framework Core | `DbContext`, LINQ, `Include`, `FindAsync`, `AnyAsync` | learn.microsoft.com/ef/core |
| Migraciones de EF Core | `dotnet ef migrations add`, `Database.MigrateAsync()` | "EF Core migrations" |
| Npgsql (PostgreSQL) | `UseNpgsql`, tipos `timestamptz` y `date` | "Npgsql Entity Framework Core provider" · npgsql.org/efcore |
| OpenAPI | Documento en `/openapi/v1.json` | "OpenAPI documents in ASP.NET Core" |
| Health checks | Endpoint `/health` | "Health checks in ASP.NET Core" |
| PBKDF2 | Hash de contraseñas | "Rfc2898DeriveBytes.Pbkdf2" |
| DateOnly y TimeZoneInfo | Fechas de reserva y zona horaria `America/Bogota` | "DateOnly Struct" · "TimeZoneInfo Class" |
| Docker Compose | PostgreSQL de desarrollo | "Docker Compose overview" · docs.docker.com/compose |
| Archivos `.http` | Peticiones de prueba | "HTTP files" en la documentación de Visual Studio |

Estos enlaces son las entradas principales. Si cambian de ruta, busca el término entre comillas.

## Requisitos

- Visual Studio 2022 o superior con la carga de trabajo **Desarrollo de ASP.NET y web**.
- SDK de **.NET 10**.
- **Docker Desktop** para PostgreSQL con Docker Compose, o un PostgreSQL local.

## Estructura del proyecto

```
GastroSENA-API/
├── README.md
├── GastroSENA.http              # Peticiones de prueba
├── docker-compose.yml           # PostgreSQL para desarrollo
└── GastroSENA.Api/
    ├── Program.cs               # Arranque: JWT, OpenAPI, health checks, migraciones
    ├── appsettings.json         # Configuración (sin secretos)
    ├── Models/                  # Entidades y enums
    ├── Data/                    # DbContext de EF Core y administrador inicial
    ├── Dtos/                    # Contratos de entrada y salida, y mapeos
    ├── Servicios/               # Hash de contraseñas y generación de JWT
    └── Endpoints/               # Rutas agrupadas por módulo
```

## Cómo correrla

1. **Base de datos.** En la carpeta `GastroSENA-API`:

   ```bash
   docker compose up -d
   ```

   Esto levanta PostgreSQL en `localhost:5432` con la base `gastrosena`. Si usas un PostgreSQL local, crea la base y ajusta `ConnectionStrings:GastroSena` en `appsettings.json`.

2. **Secretos.** En la carpeta `GastroSENA.Api`:

   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "Jwt:Clave" "pon-aqui-una-clave-de-al-menos-32-caracteres"
   dotnet user-secrets set "GastroSena:ClaveDispositivo" "clave-del-lector-facial"
   dotnet user-secrets set "Admin:Email" "admin@gastrosena.local"
   dotnet user-secrets set "Admin:Password" "UnaClaveSegura123"
   ```

   Sin `Jwt:Clave` la API no arranca. Sin `Admin:Email` y `Admin:Password` no se crea el administrador inicial.

3. **Migración inicial** (solo la primera vez, dentro de `GastroSENA.Api`):

   ```bash
   dotnet tool install --global dotnet-ef
   dotnet ef migrations add Inicial
   ```

   Al arrancar, la API aplica las migraciones y crea las tablas.

4. **Ejecutar.** Con `dotnet run` dentro de `GastroSENA.Api`, o con F5 en Visual Studio.

5. **Probar.**
   - Estado: `GET /health`
   - Documento OpenAPI: `GET /openapi/v1.json`
   - Peticiones: abre `GastroSENA.http`, cambia `@baseUrl` al puerto que muestre Visual Studio, inicia sesión y pega el token en `@token`.

## Rutas

| Método | Ruta | Acceso | Qué hace | Tecnología a buscar |
|---|---|---|---|---|
| GET | `/` | Público | Información del servicio | Minimal APIs |
| GET | `/health` | Público | Estado de la API | Health checks |
| GET | `/openapi/v1.json` | Público | Documento OpenAPI | OpenAPI |
| POST | `/api/auth/registro` | Público | Registra un estudiante | EF Core, PBKDF2 |
| POST | `/api/auth/login` | Público | Devuelve un token JWT | JwtBearer |
| GET | `/api/usuarios/me` | Usuario | Perfil, saldo y fichas | Authorization, Results |
| POST | `/api/usuarios/me/recargas` | Usuario | Recarga saldo (simulado) | EF Core, transacciones |
| GET | `/api/usuarios/me/movimientos` | Usuario | Últimos 100 movimientos | EF Core, LINQ |
| GET | `/api/fichas/precio` | Usuario | Precio de una ficha | Configuration |
| POST | `/api/fichas/compras` | Usuario | Compra fichas con saldo | EF Core, Configuration |
| POST | `/api/reservas` | Usuario | Reserva almuerzo para una fecha | DateOnly, EF Core |
| GET | `/api/reservas/me` | Usuario | Mis reservas | Include, LINQ |
| GET | `/api/reservas/{id}` | Dueño o Admin | Detalle de una reserva | Route constraints, IsInRole |
| DELETE | `/api/reservas/{id}` | Dueño | Cancela y devuelve la ficha | Results.NoContent |
| POST | `/api/entradas/reconocimiento` | Lector (`X-Api-Key`) | Valida la entrada facial | HttpContext, Request.Headers |
| GET | `/api/admin/reservas?fecha=` | Admin | Almuerzos del día | Authorization policies, DateOnly |
| GET | `/api/admin/entradas?fecha=` | Admin | Registro de entradas del día | TimeZoneInfo |
| GET | `/api/admin/usuarios` | Admin | Lista de usuarios | Authorization policies |
| PATCH | `/api/admin/usuarios/{id}/activo` | Admin | Activa o desactiva una cuenta | MapPatch |

## Reglas de negocio

- Una ficha equivale a un almuerzo. Comprar fichas descuenta saldo al precio de `GastroSena:PrecioFicha`.
- Un usuario tiene como máximo una reserva activa por fecha.
- Reservar exige tener fichas disponibles y consume una.
- Cancelar solo es posible para reservas activas con fecha futura, y devuelve la ficha.
- Para el control de entrada se exige que la confianza sea mayor o igual a `GastroSena:UmbralConfianza`, que el usuario esté activo y que tenga una reserva de hoy sin consumir. Si todo se cumple, la reserva pasa a `Consumida`.
- "Hoy" siempre se calcula en la zona horaria `America/Bogota`.
- Cada intento de entrada queda registrado, permitido o denegado, con su motivo.

## Limitaciones

- La recarga de saldo es simulada. Para cobrar de verdad se necesita una pasarela de pagos.
- El reconocimiento facial (Python, OpenCV, Teachable Machine) no vive en esta API. Solo recibe el resultado (documento y confianza) en `/api/entradas/reconocimiento`. Ese endpoint debe usarse con HTTPS en producción y con una clave de dispositivo larga y privada.
- Las rutas son un diseño basado en la descripción del proyecto. Ajústalas si tu versión en FastAPI usa otros nombres.
- No incluye pruebas automatizadas.
- Dos peticiones simultáneas podrían saltarse alguna regla. Para producción conviene añadir transacciones y restricciones únicas en la base de datos.

## Solución de problemas

- **La API no arranca y menciona `Jwt:Clave`:** falta la clave JWT o tiene menos de 32 caracteres. Configúrala con `dotnet user-secrets`.
- **`dotnet ef` no se reconoce:** instala la herramienta con `dotnet tool install --global dotnet-ef` y abre una terminal nueva.
- **Errores de tipo `timestamp` con PostgreSQL:** las fechas ya se guardan como `timestamptz`. Si aparece el error, borra la carpeta `Migrations`, vuelve a crear la migración y reinicia la base de datos (`docker compose down -v`).
- **El lector facial recibe 401:** falta la cabecera `X-Api-Key` o no coincide con `GastroSena:ClaveDispositivo`. Si la clave está vacía, el endpoint rechaza todas las peticiones.
- **Restauración de paquetes fallida:** los paquetes usan `10.0.*`. Si NuGet no encuentra alguna versión, indica una concreta con `dotnet add package <nombre> --version <versión>`.
