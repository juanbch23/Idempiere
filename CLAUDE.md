# CLAUDE.md — iDempiere desde Cero

Este archivo es el **blueprint** del proyecto iDempiere para **Pucha Wholesaler LLC**. Define cómo levantar, configurar y poblar iDempiere desde cero: organización, productos, proveedores, clientes, listas de precios y los ciclos completos de venta, compra y cobro. Incluye automatización, backup/restore y pruebas de aceptación con Selenium.

---

## Contexto del Negocio

**Empresa:** Pucha Wholesaler LLC  
**Tipo:** Distribuidora mayorista (productos alimenticios latinoamericanos)  
**ERP:** iDempiere 12 (Open Source ERP)  
**Moneda:** USD  
**Zona horaria:** America/Caracas  

### Proceso de Negocio a Implementar

```
Cliente hace pedido
        │
        ▼
  [1] Orden de Venta (Sales Order)
        │
        ▼
  [2] Verificar Stock en Almacén
        ├─ HAY STOCK → continuar al paso [5]
        └─ FALTA STOCK → [3] Orden de Compra al Proveedor
                              │
                              ▼
                         [4] Recepción de Mercancía
                              │
                              ▼
  [5] Despacho al Cliente (Shipment)
        │
        ▼
  [6] Factura al Cliente (AR Invoice)
        │
        ▼
  [7] Cobro (Payment)
```

---

## Stack Técnico

| Componente | Imagen Docker | Puerto | Notas |
|---|---|---|---|
| **iDempiere** | `idempiereofficial/idempiere:12-master` | 8080 | ERP principal |
| **PostgreSQL** | `postgres:16` | 5432 (interno) | BD de iDempiere |

**Redes Docker:**
- `int-net` — red interna entre iDempiere y Postgres (bridge)
- `sec-net` — red externa para nginx-proxy (producción en `store.thesispath.cloud`)

