using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;

namespace DenariusAI.Application.Services;

public sealed class FinancialGroupService(IUnitOfWork unitOfWork) : IFinancialGroupService
{
    public async Task<IReadOnlyList<FinancialGroupDto>> ListAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var groups = activeOnly
            ? await unitOfWork.Repository<FinancialGroup>().FindAsync(group => group.IsActive, cancellationToken)
            : await unitOfWork.Repository<FinancialGroup>().ListAsync(cancellationToken);
        return groups.OrderBy(group => group.SortOrder)
            .Select(group => new FinancialGroupDto(group.Id, group.Name, group.Description, group.Kind, group.IsActive, group.SortOrder)).ToList();
    }

    public async Task<FinancialGroupDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await unitOfWork.Repository<FinancialGroup>().GetByIdAsync(id, cancellationToken);
        return group is null ? null : new(group.Id, group.Name, group.Description, group.Kind, group.IsActive, group.SortOrder);
    }

    public async Task<Guid> CreateAsync(SaveFinancialGroupDto input, string userId, CancellationToken cancellationToken = default)
    {
        Validate(input, userId);
        var name = input.Name.Trim();
        var repository = unitOfWork.Repository<FinancialGroup>();
        if (await repository.ExistsAsync(group => group.Name == name, cancellationToken)) throw new InvalidOperationException("Já existe um grupo com este nome.");
        var group = new FinancialGroup { Name = name, Description = input.Description?.Trim(), Kind = input.Kind, SortOrder = input.SortOrder, CreatedBy = userId };
        await repository.AddAsync(group, cancellationToken); await unitOfWork.SaveChangesAsync(cancellationToken); return group.Id;
    }

    public async Task UpdateAsync(Guid id, SaveFinancialGroupDto input, string userId, CancellationToken cancellationToken = default)
    {
        Validate(input, userId); var repository = unitOfWork.Repository<FinancialGroup>();
        var group = await repository.GetByIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Grupo não encontrado.");
        var name = input.Name.Trim();
        if (await repository.ExistsAsync(item => item.Id != id && item.Name == name, cancellationToken)) throw new InvalidOperationException("Já existe um grupo com este nome.");
        if (group.Kind != input.Kind && await unitOfWork.Repository<Category>().ExistsAsync(category => category.FinancialGroupId == id, cancellationToken))
            throw new InvalidOperationException("Não é possível alterar o tipo de um grupo que já possui categorias.");
        group.Name = name; group.Description = input.Description?.Trim(); group.Kind = input.Kind; group.SortOrder = input.SortOrder; group.UpdatedBy = userId;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(Guid id, bool isActive, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId); var repository = unitOfWork.Repository<FinancialGroup>();
        var group = await repository.GetByIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Grupo não encontrado.");
        if (!isActive && await unitOfWork.Repository<Category>().ExistsAsync(category => category.FinancialGroupId == id && category.IsActive, cancellationToken))
            throw new InvalidOperationException("Desative primeiro as categorias ativas deste grupo.");
        group.IsActive = isActive; group.UpdatedBy = userId; await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void Validate(SaveFinancialGroupDto input, string userId)
    {
        ArgumentNullException.ThrowIfNull(input); ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 100) throw new ArgumentException("O nome do grupo é obrigatório e não pode exceder 100 caracteres.");
        if (input.SortOrder < 0) throw new ArgumentOutOfRangeException(nameof(input), "A ordem não pode ser negativa.");
        if (!Enum.IsDefined(input.Kind)) throw new ArgumentOutOfRangeException(nameof(input), "Tipo de grupo inválido.");
    }
}

