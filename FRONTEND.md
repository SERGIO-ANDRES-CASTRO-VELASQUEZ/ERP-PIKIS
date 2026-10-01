# ERP-PIKIS — Guía para conectar el frontend (React)

Esta guía explica cómo consumir el backend desde React: URL, autenticación, formato de datos y errores,
qué endpoint usa cada pantalla y los casos especiales (impresión, alertas, caja).

La documentación interactiva de todos los endpoints está en **Swagger**: `http://localhost:5105/swagger`
(con el backend corriendo en modo Development).

---

## 1. Conexión

| Dato | Valor |
|---|---|
| URL base (desarrollo) | `http://localhost:5105` |
| Prefijo de rutas | `/api` |
| Formato | JSON (`Content-Type: application/json`) |
| Autenticación | JWT en el header `Authorization: Bearer <token>` |

### CORS

El backend solo acepta peticiones del navegador desde los orígenes de `Cors:Origenes` en
`src/ErpPikis.Api/appsettings.json`. Por defecto:

```json
"Cors": { "Origenes": [ "http://localhost:5173", "http://localhost:3000" ] }
```

`5173` es el puerto por defecto de Vite y `3000` el de Create React App. Si el front corre en otro puerto o
en otra IP (por ejemplo una tablet en la red de la tienda, `http://192.168.1.20:5173`), **agregarlo ahí** y
reiniciar el backend. Si no, el navegador mostrará un error de CORS.

### Alternativa sin CORS: proxy de Vite

```js
// vite.config.js
export default {
  server: {
    proxy: { '/api': 'http://localhost:5105' }
  }
}
```

Con esto el front llama a `/api/...` (misma URL que el front) y Vite reenvía al backend.

### Variable de entorno

```bash
# .env (Vite)
VITE_API_URL=http://localhost:5105
```

---

## 2. Autenticación y roles

### Login

```http
POST /api/auth/login
{ "username": "admin", "password": "Pikis2026*" }
```

Respuesta `200`:

```json
{
  "token": "eyJhbGciOi...",
  "expiraUtc": "2026-10-01T13:00:00Z",
  "usuario": { "id": 1, "nombre": "Propietaria", "username": "admin", "rol": "Administrador", "activo": true }
}
```

- Credenciales malas → `401` con `{ "detail": "Usuario o contraseña incorrectos." }`.
- El token dura **12 horas** (`Jwt:ExpiracionHoras`). Guardarlo junto con `expiraUtc` y el `usuario`.
- No hay refresh token: al expirar, cualquier llamada devuelve `401` → enviar al login.

### Roles

`usuario.rol` es `"Administrador"` (la propietaria) o `"Cajero"`. Úsenlo para mostrar u ocultar menús;
el backend igual valida y responde `403` si el rol no tiene permiso.

| Pantalla / acción | Administrador | Cajero |
|---|:-:|:-:|
| POS: vender, fiar, imprimir tiquete | ✔ | ✔ |
| Abrir / cerrar su caja, registrar gastos, recibir abonos | ✔ | ✔ |
| Consultar productos y clientes | ✔ | ✔ |
| Crear / editar productos, entradas de proveedor, ajustes | ✔ | ✘ |
| Crear / editar clientes y su límite de crédito | ✔ | ✘ |
| Dashboard, alertas, destinatarios WhatsApp | ✔ | ✘ |
| Usuarios, historial de turnos de todos | ✔ | ✘ |
| Ver ventas | todas | solo las suyas |

Otros endpoints de la sesión:

- `GET /api/auth/perfil` → usuario actual (útil al recargar la página para validar el token).
- `POST /api/auth/cambiar-password` → `{ "passwordActual": "...", "passwordNueva": "..." }` → `204`.

---

## 3. Cliente HTTP recomendado

