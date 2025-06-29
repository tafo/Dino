namespace Dino.EFCore;

public interface IDinoQueryable<T> where T : class
{
    IQueryable<T> BuildQuery(string dsl);
    IQueryable<T> BuildQuery(string dsl, IDictionary<string, object?> parameters);
    Task<List<T>> ToListAsync(string dsl, CancellationToken cancellationToken = default);
    Task<List<T>> ToListAsync(string dsl, IDictionary<string, object?> parameters, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(string dsl, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(string dsl, IDictionary<string, object?> parameters, CancellationToken cancellationToken = default);
    Task<int> CountAsync(string dsl, CancellationToken cancellationToken = default);
    Task<int> CountAsync(string dsl, IDictionary<string, object?> parameters, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(string dsl, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(string dsl, IDictionary<string, object?> parameters, CancellationToken cancellationToken = default);
}