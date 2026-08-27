using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Abstractions.Services;

public interface IApplicationBackupService
{
    Task<byte[]> ExportAsync(string applicationVersion, CancellationToken cancellationToken = default);
    Task<ApplicationRestoreResult> RestoreAsync(Stream json, CancellationToken cancellationToken = default);
}