```js
// src/api/client.js
const BASE = import.meta.env.VITE_API_URL ?? '';

export class ApiError extends Error {
  constructor(status, body) {
    super(mensajeDeError(status, body));
    this.status = status;
    this.body = body;
  }
}

function mensajeDeError(status, body) {
  if (body?.errors) return Object.values(body.errors).flat().join(' ');   // validación de campos
  if (body?.detail) return body.detail;                                   // reglas de negocio
  if (status === 401) return 'Su sesión expiró. Ingrese de nuevo.';
  if (status === 403) return 'No tiene permiso para esta acción.';
  return 'Error inesperado. Intente de nuevo.';
}

export async function api(path, { method = 'GET', body, raw = false } = {}) {
  const token = localStorage.getItem('token');
  const res = await fetch(`${BASE}${path}`, {
    method,
    headers: {
      ...(body !== undefined && { 'Content-Type': 'application/json' }),
      ...(token && { Authorization: `Bearer ${token}` }),
    },
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  if (res.status === 401 && !path.startsWith('/api/auth/login')) {
    localStorage.removeItem('token');
    window.location.href = '/login';
  }

  if (!res.ok) {
    const data = await res.json().catch(() => null);
    throw new ApiError(res.status, data);
  }

  if (raw) return res.blob();
  if (res.status === 204) return null;
  return res.json();
}
```

Uso:

```js
const productos = await api('/api/productos?busqueda=arroz');
const venta = await api('/api/ventas', { method: 'POST', body: { tipoPago: 'Efectivo', items: [...] } });
```

Con Axios es lo mismo: un interceptor de request que ponga el `Authorization` y uno de response que
redirija en `401`.

---

## 4. Formato de datos

| Tipo | Cómo llega / cómo se envía | Ejemplo |
|---|---|---|
| Nombres de campo | camelCase | `precioVenta`, `stockActual` |
| Dinero | número (pesos, sin decimales en la práctica) | `2800` |
| Enums | **texto** | `"Efectivo"`, `"Credito"`, `"Critica"` |
| Fecha y hora | ISO 8601 en **UTC** (con `Z`) | `"2026-09-30T23:15:02.48Z"` |
| Fecha sola (filtros, dashboard) | `yyyy-MM-dd` | `"2026-09-30"` |

- Mostrar fechas con hora local: `new Date(v.fecha).toLocaleString('es-CO')`.
- Formatear pesos: `new Intl.NumberFormat('es-CO', { style: 'currency', currency: 'COP', maximumFractionDigits: 0 }).format(v)`.
- Los filtros `desde` / `hasta` son días de la tienda (hora local de Colombia), el backend hace la conversión.

Valores de enums:

| Enum | Valores |
|---|---|
| `tipoPago` | `Efectivo`, `Nequi`, `Daviplata`, `Credito` |
| `rol` | `Administrador`, `Cajero` |
| `severidad` (alertas) | `Info`, `Advertencia`, `Critica` |
| `tipo` (alertas) | `StockBajo`, `ProductoAgotado`, `MargenBajo`, `CreditoCercaDelLimite`, `CarteraSinAbono`, `DescuadreCaja`, `TurnoAbiertoProlongado`, `ResumenDiario` |
| `tipo` (movimiento de caja) | `Apertura`, `Cierre`, `Ingreso`, `Egreso` |
| `tipo` (movimiento de inventario) | `Entrada`, `Venta`, `Ajuste` |

---

## 5. Errores

Todas las respuestas de error usan el formato estándar *ProblemDetails*:

```json
{ "status": 400, "title": "Regla de negocio", "detail": "Stock insuficiente de Chocoramo. Disponible: 4, solicitado: 50." }
```

Los errores de validación de campos (campo vacío, fuera de rango) traen además `errors`:

```json
{ "status": 400, "title": "One or more validation errors occurred.",
  "errors": { "Nombre": ["The Nombre field is required."] } }
```

| Código | Significado | Qué hacer en el front |
|---|---|---|
| `400` | Regla de negocio o validación | Mostrar `detail` (o `errors`) al usuario; ya viene en español |
| `401` | Sin token / token vencido / login fallido | Ir al login |
| `403` | El rol no tiene permiso (cuerpo vacío) | Ocultar esa opción para ese rol |
| `404` | No existe (producto, cliente, venta) | Mostrar `detail` |
| `409` | Otro usuario modificó el mismo producto/cliente al mismo tiempo, o dato duplicado | **Reintentar** la operación una vez; si persiste, mostrar `detail` |
| `503` | La impresora térmica no responde | Avisar "revise la impresora" y ofrecer reimprimir |
| `500` | Error interno | Mensaje genérico |

