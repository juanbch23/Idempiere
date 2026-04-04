using NUnit.Framework;
using NUnit.Framework.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using IdempiereTests.Config;
using IdempiereTests.Drivers;

namespace IdempiereTests.Base;

/// <summary>
/// Clase base para todos los tests de aceptacion.
/// Proporciona Driver, Config, Wait y helpers reutilizables.
/// Guarda screenshot en disco cuando un test falla (util para debug en CI).
/// </summary>
public abstract class BaseTest
{
    protected IWebDriver Driver { get; private set; } = null!;
    protected TestConfig Config { get; private set; } = null!;
    protected WebDriverWait Wait { get; private set; } = null!;

    [SetUp]
    public void SetUp()
    {
        Config = new TestConfig();
        Driver = DriverFactory.Create(Config);
        Wait   = new WebDriverWait(Driver, TimeSpan.FromSeconds(Config.DefaultTimeoutSeconds));
        Wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException));
    }

    [TearDown]
    public void TearDown()
    {
        // Guardar screenshot y URL si el test fallo
        if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
        {
            TrySaveDebugInfo();
        }

        Driver?.Quit();
        Driver?.Dispose();
    }

    // ── Helpers de espera ─────────────────────────────────────────────────────

    protected IWebElement WaitForVisible(By locator) =>
        Wait.Until(d =>
        {
            try
            {
                var el = d.FindElement(locator);
                return el.Displayed ? el : null;
            }
            catch (StaleElementReferenceException) { return null; }
            catch (NoSuchElementException) { return null; }
        })!;

    protected void WaitUntilGone(By locator) =>
        Wait.Until(d =>
        {
            try
            {
                var els = d.FindElements(locator);
                return els.Count == 0 || !els[0].Displayed;
            }
            catch (StaleElementReferenceException) { return true; }
        });

    protected bool IsVisible(By locator)
    {
        try { return Driver.FindElement(locator).Displayed; }
        catch { return false; }
    }

    // ── Debug en fallo ────────────────────────────────────────────────────────

    private void TrySaveDebugInfo()
    {
        try
        {
            var testName = TestContext.CurrentContext.Test.Name;
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-failures");
            Directory.CreateDirectory(dir);

            // Screenshot
            if (Driver is ITakesScreenshot ss)
            {
                var path = Path.Combine(dir, $"{testName}.png");
                ss.GetScreenshot().SaveAsFile(path);
                TestContext.WriteLine($"[DEBUG] Screenshot: {path}");
            }

            // URL actual y titulo
            TestContext.WriteLine($"[DEBUG] URL: {Driver.Url}");
            TestContext.WriteLine($"[DEBUG] Title: {Driver.Title}");

            // Elementos visibles (para detectar mensajes de error de iDempiere)
            var errorTexts = Driver
                .FindElements(By.CssSelector(".z-errbox, .z-messagebox, .z-label"))
                .Where(e => e.Displayed && !string.IsNullOrWhiteSpace(e.Text))
                .Take(5)
                .Select(e => e.Text.Trim());

            foreach (var txt in errorTexts)
            {
                TestContext.WriteLine($"[DEBUG] Elemento visible: {txt}");
            }
        }
        catch
        {
            // No interrumpir el TearDown si el debug falla
        }
    }
}
