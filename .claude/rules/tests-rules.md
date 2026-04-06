---
globs: "Pruebas-Idempiere/**/*.cs,tests/**/*.cs"
---

# Reglas de Pruebas de Aceptación — iDempiere ERP

## Stack de testing

- **Framework:** NUnit 4 + Selenium WebDriver 4 — .NET 10
- **Ubicación principal:** `Pruebas-Idempiere/` (4 proyectos)
- **Tests existentes de referencia:** `tests/AcceptanceTests/IdempiereTests/` — leer ANTES de escribir tests nuevos
- **Variables de entorno:**
  - `IDE_BASE_URL` (default: `http://localhost:8080`)
  - `IDE_ADMIN_LOGIN` (default: `System`)
  - `IDE_ADMIN_PASS` (default: `System`)
  - `IDE_HEADLESS` (default: `true`) — comentar `--headless=new` en `BaseTest.cs` para debug visual

## Estructura de proyectos

```
Pruebas-Idempiere/
├── ControladorSeleniumErp/     ← Abstracción del driver (INavegadorErp + NavegadorErp)
├── NavegadorIdempiere/         ← Page Objects (PaginaAccesoErp, futuras páginas del ERP)
├── PruebasAceptacionIdempiere/ ← Tests e2e de aceptación (BaseTest + ItCases)
└── PruebasUnitariasIdempiere/  ← Tests unitarios con Moq (sin browser, rápidos)
```

## ZK Framework — reglas críticas

iDempiere usa ZK Framework. Los `id` son **DINÁMICOS** y cambian en cada sesión/render:
- `yNFAt`, `yNFAg0` → NUNCA usar como selectores
- Usar siempre: **atributos estables** (`autocomplete`, `type`) o **clases CSS del tema**

### Selectores correctos del login

```csharp
// Botón Acceso en landing page
public const string BotonAcceso = "#main-requestBtns-btnLogin";

// Campo usuario — autocomplete es estable en ZK
public const string CampoUsuario = "input[autocomplete='username']";

// Campo contraseña — ZK usa 'current-password', NO 'password'
public const string CampoContrasena = "input[autocomplete='current-password']";

// Botón OK del form — clase login-btn es del tema ZK, NO btn-ok
public const string BotonOk = "button.login-btn";
```

### Escribir en campos ZK — OBLIGATORIO usar Actions

ZK requiere eventos reales de teclado. `Clear()` + `SendKeys()` no actualiza el modelo interno:

```csharp
// CORRECTO — dispara los eventos que ZK necesita
navegador.EscribirCampoZk(selector, valor);
// Internamente: click → Ctrl+A → SendKeys(valor)

// INCORRECTO — ZK ignora Clear() y el campo queda vacío o desincronizado
elemento.Clear();
elemento.SendKeys(valor);
```

### Flujo de login completo (siempre en este orden)

```csharp
paginaAcceso.Abrir();            // GoToUrl(baseUrl) — landing page, NO /webui/
paginaAcceso.ClickAcceso();      // Click en botón "Acceso"
paginaAcceso.EsperarFormularioLogin(); // Wait hasta que el form ZK esté listo
paginaAcceso.EscribirUsuario(u); // Actions: click+Ctrl+A+type
paginaAcceso.EscribirContrasena(p);
paginaAcceso.PresionarBotonOk();
paginaAcceso.EsperarPostLogin(); // Wait hasta que input[autocomplete='username'] desaparezca
```

## BaseTest — patrón de inyección de dependencias

```csharp
// Cada test crea su propio ServiceProvider → browser nuevo → cookies limpias
// NO llamar IniciarSesionLimpia() en [SetUp] — ya viene limpio
public abstract class BaseTest
{
    protected ServiceProvider Provider = null!;

    [SetUp]
    public void BaseSetUp()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWebDriver>(_ => new ChromeDriver(opciones));
        services.AddSingleton<INavegadorErp, NavegadorErp>();
        services.AddTransient<PaginaAccesoErp>();
        // Agregar aquí nuevos Page Objects conforme se crean
        Provider = services.BuildServiceProvider();
    }

    [TearDown]
    public void BaseTearDown()
    {
        try { driver.Quit(); } catch { }
        try { Provider.Dispose(); } catch { }
    }
}
```

