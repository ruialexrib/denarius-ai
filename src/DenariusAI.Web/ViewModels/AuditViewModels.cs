using Microsoft.AspNetCore.Mvc.Rendering;

namespace DenariusAI.Web.ViewModels;

/// <summary>
/// Represents the AuditViewModels type.
/// </summary>
public sealed record AuditLinkViewModel(string EntityType, string EntityId);

/// <summary>
/// Represents the AuditLogRowViewModel type.
/// </summary>
public sealed record AuditLogRowViewModel(Guid Id, string EntityType, string EntityName, string EntityId, string RecordLabel,
    string Action, string ActionName, DateTimeOffset ChangedAt, string Actor);

/// <summary>
/// Represents the AuditIndexViewModel type.
/// </summary>
public sealed record AuditIndexViewModel(IReadOnlyList<AuditLogRowViewModel> Items, string? Search, string? EntityType, string? RecordId,
    string? Action, DateOnly? From, DateOnly? To, IReadOnlyList<SelectListItem> EntityTypes,
    PaginationViewModel Pagination);

/// <summary>
/// Represents the AuditChangeViewModel type.
/// </summary>
public sealed record AuditChangeViewModel(string Field, string FieldName, string? Before, string? After);

/// <summary>
/// Represents the AuditDetailsViewModel type.
/// </summary>
public sealed record AuditDetailsViewModel(Guid Id, string EntityName, string EntityId, string RecordLabel,
    string ActionName, DateTimeOffset ChangedAt, string Actor, IReadOnlyList<AuditChangeViewModel> Changes);
