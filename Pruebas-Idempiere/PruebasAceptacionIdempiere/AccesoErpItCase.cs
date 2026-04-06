using Microsoft.Extensions.DependencyInjection;
using NavegadorIdempiere;

namespace PruebasAceptacionIdempiere
{
    /// <summary>
    /// Pruebas de aceptación — acceso (login) de iDempiere ERP.
    ///
    /// Variables de entorno:
    ///   IDE_BASE_URL    (default: http://localhost:8080)
    ///   IDE_ADMIN_LOGIN (default: System)
    ///   IDE_ADMIN_PASS  (default: System)
    /// </summary>
    public class AccesoErpItCase : BaseTest
    {
        private PaginaAccesoErp _paginaAcceso = null!;

        private static readonly string UsuarioAdmin =
            Environment.GetEnvironmentVariable("IDE_ADMIN_LOGIN") ?? "System";
        private static readonly string ContrasenaAdmin =
            Environment.GetEnvironmentVariable("IDE_ADMIN_PASS") ?? "System";

        [SetUp]
        public void Setup()
        {
            // Cada test tiene browser propio (BaseSetUp recrea ServiceProvider+IWebDriver)
            _paginaAcceso = Provider.GetRequiredService<PaginaAccesoErp>();
        }

        [Test]
        [Category("Acceso")]
        public void PaginaPrincipal_DebeMostrarBotonAcceso()
        {
            _paginaAcceso.Abrir();

            var existe = _paginaAcceso.ExisteElemento(PaginaAccesoErp.Selectores.BotonAcceso);
            Assert.That(existe, Is.True, "El botón Acceso debe estar en la página principal");
        }

        [Test]
        [Category("Acceso")]
        public void Login_ConCredencialesValidas_DeberiaAccederAlERP()
        {
            _paginaAcceso.Login(UsuarioAdmin, ContrasenaAdmin);

            var sesionActiva = _paginaAcceso.SesionIniciadaCorrectamente();
            Assert.That(sesionActiva, Is.True,
                "El formulario de login no debe estar presente después de iniciar sesión");
        }

        [Test]
        [Category("Acceso")]
        public void FormLogin_CampoUsuario_DebeExistirTrasClickAcceso()
        {
            _paginaAcceso.Abrir();
            _paginaAcceso.ClickAcceso();
            _paginaAcceso.EsperarFormularioLogin();

            var existe = _paginaAcceso.ExisteElemento(PaginaAccesoErp.Selectores.CampoUsuario);
            Assert.That(existe, Is.True, "El campo usuario debe aparecer tras hacer click en Acceso");
        }

        [Test]
        [Category("Acceso")]
        public void FormLogin_CampoContrasena_DebeExistirTrasClickAcceso()
        {
            _paginaAcceso.Abrir();
            _paginaAcceso.ClickAcceso();
            _paginaAcceso.EsperarFormularioLogin();

            var existe = _paginaAcceso.ExisteElemento(PaginaAccesoErp.Selectores.CampoContrasena);
            Assert.That(existe, Is.True, "El campo contraseña debe aparecer tras hacer click en Acceso");
        }

        [Test]
        [Category("Acceso")]
        public void FormLogin_BotonOk_DebeExistirTrasClickAcceso()
        {
            _paginaAcceso.Abrir();
            _paginaAcceso.ClickAcceso();
            _paginaAcceso.EsperarFormularioLogin();

            var existe = _paginaAcceso.ExisteElemento(PaginaAccesoErp.Selectores.BotonOk);
            Assert.That(existe, Is.True, "El botón OK debe aparecer en el form de login");
        }
    }
}
