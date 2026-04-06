using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace ControladorSeleniumErp;

/// <summary>
/// Implementación del navegador para iDempiere ERP.
/// Soporta selectores CSS estables porque ZK Framework genera IDs dinámicos.
/// </summary>
public class NavegadorErp(IWebDriver driver) : INavegadorErp
{
    // ── Navegación ────────────────────────────────────────────────────────────

    public void Ir(string url)
    {
        try
        {
            driver.Navigate().GoToUrl(url);
        }
        catch (Exception)
        {
            Capturar("Ir_GoToUrl");
            throw;
        }
    }

    public string ObtenerTitulo()
    {
        try { return driver.Title; }
        catch (Exception) { Capturar("ObtenerTitulo"); throw; }
    }

    public string ObtenerUrl()
    {
        try { return driver.Url; }
        catch (Exception) { Capturar("ObtenerUrl"); throw; }
    }

    public void RetrocederNavegador()
    {
        try { driver.Navigate().Back(); }
        catch (Exception) { Capturar("RetrocederNavegador"); throw; }
    }

    public void ResetearCookies(string url)
    {
        try
        {
            driver.Navigate().GoToUrl(url);
            driver.Manage().Cookies.DeleteAllCookies();
            driver.Navigate().Refresh();
        }
        catch (Exception) { Capturar("ResetearCookies"); throw; }
    }

    // ── Interacción por ID ───────────────────────────────────────────────────

    public void EscribirCampo(string id, string texto)
    {
        try { driver.FindElement(By.Id(id)).SendKeys(texto); }
        catch (Exception) { Capturar("EscribirCampo"); throw; }
    }

    public void PresionarElemento(string id)
    {
        var elemento = EsperarElemento(By.Id(id));
        ClickConFallback(elemento, "PresionarElemento");
    }

    public void LimpiarCampo(string id)
    {
        try { driver.FindElement(By.Id(id)).Clear(); }
        catch (Exception) { Capturar("LimpiarCampo"); throw; }
    }

    public string ObtenerTexto(string id)
    {
        try { return driver.FindElement(By.Id(id)).Text; }
        catch (Exception) { Capturar("ObtenerTexto"); throw; }
    }

    // ── Interacción por CSS selector (ZK IDs dinámicos) ──────────────────────

    public void EscribirCampoCss(string selectorCss, string texto)
    {
        try
        {
            var elemento = EsperarElemento(By.CssSelector(selectorCss));
            elemento.Clear();
            elemento.SendKeys(texto);
        }
        catch (Exception) { Capturar("EscribirCampoCss"); throw; }
    }

    public void EscribirCampoZk(string selectorCss, string texto)
    {
        try
        {
            var elemento = EsperarElemento(By.CssSelector(selectorCss));
            // ZK requiere eventos reales de teclado — Clear()+SendKeys() no actualiza el modelo ZK
            new Actions(driver)
                .Click(elemento)
                .KeyDown(Keys.Control).SendKeys("a").KeyUp(Keys.Control)
                .SendKeys(texto)
                .Perform();
        }
        catch (Exception) { Capturar("EscribirCampoZk"); throw; }
    }

    public void PresionarElementoCss(string selectorCss)
    {
        var elemento = EsperarElemento(By.CssSelector(selectorCss));
        ClickConFallback(elemento, "PresionarElementoCss");
    }

    public void LimpiarCampoCss(string selectorCss)
    {
        try { EsperarElemento(By.CssSelector(selectorCss)).Clear(); }
        catch (Exception) { Capturar("LimpiarCampoCss"); throw; }
    }

    public string ObtenerTextoCss(string selectorCss)
    {
        try { return EsperarElemento(By.CssSelector(selectorCss)).Text; }
        catch (Exception) { Capturar("ObtenerTextoCss"); throw; }
    }

    public bool ExisteElementoCss(string selectorCss)
    {
        try
        {
            driver.FindElement(By.CssSelector(selectorCss));
            return true;
        }
        catch (NoSuchElementException) { return false; }
    }

    // ── Interacción por XPath ─────────────────────────────────────────────────

    public void PresionarPorXPath(string xpath)
    {
        var elemento = EsperarElemento(By.XPath(xpath));
        ClickConFallback(elemento, "PresionarPorXPath");
    }

