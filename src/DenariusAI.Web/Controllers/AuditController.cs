using System.Text.Json;
using DenariusAI.Infrastructure.Identity;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Administrator)]
/// <summary>
/// Contains definitions for AuditController.
/// </summary>
public sealed class AuditController(DenariusDbContext dbContext) : Controller
{
    private static readonly IReadOnlyDictionary<string, string> EntityNames = new Dictionary<string, string>
    {
        ["Account"] = "Conta", ["ApplicationSetting"] = "Definição", ["ApplicationUser"] = "Utilizador",
        ["Budget"] = "Orçamento", ["BudgetLine"] = "Linha de orçamento", ["Category"] = "Categoria",
        ["FinancialGroup"] = "Grupo", ["JournalEntry"] = "Movimento", ["JournalEntryLine"] = "Linha de movimento",
        ["Reconciliation"] = "Reconciliação", ["Reminder"] = "Lembrete", ["ReminderAcknowledgement"] = "Confirmação de lembrete",
        ["SavingsCertificate"] = "Certificado de Aforro"
    };

    public async Task<IActionResult> Index(string? search, string? entityType, string? recordId, string? action, DateOnly? from, DateOnly? to,
        int page = 1, int pageSize = 25, CancellationToken cancellationToken = default)
    {
        var query = dbContext.AuditLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(item => item.RecordLabel!.Contains(search) || item.EntityId.Contains(search) || item.UserName!.Contains(search));
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(item => item.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(recordId)) query = query.Where(item => item.EntityId == recordId);
        if (action is "Created" or "Updated" or "Deleted") query = query.Where(item => item.Action == action); else action = null;
        if (from.HasValue) query = query.Where(item => item.ChangedAt >= new DateTimeOffset(from.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        if (to.HasValue) query = query.Where(item => item.ChangedAt < new DateTimeOffset(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        var total = await query.CountAsync(cancellationToken); var pagination = PaginationViewModel.Create(total, page, pageSize);
        var logs = await query.OrderByDescending(item => item.ChangedAt).Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize).ToListAsync(cancellationToken);
        var userIds = logs.Where(item => item.UserId != null).Select(item => item.UserId!).Distinct().ToList();
        var users = await dbContext.Users.AsNoTracking().Where(item => userIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id, item => item.DisplayName, cancellationToken);
        var rows = logs.Select(item => new AuditLogRowViewModel(item.Id, item.EntityType, EntityName(item.EntityType), item.EntityId,
            item.RecordLabel ?? item.EntityId, item.Action, ActionName(item.Action), item.ChangedAt,
            item.UserId != null && users.TryGetValue(item.UserId, out var displayName) ? displayName : item.UserName ?? item.UserId ?? "Sistema")).ToList();
        var types = await dbContext.AuditLogs.AsNoTracking().Select(item => item.EntityType).Distinct().OrderBy(item => item).ToListAsync(cancellationToken);
        return View(new AuditIndexViewModel(rows, search, entityType, recordId, action, from, to,
            [new SelectListItem("Todos os registos", ""), .. types.Select(type => new SelectListItem(EntityName(type), type, type == entityType))], pagination));
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.AuditLogs.AsNoTracking().SingleOrDefaultAsync(value => value.Id == id, cancellationToken); if (item is null) return NotFound();
        var actor = item.UserName ?? item.UserId ?? "Sistema";
        if (item.UserId is not null) actor = await dbContext.Users.AsNoTracking().Where(user => user.Id == item.UserId).Select(user => user.DisplayName).FirstOrDefaultAsync(cancellationToken) ?? actor;
        var oldValues = Parse(item.OldValues); var newValues = Parse(item.NewValues);
        var fields = item.Action == "Updated" ? ParseList(item.ChangedColumns) : oldValues.Keys.Union(newValues.Keys).OrderBy(value => value).ToList();
        var referencedUserIds = fields.Where(field => field.EndsWith("By", StringComparison.Ordinal))
            .SelectMany(field => new[] { StringValue(oldValues.GetValueOrDefault(field)), StringValue(newValues.GetValueOrDefault(field)) })
            .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToList();
        var referencedUsers = await dbContext.Users.AsNoTracking().Where(user => referencedUserIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.DisplayName, cancellationToken);
        var changes = fields.Where(field => !IsTechnical(field)).Select(field => new AuditChangeViewModel(field, FieldName(field),
            Format(item.EntityType, field, oldValues.GetValueOrDefault(field), referencedUsers),
            Format(item.EntityType, field, newValues.GetValueOrDefault(field), referencedUsers))).ToList();
        return View(new AuditDetailsViewModel(item.Id, EntityName(item.EntityType), item.EntityId, item.RecordLabel ?? item.EntityId, ActionName(item.Action), item.ChangedAt, actor, changes));
    }

    public IActionResult Record(string entityType, string entityId) =>
        RedirectToAction(nameof(Index), new { entityType, recordId = entityId });

    private static Dictionary<string, JsonElement> Parse(string? json) => string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? [];
    private static List<string> ParseList(string? json) => string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<List<string>>(json) ?? [];
    private static string? Format(string entityType, string field, JsonElement value, IReadOnlyDictionary<string, string> users)
    {
        if (value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) return null;
        var raw = StringValue(value);
        if (field.EndsWith("By", StringComparison.Ordinal) && raw is not null) return users.GetValueOrDefault(raw, "Utilizador removido");
        if (field == "Status" && entityType == "JournalEntry") return raw switch { "1" => "Ativo", "2" => "Anulado", _ => raw };
        if (field == "ReconciliationStatus") return raw switch { "1" => "Não reconciliado", "2" => "Reconciliado", _ => raw };
        if ((field.EndsWith("At", StringComparison.Ordinal) || field is "Date" or "EventDate") &&
            DateTimeOffset.TryParse(raw, out var dateTime)) return dateTime.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
        return value.ValueKind switch { JsonValueKind.True => "Sim", JsonValueKind.False => "Não", _ => raw };
    }

    private static string? StringValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Undefined or JsonValueKind.Null => null,
        JsonValueKind.String => value.GetString(),
        _ => value.ToString()
    };
    private static bool IsTechnical(string field) => field is "Id" or "CreatedAt" or "CreatedBy" or "UpdatedAt" or "UpdatedBy";
    private static string EntityName(string type) => EntityNames.GetValueOrDefault(type, type);
    private static string ActionName(string action) => action switch { "Created" => "Criação", "Updated" => "Alteração", "Deleted" => "Eliminação", _ => action };
    private static string FieldName(string field) => field switch
    {
        "Name" => "Nome", "Description" => "Descrição", "Text" => "Texto", "Date" => "Data", "Amount" => "Valor",
        "Value" => "Valor protegido", "PasswordHash" => "Palavra-passe protegida", "IsActive" => "Estado",
        "Status" => "Estado", "ReconciliationStatus" => "Estado da reconciliação", "DisplayName" => "Nome apresentado",
        "Email" => "Email", "EventDate" => "Data do evento", "NoticeDays" => "Dias de aviso",
        "CancelledAt" => "Data da anulação", "CancelledBy" => "Anulado por", "ReconciledAt" => "Data da reconciliação",
        "ReconciledBy" => "Reconciliado por", "AccountType" => "Tipo de conta", "InitialBalance" => "Saldo inicial",
        "Balance" => "Saldo", "Currency" => "Moeda", "Reference" => "Referência", "Notes" => "Notas",
        "Year" => "Ano", "Month" => "Mês", "Rate" => "Taxa", "InvestmentValue" => "Valor investido",
        _ => field
    };
}
