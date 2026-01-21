# DependencyInjection.Composite

A lightweight, high-performance extension for `Microsoft.Extensions.DependencyInjection` that enables Contextual Scoping.

Create nested DI scopes with overridden or additional service registrations that flow recursively through the dependency tree, without leaking into the root container or breaking modern .NET features.

## Why DependencyInjection.Composite?

The built-in Microsoft.Extensions.DependencyInjection container is intentionally immutable. Once a ServiceProvider is built, its registrations are fixed. While scopes exist, they cannot alter registrations for a specific unit of work.

This creates a practical gap when building real systems:
- Multi-tenant request handling
- Per-pipeline or per-job overrides
- Feature-flagged or dynamically composed workflows

Historically, solving these problems meant abandoning the native container in favor of heavier third-party alternatives (Autofac, etc.) just to access features like `BeginLifetimeScope(Action<ContainerBuilder>)`. Switching to a "Full-Stack" DI provider often introduced new abstractions, proprietary registration DSLs, and performance trade-offs that felt overkill for a single use case.

**DependencyInjection.Composite** bridges this gap by allowing you to branch the container at runtime while remaining fully inside the Microsoft DI ecosystem.

It does this by introducing a composite service provider: a contextual layer that can override or extend registrations for a scope, while transparently delegating everything else to its parent.

## What This Enables

- Override services for a single unit of work without touching the root container
- Ensure overrides flow recursively through all nested scopes
- Preserve standard DI semantics (Scoped, Singleton, disposal, async disposal)
- Keep using ServiceCollection, AddScoped, and existing registrations unchanged

## Key Features

- **Contextual Overrides**: Add or replace registrations for a specific scope at runtime.
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

## Why Not Just Switch Containers?

| Feature | Standard Microsoft DI | 3rd-Party Containers | **DI.Composite** |
|--------|-----------------------|----------------------|------------------|
| Runtime overrides | ❌ Not supported | ✅ Supported | ✅ **Supported** |
| Registration style | `AddScoped` | Proprietary DSL | **`AddScoped`** |
| Recursive scoping | ❌ Partial | ✅ Yes | ✅ **Yes** |
| Ecosystem compatibility | ✅ Native | ⚠ Varies | ✅ **Native** |
| Dependency weight | Built-in | Heavy | **Ultra-light** |
