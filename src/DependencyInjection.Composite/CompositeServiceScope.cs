namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A service scope that aggregates multiple child scopes into a single composite scope.
/// </summary>
/// <remarks>
/// Disposing this scope will dispose all child scopes.
/// </remarks>
public class CompositeServiceScope : IServiceScope, IAsyncDisposable
{
    /// <inheritdoc />
    public IServiceProvider ServiceProvider { get; }
    private readonly IServiceScope[] _childScopes;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeServiceScope"/> class.
    /// </summary>
    /// <param name="childScopes">The child scopes to aggregate.</param>
    public CompositeServiceScope(IServiceScope[] childScopes)
    {
        _childScopes = childScopes;
        ServiceProvider = new CompositeServiceProvider(
            childScopes.Select(s => s.ServiceProvider).ToArray()
        );
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var scope in _childScopes)
        {
            scope.Dispose();
        }
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var scope in _childScopes)
        {
            if (scope is IAsyncDisposable ad)
            {
                await ad.DisposeAsync();
            }
            else
            {
                scope.Dispose();
            }
        }
        GC.SuppressFinalize(this);
    }
}