**Compose files:**
- `Docker Idempier/docker-compose-app.yml` — local dev (puerto 8080 expuesto)
- `Docker Idempier/x.yml` — producción (nginx-proxy + Let's Encrypt, dominio `store.thesispath.cloud`)

---

## Datos Disponibles (Inventario de Recursos)

| Recurso | Archivo | Estado | Contenido |
|---|---|---|---|
| **Productos** | `ExcelocsvDeProductos/Products.csv` | Listo | 445 productos (SKU, nombre, descripción) |
| **Órdenes de compra históricas** | `Pucha_Wholesaler_LLC_Purchases_by_Vendor_Detail-CARLOS.xlsx` | Listo | 1465 transacciones por proveedor/fecha |
| **ETL separar OCs** | `ReglasDeNegocio/idempiere.ipynb` | Listo | Python/Pandas: agrupa por proveedor+fecha |
| **Formato importación productos** | `ImportarProductosSimples/dict/PackOut.xml` | Listo | AD_ImpFormat para tabla M_Product (ID 532) |
| **Referencia UI** | `ReglasDeNegocio/ImagenComoDebeQuedar/` | Listo | 2 capturas mostrando org + listas de precios |
| **Scripts base** | `scripts/do.ps1`, `do.backup.ps1`, `setVars2.ps1` | Vacíos | Necesitan implementación |
| **Tests** | `tests/Integration/Stack.Tests.ps1` | Template | Necesita implementación |

### Estructura de datos — Products.csv
```
Columna 1: SKU (= ID externo)
Columna 2: Nombre del producto
Columna 3: Descripción
Columna 4: SKU repetido (campo Value)
```

### Configuración conocida (de capturas UI)
- **Tenant/Client:** Market (Client_ID=1000000)
- **Organización:** Pucha Org (AD_Org_ID=1000001)
- **Almacén:** Pucha Warehouse
- **Lista de Precios de Compra:** Una por proveedor (ej: "Belmont International Trading", esquema Purchase 2001)
- **Versiones de listas:** Formato fecha `YYYY-MM-DD` (ej: `2025-08-24`)

---

## Convenciones

### Docker / Naming
- Contenedores: `idempiere`, `postgres` (sin prefijo — proyecto standalone)
- Volúmenes: `idempiere_data`, `idempiere_config`, `idempiere_plugins`
- Variables de entorno: prefijo `IDE_` para este proyecto
- Backup: `backups/idempiere_YYYYMMDD_HHMMSS.dump` (formato pg_dump custom `-Fc`)

### Scripts PowerShell
- Reglas: igual que CloudPlatform (leer SOLO desde Machine env vars)
- Logs: `[*]` Cyan, `[+]` Green, `[-]` Red, `[!]` Yellow, `[=]` White, `[~]` Yellow
- Prefijo nombres: `do{Acción}.ps1`, `check{Qué}.ps1`
- PowerShell 5.1 compatible (NO `??`, NO `?.`, NO ternario)

### iDempiere — Tablas clave
| Tabla | Propósito |
|---|---|
| `AD_Org` | Organización |
| `M_Product` | Productos |
| `M_Warehouse` | Almacén |
| `M_Locator` | Ubicaciones dentro del almacén |
| `M_StorageOnHand` | Stock actual |
| `C_BPartner` | Socios de negocio (clientes Y proveedores) |
| `C_PriceList` | Lista de precios |
| `C_PriceList_Version` | Versión de lista de precios |
| `M_ProductPrice` | Precio por producto en cada versión |
| `C_Order` | Órdenes (ventas y compras) |
| `C_OrderLine` | Líneas de orden |
| `M_InOut` | Movimientos de inventario (recepción / despacho) |
| `M_InOutLine` | Líneas de movimiento |
| `C_Invoice` | Facturas (AR y AP) |
| `C_InvoiceLine` | Líneas de factura |
| `C_Payment` | Pagos |

### iDempiere — API REST (para setup automatizado)
```
Base URL: http://localhost:8080/api/v1
Autenticación: Basic Auth (System / System)
Endpoints usados:
  POST /models/AD_Org           → Crear organización
  POST /models/M_Product        → Crear producto
  POST /models/C_BPartner       → Crear cliente/proveedor
  POST /models/C_PriceList      → Crear lista de precios
  POST /models/C_Order          → Crear orden
  POST /process/M_Product.Import → Importar productos desde I_Product
```

### Pruebas de Aceptación (Selenium)
- Framework: **Python + pytest + selenium** (WebDriver Chrome)
- Ubicación: `tests/acceptance/`
- Ejecutar: `python -m pytest tests/acceptance/ -v`
- Los tests deben poder correr contra `localhost:8080` (dev) o contra `store.thesispath.cloud` (prod)
- Variable de entorno: `IDE_BASE_URL` (default: `http://localhost:8080`)
- Cada escenario es independiente — crea sus datos, los valida, los limpia
- ChromeDriver en modo headless para CI

---

## Escenarios de Prueba de Aceptación — Scope Completo

El usuario especificó: *"un cambio en la configuración del producto o la configuración de la forma de pago del cliente hace que se necesitan algunos pasos adicionales o de plano rompe el proceso"*. Estos escenarios deben estar controlados:

### Escenarios Felices (Happy Path)
1. **Ciclo completo con stock:** SO → verificar stock OK → Shipment → Invoice → Payment
2. **Ciclo completo sin stock:** SO → stock insuficiente → PO → Receipt → Shipment → Invoice → Payment
3. **Ciclo completo con stock parcial:** Stock parcial → PO por diferencia → Receipt parcial → Shipment completo

### Escenarios que Requieren Pasos Adicionales
4. **Cliente con crédito limitado:** Al crear SO, el sistema requiere aprobación de crédito antes de proceder
5. **Cliente con forma de pago "Contado":** Al cobrar, no se genera crédito — pago inmediato obligatorio
6. **Producto con impuesto incluido vs. excluido:** La factura muestra montos diferentes, requiere validación del total

### Escenarios que ROMPEN el proceso (deben detectarse antes)
7. **Producto sin precio en la lista de precios del cliente:** SO se crea pero precio = 0 → BLOQUEADO en validación
8. **Producto inactivo:** No puede añadirse a una SO → error explícito
9. **Cliente sin lista de precios asignada:** SO no puede completarse — lista requerida
10. **Producto sin almacén origen configurado:** Shipment falla al seleccionar ubicación
11. **Lista de precios vencida (versión expirada):** Precio no disponible → SO bloqueada
12. **Proveedor inactivo:** PO no puede crearse para ese proveedor

---

## Variables de Entorno Requeridas

Setear con: `[Environment]::SetEnvironmentVariable('IDE_VAR', 'valor', 'Machine')`

| Variable | Valor de ejemplo | Propósito |
|---|---|---|
| `IDE_DB_PASS` | `PoZz!bbsq-JQE7EJyNNX` | Contraseña BD usuario adempiere |
| `IDE_DB_ADMIN_PASS` | `2nw3(HRRMzu@#UlMUjA+` | Contraseña admin PostgreSQL |
| `IDE_BASE_URL` | `http://localhost:8080` | URL base de iDempiere |
| `IDE_SUPERUSER_PASS` | `System` | Contraseña SuperUser iDempiere |
| `IDE_ORG_NAME` | `Pucha Org` | Nombre de la organización |
| `IDE_WAREHOUSE_NAME` | `Pucha Warehouse` | Nombre del almacén |
| `IDE_CLIENT_NAME` | `Market` | Nombre del cliente/tenant |
| `IDE_BACKUP_DIR` | `.\backups` | Directorio para dumps de BD |

---

## Comandos de referencia rápida

```powershell
# Levantar stack
.\scripts\do.ps1

# Backup manual
.\scripts\do.backup.ps1

# Restaurar desde último backup
.\scripts\do.backup.ps1 -Restore

# Solo Docker (sin backup)
docker compose -f "Docker Idempier/docker-compose-app.yml" up -d

# Ver logs
docker logs idempiere -f
docker logs postgres -f

# Entrar a la BD
docker exec -it postgres psql -U adempiere -d idempiere

# Backup manual
docker exec postgres pg_dump -U adempiere idempiere -Fc > backups/manual.dump

# Restore manual
docker exec -i postgres pg_restore -U adempiere -d idempiere --clean < backups/manual.dump
```

```bash
# Correr pruebas de aceptación
python -m pytest tests/acceptance/ -v

# Correr solo un escenario
python -m pytest tests/acceptance/test_sales_cycle.py::test_full_cycle_with_stock -v

# Correr pruebas con headless desactivado (debug visual)
IDE_HEADLESS=false python -m pytest tests/acceptance/ -v
```

---

## Implementation Roadmap

Status: ⬜ No iniciado | 🔧 En progreso | ✅ Hecho

> **Orden de ejecución:**
> - Phase 0 primero (infraestructura)
> - Phase 1 (org) antes que todo lo demás
> - Phases 2, 3, 4 son independientes entre sí (paralelas)
> - Phase 5 (ventas) requiere 1+2+3+4
> - Phase 6 (compras) requiere 1+2+3
> - Phase 7 (pruebas aceptación) requiere 5+6

---

### Phase 0 — Infraestructura y Scripts ⬜

**Goal:** Levantar el stack desde cero con un solo comando. Si existe un backup, restaurarlo. Scripts de backup/restore funcionando.

#### Task 0.1 — Estructura de carpetas y `.gitignore`
- [ ] Crear `backups/` (gitignoreado)
- [ ] Crear `tests/acceptance/` con `__init__.py`
- [ ] Crear `tests/acceptance/helpers/` con `base_test.py` (clase base Selenium)
- [ ] Actualizar `.gitignore`: `backups/*.dump`, `*.pyc`, `__pycache__/`, `.env`, `*.pfx`
- [ ] Crear `requirements.txt`: `selenium`, `pytest`, `python-dotenv`, `requests`

#### Task 0.2 — `scripts/checkVars.ps1`
> **Testing:** Ejecutar el script — con vars seteadas: exit 0 todo verde. Sin vars: exit 1 rojo.
- [ ] Validar todas las variables `IDE_*` de la tabla de env vars
- [ ] Leer SOLO desde Machine scope: `[Environment]::GetEnvironmentVariable($v, 'Machine')`
- [ ] Print `[+]` verde si existe, `[-]` rojo si falta
- [ ] Exit 1 si alguna falta

#### Task 0.3 — `scripts/doCompose.ps1`
> **Testing:** Ejecutar → contenedores `idempiere` y `postgres` en estado `running/healthy`
- [ ] Verificar que Docker Desktop está corriendo (si no, intentar iniciarlo, timeout 120s)
- [ ] Ejecutar `docker compose -f "Docker Idempier/docker-compose-app.yml" up -d`
- [ ] Esperar hasta que `idempiere` esté healthy (polling, timeout 300s — iDempiere tarda)
- [ ] Imprimir URL: `http://localhost:8080/webui`

#### Task 0.4 — `scripts/do.backup.ps1`
> **Testing:** Ejecutar backup → archivo `.dump` en `backups/`. Ejecutar restore → iDempiere carga los datos del dump.
- [ ] **Backup:** `docker exec postgres pg_dump -U adempiere idempiere -Fc > backups/idempiere_YYYYMMDD_HHMMSS.dump`
- [ ] **Restore (flag `-Restore`):**
  - Detectar el dump más reciente en `backups/`
  - `docker exec -i postgres pg_restore -U adempiere -d idempiere --clean < backups/{latest}.dump`
  - Reiniciar el contenedor `idempiere` post-restore (necesario para limpiar caché)
- [ ] Idempotente: backup no falla si `backups/` no existe — lo crea
- [ ] Imprimir tamaño del archivo generado

#### Task 0.5 — `scripts/do.ps1` (Orchestrador)
> **Testing:** Ejecutar desde cero → stack levantado. Si hay backup, datos restaurados.
- [ ] `#requires -RunAsAdministrator`
- [ ] Paso 1: `checkVars.ps1` — si falla, abort
- [ ] Paso 2: `doCompose.ps1` — levantar stack vacío (fresh)
- [ ] Paso 3: Detectar si existe backup en `backups/`
  - SI existe → ejecutar `do.backup.ps1 -Restore`
  - NO existe → imprimir `[!] No hay backup — iDempiere iniciará con BD limpia`
- [ ] Paso 4: Imprimir URLs y estado final

#### Task 0.6 — `scripts/setVars2.ps1`
- [ ] Script de referencia para setear todas las variables `IDE_*` de una sola vez
- [ ] Contiene los valores reales (NO commitear si tiene contraseñas reales — está en .gitignore... pero mejor usar placeholders)
- [ ] Instrucciones comentadas para cada variable

---

### Phase 1 — Organización y Almacén ⬜

**Goal:** Crear la organización "Pucha Org" con su almacén "Pucha Warehouse" y mapear los IDs. Estos IDs son necesarios para TODOS los pasos siguientes.

**Referencia visual:** `ReglasDeNegocio/ImagenComoDebeQuedar/image (1).png` muestra la org con `AD_Org_ID=1000001` y `M_Warehouse_ID` asignado.

**Pre-requisitos:** Phase 0 completa, iDempiere corriendo.

#### Task 1.1 — Crear organización via UI + mapear IDs
> **Testing:** Selenium — login, navegar a Organization, verificar que "Pucha Org" existe y está activa
- [ ] Login como SuperUser en `http://localhost:8080/webui`
- [ ] Ir a System Configurator → Organization → Crear "Pucha Org"
- [ ] Registrar `AD_Org_ID` obtenido (verificar con: `docker exec postgres psql -U adempiere -d idempiere -c "SELECT AD_Org_ID, Name FROM AD_Org WHERE Name='Pucha Org'"`)
- [ ] Cambiar rol a "Market Admin" para operar dentro de la org
- [ ] Crear `scripts/ids.ps1` — archivo de mapeo de IDs (gitignoreado o en backup)

#### Task 1.2 — Crear almacén "Pucha Warehouse"
> **Testing:** Selenium — navegar a Warehouse and Locators, verificar "Pucha Warehouse" con locator por defecto
- [ ] Ir a Warehouse and Locators → Crear "Pucha Warehouse"
- [ ] Crear locator por defecto (ej: `Principal`)
- [ ] Mapear `M_Warehouse_ID` y `M_Locator_ID` en `scripts/ids.ps1`
- [ ] Asignar almacén a la organización (Organization Info → Warehouse)
- [ ] Registrar IDs en un script de referencia para usos futuros

#### Task 1.3 — Script `doSetup.ps1` — Setup inicial automatizable
> **Testing:** Ejecutar contra iDempiere limpio → org y almacén creados vía API REST
- [ ] Usar la API REST de iDempiere (`http://localhost:8080/api/v1`) para:
  - Verificar que la organización existe o crearla
  - Verificar que el almacén existe o crearlo
- [ ] Guardar IDs en variables de entorno de sesión o en un archivo JSON de mapeo
- [ ] Integrar en `do.ps1`: si no hay backup, ejecutar `doSetup.ps1` para inicializar

---

### Phase 2 — Catálogo de Productos ⬜

**Goal:** Importar los 445 productos desde `Products.csv` en iDempiere. Resultado: `M_Product` poblado con todos los productos activos.

**Pre-requisitos:** Phase 1 completa (org existe).

**Formato de importación disponible:** `ImportarProductosSimples/dict/PackOut.xml`  
Columnas CSV: `Value, Name, Description, SKU` → tabla `I_Product` → proceso de importación.

#### Task 2.1 — Instalar formato de importación via PackIn
> **Testing:** Selenium — navegar a Import File Loader, verificar que "Importar productos simple" existe
- [ ] Usar **Import Package (PackIn)** en iDempiere para instalar `ImportarProductosSimples/dict/PackOut.xml`
- [ ] Verificar en "Formato Importación de Datos" que el formato "Importar productos simple" aparece
- [ ] El formato mapea a tabla 532 (M_Product): Value, Name, Description, SKU

#### Task 2.2 — Script `doImportProducts.ps1`
> **Testing:** Ejecutar → 445 productos en M_Product. Verificar conteo con SQL.
- [ ] Copiar `ExcelocsvDeProductos/Products.csv` al contenedor de iDempiere
- [ ] Usar el endpoint de import de iDempiere o la UI para cargar el CSV
- [ ] Alternativamente: cargar directo en tabla `I_Product` via psql y ejecutar proceso de importación
  ```sql
  -- Cargar en tabla staging
  COPY I_Product(Value, Name, Description, SKU) FROM '/tmp/products.csv' CSV;
  -- Ejecutar importación (llama al process M_Product.Import)
  ```
- [ ] Verificar: `SELECT COUNT(*) FROM M_Product WHERE AD_Client_ID=1000000;` → debe ser ≥ 445
- [ ] Mapear al menos 5 productos clave con sus `M_Product_ID` para los tests

#### Task 2.3 — Validación de productos
> **Testing:** Selenium — buscar productos específicos en la ventana Producto, verificar campos
- [ ] Verificar que los productos importados tienen: Value, Name, Description, SKU
- [ ] Verificar que están activos (`IsActive = Y`)
- [ ] Verificar que están asignados a la organización correcta

---

### Phase 3 — Proveedores y Listas de Precios de Compra ⬜

**Goal:** Crear los proveedores extraídos del Excel y crear una lista de precios de compra por proveedor con su versión correspondiente.

**Pre-requisitos:** Phase 2 (productos existen para asignarles precio).

**Datos disponibles:** Excel con 1465 transacciones de compra agrupadas por proveedor + ETL Python que las separa.

#### Task 3.1 — Extraer proveedores únicos del Excel
> **Testing:** Script Python ejecuta → archivo `vendors.json` con lista de proveedores únicos
- [ ] Ejecutar ETL del notebook `idempiere.ipynb` (adaptar a script Python puro: `scripts/extract_vendors.py`)
- [ ] Extraer lista única de proveedores con: nombre, total de transacciones, fechas min/max
- [ ] Guardar en `data/vendors.json`
- [ ] Extraer órdenes de compra por proveedor+fecha en `data/purchase_orders/` (un archivo JSON por OC)

#### Task 3.2 — Crear proveedores en iDempiere
> **Testing:** Selenium — navegar a Tercero, buscar cada proveedor, verificar que tiene rol "Proveedor" activo
- [ ] Para cada proveedor en `data/vendors.json`: crear `C_BPartner` con:
  - `IsVendor = Y`
  - `Name = nombre del proveedor`
  - `IsActive = Y`
  - Asignar a la organización Pucha Org
- [ ] Script `doCreateVendors.ps1` o `scripts/create_vendors.py` vía API REST
- [ ] Mapear `C_BPartner_ID` de cada proveedor → `data/vendor_ids.json`

#### Task 3.3 — Crear listas de precios de compra (una por proveedor)
> **Testing:** Selenium — en Lista de Precios, verificar que existe una lista por cada proveedor con versión fechada
- [ ] Para cada proveedor: crear `C_PriceList` de tipo "Compra" (IsPurchasePriceList = Y)
  - Nombre: `{NombreProveedor}` (igual que en la imagen de referencia)
  - Moneda: USD
  - Esquema: Purchase 2001
- [ ] Crear versión de lista (`C_PriceList_Version`) con fecha del lote más reciente de compras
- [ ] Mapear `C_PriceList_ID` y `C_PriceList_Version_ID` → `data/pricelist_ids.json`
- [ ] Script: `doCreatePriceLists.ps1` vía API REST

#### Task 3.4 — Importar precios de productos por proveedor
> **Testing:** SQL — verificar que M_ProductPrice tiene registros para cada versión de lista
- [ ] Para cada OC por proveedor+fecha en `data/purchase_orders/`:
  - Cargar en `I_PriceList` (tabla staging de precios)
  - Ejecutar proceso de importación de precios
- [ ] Columnas: `M_PriceList_Version_ID`, `M_Product_ID` (buscar por SKU/Value), `PriceList`, `PriceStd`, `PriceLimit`
- [ ] Verificar: `SELECT COUNT(*) FROM M_ProductPrice WHERE M_PriceList_Version_ID IN (...)` → ≥ productos únicos

---

### Phase 4 — Clientes ⬜

**Goal:** Crear la lista de clientes de la empresa. Configurar correctamente sus formas de pago, términos y lista de precios de venta.

**Pre-requisitos:** Phase 1 (org).

**Nota crítica:** La forma de pago y crédito del cliente afecta directamente el flujo del proceso. Un cliente mal configurado puede romper el ciclo o requerir pasos adicionales (aprobación, pago inmediato). Esto se validará en los tests de aceptación de Phase 7.

#### Task 4.1 — Crear lista de precios de venta general
> **Testing:** Selenium — verificar "Lista Precios Venta General" activa con versión fechada
- [ ] Crear `C_PriceList` de tipo "Venta" (IsSalesOrderPriceList = Y):
  - Nombre: `Lista Precios Venta General`
  - Moneda: USD
- [ ] Crear versión con fecha actual
- [ ] Los precios de venta se pueden calcular a partir de los precios de compra + margen (ej: 20%)
- [ ] Cargar precios de venta en `M_ProductPrice` para esta lista

#### Task 4.2 — Crear clientes de ejemplo (mínimo 3 para tests)
> **Testing:** Selenium — buscar cada cliente, verificar campos de pago, crédito y lista de precios
- [ ] Crear al menos 3 clientes con configuraciones distintas para cubrir los escenarios de test:
  - **Cliente A — Estándar:** Términos Net 30, crédito $10,000, lista de precios general
  - **Cliente B — Contado:** Forma de pago inmediata, sin crédito, lista de precios general
  - **Cliente C — Crédito limitado:** Límite de crédito bajo ($100) para probar bloqueo
- [ ] Script `doCreateCustomers.ps1` vía API REST
- [ ] Mapear `C_BPartner_ID` de cada cliente → `data/customer_ids.json`

---

### Phase 5 — Ciclo de Ventas (Pedido → Despacho → Cobro) ⬜

**Goal:** Implementar y validar el flujo completo de venta: Orden de Venta → verificar stock → Despacho → Factura → Pago.

**Pre-requisitos:** Phases 1, 2, 3 (precios), 4 (clientes).

#### Task 5.1 — Crear Orden de Venta
> **Testing:** Selenium — crear SO para Cliente A con 2 productos, verificar Total, completar documento
- [ ] Navegar a Órdenes de Venta → Nuevo
- [ ] Seleccionar cliente (debe autoasignar lista de precios)
- [ ] Agregar líneas de productos (precio autocompleta desde lista de precios)
- [ ] Verificar total calculado correctamente
- [ ] Completar (Complete) el documento → status: `CO` (Completed)
- [ ] Registrar `C_Order_ID` para el siguiente paso

#### Task 5.2 — Verificar disponibilidad de stock
> **Testing:** SQL — antes de despachar, consultar M_StorageOnHand vs. OrderLine quantity
- [ ] Consulta: `SELECT SUM(qtyonhand) FROM M_StorageOnHand WHERE M_Product_ID = ? AND M_Warehouse_ID = ?`
- [ ] Script `checkStock.ps1`: para cada línea de la SO, comparar qty ordenada vs. disponible
- [ ] Si hay suficiente stock → continuar a Task 5.3
- [ ] Si falta stock → registrar los faltantes → activar Phase 6 (Compra)

#### Task 5.3 — Generar Despacho (Shipment)
> **Testing:** Selenium — desde la SO completada, generar Shipment, verificar inventario decrementado
- [ ] Desde la SO: botón "Generar Despachos" o navegar a M_InOut tipo Customer Shipment
- [ ] Confirmar líneas a despachar
- [ ] Completar el Shipment → stock se descuenta de `M_StorageOnHand`
- [ ] Verificar: `SELECT qtyonhand FROM M_StorageOnHand` disminuyó

#### Task 5.4 — Generar Factura AR (Invoice)
> **Testing:** Selenium — desde la SO o Shipment, generar factura, verificar importe y estado
- [ ] Desde la SO: "Generar Facturas"
- [ ] Verificar monto = suma de líneas de la SO
- [ ] Completar la factura → status: `CO`
- [ ] Registrar `C_Invoice_ID`

#### Task 5.5 — Registrar Cobro (Payment)
> **Testing:** Selenium — registrar pago contra la factura, verificar que queda en cero
- [ ] Navegar a Pagos → Nuevo pago AR
- [ ] Asignar a la factura del cliente
- [ ] Completar el pago → factura queda en estado "Pagado"
- [ ] Verificar: `SELECT IsPaid FROM C_Invoice WHERE C_Invoice_ID = ?` → `Y`

---

### Phase 6 — Ciclo de Compras (Faltante → Recepción) ⬜

**Goal:** Cuando hay stock insuficiente para una SO, generar automáticamente una Orden de Compra al proveedor, recibirla y reponer el stock.

**Pre-requisitos:** Phases 1, 2, 3 (proveedores + precios de compra).

#### Task 6.1 — Generar Orden de Compra
> **Testing:** Selenium — crear PO para proveedor con productos faltantes, completar
- [ ] Navegar a Órdenes de Compra → Nuevo
- [ ] Seleccionar proveedor → se asigna su lista de precios de compra
- [ ] Agregar líneas con cantidades a reponer (los faltantes calculados en Task 5.2)
- [ ] Completar la PO → status: `CO`
- [ ] Registrar `C_Order_ID` de la PO

#### Task 6.2 — Recibir mercancía (Material Receipt)
> **Testing:** Selenium — generar Receipt desde la PO, verificar que stock en almacén aumenta
- [ ] Desde la PO: "Generar Recepción"
- [ ] Confirmar cantidades recibidas (pueden ser parciales)
- [ ] Completar el Receipt → stock sube en `M_StorageOnHand`
- [ ] Verificar incremento de stock

#### Task 6.3 — Generar Factura AP (proveedor)
> **Testing:** Selenium — generar factura AP desde el Receipt, verificar monto
- [ ] Desde la PO o Receipt: "Generar Facturas de Proveedor"
- [ ] Completar → cuenta por pagar registrada
- [ ] Opcionalmente: registrar pago al proveedor (C_Payment AP)

#### Task 6.4 — Integración: Comprar lo faltante y retomar la venta
> **Testing:** Selenium — flujo completo: SO con stock insuficiente → PO → Receipt → Shipment → Invoice → Payment
- [ ] Script `doReplenishAndShip.ps1`: dado un `C_Order_ID` de SO:
  1. Detectar líneas con stock insuficiente
  2. Crear PO por proveedor correspondiente
  3. Recibir la PO
  4. Generar Shipment de la SO original
  5. Generar Invoice y Payment
- [ ] Este es el flujo completo del negocio

---

### Phase 7 — Pruebas de Aceptación Selenium ⬜

**Goal:** Suite completa de tests de aceptación que valida todos los escenarios del proceso de negocio, incluyendo variaciones de configuración de producto y cliente que pueden requerir pasos adicionales o romper el proceso.

**Pre-requisitos:** Phases 5 y 6 completas.

**Framework:** Python 3 + pytest + Selenium WebDriver (Chrome headless)

**Estructura de archivos:**
```
tests/
├── acceptance/
│   ├── __init__.py
│   ├── conftest.py                    # fixtures: driver, base_url, usuarios
│   ├── helpers/
│   │   ├── __init__.py
│   │   ├── base_page.py               # PageObject base
│   │   ├── login_page.py              # Login iDempiere
│   │   ├── sales_order_page.py        # PageObject SO
│   │   ├── purchase_order_page.py     # PageObject PO
│   │   ├── shipment_page.py           # PageObject Shipment
│   │   ├── invoice_page.py            # PageObject Invoice
│   │   └── payment_page.py            # PageObject Payment
│   ├── test_stack.py                  # Stack up, login OK
│   ├── test_sales_cycle.py            # Escenarios de venta
│   ├── test_purchase_cycle.py         # Escenarios de compra
│   ├── test_full_cycle.py             # Ciclo completo E2E
│   └── test_edge_cases.py             # Casos borde (rompen el proceso)
└── fixtures/
    ├── test_customer_standard.json
    ├── test_customer_cash_only.json
    └── test_products_sample.json
```

#### Task 7.1 — Setup framework Selenium
> **Testing:** `pytest tests/acceptance/test_stack.py` → 1 test verde (login OK)
- [ ] Crear `requirements.txt` con: `selenium>=4.0`, `pytest`, `requests`
- [ ] Crear `conftest.py` con fixtures: driver Chrome headless, base_url desde env var `IDE_BASE_URL`
- [ ] Crear `helpers/login_page.py` con PageObject de login
- [ ] Crear `test_stack.py`:
  - `test_idempiere_login_ok`: abrir URL, hacer login como SuperUser, verificar que aparece el menú

#### Task 7.2 — Tests: Ciclo de ventas (Happy Path)
> **Testing:** `pytest tests/acceptance/test_sales_cycle.py -v` → todos verdes
- [ ] `test_so_with_stock`: SO → Shipment → Invoice → Payment (stock suficiente)
- [ ] `test_so_total_calculation`: SO con 3 productos → verificar Total = suma correcta
- [ ] `test_payment_closes_invoice`: después del pago, `IsPaid = Y` en factura

#### Task 7.3 — Tests: Ciclo de compras
> **Testing:** `pytest tests/acceptance/test_purchase_cycle.py -v` → todos verdes
- [ ] `test_po_to_receipt`: PO → Receipt → verificar stock sube
- [ ] `test_po_price_from_vendor_list`: precio de línea PO viene de lista del proveedor

#### Task 7.4 — Tests: Ciclo completo E2E
> **Testing:** `pytest tests/acceptance/test_full_cycle.py -v` → todos verdes
- [ ] `test_full_cycle_with_stock`: SO con stock → Shipment → Invoice → Pago
- [ ] `test_full_cycle_without_stock`: SO sin stock → PO → Receipt → Shipment → Invoice → Pago
- [ ] `test_full_cycle_partial_stock`: Stock parcial → PO por diferencia → Receipt → Shipment completo → Pago

#### Task 7.5 — Tests: Casos borde y configuraciones que rompen
> **Testing:** `pytest tests/acceptance/test_edge_cases.py -v` → todos verdes
- [ ] `test_product_without_price_blocks_so`: Producto sin precio en lista → SO no puede completarse
- [ ] `test_inactive_product_rejected`: Producto inactivo → no puede añadirse a SO → error
- [ ] `test_customer_without_pricelist_blocks_so`: Cliente sin lista asignada → SO falla
- [ ] `test_customer_credit_limit_exceeded`: Cliente C (crédito bajo) → SO > límite → alerta de crédito
- [ ] `test_cash_customer_requires_immediate_payment`: Cliente B (contado) → al cobrar no hay crédito, pago requerido inmediatamente
- [ ] `test_tax_included_vs_excluded`: Dos productos (impuesto incluido vs excluido) → totales correctos en factura
- [ ] `test_inactive_vendor_blocks_po`: Proveedor inactivo → PO no puede crearse
- [ ] `test_expired_pricelist_version_blocks_price`: Versión de lista de precios vencida → precio no disponible → SO bloqueada

---

### Phase 8 — Backup con Data Completa y do.ps1 Final ⬜

**Goal:** Una vez que todas las fases están completas y las pruebas pasan, hacer un backup "golden" que al restaurarse tenga todo configurado: org, almacén, productos, proveedores, clientes, listas de precios. Desde cero, `do.ps1` levanta y restaura ese estado.

#### Task 8.1 — Backup golden post-setup
- [ ] Con todo configurado y tests pasando: `.\scripts\do.backup.ps1` → genera `backups/idempiere_golden_YYYYMMDD.dump`
- [ ] Renombrar como `backups/idempiere_golden.dump` (el do.ps1 buscará este archivo primero)
- [ ] Documentar qué datos contiene el golden backup

#### Task 8.2 — Validar levantamiento desde 0 con backup
> **Testing:** `docker compose down -v` → `.\scripts\do.ps1` → `pytest tests/acceptance/` → todos verdes
- [ ] Destruir todo: `docker compose down -v`
- [ ] Ejecutar `.\scripts\do.ps1` → debe restaurar el golden backup
- [ ] Ejecutar `pytest tests/acceptance/ -v` → todos los tests pasan sin setup manual
- [ ] Este es el objetivo final: **reproducibilidad total desde cero**

---

## Gotchas y Problemas Conocidos

- **iDempiere tarda ~3-5 minutos en iniciar** la primera vez (inicialización de BD). No asumir que falló.
- **El usuario SuperUser opera en Client=System**, para operar en "Market" cambiar rol a "Market Admin" o "Market User"
- **La organización `*` (asterisco)** en iDempiere es global. Los datos de "Pucha Org" van con `AD_Org_ID=1000001`
- **M_Product se importa en dos pasos:** primero a `I_Product` (tabla staging), luego se ejecuta el proceso de importación que valida y pasa a `M_Product`
- **Listas de precios y versiones:** La versión debe estar activa y dentro del rango de fechas válido. Una versión vencida hace que los precios no estén disponibles
- **Un producto puede estar en múltiples listas de precios** — tanto de compra (por proveedor) como de venta
- **El backup con `-Fc`** (custom format) es comprimido y más eficiente. El restore requiere `pg_restore`, NO `psql`
- **Después de un restore**, reiniciar el contenedor de iDempiere limpia los caches en memoria
- **ChromeDriver y Chrome** deben estar en la misma versión. Usar `webdriver-manager` para auto-gestionar versiones:
  ```python
  from webdriver_manager.chrome import ChromeDriverManager
  driver = webdriver.Chrome(ChromeDriverManager().install())
  ```
- **Los tests de aceptación son lentos** (Selenium + iDempiere tiene latencia). Esperar ~2-5 min para el full suite
- **iDempiere ZK UI** usa componentes JavaScript — en Selenium usar `WebDriverWait` con condiciones explícitas, nunca `time.sleep` fijo
- **La forma de pago del cliente** (`C_PaymentTerm`) define si hay crédito o es contado. Cambiar esto DESPUÉS de crear órdenes puede dejar inconsistencias
- **Los productos deben estar en la lista de precios del cliente** para poder agregarse a una SO — de lo contrario el precio queda en 0 y el documento no puede completarse

---

## Referencias

- **Docker Hub iDempiere:** https://hub.docker.com/r/idempiereofficial/idempiere
- **Wiki iDempiere — Importar Productos:** https://wiki.idempiere.org/es/Plantilla:Importar_Productos_(Ventana_ID-247_V1.0.0)
- **iDempiere REST API:** http://localhost:8080/api/v1 (Swagger disponible en `/api/v1/swagger-ui`)
- **Dominio producción:** store.thesispath.cloud
- **Compose para producción:** `Docker Idempier/x.yml` (requiere red `sec-net` de CloudPlatform)
