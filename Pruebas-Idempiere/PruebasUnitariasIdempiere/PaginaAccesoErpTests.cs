using Moq;
using ControladorSeleniumErp;
using NavegadorIdempiere;

namespace PruebasUnitariasIdempiere;

/// <summary>
/// Pruebas unitarias para PaginaAccesoErp.
/// Verifican que los métodos llamen al navegador con los selectores correctos
/// sin necesidad de un browser real (usando Mock).
/// </summary>
public class PaginaAccesoErpTests
{
    private Mock<INavegadorErp> _mockNavegador = null!;
    private PaginaAccesoErp _paginaAcceso = null!;

    [SetUp]
    public void Setup()
    {
        _mockNavegador = new Mock<INavegadorErp>();
        _paginaAcceso = new PaginaAccesoErp(_mockNavegador.Object);
    }

    [Test, Category("Unit")]
    public void EscribirUsuario_DeberiaUsarEscribirCampoZkConSelectorCorrecto()
    {
        _mockNavegador.Setup(n => n.EscribirCampoZk(It.IsAny<string>(), It.IsAny<string>()));

        _paginaAcceso.EscribirUsuario("System");

        _mockNavegador.Verify(n => n.EscribirCampoZk(
            PaginaAccesoErp.Selectores.CampoUsuario, "System"), Times.Once);
    }

    [Test, Category("Unit")]
    public void EscribirContrasena_DeberiaUsarEscribirCampoZkConSelectorCorrecto()
    {
        _mockNavegador.Setup(n => n.EscribirCampoZk(It.IsAny<string>(), It.IsAny<string>()));

        _paginaAcceso.EscribirContrasena("System");

        _mockNavegador.Verify(n => n.EscribirCampoZk(
            PaginaAccesoErp.Selectores.CampoContrasena, "System"), Times.Once);
    }

    [Test, Category("Unit")]
    public void PresionarBotonOk_DeberiaUsarSelectorLoginBtn()
    {
        _mockNavegador.Setup(n => n.PresionarElementoCss(It.IsAny<string>()));

        _paginaAcceso.PresionarBotonOk();

        _mockNavegador.Verify(n => n.PresionarElementoCss(
            PaginaAccesoErp.Selectores.BotonOk), Times.Once);
    }

    [Test, Category("Unit")]
    public void ClickAcceso_DeberiaUsarSelectorBotonAcceso()
    {
        _mockNavegador.Setup(n => n.PresionarElementoCss(It.IsAny<string>()));

        _paginaAcceso.ClickAcceso();

        _mockNavegador.Verify(n => n.PresionarElementoCss(
            PaginaAccesoErp.Selectores.BotonAcceso), Times.Once);
    }

    [Test, Category("Unit")]
    public void Login_DeberiaEjecutarTodosLosPasosEnOrden()
    {
        var orden = new List<string>();
        _mockNavegador.Setup(n => n.Ir(It.IsAny<string>()))
                      .Callback<string>(_ => orden.Add("Ir"));
        _mockNavegador.Setup(n => n.PresionarElementoCss(PaginaAccesoErp.Selectores.BotonAcceso))
                      .Callback(() => orden.Add("ClickAcceso"));
        _mockNavegador.Setup(n => n.EsperarElementoCss(PaginaAccesoErp.Selectores.CampoUsuario, It.IsAny<int>()))
                      .Callback<string, int>((_, __) => orden.Add("EsperarForm"));
        _mockNavegador.Setup(n => n.EscribirCampoZk(PaginaAccesoErp.Selectores.CampoUsuario, It.IsAny<string>()))
                      .Callback<string, string>((_, __) => orden.Add("EscribirUsuario"));
        _mockNavegador.Setup(n => n.EscribirCampoZk(PaginaAccesoErp.Selectores.CampoContrasena, It.IsAny<string>()))
                      .Callback<string, string>((_, __) => orden.Add("EscribirContrasena"));
        _mockNavegador.Setup(n => n.PresionarElementoCss(PaginaAccesoErp.Selectores.BotonOk))
                      .Callback(() => orden.Add("ClickOk"));
        _mockNavegador.Setup(n => n.EsperarHastaQueDesaparezca(PaginaAccesoErp.Selectores.CampoUsuario, It.IsAny<int>()))
                      .Callback<string, int>((_, __) => orden.Add("EsperarPostLogin"));

        _paginaAcceso.Login("System", "System");

        Assert.That(orden, Is.EqualTo(new[]
            { "Ir", "ClickAcceso", "EsperarForm", "EscribirUsuario", "EscribirContrasena", "ClickOk", "EsperarPostLogin" }),
            "Los pasos del login deben ejecutarse en el orden correcto");
    }

    [Test, Category("Unit")]
    public void SesionIniciadaCorrectamente_CuandoCampoUsuarioDesaparecio_DeberiaRetornarTrue()
    {
        _mockNavegador.Setup(n => n.ExisteElementoCss(PaginaAccesoErp.Selectores.CampoUsuario))
                      .Returns(false);

        Assert.That(_paginaAcceso.SesionIniciadaCorrectamente(), Is.True);
    }

    [Test, Category("Unit")]
    public void SesionIniciadaCorrectamente_CuandoCampoUsuarioSigueVisible_DeberiaRetornarFalse()
    {
        _mockNavegador.Setup(n => n.ExisteElementoCss(PaginaAccesoErp.Selectores.CampoUsuario))
                      .Returns(true);

        Assert.That(_paginaAcceso.SesionIniciadaCorrectamente(), Is.False);
    }
}
