using DenariusAI.Application.DTOs;

namespace DenariusAI.Web.ViewModels;

/// <summary>
/// Represents the ClassificationStatementViewModel type.
/// </summary>
public sealed record ClassificationStatementViewModel(
    string EntityType,
    Guid Id,
    string Name,
    DenariusAI.Domain.Enums.FinancialGroupKind Kind,
    decimal CurrentBalance,
    IReadOnlyList<ClassificationStatementLineDto> Items,
    DateOnly? From,
    DateOnly? To,
    string? Search,
    PaginationViewModel Pagination);
