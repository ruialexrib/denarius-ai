using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DenariusAI.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Normalizes untouched historical demonstration financial records so that <c>DemonstrationDataService</c>
    /// becomes the single source of truth for a fresh demonstration scenario while preserving existing user data.
    /// </summary>
    /// <remarks>
    /// Historical migrations inserted the original demonstration scenario directly. This migration only removes that
    /// legacy scenario when the database still looks like an untouched demonstration-only installation: there are no
    /// user-created core financial records and there is no audit history for any of the deterministic demonstration
    /// entity identifier ranges. If either condition is false, the legacy rows are left intact and startup merely marks
    /// the installation as already evaluated. Structural data (financial groups and categories) is never removed.
    /// </remarks>
    public partial class NormalizeDemonstrationSeedOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF
                    NOT EXISTS (
                        SELECT 1
                        FROM [denarius].[Accounts]
                        WHERE [Id] NOT IN (
                            '30000000-0000-0000-0000-000000000001',
                            '30000000-0000-0000-0000-000000000002',
                            '30000000-0000-0000-0000-000000000003',
                            '30000000-0000-0000-0000-000000000004',
                            '30000000-0000-0000-0000-000000000005'
                        )
                    )
                    AND NOT EXISTS (
                        SELECT 1 FROM [denarius].[JournalEntries]
                        WHERE ISNULL([CreatedBy], N'') <> N'demo-seed'
                    )
                    AND NOT EXISTS (
                        SELECT 1 FROM [denarius].[Budgets]
                        WHERE ISNULL([CreatedBy], N'') <> N'demo-seed'
                    )
                    AND NOT EXISTS (
                        SELECT 1 FROM [denarius].[BudgetLines]
                        WHERE ISNULL([CreatedBy], N'') <> N'demo-seed'
                    )
                    AND NOT EXISTS (
                        SELECT 1 FROM [denarius].[Reminders]
                        WHERE ISNULL([CreatedBy], N'') <> N'demo-seed'
                    )
                    AND NOT EXISTS (
                        SELECT 1
                        FROM [denarius].[AuditLogs]
                        WHERE [EntityId] LIKE N'30000000-%'
                           OR [EntityId] LIKE N'40000000-%'
                           OR [EntityId] LIKE N'60000000-%'
                           OR [EntityId] LIKE N'70000000-%'
                           OR [EntityId] LIKE N'80000000-%'
                           OR [EntityId] LIKE N'90000000-%'
                    )
                BEGIN
                    -- The database contains only the untouched historical demonstration scenario. Remove its leaf
                    -- records first so startup can recreate the current scenario through DemonstrationDataService.
                    DELETE FROM [denarius].[JournalEntryLines] WHERE [CreatedBy] = N'demo-seed';
                    DELETE FROM [denarius].[Reconciliations] WHERE [CreatedBy] = N'demo-seed';
                    DELETE FROM [denarius].[JournalEntries] WHERE [CreatedBy] = N'demo-seed';
                    DELETE FROM [denarius].[BudgetLines] WHERE [CreatedBy] = N'demo-seed';
                    DELETE FROM [denarius].[Budgets] WHERE [CreatedBy] = N'demo-seed';
                    DELETE FROM [denarius].[Reminders] WHERE [CreatedBy] = N'demo-seed';

                    DELETE FROM [denarius].[Accounts]
                    WHERE [Id] IN (
                        '30000000-0000-0000-0000-000000000001',
                        '30000000-0000-0000-0000-000000000002',
                        '30000000-0000-0000-0000-000000000003',
                        '30000000-0000-0000-0000-000000000004',
                        '30000000-0000-0000-0000-000000000005'
                    )
                    AND NOT EXISTS (
                        SELECT 1
                        FROM [denarius].[JournalEntryLines]
                        WHERE [AccountId] = [denarius].[Accounts].[Id]
                    );
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally irreversible: only untouched historical demonstration rows may be removed by Up().
            // Re-inserting the superseded scenario would duplicate the current DemonstrationDataService data.
        }
    }
}
