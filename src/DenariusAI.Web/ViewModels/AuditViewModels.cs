using Microsoft.AspNetCore.Mvc.Rendering;

namespace DenariusAI.Web.ViewModels;

/// <summary>
/// Contains definitions for AuditViewModels.
/// </summary>
public sealed record AuditLinkViewModel(string EntityType, string EntityId);

public sealed record AuditLogRowViewModel(Guid Id, string EntityType, string EntityName, string EntityId, string RecordLabel,
    string Action, string ActionName, DateTimeOffset ChangedAt, string Actor);

public sealed record AuditIndexViewModel(IReadOnlyList<AuditLogRowViewModel> Items, string? Search, string? EntityType, string? RecordId,
    string? Action, DateOnly? From, DateOnly? To, IReadOnlyList<SelectListItem> EntityTypes,
    PaginationViewModel Pagination);

public sealed record AuditChangeViewModel(string Field, string FieldName, string? Before, string? After);

public sealed record AuditDetailsViewModel(Guid Id, string EntityName, string EntityId, string RecordLabel,
    string ActionName, DateTimeOffset ChangedAt, string Actor, IReadOnlyList<AuditChangeViewModel> Changes);
