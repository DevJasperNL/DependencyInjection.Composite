# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

`DependencyInjection.Composite` is a small NuGet library (single project, .NET 10) that adds runtime "contextual scopes" to `Microsoft.Extensions.DependencyInjection`. Users call `rootProvider.CreateScope(context => context.AddScoped<...>())` to get a scope whose registrations override or extend the parent without touching the root container.

## Commands

```bash
dotnet build                                   # Debug build of the solution (DependencyInjection.Composite.slnx)
dotnet build -c Release -p:TreatWarningsAsErrors=true   # what CI runs; warnings fail the build
dotnet test                                    # run all tests (MSTest 4, VSTest runner)
dotnet test --filter "FullyQualifiedName~ServiceProviderExtensionsTests"        # one test class
dotnet test --filter "FullyQualifiedName~GetService_ReturnsNullWhenNotRegistered"  # one test method
dotnet pack -c Release -p:PackageVersion=1.2.3 -p:Version=1.2.3                 # how the release workflow packs
```

There is no lint step beyond the compiler. CI treats warnings as errors, so keep XML doc comments on all public members (`GenerateDocumentationFile` is on) and keep nullable annotations clean.

## Architecture

All library code lives in `src/DependencyInjection.Composite/` and is deliberately placed in the `Microsoft.Extensions.DependencyInjection` namespace (`RootNamespace` in the csproj, plus an `IDE0130` suppression in the extensions file) so the `CreateScope` overloads show up as soon as a consumer has the standard DI `using`.

Three pieces work together:

- `ServiceProviderExtensions.CreateScope(Action<IServiceCollection>, ...)` is the public entry point. It creates a normal scope on the parent, builds a brand-new `ServiceProvider` from the context `IServiceCollection`, and wraps both in a `CompositeServiceProvider` with the **context provider first**. Order matters: first provider wins, which is what gives "last registration wins" semantics for overrides and puts context services before parent services in `IEnumerable<T>` results.
- `CompositeServiceProvider` resolves across an ordered list of providers. It implements `IKeyedServiceProvider`, `IServiceProviderIsService`, and `IServiceScopeFactory`. `IEnumerable<T>` requests are special-cased to aggregate from every provider into a `T[]`. `CreateScope()` on it creates a child scope on *each* inner provider and wraps them in `CompositeServiceScope`; this is how overrides flow recursively into sub-scopes created via `IServiceScopeFactory`.
- `LinkedContextScope` (internal) is what `CreateScope` actually returns. It owns both the context `ServiceProvider` and the parent `IServiceScope` and disposes both (sync and async) so nothing leaks.

The known limitation to keep in mind when changing resolution: services registered in the *root* container are built with the root's cached call sites and will not see contextual overrides. Consumers must re-register the consuming service inside the context. The README's "Common Gotchas" section documents this and the `IEnumerable<T>` ordering; keep it in sync if behavior changes.

## Tests

`tests/DependencyInjection.Composite.Tests/` uses MSTest with method-level parallelization enabled in `MSTestSettings.cs`, so tests must not share mutable static state. Shared fake service types live in `TestTypes.cs`. Tests are split by unit under test: `CompositeServiceProviderTests.cs` for the provider/scope classes and `ServiceProviderExtensionsTests.cs` for the `CreateScope` extension behavior (override precedence, recursion into sub-scopes, disposal).

## Releases and PRs

- Every PR must carry at least one label starting with `pr:` (enforced by `check-pr-labels.yml`). Valid ones are listed in `.github/release-drafter.yml`: `pr: breaking change`, `pr: new-feature`, `pr: enhancement`, `pr: bugfix`, `pr: documentation`, `pr: dependency-update`.
- Versions are not stored in the csproj. Release Drafter creates a tag from PR labels, and `nuget-publish.yml` packs with the latest git tag as the version and pushes to NuGet when a GitHub release is published.
- `README.md` is packed into the NuGet package, so keep it accurate for consumers.
