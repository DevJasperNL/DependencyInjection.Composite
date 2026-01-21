# DependencyInjection.Composite

A lightweight, high-performance extension for `Microsoft.Extensions.DependencyInjection` that enables Contextual Scoping.

Create nested DI scopes with overridden or additional service registrations that flow recursively through the dependency tree, without leaking into the root container or breaking modern .NET features.

## Table of Contents

- [Why DependencyInjection.Composite?](#why-dependencyinjectioncomposite)
- [What This Enables](#what-this-enables)
- [Key Features](#key-features)
- [Installation](#installation)
- [Quick Start](#quick-start)
    - [Example 1: Service Overriding](#example-1-service-overriding)
    - [Example 2: Contextual Enrichment](#example-2-contextual-enrichment)
- [Why Not Just Switch Containers?](#why-not-just-switch-containers)
- [Common Gotchas](#️-common-gotchas)
    - [The "Scope Disconnect" (Captive Dependencies)](#the-scope-disconnect-captive-dependencies)
    - [Disposal of Contextual Services](#disposal-of-contextual-services)
    - [IEnumerable<T> Ordering](#ienumerablet-ordering)

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

`DependencyInjection.Composite` allows you to "branch" your service registrations at runtime. Because the library looks at the **Context Scope** before the **Parent Provider**, it handles both overriding existing services and injecting brand-new ones seamlessly.

### Example 1: Service Overriding

Use this when you need to swap a global implementation for a specific unit of work. Because .NET DI follows a "Last-In, First-Out" resolution, your context registration becomes the primary implementation.

```csharp
using Microsoft.Extensions.DependencyInjection;

// 1. Standard Setup
var services = new ServiceCollection();
services.AddScoped<ILight, DefaultLight>();
var rootProvider = services.BuildServiceProvider();

// 2. Create a Contextual Scope with an override
using (var scope = rootProvider.CreateScope(context =>
{
    // This override exists ONLY inside this scope
    context.AddScoped<ILight, SpecialLight>();
}))
{
    // The light here will be 'SpecialLight'
    var specialLight = scope.ServiceProvider.GetRequiredService<ILight>();
    specialLight.TurnOn();
}

// 3. Back outside, the rootProvider remains untouched
// The light here will be 'DefaultLight'
var defaultLight = rootProvider.GetRequiredService<ILight>();
defaultLight.TurnOn();
```

### Example 2: Contextual Enrichment

Use this to provide "State" or "Context" to a service tree without passing it through every constructor.

```cs
using Microsoft.Extensions.DependencyInjection;

// 1. Global Setup
var services = new ServiceCollection();
services.AddScoped<ILogger, DefaultLogger>();
var rootProvider = services.BuildServiceProvider();

// 2. Start a specific job with its own context
using (var scope = rootProvider.CreateScope(context =>
{
    // Contextual Enrichment: Add a service that doesn't exist globally
    context.AddSingleton(new JobInfo { Id = 42, User = "Jasper" });
    
    // Service Overriding: Swap the global logger for a job-specific one
    context.AddScoped<ILogger, JobInfoLogger>();

    // Register a service
    context.AddScoped<IReportGenerator, PDFGenerator>();
}))
{
    // The generator now resolves with 'JobInfoLogger' and 'currentJob'
    // via standard constructor injection.
    var generator = scope.ServiceProvider.GetRequiredService<IReportGenerator>();
    await generator.GenerateAsync();
}
```

## Why Not Just Switch Containers?

| Feature | Standard Microsoft DI | 3rd-Party Containers | **DI.Composite** |
|--------|-----------------------|----------------------|------------------|
| Runtime overrides | ❌ Not supported | ✅ Supported | ✅ **Supported** |
| Registration style | `AddScoped` | Proprietary DSL | **`AddScoped`** |
| Recursive scoping | ❌ Partial | ✅ Yes | ✅ **Yes** |
| Ecosystem compatibility | ✅ Native | ⚠ Varies | ✅ **Native** |
| Dependency weight | Built-in | Heavy | **Ultra-light** |

## ⚠️ Common Gotchas

### The "Scope Disconnect" (Captive Dependencies)
One of the most important concepts to understand is that .NET DI engine caches "resolution recipes" for services registered in the root container. 

If a service is registered globally (e.g. `Processor`), it will continue to use the **global** dependencies it was originally paired with. It will not automatically use your contextual overrides.

**The Fix:**
You must register the **consuming service** inside the `CreateScope` block. This forces the DI engine to build that service using the Composite Provider, allowing it to see your new registrations.

```csharp
using (var scope = rootProvider.CreateScope(context =>
{
    context.AddScoped<ILight, SpecialLight>(); // The Override
    context.AddScoped<Processor>();            // The Consumer (Must be re-registered!)
}))
{
    var processor = scope.ServiceProvider.GetRequiredService<Processor>();
    processor.Run(); // Now uses SpecialLight
}
```

### Disposal of Contextual Services
Services registered with `context.AddScoped` or `context.AddTransient` inside the `CreateScope` block are automatically disposed of when the scope itself is disposed.

However, if you pass a pre-existing object instance using `context.AddSingleton(myExistingObject)`, the DI container **will not** dispose of it. The container only manages the lifetime of objects it creates itself.

### `IEnumerable<T>` Ordering
When resolving a collection of services (e.g., `IEnumerable<ILogger>`), `DependencyInjection.Composite` prepends contextual services to the parent services. 

The resulting order is:
1.  **Contextual Services** (Registered inside the `CreateScope` lambda)
2.  **Parent Services** (Registered in the root `ServiceCollection`)

This allows you to "Insert" a priority validator or processor at the beginning of a pipeline for a specific operation.