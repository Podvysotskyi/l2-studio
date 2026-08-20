using L2.Studio.Contracts;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Repositories;

internal static class ContentDirectoryQueryPrimitives
{
    public static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);

    public static async Task<DirectoryPage<TItem>> PageAsync<TItem>(
        IQueryable<TItem> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var total = await query.LongCountAsync(cancellationToken);
        var offset = ((long)page - 1) * pageSize;
        if (offset > int.MaxValue) return new DirectoryPage<TItem>([], total, page, pageSize);
        var items = await query.Skip((int)offset).Take(pageSize).ToListAsync(cancellationToken);
        return new DirectoryPage<TItem>(items, total, page, pageSize);
    }
}