Los mensajes de `detail` ya son aptos para mostrarse tal cual (por ejemplo
*"El cliente Doña Rosa supera su límite de crédito. Disponible: 6,000."*).

---

## 6. Endpoints por pantalla

### 6.1 Arranque de la sesión del cajero

1. Login.
2. `GET /api/caja/turno-actual`
   - `204` (sin cuerpo) → no tiene caja abierta → mostrar pantalla **Abrir caja**.
   - `200` → caja abierta → ir al POS.
3. Abrir caja: `POST /api/caja/apertura` `{ "montoInicial": 50000 }`.

**Sin caja abierta no se puede vender ni recibir abonos** (el backend responde `400`
*"Debe abrir caja antes de registrar ventas."*).

### 6.2 Punto de venta (POS)

**Buscar producto**

- Por código de barras: `GET /api/productos/codigo/{codigo}` → `200` producto o `404`.
  Los lectores de código de barras USB escriben el código y envían **Enter**: escuchar el `Enter` en el
  input y consultar.
- Por nombre: `GET /api/productos?busqueda=arroz` (busca en nombre y código).

```json
{
  "id": 1, "codigoBarras": "7702004003591", "nombre": "Arroz Diana 500 g",
  "categoriaId": 1, "categoria": "Víveres",
  "precioCompra": 2300, "precioVenta": 2800, "margenPorcentaje": 17.86,
  "stockActual": 40, "stockMinimo": 10, "requiereReabastecimiento": false
}
```

El carrito se arma en el front; el **precio y el total los calcula el backend** al registrar, así que el
total del front es solo una vista previa.

**Botón "Pagar"** (efectivo, Nequi o Daviplata):

```http
POST /api/ventas
{
  "tipoPago": "Efectivo",
  "montoRecibido": 20000,
  "imprimir": true,
  "items": [ { "productoId": 1, "cantidad": 3 }, { "productoId": 2, "cantidad": 2 } ]
}
```

- `montoRecibido` solo aplica a `Efectivo` (opcional). Si es menor que el total → `400`.
- `imprimir: true` imprime el tiquete en la térmica y abre el cajón monedero (solo efectivo).

**Botón "Fiar a cliente"**: mismo endpoint con `"tipoPago": "Credito"` y `"clienteId": 5`.
Para elegir al vecino: `GET /api/clientes?busqueda=rosa` y mostrar `creditoDisponible`.

Respuesta `201`:

```json
{
  "venta": {
    "id": 1, "fecha": "2026-09-30T23:59:10Z", "tipoPago": "Efectivo",
    "total": 17300, "montoRecibido": 20000, "cambio": 2700,
    "cajero": "Nieta Laura", "clienteId": null, "cliente": null,
    "detalles": [ { "productoId": 1, "producto": "Arroz Diana 500 g", "cantidad": 3, "precioUnitario": 2800, "subtotal": 8400 } ]
  },
  "ticketImpreso": true,
  "errorImpresion": null
}
```

**Importante:** si la impresora falla, la venta **sí queda guardada** y llega `ticketImpreso: false` con
`errorImpresion`. Mostrar un aviso con botón **Reimprimir** → `POST /api/ventas/{id}/imprimir`.
No volver a registrar la venta.

Errores típicos a mostrar: stock insuficiente, límite de crédito superado, caja no abierta (todos `400`).

### 6.3 Tiquete en pantalla / PDF

`GET /api/ventas/{id}/ticket/pdf?ancho=80` (o `58`). Requiere el token, así que **no** se puede poner
directo en un `<a href>`; hay que descargarlo como blob:

```js
const blob = await api(`/api/ventas/${id}/ticket/pdf?ancho=80`, { raw: true });
const url = URL.createObjectURL(blob);
window.open(url);                 // ver o imprimir desde el navegador
// o en un <iframe src={url}> y luego iframe.contentWindow.print()
```

Reimprimir directo en la térmica (marca "REIMPRESIÓN", no abre el cajón):
`POST /api/ventas/{id}/imprimir` → `204`, o `503` si la impresora no responde.

