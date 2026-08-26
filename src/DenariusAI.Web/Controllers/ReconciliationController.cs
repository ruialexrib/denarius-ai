using System.Security.Claims;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Domain.Enums;
using DenariusAI.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using DenariusAI.Infrastructure.Persistence;
using DenariusAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;

namespace DenariusAI.Web.Controllers;

/// <summary>
/// Represents the ReconciliationController type.
/// </summary>
[Authorize]
public sealed class ReconciliationController(IReconciliationService service, IAccountService accountService, ILogger<ReconciliationController> logger, DenariusDbContext dbContext, ILLMService llmService, IApplicationSettingsService settingsService) : Controller
{
    private const string ImportSessionKey = "Reconciliation.ConversationImport";
    private static readonly AccountType[] BankingAccountTypes = [AccountType.BankAccount, AccountType.Savings, AccountType.TermDeposit];

    [HttpGet]
    public async Task<IActionResult> Index(Guid? accountId, DateOnly? from, DateOnly? to, ReconciliationStatus? status, string? search, string sort = "dateDesc", int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DenariusAI.Application.DTOs.ReconciliationItemDto> items;
        try { items = await service.ListAsync(accountId, from, to, status, search, cancellationToken); }
        catch (ArgumentException exception) { ModelState.AddModelError(string.Empty, exception.Message); items = []; }
        var unreconciledCount = items.Count(item => item.Status == ReconciliationStatus.Unreconciled);
        var reconciledCount = items.Count - unreconciledCount;
        items = sort switch
        {
            "dateAsc" => items.OrderBy(item => item.Date).ThenBy(item => item.Description).ToList(),
            "description" => items.OrderBy(item => item.Description).ThenByDescending(item => item.Date).ToList(),
            "amountDesc" => items.OrderByDescending(item => Math.Max(item.Debit, item.Credit)).ThenByDescending(item => item.Date).ToList(),
            "status" => items.OrderBy(item => item.Status).ThenByDescending(item => item.Date).ToList(),
            _ => items.OrderByDescending(item => item.Date).ThenBy(item => item.Description).ToList()
        };
        var pagination = PaginationViewModel.Create(items.Count, page, pageSize);
        items = items.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize).ToList();
        var reconcilerIds = items.Select(item => item.ReconciledBy).Where(id => !string.IsNullOrWhiteSpace(id) && id != "demo-seed").Distinct().ToList();
        var reconciledByNames = await dbContext.Users.AsNoTracking().Where(user => reconcilerIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName ?? "Utilizador" : user.DisplayName, cancellationToken);
        if (items.Any(item => item.ReconciledBy == "demo-seed")) reconciledByNames["demo-seed"] = "Dados de demonstração";
        return View(new ReconciliationIndexViewModel(items, accountId, from, to, status, search, sort,
            await AccountItemsAsync(accountId, cancellationToken), StatusItems(status), SortItems(sort), unreconciledCount, reconciledCount, reconciledByNames, pagination));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reconcile(Guid id, Guid? accountId, DateOnly? from, DateOnly? to, ReconciliationStatus? status, string? search, string sort = "dateDesc", int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            await service.ReconcileAsync(id, UserId(), cancellationToken);
            logger.LogInformation("Journal entry {JournalEntryId} reconciled by user {UserId}.", id, UserId());
            TempData["SuccessMessage"] = "Movimento reconciliado com sucesso.";
        }
        catch (KeyNotFoundException exception) { TempData["ErrorMessage"] = exception.Message; }
        catch (InvalidOperationException exception) { TempData["ErrorMessage"] = exception.Message; }
        return RedirectToIndex(accountId, from, to, status, search, sort, page, pageSize);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Undo(Guid id, Guid? accountId, DateOnly? from, DateOnly? to, ReconciliationStatus? status, string? search, string sort = "dateDesc", int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            await service.UndoAsync(id, UserId(), cancellationToken);
            logger.LogInformation("Reconciliation for journal entry {JournalEntryId} undone by user {UserId}.", id, UserId());
            TempData["SuccessMessage"] = "Reconciliação desfeita.";
        }
        catch (KeyNotFoundException exception) { TempData["ErrorMessage"] = exception.Message; }
        catch (InvalidOperationException exception) { TempData["ErrorMessage"] = exception.Message; }
        return RedirectToIndex(accountId, from, to, status, search, sort, page, pageSize);
    }

    [HttpGet]
    public async Task<IActionResult> Import(CancellationToken cancellationToken) => View(new ReconciliationPasteViewModel { BankAccounts = await BankAccountItemsAsync(cancellationToken) });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AnalyzeConversation(ReconciliationPasteViewModel model, CancellationToken cancellationToken)
    {
        model.BankAccounts = await BankAccountItemsAsync(cancellationToken);
        var bank = await dbContext.Accounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.BankAccountId && BankingAccountTypes.Contains(x.AccountType), cancellationToken);
        if (bank is null) ModelState.AddModelError(nameof(model.BankAccountId), "Selecione uma conta bancária válida.");
        if (string.IsNullOrWhiteSpace(model.MovementsText) || model.MovementsText.Trim().Length < 5) ModelState.AddModelError(nameof(model.MovementsText), "Cole os movimentos que pretende analisar.");
        if (model.MovementsText?.Length > 50000) ModelState.AddModelError(nameof(model.MovementsText), "O texto não pode exceder 50 000 caracteres.");
        if (!llmService.IsConfigured) ModelState.AddModelError(string.Empty, "Configure a integração Mistral antes de analisar os movimentos.");
        if (!ModelState.IsValid) return View("Import", model);

        ConversationExtraction parsed;
        try
        {
            var settings = await settingsService.GetAsync(cancellationToken);
            var prompt = settings.ReconciliationExtractionPrompt;
            var completion = await llmService.CompleteAsync([new("system", prompt), new("user", model.MovementsText!.Trim())], cancellationToken);
            parsed = JsonSerializer.Deserialize<ConversationExtraction>(StripJsonFence(completion.Content), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch (Exception exception) when (exception is JsonException or HttpRequestException or InvalidOperationException)
        {
            logger.LogWarning(exception, "Could not extract pasted reconciliation movements.");
            model.AssistantMessage = "Não consegui interpretar o texto. Inclua uma linha por movimento com data, descrição e valor.";
            return View("Import", model);
        }

        if (!string.Equals(parsed.Status, "complete", StringComparison.OrdinalIgnoreCase) || parsed.Movements.Count == 0)
        {
            model.AssistantMessage = string.IsNullOrWhiteSpace(parsed.Message) ? "Preciso de data, descrição e valor para cada movimento. Pode completar esses dados?" : parsed.Message;
            return View("Import", model);
        }

        var imported = parsed.Movements.Take(200).Where(x => x.Date != default && !string.IsNullOrWhiteSpace(x.Description) && x.Amount != 0)
            .Select((x, index) => new ReconciliationImportRowViewModel { RowNumber = index + 1, Date = x.Date, Description = x.Description.Trim(), Reference = x.Reference?.Trim(), Amount = x.Amount }).ToList();
        if (imported.Count == 0) { model.AssistantMessage = "Não encontrei movimentos completos. Confirme datas, descrições e valores."; return View("Import", model); }

        var minDate = imported.Min(x => x.Date); var maxDate = imported.Max(x => x.Date);
        var existing = await dbContext.JournalEntries.AsNoTracking().Include(x => x.Lines).Where(x => x.Date >= minDate && x.Date <= maxDate).ToListAsync(cancellationToken);
        imported = imported.Where(row => !existing.Any(entry => EntryMatches(entry, row))).ToList();
        await ApplySuggestionsAsync(imported, cancellationToken);
        var review = new ReconciliationImportReviewViewModel { BankAccountId = bank!.Id, BankAccountName = bank.Name, Rows = imported };
        HttpContext.Session.SetString(ImportSessionKey, JsonSerializer.Serialize(review));
        return RedirectToAction(nameof(ReviewImport));
    }
    [HttpGet]
    public async Task<IActionResult> ReviewImport(CancellationToken cancellationToken)
    {
        var json = HttpContext.Session.GetString(ImportSessionKey); if (string.IsNullOrWhiteSpace(json)) return RedirectToAction(nameof(Import));
        var model = JsonSerializer.Deserialize<ReconciliationImportReviewViewModel>(json) ?? new(); await PopulateReviewOptionsAsync(model, cancellationToken); return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmImport(ReconciliationImportReviewViewModel model, CancellationToken cancellationToken)
    {
        var bank = await dbContext.Accounts.FindAsync([model.BankAccountId], cancellationToken); if (bank is null) return BadRequest();
        var selected = model.Rows.Where(x => x.Selected).ToList();
        if (selected.Any(x => !x.CounterAccountId.HasValue || !x.CategoryId.HasValue)) { ModelState.AddModelError(string.Empty, "Classifique a conta e a categoria de todas as linhas selecionadas."); await PopulateReviewOptionsAsync(model, cancellationToken); return View("ReviewImport", model); }
        foreach (var row in selected)
        {
            var entry = new JournalEntry(row.Date, row.Description, row.Reference, "Criado através da conversa de reconciliação") { CreatedBy = UserId() };
            if (row.Amount >= 0) { entry.AddLine(bank.Id, row.Amount, 0m); entry.AddLine(row.CounterAccountId!.Value, 0m, row.Amount, categoryId: row.CategoryId); }
            else { var amount = Math.Abs(row.Amount); entry.AddLine(row.CounterAccountId!.Value, amount, 0m, categoryId: row.CategoryId); entry.AddLine(bank.Id, 0m, amount); }
            entry.EnsureBalanced(); dbContext.JournalEntries.Add(entry);
        }
        await dbContext.SaveChangesAsync(cancellationToken); HttpContext.Session.Remove(ImportSessionKey); TempData["SuccessMessage"] = $"{selected.Count} movimentos criados e preparados para reconciliação."; return RedirectToAction(nameof(Index), new { accountId = bank.Id });
    }

    private async Task ApplySuggestionsAsync(List<ReconciliationImportRowViewModel> rows, CancellationToken cancellationToken)
    {
        if (rows.Count == 0) return; var categories = await dbContext.Categories.AsNoTracking().Where(x => x.IsActive).Select(x => new { x.Id, x.Name, Kind = x.FinancialGroup.Kind }).ToListAsync(cancellationToken); var accounts = await dbContext.Accounts.AsNoTracking().Where(x => x.IsActive).Select(x => new { x.Id, x.Name, x.AccountType }).ToListAsync(cancellationToken);
        foreach (var row in rows) { var category = categories.FirstOrDefault(x => row.Description.Contains(x.Name, StringComparison.OrdinalIgnoreCase)); if (category is not null) { row.CategoryId = category.Id; row.CounterAccountId = accounts.FirstOrDefault(x => category.Kind == FinancialGroupKind.Expense ? x.AccountType == AccountType.Expense : x.AccountType == AccountType.Income)?.Id; row.SuggestionReason = "Correspondência pelo nome da categoria."; } }
        if (!llmService.IsConfigured) return;
        var recentExamples = await dbContext.JournalEntries.AsNoTracking()
            .Where(x => x.Status == JournalEntryStatus.Active).OrderByDescending(x => x.Date).ThenByDescending(x => x.CreatedAt).Take(50)
            .Select(x => new { x.Date, x.Description, x.Reference, lines = x.Lines.Select(line => new { line.AccountId, Account = line.Account.Name, line.CategoryId, Category = line.Category != null ? line.Category.Name : null, line.Debit, line.Credit }) })
            .ToListAsync(cancellationToken);
        var groups = await dbContext.FinancialGroups.AsNoTracking().Where(x => x.IsActive).Select(x => new { x.Id, x.Name, x.Kind }).ToListAsync(cancellationToken);
        var payload = new { rows = rows.Select(x => new { x.RowNumber, x.Description, x.Amount }), groups, categories = categories.Select(x => new { x.Id, x.Name, x.Kind }), accounts, recentExamples };
        try { var completion = await llmService.CompleteAsync([new("system", (await settingsService.GetAsync(cancellationToken)).ReconciliationClassificationPrompt), new("user", JsonSerializer.Serialize(payload))], cancellationToken); var json = completion.Content.Replace("```json", "").Replace("```", "").Trim(); var suggestions = JsonSerializer.Deserialize<List<ImportSuggestion>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? []; foreach (var suggestion in suggestions) { var row = rows.FirstOrDefault(x => x.RowNumber == suggestion.RowNumber); if (row is not null && categories.Any(x => x.Id == suggestion.CategoryId) && accounts.Any(x => x.Id == suggestion.CounterAccountId)) { row.CategoryId = suggestion.CategoryId; row.CounterAccountId = suggestion.CounterAccountId; row.SuggestionReason = suggestion.Reason; } } } catch (Exception exception) when (exception is JsonException or HttpRequestException or InvalidOperationException) { logger.LogWarning(exception, "AI import classification failed; manual review remains available."); }
    }

    private async Task PopulateReviewOptionsAsync(ReconciliationImportReviewViewModel model, CancellationToken cancellationToken)
    {
        model.Categories = await dbContext.Categories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.FinancialGroup.SortOrder).ThenBy(x => x.Name).Select(x => new SelectListItem(x.FinancialGroup.Name + " · " + x.Name, x.Id.ToString())).ToListAsync(cancellationToken);
        model.CounterAccounts = await dbContext.Accounts.AsNoTracking().Where(x => x.IsActive && (x.AccountType == AccountType.Income || x.AccountType == AccountType.Expense)).OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<SelectListItem>> BankAccountItemsAsync(CancellationToken cancellationToken) => (await accountService.ListAsync(cancellationToken: cancellationToken)).Where(x => BankingAccountTypes.Contains(x.AccountType)).Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();
    private static bool EntryMatches(JournalEntry entry, ReconciliationImportRowViewModel row) => entry.Date == row.Date && entry.Lines.Any(x => Math.Max(x.Debit, x.Credit) == Math.Abs(row.Amount)) && (!string.IsNullOrWhiteSpace(row.Reference) ? string.Equals(entry.Reference, row.Reference, StringComparison.OrdinalIgnoreCase) : Normalize(entry.Description) == Normalize(row.Description));
    private static string Normalize(string value) => new(value.Normalize(NormalizationForm.FormD).Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c)).Select(char.ToLowerInvariant).ToArray());
    private static string StripJsonFence(string content)
    {
        var json = content.Trim(); var fence = new string((char)96, 3);
        if (!json.StartsWith(fence, StringComparison.Ordinal)) return json;
        var firstLine = json.IndexOf('\n'); var lastFence = json.LastIndexOf(fence, StringComparison.Ordinal);
        return firstLine >= 0 && lastFence > firstLine ? json[(firstLine + 1)..lastFence].Trim() : json;
    }
    /// <summary>
    /// Represents the ConversationExtraction type.
    /// </summary>
    private sealed class ConversationExtraction { public string? Status { get; set; } public string? Message { get; set; } public List<ConversationMovement> Movements { get; set; } = []; }
    /// <summary>
    /// Represents the ConversationMovement type.
    /// </summary>
    private sealed class ConversationMovement { public DateOnly Date { get; set; } public string Description { get; set; } = string.Empty; public string? Reference { get; set; } public decimal Amount { get; set; } }
    /// <summary>
    /// Represents the ImportSuggestion type.
    /// </summary>
    private sealed record ImportSuggestion(int RowNumber, Guid CategoryId, Guid CounterAccountId, string Reason);

    private IActionResult RedirectToIndex(Guid? accountId, DateOnly? from, DateOnly? to, ReconciliationStatus? status, string? search, string sort, int page, int pageSize) =>
        RedirectToAction(nameof(Index), new { accountId, from, to, status, search, sort, page, pageSize });

    private async Task<IReadOnlyList<SelectListItem>> AccountItemsAsync(Guid? selected, CancellationToken cancellationToken)
    {
        var accounts = await accountService.ListAsync(cancellationToken: cancellationToken);
        return accounts.Where(item => BankingAccountTypes.Contains(item.AccountType)).OrderBy(item => item.Name)
            .Select(item => new SelectListItem($"{item.Name} · {item.Currency}{(item.IsActive ? string.Empty : " · Inativa")}", item.Id.ToString(), item.Id == selected))
            .Prepend(new SelectListItem("Todas as contas bancárias", string.Empty, selected is null)).ToList();
    }

    private static IReadOnlyList<SelectListItem> StatusItems(ReconciliationStatus? selected) =>
    [
        new("Todos os estados", string.Empty, selected is null),
        new("Não reconciliado", ((int)ReconciliationStatus.Unreconciled).ToString(), selected == ReconciliationStatus.Unreconciled),
        new("Reconciliado", ((int)ReconciliationStatus.Reconciled).ToString(), selected == ReconciliationStatus.Reconciled)
    ];

    private static IReadOnlyList<SelectListItem> SortItems(string selected) =>
    [
        new("Data mais recente", "dateDesc", selected == "dateDesc"), new("Data mais antiga", "dateAsc", selected == "dateAsc"),
        new("Descrição", "description", selected == "description"), new("Maior valor", "amountDesc", selected == "amountDesc"), new("Estado", "status", selected == "status")
    ];

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Utilizador não identificado.");
}
