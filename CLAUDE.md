# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

`Taste` is a small NuGet library that persists console-app state as JSON. The public surface is three types: `Kitchen` (static entry point — `Serve<T>`, `Reheat<T>`), `Serving<T>` (the handle — `Flavour`, `Savor()`, `IDisposable`), and `Pantry` (where files live — `Location`).

The name is an anagram of *state* — that pun is the origin of the food metaphor, which is deliberate and pervasive from there (`Flavour` for the state value, `Savor()` for the write, `recipe` for the factory, `dish` for the file path). Keep API and identifier naming in that vocabulary.

## Commands

```bash
dotnet build                                    # build solution
dotnet test                                     # run all tests
dotnet test --filter DisposingSavors            # run a single test by method name
dotnet pack Taste/Taste.csproj -c Release       # produce the NuGet package
```

`Taste.csproj` sets `TreatWarningsAsErrors` and `GenerateDocumentationFile`, so every public member needs an XML doc comment or the build fails. The test project does not have those settings.

## Architecture notes

**The static entry point cannot be named `Taste`.** The namespace is `Taste`, so a non-generic `Taste` class would be `Taste.Taste` and consumers hit CS0118 ("is a namespace but is used like a type"). v1's `Taste<TFlavour>` got away with it only because generic arity disambiguates — namespaces can't be generic. That constraint is why the entry point is `Kitchen`.

**Per-closed-generic singleton.** `Serving<T>.current` is a static field on a generic type, so each `TFlavour` gets its own serving. That is the mechanism behind "different flavours coexist" — there is no registry or dictionary. Consequence: `Serve<T>` consults its `recipe` and `pantry` arguments *only* on the first serving; later calls hand back the cached instance and ignore both.

**File location.** `Pantry.LocateDish<T>()` builds `{entry-assembly-name}.{flavour-type-name}.json` (both `ToLowerInvariant`) inside the explicit `pantry` argument, else `Pantry.Location`, else the directory of `Environment.ProcessPath`. The default is resolved lazily, so a null `ProcessPath` only throws for callers who actually rely on it. Two things to know: the name depends on the *entry* assembly (under `dotnet test` that is the test host), and two `TFlavour` types with the same simple name collide on one file.

**Load on serve, write on `Savor()`/`Dispose()`.** `DishUp` reads and deserializes, falling back to the recipe when the file is absent or deserializes to null. A corrupt file throws `JsonException` on purpose — do not "helpfully" fall back to the recipe there, it would silently destroy user state. `Dispose()` savors, then clears the singleton so `Reheat` throws and the next `Serve` re-reads.

**Tests share process state.** MSTest runs all tests in one process, so singletons and on-disk JSON persist across test methods and across runs. Each test declares its own record type to stay isolated — follow that, rather than reusing an existing flavour. `Unserved` exists solely so `CannotReheatBeforeYouServe` can observe a never-served type; never call `Serve<Unserved>()`. Tests that touch the filesystem use `FreshPantry()` (a GUID temp directory) so they do not depend on run order.

## Docs

`Readme.md` lives at the repo root and is packed into the NuGet package via `Taste.csproj`. Public API changes should be reflected there.
