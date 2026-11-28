using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Penny.Infrastructure.Data;

namespace Penny.Infrastructure.Reports;

public static class MonthlyExpenseReportQuery
{
    public static async Task<MonthlyExpenseReport?> GetMonthlyExpenseReportAsync(
        this PennyDbContext db,
        string userId,
        int accountId,
        int year,
        CancellationToken cancellationToken)
    {
        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "dbo.GetMonthlyExpenseReport";
            command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.NVarChar, 450) { Value = userId });
            command.Parameters.Add(new SqlParameter("@AccountId", SqlDbType.Int) { Value = accountId });
            command.Parameters.Add(new SqlParameter("@Year", SqlDbType.Int) { Value = year });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            const int firstMonthColumn = 4;
            const int monthsPerYear = 12;
            const int annualTotalColumn = firstMonthColumn + monthsPerYear;
            const int annualShareColumn = annualTotalColumn + 1;
            var rows = new List<MonthlyExpenseRow>();
            do
            {
                var amounts = new decimal[monthsPerYear];
                for (var month = 0; month < amounts.Length; month++)
                {
                    amounts[month] = reader.GetDecimal(firstMonthColumn + month);
                }

                rows.Add(new MonthlyExpenseRow(
                    (ExpenseRowKind)reader.GetInt32(0),
                    reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3),
                    amounts,
                    reader.GetDecimal(annualTotalColumn),
                    reader.IsDBNull(annualShareColumn) ? null : reader.GetDecimal(annualShareColumn)));
            }
            while (await reader.ReadAsync(cancellationToken));

            if (!await reader.NextResultAsync(cancellationToken) || !await reader.ReadAsync(cancellationToken))
            {
                throw new DataException("The expense report did not return its year range.");
            }

            return new MonthlyExpenseReport(
                reader.IsDBNull(0) ? null : reader.GetInt32(0),
                reader.IsDBNull(1) ? null : reader.GetInt32(1),
                rows);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}