### 6.4 Caja

| Acción | Endpoint |
|---|---|
| Estado del turno (totales en vivo) | `GET /api/caja/turno-actual` |
| Registrar gasto (bolsas, servicios...) | `POST /api/caja/egresos` `{ "monto": 3000, "descripcion": "Compra de bolsas" }` |
| Cerrar caja | `POST /api/caja/cierre` `{ "montoContado": 64000 }` |
| Historial de turnos (admin) | `GET /api/caja/turnos?desde=2026-09-01&hasta=2026-09-30` |
| Detalle de un turno (admin) | `GET /api/caja/turnos/{id}` |

Respuesta de turno (apertura, gasto, cierre y turno-actual devuelven lo mismo):

```json
{
  "id": 1, "cajero": "Nieta Laura",
  "fechaApertura": "2026-09-30T13:00:00Z", "fechaCierre": null, "abierto": true,
  "montoInicial": 50000,
  "ventasEfectivo": 13400, "ventasNequi": 2800, "ventasDaviplata": 0, "ventasCredito": 14000,
  "ingresos": 5000, "egresos": 3000,
  "efectivoEsperado": 65400, "montoContado": null, "diferencia": null,
  "movimientos": [ { "id": 1, "fecha": "...", "tipo": "Apertura", "monto": 50000, "descripcion": "Apertura de caja (base)", "cliente": null } ]
}
```

Pantalla de cierre sugerida: mostrar `efectivoEsperado`, pedir el efectivo contado, enviar el cierre y
mostrar `diferencia` (negativo = faltante, positivo = sobrante). Después del cierre volver a la pantalla
de **Abrir caja**.

### 6.5 Clientes y fiado (cartera)

| Acción | Endpoint | Rol |
|---|---|---|
| Listar / buscar | `GET /api/clientes?busqueda=rosa` | ambos |
| Ver uno | `GET /api/clientes/{id}` | ambos |
| Crear | `POST /api/clientes` `{ "nombre", "telefono", "limiteCredito" }` | admin |
| Editar | `PUT /api/clientes/{id}` (mismo cuerpo) | admin |
| Recibir abono | `POST /api/clientes/{id}/abonos` `{ "monto": 5000, "nota": "opcional" }` | ambos (con caja abierta) |

```json
{ "id": 1, "nombre": "Doña Rosa", "telefono": "3101234567", "limiteCredito": 20000, "saldoActual": 14000, "creditoDisponible": 6000 }
```

### 6.6 Inventario (solo administrador)

| Acción | Endpoint |
|---|---|
| Grilla de productos | `GET /api/productos` |
| Solo los que hay que pedir (marcar en rojo) | `GET /api/productos?soloBajoStock=true` o usar `requiereReabastecimiento` |
| Categorías | `GET /api/categorias` / `POST /api/categorias` `{ "nombre" }` |
| Crear producto | `POST /api/productos` `{ "codigoBarras", "nombre", "categoriaId", "precioCompra", "precioVenta", "stockInicial", "stockMinimo" }` |
| Editar precios / datos | `PUT /api/productos/{id}` `{ "codigoBarras", "nombre", "categoriaId", "precioCompra", "precioVenta", "stockMinimo" }` |
| Compra a proveedor (entrada) | `POST /api/productos/{id}/entradas` `{ "cantidad": 24, "costoUnitario": 1850, "motivo": "Factura Postobón 123" }` |
| Merma / ajuste | `POST /api/productos/{id}/ajustes` `{ "diferencia": -1, "motivo": "Empaque roto" }` |
| Historial (kardex) | `GET /api/productos/{id}/movimientos` |
| Desactivar | `DELETE /api/productos/{id}` |

Reglas que el backend valida (mostrar el `detail` si ocurren): el precio de venta no puede ser menor que el
de compra; el código de barras no se puede repetir; un ajuste no puede dejar el stock negativo.
El stock **no se edita directamente**: solo con entradas, ajustes y ventas (queda trazabilidad).

### 6.7 Dashboard financiero (solo administrador)

`GET /api/dashboard` (hoy) o `GET /api/dashboard?fecha=2026-09-29`.

