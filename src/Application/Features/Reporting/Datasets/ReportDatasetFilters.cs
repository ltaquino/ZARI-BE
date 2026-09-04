namespace ZARI.Application.Features.Reporting.Datasets;

using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

/// <summary>
/// Shared expression-tree plumbing behind every dataset's own FieldKey-keyed filter switch (not a
/// dataset itself — never picked up by AddReportDatasetsFromAssembly, which only scans for
/// IReportDataset implementations). Each dataset writes its own switch mapping a request's
/// FieldKey to one of its own compile-time property-selector lambdas (e.g. `i => i.InvoiceNo`) —
/// this file only turns that selector plus one ReportFilterValue into an EF-Core-translatable
/// predicate Where()'d onto the query. Nothing here is reachable except through a selector a
/// dataset wrote itself, so there is still no dynamic/string-parsed query anywhere.
/// </summary>
internal static class ReportDatasetFilters
{
    private static readonly MethodInfo StringContainsMethod =
        typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

    /// <summary>Applies a text-typed filter (Equals/NotEquals/Contains/In). Any other operator is
    /// silently skipped — the field simply isn't filtered by it, rather than erroring.</summary>
    public static IQueryable<T> Text<T>(IQueryable<T> query, ReportFilterValue filter, Expression<Func<T, string?>> selector)
    {
        var parameter = selector.Parameters[0];
        Expression? body = filter.Operator switch
        {
            ReportFilterOperator.Equals => Expression.Equal(selector.Body, Expression.Constant(filter.Value, typeof(string))),
            ReportFilterOperator.NotEquals => Expression.NotEqual(selector.Body, Expression.Constant(filter.Value, typeof(string))),
            ReportFilterOperator.Contains when filter.Value is not null =>
                Expression.Call(selector.Body, StringContainsMethod, Expression.Constant(filter.Value)),
            ReportFilterOperator.In when filter.Value is not null =>
                BuildContains(SplitList(filter.Value), selector.Body),
            _ => null,
        };
        return body is null ? query : query.Where(Expression.Lambda<Func<T, bool>>(body, parameter));
    }

    /// <summary>Applies a decimal-typed filter (Number/Currency fields): Equals/NotEquals/GreaterThan(OrEqual)/
    /// LessThan(OrEqual)/Between/In. The selector may target a non-nullable decimal property via a
    /// harmless `(decimal?)i.Prop` cast at the call site.</summary>
    public static IQueryable<T> Decimal<T>(IQueryable<T> query, ReportFilterValue filter, Expression<Func<T, decimal?>> selector)
    {
        var parameter = selector.Parameters[0];
        Expression? body = filter.Operator switch
        {
            ReportFilterOperator.Equals => Compare(selector.Body, ParseDecimal(filter.Value), Expression.Equal),
            ReportFilterOperator.NotEquals => Compare(selector.Body, ParseDecimal(filter.Value), Expression.NotEqual),
            ReportFilterOperator.GreaterThan => Compare(selector.Body, ParseDecimal(filter.Value), Expression.GreaterThan),
            ReportFilterOperator.GreaterThanOrEqual => Compare(selector.Body, ParseDecimal(filter.Value), Expression.GreaterThanOrEqual),
            ReportFilterOperator.LessThan => Compare(selector.Body, ParseDecimal(filter.Value), Expression.LessThan),
            ReportFilterOperator.LessThanOrEqual => Compare(selector.Body, ParseDecimal(filter.Value), Expression.LessThanOrEqual),
            ReportFilterOperator.Between => BuildBetween(selector.Body, ParseDecimal(filter.Value), ParseDecimal(filter.Value2)),
            ReportFilterOperator.In when filter.Value is not null =>
                BuildContains(SplitList(filter.Value).Select(ParseDecimal).ToList(), selector.Body),
            _ => null,
        };
        return body is null ? query : query.Where(Expression.Lambda<Func<T, bool>>(body, parameter));
    }

