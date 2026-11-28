namespace Penny.Infrastructure.Reports;

public sealed record MonthlyExpenseReport(
    int? FirstYear,
    int? LastYear,
    IReadOnlyList<MonthlyExpenseRow> Rows);

public sealed record MonthlyExpenseRow(
    ExpenseRowKind RowKind,
    int? CategoryId,
    int? SubcategoryId,
    string? Name,
    decimal[] MonthlyAmounts,
    decimal AnnualTotal,
    decimal? AnnualShare);

public enum ExpenseRowKind
{
    Category = 0,
    Subcategory = 1,
    Other = 2,
    Total = 3
}
