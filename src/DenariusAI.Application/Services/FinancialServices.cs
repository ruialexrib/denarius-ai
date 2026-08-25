using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;

namespace DenariusAI.Application.Services;

/// <summary>
/// Service for managing financial groups.
/// </summary>
/// <param name="unitOfWork">The unit of work instance.</param>
public sealed class FinancialGroupService(IUnitOfWork unitOfWork) : IFinancialGroupService
{
    /// <summary>
    /// Lists all financial groups.
    /// </summary>
    /// <param name="activeOnly">If true, returns only active groups.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of financial group DTOs.</returns>
    public async Task<IReadOnlyList<FinancialGroupDto>> ListAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var groups = activeOnly
            ? await unitOfWork.Repository<FinancialGroup>().FindAsync(group => group.IsActive, cancellationToken)
            : await unitOfWork.Repository<FinancialGroup>().ListAsync(cancellationToken);
        return groups.OrderBy(group => group.SortOrder)
            .Select(group => new FinancialGroupDto(group.Id, group.Name, group.Description, group.Kind, group.IsActive, group.SortOrder)).ToList();
    }

    /// <summary>
    /// Gets a financial group by ID.
    /// </summary>
    /// <param name="id">The group ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The financial group DTO or null if not found.</returns>
    public async Task<FinancialGroupDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await unitOfWork.Repository<FinancialGroup>().GetByIdAsync(id, cancellationToken);
        return group is null ? null : new(group.Id, group.Name, group.Description, group.Kind, group.IsActive, group.SortOrder);
    }

    /// <summary>
    /// Gets the classification statement for a financial group.
    /// </summary>
    /// <param name="id">The group ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of classification statement lines.</returns>
    public async Task<IReadOnlyList<ClassificationStatementLineDto>> GetStatementAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await GetAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Grupo não encontrado.");
        return await unitOfWork.JournalEntries.GetClassificationStatementAsync(id, null, group.Kind, cancellationToken);
    }

    /// <summary>
    /// Creates a new financial group.
    /// </summary>
    /// <param name="input">The group data.</param>
    /// <param name="userId">The user ID creating the group.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created group.</returns>
    public async Task<Guid> CreateAsync(SaveFinancialGroupDto input, string userId, CancellationToken cancellationToken = default)
    {
        Validate(input, userId);
        var name = input.Name.Trim();
        var repository = unitOfWork.Repository<FinancialGroup>();
        if (await repository.ExistsAsync(group => group.Name == name, cancellationToken)) throw new InvalidOperationException("Já existe um grupo com este nome.");
        var group = new FinancialGroup { Name = name, Description = input.Description?.Trim(), Kind = input.Kind, SortOrder = input.SortOrder, CreatedBy = userId };
        await repository.AddAsync(group, cancellationToken); await unitOfWork.SaveChangesAsync(cancellationToken); return group.Id;
    }

    /// <summary>
    /// Updates an existing financial group.
    /// </summary>
    /// <param name="id">The group ID.</param>
    /// <param name="input">The updated group data.</param>
    /// <param name="userId">The user ID updating the group.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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

    /// <summary>
    /// Sets the active status of a financial group.
    /// </summary>
    /// <param name="id">The group ID.</param>
    /// <param name="isActive">The active status to set.</param>
    /// <param name="userId">The user ID performing the action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SetActiveAsync(Guid id, bool isActive, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId); var repository = unitOfWork.Repository<FinancialGroup>();
        var group = await repository.GetByIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Grupo não encontrado.");
        if (!isActive && await unitOfWork.Repository<Category>().ExistsAsync(category => category.FinancialGroupId == id && category.IsActive, cancellationToken))
            throw new InvalidOperationException("Desative primeiro as categorias ativas deste grupo.");
        group.IsActive = isActive; group.UpdatedBy = userId; await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Validates the financial group input data.
    /// </summary>
    /// <param name="input">The input data to validate.</param>
    /// <param name="userId">The user ID.</param>
    private static void Validate(SaveFinancialGroupDto input, string userId)
    {
        ArgumentNullException.ThrowIfNull(input); ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 100) throw new ArgumentException("O nome do grupo é obrigatório e não pode exceder 100 caracteres.");
        if (input.SortOrder < 0) throw new ArgumentOutOfRangeException(nameof(input), "A ordem não pode ser negativa.");
        if (!Enum.IsDefined(input.Kind)) throw new ArgumentOutOfRangeException(nameof(input), "Tipo de grupo inválido.");
    }
}

