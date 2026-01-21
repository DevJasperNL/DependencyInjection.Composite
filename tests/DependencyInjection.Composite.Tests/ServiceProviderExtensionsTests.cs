using DependencyInjection.Composite.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.Composite.Tests;

[TestClass]
public sealed class ServiceProviderExtensionsTests
{
    [TestMethod]
    public void CreateScope_WithContextBuilder_AddsContextServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITestService, TestServiceA>();
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope(ctx =>
        {
            ctx.AddSingleton<IOtherService, OtherService>();
        });

        var testService = scope.ServiceProvider.GetService<ITestService>();
        var otherService = scope.ServiceProvider.GetService<IOtherService>();

        Assert.IsNotNull(testService);
        Assert.IsNotNull(otherService);
    }

    [TestMethod]
    public void CreateScope_WithContextBuilder_ContextServicesTakePriority()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITestService, TestServiceA>();
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope(ctx =>
        {
            ctx.AddSingleton<ITestService, TestServiceB>();
        });

        var service = scope.ServiceProvider.GetService<ITestService>();

        Assert.IsNotNull(service);
        Assert.IsInstanceOfType<TestServiceB>(service);
    }

    [TestMethod]
    public void CreateScope_WithServiceProviderOptions_PassesOptionsToContext()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var options = new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = true
        };

        using var scope = provider.CreateScope(ctx =>
        {
            ctx.AddSingleton<ITestService, TestServiceA>();
        }, options);

        var service = scope.ServiceProvider.GetService<ITestService>();

        Assert.IsNotNull(service);
    }

    [TestMethod]
    public void CreateScope_ContextCanAccessParentScopedServices()
    {
        var services = new ServiceCollection();
        services.AddScoped<ITestService, TestServiceA>();
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope(ctx =>
        {
            ctx.AddSingleton<IOtherService, OtherService>();
        });

        var testService = scope.ServiceProvider.GetService<ITestService>();
        var otherService = scope.ServiceProvider.GetService<IOtherService>();

        Assert.IsNotNull(testService);
        Assert.IsNotNull(otherService);
    }

    [TestMethod]
    public void CreateScope_WithValidateScopes_PassesOptionToContext()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope(ctx =>
        {
            ctx.AddSingleton<ITestService, TestServiceA>();
        }, validateScopes: true);

        var service = scope.ServiceProvider.GetService<ITestService>();

        Assert.IsNotNull(service);
    }

    [TestMethod]
    public void CreateScope_DisposesParentScope_WhenContextBuilderThrows()
    {
        var services = new ServiceCollection();
        services.AddScoped<DisposableService>();
        var provider = services.BuildServiceProvider();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            provider.CreateScope(_ => throw new InvalidOperationException("Test exception"));
        });
    }

    [TestMethod]
    public void CreateScope_Dispose_DisposesContextAndParentScope()
    {
        var services = new ServiceCollection();
        services.AddScoped<DisposableService>();
        var provider = services.BuildServiceProvider();

        DisposableService? contextService = null;
        var scope = provider.CreateScope(ctx =>
        {
            ctx.AddScoped<DisposableService>();
        });

        contextService = scope.ServiceProvider.GetRequiredService<DisposableService>();
        scope.Dispose();

        Assert.IsTrue(contextService.IsDisposed);
    }

    [TestMethod]
    public async Task CreateScope_DisposeAsync_DisposesContextAndParentScope()
    {
        var services = new ServiceCollection();
        services.AddScoped<AsyncDisposableService>();
        var provider = services.BuildServiceProvider();

        var scope = provider.CreateScope(ctx =>
        {
            ctx.AddScoped<AsyncDisposableService>();
        });

        var contextService = scope.ServiceProvider.GetRequiredService<AsyncDisposableService>();
        await ((IAsyncDisposable)scope).DisposeAsync();

        Assert.IsTrue(contextService.IsDisposed);
    }

    [TestMethod]
    public void CreateScope_CannotResolveServices_AfterDispose()
    {
        var services = new ServiceCollection();
        services.AddScoped<ITestService, TestServiceA>();
        var provider = services.BuildServiceProvider();

        var scope = provider.CreateScope(ctx =>
        {
            ctx.AddScoped<IOtherService, OtherService>();
        });

        scope.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
            scope.ServiceProvider.GetService<ITestService>());
    }
}