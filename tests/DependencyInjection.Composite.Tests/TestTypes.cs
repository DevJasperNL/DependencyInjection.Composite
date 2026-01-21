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
