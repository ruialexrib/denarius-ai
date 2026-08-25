using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Abstractions.Services;

/// <summary>
/// Service interface for managing application settings.
/// </summary>
public interface IApplicationSettingsService
{
    /// <summary>
    /// Retrieves the current application settings.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation, containing the application settings.</returns>
    Task<ApplicationSettingsDto> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the application settings.
    /// </summary>
    /// <param name="settings">The settings data to update.</param>
    /// <param name="userId">The identifier of the user performing the update.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task UpdateAsync(ApplicationSettingsDto settings, string userId, CancellationToken cancellationToken = default);
}
