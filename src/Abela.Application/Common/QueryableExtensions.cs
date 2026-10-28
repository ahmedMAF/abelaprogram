using Microsoft.EntityFrameworkCore;

namespace Abela.Application.Common;

/// <summary>
/// SQLite's EF Core provider cannot translate Sum() over decimal columns to SQL
/// (decimal has no native SQLite type), so these helpers materialize the query
/// first and aggregate client-side.
/// </summary>
public static class QueryableExtensions
{
    public static async Task<decimal> SumDecimalAsync<T>(this IQueryable<T> source, Func<T, decimal> selector)
    {
        var values = await source.ToListAsync();
        return values.Sum(selector);
    }
}
