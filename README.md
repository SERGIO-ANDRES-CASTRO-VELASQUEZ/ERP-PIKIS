# ERP-PIKIS — Backend (.NET 8)

API REST local (on-premise) para la Tienda PIKIS: ventas/POS, inventario, caja, cartera (fiao), empleados y dashboard.

Para conectar el frontend React ver **[FRONTEND.md](FRONTEND.md)**.

## Requisitos

- .NET 8 SDK (Visual Studio 2022 17.8 o superior)
- PostgreSQL 14+ local

## Estructura

| Proyecto | Capa |
|---|---|
| `ErpPikis.Api` | Controladores (API REST, JWT, Swagger) |
| `ErpPikis.Service` | Lógica de negocio, DTOs |
| `ErpPikis.Domain` | Entidades y reglas del negocio |
| `ErpPikis.Repository` | EF Core + PostgreSQL, repositorios, migraciones |

## Cómo correrlo

1. Crear en PostgreSQL un usuario con permiso para crear la base, o la base `erp_pikis`.
2. Ajustar `src/ErpPikis.Api/appsettings.json`:
   - `ConnectionStrings:ErpPikis`
   - `Jwt:Key` (mínimo 32 caracteres, secreta)
   - `AdminInicial` (usuario de la propietaria que se crea la primera vez)
3. Abrir `ErpPikis.sln` en Visual Studio y ejecutar `ErpPikis.Api` (o `dotnet run --project src/ErpPikis.Api`).
4. Al iniciar aplica las migraciones y crea el admin. Swagger: `http://localhost:5105/swagger`.
5. `POST /api/auth/login` con `admin` / `Pikis2026*`, copiar el token y usar el botón **Authorize** de Swagger.

Si prefieren crear la base a mano, `database/erp_pikis_schema.sql` tiene el esquema completo
(poner `BaseDatos:AplicarMigracionesAlIniciar` en `false`).

## Flujo típico

1. El cajero abre caja: `POST /api/caja/apertura`.
2. Ventas: `POST /api/ventas` con `tipoPago` = `Efectivo`, `Nequi`, `Daviplata` o `Credito` (+ `clienteId`).
3. Abonos de vecinos: `POST /api/clientes/{id}/abonos`. Gastos: `POST /api/caja/egresos`.
4. Cierre: `POST /api/caja/cierre` con el efectivo contado; devuelve esperado y diferencia.
5. La propietaria consulta `GET /api/dashboard`.

## Impresora térmica (tiquetes)

Configurar la sección `ImpresoraTermica` de `appsettings.json` (y los datos del encabezado en `Tienda`):

| `Modo` | Cuándo usarlo | Campo obligatorio |
|---|---|---|
| `Ninguna` | Sin impresora (solo PDF) | — |
| `Red` | Impresora con puerto Ethernet/WiFi | `Host` (IP), `Puerto` (9100) |
| `Windows` | Impresora USB instalada en el PC de la tienda | `NombreImpresora` (nombre exacto en Windows) |

- `AnchoPapelMm`: 80 o 58. `CodePage`: 850 sirve para la mayoría (tildes y ñ); si salen símbolos raros probar 858 o 1252.
- `AbrirCajonEnVentaEfectivo`: abre el cajón monedero conectado a la impresora solo en ventas nuevas en efectivo (nunca en reimpresiones).

Endpoints:

- `POST /api/ventas` con `"imprimir": true` (y `"montoRecibido"` en efectivo para imprimir el cambio). Si la impresora falla la venta igual se guarda y la respuesta trae `ticketImpreso: false` y `errorImpresion`.
- `POST /api/ventas/{id}/imprimir`: reimprime (marca "REIMPRESIÓN").
- `GET /api/ventas/{id}/ticket/pdf?ancho=80`: PDF del ancho del rollo.
- `GET /api/ventas/{id}/ticket/escpos`: bytes ESC/POS crudos.

El tiquete es un comprobante interno; no es factura electrónica DIAN.

## Alertas y WhatsApp

Las alertas se guardan en la base (panel: `GET /api/alertas`, contador: `GET /api/alertas/resumen`) y se envían por
WhatsApp a los números registrados. Sin internet quedan en cola y se reintentan solas.

| Alerta | Cuándo | Se cierra sola |
|---|---|---|
| Stock bajo / Producto agotado | Al vender o ajustar | Al reabastecer |
| Margen bajo | Precio de venta con menos de `MargenMinimoPorcentaje` | Al corregir precio/costo |
| Cliente cerca del límite | Deuda ≥ `PorcentajeLimiteCredito` del límite | Al abonar |
| Fiado sin abonos | Más de `DiasSinAbonar` días sin abonar | Al abonar o saldar |
| Faltante / sobrante en caja | Cierre con diferencia ≥ `UmbralDescuadreCaja` | — |
| Caja sin cerrar | Turno abierto más de `HorasTurnoAbierto` | Al cerrar |
| Resumen diario | Desde `ResumenDiarioHora`, una vez al día | — |

Configuración (`appsettings.json`, sección `WhatsApp:Proveedor`):

- **`CallMeBot`** (gratis, configurado por defecto): cada número que recibirá alertas debe:
  1. Guardar el contacto del bot **+34 684 783 708** (verificar el número vigente en
     https://www.callmebot.com/blog/free-api-whatsapp-messages/, a veces cambia).
  2. Enviarle por WhatsApp: `I allow callmebot to send me messages`
  3. Esperar la respuesta `API Activated for your phone number. Your APIKEY is ...` (si no llega en 2 minutos, reintentar en 24 h).
  4. Registrar ese número con su apikey en `POST /api/alertas/destinatarios`.

  La API gratuita es de uso personal y solo envía al número que activó su propia apikey.
- **`MetaCloud`** (oficial): requiere app en Meta for Developers con WhatsApp Business, `PhoneNumberId`, `AccessToken`
  y una plantilla aprobada con un parámetro en el cuerpo, por ejemplo `Alerta de la tienda: {{1}}`.

Luego registrar los números (rol administrador):

```
POST /api/alertas/destinatarios   { "nombre": "Abuela", "telefono": "3001234567", "apiKey": "123456" }
POST /api/alertas/destinatarios/{id}/prueba
```

`GET /api/alertas/whatsapp/notificaciones?estado=Fallida` muestra los mensajes que no se pudieron entregar.

Una respuesta `409` indica que otro cajero modificó el mismo producto o cliente al mismo tiempo; basta con reintentar.
