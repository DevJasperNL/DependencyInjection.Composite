using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.Composite.Tests;

public interface ITestService;
public interface IOtherService;

public class TestServiceA : ITestService;
public class TestServiceB : ITestService;
public class OtherService : IOtherService;

public class DisposableService : IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        IsDisposed = true;
    }
}

public class AsyncDisposableService : IAsyncDisposable
{
    public bool IsDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}

public class ThrowingDisposableService : IDisposable
{
    public void Dispose() => throw new InvalidOperationException("Dispose failed");
}

public class ThrowingAsyncDisposableService : IAsyncDisposable
{
    public ValueTask DisposeAsync() => throw new InvalidOperationException("DisposeAsync failed");
}

public sealed class RecordingScope : IServiceScope
{
    public bool IsDisposed { get; private set; }
    public IServiceProvider ServiceProvider { get; } = new ServiceCollection().BuildServiceProvider();
    public void Dispose() => IsDisposed = true;
}

public sealed class RecordingScopeProvider : IServiceProvider, IServiceScopeFactory
{
    public List<RecordingScope> Scopes { get; } = [];

    public object? GetService(Type serviceType) =>
        serviceType == typeof(IServiceScopeFactory) ? this : null;

    public IServiceScope CreateScope()
    {
        var scope = new RecordingScope();
        Scopes.Add(scope);
        return scope;
    }
}

public sealed class FailingScopeProvider : IServiceProvider, IServiceScopeFactory
{
    public object? GetService(Type serviceType) =>
        serviceType == typeof(IServiceScopeFactory) ? this : null;

    public IServiceScope CreateScope() => throw new InvalidOperationException("CreateScope failed");
}
