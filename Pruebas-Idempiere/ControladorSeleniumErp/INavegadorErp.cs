namespace ControladorSeleniumErp
{
    /// <summary>
    /// Abstracción del navegador para iDempiere ERP (ZK Framework).
    /// ZK genera IDs dinámicos — se usan selectores CSS y atributos estables.
    /// </summary>
    public interface INavegadorErp
    {
        // Navegación
        void Ir(string url);
        string ObtenerTitulo();
        string ObtenerUrl();
        void RetrocederNavegador();
        void ResetearCookies(string url);

        // Interacción por ID (cuando el id sea estable)
        void EscribirCampo(string id, string texto);
        void PresionarElemento(string id);
        void LimpiarCampo(string id);
        string ObtenerTexto(string id);

        // Interacción por CSS selector (para ZK IDs dinámicos)
        void EscribirCampoCss(string selectorCss, string texto);
        void PresionarElementoCss(string selectorCss);
        void LimpiarCampoCss(string selectorCss);
        string ObtenerTextoCss(string selectorCss);
        bool ExisteElementoCss(string selectorCss);

        /// <summary>
        /// Escribe en un campo ZK usando Actions: click → Ctrl+A → type.
        /// ZK necesita eventos reales de teclado para actualizar su modelo interno.
        /// Clear()+SendKeys() no funciona correctamente en ZK.
        /// </summary>
        void EscribirCampoZk(string selectorCss, string texto);

        // Interacción por XPath
        void PresionarPorXPath(string xpath);
        string ObtenerTextoPorXPath(string xpath);
        void ScrollHastaXPath(string xpath);

        // Esperas explícitas (necesarias en ZK por carga asíncrona)
        void EsperarElementoCss(string selectorCss, int segundos = 10);
        void EsperarElementoXPath(string xpath, int segundos = 10);

        /// <summary>Espera hasta que el elemento CSS DESAPAREZCA (útil post-submit).</summary>
        void EsperarHastaQueDesaparezca(string selectorCss, int segundos = 10);

        /// <summary>
        /// Obtiene texto de un elemento CSS con timeout corto.
        /// Devuelve string.Empty si no existe (no lanza excepción).
        /// </summary>
        string ObtenerTextoCssSinBloqueo(string selectorCss, int timeoutSegundos = 3);

        // Pestañas / ventanas
        IList<string> ObtenerPestañas();
        void CambiarPestaña(int indice, IList<string> pestañas);

        // Scroll
        void ScrollHasta(string id);
    }
}
