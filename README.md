# ERP PIKIS — Frontend conectado al backend .NET

React + Vite + Tailwind. Consume la API del backend ERP-PIKIS (JWT). Todo pasa por `src/services/*` (Axios).
Todos los datos (incluidos proveedores, facturas y pagos) se guardan en el servidor, no en el navegador.

## Arranque

1. **Backend**: PostgreSQL local, ajustar `appsettings.json` y ejecutar
   `dotnet run --project src/ErpPikis.Api` (o F5 en Visual Studio) → API en `http://localhost:5105` (Swagger en `/swagger`).
   Al iniciar aplica sola las migraciones de la base de datos.
2. **Frontend** (esta carpeta):
   ```bash
   npm install
   npm run dev        # http://localhost:3000
   ```
   Vite redirige `/api/*` → `http://localhost:5105` (ver `vite.config.js`), así que no hay problemas de CORS.
3. Ingresa con el admin inicial del backend: `admin` / `Pikis2026*` (cámbialo en `AdminInicial`).
   Desde **Empleados → Nuevo usuario** crea los cajeros.

Si la API corre en otro equipo, crea un `.env` (ver `.env.example`):

- `VITE_PROXY_TARGET=http://IP-DEL-SERVIDOR:5105` para `npm run dev` (sigue sin CORS), o
- `VITE_API_URL=http://IP-DEL-SERVIDOR:5105` y agrega el origen del front a `Cors:Origenes` del backend
  (necesario si publicas el front con `npm run build`).

## Qué hay en cada pantalla (y qué endpoint usa)

| Pantalla | Rol | Endpoints |
|---|---|---|
| Login | todos | `POST /api/auth/login` |
| Cambiar mi contraseña (ícono de llave) | todos | `POST /api/auth/cambiar-password` |
| Punto de venta | ambos | `/api/productos`, `/api/ventas`, `/api/clientes`, `/api/caja/turno-actual` |
| Caja (mi turno / historial) | ambos / admin | `/api/caja/*` |
| Clientes y fiado | ambos (crear/editar: admin) | `/api/clientes`, `/api/clientes/{id}/abonos` |
| Dashboard | admin | `GET /api/dashboard` |
| Inventario | admin | `/api/productos`, `/entradas`, `/ajustes`, `/movimientos`, `/api/categorias` |
| Mermas y ajustes | admin | `/api/productos/{id}/ajustes` + `GET /api/productos/movimientos?tipo=Ajuste` |
| Proveedores (compras, facturas, pagos) | admin | `/api/proveedores`, `/api/proveedores/facturas`, `/api/proveedores/pagos` |
| Empleados (usuarios, turnos, ventas por cajero) | admin | `/api/usuarios` (crear, editar, restablecer contraseña), `/api/caja/turnos`, `/api/ventas` |
| Alertas + WhatsApp | admin | `/api/alertas/*` |

Pagos del POS: **Efectivo, Nequi, Daviplata y Fiar (crédito)**. Tras cobrar se muestra el tiquete, con
PDF (`/api/ventas/{id}/ticket/pdf`) y reimpresión en térmica (`/api/ventas/{id}/imprimir`).

**Compras a proveedor:** al registrar una factura con productos, el backend sube el stock y actualiza el costo
en la misma operación. Un pago en efectivo puede salir de la caja abierta (queda como gasto del turno).

## Notas

- **Mermas:** el backend guarda los ajustes como (diferencia + motivo). El tipo (avería, vencimiento…) va como
  prefijo del motivo, p. ej. `[Avería] empaque roto`.
- **Turnos/asistencia:** corresponden a la apertura y cierre de caja de cada cajero (no hay reloj de entrada aparte).
- **Nequi/Daviplata en el POS:** no se guarda número de comprobante; el POS pide confirmar que el pago llegó.