/// <summary>
/// Service for managing categories.
/// </summary>
/// <param name="unitOfWork">The unit of work instance.</param>
public sealed class CategoryService(IUnitOfWork unitOfWork) : ICategoryService
{
    /// <summary>
    /// Lists all categories.
    /// </summary>
    /// <param name="groupId">Optional group ID to filter by.</param>
    /// <param name="activeOnly">If true, returns only active categories.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of category DTOs.</returns>
    public async Task<IReadOnlyList<CategoryDto>> ListAsync(Guid? groupId = null, bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var categories = await unitOfWork.Repository<Category>().FindAsync(category =>
            (!groupId.HasValue || category.FinancialGroupId == groupId) && (!activeOnly || category.IsActive), cancellationToken);
        return categories.OrderBy(category => category.SortOrder)
            .Select(category => new CategoryDto(category.Id, category.FinancialGroupId, category.Name, category.Description, category.IsActive, category.SortOrder)).ToList();
    }

    /// <summary>
    /// Gets a category by ID.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The category DTO or null if not found.</returns>
    public async Task<CategoryDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await unitOfWork.Repository<Category>().GetByIdAsync(id, cancellationToken);
        return category is null ? null : new(category.Id, category.FinancialGroupId, category.Name, category.Description, category.IsActive, category.SortOrder);
    }

    /// <summary>
    /// Gets the classification statement for a category.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of classification statement lines.</returns>
    public async Task<IReadOnlyList<ClassificationStatementLineDto>> GetStatementAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await GetAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Categoria não encontrada.");
        var group = await unitOfWork.Repository<FinancialGroup>().GetByIdAsync(category.FinancialGroupId, cancellationToken)
            ?? throw new KeyNotFoundException("Grupo não encontrado.");
        return await unitOfWork.JournalEntries.GetClassificationStatementAsync(null, id, group.Kind, cancellationToken);
    }

    /// <summary>
    /// Creates a new category.
    /// </summary>
    /// <param name="input">The category data.</param>
    /// <param name="userId">The user ID creating the category.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created category.</returns>
    public async Task<Guid> CreateAsync(SaveCategoryDto input, string userId, CancellationToken cancellationToken = default)
    {
        Validate(input, userId); await EnsureActiveGroupAsync(input.FinancialGroupId, cancellationToken);
        var name = input.Name.Trim(); var repository = unitOfWork.Repository<Category>();
        if (await repository.ExistsAsync(category => category.FinancialGroupId == input.FinancialGroupId && category.Name == name, cancellationToken))
            throw new InvalidOperationException("Já existe uma categoria com este nome no grupo selecionado.");
        var category = new Category { FinancialGroupId = input.FinancialGroupId, Name = name, Description = input.Description?.Trim(), SortOrder = input.SortOrder, CreatedBy = userId };
        await repository.AddAsync(category, cancellationToken); await unitOfWork.SaveChangesAsync(cancellationToken); return category.Id;
    }

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="input">The updated category data.</param>
    /// <param name="userId">The user ID updating the category.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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

    /// <summary>
    /// Sets the active status of a category.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="isActive">The active status to set.</param>
    /// <param name="userId">The user ID performing the action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SetActiveAsync(Guid id, bool isActive, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId); var repository = unitOfWork.Repository<Category>();
        var category = await repository.GetByIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Categoria não encontrada.");
        if (isActive) await EnsureActiveGroupAsync(category.FinancialGroupId, cancellationToken);
        category.IsActive = isActive; category.UpdatedBy = userId; await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if a category is being used in accounts, journal entries, or budgets.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the category is in use, otherwise false.</returns>
    private async Task<bool> IsCategoryInUseAsync(Guid id, CancellationToken cancellationToken) =>
        await unitOfWork.Repository<Account>().ExistsAsync(account => account.CategoryId == id, cancellationToken) ||
        await unitOfWork.Repository<JournalEntryLine>().ExistsAsync(line => line.CategoryId == id, cancellationToken) ||
        await unitOfWork.Repository<BudgetLine>().ExistsAsync(line => line.CategoryId == id, cancellationToken);

    /// <summary>
    /// Ensures that the specified financial group exists and is active.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task EnsureActiveGroupAsync(Guid groupId, CancellationToken cancellationToken)
    {
        if (!await unitOfWork.Repository<FinancialGroup>().ExistsAsync(group => group.Id == groupId && group.IsActive, cancellationToken))
            throw new InvalidOperationException("O grupo selecionado não existe ou está inativo.");
    }

    /// <summary>
    /// Validates the category input data.
    /// </summary>
    /// <param name="input">The input data to validate.</param>
    /// <param name="userId">The user ID.</param>
    private static void Validate(SaveCategoryDto input, string userId)
    {
        ArgumentNullException.ThrowIfNull(input); ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (input.FinancialGroupId == Guid.Empty) throw new ArgumentException("Selecione um grupo.");
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 100) throw new ArgumentException("O nome da categoria é obrigatório e não pode exceder 100 caracteres.");
        if (input.SortOrder < 0) throw new ArgumentOutOfRangeException(nameof(input), "A ordem não pode ser negativa.");
    }
}

