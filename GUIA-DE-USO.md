# Guía de uso de GastroSENA API

Cómo arrancar la API, verla en el navegador y probar cada endpoint con PowerShell, Scalar, Thunder Client o el archivo `.http`.

## Contenido

1. [Datos rápidos](#1-datos-rápidos)
2. [Arrancar y detener la API](#2-arrancar-y-detener-la-api)
3. [Ver la API en el navegador (Scalar)](#3-ver-la-api-en-el-navegador-scalar)
4. [Endpoints uno por uno](#4-endpoints-uno-por-uno)
5. [Flujo completo en PowerShell](#5-flujo-completo-en-powershell)
6. [Thunder Client](#6-thunder-client)
7. [Archivo GastroSENA.http](#7-archivo-gastrosenahttp)
8. [Tareas de VS Code](#8-tareas-de-vs-code)
9. [Reglas de negocio](#9-reglas-de-negocio)
10. [Solución de problemas](#10-solución-de-problemas)
11. [Reiniciar la base de datos](#11-reiniciar-la-base-de-datos)

---

## 1. Datos rápidos

### Direcciones

| Dirección | Qué muestra |
|---|---|
| http://localhost:5000/scalar | Interfaz visual con todos los endpoints para probarlos |
| http://localhost:5000/health | Estado de la API (`Healthy`) |
| http://localhost:5000/openapi/v1.json | Documento OpenAPI (JSON) |
| http://localhost:5000/ | Nombre y versión del servicio |

### Credenciales de desarrollo

Estas credenciales son solo para tu PC. Usa otras en producción.

| Qué | Valor |
|---|---|
| PostgreSQL | usuario `postgres`, contraseña `postgres`, base `gastrosena`, puerto `5432` |
| Administrador | `admin@gastrosena.local` / `UnaClaveSegura123` |
| Clave del lector facial (`X-Api-Key`) | `clave-del-lector-facial` |

Los secretos se guardan con `dotnet user-secrets`, fuera del proyecto. Para verlos: tarea **Secretos: ver** en VS Code, o `dotnet user-secrets list` dentro de `GastroSENA.Api`.

### Configuración (`GastroSENA.Api/appsettings.json`)

| Clave | Valor | Para qué |
|---|---|---|
| `GastroSena:PrecioFicha` | `3500` | Precio de una ficha (un almuerzo) |
| `GastroSena:UmbralConfianza` | `0.85` | Confianza mínima del reconocimiento facial |
| `Jwt:ExpiracionMinutos` | `120` | Duración del token |

---

## 2. Arrancar y detener la API

PostgreSQL debe estar encendido. Se instala como servicio de Windows (`postgresql-x64-18`) y arranca solo.

### Opción A: VS Code (recomendada)

1. Abre la carpeta `APIREST C#` en VS Code.
2. Pulsa **F5**. Se compila, arranca con depurador y el navegador abre `/scalar`.
3. Para detenerla: **Shift+F5**.

La primera vez, si F5 pide elegir un depurador, pulsa **Ctrl+Shift+P**, ejecuta **Developer: Reload Window** y vuelve a pulsar F5.

Sin depurador y con recarga en caliente: **Ctrl+Shift+P → Tasks: Run Task → API: ejecutar con recarga en caliente**.

### Opción B: Terminal

```powershell
dotnet run --project "C:\Users\BURBANO\Desktop\APIREST C#\GastroSENA.Api"
```

Luego abre http://localhost:5000/scalar. Para detenerla, pulsa **Ctrl+C** en esa terminal.

### Opción C: Visual Studio 2026

1. Abre `GastroSENA.slnx`.
2. Pulsa **F5**.
3. Si pregunta si quieres confiar en el certificado SSL de IIS Express, responde **No**. La API usa HTTP y no lo necesita.
4. Para detenerla: **Shift+F5**.

> Solo puede haber una copia de la API corriendo, porque todas usan el puerto 5000.

---

## 3. Ver la API en el navegador (Scalar)

1. Con la API corriendo, abre **http://localhost:5000/scalar**.
2. A la izquierda aparecen todos los endpoints agrupados. Al hacer clic en uno ves su descripción, sus datos de entrada y un ejemplo de respuesta.
3. Para probar uno, pulsa **Test Request**, completa el body y pulsa **Send**.
4. Para las rutas protegidas:
   1. Prueba **POST /api/auth/login** con:
      ```json
      {"email":"admin@gastrosena.local","password":"UnaClaveSegura123"}
      ```
   2. Copia el valor de `token` de la respuesta.
   3. Arriba, en **Authentication → Bearer Token**, pégalo. Todas las peticiones siguientes lo envían.

---

## 4. Endpoints uno por uno

### Preparación para los comandos de PowerShell

Ejecuta esto primero en la terminal. Los demás comandos usan estas variables.

```powershell
$api = "http://localhost:5000"
$hoy = Get-Date -Format "yyyy-MM-dd"
$manana = (Get-Date).AddDays(1).ToString("yyyy-MM-dd")
```

`irm` es la forma corta de `Invoke-RestMethod`. Cuando la API responde con un error (400, 401, 403, 404, 409), PowerShell muestra un mensaje en rojo con el JSON del error.

### Resumen

| # | Método | Ruta | Acceso | Qué hace |
|---|---|---|---|---|
| 4.1 | GET | `/` | Público | Información del servicio |
| 4.2 | GET | `/health` | Público | Estado de la API |
| 4.3 | POST | `/api/auth/registro` | Público | Registra un estudiante |
| 4.4 | POST | `/api/auth/login` | Público | Devuelve un token JWT |
| 4.5 | GET | `/api/usuarios/me` | Usuario | Perfil, saldo y fichas |
| 4.6 | POST | `/api/usuarios/me/recargas` | Usuario | Recarga saldo (simulado) |
| 4.7 | GET | `/api/usuarios/me/movimientos` | Usuario | Últimos 100 movimientos |
| 4.8 | GET | `/api/fichas/precio` | Usuario | Precio de una ficha |
| 4.9 | POST | `/api/fichas/compras` | Usuario | Compra fichas con saldo |
| 4.10 | POST | `/api/reservas` | Usuario | Reserva almuerzo para una fecha |
| 4.11 | GET | `/api/reservas/me` | Usuario | Mis reservas |
| 4.12 | GET | `/api/reservas/{id}` | Dueño o Admin | Detalle de una reserva |
| 4.13 | DELETE | `/api/reservas/{id}` | Dueño | Cancela y devuelve la ficha |
| 4.14 | POST | `/api/entradas/reconocimiento` | Lector (`X-Api-Key`) | Valida la entrada al comedor |
| 4.15 | GET | `/api/admin/reservas?fecha=` | Admin | Almuerzos reservados del día |
| 4.16 | GET | `/api/admin/entradas?fecha=` | Admin | Registro de entradas del día |
| 4.17 | GET | `/api/admin/usuarios` | Admin | Lista de usuarios |
| 4.18 | PATCH | `/api/admin/usuarios/{id}/activo` | Admin | Activa o desactiva una cuenta |

**Tipos de acceso:**
- **Usuario:** cualquier token válido (`Authorization: Bearer <token>`). Sin token responde **401**.
- **Admin:** token de un usuario con rol Admin. Sin token responde **401**; con token de estudiante, **403**.

---

### 4.1 `GET /` · Información del servicio

Devuelve el nombre y la versión de la API. Sirve para comprobar rápido que responde.

```powershell
irm "$api/"
```

Respuesta `200`:
```json
{"servicio":"GastroSENA API","version":"1.0"}
```

---

### 4.2 `GET /health` · Estado de la API

Health check de ASP.NET Core. Devuelve el texto `Healthy` si la API está viva.

```powershell
irm "$api/health"
```

---

### 4.3 `POST /api/auth/registro` · Registrar estudiante

Crea una cuenta con rol **Estudiante**, saldo 0 y 0 fichas:
- La contraseña se guarda con hash PBKDF2, nunca en texto plano.
- El email se guarda en minúsculas.

| Campo | Tipo | Regla |
|---|---|---|
| `nombre` | texto | Obligatorio |
| `documento` | texto | Obligatorio y único. Es el que usa el lector facial |
| `email` | texto | Obligatorio, con `@`, y único |
| `password` | texto | Mínimo 8 caracteres |

```powershell
irm "$api/api/auth/registro" -Method Post -ContentType "application/json" -Body '{"nombre":"Estudiante de prueba","documento":"1000000001","email":"prueba@sena.edu.co","password":"clave12345"}'
```

| Código | Cuándo |
|---|---|
| `201` | Creado. Devuelve `id, nombre, documento, email, rol, saldo, fichas` |
| `400` | Falta nombre o documento, el email no es válido o la contraseña tiene menos de 8 caracteres |
| `409` | Ya existe un usuario con ese email o documento |

---

### 4.4 `POST /api/auth/login` · Iniciar sesión

Verifica email y contraseña y devuelve un **token JWT** válido por 120 minutos. El token lleva el id, el nombre y el rol del usuario.

```powershell
# Estudiante: guarda el token en $token y prepara la cabecera $h
$token = (irm "$api/api/auth/login" -Method Post -ContentType "application/json" -Body '{"email":"prueba@sena.edu.co","password":"clave12345"}').token
$h = @{ Authorization = "Bearer $token" }

# Administrador: guarda el token en $tokenAdmin y prepara la cabecera $ha
$tokenAdmin = (irm "$api/api/auth/login" -Method Post -ContentType "application/json" -Body '{"email":"admin@gastrosena.local","password":"UnaClaveSegura123"}').token
$ha = @{ Authorization = "Bearer $tokenAdmin" }
```

| Código | Cuándo |
|---|---|
| `200` | Correcto. Devuelve `{"token":"eyJ...","expira":"2026-11-20T17:00:00Z"}` |
| `401` | El email no existe o la contraseña es incorrecta |
| `403` | La cuenta está desactivada (`"Usuario inactivo."`) |

---

### 4.5 `GET /api/usuarios/me` · Mi perfil

Devuelve los datos del usuario dueño del token: id, nombre, documento, email, rol, **saldo** y **fichas**.

```powershell
irm "$api/api/usuarios/me" -Headers $h
```

| Código | Cuándo |
|---|---|
| `200` | Correcto |
| `401` | Falta el token o expiró |

---

### 4.6 `POST /api/usuarios/me/recargas` · Recargar saldo

Suma dinero al saldo del usuario y registra un movimiento de tipo `Recarga`. Es **simulado**: no hay pasarela de pagos.

| Campo | Tipo | Regla |
|---|---|---|
| `monto` | número | Mayor que 0 y como máximo 1.000.000 |

```powershell
irm "$api/api/usuarios/me/recargas" -Method Post -Headers $h -ContentType "application/json" -Body '{"monto":50000}'
```

| Código | Cuándo |
|---|---|
| `200` | Correcto. Devuelve el perfil con el saldo nuevo |
| `400` | El monto está fuera del rango |

---

### 4.7 `GET /api/usuarios/me/movimientos` · Historial

Devuelve los últimos 100 movimientos del usuario, del más reciente al más antiguo.

Cada movimiento tiene `id, tipo, monto, fichas, fechaHora, descripcion`. El campo `tipo` puede ser:

| Tipo | Monto | Fichas |
|---|---|---|
| `Recarga` | positivo | 0 |
| `CompraFichas` | negativo | positivas |
| `ReservaCreada` | 0 | -1 |
| `ReservaCancelada` | 0 | +1 |

```powershell
irm "$api/api/usuarios/me/movimientos" -Headers $h | Format-Table
```

---

### 4.8 `GET /api/fichas/precio` · Precio de la ficha

Devuelve el precio de una ficha según `GastroSena:PrecioFicha`. Una ficha equivale a un almuerzo.

```powershell
irm "$api/api/fichas/precio" -Headers $h
```

Respuesta `200`:
```json
{"precioFicha":3500}
```

---

### 4.9 `POST /api/fichas/compras` · Comprar fichas

Descuenta del saldo `cantidad × precio` y suma las fichas. Registra un movimiento `CompraFichas`.

| Campo | Tipo | Regla |
|---|---|---|
| `cantidad` | entero | Entre 1 y 20 por operación |

```powershell
irm "$api/api/fichas/compras" -Method Post -Headers $h -ContentType "application/json" -Body '{"cantidad":5}'
```

| Código | Cuándo |
|---|---|
| `200` | Correcto. Devuelve el perfil con el saldo y las fichas nuevos |
| `400` | La cantidad no está entre 1 y 20, o el saldo no alcanza (la respuesta incluye `costo` y `saldo`) |

---

### 4.10 `POST /api/reservas` · Reservar almuerzo

Reserva un almuerzo para una fecha y **consume 1 ficha**. Registra un movimiento `ReservaCreada`.

| Campo | Tipo | Regla |
|---|---|---|
| `fecha` | fecha `yyyy-MM-dd` | Hoy o una fecha futura (hora de Colombia) |

```powershell
# Reserva para hoy
$reservaHoy = irm "$api/api/reservas" -Method Post -Headers $h -ContentType "application/json" -Body (@{ fecha = $hoy } | ConvertTo-Json)
$reservaHoy

# Reserva para mañana (sirve para probar la cancelación)
$reservaManana = irm "$api/api/reservas" -Method Post -Headers $h -ContentType "application/json" -Body (@{ fecha = $manana } | ConvertTo-Json)
$reservaManana
```

| Código | Cuándo |
|---|---|
| `201` | Creada. Devuelve `id, usuario, documento, fecha, estado ("Reservada"), fechaCreacion, fechaConsumo` |
| `400` | La fecha ya pasó, o el usuario no tiene fichas |
| `404` | La cuenta está desactivada |
| `409` | Ya tienes una reserva activa para esa fecha |

---

### 4.11 `GET /api/reservas/me` · Mis reservas

Lista todas las reservas del usuario, de la fecha más reciente a la más antigua. Incluye las canceladas y las consumidas.

Estados posibles: `Reservada`, `Consumida`, `Cancelada`.

```powershell
irm "$api/api/reservas/me" -Headers $h | Format-Table id, fecha, estado
```

---

### 4.12 `GET /api/reservas/{id}` · Detalle de una reserva

Devuelve una reserva por su id. Solo puede verla su **dueño** o un **admin**.

```powershell
irm "$api/api/reservas/$($reservaHoy.id)" -Headers $h
```

| Código | Cuándo |
|---|---|
| `200` | Correcto |
| `404` | No existe, o no es tuya y no eres admin. Se responde 404 para no revelar que existe |

---

### 4.13 `DELETE /api/reservas/{id}` · Cancelar reserva

Cancela una reserva propia y **devuelve la ficha**. Registra un movimiento `ReservaCancelada`.

Solo funciona si la reserva está en estado `Reservada` y su fecha es **a partir de mañana**: hay que cancelar con al menos un día de anticipación.

```powershell
irm "$api/api/reservas/$($reservaManana.id)" -Method Delete -Headers $h
```

| Código | Cuándo |
|---|---|
| `204` | Cancelada (sin contenido) |
| `400` | La reserva ya estaba cancelada o consumida, o su fecha es hoy o ya pasó |
| `404` | No existe o no es tuya |

---

### 4.14 `POST /api/entradas/reconocimiento` · Entrada al comedor (lector facial)

Lo llama el **lector de reconocimiento facial**, no el estudiante. No usa JWT: exige la cabecera `X-Api-Key` con la clave del dispositivo.

| Campo | Tipo | Significado |
|---|---|---|
| `documento` | texto | Documento de la persona reconocida |
| `confianza` | número 0 a 1 | Qué tan seguro está el modelo |
| `dispositivo` | texto | Nombre de la cámara o lector |

La API revisa, en este orden:

1. Que la confianza sea **≥ 0.85**. Si no: `"Confianza de reconocimiento insuficiente."`
2. Que el usuario exista y esté activo. Si no: `"Usuario no encontrado o inactivo."`
3. Que tenga una reserva **para hoy**. Si no: `"No tienes reserva de almuerzo para hoy."`
4. Que esa reserva no esté consumida. Si lo está: `"El almuerzo de hoy ya fue consumido."`

Si todo se cumple, la reserva pasa a `Consumida` y la respuesta dice `"Acceso permitido."`. **Cada intento queda registrado**, tanto los permitidos como los denegados.

```powershell
irm "$api/api/entradas/reconocimiento" -Method Post -Headers @{ "X-Api-Key" = "clave-del-lector-facial" } -ContentType "application/json" -Body '{"documento":"1000000001","confianza":0.93,"dispositivo":"camara-1"}'
```

| Código | Cuándo |
|---|---|
| `200` | Acceso permitido. Devuelve `id, documento, fechaHora, confianza, dispositivo, resultado ("Permitida"), motivo` |
| `401` | Falta `X-Api-Key` o la clave no coincide |
| `403` | Acceso denegado. Mismo formato, con `resultado: "Denegada"` y el `motivo` |

---

### 4.15 `GET /api/admin/reservas?fecha=` · Almuerzos del día (cocina)

Lista las reservas **no canceladas** de una fecha, con el total. Sirve para que la cocina sepa cuántos almuerzos preparar.

Si no se envía `fecha`, usa la de hoy.

```powershell
irm "$api/api/admin/reservas?fecha=$hoy" -Headers $ha | ConvertTo-Json -Depth 5
```

Respuesta `200`:
```json
{ "fecha": "2026-11-20", "total": 1, "reservas": [ { "id": 1, "usuario": "Estudiante de prueba", "estado": "Reservada", "...": "..." } ] }
```

---

### 4.16 `GET /api/admin/entradas?fecha=` · Registro de entradas

Lista todos los intentos de entrada del día, permitidos y denegados, del más reciente al más antiguo. El día se calcula en hora de Colombia. Si no se envía `fecha`, usa la de hoy.

```powershell
irm "$api/api/admin/entradas?fecha=$hoy" -Headers $ha | Format-Table fechaHora, documento, confianza, resultado, motivo
```

---

### 4.17 `GET /api/admin/usuarios` · Lista de usuarios

Lista todos los usuarios ordenados por nombre, con su rol, saldo y fichas. Nunca incluye la contraseña.

```powershell
irm "$api/api/admin/usuarios" -Headers $ha | Format-Table id, nombre, documento, email, rol, saldo, fichas
```

---

### 4.18 `PATCH /api/admin/usuarios/{id}/activo` · Activar o desactivar cuenta

Activa (`true`) o desactiva (`false`) una cuenta. Un usuario desactivado:
- no puede iniciar sesión (**403**);
- no puede reservar (**404**);
- el lector facial le niega la entrada.

| Campo | Tipo |
|---|---|
| `activo` | `true` / `false` |

```powershell
# Desactivar al usuario 2
irm "$api/api/admin/usuarios/2/activo" -Method Patch -Headers $ha -ContentType "application/json" -Body '{"activo":false}'

# Volver a activarlo
irm "$api/api/admin/usuarios/2/activo" -Method Patch -Headers $ha -ContentType "application/json" -Body '{"activo":true}'
```

| Código | Cuándo |
|---|---|
| `200` | Correcto. Devuelve el usuario |
| `404` | No existe un usuario con ese id |

---

## 5. Flujo completo en PowerShell

Copia y pega este bloque en una terminal con la API corriendo. Recorre todos los endpoints, de principio a fin.

Hazlo con la base recién creada, solo con el admin; si no, el registro devuelve 409 (ver [sección 11](#11-reiniciar-la-base-de-datos)).

```powershell
# ===== Preparación =====
$api = "http://localhost:5000"
$hoy = Get-Date -Format "yyyy-MM-dd"
$manana = (Get-Date).AddDays(1).ToString("yyyy-MM-dd")

# ===== Público =====
irm "$api/" | Format-List
irm "$api/health"

# ===== Estudiante: registro y login =====
irm "$api/api/auth/registro" -Method Post -ContentType "application/json" -Body '{"nombre":"Estudiante de prueba","documento":"1000000001","email":"prueba@sena.edu.co","password":"clave12345"}' | Format-List
$token = (irm "$api/api/auth/login" -Method Post -ContentType "application/json" -Body '{"email":"prueba@sena.edu.co","password":"clave12345"}').token
$h = @{ Authorization = "Bearer $token" }

# ===== Estudiante: saldo y fichas =====
irm "$api/api/usuarios/me" -Headers $h | Format-List
irm "$api/api/usuarios/me/recargas" -Method Post -Headers $h -ContentType "application/json" -Body '{"monto":50000}' | Format-List
irm "$api/api/fichas/precio" -Headers $h | Format-List
irm "$api/api/fichas/compras" -Method Post -Headers $h -ContentType "application/json" -Body '{"cantidad":5}' | Format-List

# ===== Estudiante: reservas =====
$reservaHoy = irm "$api/api/reservas" -Method Post -Headers $h -ContentType "application/json" -Body (@{ fecha = $hoy } | ConvertTo-Json)
$reservaManana = irm "$api/api/reservas" -Method Post -Headers $h -ContentType "application/json" -Body (@{ fecha = $manana } | ConvertTo-Json)
irm "$api/api/reservas/me" -Headers $h | Format-Table id, fecha, estado
irm "$api/api/reservas/$($reservaHoy.id)" -Headers $h | Format-List
irm "$api/api/reservas/$($reservaManana.id)" -Method Delete -Headers $h

# ===== Lector facial =====
irm "$api/api/entradas/reconocimiento" -Method Post -Headers @{ "X-Api-Key" = "clave-del-lector-facial" } -ContentType "application/json" -Body '{"documento":"1000000001","confianza":0.93,"dispositivo":"camara-1"}' | Format-List

# ===== Estudiante: historial =====
irm "$api/api/usuarios/me/movimientos" -Headers $h | Format-Table tipo, monto, fichas, descripcion

# ===== Administrador =====
$tokenAdmin = (irm "$api/api/auth/login" -Method Post -ContentType "application/json" -Body '{"email":"admin@gastrosena.local","password":"UnaClaveSegura123"}').token
$ha = @{ Authorization = "Bearer $tokenAdmin" }
irm "$api/api/admin/reservas?fecha=$hoy" -Headers $ha | ConvertTo-Json -Depth 5
irm "$api/api/admin/entradas?fecha=$hoy" -Headers $ha | Format-Table fechaHora, documento, resultado, motivo
irm "$api/api/admin/usuarios" -Headers $ha | Format-Table id, nombre, email, rol, saldo, fichas
irm "$api/api/admin/usuarios/2/activo" -Method Patch -Headers $ha -ContentType "application/json" -Body '{"activo":false}' | Format-List
irm "$api/api/admin/usuarios/2/activo" -Method Patch -Headers $ha -ContentType "application/json" -Body '{"activo":true}' | Format-List
```

---

## 6. Thunder Client

Thunder Client es la extensión con el **icono del rayo ⚡** en la barra izquierda de VS Code.

### 6.1 Crear el entorno (una sola vez)

1. Ve a la pestaña **Env** y crea un **New Environment** llamado `GastroSENA`.
2. Agrega estas variables:

   | Variable | Valor |
   |---|---|
   | `baseUrl` | `http://localhost:5000` |
   | `token` | *(vacío)* |
   | `tokenAdmin` | *(vacío)* |

3. Guárdalo y márcalo como activo con **Set Active**.

### 6.2 Login que guarda el token automáticamente

1. Crea un **New Request** con método `POST` y URL `{{baseUrl}}/api/auth/login`.
2. En **Body → JSON**:
   ```json
   {"email":"prueba@sena.edu.co","password":"clave12345"}
   ```
3. En la pestaña **Tests**, elige **Set Env Variable** y escribe `json.token` → `{{token}}`.
4. Pulsa **Send**. El token queda guardado en el entorno.

Repite con el admin (`admin@gastrosena.local` / `UnaClaveSegura123`), guardando en `{{tokenAdmin}}`.

### 6.3 Usar el token

- En cada petición protegida, ve a **Auth → Bearer** y escribe `{{token}}`, o `{{tokenAdmin}}` para `/api/admin/...`.
- Atajo: guarda las peticiones en una **Collection** `GastroSENA` y configura en la colección **Auth → Bearer `{{token}}`**. Todas las peticiones lo heredan. En las de admin, cambia a `{{tokenAdmin}}`.
- Para el lector facial **no uses Bearer**. En la pestaña **Headers** agrega `X-Api-Key` = `clave-del-lector-facial`.

### 6.4 Peticiones para crear en Thunder Client

| Método | URL | Body (JSON) | Auth / Headers |
|---|---|---|---|
| GET | `{{baseUrl}}/health` | — | — |
| POST | `{{baseUrl}}/api/auth/registro` | `{"nombre":"Estudiante de prueba","documento":"1000000001","email":"prueba@sena.edu.co","password":"clave12345"}` | — |
| POST | `{{baseUrl}}/api/auth/login` | `{"email":"prueba@sena.edu.co","password":"clave12345"}` | Tests: `json.token` → `{{token}}` |
| POST | `{{baseUrl}}/api/auth/login` | `{"email":"admin@gastrosena.local","password":"UnaClaveSegura123"}` | Tests: `json.token` → `{{tokenAdmin}}` |
| GET | `{{baseUrl}}/api/usuarios/me` | — | Bearer `{{token}}` |
| POST | `{{baseUrl}}/api/usuarios/me/recargas` | `{"monto":50000}` | Bearer `{{token}}` |
| GET | `{{baseUrl}}/api/usuarios/me/movimientos` | — | Bearer `{{token}}` |
| GET | `{{baseUrl}}/api/fichas/precio` | — | Bearer `{{token}}` |
| POST | `{{baseUrl}}/api/fichas/compras` | `{"cantidad":5}` | Bearer `{{token}}` |
| POST | `{{baseUrl}}/api/reservas` | `{"fecha":"2026-11-20"}` (hoy o una fecha futura) | Bearer `{{token}}` |
| GET | `{{baseUrl}}/api/reservas/me` | — | Bearer `{{token}}` |
| GET | `{{baseUrl}}/api/reservas/1` | — | Bearer `{{token}}` |
| DELETE | `{{baseUrl}}/api/reservas/1` | — | Bearer `{{token}}` |
| POST | `{{baseUrl}}/api/entradas/reconocimiento` | `{"documento":"1000000001","confianza":0.93,"dispositivo":"camara-1"}` | Header `X-Api-Key: clave-del-lector-facial` |
| GET | `{{baseUrl}}/api/admin/reservas?fecha=2026-11-20` | — | Bearer `{{tokenAdmin}}` |
| GET | `{{baseUrl}}/api/admin/entradas?fecha=2026-11-20` | — | Bearer `{{tokenAdmin}}` |
| GET | `{{baseUrl}}/api/admin/usuarios` | — | Bearer `{{tokenAdmin}}` |
| PATCH | `{{baseUrl}}/api/admin/usuarios/2/activo` | `{"activo":false}` | Bearer `{{tokenAdmin}}` |

---

## 7. Archivo GastroSENA.http

El archivo `GastroSENA.http`, en la raíz del proyecto, ya trae todas las peticiones.

- **VS Code** (extensión REST Client): haz clic en **Send Request** encima de cada `###`.
- **Visual Studio**: haz clic en **Enviar solicitud** junto a cada `###`.

Después de cada login, copia el `token` de la respuesta en `@token` (estudiante) o en `@tokenAdmin` (admin), al principio del archivo.

---

## 8. Tareas de VS Code

Abre **Ctrl+Shift+P → Tasks: Run Task** y elige una tarea.

| Tarea | Qué hace | Comando equivalente (en `GastroSENA.Api`) |
|---|---|---|
| `build` (también **Ctrl+Shift+B**) | Compila y muestra los errores en el panel de Problemas | `dotnet build` |
| `API: ejecutar con recarga en caliente` | Corre la API y aplica los cambios al guardar un `.cs` | `dotnet watch run` |
| `EF: nueva migración` | Pide un nombre y crea la migración (detén la API antes) | `dotnet ef migrations add <Nombre>` |
| `EF: quitar última migración` | Deshace la última migración no aplicada | `dotnet ef migrations remove` |
| `Secretos: ver` | Muestra los user-secrets | `dotnet user-secrets list` |
| `Secretos: cambiar` | Pide clave y valor y lo guarda | `dotnet user-secrets set "<clave>" "<valor>"` |
| `NuGet: agregar paquete` | Pide el nombre del paquete y lo instala | `dotnet add package <Paquete>` |

Las migraciones nuevas se aplican solas la próxima vez que arranca la API.

---

## 9. Reglas de negocio

- Una **ficha = un almuerzo**. Comprar fichas descuenta saldo a 3.500 por ficha.
- Un usuario tiene **como máximo una reserva activa por fecha**.
- Reservar **consume una ficha**; cancelar la **devuelve**.
- Solo se cancela con **al menos un día de anticipación**: la reserva de hoy no se puede cancelar.
- La entrada al comedor exige **confianza ≥ 0.85**, usuario activo y una reserva de hoy sin consumir. Al entrar, la reserva pasa a `Consumida`.
- **"Hoy" siempre es la fecha de Colombia** (`America/Bogota`).
- **Cada intento de entrada queda registrado**, con su resultado y motivo.
- La recarga de saldo es **simulada**: no hay cobro real.
- Desactivar una cuenta **no invalida los tokens ya emitidos**. Hasta que el token expire (120 minutos), el usuario aún puede ver su perfil, recargar y comprar fichas, pero no puede reservar ni entrar al comedor.

---

## 10. Solución de problemas

| Problema | Solución |
|---|---|
| `No es posible conectar con el servidor remoto` | La API no está corriendo. Arráncala (sección 2) |
| `401 Unauthorized` en rutas protegidas | Falta el token o expiró (120 min). Vuelve a hacer login |
| `403` en `/api/admin/...` | Estás usando el token del estudiante. Usa `$ha` o `{{tokenAdmin}}` |
| `409` al registrar | Ese email o documento ya existe. Usa otros o reinicia la base (sección 11) |
| `400` al cancelar una reserva | Es de hoy o ya pasó. Solo se cancelan reservas desde mañana en adelante |
| `403` en el reconocimiento facial | Lee el campo `motivo`: confianza baja, sin reserva hoy o almuerzo ya consumido |
| `401` en el reconocimiento facial | Falta la cabecera `X-Api-Key: clave-del-lector-facial` |
| La API no arranca y menciona `Jwt:Clave` | Faltan los user-secrets o la API no está en modo `Development`. Arráncala con F5 o `dotnet run` (usan `launchSettings.json`) |
| La API no arranca: `la autentificación password falló` | PostgreSQL rechazó la contraseña. Revisa `ConnectionStrings:GastroSena` |
| La API no arranca: `address already in use` / puerto 5000 | Ya hay otra copia corriendo (VS Code, Visual Studio o una terminal). Detenla |
| F5 en VS Code pide elegir depurador | **Ctrl+Shift+P → Developer: Reload Window** y vuelve a pulsar F5 |
| `dotnet ef` no se reconoce | `dotnet tool install --global dotnet-ef --version "10.*"` y abre una terminal nueva |
| Visual Studio pregunta por el certificado de IIS Express | Responde **No**. La API usa HTTP |

---

## 11. Reiniciar la base de datos

Sirve para borrar todos los datos y empezar de cero.

1. Detén la API.
2. Ejecuta:
   ```powershell
   $env:PGPASSWORD = "postgres"
   & "C:\Program Files\PostgreSQL\18\bin\dropdb.exe" -h localhost -U postgres --if-exists gastrosena
   ```
3. Arranca la API otra vez. Crea la base, aplica las migraciones y vuelve a crear al administrador automáticamente.
