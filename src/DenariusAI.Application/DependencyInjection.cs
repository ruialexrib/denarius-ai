using Microsoft.Extensions.DependencyInjection;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.Services;

namespace DenariusAI.Application;

/// <summary>
/// Provides extension methods for registering application services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds application layer services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection with registered application services.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IFinancialGroupService, FinancialGroupService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IJournalEntryService, JournalEntryService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IReconciliationService, ReconciliationService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IAssistantService, FinancialAssistantService>();
        services.AddScoped<IJournalEntrySuggestionService, JournalEntrySuggestionService>();
        return services;
    }
}
