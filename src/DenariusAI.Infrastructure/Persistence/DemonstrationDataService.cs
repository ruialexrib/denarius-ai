using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using DenariusAI.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;

namespace DenariusAI.Infrastructure.Persistence;

/// <summary>
/// Service responsible for loading demonstration data into the database.
/// </summary>
/// <param name="dbContext">The database context for data operations.</param>
/// <param name="userManager">Optional user manager for creating demonstration users.</param>
public sealed class DemonstrationDataService(DenariusDbContext dbContext, UserManager<ApplicationUser>? userManager = null) : IDemonstrationDataService
{
    private const string DemonstrationPdfBase64 = "JVBERi0xLjQKMSAwIG9iago8PCAvVHlwZSAvQ2F0YWxvZyAvUGFnZXMgMiAwIFIgPj4KZW5kb2JqCjIgMCBvYmoKPDwgL1R5cGUgL1BhZ2VzIC9LaWRzIFszIDAgUl0gL0NvdW50IDEgPj4KZW5kb2JqCjMgMCBvYmoKPDwgL1R5cGUgL1BhZ2UgL1BhcmVudCAyIDAgUiAvTWVkaWFCb3ggWzAgMCA2MTIgNzkyXSAvQ29udGVudHMgNCAwIFIgPj4KZW5kb2JqCjQgMCBvYmoKPDwgL0xlbmd0aCAwID4+CnN0cmVhbQoKZW5kc3RyZWFtCmVuZG9iagp0cmFpbGVyCjw8IC9Sb290IDEgMCBSIC9TaXplIDUgPj4KJSVFT0YK";

    /// <summary>
    /// The application settings key used to persist that the first-installation demonstration scenario
    /// has already been evaluated, independently of whether financial records still exist.
    /// </summary>
    private const string InitializationStateKey = "System.InitialDemonstrationDataSeededAt";