/// <summary>
/// Service for managing accounts.
/// </summary>
/// <param name="unitOfWork">The unit of work instance.</param>
public sealed class AccountService(IUnitOfWork unitOfWork) : IAccountService
{
    /// <summary>
    /// Lists all accounts with their balances.
    /// </summary>
    /// <param name="activeOnly">If true, returns only active accounts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of account DTOs.</returns>
    public Task<IReadOnlyList<AccountDto>> ListAsync(bool activeOnly = false, CancellationToken cancellationToken = default) =>
        unitOfWork.Accounts.ListWithBalancesAsync(activeOnly, cancellationToken);

    /// <summary>
    /// Gets an account by ID with its balance.
    /// </summary>
    /// <param name="id">The account ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The account DTO or null if not found.</returns>
    public async Task<AccountDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await unitOfWork.Accounts.GetByIdAsync(id, cancellationToken);
        if (account is null) return null;
        var balance = await unitOfWork.Accounts.GetBalanceAsync(id, cancellationToken);
        return new AccountDto(account.Id, account.Name, account.Description, account.AccountType, account.InitialBalance, balance, account.Currency, account.IsActive, account.CategoryId);
    }

    /// <summary>
    /// Gets the statement of transactions for an account.
    /// </summary>
    /// <param name="id">The account ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of account statement lines.</returns>
    public Task<IReadOnlyList<AccountStatementLineDto>> GetStatementAsync(Guid id, CancellationToken cancellationToken = default) =>
        unitOfWork.Accounts.GetStatementAsync(id, cancellationToken);

    /// <summary>
    /// Creates a new account.
    /// </summary>
    /// <param name="input">The account data.</param>
    /// <param name="userId">The user ID creating the account.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created account.</returns>
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

    /// <summary>
    /// Updates an existing account.
    /// </summary>
    /// <param name="id">The account ID.</param>
    /// <param name="input">The updated account data.</param>
    /// <param name="userId">The user ID updating the account.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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

    /// <summary>
    /// Sets the active status of an account.
    /// </summary>
    /// <param name="id">The account ID.</param>
    /// <param name="isActive">The active status to set.</param>
    /// <param name="userId">The user ID performing the action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SetActiveAsync(Guid id, bool isActive, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        var account = await unitOfWork.Accounts.GetByIdAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Conta não encontrada.");
        if (isActive) await EnsureCategoryMatchesAsync(account.AccountType, account.CategoryId, cancellationToken);
        account.IsActive = isActive;
        account.UpdatedBy = userId;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Ensures that the category matches the account type.
    /// </summary>
    /// <param name="accountType">The account type.</param>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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

    /// <summary>
    /// Validates the account input data.
    /// </summary>
    /// <param name="input">The input data to validate.</param>
    /// <param name="userId">The user ID.</param>
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

    /// <summary>
    /// Normalizes the currency code to uppercase.
    /// </summary>
    /// <param name="currency">The currency code.</param>
    /// <returns>The normalized currency code.</returns>
    private static string NormalizeCurrency(string currency) => currency?.Trim().ToUpperInvariant() ?? string.Empty;
}