public sealed class CategoryService(IUnitOfWork unitOfWork) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> ListAsync(Guid? groupId = null, bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var categories = await unitOfWork.Repository<Category>().FindAsync(category =>
            (!groupId.HasValue || category.FinancialGroupId == groupId) && (!activeOnly || category.IsActive), cancellationToken);
        return categories.OrderBy(category => category.SortOrder)
            .Select(category => new CategoryDto(category.Id, category.FinancialGroupId, category.Name, category.Description, category.IsActive, category.SortOrder)).ToList();
    }

    public async Task<CategoryDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await unitOfWork.Repository<Category>().GetByIdAsync(id, cancellationToken);
        return category is null ? null : new(category.Id, category.FinancialGroupId, category.Name, category.Description, category.IsActive, category.SortOrder);
    }

    public async Task<Guid> CreateAsync(SaveCategoryDto input, string userId, CancellationToken cancellationToken = default)
    {
        Validate(input, userId); await EnsureActiveGroupAsync(input.FinancialGroupId, cancellationToken);
        var name = input.Name.Trim(); var repository = unitOfWork.Repository<Category>();
        if (await repository.ExistsAsync(category => category.FinancialGroupId == input.FinancialGroupId && category.Name == name, cancellationToken))
            throw new InvalidOperationException("Já existe uma categoria com este nome no grupo selecionado.");
        var category = new Category { FinancialGroupId = input.FinancialGroupId, Name = name, Description = input.Description?.Trim(), SortOrder = input.SortOrder, CreatedBy = userId };
        await repository.AddAsync(category, cancellationToken); await unitOfWork.SaveChangesAsync(cancellationToken); return category.Id;
    }

    public async Task UpdateAsync(Guid id, SaveCategoryDto input, string userId, CancellationToken cancellationToken = default)
    {
        Validate(input, userId); await EnsureActiveGroupAsync(input.FinancialGroupId, cancellationToken); var repository = unitOfWork.Repository<Category>();
        var category = await repository.GetByIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Categoria não encontrada.");
        var name = input.Name.Trim();
        if (await repository.ExistsAsync(item => item.Id != id && item.FinancialGroupId == input.FinancialGroupId && item.Name == name, cancellationToken))
            throw new InvalidOperationException("Já existe uma categoria com este nome no grupo selecionado.");
        if (category.FinancialGroupId != input.FinancialGroupId && await IsCategoryInUseAsync(id, cancellationToken))
            throw new InvalidOperationException("Não é possível mudar o grupo de uma categoria que já está a ser utilizada.");
        category.FinancialGroupId = input.FinancialGroupId; category.Name = name; category.Description = input.Description?.Trim(); category.SortOrder = input.SortOrder; category.UpdatedBy = userId;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(Guid id, bool isActive, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId); var repository = unitOfWork.Repository<Category>();
        var category = await repository.GetByIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Categoria não encontrada.");
        if (isActive) await EnsureActiveGroupAsync(category.FinancialGroupId, cancellationToken);
        category.IsActive = isActive; category.UpdatedBy = userId; await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> IsCategoryInUseAsync(Guid id, CancellationToken cancellationToken) =>
        await unitOfWork.Repository<Account>().ExistsAsync(account => account.CategoryId == id, cancellationToken) ||
        await unitOfWork.Repository<JournalEntryLine>().ExistsAsync(line => line.CategoryId == id, cancellationToken) ||
        await unitOfWork.Repository<BudgetLine>().ExistsAsync(line => line.CategoryId == id, cancellationToken);

    private async Task EnsureActiveGroupAsync(Guid groupId, CancellationToken cancellationToken)
    {
        if (!await unitOfWork.Repository<FinancialGroup>().ExistsAsync(group => group.Id == groupId && group.IsActive, cancellationToken))
            throw new InvalidOperationException("O grupo selecionado não existe ou está inativo.");
    }

    private static void Validate(SaveCategoryDto input, string userId)
    {
        ArgumentNullException.ThrowIfNull(input); ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (input.FinancialGroupId == Guid.Empty) throw new ArgumentException("Selecione um grupo.");
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 100) throw new ArgumentException("O nome da categoria é obrigatório e não pode exceder 100 caracteres.");
        if (input.SortOrder < 0) throw new ArgumentOutOfRangeException(nameof(input), "A ordem não pode ser negativa.");
    }
}

public sealed class AccountService(IUnitOfWork unitOfWork) : IAccountService
{
    public Task<IReadOnlyList<AccountDto>> ListAsync(bool activeOnly = false, CancellationToken cancellationToken = default) =>
        unitOfWork.Accounts.ListWithBalancesAsync(activeOnly, cancellationToken);

