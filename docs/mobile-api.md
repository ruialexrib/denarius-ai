# Mobile API foundation

This document describes Module 1 of issue #138. The API lives in
`DenariusAI.Web` and shares application services, Identity configuration,
dependency health checks and the existing data-protection key ring with MVC.
There is no separate API deployment, mobile project or financial endpoint yet.
No database schema, SQL port or financial workflow changes are introduced.

## Routes and authentication

| Method | Route | Authentication | Successful response |
| --- | --- | --- | --- |
| GET | `/api/v1/info` | API bearer policy | `{"apiVersion":"1","applicationVersion":"0.25.1-dev.45"}` |
| GET | `/api/v1/health/live` | Anonymous | `{"status":"ok"}` |
| GET | `/api/v1/health/ready` | Anonymous | `{"status":"ready"}` |

Readiness checks the registered dependencies, currently SQL Server. An unhealthy
or degraded dependency returns a generic 503 problem without dependency names,
connection strings or diagnostic messages. Liveness does not query dependencies.
The original anonymous `/health` endpoint remains unchanged for Docker probes.
The host still completes migrations and initialization before accepting requests.

The entire `/api` namespace is reserved, including unsupported versions and unknown
paths. Its fallback requires API authentication and returns a JSON 404 after
successful authentication. MVC login redirects are never used by API endpoints.

`ApiFoundation.AuthenticationScheme` identifies a dedicated ASP.NET Core opaque
bearer-token handler, with a ten-minute access-token lifetime. The named
`ApiFoundation.AuthorizationPolicy` accepts only that scheme. MVC cookies cannot
authorize API requests; API bearer tokens cannot authorize MVC pages. Existing
Identity role claims remain compatible with server-side role policies.

**No login, token issuance, refresh, logout or registration endpoints are exposed.**
Module 2 must implement identity validation, revocable sessions, refresh-credential
rotation/revocation, current account/role validation and Android secure storage
before users can obtain native client credentials. Registering the handler does
not expose Identity API endpoints. Do not call `MapIdentityApi` as a shortcut.
The scheme and named policy form the replacement boundary for a future OIDC
provider; these opaque tokens are not JWTs or an OAuth/OIDC server.

## Transport and limits

All `/api` routes, including probes, require HTTPS. Plain HTTP returns a 400
`https_required` problem instead of redirecting a request that might contain
credentials. Setting `HttpsRedirection:Enabled=false` for local MVC/Docker use
does not disable this API requirement. The existing Compose HTTP port can still
serve MVC and `/health`, but cannot serve a usable native API on its own.

Use a valid TLS certificate on the API host. If TLS terminates at a reverse proxy,
the host must receive a trusted HTTPS scheme through platform integration or
explicit forwarded-header configuration restricted to that proxy. This module
does not enable unrestricted forwarded headers: a caller-supplied
`X-Forwarded-Proto: https` is not trusted by the default configuration. Establish
and verify the trusted-proxy configuration for the target deployment before
connecting a native client. Never disable certificate validation or expose SQL
Server to Android or the public Internet.

API request bodies are limited to 64 KiB through Kestrel's per-request body limit
and an early Content-Length check. This applies to streamed/chunked bodies when
the server reads them as well. Future upload modules must deliberately define
their own bounded upload design. API responses use `Cache-Control: no-store`.

The API group permits 60 requests per minute per remote IP, with no queue. The
limit applies before authorization, including anonymous probes and invalid
credentials. Rejection returns 429 and `Retry-After: 60`. Requests behind an
unconfigured proxy share that proxy's quota. Future authentication endpoints need
their own abuse policy, and multi-instance deployment needs gateway/distributed
rate limiting; the current limiter is local to each host process.

## Contracts and errors

Use immutable, explicitly documented response/request DTOs, initially under
`DenariusAI.Web/Api/Contracts`, with camelCase JSON names. Do not return EF entities,
MVC view models, navigation properties, secrets or environment configuration.
When Android begins sharing contracts, move appropriate DTOs into an explicit
dependency-free contracts project; never reference Web or Infrastructure from
Android. Prefer additive changes within `/v1`; version breaking changes.

Attach subsequent minimal API endpoints to the group returned by
`MapDenariusApi` so that its bearer policy and rate limit are inherited. Add role
and object-level authorization where relevant. Do not create ungrouped financial
routes or rely on the MVC fallback policy. Resolve business operations through
Application/Domain services, pass request cancellation through asynchronous I/O,
and keep financial arithmetic and AI confirmation on the server.

Validate request DTOs explicitly before invoking services. Use
`ApiProblems.Validation` with field names and safe European Portuguese messages;
never echo attempted values or raw exception messages. Collection modules must
define a common pagination DTO and bounded page sizes when first introduced.
Required fields, constraints and pagination semantics belong in each endpoint's
contract documentation and HTTP integration tests.

Failures use `application/problem+json`, even when a client requests HTML:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "É necessário autenticar-se.",
  "status": 401,
  "code": "authentication_required",
  "traceId": "request-correlation-identifier"
}
```

`code` is the stable machine-readable discriminator; clients must not parse
translated titles. Validation additionally contains an `errors` object mapping
field names to message arrays. Status codes include 400, 401, 403, 404, 413, 429,
500 and 503. API exception logging records only a correlation identifier, not
exception payloads. No request-body or credential logging is introduced.

## Verification and next module

The HTTP tests use `WebApplicationFactory<Program>` in Production mode.
database initialization/persistence and data-protection persistence are replaced
with test implementations; OS-dependent logging providers are disabled in tests.
The production routing, middleware, authorization,
bearer and cookie handlers, MVC views and antiforgery filters execute normally.
Synthetic tickets are protected through the real configured token/cookie
protectors; no test authentication scheme bypasses validation. Test-only routes
exercise roles, exception handling and field errors and are absent from the
published application.

Run the Release solution build and tests, including `ApiFoundationTests`.
Container verification must build `final`, start an isolated test instance,
check `/health`, and confirm HTTP API rejection. HTTPS success paths are covered
by the HTTP integration suite and must also be verified on the chosen TLS host
when deployment is configured.

No Ajuda update is needed for Module 1: no end-user screen, action or financial
workflow changes. Module 2 introduces native authentication and session handling;
Android screens, financial data, writes and remaining modules are outside this
change set.