## Cómo escribir un nuevo Page Object

1. Crear en `NavegadorIdempiere/` — nombre: `Pagina{NombreVentana}Erp.cs`
2. Constructor con `(INavegadorErp navegador)` — inyección directa
3. `static class Selectores` con todos los CSS selectors de esa ventana
4. Métodos atómicos (un elemento = un método)
5. Métodos de flujo compuesto (combinan atómicos)
6. Registrar en `BaseTest.cs`: `services.AddTransient<PaginaNuevaErp>()`

```csharp
public class PaginaProductosErp(INavegadorErp navegador)
{
    public static class Selectores
    {
        public const string MenuProductos = "...css estable...";
        public const string CampoBusqueda = "...css estable...";
    }

    public void AbrirMenuProductos() => navegador.PresionarElementoCss(Selectores.MenuProductos);
    public void BuscarProducto(string nombre) => navegador.EscribirCampoZk(Selectores.CampoBusqueda, nombre);
}
```

## Cómo escribir un test de aceptación

- Archivo en `PruebasAceptacionIdempiere/` — nombre: `{Modulo}ErpItCase.cs`
- Hereda de `BaseTest`
- `[SetUp]` solo resuelve los Page Objects del `Provider` — sin navegación
- Cada `[Test]` es independiente (browser propio)
- Categorías: `[Category("Acceso")]`, `[Category("Productos")]`, `[Category("Ventas")]`

```csharp
public class ProductosErpItCase : BaseTest
{
    private PaginaAccesoErp _acceso = null!;
    private PaginaProductosErp _productos = null!;

    [SetUp]
    public void Setup()
    {
        _acceso   = Provider.GetRequiredService<PaginaAccesoErp>();
        _productos = Provider.GetRequiredService<PaginaProductosErp>();
    }

    [Test, Category("Productos")]
    public void ListaProductos_DebeCargar()
    {
        _acceso.Login(Config.AdminLogin, Config.AdminPassword);
        _productos.AbrirMenuProductos();
        Assert.That(_productos.ExisteListaProductos(), Is.True);
    }
}
```

## Cómo escribir un test unitario (sin browser)

- Archivo en `PruebasUnitariasIdempiere/` — nombre: `{PageObject}Tests.cs`
- Usa `Mock<INavegadorErp>` de Moq
- Verifica que los Page Objects llamen al navegador con los selectores correctos

```csharp
[Test, Category("Unit")]
public void EscribirUsuario_DeberiaUsarEscribirCampoZk()
{
    var mock = new Mock<INavegadorErp>();
    var pagina = new PaginaAccesoErp(mock.Object);

    pagina.EscribirUsuario("System");

    mock.Verify(n => n.EscribirCampoZk(
        PaginaAccesoErp.Selectores.CampoUsuario, "System"), Times.Once);
}
```

## Ejecutar tests

```bash
# Solo unitarios (sin browser, rápido ~3s)
cd Pruebas-Idempiere && dotnet test PruebasUnitariasIdempiere

# Tests de aceptación (requiere iDempiere corriendo)
cd Pruebas-Idempiere && dotnet test PruebasAceptacionIdempiere

# Solo un módulo
dotnet test PruebasAceptacionIdempiere --filter "Category=Acceso"

# Todo (unitarios + aceptación)
cd Pruebas-Idempiere && dotnet test

# Con headless desactivado (debug visual — browser visible)
# Comentar la línea --headless=new en PruebasAceptacionIdempiere/BaseTest.cs
```

## Antipatrones — NUNCA hacer

- `Thread.Sleep(N)` fijo — usar `EsperarElementoCss`, `EsperarHastaQueDesaparezca`
- Selectores por `id` dinámico ZK (`yNFAt`, `yNFAg0`, etc.)
- `SendKeys` directo en campos ZK — usar `EscribirCampoZk` (Actions)
- `[assembly: LevelOfParallelism(N > 1)]` en aceptación — tests e2e son secuenciales
- `IniciarSesionLimpia()` en `[SetUp]` — cada test ya tiene browser nuevo
- Tests de aceptación que dependen del estado de un test anterior