    public string ObtenerTextoPorXPath(string xpath)
    {
        try { return driver.FindElement(By.XPath(xpath)).Text; }
        catch (Exception) { Capturar("ObtenerTextoPorXPath"); throw; }
    }

    public void ScrollHastaXPath(string xpath)
    {
        var elemento = EsperarElemento(By.XPath(xpath));
        ScrollConFallback(elemento, "ScrollHastaXPath");
    }

    // ── Esperas explícitas ────────────────────────────────────────────────────

    public void EsperarElementoCss(string selectorCss, int segundos = 10)
    {
        EsperarElemento(By.CssSelector(selectorCss), segundos);
    }

    public void EsperarElementoXPath(string xpath, int segundos = 10)
    {
        EsperarElemento(By.XPath(xpath), segundos);
    }

    public void EsperarHastaQueDesaparezca(string selectorCss, int segundos = 10)
    {
        try
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(segundos));
            wait.Until(d =>
            {
                try { d.FindElement(By.CssSelector(selectorCss)); return false; }
                catch (NoSuchElementException) { return true; }
            });
        }
        catch (WebDriverTimeoutException)
        {
            // Si no desaparece en el timeout, continuamos — el test aserción decidirá
        }
    }

    public string ObtenerTextoCssSinBloqueo(string selectorCss, int timeoutSegundos = 3)
    {
        try
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSegundos));
            var elemento = wait.Until(d => d.FindElement(By.CssSelector(selectorCss)));
            return elemento.Text;
        }
        catch
        {
            return string.Empty;
        }
    }

    // ── Pestañas ──────────────────────────────────────────────────────────────

    public IList<string> ObtenerPestañas()
    {
        try { return driver.WindowHandles; }
        catch (Exception) { Capturar("ObtenerPestañas"); throw; }
    }

    public void CambiarPestaña(int indice, IList<string> pestañas)
    {
        try
        {
            if (pestañas.Count > 1)
                driver.SwitchTo().Window(pestañas[indice]);
        }
        catch (Exception) { Capturar("CambiarPestaña"); throw; }
    }

    // ── Scroll ────────────────────────────────────────────────────────────────

    public void ScrollHasta(string id)
    {
        var elemento = EsperarElemento(By.Id(id));
        ScrollConFallback(elemento, "ScrollHasta");
    }

    // ── Helpers privados ─────────────────────────────────────────────────────

    private IWebElement EsperarElemento(By localizador, int segundos = 10)
    {
        try
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(segundos));
            return wait.Until(d => d.FindElement(localizador));
        }
        catch (Exception) { Capturar("EsperarElemento"); throw; }
    }

    private void ClickConFallback(IWebElement elemento, string nombrePaso)
    {
        try
        {
            try
            {
                elemento.Click();
            }
            catch (ElementClickInterceptedException)
            {
                try { ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", elemento); }
                catch { new Actions(driver).MoveToElement(elemento).Click().Perform(); }
            }
        }
        catch (Exception) { Capturar(nombrePaso); throw; }
    }

    private void ScrollConFallback(IWebElement elemento, string nombrePaso)
    {
        try
        {
            try
            {
                new Actions(driver).ScrollToElement(elemento).Perform();
            }
            catch (MoveTargetOutOfBoundsException)
            {
                try
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript(
                        "arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", elemento);
                    Thread.Sleep(500);
                }
                catch
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript(
                        "window.scrollTo(0, arguments[0].offsetTop - 200);", elemento);
                    Thread.Sleep(300);
                }
            }
        }
        catch (Exception) { Capturar(nombrePaso); throw; }
    }

    private void Capturar(string nombrePaso)
    {
        try
        {
            if (driver is not ITakesScreenshot takesScreenshot) return;

            var screenshot = takesScreenshot.GetScreenshot();
            var folder = Path.Combine(AppContext.BaseDirectory, "screenshots");
            Directory.CreateDirectory(folder);
            var fileName = $"{DateTime.Now:yyyyMMdd_HHmmssfff}_{nombrePaso}.png";
            screenshot.SaveAsFile(Path.Combine(folder, fileName));
            Console.WriteLine($"[Screenshot] {fileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Screenshot] Error al capturar: {ex.Message}");
        }
    }
}
