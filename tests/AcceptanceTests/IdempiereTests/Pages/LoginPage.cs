using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Interactions;

namespace IdempiereTests.Pages;

/// <summary>
/// PageObject del login de iDempiere.
///
/// Selectores estables (ZK genera IDs dinamicos como "nQqJt" — NO usarlos):
///   Boton Acceso  ->  #main-requestBtns-btnLogin a
///   Campo usuario ->  input[autocomplete="username"]
///   Campo clave   ->  input[autocomplete="current-password"]
///   Boton OK      ->  button.login-btn
///
/// ZK AJAX: se usa Actions (click + Ctrl+A + type) en vez de Clear()+SendKeys.
/// Clear() no dispara los eventos que ZK necesita para actualizar su modelo.
/// </summary>
public sealed class LoginPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    private static readonly By AccesoLink    = By.CssSelector("#main-requestBtns-btnLogin a");
    private static readonly By UsernameField = By.CssSelector("input[autocomplete='username']");
    private static readonly By PasswordField = By.CssSelector("input[autocomplete='current-password']");
    private static readonly By OkButton      = By.CssSelector("button.login-btn");

    public LoginPage(IWebDriver driver, string baseUrl, int timeoutSeconds = 60)
    {
        _driver  = driver;
        _baseUrl = baseUrl.TrimEnd('/');
        _wait    = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
        _wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException));
    }

    // ── Acciones atomicas ─────────────────────────────────────────────────────

    public void GoToMain() => _driver.Navigate().GoToUrl(_baseUrl);

    public void ClickAcceso()
    {
        var link = _wait.Until(d =>
        {
            try { var e = d.FindElement(AccesoLink); return e.Displayed ? e : null; }
            catch { return null; }
        })!;
        link.Click();
    }

    public void WaitForLoginForm()
    {
        _wait.Until(d =>
        {
            try { var e = d.FindElement(UsernameField); return e.Displayed && e.Enabled ? e : null; }
            catch { return null; }
        });
    }

    /// <summary>
    /// Escribe en un campo ZK usando: click (focus) -> Ctrl+A (seleccionar todo) -> type.
    /// Esto dispara todos los eventos de teclado que ZK necesita para actualizar su modelo.
    /// </summary>
    private void TypeInZkField(By locator, string value)
    {
        var field = _wait.Until(d =>
        {
            try { var e = d.FindElement(locator); return e.Displayed && e.Enabled ? e : null; }
            catch { return null; }
        })!;

        new Actions(_driver)
            .Click(field)
            .KeyDown(Keys.Control).SendKeys("a").KeyUp(Keys.Control)
            .SendKeys(value)
            .Perform();
    }

    public void EnterUsername(string username) => TypeInZkField(UsernameField, username);

    public void EnterPassword(string password) => TypeInZkField(PasswordField, password);

    public void ClickOk()
    {
        var btn = _wait.Until(d =>
        {
            try { var e = d.FindElement(OkButton); return e.Displayed && e.Enabled ? e : null; }
            catch { return null; }
        })!;
        btn.Click();
    }

    /// <summary>
    /// Espera hasta que el formulario de login desaparezca.
    /// Exito: campo username no existe o no es visible.
    /// StaleElementReferenceException = ZK reemplazo el DOM = login exitoso.
    /// </summary>
    public void WaitForLoginSuccess()
    {
        _wait.Until(d =>
        {
            try
            {
                var fields = d.FindElements(UsernameField);
                return fields.Count == 0 || !fields[0].Displayed;
            }
            catch (StaleElementReferenceException) { return true; }
        });
    }

    // ── Flujo completo ────────────────────────────────────────────────────────

    public void Login(string username = "System", string password = "System")
    {
        GoToMain();
        ClickAcceso();
        WaitForLoginForm();   // Esperar que ZK inicialice el form completamente
        EnterUsername(username);
        EnterPassword(password);
        ClickOk();
        WaitForLoginSuccess();
    }
}