```json
{
  "fecha": "2026-09-30",
  "ventasDelDia": 30200, "numeroVentasDelDia": 3,
  "ventasContado": 16200, "ventasCredito": 14000,
  "abonosCartera": 5000, "egresosDelDia": 3000,
  "utilidadBrutaDelDia": 5900,
  "carteraTotal": 9000, "valorInventario": 78500,
  "alertasStock": [ { "productoId": 2, "nombre": "Chocoramo", "stockActual": 4, "stockMinimo": 5 } ],
  "ventasUltimos7Dias": [ { "dia": "2026-09-24", "total": 0, "numeroVentas": 0 }, "... 7 elementos, del más antiguo a hoy" ],
  "masVendidos7Dias": [ { "productoId": 1, "nombre": "Arroz Diana 500 g", "cantidad": 9, "total": 25200 } ]
}
```

Sugerencia de tarjetas: ventas del día, ganancia del día (`utilidadBrutaDelDia`), cartera por cobrar,
valor del inventario. Gráfico de barras con `ventasUltimos7Dias` (siempre trae 7 días, incluso en cero).
Refrescar cada 1–2 minutos o al entrar a la pantalla.

### 6.8 Alertas (campana del panel, solo administrador)

| Acción | Endpoint |
|---|---|
| Contador para la campana | `GET /api/alertas/resumen` → `{ "noLeidas": 6, "criticas": 2, "advertencias": 3, "informativas": 1 }` |
| Lista | `GET /api/alertas?soloNoLeidas=true` (también `soloActivas=true`) |
| Marcar una como leída | `PUT /api/alertas/{id}/leida` |
| Marcar todas | `PUT /api/alertas/leer-todas` |
| Forzar revisión ahora | `POST /api/alertas/revisar` |

```json
{ "id": 3, "tipo": "ProductoAgotado", "severidad": "Critica", "titulo": "Producto agotado",
  "mensaje": "Chocoramo se agotó. Hay que pedirlo al proveedor.", "fecha": "...", "leida": false, "activa": true }
```

- **No hay WebSocket**: consultar `/api/alertas/resumen` cada **30–60 segundos** (polling) y mostrar el número
  en la campana; rojo si `criticas > 0`.
- `activa: true` = el problema sigue (por ejemplo sigue agotado); `false` = ya se resolvió o es un aviso
  puntual (descuadre de caja, resumen diario).
- `mensaje` puede traer saltos de línea (resumen diario): mostrar con `white-space: pre-line`.

**Configuración de WhatsApp** (pantalla de ajustes):

| Acción | Endpoint |
|---|---|
| Estado del proveedor | `GET /api/alertas/whatsapp/estado` → `{ "proveedor": "CallMeBot", "habilitado": true, "destinatariosActivos": 2 }` |
| Números registrados | `GET /api/alertas/destinatarios` |
| Agregar número | `POST /api/alertas/destinatarios` `{ "nombre": "Abuela", "telefono": "3001234567", "apiKey": "123456" }` |
| Editar / desactivar | `PUT /api/alertas/destinatarios/{id}` `{ "nombre", "telefono", "apiKey", "activo" }` (enviar `apiKey: null` para no cambiarla) |
| Eliminar | `DELETE /api/alertas/destinatarios/{id}` |
| Enviar WhatsApp de prueba | `POST /api/alertas/destinatarios/{id}/prueba` → `204` o `400` con el motivo |
| Mensajes que fallaron | `GET /api/alertas/whatsapp/notificaciones?estado=Fallida` |

La `apiKey` nunca se devuelve (solo `tieneApiKey: true/false`). En la pantalla incluir las instrucciones
para obtenerla (ver `README.md`, sección CallMeBot).

### 6.9 Usuarios (solo administrador)

| Acción | Endpoint |
|---|---|
| Listar | `GET /api/usuarios` |
| Crear cajero | `POST /api/usuarios` `{ "nombre", "username", "password", "rol": "Cajero" }` |
| Activar / desactivar | `PUT /api/usuarios/{id}/activar` / `PUT /api/usuarios/{id}/desactivar` |

### 6.10 Historial de ventas

