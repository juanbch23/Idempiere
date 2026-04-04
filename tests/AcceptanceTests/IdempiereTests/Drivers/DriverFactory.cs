using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using IdempiereTests.Config;

namespace IdempiereTests.Drivers;

/// <summary>
/// Fabrica de WebDriver. Centraliza la creacion y configuracion de Chrome.
/// Usa el Selenium Manager incorporado en Selenium 4.6+ que detecta y descarga
/// el ChromeDriver correcto para la version de Chrome instalada — sin dependencias extra.
/// </summary>
public static class DriverFactory
{
    /// <summary>
    /// Crea un ChromeDriver configurado segun TestConfig.
    /// Selenium Manager gestiona la version del driver automaticamente.
    /// </summary>
    public static IWebDriver Create(TestConfig config)
    {
        var options = BuildOptions(config);
        var driver = new ChromeDriver(options);

        driver.Manage().Timeouts().PageLoad   = TimeSpan.FromSeconds(60);
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero; // Usamos esperas explicitas

        return driver;
    }

    private static ChromeOptions BuildOptions(TestConfig config)
    {
        var options = new ChromeOptions();

        if (config.Headless)
        {
            options.AddArgument("--headless");
        }

        options.AddArguments(
            "--no-sandbox",
            "--disable-dev-shm-usage",
            "--disable-gpu",
            "--window-size=1920,1080",
            "--disable-extensions"
        );

        return options;
    }
}
