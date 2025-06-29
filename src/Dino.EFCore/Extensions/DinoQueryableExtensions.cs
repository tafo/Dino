namespace Dino.EFCore.Extensions;

using Microsoft.EntityFrameworkCore;

public static class DinoQueryableExtensions
{
    public static IDinoQueryable<T> AsDinoQueryable<T>(this IQueryable<T> source) where T : class
    {
        return new DinoQueryBuilder<T>(source);
    }

    public static IDinoQueryable<T> AsDinoQueryable<T>(this DbSet<T> source) where T : class
    {
        return new DinoQueryBuilder<T>(source);
    }

    public static async Task<List<T>> ToDinoListAsync<T>(this IQueryable<T> source, string dsl, CancellationToken cancellationToken = default) where T : class
    {
        var dinoQueryable = source.AsDinoQueryable();
        return await dinoQueryable.ToListAsync(dsl, cancellationToken);
    }

    public static async Task<List<T>> ToDinoListAsync<T>(this IQueryable<T> source, string dsl, IDictionary<string, object?> parameters, CancellationToken cancellationToken = default) where T : class
    {
        var dinoQueryable = source.AsDinoQueryable();
        return await dinoQueryable.ToListAsync(dsl, parameters, cancellationToken);
    }

    public static async Task<T?> DinoFirstOrDefaultAsync<T>(this IQueryable<T> source, string dsl, CancellationToken cancellationToken = default) where T : class
    {
        var dinoQueryable = source.AsDinoQueryable();
        return await dinoQueryable.FirstOrDefaultAsync(dsl, cancellationToken);
    }

    public static async Task<T?> DinoFirstOrDefaultAsync<T>(this IQueryable<T> source, string dsl, IDictionary<string, object?> parameters, CancellationToken cancellationToken = default) where T : class
    {
        var dinoQueryable = source.AsDinoQueryable();
        return await dinoQueryable.FirstOrDefaultAsync(dsl, parameters, cancellationToken);
    }

    public static async Task<int> DinoCountAsync<T>(this IQueryable<T> source, string dsl, CancellationToken cancellationToken = default) where T : class
    {
        var dinoQueryable = source.AsDinoQueryable();
        return await dinoQueryable.CountAsync(dsl, cancellationToken);
    }

    public static async Task<int> DinoCountAsync<T>(this IQueryable<T> source, string dsl, IDictionary<string, object?> parameters, CancellationToken cancellationToken = default) where T : class
    {
        var dinoQueryable = source.AsDinoQueryable();
        return await dinoQueryable.CountAsync(dsl, parameters, cancellationToken);
    }

    public static async Task<bool> DinoAnyAsync<T>(this IQueryable<T> source, string dsl, CancellationToken cancellationToken = default) where T : class
    {
        var dinoQueryable = source.AsDinoQueryable();
        return await dinoQueryable.AnyAsync(dsl, cancellationToken);
    }

    public static async Task<bool> DinoAnyAsync<T>(this IQueryable<T> source, string dsl, IDictionary<string, object?> parameters, CancellationToken cancellationToken = default) where T : class
    {
        var dinoQueryable = source.AsDinoQueryable();
        return await dinoQueryable.AnyAsync(dsl, parameters, cancellationToken);
    }
}