    /// <summary>Applies a date-typed filter. Value(s) are "yyyy-MM-dd" per IReportDataset's own
    /// convention. Equals/NotEquals match the whole calendar day; GreaterThan(OrEqual)/LessThan(OrEqual)/
    /// Between treat the bound(s) as whole-day boundaries (GreaterThan/LessThanOrEqual are exclusive/
    /// inclusive of that day's last tick, so "GreaterThan 2026-09-04" means "any time from 2026-09-05
    /// onward"). The selector may target a non-nullable DateTimeOffset property via a harmless
    /// `(DateTimeOffset?)i.Prop` cast at the call site.</summary>
    public static IQueryable<T> Date<T>(IQueryable<T> query, ReportFilterValue filter, Expression<Func<T, DateTimeOffset?>> selector)
    {
        var parameter = selector.Parameters[0];
        Expression? body = filter.Operator switch
        {
            ReportFilterOperator.Equals => BuildDayRange(selector.Body, filter.Value, negate: false),
            ReportFilterOperator.NotEquals => BuildDayRange(selector.Body, filter.Value, negate: true),
            ReportFilterOperator.GreaterThan => Compare(selector.Body, EndOfDay(filter.Value), Expression.GreaterThan),
            ReportFilterOperator.GreaterThanOrEqual => Compare(selector.Body, ParseDayStart(filter.Value), Expression.GreaterThanOrEqual),
            ReportFilterOperator.LessThan => Compare(selector.Body, ParseDayStart(filter.Value), Expression.LessThan),
            ReportFilterOperator.LessThanOrEqual => Compare(selector.Body, EndOfDay(filter.Value), Expression.LessThanOrEqual),
            ReportFilterOperator.Between => BuildBetween(selector.Body, ParseDayStart(filter.Value), EndOfDay(filter.Value2)),
            _ => null,
        };
        return body is null ? query : query.Where(Expression.Lambda<Func<T, bool>>(body, parameter));
    }

    /// <summary>Orders by a compile-time-known key selector — every dataset's own sort switch calls
    /// this once per known SortFieldKey case, so this is never given a dynamic/string-built key.</summary>
    public static IQueryable<T> Sort<T, TKey>(IQueryable<T> query, bool descending, Expression<Func<T, TKey>> keySelector) =>
        descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);

    private static Expression? Compare<TValue>(Expression left, TValue? value, Func<Expression, Expression, BinaryExpression> op)
        where TValue : struct =>
        value is null ? null : op(left, Expression.Constant(value, typeof(TValue?)));

    private static Expression? BuildBetween<TValue>(Expression left, TValue? lower, TValue? upper)
        where TValue : struct
    {
        Expression? lowerExpr = lower is null ? null : Expression.GreaterThanOrEqual(left, Expression.Constant(lower, typeof(TValue?)));
        Expression? upperExpr = upper is null ? null : Expression.LessThanOrEqual(left, Expression.Constant(upper, typeof(TValue?)));
        if (lowerExpr is null) return upperExpr;
        if (upperExpr is null) return lowerExpr;
        return Expression.AndAlso(lowerExpr, upperExpr);
    }

    private static Expression? BuildDayRange(Expression left, string? value, bool negate)
    {
        var start = ParseDayStart(value);
        if (start is null) return null;
        var end = start.Value.AddDays(1);
        var range = Expression.AndAlso(
            Expression.GreaterThanOrEqual(left, Expression.Constant((DateTimeOffset?)start, typeof(DateTimeOffset?))),
            Expression.LessThan(left, Expression.Constant((DateTimeOffset?)end, typeof(DateTimeOffset?))));
        return negate ? Expression.Not(range) : range;
    }

    private static Expression BuildContains<TValue>(List<TValue?> values, Expression left) where TValue : struct =>
        Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), [typeof(TValue?)], Expression.Constant(values), left);

    private static Expression BuildContains(List<string> values, Expression left) =>
        Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), [typeof(string)], Expression.Constant(values), left);

    private static List<string> SplitList(string value) =>
        value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();

    private static decimal? ParseDecimal(string? value) =>
        value is not null && System.Decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null;

    private static DateTimeOffset? ParseDayStart(string? value)
    {
        if (value is null) return null;
        return DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? new DateTimeOffset(d.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
            : null;
    }

    /// <summary>The last tick of the given calendar day — the inclusive upper bound for
    /// LessThanOrEqual/Between and the exclusive lower bound for GreaterThan.</summary>
    private static DateTimeOffset? EndOfDay(string? value) => ParseDayStart(value)?.AddDays(1).AddTicks(-1);
}