/// <summary>
/// Service for managing journal entries.
/// </summary>
/// <param name="unitOfWork">The unit of work instance.</param>
public sealed class JournalEntryService(IUnitOfWork unitOfWork) : IJournalEntryService
{
    /// <summary>
    /// Lists all journal entries with summaries.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of journal entry summary DTOs.</returns>
    public Task<IReadOnlyList<JournalEntrySummaryDto>> ListAsync(CancellationToken cancellationToken = default) =>
        unitOfWork.JournalEntries.ListSummariesAsync(cancellationToken);

    /// <summary>
    /// Gets a journal entry by ID with full details.
    /// </summary>
    /// <param name="id">The journal entry ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The journal entry details DTO or null if not found.</returns>
    public async Task<JournalEntryDetailsDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await unitOfWork.JournalEntries.GetWithDetailsAsync(id, cancellationToken);
        return entry is null ? null : ToDetails(entry);
    }

    /// <summary>
    /// Creates a new journal entry.
    /// </summary>
    /// <param name="request">The journal entry data.</param>
    /// <param name="userId">The user ID creating the entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the created journal entry.</returns>
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

    /// <summary>
    /// Updates an existing journal entry.
    /// </summary>
    /// <param name="id">The journal entry ID.</param>
    /// <param name="request">The updated journal entry data.</param>
    /// <param name="userId">The user ID updating the entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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

    /// <summary>
    /// Cancels a journal entry.
    /// </summary>
    /// <param name="id">The journal entry ID.</param>
    /// <param name="userId">The user ID cancelling the entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task CancelAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        var entry = await unitOfWork.JournalEntries.GetWithDetailsAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Movimento não encontrado.");
        if (entry.Reconciliation?.Status == ReconciliationStatus.Reconciled) throw new InvalidOperationException("Um movimento reconciliado não pode ser anulado.");
        entry.Cancel(userId, DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the monthly summary of income and expenses.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The monthly summary DTO.</returns>
    public async Task<MonthlySummaryDto> GetMonthlySummaryAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, month);
        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1).AddDays(-1);
        var income = await unitOfWork.JournalEntries.GetAmountByGroupKindAsync(from, to, FinancialGroupKind.Income, cancellationToken);
        var expenses = await unitOfWork.JournalEntries.GetAmountByGroupKindAsync(from, to, FinancialGroupKind.Expense, cancellationToken);
        return new MonthlySummaryDto(income, expenses);
    }

    /// <summary>
    /// Validates the period (year and month).
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    private static void ValidatePeriod(int year, int month)
    {
        if (year is < 2000 or > 9999) throw new ArgumentOutOfRangeException(nameof(year));
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));
    }

    /// <summary>
    /// Validates the references (accounts and categories) in the journal entry lines.
    /// </summary>
    /// <param name="lines">The journal entry lines.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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

    /// <summary>
    /// Validates the journal entry request.
    /// </summary>
    /// <param name="request">The request to validate.</param>
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

    /// <summary>
    /// Validates that the budget exists.
    /// </summary>
    /// <param name="budgetId">The budget ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task ValidateBudgetAsync(Guid? budgetId, CancellationToken cancellationToken)
    {
        if (budgetId.HasValue && await unitOfWork.Budgets.GetByIdAsync(budgetId.Value, cancellationToken) is null)
            throw new InvalidOperationException("O orçamento selecionado não existe.");
    }

    /// <summary>
    /// Converts a journal entry entity to a details DTO.
    /// </summary>
    /// <param name="entry">The journal entry entity.</param>
    /// <returns>The journal entry details DTO.</returns>
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

