using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DenariusAI.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Removes the historical demonstration financial records that earlier migrations inserted directly
    /// (<c>DemonstrationAccountSeed</c>, <c>DemonstrationFinancialScenario</c>, <c>AssociateJournalEntriesWithBudgets</c>,
    /// <c>ExpandDemonstrationScenario</c> and <c>AddReminders</c>), so that <c>DemonstrationDataService</c> becomes the
    /// single source of truth for the demonstration scenario. Structural data (financial groups and categories) is
    /// intentionally left untouched, as it is required for the application to operate regardless of demonstration data.
    /// </summary>
    /// <remarks>
    /// This migration only removes rows that carry the <c>demo-seed</c> system marker used exclusively by the
    /// demonstration generator (never assignable through normal user actions), or, for accounts (which do not carry
    /// that marker), the five fixed identifiers reserved for the demonstration accounts since the first migration.
    /// Deletions are additionally guarded so that a row still referenced by data that is not itself marked as
    /// demonstration data (for example a real financial entry recorded against a demonstration account, or a real
    /// budget line added to a demonstration budget) is preserved instead of being removed. This makes the migration
    /// safe to apply to installations where a user may already have started recording real financial data.
    /// </remarks>
    public partial class NormalizeDemonstrationSeedOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- Remove demonstration journal entry lines and reconciliations first: they are leaf records that
                -- only ever reference the demonstration journal entries and accounts created by the same seed.
                DELETE FROM [denarius].[JournalEntryLines] WHERE [CreatedBy] = N'demo-seed';
                DELETE FROM [denarius].[Reconciliations] WHERE [CreatedBy] = N'demo-seed';

                -- Remove demonstration journal entries once their lines and reconciliations are gone.
                DELETE FROM [denarius].[JournalEntries] WHERE [CreatedBy] = N'demo-seed';

                -- Remove demonstration budget lines, then demonstration budgets, but keep a budget when a real
                -- (non demo-seed) budget line or journal entry still depends on it.
                DELETE FROM [denarius].[BudgetLines] WHERE [CreatedBy] = N'demo-seed';
                DELETE FROM [denarius].[Budgets]
                WHERE [CreatedBy] = N'demo-seed'
                    AND NOT EXISTS (SELECT 1 FROM [denarius].[BudgetLines] WHERE [BudgetId] = [denarius].[Budgets].[Id])
                    AND NOT EXISTS (SELECT 1 FROM [denarius].[JournalEntries] WHERE [BudgetId] = [denarius].[Budgets].[Id]);

                -- Remove demonstration reminders (ReminderAcknowledgements cascade automatically).
                DELETE FROM [denarius].[Reminders] WHERE [CreatedBy] = N'demo-seed';

                -- Remove the five demonstration accounts, identified by their fixed historical identifiers, but only
                -- when no remaining journal entry line (necessarily real user data, since demonstration lines were
                -- already removed above) still references the account.
                DELETE FROM [denarius].[Accounts]
                WHERE [Id] IN (
                    '30000000-0000-0000-0000-000000000001',
                    '30000000-0000-0000-0000-000000000002',
                    '30000000-0000-0000-0000-000000000003',
                    '30000000-0000-0000-0000-000000000004',
                    '30000000-0000-0000-0000-000000000005'
                )
                AND NOT EXISTS (SELECT 1 FROM [denarius].[JournalEntryLines] WHERE [AccountId] = [denarius].[Accounts].[Id]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally irreversible: the removed rows were historical demonstration data superseded by
            // DemonstrationDataService, which is now the single source of truth for the demonstration scenario.
            // Re-inserting them would recreate a duplicate, stale scenario instead of restoring user data.
        }
    }
}
