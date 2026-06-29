using LinkUp.BuildingBlocks.Common.Entities;

namespace LinkUp.BuildingBlocks.Infrastructure.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> NotDeleted<T>(this IQueryable<T> query)
        where T : AuditableEntity =>
        query.Where(e => !e.IsDeleted);

    public static IQueryable<T> OrderByCreatedDesc<T>(this IQueryable<T> query)
        where T : AuditableEntity =>
        query.OrderByDescending(e => e.CreatedAt);
}
