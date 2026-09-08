using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DenariusAI.Web.Controllers;

/// <summary>
/// Provides the entry point for the financial-analysis areas.
/// </summary>
[Authorize]
public sealed class AnalyticsController : Controller
{
    /// <summary>
    /// Displays the catalogue of thematic financial-analysis areas.
    /// </summary>
    public IActionResult Index() => View();
}