    public async Task<AccountDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await unitOfWork.Accounts.GetByIdAsync(id, cancellationToken);
        if (account is null) return null;
        var balance = await unitOfWork.Accounts.GetBalanceAsync(id, cancellationToken);
        return new AccountDto(account.Id, account.Name, account.Description, account.AccountType, account.InitialBalance, balance, account.Currency, account.IsActive, account.CategoryId);
    }

    public Task<IReadOnlyList<AccountStatementLineDto>> GetStatementAsync(Guid id, CancellationToken cancellationToken = default) =>
        unitOfWork.Accounts.GetStatementAsync(id, cancellationToken);

    public async Task<Guid> CreateAsync(SaveAccountDto input, string userId, CancellationToken cancellationToken = default)
    {
        Validate(input, userId);
        await EnsureCategoryMatchesAsync(input.AccountType, input.CategoryId, cancellationToken);
        var repository = unitOfWork.Accounts;
        var name = input.Name.Trim();
        if (await repository.ExistsAsync(account => account.Name == name, cancellationToken))
            throw new InvalidOperationException("Já existe uma conta com este nome.");
        var account = new Account
        {
            Name = name,
            Description = input.Description?.Trim(),
            AccountType = input.AccountType,
            InitialBalance = input.InitialBalance,
            Currency = NormalizeCurrency(input.Currency),
            CategoryId = input.CategoryId,
            CreatedBy = userId
        };
        await repository.AddAsync(account, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return account.Id;
    }

    public async Task UpdateAsync(Guid id, SaveAccountDto input, string userId, CancellationToken cancellationToken = default)
    {
        Validate(input, userId);
        var repository = unitOfWork.Accounts;
        var account = await repository.GetByIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Conta não encontrada.");
        await EnsureCategoryMatchesAsync(input.AccountType, input.CategoryId, cancellationToken);
        var name = input.Name.Trim();
        if (await repository.ExistsAsync(item => item.Id != id && item.Name == name, cancellationToken))
            throw new InvalidOperationException("Já existe uma conta com este nome.");
        var currency = NormalizeCurrency(input.Currency);
        var changesAccountingMeaning = account.AccountType != input.AccountType || account.CategoryId != input.CategoryId || account.Currency != currency;
        if (changesAccountingMeaning && await unitOfWork.Repository<JournalEntryLine>().ExistsAsync(line => line.AccountId == id, cancellationToken))
            throw new InvalidOperationException("Não é possível alterar o tipo, a moeda ou a categoria de uma conta que já possui movimentos.");
        account.Name = name;
        account.Description = input.Description?.Trim();
        account.AccountType = input.AccountType;
        account.InitialBalance = input.InitialBalance;
        account.Currency = currency;
        account.CategoryId = input.CategoryId;
        account.UpdatedBy = userId;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(Guid id, bool isActive, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        var account = await unitOfWork.Accounts.GetByIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Conta não encontrada.");
        if (isActive) await EnsureCategoryMatchesAsync(account.AccountType, account.CategoryId, cancellationToken);
        account.IsActive = isActive;
        account.UpdatedBy = userId;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCategoryMatchesAsync(AccountType accountType, Guid? categoryId, CancellationToken cancellationToken)
    {
        if (!categoryId.HasValue) return;
        var category = await unitOfWork.Repository<Category>().GetByIdAsync(categoryId.Value, cancellationToken);
        if (category is null || !category.IsActive) throw new InvalidOperationException("A categoria selecionada não existe ou está inativa.");
        var group = await unitOfWork.Repository<FinancialGroup>().GetByIdAsync(category.FinancialGroupId, cancellationToken);
        if (group is null || !group.IsActive) throw new InvalidOperationException("O grupo da categoria selecionada está inativo.");
        var expectedKind = accountType switch
        {
            AccountType.Income => FinancialGroupKind.Income,
            AccountType.Expense => FinancialGroupKind.Expense,
            _ => FinancialGroupKind.Asset
        };
        if (group.Kind != expectedKind) throw new InvalidOperationException("A categoria selecionada não é compatível com o tipo da conta.");
    }

    private static void Validate(SaveAccountDto input, string userId)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 120)
            throw new ArgumentException("O nome da conta é obrigatório e não pode exceder 120 caracteres.");
        if (input.Description?.Trim().Length > 500) throw new ArgumentException("A descrição não pode exceder 500 caracteres.");
        if (!Enum.IsDefined(input.AccountType)) throw new ArgumentOutOfRangeException(nameof(input), "Tipo de conta inválido.");
        var currency = NormalizeCurrency(input.Currency);
        if (currency.Length != 3 || currency.Any(character => character is < 'A' or > 'Z'))
            throw new ArgumentException("A moeda deve conter um código de três letras, por exemplo EUR.");
    }

    private static string NormalizeCurrency(string currency) => currency?.Trim().ToUpperInvariant() ?? string.Empty;
}

public sealed class JournalEntryService(IUnitOfWork unitOfWork) : IJournalEntryService
{
    public Task<IReadOnlyList<JournalEntrySummaryDto>> ListAsync(CancellationToken cancellationToken = default) =>
        unitOfWork.JournalEntries.ListSummariesAsync(cancellationToken);

