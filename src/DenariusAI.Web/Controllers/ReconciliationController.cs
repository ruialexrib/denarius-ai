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
using ClosedXML.Excel;
using System.Text.Json;
using System.Text;

namespace DenariusAI.Web.Controllers;

[Authorize]
public sealed class ReconciliationController(IReconciliationService service, IAccountService accountService, ILogger<ReconciliationController> logger, DenariusDbContext dbContext, ILLMService llmService) : Controller
{
    private const string ImportSessionKey = "Reconciliation.ExcelImport";
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
        return View(new ReconciliationIndexViewModel(items, accountId, from, to, status, search, sort,
            await AccountItemsAsync(accountId, cancellationToken), StatusItems(status), SortItems(sort), unreconciledCount, reconciledCount, pagination));
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
    public async Task<IActionResult> Import(CancellationToken cancellationToken) => View(new ReconciliationImportReviewViewModel { CounterAccounts = await BankAccountItemsAsync(cancellationToken) });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportExcel(IFormFile file, Guid bankAccountId, CancellationToken cancellationToken)
    {
        var bank = await dbContext.Accounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == bankAccountId && BankingAccountTypes.Contains(x.AccountType), cancellationToken);
        if (bank is null) { TempData["ErrorMessage"] = "Selecione uma conta bancária válida."; return RedirectToAction(nameof(Import)); }
        if (file is null || file.Length == 0 || !Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase)) { TempData["ErrorMessage"] = "Selecione um ficheiro Excel no formato .xlsx."; return RedirectToAction(nameof(Import)); }
        List<ReconciliationImportRowViewModel> imported;
        try { await using var stream = file.OpenReadStream(); imported = ReadWorkbook(stream); }
        catch (Exception exception) when (exception is InvalidDataException or FormatException) { TempData["ErrorMessage"] = exception.Message; return RedirectToAction(nameof(Import)); }
        var minDate = imported.Select(x => x.Date).DefaultIfEmpty(DateOnly.FromDateTime(DateTime.Today)).Min(); var maxDate = imported.Select(x => x.Date).DefaultIfEmpty(minDate).Max();
        var existing = await dbContext.JournalEntries.AsNoTracking().Include(x => x.Lines).Where(x => x.Date >= minDate && x.Date <= maxDate).ToListAsync(cancellationToken);
        imported = imported.Where(row => !existing.Any(entry => EntryMatches(entry, row))).ToList();
        await ApplySuggestionsAsync(imported, cancellationToken);
        var review = new ReconciliationImportReviewViewModel { BankAccountId = bank.Id, BankAccountName = bank.Name, Rows = imported };
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
            var entry = new JournalEntry(row.Date, row.Description, row.Reference, "Importado de extrato Excel") { CreatedBy = UserId() };
            if (row.Amount >= 0) { entry.AddLine(bank.Id, row.Amount, 0m); entry.AddLine(row.CounterAccountId!.Value, 0m, row.Amount, categoryId: row.CategoryId); }
            else { var amount = Math.Abs(row.Amount); entry.AddLine(row.CounterAccountId!.Value, amount, 0m, categoryId: row.CategoryId); entry.AddLine(bank.Id, 0m, amount); }
            entry.EnsureBalanced(); dbContext.JournalEntries.Add(entry);
        }
        await dbContext.SaveChangesAsync(cancellationToken); HttpContext.Session.Remove(ImportSessionKey); TempData["SuccessMessage"] = $"{selected.Count} movimentos importados e preparados para reconciliação."; return RedirectToAction(nameof(Index), new { accountId = bank.Id });
    }

    private static List<ReconciliationImportRowViewModel> ReadWorkbook(Stream stream)
    {
        using var workbook = new XLWorkbook(stream); var sheet = workbook.Worksheets.First(); var range = sheet.RangeUsed() ?? throw new InvalidDataException("O ficheiro não contém dados.");
        var headers = range.FirstRow().Cells().ToDictionary(cell => Normalize(cell.GetString()), cell => cell.Address.ColumnNumber);
        int Column(params string[] names) => names.Select(Normalize).Where(headers.ContainsKey).Select(name => headers[name]).FirstOrDefault();
        var dateColumn = Column("data", "date"); var descriptionColumn = Column("descrição", "descricao", "movimento"); var referenceColumn = Column("referência", "referencia", "ref"); var amountColumn = Column("valor", "montante", "amount"); var debitColumn = Column("débito", "debito", "saída", "saida"); var creditColumn = Column("crédito", "credito", "entrada");
        if (dateColumn == 0 || descriptionColumn == 0 || (amountColumn == 0 && debitColumn == 0 && creditColumn == 0)) throw new InvalidDataException("Não foram encontrados os cabeçalhos Data, Descrição e Valor/Débito/Crédito.");
        var rows = new List<ReconciliationImportRowViewModel>();
        foreach (var row in range.RowsUsed().Skip(1))
        {
            if (row.Cell(dateColumn).IsEmpty()) continue; DateOnly date;
            if (row.Cell(dateColumn).TryGetValue<DateTime>(out var dateTime)) date = DateOnly.FromDateTime(dateTime); else if (!DateOnly.TryParse(row.Cell(dateColumn).GetString(), out date)) continue;
            decimal amount; if (amountColumn > 0) amount = DecimalValue(row.Cell(amountColumn)); else amount = DecimalValue(row.Cell(creditColumn)) - DecimalValue(row.Cell(debitColumn));
            if (amount == 0) continue; rows.Add(new() { RowNumber = row.RowNumber(), Date = date, Description = row.Cell(descriptionColumn).GetString().Trim(), Reference = referenceColumn > 0 ? row.Cell(referenceColumn).GetString().Trim() : null, Amount = amount });
        }
        return rows;
    }

    private async Task ApplySuggestionsAsync(List<ReconciliationImportRowViewModel> rows, CancellationToken cancellationToken)
    {
        if (rows.Count == 0) return; var categories = await dbContext.Categories.AsNoTracking().Where(x => x.IsActive).Select(x => new { x.Id, x.Name, Kind = x.FinancialGroup.Kind }).ToListAsync(cancellationToken); var accounts = await dbContext.Accounts.AsNoTracking().Where(x => x.IsActive).Select(x => new { x.Id, x.Name, x.AccountType }).ToListAsync(cancellationToken);
        foreach (var row in rows) { var category = categories.FirstOrDefault(x => row.Description.Contains(x.Name, StringComparison.OrdinalIgnoreCase)); if (category is not null) { row.CategoryId = category.Id; row.CounterAccountId = accounts.FirstOrDefault(x => category.Kind == FinancialGroupKind.Expense ? x.AccountType == AccountType.Expense : x.AccountType == AccountType.Income)?.Id; row.SuggestionReason = "Correspondência pelo nome da categoria."; } }
        if (!llmService.IsConfigured) return;
        var payload = new { rows = rows.Select(x => new { x.RowNumber, x.Description, x.Amount }), categories = categories.Select(x => new { x.Id, x.Name, x.Kind }), accounts };
        try { var completion = await llmService.CompleteAsync([new("system", "Classifica movimentos bancários. Responde só com JSON array: [{rowNumber,categoryId,counterAccountId,reason}]. Usa apenas IDs fornecidos; omite sugestões incertas."), new("user", JsonSerializer.Serialize(payload))], cancellationToken); var json = completion.Content.Replace("```json", "").Replace("```", "").Trim(); var suggestions = JsonSerializer.Deserialize<List<ImportSuggestion>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? []; foreach (var suggestion in suggestions) { var row = rows.FirstOrDefault(x => x.RowNumber == suggestion.RowNumber); if (row is not null && categories.Any(x => x.Id == suggestion.CategoryId) && accounts.Any(x => x.Id == suggestion.CounterAccountId)) { row.CategoryId = suggestion.CategoryId; row.CounterAccountId = suggestion.CounterAccountId; row.SuggestionReason = suggestion.Reason; } } } catch (Exception exception) when (exception is JsonException or HttpRequestException or InvalidOperationException) { logger.LogWarning(exception, "AI import classification failed; manual review remains available."); }
    }

    private async Task PopulateReviewOptionsAsync(ReconciliationImportReviewViewModel model, CancellationToken cancellationToken)
    {
        model.Categories = await dbContext.Categories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.FinancialGroup.SortOrder).ThenBy(x => x.Name).Select(x => new SelectListItem(x.FinancialGroup.Name + " · " + x.Name, x.Id.ToString())).ToListAsync(cancellationToken);
        model.CounterAccounts = await dbContext.Accounts.AsNoTracking().Where(x => x.IsActive && (x.AccountType == AccountType.Income || x.AccountType == AccountType.Expense)).OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<SelectListItem>> BankAccountItemsAsync(CancellationToken cancellationToken) => (await accountService.ListAsync(cancellationToken: cancellationToken)).Where(x => BankingAccountTypes.Contains(x.AccountType)).Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();
    private static bool EntryMatches(JournalEntry entry, ReconciliationImportRowViewModel row) => entry.Date == row.Date && entry.Lines.Any(x => Math.Max(x.Debit, x.Credit) == Math.Abs(row.Amount)) && (!string.IsNullOrWhiteSpace(row.Reference) ? string.Equals(entry.Reference, row.Reference, StringComparison.OrdinalIgnoreCase) : Normalize(entry.Description) == Normalize(row.Description));
    private static decimal DecimalValue(IXLCell cell) => cell.TryGetValue<decimal>(out var value) ? value : decimal.TryParse(cell.GetString(), out value) ? value : 0m;
    private static string Normalize(string value) => new(value.Normalize(NormalizationForm.FormD).Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c)).Select(char.ToLowerInvariant).ToArray());
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
