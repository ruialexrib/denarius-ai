using DenariusAI.Web.Api;
using DenariusAI.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DenariusAI.IntegrationTests.Api;

/// <summary>Provides test-only HTTP probes for policies and errors; never included in the production assembly.</summary>
[ApiController]
[Route("api/v1/test")]
[Authorize(Policy = ApiFoundation.AuthorizationPolicy)]
public sealed class ApiProbeController : ControllerBase
{
    /// <summary>Exercises server-side administrator role authorization.</summary>
    /// <returns>An empty successful response for administrators.</returns>
    [HttpGet("admin")]
    [Authorize(Roles = ApplicationRoles.Administrator)]
    public IActionResult Administrator() => NoContent();

    /// <summary>Exercises safe exception handling without disclosing exception messages.</summary>
    /// <returns>No response because the test deliberately throws.</returns>
    /// <exception cref="InvalidOperationException">Always thrown to test error handling.</exception>
    [HttpGet("failure")]
    public IActionResult Failure() => throw new InvalidOperationException("SENSITIVE_TEST_DETAIL");

    /// <summary>Exercises the common field validation contract.</summary>
    /// <returns>A safe validation problem response.</returns>
    [HttpGet("validation")]
    public IResult Validation() => ApiProblems.Validation(HttpContext,
        new Dictionary<string, string[]> { ["name"] = ["Indique o nome."] });
}