    public async Task<JournalEntryDetailsDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await unitOfWork.JournalEntries.GetWithDetailsAsync(id, cancellationToken);
        return entry is null ? null : ToDetails(entry);
    }

    public async Task<JournalEntryResultDto> CreateAsync(CreateJournalEntryRequest request, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ValidateRequest(request);

        JournalEntryResultDto? result = null;
        await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            await ValidateReferencesAsync(request.Lines, transactionToken);
            await ValidateBudgetAsync(request.BudgetId, transactionToken);

            var entry = new JournalEntry(request.Date, request.Description, request.Reference, request.Notes) { CreatedBy = userId };
            entry.AssignBudget(request.BudgetId);
            foreach (var line in request.Lines) entry.AddLine(line.AccountId, line.Debit, line.Credit, line.Description, line.CategoryId);
            entry.EnsureBalanced();
            await unitOfWork.JournalEntries.AddAsync(entry, transactionToken);
            await unitOfWork.SaveChangesAsync(transactionToken);
            result = new JournalEntryResultDto(entry.Id, entry.Date, entry.Description, entry.TotalDebit, entry.TotalCredit, entry.Status);
        }, cancellationToken);
        return result ?? throw new InvalidOperationException("The journal entry was not created.");
    }

    public async Task UpdateAsync(Guid id, CreateJournalEntryRequest request, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ValidateRequest(request);
        await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            var entry = await unitOfWork.JournalEntries.GetWithDetailsAsync(id, transactionToken)
                ?? throw new KeyNotFoundException("Movimento não encontrado.");
            if (entry.Status == JournalEntryStatus.Cancelled) throw new InvalidOperationException("Um movimento anulado não pode ser editado.");
            if (entry.Reconciliation?.Status == ReconciliationStatus.Reconciled) throw new InvalidOperationException("Um movimento reconciliado não pode ser editado.");
            await ValidateReferencesAsync(request.Lines, transactionToken);
            await ValidateBudgetAsync(request.BudgetId, transactionToken);
            entry.UpdateDetails(request.Date, request.Description, request.Reference, request.Notes);
            entry.AssignBudget(request.BudgetId);
            entry.ClearLines();
            foreach (var line in request.Lines)
            {
                var addedLine = entry.AddLine(line.AccountId, line.Debit, line.Credit, line.Description, line.CategoryId);
                await unitOfWork.Repository<JournalEntryLine>().AddAsync(addedLine, transactionToken);
            }
            entry.EnsureBalanced();
            entry.UpdatedBy = userId;
            await unitOfWork.SaveChangesAsync(transactionToken);
        }, cancellationToken);
    }

    public async Task CancelAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        var entry = await unitOfWork.JournalEntries.GetWithDetailsAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Movimento não encontrado.");
        if (entry.Reconciliation?.Status == ReconciliationStatus.Reconciled) throw new InvalidOperationException("Um movimento reconciliado não pode ser anulado.");
        entry.Cancel(userId, DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<MonthlySummaryDto> GetMonthlySummaryAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, month);
        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1).AddDays(-1);
        var income = await unitOfWork.JournalEntries.GetAmountByGroupKindAsync(from, to, FinancialGroupKind.Income, cancellationToken);
        var expenses = await unitOfWork.JournalEntries.GetAmountByGroupKindAsync(from, to, FinancialGroupKind.Expense, cancellationToken);
        return new MonthlySummaryDto(income, expenses);
    }

    private static void ValidatePeriod(int year, int month)
    {
        if (year is < 2000 or > 9999) throw new ArgumentOutOfRangeException(nameof(year));
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));
    }

    private async Task ValidateReferencesAsync(IReadOnlyCollection<JournalEntryLineInput> lines, CancellationToken cancellationToken)
    {
        var currencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            var account = await unitOfWork.Accounts.GetByIdAsync(line.AccountId, cancellationToken);
            if (account is null || !account.IsActive) throw new InvalidOperationException("Todas as linhas devem utilizar contas ativas.");
            currencies.Add(account.Currency);
            if (!line.CategoryId.HasValue) continue;
            var category = await unitOfWork.Repository<Category>().GetByIdAsync(line.CategoryId.Value, cancellationToken);
            if (category is null || !category.IsActive) throw new InvalidOperationException("Todas as categorias utilizadas devem estar ativas.");
            var group = await unitOfWork.Repository<FinancialGroup>().GetByIdAsync(category.FinancialGroupId, cancellationToken);
            if (group is null || !group.IsActive) throw new InvalidOperationException("O grupo de uma categoria utilizada está inativo.");
            var expectedKind = account.AccountType switch
            {
                AccountType.Income => FinancialGroupKind.Income,
                AccountType.Expense => FinancialGroupKind.Expense,
                _ => FinancialGroupKind.Asset
            };
            if (group.Kind != expectedKind) throw new InvalidOperationException("Uma categoria não é compatível com o tipo da conta da respetiva linha.");
        }
        if (currencies.Count > 1) throw new InvalidOperationException("Não é possível lançar movimentos entre contas com moedas diferentes sem conversão cambial.");
    }

    private static void ValidateRequest(CreateJournalEntryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Date == default) throw new ArgumentException("A data do movimento é obrigatória.");
        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length > 250) throw new ArgumentException("A descrição é obrigatória e não pode exceder 250 caracteres.");
        if (request.Reference?.Trim().Length > 100) throw new ArgumentException("A referência não pode exceder 100 caracteres.");
        if (request.Notes?.Trim().Length > 2000) throw new ArgumentException("As notas não podem exceder 2000 caracteres.");
        if (request.Lines is null || request.Lines.Count < 2) throw new ArgumentException("O movimento deve possuir pelo menos duas linhas.");
        if (request.Lines.Select(line => line.AccountId).Distinct().Count() < 2) throw new ArgumentException("O movimento deve utilizar pelo menos duas contas diferentes.");
        foreach (var line in request.Lines)
        {
            if (line.AccountId == Guid.Empty) throw new ArgumentException("Selecione uma conta em todas as linhas.");
            if (line.Debit < 0m || line.Credit < 0m || (line.Debit == 0m) == (line.Credit == 0m)) throw new ArgumentException("Cada linha deve ter um valor positivo apenas no débito ou apenas no crédito.");
            if (line.Description?.Trim().Length > 250) throw new ArgumentException("A descrição de uma linha não pode exceder 250 caracteres.");
        }
        if (request.Lines.Sum(line => line.Debit) != request.Lines.Sum(line => line.Credit)) throw new InvalidOperationException("O total do débito deve ser igual ao total do crédito.");
    }

    private async Task ValidateBudgetAsync(Guid? budgetId, CancellationToken cancellationToken)
    {
        if (budgetId.HasValue && await unitOfWork.Budgets.GetByIdAsync(budgetId.Value, cancellationToken) is null)
            throw new InvalidOperationException("O orçamento selecionado não existe.");
    }

    private static JournalEntryDetailsDto ToDetails(JournalEntry entry) => new(
        entry.Id,
        entry.Date,
        entry.Description,
        entry.Reference,
        entry.Notes,
        entry.Status,
        entry.CancelledAt,
        entry.CancelledBy,
        entry.Reconciliation?.Status ?? ReconciliationStatus.Unreconciled,
        entry.Lines.Select(line => new JournalEntryLineDto(line.Id, line.AccountId, line.Account.Name, line.CategoryId, line.Category?.Name, line.Debit, line.Credit, line.Description)).ToList(),
        entry.BudgetId,
        entry.Budget is null ? null : $"{entry.Budget.Month:D2}/{entry.Budget.Year}");
}

