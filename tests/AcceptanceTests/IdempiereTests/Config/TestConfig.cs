namespace IdempiereTests.Config;

/// <summary>
/// Lee la configuración de los tests desde variables de entorno.
/// Todas las variables son opcionales y tienen valores por defecto para dev local.
/// </summary>
public sealed class TestConfig
{
    /// <summary>URL base de iDempiere. Ej: http://localhost:8080</summary>
    public string BaseUrl { get; }

    /// <summary>Login (value) del usuario admin de iDempiere. Ej: System, superusr.</summary>
    public string AdminLogin { get; }

    /// <summary>Contraseña del usuario admin de iDempiere.</summary>
    public string AdminPassword { get; }

    /// <summary>Si true, Chrome corre en modo headless (sin ventana).</summary>
    public bool Headless { get; }

    /// <summary>Timeout por defecto en segundos para esperas explícitas.</summary>
    public int DefaultTimeoutSeconds { get; }

    public TestConfig()
    {
        BaseUrl = (Environment.GetEnvironmentVariable("IDE_BASE_URL") ?? "http://localhost:8080")
                  .TrimEnd('/');

        AdminLogin    = Environment.GetEnvironmentVariable("IDE_ADMIN_LOGIN")    ?? "System";
        AdminPassword = Environment.GetEnvironmentVariable("IDE_ADMIN_PASS")     ?? "System";

        Headless = (Environment.GetEnvironmentVariable("IDE_HEADLESS") ?? "true")
                   .Trim().ToLowerInvariant() != "false";

        DefaultTimeoutSeconds = int.TryParse(
            Environment.GetEnvironmentVariable("IDE_TIMEOUT_SECONDS"), out var t) ? t : 60;
    }
}
