namespace DenariusAI.Web.Api.Contracts;

/// <summary>Reports the protocol and server versions without exposing financial or environment data.</summary>
/// <param name="ApiVersion">The supported API major version.</param>
/// <param name="ApplicationVersion">The complete server version, including any prerelease suffix.</param>
public sealed record ApiInfoResponse(string ApiVersion, string ApplicationVersion);
