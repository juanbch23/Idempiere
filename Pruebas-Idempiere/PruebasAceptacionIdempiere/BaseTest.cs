using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using ControladorSeleniumErp;
using NavegadorIdempiere;
using NUnit.Framework;

// Pruebas de aceptación: secuenciales — cada test abre/cierra su propio browser
[assembly: Parallelizable(ParallelScope.None)]

namespace PruebasAceptacionIdempiere
{
    /// <summary>
    /// Clase base para todas las pruebas de aceptación de iDempiere.
    /// Configura el contenedor DI, el WebDriver (local o Grid) y los Page Objects.
    /// </summary>
    public abstract class BaseTest
    {
        protected ServiceProvider Provider = null!;
        private IWebDriver _driver = null!;

        [SetUp]
        public void BaseSetUp()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IWebDriver>(_ =>
            {
                var opciones = new ChromeOptions();
                opciones.AcceptInsecureCertificates = true;
                opciones.AddArguments("--headless=new");
                opciones.AddArguments("--no-sandbox");
                opciones.AddArguments("--disable-gpu");
                opciones.AddArguments("--disable-dev-shm-usage");
                opciones.AddArguments("--window-size=1920,1080");
                opciones.AddArguments("--disable-web-security");
                opciones.AddArguments("--remote-allow-origins=*");
                // Necesario para que ZK Framework no bloquee la carga
                opciones.AddArguments("--disable-blink-features=AutomationControlled");

                var gridUrl = Environment.GetEnvironmentVariable("SELENIUM_HUB_URL");
                var usarGrid = Environment.GetEnvironmentVariable("SELENIUM_GRID_ENABLED") == "true";

                if (usarGrid && !string.IsNullOrEmpty(gridUrl))
                {
                    Console.WriteLine($"[GRID] Conectando a Selenium Grid: {gridUrl}");
                    return new RemoteWebDriver(
                        new Uri(gridUrl),
                        opciones.ToCapabilities(),
                        TimeSpan.FromMinutes(5));
                }

                Console.WriteLine("[LOCAL] Usando ChromeDriver local");
                var driver = new ChromeDriver(opciones);
                driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero; // Esperas explícitas siempre
                return driver;
            });

            services.AddSingleton<INavegadorErp, NavegadorErp>();
            services.AddTransient<PaginaAccesoErp>();

            Provider = services.BuildServiceProvider();
            _driver = Provider.GetRequiredService<IWebDriver>();
        }

        [TearDown]
        public void BaseTearDown()
        {
            try { _driver.Quit(); } catch { /* silencio */ }
            try { _driver.Dispose(); } catch { /* silencio */ }
            try { Provider.Dispose(); } catch { /* silencio */ }
        }
    }
}