public sealed class BudgetService(IUnitOfWork unitOfWork) : IBudgetService
{
    public async Task<IReadOnlyList<BudgetPeriodDto>> ListPeriodsAsync(CancellationToken cancellationToken = default) =>
        (await unitOfWork.Repository<Budget>().ListAsync(cancellationToken))
            .OrderByDescending(item => item.Year).ThenByDescending(item => item.Month)
            .Select(item => new BudgetPeriodDto(item.Id, item.Year, item.Month)).ToList();

    public Task<IReadOnlyList<BudgetExecutionItemDto>> GetExecutionAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        if (year is < 2000 or > 9999) throw new ArgumentOutOfRangeException(nameof(year));
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));
        return unitOfWork.Budgets.GetExecutionAsync(year, month, cancellationToken);
    }

    public async Task SaveAsync(int year, int month, IReadOnlyCollection<SaveBudgetLineDto> lines, string userId, CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, month);
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (lines.Count == 0) throw new ArgumentException("Indique pelo menos uma categoria.", nameof(lines));
        if (lines.Any(line => line.CategoryId == Guid.Empty)) throw new ArgumentException("Todas as categorias são obrigatórias.", nameof(lines));
        if (lines.GroupBy(line => line.CategoryId).Any(group => group.Count() > 1)) throw new ArgumentException("O orçamento contém categorias repetidas.", nameof(lines));
        if (lines.Any(line => line.Amount < 0m)) throw new ArgumentException("Os valores orçamentados não podem ser negativos.", nameof(lines));

        await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            var categoryIds = lines.Select(line => line.CategoryId).ToArray();
            var categories = await unitOfWork.Repository<Category>().FindAsync(category => categoryIds.Contains(category.Id) && category.IsActive && category.FinancialGroup.IsActive && category.FinancialGroup.Kind == FinancialGroupKind.Expense, transactionToken);
            if (categories.Count != categoryIds.Length) throw new InvalidOperationException("Apenas categorias de despesa ativas podem ser orçamentadas.");

            var budget = await unitOfWork.Budgets.GetByPeriodAsync(year, month, transactionToken);
            if (budget is null)
            {
                budget = new Budget { Year = year, Month = month, CreatedBy = userId };
                await unitOfWork.Budgets.AddAsync(budget, transactionToken);
            }
            else { budget.UpdatedBy = userId; }

            var lineRepository = unitOfWork.Repository<BudgetLine>();
            foreach (var input in lines)
            {
                var existing = budget.Lines.SingleOrDefault(line => line.CategoryId == input.CategoryId);
                if (input.Amount == 0m)
                {
                    if (existing is not null) lineRepository.Remove(existing);
                    continue;
                }
                if (existing is null)
                {
                    await lineRepository.AddAsync(new BudgetLine { BudgetId = budget.Id, CategoryId = input.CategoryId, Amount = input.Amount, CreatedBy = userId }, transactionToken);
                }
                else { existing.Amount = input.Amount; existing.UpdatedBy = userId; }
            }
            await unitOfWork.SaveChangesAsync(transactionToken);
        }, cancellationToken);
    }

    private static void ValidatePeriod(int year, int month)
    {
        if (year is < 2000 or > 9999) throw new ArgumentOutOfRangeException(nameof(year));
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));
    }
}

