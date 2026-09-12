using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using DenariusAI.Web.Api;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace DenariusAI.IntegrationTests.Api;

/// <summary>Verifies the API boundary and MVC regressions through the production HTTP pipeline.</summary>
public sealed class ApiFoundationTests
{
    /// <summary>Verifies missing, malformed and expired bearer credentials fail without redirects.</summary>
    /// <param name="credential">The credential scenario.</param>
    [Theory]
    [InlineData("missing")]
    [InlineData("invalid")]
    [InlineData("expired")]
    public async Task InvalidCredentialsReturnJsonUnauthorized(string credential)
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateApiClient();
        if (credential != "missing")
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                credential == "expired" ? factory.CreateAccessToken(expired: true) : "invalid-token");
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html");
        using var response = await client.GetAsync("/api/v1/info");
        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "authentication_required");
        Assert.Null(response.Headers.Location);
        Assert.Contains(response.Headers.WwwAuthenticate, value => value.Scheme == "Bearer");
    }

    /// <summary>Verifies a valid bearer ticket exposes only version metadata with the prerelease suffix.</summary>
    [Fact]
    public async Task AuthenticatedInfoReportsFullVersion()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.CreateAccessToken());
        using var response = await client.GetAsync("/api/v1/info");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("1", json.RootElement.GetProperty("apiVersion").GetString());
        Assert.Equal(factory.Services.GetRequiredService<DenariusAI.Web.Models.ApplicationInfo>().Version,
            json.RootElement.GetProperty("applicationVersion").GetString());
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Equal(TimeSpan.FromMinutes(10), factory.Services
            .GetRequiredService<IOptionsMonitor<BearerTokenOptions>>()
            .Get(ApiFoundation.AuthenticationScheme).BearerTokenExpiration);
    }

    /// <summary>Verifies cookies cannot authorize the API while MVC still redirects to login.</summary>
    [Fact]
    public async Task ApiAndMvcAuthenticationRemainSeparate()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Add("Cookie", factory.CreateMvcCookie());
        using var api = await client.GetAsync("/api/v1/info");
        await AssertProblemAsync(api, HttpStatusCode.Unauthorized, "authentication_required");
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.CreateAccessToken());
        using var mvc = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, mvc.StatusCode);
        Assert.Contains("/Account/Login", mvc.Headers.Location?.ToString());
    }

    /// <summary>Verifies MVC login remains HTML and login POST still requires an antiforgery token.</summary>
    [Fact]
    public async Task MvcLoginPreservesHtmlAndAntiforgery()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateApiClient();
        using var login = await client.GetAsync("/Account/Login");
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal("text/html", login.Content.Headers.ContentType?.MediaType);
        var html = await login.Content.ReadAsStringAsync();
        Assert.Contains("__RequestVerificationToken", html);
        Assert.Contains(factory.Services.GetRequiredService<DenariusAI.Web.Models.ApplicationInfo>().Version, html);
        using var post = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["Email"] = "fixture@example.invalid", ["Password"] = "invalid" }));
        Assert.Equal(HttpStatusCode.BadRequest, post.StatusCode);
    }

    /// <summary>Verifies only administrator claims satisfy administrator endpoint authorization.</summary>
    /// <param name="role">The role encoded in the test access ticket.</param>
    /// <param name="status">The expected HTTP status.</param>
    [Theory]
    [InlineData("User", HttpStatusCode.Forbidden)]
    [InlineData("Administrator", HttpStatusCode.NoContent)]
    public async Task RolesAreEnforced(string role, HttpStatusCode status)
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.CreateAccessToken(role));
        using var response = await client.GetAsync("/api/v1/test/admin");
        Assert.Equal(status, response.StatusCode);
        if (status == HttpStatusCode.Forbidden)
            await AssertProblemAsync(response, status, "access_denied");
    }

    /// <summary>Verifies probes are anonymous and the existing container health route remains accessible over HTTP.</summary>
    /// <param name="path">The health route.</param>
    [Theory]
    [InlineData("/api/v1/health/live")]
    [InlineData("/api/v1/health/ready")]
    [InlineData("http://localhost/health")]
    public async Task HealthRoutesRemainAvailable(string path)
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateApiClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("sqlserver", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies dependency failure affects readiness but not liveness, without leaking diagnostics.</summary>
    [Fact]
    public async Task FailedDependencyOnlyFailsReadiness()
    {
        await using var factory = new ApiWebApplicationFactory();
        await using var failing = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddHealthChecks().AddCheck("failing", () => HealthCheckResult.Unhealthy("SENSITIVE_TEST_DETAIL"))));
        using var client = failing.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var ready = await client.GetAsync("/api/v1/health/ready");
        await AssertProblemAsync(ready, HttpStatusCode.ServiceUnavailable, "service_unavailable");
        Assert.DoesNotContain("SENSITIVE_TEST_DETAIL", await ready.Content.ReadAsStringAsync());
        using var live = await client.GetAsync("/api/v1/health/live");
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
    }

    /// <summary>Verifies HTTP is rejected even if a caller supplies an untrusted forwarding header.</summary>
    [Fact]
    public async Task PlaintextRequestsAreRejectedWithoutRedirect()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");
        using var response = await client.GetAsync("http://localhost/api/v1/info");
        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "https_required");
        Assert.Null(response.Headers.Location);
    }

    /// <summary>Verifies the API cannot fall through to MVC, including paths resembling files.</summary>
    /// <param name="path">The unsupported API path.</param>
    [Theory]
    [InlineData("/api/v2/info")]
    [InlineData("/api/v1/unknown.json")]
    public async Task UnknownRoutesHaveProtectedJsonFallback(string path)
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateApiClient();
        using var anonymous = await client.GetAsync(path);
        await AssertProblemAsync(anonymous, HttpStatusCode.Unauthorized, "authentication_required");
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.CreateAccessToken());
        using var authenticated = await client.GetAsync(path);
        await AssertProblemAsync(authenticated, HttpStatusCode.NotFound, "not_found");
    }

    /// <summary>Verifies unhandled exceptions use safe JSON instead of the MVC error page.</summary>
    [Fact]
    public async Task ExceptionsDoNotDiscloseDetails()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.CreateAccessToken());
        using var response = await client.GetAsync("/api/v1/test/failure");
        await AssertProblemAsync(response, HttpStatusCode.InternalServerError, "server_error");
        Assert.DoesNotContain("SENSITIVE_TEST_DETAIL", await response.Content.ReadAsStringAsync());
    }

    /// <summary>Verifies oversized requests are rejected before endpoint execution.</summary>
    [Fact]
    public async Task OversizedRequestsAreRejected()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateApiClient();
        using var response = await client.PostAsync("/api/v1/info",
            new ByteArrayContent(new byte[ApiBoundaryMiddleware.MaximumRequestBodySize + 1]));
        await AssertProblemAsync(response, HttpStatusCode.RequestEntityTooLarge, "payload_too_large");
    }

    /// <summary>Verifies the validation response includes stable field and problem metadata.</summary>
    [Fact]
    public async Task ValidationUsesCommonProblemContract()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.CreateAccessToken());
        using var response = await client.GetAsync("/api/v1/test/validation");
        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation_failed");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Indique o nome.", json.RootElement.GetProperty("errors").GetProperty("name")[0].GetString());
    }

    /// <summary>Verifies the API rate limit also bounds unauthenticated requests.</summary>
    [Fact]
    public async Task ExcessiveRequestsReceiveRetryAdvice()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateApiClient();
        for (var request = 0; request < 60; request++)
        {
            using var allowed = await client.GetAsync("/api/v1/info");
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }
        using var rejected = await client.GetAsync("/api/v1/info");
        await AssertProblemAsync(rejected, HttpStatusCode.TooManyRequests, "rate_limit_exceeded");
        Assert.NotNull(rejected.Headers.RetryAfter);
    }

    /// <summary>Asserts the shared machine-readable JSON error contract.</summary>
    /// <param name="response">The received HTTP response.</param>
    /// <param name="status">The expected HTTP status.</param>
    /// <param name="code">The expected stable problem code.</param>
    /// <returns>A task representing assertion completion.</returns>
    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)status, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(code, json.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
    }
}
