# ERP PIKIS — Frontend conectado al backend .NET

React + Vite + Tailwind. Consume la API `ERP-PIKIS-backend` (JWT). Todo pasa por `src/services/*` (Axios).

## Arranque

1. **Backend** (carpeta `ERP-PIKIS-backend`): PostgreSQL local, ajustar `appsettings.json` y ejecutar
   `dotnet run --project src/ErpPikis.Api` → API en `http://localhost:5105` (Swagger en `/swagger`).
2. **Frontend** (esta carpeta):
   ```bash
   npm install
   npm run dev        # http://localhost:3000
   ```
   Vite redirige `/api/*` → `http://localhost:5105` (ver `vite.config.js`), así que no hay problemas de CORS.
3. Ingresa con el admin inicial del backend: `admin` / `Pikis2026*` (cámbialo en `AdminInicial`).
   Desde **Empleados → Nuevo usuario** crea los cajeros.

Si sirves el front en otro equipo/puerto: copia `.env.example` a `.env`, pon `VITE_API_URL` y agrega ese origen a `Cors:Origenes` del backend.

## Qué hay en cada pantalla (y qué endpoint usa)

| Pantalla | Rol | Endpoints |
|---|---|---|
| Login | todos | `POST /api/auth/login` |
| Punto de venta | ambos | `/api/productos`, `/api/ventas`, `/api/clientes`, `/api/caja/turno-actual` |
| Caja (mi turno / historial) | ambos / admin | `/api/caja/*` |
| Clientes y fiado | ambos (crear/editar: admin) | `/api/clientes`, `/api/clientes/{id}/abonos` |
| Dashboard | admin | `GET /api/dashboard` |
| Inventario | admin | `/api/productos`, `/entradas`, `/ajustes`, `/movimientos`, `/api/categorias` |
| Mermas y ajustes | admin | `/api/productos/{id}/ajustes` + kardex |
| Empleados (usuarios, turnos, ventas por cajero) | admin | `/api/usuarios`, `/api/caja/turnos`, `/api/ventas` |
| Alertas + WhatsApp | admin | `/api/alertas/*` |
| Proveedores | admin | entradas → `/api/productos/{id}/entradas`; el resto **local** (ver abajo) |

Pagos del POS: **Efectivo, Nequi, Daviplata y Fiar (crédito)**. Tras cobrar se muestra el tiquete, con
PDF (`/api/ventas/{id}/ticket/pdf`) y reimpresión en térmica (`/api/ventas/{id}/imprimir`).

## Limitaciones conocidas del backend (no del front)

- **Proveedores, facturas y pagos a proveedores no existen en el backend.** Se guardan en `localStorage`
  (solo en ese navegador/equipo). Las entradas de mercancía sí se envían al servidor. Para centralizarlo hay que
  crear en el backend las entidades Proveedor, FacturaProveedor y PagoProveedor y sus endpoints, y cambiar
  `src/store/proveedoresStore.js` por un servicio.
- **Mermas:** el backend solo tiene `ajustes` (diferencia + motivo). El tipo (avería, vencimiento…) se guarda como
  prefijo del motivo, p. ej. `[Avería] empaque roto`, y el reporte se arma leyendo el kardex de cada producto.
- **Turnos/asistencia:** corresponden a la apertura y cierre de caja de cada cajero (no hay reloj de entrada aparte).
- **Nequi/Daviplata:** el backend no guarda número de comprobante; el POS pide confirmar que el pago llegó.
- **Editar usuarios:** el backend solo permite crear, activar y desactivar (no editar nombre/rol ni resetear contraseña).
