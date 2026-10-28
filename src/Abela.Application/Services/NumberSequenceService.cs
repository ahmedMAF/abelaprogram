using Abela.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abela.Application.Services;

public class NumberSequenceService
{
    private readonly AppDbContext _db;

    public NumberSequenceService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string> NextAsync<T>(string prefix, Func<AppDbContext, IQueryable<T>> query, Func<T, string> numberSelector, int padding = 6) where T : class
    {
        var numbers = await query(_db).ToListAsync();
        int max = 0;
        foreach (var item in numbers)
        {
            var num = numberSelector(item);
            if (num.StartsWith(prefix) && int.TryParse(num[prefix.Length..], out var n) && n > max)
                max = n;
        }
        return prefix + (max + 1).ToString().PadLeft(padding, '0');
    }
}