public sealed class ReconciliationService(IUnitOfWork unitOfWork) : IReconciliationService
{
    private static readonly AccountType[] BankingAccountTypes = [AccountType.BankAccount, AccountType.Savings, AccountType.TermDeposit];

    public Task<IReadOnlyList<ReconciliationItemDto>> ListAsync(Guid? accountId = null, DateOnly? from = null, DateOnly? to = null, ReconciliationStatus? status = null, string? search = null, CancellationToken cancellationToken = default)
    {
        if (from.HasValue && to.HasValue && from > to) throw new ArgumentException("A data inicial não pode ser posterior à data final.");
        return unitOfWork.JournalEntries.ListForReconciliationAsync(accountId, from, to, status, search, cancellationToken);
    }

    public async Task ReconcileAsync(Guid journalEntryId, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        var entry = await unitOfWork.JournalEntries.GetWithDetailsAsync(journalEntryId, cancellationToken)
            ?? throw new KeyNotFoundException("Movimento não encontrado.");
        if (entry.Status == JournalEntryStatus.Cancelled) throw new InvalidOperationException("Um movimento anulado não pode ser reconciliado.");
        if (!entry.Lines.Any(line => BankingAccountTypes.Contains(line.Account.AccountType))) throw new InvalidOperationException("Apenas movimentos associados a contas bancárias podem ser reconciliados.");
        var repository = unitOfWork.Repository<Reconciliation>();
        var reconciliation = entry.Reconciliation;
        if (reconciliation is null)
        {
            reconciliation = new Reconciliation { JournalEntryId = journalEntryId, CreatedBy = userId };
            await repository.AddAsync(reconciliation, cancellationToken);
        }
        else if (reconciliation.Status == ReconciliationStatus.Reconciled) throw new InvalidOperationException("O movimento já está reconciliado.");
        reconciliation.MarkReconciled(userId, DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UndoAsync(Guid journalEntryId, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        var entry = await unitOfWork.JournalEntries.GetWithDetailsAsync(journalEntryId, cancellationToken)
            ?? throw new KeyNotFoundException("Movimento não encontrado.");
        var reconciliation = entry.Reconciliation
            ?? throw new KeyNotFoundException("Reconciliação não encontrada.");
        if (reconciliation.Status != ReconciliationStatus.Reconciled) throw new InvalidOperationException("O movimento não está reconciliado.");
        reconciliation.MarkUnreconciled(); reconciliation.UpdatedBy = userId; reconciliation.UpdatedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
