using System.Text.Json;

namespace DenariusAI.Application.DTOs;

public sealed record ApplicationBackupDto(
    string Format,
    int SchemaVersion,
    DateTimeOffset CreatedAt,
    string ApplicationVersion,
    Dictionary<string, List<Dictionary<string, JsonElement>>> Tables);

public sealed record ApplicationRestoreResult(int Tables, int Records);
