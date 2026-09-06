namespace Microsoft.Extensions.DependencyInjection;

internal sealed class LinkedContextScope(
    IServiceProvider composite,
    IServiceScope contextScope,
    ServiceProvider contextRoot,
    IServiceScope parent)
    : IServiceScope, IAsyncDisposable
{
    public IServiceProvider ServiceProvider { get; } = composite;

    // Context services may depend on parent services, so the context is torn down before the parent.
    private IDisposable[] Disposables => [contextScope, contextRoot, parent];

    public void Dispose() => DisposalHelper.DisposeAll(Disposables);

    public ValueTask DisposeAsync() => DisposalHelper.DisposeAllAsync(Disposables);
}
