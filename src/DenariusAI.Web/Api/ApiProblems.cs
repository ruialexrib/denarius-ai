using System.Diagnostics;

namespace DenariusAI.Web.Api;

/// <summary>Creates stable, non-sensitive problem responses for the versioned API.</summary>
public static class ApiProblems
{
    /// <summary>Writes a JSON problem regardless of the client's Accept header.</summary>
    /// <param name="context">The current request context.</param>
    /// <param name="status">The HTTP error status.</param>
    /// <param name="code">An optional stable error code overriding the status default.</param>
    /// <returns>A task representing response completion.</returns>
    public static Task WriteAsync(HttpContext context, int status, string? code = null)
    {
        var (defaultCode, title) = status switch
        {
            400 => ("invalid_request", "O pedido é inválido."),
            401 => ("authentication_required", "É necessário autenticar-se."),
            403 => ("access_denied", "Não tem permissão para aceder a este recurso."),
            404 => ("not_found", "O recurso pedido não foi encontrado."),
            405 => ("method_not_allowed", "O método não é permitido neste recurso."),
            413 => ("payload_too_large", "O pedido excede o tamanho permitido."),
            415 => ("unsupported_media_type", "O formato do pedido não é suportado."),
            429 => ("rate_limit_exceeded", "Foram enviados demasiados pedidos. Tente novamente mais tarde."),
            503 => ("service_unavailable", "O serviço está temporariamente indisponível."),
            _ => ("server_error", "Não foi possível concluir o pedido.")
        };
        return Results.Problem(statusCode: status,
            title: code == "https_required" ? "É necessária uma ligação HTTPS." : title,
            extensions: Extensions(context, code ?? defaultCode)).ExecuteAsync(context);
    }

    /// <summary>Creates field validation errors without echoing submitted values.</summary>
    /// <param name="context">The current request context.</param>
    /// <param name="errors">Field names mapped to safe Portuguese validation messages.</param>
    /// <returns>A validation problem with stable error and correlation identifiers.</returns>
    public static IResult Validation(HttpContext context, IDictionary<string, string[]> errors) =>
        Results.ValidationProblem(errors, title: "Verifique os dados introduzidos.",
            extensions: Extensions(context, "validation_failed"));

    /// <summary>Builds safe correlation metadata shared by all API problems.</summary>
    /// <param name="context">The current request context.</param>
    /// <param name="code">The stable machine-readable error code.</param>
    /// <returns>The problem extension fields.</returns>
    private static Dictionary<string, object?> Extensions(HttpContext context, string code) => new()
    {
        ["code"] = code,
        ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier
    };
}
