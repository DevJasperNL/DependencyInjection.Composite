# DependencyInjection.Composite

A lightweight, high-performance extension for `Microsoft.Extensions.DependencyInjection` that enables Contextual Scoping.

Create nested DI scopes with overridden or additional service registrations that flow recursively through the dependency tree, without leaking into the root container or breaking modern .NET features.

## Why DependencyInjection.Composite?

The standard .NET IServiceProvider is immutable once built. While you can create a scope, you cannot change registrations for that specific unit of work. This library provides a Composite Service Provider pattern that allows you to branch the container at runtime.

## Key Features

- **Recursive Overrides**: Overridden services persist through sub-scopes created via `IServiceScopeFactory`.
- **Leak-Proof Disposal**: Uses a `LinkedContextScope` to ensure that both the context provider and temporary parent-scoped services are cleaned up correctly.
- **Modern .NET Support**: Full implementation of `IKeyedServiceProvider`, `IServiceProviderIsService`, and `IAsyncDisposable`.
- **Transparent Integration**: Uses the standard `Microsoft.Extensions.DependencyInjection` namespace so the extensions are immediately discoverable.

## Installation

```bash
dotnet add package DependencyInjection.Composite
```
## Quick Start
Imagine you have a global ILight service, but for a specific pipeline run, you need to use a SpecialLight.
```csharp
using Microsoft.Extensions.DependencyInjection;

// 1. Standard Setup
var services = new ServiceCollection();
services.AddScoped<ILight, DefaultLight>();
services.AddScoped<Processor>();
var rootProvider = services.BuildServiceProvider();

// 2. Create a Contextual Scope with an override
using (var scope = rootProvider.CreateScope(context =>
{
    // This override exists ONLY inside this scope
    context.AddScoped<ILight, SpecialLight>();
}))
{
    // The Processor resolved here will receive 'SpecialLight'
    var processor = scope.ServiceProvider.GetRequiredService<Processor>();
    processor.Run();
}

// 3. Back outside, the rootProvider remains untouched
```
