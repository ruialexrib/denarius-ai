namespace DenariusAI.Web.Api.Contracts;

/// <summary>Reports aggregate availability without disclosing dependency details.</summary>
/// <param name="Status">The stable availability state.</param>
public sealed record ApiHealthResponse(string Status);
