using LinkUp.BuildingBlocks.Common.Pagination;
using Microsoft.EntityFrameworkCore;

namespace LinkUp.BuildingBlocks.Infrastructure.Extensions;

public static class PaginationExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return PagedResult<T>.Create(items, totalCount, pageNumber, pageSize);
    }

    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PagedRequest request,
        CancellationToken ct = default) =>
        await query.ToPagedResultAsync(request.PageNumber, request.PageSize, ct);
}
