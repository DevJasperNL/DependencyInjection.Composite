using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.Composite.Tests;

[TestClass]
public sealed class CompositeServiceProviderTests
{
    [TestMethod]
    public void GetService_ReturnsServiceFromFirstProvider()
    {
        var services1 = new ServiceCollection();
        services1.AddSingleton<ITestService, TestServiceA>();
        var provider1 = services1.BuildServiceProvider();

        var services2 = new ServiceCollection();
        services2.AddSingleton<ITestService, TestServiceB>();
        var provider2 = services2.BuildServiceProvider();

        var composite = new CompositeServiceProvider(provider1, provider2);

        var result = composite.GetService<ITestService>();

        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<TestServiceA>(result);
    }

    [TestMethod]
    public void GetService_FallsBackToSecondProvider()
    {
        var services1 = new ServiceCollection();
        var provider1 = services1.BuildServiceProvider();

        var services2 = new ServiceCollection();
        services2.AddSingleton<ITestService, TestServiceB>();
        var provider2 = services2.BuildServiceProvider();

        var composite = new CompositeServiceProvider(provider1, provider2);

        var result = composite.GetService<ITestService>();

        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<TestServiceB>(result);
    }

    [TestMethod]
    public void GetService_ReturnsNullWhenNotRegistered()
    {
        var services1 = new ServiceCollection();
        var provider1 = services1.BuildServiceProvider();

        var composite = new CompositeServiceProvider(provider1);

        var result = composite.GetService<ITestService>();

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetService_ReturnsItself_ForCompatibleTypes()
    {
        var composite = new CompositeServiceProvider();

        var result = composite.GetService<CompositeServiceProvider>();

        Assert.AreSame(composite, result);
    }

    [TestMethod]
    public void GetService_ReturnsAllServicesFromAllProviders_ForIEnumerable()
    {
        var services1 = new ServiceCollection();
        services1.AddSingleton<ITestService, TestServiceA>();
        var provider1 = services1.BuildServiceProvider();

        var services2 = new ServiceCollection();
        services2.AddSingleton<ITestService, TestServiceB>();
        var provider2 = services2.BuildServiceProvider();

        var composite = new CompositeServiceProvider(provider1, provider2);

        var results = composite.GetService<IEnumerable<ITestService>>();

        Assert.IsNotNull(results);
        var list = results.ToList();
        Assert.HasCount(2, list);
        Assert.IsTrue(list.Any(s => s is TestServiceA));
        Assert.IsTrue(list.Any(s => s is TestServiceB));
    }

    [TestMethod]
    public void GetKeyedService_ReturnsKeyedServiceFromProvider()
    {
        var services1 = new ServiceCollection();
        services1.AddKeyedSingleton<ITestService, TestServiceA>("keyA");
        services1.AddKeyedSingleton<ITestService, TestServiceB>("keyB");
        var provider1 = services1.BuildServiceProvider();

        var composite = new CompositeServiceProvider(provider1);

        var resultA = composite.GetKeyedService(typeof(ITestService), "keyA");
        var resultB = composite.GetKeyedService(typeof(ITestService), "keyB");

        Assert.IsNotNull(resultA);
        Assert.IsInstanceOfType<TestServiceA>(resultA);
        Assert.IsNotNull(resultB);
        Assert.IsInstanceOfType<TestServiceB>(resultB);
    }

    [TestMethod]
    public void GetRequiredKeyedService_ThrowsWhenNotFound()
    {
        var services1 = new ServiceCollection();
        var provider1 = services1.BuildServiceProvider();

        var composite = new CompositeServiceProvider(provider1);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            composite.GetRequiredKeyedService(typeof(ITestService), "nonexistent"));
    }

