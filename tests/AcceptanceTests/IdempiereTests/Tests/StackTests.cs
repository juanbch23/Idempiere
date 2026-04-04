using NUnit.Framework;
using OpenQA.Selenium;
using IdempiereTests.Base;
using IdempiereTests.Pages;

namespace IdempiereTests.Tests;

/// <summary>
/// Tests de aceptacion — Phase 0: Stack levantado y login.
///
/// Ejecutar todos:
///   cd tests\AcceptanceTests && dotnet test --filter Category=Stack
///
/// Ejecutar uno solo:
///   dotnet test --filter "Name=Admin_IniciaSessionCorrectamente"
///
/// Variables de entorno:
///   IDE_BASE_URL    (default: http://localhost:8080)
///   IDE_ADMIN_LOGIN (default: System)
///   IDE_ADMIN_PASS  (default: System)
///   IDE_HEADLESS    (default: true)
/// </summary>
[TestFixture]
[Category("Stack")]
public class StackTests : BaseTest
{
    [Test]
    [Description("La pagina principal debe cargar y mostrar el boton Acceso")]
    public void PaginaPrincipal_CargaYMuestraBotonAcceso()
    {
        Driver.Navigate().GoToUrl(Config.BaseUrl);

        var accesoBtn = WaitForVisible(By.Id("main-requestBtns-btnLogin"));

        Assert.That(accesoBtn.Displayed, Is.True,
            "El boton de Acceso debe ser visible en la pagina principal");
    }

    [Test]
    [Description("El usuario admin (System/System) puede iniciar sesion")]
    public void Admin_IniciaSessionCorrectamente()
    {
        var loginPage = new LoginPage(Driver, Config.BaseUrl, Config.DefaultTimeoutSeconds);
        loginPage.Login(username: Config.AdminLogin, password: Config.AdminPassword);

        WaitUntilGone(By.CssSelector("input[autocomplete='username']"));

        var remaining = Driver.FindElements(By.CssSelector("input[autocomplete='username']"));
        Assert.That(remaining, Is.Empty,
            "El formulario de login no debe estar presente despues de iniciar sesion");
    }
}
