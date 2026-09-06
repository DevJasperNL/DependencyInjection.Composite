namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A service scope that aggregates multiple child scopes into a single composite scope.
/// </summary>
/// <remarks>
/// Disposing this scope will dispose all child scopes, even if one of them throws during disposal.
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
    public void Dispose() => DisposalHelper.DisposeAll(_childScopes);

    /// <inheritdoc />
    public ValueTask DisposeAsync() => DisposalHelper.DisposeAllAsync(_childScopes);
}