    [TestMethod]
    public void IsService_ReturnsTrueForRegisteredService()
    {
        var services1 = new ServiceCollection();
        services1.AddSingleton<ITestService, TestServiceA>();
        var provider1 = services1.BuildServiceProvider();

        var composite = new CompositeServiceProvider(provider1);

        var result = composite.IsService(typeof(ITestService));

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsService_ReturnsFalseForUnregisteredService()
    {
        var services1 = new ServiceCollection();
        var provider1 = services1.BuildServiceProvider();

        var composite = new CompositeServiceProvider(provider1);

        var result = composite.IsService(typeof(ITestService));

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void GetKeyedService_FallsBackToSecondProvider()
    {
        var services1 = new ServiceCollection();
        var provider1 = services1.BuildServiceProvider();

        var services2 = new ServiceCollection();
        services2.AddKeyedSingleton<ITestService, TestServiceB>("key");
        var provider2 = services2.BuildServiceProvider();

        var composite = new CompositeServiceProvider(provider1, provider2);

        var result = composite.GetKeyedService(typeof(ITestService), "key");

        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<TestServiceB>(result);
    }

    [TestMethod]
    public void GetKeyedService_ReturnsNullWhenNotFound()
    {
        var services1 = new ServiceCollection();
        var provider1 = services1.BuildServiceProvider();

        var composite = new CompositeServiceProvider(provider1);

        var result = composite.GetKeyedService(typeof(ITestService), "nonexistent");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetService_ReturnsEmptyEnumerable_WhenNoServicesRegistered()
    {
        var services1 = new ServiceCollection();
        var provider1 = services1.BuildServiceProvider();

        var composite = new CompositeServiceProvider(provider1);

        var results = composite.GetService<IEnumerable<ITestService>>();

        Assert.IsNotNull(results);
        Assert.HasCount(0, results);
    }

    [TestMethod]
    public void CreateScope_ReturnsCompositeServiceScope()
    {
        var services1 = new ServiceCollection();
        services1.AddScoped<ITestService, TestServiceA>();
        var provider1 = services1.BuildServiceProvider();

        var composite = new CompositeServiceProvider(provider1);

        using var scope = composite.CreateScope();

        Assert.IsNotNull(scope);
        Assert.IsInstanceOfType<CompositeServiceScope>(scope);
    }
}

[TestClass]
public sealed class CompositeServiceScopeTests
{
    [TestMethod]
    public void ServiceProvider_ResolvesServicesFromChildScopes()
    {
        var services = new ServiceCollection();
        services.AddScoped<ITestService, TestServiceA>();
        var provider = services.BuildServiceProvider();

        var composite = new CompositeServiceProvider(provider);

        using var scope = composite.CreateScope();

        var service = scope.ServiceProvider.GetService<ITestService>();

        Assert.IsNotNull(service);
        Assert.IsInstanceOfType<TestServiceA>(service);
    }

    [TestMethod]
    public void ScopedService_ReturnsSameInstanceWithinScope()
    {
        var services = new ServiceCollection();
        services.AddScoped<ITestService, TestServiceA>();
        var provider = services.BuildServiceProvider();

        var composite = new CompositeServiceProvider(provider);

        using var scope = composite.CreateScope();

        var service1 = scope.ServiceProvider.GetService<ITestService>();
        var service2 = scope.ServiceProvider.GetService<ITestService>();

        Assert.AreSame(service1, service2);
    }

    [TestMethod]
    public void ScopedService_ReturnsDifferentInstancesAcrossScopes()
    {
        var services = new ServiceCollection();
        services.AddScoped<ITestService, TestServiceA>();
        var provider = services.BuildServiceProvider();

        var composite = new CompositeServiceProvider(provider);

        using var scope1 = composite.CreateScope();
        using var scope2 = composite.CreateScope();

        var service1 = scope1.ServiceProvider.GetService<ITestService>();
        var service2 = scope2.ServiceProvider.GetService<ITestService>();

        Assert.AreNotSame(service1, service2);
    }

    [TestMethod]
    public void Dispose_DisposesAllChildScopes()
    {
        var services = new ServiceCollection();
        services.AddScoped<DisposableService>();
        var provider = services.BuildServiceProvider();

        var composite = new CompositeServiceProvider(provider);

        var scope = composite.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<DisposableService>();

        scope.Dispose();

        Assert.IsTrue(service.IsDisposed);
    }

    [TestMethod]
    public async Task DisposeAsync_DisposesAllChildScopes()
    {
        var services = new ServiceCollection();
        services.AddScoped<AsyncDisposableService>();
        var provider = services.BuildServiceProvider();

        var composite = new CompositeServiceProvider(provider);

        var scope = composite.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<AsyncDisposableService>();

        await ((IAsyncDisposable)scope).DisposeAsync();

        Assert.IsTrue(service.IsDisposed);
    }
}

