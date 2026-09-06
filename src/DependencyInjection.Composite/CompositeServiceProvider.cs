namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A service provider that aggregates multiple service providers and resolves services from them in order.
/// </summary>
/// <remarks>
/// <para>Services are resolved from the providers in the order they are supplied. The first provider that can resolve a service wins.</para>
/// <para>For <see cref="IEnumerable{T}"/> requests, keyed or not, services are aggregated from all providers.</para>
/// </remarks>
/// <param name="providers">The service providers to aggregate, in priority order.</param>
public class CompositeServiceProvider(params IServiceProvider[] providers) :
    IKeyedServiceProvider, IServiceScopeFactory, IServiceProviderIsKeyedService
{
    private readonly IEnumerable<IServiceProvider> _providers = providers;

    /// <inheritdoc />
    public object? GetService(Type serviceType)
    {
        if (serviceType.IsInstanceOfType(this))
        {
            return this;
        }

        if (TryGetEnumerableElementType(serviceType, out var elementType))
        {
            return ToTypedArray(elementType, _providers.SelectMany(p => p.GetServices(elementType)));
        }

        foreach (var provider in _providers)
        {
            var service = provider.GetService(serviceType);
            if (service != null)
            {
                return service;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public bool IsService(Type serviceType) =>
        _providers.Any(p => p.GetService<IServiceProviderIsService>()?.IsService(serviceType) == true);

    /// <inheritdoc />
    public bool IsKeyedService(Type serviceType, object? serviceKey) =>
        _providers.Any(p => p.GetService<IServiceProviderIsKeyedService>()?.IsKeyedService(serviceType, serviceKey) == true);

    /// <inheritdoc />
    public IServiceScope CreateScope()
    {
        var scopes = new List<IServiceScope>();
        try
        {
            foreach (var provider in _providers)
            {
                scopes.Add(provider.CreateScope());
            }
        }
        catch
        {
            try
            {
                DisposalHelper.DisposeAll(scopes);
            }
            catch
            {
                // The original failure is the one worth surfacing.
            }
            throw;
        }

        return new CompositeServiceScope(scopes.ToArray());
    }

    /// <inheritdoc />
    public object? GetKeyedService(Type serviceType, object? serviceKey)
    {
        if (TryGetEnumerableElementType(serviceType, out var elementType))
        {
            return ToTypedArray(elementType, _providers
                .OfType<IKeyedServiceProvider>()
                .SelectMany(p => p.GetKeyedServices(elementType, serviceKey)));
        }

        foreach (var provider in _providers)
        {
            if (provider is not IKeyedServiceProvider keyed)
            {
                continue;
            }
            var service = keyed.GetKeyedService(serviceType, serviceKey);
            if (service != null)
            {
                return service;
            }
        }
        return null;
    }

    /// <inheritdoc />
    public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
        => GetKeyedService(serviceType, serviceKey) ??
           throw new InvalidOperationException($"Keyed service {serviceType} not found.");

    private static bool TryGetEnumerableElementType(Type serviceType, out Type elementType)
    {
        if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            elementType = serviceType.GetGenericArguments()[0];
            return true;
        }

        elementType = null!;
        return false;
    }

    // Callers expect the same T[] the default container returns, not a List<object>.
    private static Array ToTypedArray(Type elementType, IEnumerable<object?> services)
    {
        var list = services.ToList();
        var array = Array.CreateInstance(elementType, list.Count);
        for (var i = 0; i < list.Count; i++)
        {
            array.SetValue(list[i], i);
        }

        return array;
    }
}
