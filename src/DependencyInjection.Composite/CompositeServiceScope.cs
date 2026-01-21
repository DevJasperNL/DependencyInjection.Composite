namespace Microsoft.Extensions.DependencyInjection;

public class CompositeServiceScope : IServiceScope, IAsyncDisposable
{
    public IServiceProvider ServiceProvider { get; }
    private readonly IServiceScope[] _childScopes;

    public CompositeServiceScope(IServiceScope[] childScopes)
    {
        _childScopes = childScopes;
        ServiceProvider = new CompositeServiceProvider(
            childScopes.Select(s => s.ServiceProvider).ToArray()
        );
    }

    public void Dispose()
    {
        foreach (var scope in _childScopes)
        {
            scope.Dispose();
        }
        GC.SuppressFinalize(this);
    }

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