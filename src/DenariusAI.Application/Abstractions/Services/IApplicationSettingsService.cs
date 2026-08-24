using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Abstractions.Services;

public interface IApplicationSettingsService
{
    Task<ApplicationSettingsDto> GetAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(ApplicationSettingsDto settings, string userId, CancellationToken cancellationToken = default);
}
