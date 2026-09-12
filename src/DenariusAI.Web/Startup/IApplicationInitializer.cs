namespace DenariusAI.Web.Startup;

/// <summary>Defines database initialization performed before the web host accepts requests.</summary>
public interface IApplicationInitializer
{
    /// <summary>Applies migrations and initializes required application data.</summary>
    /// <param name="application">The application providing startup services.</param>
    /// <returns>A task representing initialization completion.</returns>
    Task InitializeAsync(WebApplication application);
}
