using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Penny.Infrastructure.Data.Migrations;

[DbContext(typeof(PennyDbContext))]
[Migration("20261128000000_AddMonthlyExpenseReport")]
public sealed class AddMonthlyExpenseReport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE INDEX [IX_Movement_MonthlyExpenseReport]
            ON [dbo].[Movement] ([AccountId], [Type], [MovementDate])
            INCLUDE ([CategoryId], [SubcategoryId], [Amount]);
            """);

        // Dynamic DDL keeps CREATE PROCEDURE first in its batch in idempotent migration scripts.
        migrationBuilder.Sql("""
            EXEC(N'
            CREATE PROCEDURE [dbo].[GetMonthlyExpenseReport]
                @UserId nvarchar(450),
                @AccountId int,
                @Year int
            AS
            BEGIN
                SET NOCOUNT ON;

                IF NOT EXISTS (
                    SELECT 1 FROM [dbo].[Account] WHERE [Id] = @AccountId AND [UserId] = @UserId
                )
                    RETURN;

                DECLARE @StartDate datetime2(7) = DATEFROMPARTS(@Year, 1, 1);
                DECLARE @EndDate datetime2(7) = DATETIME2FROMPARTS(@Year, 12, 31, 23, 59, 59, 9999999, 7);

                WITH UserCategories AS (
                    SELECT [Id], [Name] FROM [dbo].[Category] WHERE [UserId] = @UserId
                ),
                ExpenseTotals AS (
                    SELECT
                           CASE
                               WHEN GROUPING(movement.CategoryId) = 1 THEN 3
                               WHEN GROUPING(movement.SubcategoryId) = 1 THEN 0
                               WHEN movement.SubcategoryId IS NULL THEN 2
                               ELSE 1
                           END AS RowKind,
                           movement.CategoryId,
                           movement.SubcategoryId,
                           SUM(CASE WHEN DATEPART(month, movement.MovementDate) = 1 THEN movement.Amount ELSE 0 END) AS [January],
                           SUM(CASE WHEN DATEPART(month, movement.MovementDate) = 2 THEN movement.Amount ELSE 0 END) AS [February],
                           SUM(CASE WHEN DATEPART(month, movement.MovementDate) = 3 THEN movement.Amount ELSE 0 END) AS [March],
                           SUM(CASE WHEN DATEPART(month, movement.MovementDate) = 4 THEN movement.Amount ELSE 0 END) AS [April],
                           SUM(CASE WHEN DATEPART(month, movement.MovementDate) = 5 THEN movement.Amount ELSE 0 END) AS [May],
                           SUM(CASE WHEN DATEPART(month, movement.MovementDate) = 6 THEN movement.Amount ELSE 0 END) AS [June],
                           SUM(CASE WHEN DATEPART(month, movement.MovementDate) = 7 THEN movement.Amount ELSE 0 END) AS [July],
                           SUM(CASE WHEN DATEPART(month, movement.MovementDate) = 8 THEN movement.Amount ELSE 0 END) AS [August],
                           SUM(CASE WHEN DATEPART(month, movement.MovementDate) = 9 THEN movement.Amount ELSE 0 END) AS [September],
                           SUM(CASE WHEN DATEPART(month, movement.MovementDate) = 10 THEN movement.Amount ELSE 0 END) AS [October],
                           SUM(CASE WHEN DATEPART(month, movement.MovementDate) = 11 THEN movement.Amount ELSE 0 END) AS [November],
                           SUM(CASE WHEN DATEPART(month, movement.MovementDate) = 12 THEN movement.Amount ELSE 0 END) AS [December],
                           SUM(movement.Amount) AS AnnualTotal
                    FROM [dbo].[Movement] AS movement
                    INNER JOIN UserCategories AS category ON category.Id = movement.CategoryId
                    WHERE movement.AccountId = @AccountId
                      AND movement.[Type] = 2
                      AND movement.MovementDate BETWEEN @StartDate AND @EndDate
                    GROUP BY GROUPING SETS (
                        (movement.CategoryId, movement.SubcategoryId),
                        (movement.CategoryId),
                        ()
                    )
                ),
                ReportRows AS (
                    SELECT 0 AS RowKind, category.Id AS CategoryId, CAST(NULL AS int) AS SubcategoryId,
                           category.[Name] AS CategoryName, category.[Name] AS [Name]
                    FROM UserCategories AS category
                    UNION ALL
                    SELECT 1, category.Id, subcategory.Id, category.[Name], subcategory.[Name]
                    FROM UserCategories AS category
                    INNER JOIN [dbo].[Subcategory] AS subcategory ON subcategory.CategoryId = category.Id
                    UNION ALL
                    SELECT 2, category.Id, NULL, category.[Name], NULL FROM UserCategories AS category
                    UNION ALL
                    SELECT 3, NULL, NULL, NULL, NULL
                ),
                ReportAmounts AS (
                    SELECT reportRow.RowKind, reportRow.CategoryId, reportRow.SubcategoryId, reportRow.CategoryName, reportRow.[Name],
                       ISNULL(totals.[January], 0) AS [January],
                       ISNULL(totals.[February], 0) AS [February],
                       ISNULL(totals.[March], 0) AS [March],
                       ISNULL(totals.[April], 0) AS [April],
                       ISNULL(totals.[May], 0) AS [May],
                       ISNULL(totals.[June], 0) AS [June],
                       ISNULL(totals.[July], 0) AS [July],
                       ISNULL(totals.[August], 0) AS [August],
                       ISNULL(totals.[September], 0) AS [September],
                       ISNULL(totals.[October], 0) AS [October],
                       ISNULL(totals.[November], 0) AS [November],
                       ISNULL(totals.[December], 0) AS [December],
                       CAST(ISNULL(totals.AnnualTotal, 0) AS decimal(28, 2)) AS AnnualTotal
                    FROM ReportRows AS reportRow
                    LEFT JOIN ExpenseTotals AS totals
                        ON totals.RowKind = reportRow.RowKind
                        AND (totals.CategoryId = reportRow.CategoryId OR reportRow.CategoryId IS NULL)
                        AND (totals.SubcategoryId = reportRow.SubcategoryId OR reportRow.SubcategoryId IS NULL)
                )
                SELECT RowKind, CategoryId, SubcategoryId, [Name],
                   [January],
                   [February],
                   [March],
                   [April],
                   [May],
                   [June],
                   [July],
                   [August],
                   [September],
                   [October],
                   [November],
                   [December],
                   AnnualTotal,
                   AnnualTotal / NULLIF(MAX(CASE WHEN RowKind = 3 THEN AnnualTotal END) OVER (), 0) AS AnnualShare
                FROM ReportAmounts
                ORDER BY CASE WHEN RowKind = 3 THEN 1 ELSE 0 END, CategoryName, CategoryId, RowKind, [Name], SubcategoryId;

                SELECT
                    (SELECT TOP (1) YEAR(MovementDate) FROM [dbo].[Movement]
                     WHERE AccountId = @AccountId AND [Type] = 2 ORDER BY MovementDate) AS FirstYear,
                    (SELECT TOP (1) YEAR(MovementDate) FROM [dbo].[Movement]
                     WHERE AccountId = @AccountId AND [Type] = 2 ORDER BY MovementDate DESC) AS LastYear;
            END');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP PROCEDURE [dbo].[GetMonthlyExpenseReport];
            DROP INDEX [IX_Movement_MonthlyExpenseReport] ON [dbo].[Movement];
            """);
    }
}