/// <summary>
/// Service for managing budgets.
/// </summary>
/// <param name="unitOfWork">The unit of work instance.</param>
public sealed class BudgetService(IUnitOfWork unitOfWork) : IBudgetService
{
    /// <summary>
    /// Lists all budget periods.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of budget period DTOs.</returns>
    public async Task<IReadOnlyList<BudgetPeriodDto>> ListPeriodsAsync(CancellationToken cancellationToken = default) =>
        (await unitOfWork.Repository<Budget>().ListAsync(cancellationToken))
            .OrderByDescending(item => item.Year).ThenByDescending(item => item.Month)
            .Select(item => new BudgetPeriodDto(item.Id, item.Year, item.Month)).ToList();

    /// <summary>
    /// Gets the budget execution for a specific period.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of budget execution items.</returns>
    public Task<IReadOnlyList<BudgetExecutionItemDto>> GetExecutionAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        if (year is < 2000 or > 9999) throw new ArgumentOutOfRangeException(nameof(year));
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));
        return unitOfWork.Budgets.GetExecutionAsync(year, month, cancellationToken);
    }

    /// <summary>
    /// Saves budget lines for a specific period.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    /// <param name="lines">The budget lines to save.</param>
    /// <param name="userId">The user ID saving the budget.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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

    /// <summary>
    /// Validates the period (year and month).
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    private static void ValidatePeriod(int year, int month)
    {
        if (year is < 2000 or > 9999) throw new ArgumentOutOfRangeException(nameof(year));
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));
    }
}

/// <summary>
/// Service for managing reconciliations.
/// </summary>
/// <param name="unitOfWork">The unit of work instance.</param>
public sealed class ReconciliationService(IUnitOfWork unitOfWork) : IReconciliationService
{
    private static readonly AccountType[] BankingAccountTypes = [AccountType.BankAccount, AccountType.Savings, AccountType.TermDeposit];

    /// <summary>
    /// Lists journal entries for reconciliation.
    /// </summary>
    /// <param name="accountId">Optional account ID to filter by.</param>
    /// <param name="from">Optional start date.</param>
    /// <param name="to">Optional end date.</param>
    /// <param name="status">Optional reconciliation status filter.</param>
    /// <param name="search">Optional search term.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of reconciliation item DTOs.</returns>
    public Task<IReadOnlyList<ReconciliationItemDto>> ListAsync(Guid? accountId = null, DateOnly? from = null, DateOnly? to = null, ReconciliationStatus? status = null, string? search = null, CancellationToken cancellationToken = default)
    {
        if (from.HasValue && to.HasValue && from > to) throw new ArgumentException("A data inicial não pode ser posterior à data final.");
        return unitOfWork.JournalEntries.ListForReconciliationAsync(accountId, from, to, status, search, cancellationToken);
    }

    /// <summary>
    /// Reconciles a journal entry.
    /// </summary>
    /// <param name="journalEntryId">The journal entry ID.</param>
    /// <param name="userId">The user ID performing the reconciliation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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

    /// <summary>
    /// Undoes the reconciliation of a journal entry.
    /// </summary>
    /// <param name="journalEntryId">The journal entry ID.</param>
    /// <param name="userId">The user ID undoing the reconciliation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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
