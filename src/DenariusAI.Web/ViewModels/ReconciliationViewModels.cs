using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DenariusAI.Web.ViewModels;

public sealed record ReconciliationIndexViewModel(
    IReadOnlyList<ReconciliationItemDto> Items,
    Guid? AccountId,
    DateOnly? From,
    DateOnly? To,
    ReconciliationStatus? Status,
    string? Search,
    string Sort,
    IReadOnlyList<SelectListItem> Accounts,
    IReadOnlyList<SelectListItem> Statuses,
    IReadOnlyList<SelectListItem> SortOptions,
    int UnreconciledCount,
    int ReconciledCount,
    PaginationViewModel Pagination);