    /// <summary>
    /// Ensures the demonstration scenario is loaded exactly once for a brand-new installation.
    /// </summary>
    /// <remarks>
    /// Detection relies on an explicit, persisted marker in <see cref="DenariusDbContext.ApplicationSettings"/>
    /// rather than on the presence of financial records (such as <see cref="DenariusDbContext.Accounts"/> or
    /// <see cref="DenariusDbContext.JournalEntries"/>), so that ordinary restarts never reload or duplicate
    /// demonstration data even after the user deletes or resets their financial records.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The result of the load operation, or an unloaded result when initialization already occurred.</returns>
    public async Task<DemonstrationDataLoadResult> EnsureInitialDemonstrationDataAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.ApplicationSettings.AnyAsync(setting => setting.Key == InitializationStateKey, cancellationToken))
            return new(false, 0, 0, 0);

        var result = await LoadAsync(cancellationToken);
        dbContext.ApplicationSettings.Add(new ApplicationSetting
        {
            Key = InitializationStateKey,
            Value = DateTimeOffset.UtcNow.ToString("O"),
            CreatedBy = "system-init"
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    /// <summary>
    /// Loads demonstration data into the database if no data exists.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A result containing information about the loaded data.</returns>
    public async Task<DemonstrationDataLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Accounts.AnyAsync(cancellationToken)
            || await dbContext.JournalEntries.AnyAsync(cancellationToken)
            || await dbContext.Budgets.AnyAsync(cancellationToken)
            || await dbContext.SavingsCertificates.AnyAsync(cancellationToken))
        {
            return new(false, 0, 0, 0);
        }

        var accounts = CreateAccounts();
        var entries = CreateEntries();
        var budgets = CreateBudgets();

        dbContext.Accounts.AddRange(accounts);
        dbContext.JournalEntries.AddRange(entries);
        dbContext.Budgets.AddRange(budgets);
        dbContext.BudgetLines.AddRange(CreateBudgetLines());
        dbContext.Reconciliations.AddRange(CreateReconciliations());
        if (!await dbContext.Reminders.AnyAsync(cancellationToken))
            dbContext.Reminders.AddRange(CreateReminders());
        var savingsCertificates = CreateSavingsCertificates();
        dbContext.SavingsCertificates.AddRange(savingsCertificates);
        await AddCompleteScenarioAsync(cancellationToken);
        if (dbContext.Database.IsRelational())
        {
            await dbContext.Set<ApplicationUser>()
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(user => user.DemonstrationDataAcknowledgedAt, (DateTimeOffset?)null), cancellationToken);
        }
        else
        {
            foreach (var user in await dbContext.Set<ApplicationUser>().ToListAsync(cancellationToken))
                user.DemonstrationDataAcknowledgedAt = null;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        await EnsureUsersAsync(cancellationToken);

        return new(true, accounts.Length, entries.Length, budgets.Length);
    }

    /// <summary>Creates the demonstration accounts (bank account, savings, cash, income and expense counterparts).</summary>
    /// <returns>The five demonstration accounts.</returns>
    private static Account[] CreateAccounts() =>
    [
        new()
        {
            Id = Id("30000000", 1),
            Name = "Conta à Ordem — Demonstração",
            Description = "Conta bancária principal do cenário de demonstração.",
            AccountType = AccountType.BankAccount,
            InitialBalance = 1850m,
            Currency = "EUR",
            CategoryId = Id("20000000", 1),
            CreatedAt = SeedDate()
        },
        DemoAccount(2, "Conta Poupança — Demonstração", "Poupança familiar do cenário de demonstração.", AccountType.Savings, 4200m, 2),
        DemoAccount(3, "Dinheiro — Demonstração", "Carteira de numerário do cenário de demonstração.", AccountType.Cash, 120m, 4),
        DemoAccount(4, "Rendimentos — Demonstração", "Contrapartida contabilística dos rendimentos.", AccountType.Income, 0m, 10),
        DemoAccount(5, "Despesas — Demonstração", "Contrapartida contabilística das despesas.", AccountType.Expense, 0m, 33)
    ];

    /// <summary>Creates one deterministic demonstration account.</summary>
    /// <param name="id">Account identifier suffix.</param>
    /// <param name="name">Account name.</param>
    /// <param name="description">Account description.</param>
    /// <param name="type">Account type.</param>
    /// <param name="balance">Initial balance.</param>
    /// <param name="categoryCode">Category code.</param>
    /// <returns>A configured account.</returns>
    private static Account DemoAccount(int id, string name, string description, AccountType type, decimal balance, int categoryCode) => new()
    {
        Id = Id("30000000", id), Name = name, Description = description, AccountType = type, InitialBalance = balance,
        Currency = "EUR", CategoryId = Id("20000000", categoryCode), CreatedAt = SeedDate()
    };

    /// <summary>Creates the demonstration budgets for 8 months of the current demonstration year.</summary>
    /// <returns>The demonstration budgets.</returns>
    private static Budget[] CreateBudgets() => Enumerable.Range(1, 8)
        .Select(month => new Budget { Id = Id("60000000", month), Year = 2026, Month = month, CreatedAt = SeedDate(), CreatedBy = "demo-seed" })
        .ToArray();

    /// <summary>Creates the demonstration budget lines for all demonstration budgets.</summary>
    /// <returns>The demonstration budget lines.</returns>
    private static BudgetLine[] CreateBudgetLines() => Enumerable.Range(1, 8).SelectMany(month => new[]
    {
        BudgetLine(month, 1, 30, 780m), BudgetLine(month, 2, 31, 40m), BudgetLine(month, 3, 32, 85m),
        BudgetLine(month, 4, 33, 320m), BudgetLine(month, 5, 34, 140m), BudgetLine(month, 6, 35, 90m),
        BudgetLine(month, 7, 37, 110m), BudgetLine(month, 8, 38, 120m), BudgetLine(month, 9, 39, 35m)
    }).ToArray();

    /// <summary>Creates one deterministic demonstration budget line.</summary>
    /// <param name="month">The month number.</param>
    /// <param name="slot">The line slot within the budget.</param>
    /// <param name="categoryCode">The category code.</param>
    /// <param name="amount">The budgeted amount.</param>
    /// <returns>A configured budget line.</returns>
    private static BudgetLine BudgetLine(int month, int slot, int categoryCode, decimal amount) => new()
    {
        Id = Id("70000000", ((month - 1) * 9) + slot), BudgetId = Id("60000000", month), CategoryId = Id("20000000", categoryCode),
        Amount = amount, CreatedAt = SeedDate(), CreatedBy = "demo-seed"
    };

    /// <summary>Creates the demonstration reconciliations for the first six journal entries of every month.</summary>
    /// <returns>The demonstration reconciliations.</returns>
    private static Reconciliation[] CreateReconciliations() => Enumerable.Range(1, 8)
        .SelectMany(month => Enumerable.Range(1, 6).Select(slot => DemoReconciliation(((month - 1) * 6) + slot, EntryId(month, slot))))
        .ToArray();

    /// <summary>Creates one deterministic demonstration reconciliation.</summary>
    /// <param name="id">The reconciliation identifier suffix.</param>
    /// <param name="entryId">The reconciled journal entry identifier.</param>
    /// <returns>A configured, reconciled record.</returns>
    private static Reconciliation DemoReconciliation(int id, Guid entryId) => new()
    {
        Id = Id("80000000", id), JournalEntryId = entryId, Status = ReconciliationStatus.Reconciled,
        ReconciledAt = SeedDate().AddDays(id), ReconciledBy = "demo-seed", CreatedAt = SeedDate(), CreatedBy = "demo-seed"
    };

    /// <summary>Creates the demonstration reminders shown on the dashboard for a brand-new installation.</summary>
    /// <returns>The demonstration reminders.</returns>
    private static Reminder[] CreateReminders() =>
    [
        DemoReminder(1, "Confirmar a próxima capitalização dos Certificados de Aforro", new DateOnly(2026, 8, 28), 7),
        DemoReminder(2, "Rever e renovar o seguro automóvel", new DateOnly(2026, 9, 15), 15),
        DemoReminder(3, "Preparar o orçamento familiar do próximo ano", new DateOnly(2026, 12, 15), 30)
    ];

    /// <summary>Creates one deterministic demonstration reminder.</summary>
    /// <param name="id">Reminder identifier suffix.</param>
    /// <param name="text">Reminder text.</param>
    /// <param name="eventDate">Date of the reminded event.</param>
    /// <param name="noticeDays">Number of days before the event to send notice.</param>
    /// <returns>A configured reminder.</returns>
    private static Reminder DemoReminder(int id, string text, DateOnly eventDate, int noticeDays) =>
        new(text, eventDate, noticeDays) { Id = Id("90000000", id), CreatedAt = SeedDate(), CreatedBy = "demo-seed" };

    /// <summary>Generates a journal entry identifier based on month and slot, matching <see cref="Entry"/>.</summary>
    /// <param name="month">The month number.</param>
    /// <param name="slot">The entry slot within the month.</param>
    /// <returns>A unique identifier for the journal entry.</returns>
    private static Guid EntryId(int month, int slot) => Id("40000000", ((month - 1) * 9) + slot);

    /// <summary>Adds demonstration records for every user-facing non-core application area when that area is empty.</summary>
    /// <param name="cancellationToken">Cancellation token to cancel database checks.</param>
    private async Task AddCompleteScenarioAsync(CancellationToken cancellationToken)
    {
        if (!await dbContext.StockPositions.AnyAsync(cancellationToken))
        {
            var positions = CreateStockPositions();
            dbContext.StockPositions.AddRange(positions);
            dbContext.StockPrices.AddRange(CreateStockPrices(positions));
        }

        if (!await dbContext.Warranties.AnyAsync(cancellationToken))
        {
            var warranties = CreateWarranties();
            dbContext.Warranties.AddRange(warranties);
            dbContext.Reminders.AddRange(CreateWarrantyReminders(warranties));
        }

        if (!await dbContext.Correspondence.AnyAsync(cancellationToken))
        {
            var correspondence = CreateCorrespondence();
            dbContext.Correspondence.AddRange(correspondence);
            dbContext.CorrespondenceMetadata.AddRange(CreateCorrespondenceMetadata(correspondence));
        }

        if (!await dbContext.InsurancePolicies.AnyAsync(cancellationToken))
        {
            var policies = CreateInsurancePolicies();
            var premiums = CreateInsurancePremiums(policies);
            dbContext.InsurancePolicies.AddRange(policies);
            dbContext.InsurancePremiums.AddRange(premiums);
            dbContext.InsurancePolicyAttachments.Add(CreatePolicyAttachment(policies[0]));
            dbContext.InsurancePremiumAttachments.Add(CreatePremiumAttachment(premiums[0]));
        }
    }

    /// <summary>Creates owned and watchlist stock positions for portfolio exploration.</summary>
    /// <returns>Deterministic stock positions in EUR.</returns>
    private static StockPosition[] CreateStockPositions() =>
    [
        StockPosition(1, "EDP.LS", "EDP", "EURONEXT", 180m, 3.42m, 3.68m, false),
        StockPosition(2, "GALP.LS", "Galp Energia", "EURONEXT", 45m, 15.20m, 16.05m, false),
        StockPosition(3, "VWCE.DE", "Vanguard FTSE All-World ETF", "XETRA", 0m, 0m, 132.40m, true)
    ];

    /// <summary>Creates one deterministic stock position.</summary>
    /// <param name="id">Position identifier suffix.</param>
    /// <param name="ticker">Market ticker.</param>
    /// <param name="name">Instrument name.</param>
    /// <param name="exchange">Market exchange.</param>
    /// <param name="quantity">Owned quantity.</param>
    /// <param name="averageCost">Average acquisition cost.</param>
    /// <param name="currentPrice">Latest known price.</param>
    /// <param name="watchlistOnly">Whether the instrument belongs only to the watchlist.</param>
    /// <returns>A configured stock position.</returns>
    private static StockPosition StockPosition(int id, string ticker, string name, string exchange, decimal quantity, decimal averageCost, decimal currentPrice, bool watchlistOnly) =>
        new(ticker, name, exchange, "EUR", quantity, averageCost, currentPrice, new DateOnly(2026, 8, 31), new DateOnly(2026, 1, 1), true, watchlistOnly)
        {
            Id = Id("80000000", id), CreatedAt = SeedDate(), CreatedBy = "demo-seed"
        };

    /// <summary>Creates monthly historical prices for the demonstration portfolio.</summary>
    /// <param name="positions">Positions that own the price observations.</param>
    /// <returns>Dated price observations for each position.</returns>
    private static StockPrice[] CreateStockPrices(IReadOnlyList<StockPosition> positions)
    {
        var closingPrices = new[]
        {
            new[] { 3.21m, 3.28m, 3.34m, 3.30m, 3.45m, 3.52m, 3.61m, 3.68m },
            new[] { 14.10m, 14.55m, 14.82m, 15.05m, 15.42m, 15.70m, 15.88m, 16.05m },
            new[] { 118.20m, 120.35m, 119.80m, 123.10m, 126.45m, 128.70m, 130.20m, 132.40m }
        };
        return positions.SelectMany((position, positionIndex) => Enumerable.Range(1, 8)
            .Select(month => new StockPrice(position.Id, new DateOnly(2026, month, DateTime.DaysInMonth(2026, month)), closingPrices[positionIndex][month - 1])))
            .ToArray();
    }

    /// <summary>Creates warranties with downloadable demonstration documents.</summary>
    /// <returns>Representative active warranties.</returns>
    private static Warranty[] CreateWarranties() =>
    [
        Warranty(1, "Computador portátil", "Loja Tecnologia — Demonstração", new DateOnly(2025, 11, 10), new DateOnly(2028, 11, 10), "Garantia alargada do equipamento familiar."),
        Warranty(2, "Máquina de lavar roupa", "Loja Casa — Demonstração", new DateOnly(2024, 5, 18), new DateOnly(2027, 5, 18), "Inclui assistência técnica no domicílio.")
    ];

    /// <summary>Creates one deterministic warranty.</summary>
    /// <param name="id">Warranty identifier suffix.</param>
    /// <param name="name">Warranty name.</param>
    /// <param name="supplier">Fictitious supplier.</param>
    /// <param name="purchaseDate">Purchase date.</param>
    /// <param name="expiryDate">Expiry date.</param>
    /// <param name="notes">Demonstration notes.</param>
    /// <returns>A configured warranty.</returns>
    private static Warranty Warranty(int id, string name, string supplier, DateOnly purchaseDate, DateOnly expiryDate, string notes) =>
        new(name, supplier, purchaseDate, expiryDate, notes, $"garantia-demo-{id}.pdf", DemonstrationPdfBase64)
        {
            Id = Id("90000000", id), CreatedAt = SeedDate(), CreatedBy = "demo-seed"
        };

    /// <summary>Creates reminders linked to demonstration warranties.</summary>
    /// <param name="warranties">Warranties requiring expiry reminders.</param>
    /// <returns>Linked reminders.</returns>
    private static Reminder[] CreateWarrantyReminders(IReadOnlyList<Warranty> warranties) => warranties.Select((warranty, index) =>
    {
        var reminder = new Reminder($"Rever garantia: {warranty.Name}", warranty.ExpiryDate, 30)
        {
            Id = Id("91000000", index + 1), CreatedAt = SeedDate(), CreatedBy = "demo-seed"
        };
        reminder.LinkToWarranty(warranty.Id);
        return reminder;
    }).ToArray();

    /// <summary>Creates correspondence records with representative documents.</summary>
    /// <returns>Demonstration correspondence.</returns>
    private static Correspondence[] CreateCorrespondence() =>
    [
        Correspondence(1, "Atualização do contrato de energia", "Energia Exemplo", new DateOnly(2026, 7, 14), "Comunicação sobre novas condições tarifárias."),
        Correspondence(2, "Informação anual da conta", "Banco Exemplo", new DateOnly(2026, 2, 3), "Resumo anual para arquivo familiar.")
    ];

    /// <summary>Creates one deterministic correspondence record.</summary>
    /// <param name="id">Correspondence identifier suffix.</param>
    /// <param name="subject">Message subject.</param>
    /// <param name="sender">Fictitious sender.</param>
    /// <param name="receivedDate">Receipt date.</param>
    /// <param name="notes">Demonstration notes.</param>
    /// <returns>A configured correspondence record.</returns>
    private static Correspondence Correspondence(int id, string subject, string sender, DateOnly receivedDate, string notes) =>
        new(subject, sender, receivedDate, notes, $"correspondencia-demo-{id}.pdf", DemonstrationPdfBase64)
        {
            Id = Id("a0000000", id), CreatedAt = SeedDate(), CreatedBy = "demo-seed"
        };

    /// <summary>Creates searchable metadata for demonstration correspondence.</summary>
    /// <param name="correspondence">Correspondence records that own the metadata.</param>
    /// <returns>Metadata values with confidence information.</returns>
    private static CorrespondenceMetadata[] CreateCorrespondenceMetadata(IReadOnlyList<Correspondence> correspondence) =>
    [
        Metadata(1, correspondence[0].Id, "Entidade", "Energia Exemplo", "high"),
        Metadata(2, correspondence[0].Id, "Referência", "DEMO-ENERGIA-2026", "high"),
        Metadata(3, correspondence[1].Id, "Entidade", "Banco Exemplo", "high"),
        Metadata(4, correspondence[1].Id, "Ano", "2025", "high")
    ];

    /// <summary>Creates one deterministic correspondence metadata value.</summary>
    /// <param name="id">Metadata identifier suffix.</param>
    /// <param name="correspondenceId">Owning correspondence identifier.</param>
    /// <param name="key">Metadata key.</param>
    /// <param name="value">Metadata value.</param>
    /// <param name="confidence">Extraction confidence.</param>
    /// <returns>A configured metadata value.</returns>
    private static CorrespondenceMetadata Metadata(int id, Guid correspondenceId, string key, string value, string confidence) =>
        new(correspondenceId, key, value, confidence)
        {
            Id = Id("a1000000", id), CreatedAt = SeedDate(), CreatedBy = "demo-seed"
        };

    /// <summary>Creates active insurance policies across common household coverage types.</summary>
    /// <returns>Demonstration insurance policies.</returns>
    private static InsurancePolicy[] CreateInsurancePolicies() =>
    [
        InsurancePolicy(1, "Seguro multirriscos habitação", "Seguradora Exemplo", "DEMO-HAB-2026", InsurancePolicyType.Home, InsurancePaymentFrequency.Annual, new DateOnly(2025, 10, 1), new DateOnly(2027, 9, 30), new DateOnly(2026, 10, 1), "Habitação principal"),
        InsurancePolicy(2, "Seguro automóvel", "Companhia Exemplo", "DEMO-AUTO-2026", InsurancePolicyType.Motor, InsurancePaymentFrequency.Semiannual, new DateOnly(2026, 1, 12), new DateOnly(2027, 1, 11), new DateOnly(2027, 1, 12), "Veículo familiar"),
        InsurancePolicy(3, "Seguro de saúde familiar", "Saúde Exemplo", "DEMO-SAUDE-2026", InsurancePolicyType.Health, InsurancePaymentFrequency.Monthly, new DateOnly(2026, 1, 1), null, new DateOnly(2027, 1, 1), "Agregado familiar")
    ];

    /// <summary>Creates one deterministic insurance policy.</summary>
    /// <param name="id">Policy identifier suffix.</param>
    /// <param name="name">Policy name.</param>
    /// <param name="insurer">Fictitious insurer.</param>
    /// <param name="number">Policy number.</param>
    /// <param name="type">Coverage type.</param>
    /// <param name="frequency">Payment frequency.</param>
    /// <param name="startDate">Coverage start.</param>
    /// <param name="endDate">Optional coverage end.</param>
    /// <param name="renewalDate">Optional renewal date.</param>
    /// <param name="insuredSubject">Insured subject.</param>
    /// <returns>A configured insurance policy.</returns>
    private static InsurancePolicy InsurancePolicy(int id, string name, string insurer, string number, InsurancePolicyType type, InsurancePaymentFrequency frequency, DateOnly startDate, DateOnly? endDate, DateOnly? renewalDate, string insuredSubject) =>
        new(name, insurer, number, type, frequency, startDate, endDate, renewalDate, insuredSubject, "Dados exclusivamente demonstrativos.")
        {
            Id = Id("b0000000", id), CreatedAt = SeedDate(), CreatedBy = "demo-seed"
        };

    /// <summary>Creates paid-history candidates and upcoming premiums for demonstration policies.</summary>
    /// <param name="policies">Policies that own the premiums.</param>
    /// <returns>Demonstration premium schedule.</returns>
    private static InsurancePremium[] CreateInsurancePremiums(IReadOnlyList<InsurancePolicy> policies) =>
    [
        Premium(1, policies[0].Id, 186.40m, new DateOnly(2025, 10, 1), new DateOnly(2026, 9, 30), new DateOnly(2025, 10, 1), "DEMO-PREM-HAB"),
        Premium(2, policies[1].Id, 214.75m, new DateOnly(2026, 1, 12), new DateOnly(2026, 7, 11), new DateOnly(2026, 1, 12), "DEMO-PREM-AUTO-1"),
        Premium(3, policies[1].Id, 214.75m, new DateOnly(2026, 7, 12), new DateOnly(2027, 1, 11), new DateOnly(2026, 7, 12), "DEMO-PREM-AUTO-2"),
        Premium(4, policies[2].Id, 68.90m, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), new DateOnly(2026, 8, 1), "DEMO-PREM-SAUDE-08"),
        Premium(5, policies[2].Id, 68.90m, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), new DateOnly(2026, 9, 1), "DEMO-PREM-SAUDE-09")
    ];

    /// <summary>Creates one deterministic insurance premium.</summary>
    /// <param name="id">Premium identifier suffix.</param>
    /// <param name="policyId">Owning policy identifier.</param>
    /// <param name="amount">Premium amount.</param>
    /// <param name="periodStart">Coverage period start.</param>
    /// <param name="periodEnd">Coverage period end.</param>
    /// <param name="dueDate">Premium due date.</param>
    /// <param name="reference">Provider reference.</param>
    /// <returns>A configured premium.</returns>
    private static InsurancePremium Premium(int id, Guid policyId, decimal amount, DateOnly periodStart, DateOnly periodEnd, DateOnly dueDate, string reference) =>
        new(policyId, amount, periodStart, periodEnd, dueDate, reference)
        {
            Id = Id("b1000000", id), CreatedAt = SeedDate(), CreatedBy = "demo-seed"
        };

    /// <summary>Creates a demonstration PDF attached to an insurance policy.</summary>
    /// <param name="policy">Policy that owns the attachment.</param>
    /// <returns>A configured policy attachment.</returns>
    private static InsurancePolicyAttachment CreatePolicyAttachment(InsurancePolicy policy) =>
        new(policy.Id, "condicoes-seguro-demo.pdf", "application/pdf", DemonstrationPdfBase64)
        {
            Id = Id("b2000000", 1), CreatedAt = SeedDate(), CreatedBy = "demo-seed"
        };

    /// <summary>Creates a demonstration PDF attached to an insurance premium.</summary>
    /// <param name="premium">Premium that owns the attachment.</param>
    /// <returns>A configured premium attachment.</returns>
    private static InsurancePremiumAttachment CreatePremiumAttachment(InsurancePremium premium) =>
        new(premium.Id, "recibo-premio-demo.pdf", "application/pdf", DemonstrationPdfBase64)
        {
            Id = Id("b3000000", 1), CreatedAt = SeedDate(), CreatedBy = "demo-seed"
        };

    /// <summary>Gets the fixed timestamp used by supplementary demonstration records.</summary>
    /// <returns>The deterministic seed timestamp.</returns>
    private static DateTimeOffset SeedDate() => new(2026, 8, 24, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Creates an array of sample savings certificates for demonstration purposes.
    /// </summary>
    /// <returns>An array of savings certificates.</returns>
    private static SavingsCertificate[] CreateSavingsCertificates() =>
    [
        Certificate(1, new DateOnly(2023, 3, 15), "E-2023-001842", "Poupança familiar", 2500m, 2.50m, 2684.72m, new DateOnly(2026, 9, 15)),
        Certificate(2, new DateOnly(2024, 7, 2), "F-2024-007316", "Fundo de emergência", 5000m, 2.50m, 5218.34m, new DateOnly(2026, 10, 2)),
        Certificate(3, new DateOnly(2025, 11, 21), "F-2025-014908", "Objetivos de longo prazo", 1500m, 2.00m, 1521.66m, new DateOnly(2026, 8, 21))
    ];

    /// <summary>
    /// Creates a savings certificate with the specified parameters.
    /// </summary>
    /// <param name="id">The certificate identifier.</param>
    /// <param name="date">The certificate date.</param>
    /// <param name="number">The certificate number.</param>
    /// <param name="description">The certificate description.</param>
    /// <param name="investment">The investment amount.</param>
    /// <param name="rate">The interest rate.</param>
    /// <param name="currentValue">The current value of the certificate.</param>
    /// <param name="nextCapitalization">The next capitalization date.</param>
    /// <returns>A configured savings certificate.</returns>
    private static SavingsCertificate Certificate(int id, DateOnly date, string number, string description,
        decimal investment, decimal rate, decimal currentValue, DateOnly nextCapitalization) =>
        new(date, number, description, investment, rate, currentValue, nextCapitalization)
        {
            Id = Id("70000000", id), CreatedAt = new DateTimeOffset(2026, 8, 24, 0, 0, 0, TimeSpan.Zero), CreatedBy = "demo-seed"
        };

    /// <summary>
    /// Creates sample journal entries for demonstration purposes covering 8 months.
    /// </summary>
    /// <returns>An array of journal entries.</returns>
    private static JournalEntry[] CreateEntries()
    {
        var entries = new List<JournalEntry>();
        for (var month = 1; month <= 8; month++)
        {
            var values = new[] { 2650m, 780m, 210m + month * 4m, 62m + month, 28m + month, 95m, 250m, 70m + month * 3m, 180m + month * 10m };
            entries.Add(Entry(month,1,1,"Salário mensal",$"SAL-2026-{month:D2}",(1,values[0],0,null),(4,0,values[0],10)));
            entries.Add(Entry(month,2,3,"Renda da casa",$"RENDA-{month:D2}",(5,values[1],0,30),(1,0,values[1],null)));
            entries.Add(Entry(month,3,6,"Compras de supermercado",$"SUPER-{month:D2}",(5,values[2],0,33),(1,0,values[2],null)));
            entries.Add(Entry(month,4,8,"Fatura de eletricidade",$"ELEC-{month:D2}",(5,values[3],0,32),(1,0,values[3],null)));
            entries.Add(Entry(month,5,9,"Fatura de água",$"AGUA-{month:D2}",(5,values[4],0,31),(1,0,values[4],null)));
            entries.Add(Entry(month,6,12,"Passe e combustível",$"TRANSP-{month:D2}",(5,values[5],0,34),(1,0,values[5],null)));
            entries.Add(Entry(month,7,15,"Transferência para poupança",$"POUP-{month:D2}",(2,values[6],0,2),(1,0,values[6],1100)));
            entries.Add(Entry(month,8,20,"Lazer em família",$"LAZER-{month:D2}",(5,values[7],0,38),(1,0,values[7],null)));
            entries.Add(Entry(month,9,24,"Trabalho ocasional",$"EXTRA-{month:D2}",(1,values[8],0,null),(4,0,values[8],20)));
        }
        return entries.ToArray();
    }

    /// <summary>
    /// Creates a journal entry with two lines (debit and credit).
    /// </summary>
    /// <param name="month">The month of the entry.</param>
    /// <param name="slot">The slot number within the month.</param>
    /// <param name="day">The day of the month.</param>
    /// <param name="description">The entry description.</param>
    /// <param name="reference">The entry reference.</param>
    /// <param name="first">The first line details (account, debit, credit, category).</param>
    /// <param name="second">The second line details (account, debit, credit, category).</param>
    /// <returns>A configured journal entry.</returns>
    private static JournalEntry Entry(int month, int slot, int day, string description, string reference,
        (int Account, decimal Debit, decimal Credit, int? Category) first,
        (int Account, decimal Debit, decimal Credit, int? Category) second)
    {
        var entryId = ((month - 1) * 9) + slot;
        var entry = new JournalEntry(new DateOnly(2026, month, day), description, reference, $"Dados de demonstração — {month:D2}/2026")
        {
            Id = Id("40000000", entryId), CreatedAt = new DateTimeOffset(2026, 8, 24, 0, 0, 0, TimeSpan.Zero), CreatedBy = "demo-seed"
        };
        entry.AssignBudget(Id("60000000", month));
        var firstLine = entry.AddLine(Id("30000000", first.Account), first.Debit, first.Credit, categoryId: first.Category.HasValue ? Id("20000000", first.Category.Value) : null);
        var secondLine = entry.AddLine(Id("30000000", second.Account), second.Debit, second.Credit, categoryId: second.Category.HasValue ? Id("20000000", second.Category.Value) : null);
        firstLine.Id = Id("50000000", (entryId * 2) - 1);
        secondLine.Id = Id("50000000", entryId * 2);
        firstLine.CreatedAt = secondLine.CreatedAt = entry.CreatedAt;
        firstLine.CreatedBy = secondLine.CreatedBy = "demo-seed";
        entry.EnsureBalanced();
        return entry;
    }

    /// <summary>
    /// Ensures demonstration users exist in the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    public async Task EnsureUsersAsync(CancellationToken cancellationToken = default)
    {
        if (userManager is null) return;
        foreach (var (email, name, fixedPassword) in new[]
        {
            ("guest@denarius-ai.local", "Convidado — Demo", "Denarius2026!"),
            ("demo.familia@denarius.local", "Membro da família — Demo", (string?)null),
            ("demo.consulta@denarius.local", "Consulta financeira — Demo", (string?)null)
        })
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await userManager.FindByEmailAsync(email) is not null) continue;
            var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = name };
            var password = fixedPassword ?? $"Demo!{Convert.ToHexString(RandomNumberGenerator.GetBytes(12))}aA1";
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Demonstration user could not be created: {string.Join("; ", result.Errors.Select(error => error.Code))}");
            var roleResult = await userManager.AddToRoleAsync(user, ApplicationRoles.User);
            if (!roleResult.Succeeded)
                throw new InvalidOperationException($"Demonstration user role could not be assigned: {string.Join("; ", roleResult.Errors.Select(error => error.Code))}");
        }
    }

    /// <summary>
    /// Generates a GUID by combining a prefix with a numeric value.
    /// </summary>
    /// <param name="prefix">The prefix for the GUID.</param>
    /// <param name="value">The numeric value to include in the GUID.</param>
    /// <returns>A generated GUID.</returns>
    private static Guid Id(string prefix, int value) => Guid.Parse($"{prefix}-0000-0000-0000-{value:D12}");
}
