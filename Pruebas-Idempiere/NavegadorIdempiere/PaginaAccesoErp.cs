using ControladorSeleniumErp;

namespace NavegadorIdempiere
{
    /// <summary>
    /// Page Object de la pantalla de acceso (login) de iDempiere ERP.
    ///
    /// Selectores estables (ZK genera IDs dinámicos — NO usarlos):
    ///   Botón Acceso  ->  #main-requestBtns-btnLogin
    ///   Campo usuario ->  input[autocomplete='username']
    ///   Campo clave   ->  input[autocomplete='current-password']
    ///   Botón OK      ->  button.login-btn
    ///
    /// Flujo: GoTo(baseUrl) → ClickAcceso() → WaitForForm → EnterUser → EnterPass → ClickOk
    /// </summary>
    public class PaginaAccesoErp(INavegadorErp navegador)
    {
        private static readonly string UrlBase =
            (Environment.GetEnvironmentVariable("IDE_BASE_URL") ?? "http://localhost:8080")
            .TrimEnd('/');

        public static class Selectores
        {
            // Botón "Acceso" en la página principal (antes del form de login)
            public const string BotonAcceso = "#main-requestBtns-btnLogin";

            // Campo usuario (estable — atributo autocomplete)
            public const string CampoUsuario = "input[autocomplete='username']";

            // Campo contraseña (ZK usa current-password, no 'password')
            public const string CampoContrasena = "input[autocomplete='current-password']";

            // Botón OK del form de login
            public const string BotonOk = "button.login-btn";
        }

        // ── Navegación ──────────────────────────────────────────────────────

        public void Abrir() => navegador.Ir(UrlBase);

        public string ObtenerTitulo() => navegador.ObtenerTitulo();

        public string ObtenerUrlActual() => navegador.ObtenerUrl();

        // ── Pasos atómicos ──────────────────────────────────────────────────

        public void ClickAcceso()
        {
            navegador.PresionarElementoCss(Selectores.BotonAcceso);
        }

        public void EsperarFormularioLogin()
        {
            navegador.EsperarElementoCss(Selectores.CampoUsuario);
        }

        /// <summary>Escribe usuario usando Actions (ZK requiere eventos de teclado reales).</summary>
        public void EscribirUsuario(string usuario)
        {
            navegador.EscribirCampoZk(Selectores.CampoUsuario, usuario);
        }

        /// <summary>Escribe contraseña usando Actions.</summary>
        public void EscribirContrasena(string contrasena)
        {
            navegador.EscribirCampoZk(Selectores.CampoContrasena, contrasena);
        }

        public void PresionarBotonOk()
        {
            navegador.PresionarElementoCss(Selectores.BotonOk);
        }

        public void EsperarPostLogin()
        {
            // Login exitoso = campo usuario desaparece del DOM
            navegador.EsperarHastaQueDesaparezca(Selectores.CampoUsuario);
        }

        // ── Flujo completo ──────────────────────────────────────────────────

        /// <summary>
        /// Login completo: landing → Acceso → form → usuario → contraseña → OK → esperar éxito.
        /// </summary>
        public void Login(string usuario, string contrasena)
        {
            Abrir();
            ClickAcceso();
            EsperarFormularioLogin();
            EscribirUsuario(usuario);
            EscribirContrasena(contrasena);
            PresionarBotonOk();
            EsperarPostLogin();
        }

        // ── Validaciones ────────────────────────────────────────────────────

        public bool SesionIniciadaCorrectamente()
        {
            return !navegador.ExisteElementoCss(Selectores.CampoUsuario);
        }

        public bool ExisteElemento(string selectorCss)
        {
            return navegador.ExisteElementoCss(selectorCss);
        }

        public string ObtenerTextoPorCss(string selectorCss)
        {
            return navegador.ObtenerTextoCss(selectorCss);
        }
    }
}