`GET /api/ventas?desde=2026-09-01&hasta=2026-09-30` (sin fechas = hoy). El administrador puede filtrar
por cajero con `&usuarioId=2`; el cajero siempre ve solo las suyas. Detalle: `GET /api/ventas/{id}`.

---

## 7. Tipos TypeScript (opcional)

```ts
export type Rol = 'Administrador' | 'Cajero';
export type TipoPago = 'Efectivo' | 'Nequi' | 'Daviplata' | 'Credito';
export type Severidad = 'Info' | 'Advertencia' | 'Critica';

export interface Usuario { id: number; nombre: string; username: string; rol: Rol; activo: boolean }
export interface LoginResponse { token: string; expiraUtc: string; usuario: Usuario }

export interface Producto {
  id: number; codigoBarras: string | null; nombre: string; categoriaId: number; categoria: string;
  precioCompra: number; precioVenta: number; margenPorcentaje: number;
  stockActual: number; stockMinimo: number; requiereReabastecimiento: boolean;
}

export interface Cliente {
  id: number; nombre: string; telefono: string | null;
  limiteCredito: number; saldoActual: number; creditoDisponible: number;
}

export interface RegistrarVentaRequest {
  items: { productoId: number; cantidad: number }[];
  tipoPago: TipoPago;
  clienteId?: number | null;
  montoRecibido?: number | null;
  imprimir?: boolean;
}

export interface DetalleVenta { productoId: number; producto: string; cantidad: number; precioUnitario: number; subtotal: number }
export interface Venta {
  id: number; fecha: string; tipoPago: TipoPago; total: number;
  montoRecibido: number | null; cambio: number | null;
  cajero: string; clienteId: number | null; cliente: string | null; detalles: DetalleVenta[];
}
export interface VentaRegistrada { venta: Venta; ticketImpreso: boolean; errorImpresion: string | null }

export interface MovimientoCaja { id: number; fecha: string; tipo: 'Apertura' | 'Cierre' | 'Ingreso' | 'Egreso'; monto: number; descripcion: string; cliente: string | null }
export interface TurnoCaja {
  id: number; cajero: string; fechaApertura: string; fechaCierre: string | null; abierto: boolean;
  montoInicial: number; ventasEfectivo: number; ventasNequi: number; ventasDaviplata: number; ventasCredito: number;
  ingresos: number; egresos: number; efectivoEsperado: number;
  montoContado: number | null; diferencia: number | null; movimientos: MovimientoCaja[];
}

export interface Dashboard {
  fecha: string; ventasDelDia: number; numeroVentasDelDia: number;
  ventasContado: number; ventasCredito: number; abonosCartera: number; egresosDelDia: number;
  utilidadBrutaDelDia: number; carteraTotal: number; valorInventario: number;
  alertasStock: { productoId: number; nombre: string; stockActual: number; stockMinimo: number }[];
  ventasUltimos7Dias: { dia: string; total: number; numeroVentas: number }[];
  masVendidos7Dias: { productoId: number; nombre: string; cantidad: number; total: number }[];
}

export interface Alerta {
  id: number; tipo: string; severidad: Severidad; titulo: string; mensaje: string;
  fecha: string; leida: boolean; activa: boolean;
}
export interface ResumenAlertas { noLeidas: number; criticas: number; advertencias: number; informativas: number }

export interface ProblemDetails { status: number; title: string; detail?: string; errors?: Record<string, string[]> }
```

---

## 8. Checklist para probar la integración

1. Backend corriendo y Swagger abre en `http://localhost:5105/swagger`.
2. El origen del front está en `Cors:Origenes` (o se usa el proxy de Vite).
3. Login con `admin` / `Pikis2026*` devuelve token.
4. `GET /api/caja/turno-actual` responde `204` → abrir caja.
5. Crear un producto, venderlo desde el POS y ver que baja `stockActual`.
6. Fiar a un cliente y ver que sube `saldoActual`.
7. Cerrar caja y ver la `diferencia`.
8. Entrar como administrador al dashboard y a la campana de alertas.
9. Con `ImpresoraTermica:Modo = Ninguna`, vender con `imprimir: true` debe guardar la venta y devolver
   `ticketImpreso: false` (así se prueba el aviso de impresora sin tener la térmica).
