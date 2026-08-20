# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

`Taste` is a small NuGet library that persists console-app state as JSON. The public surface is two types: `Cook` (the static entry point — `Serve<T>`, `Preserve<T>`, `Learn<T>`, `UseKitchen`) and `Kitchen` (the arrangements — `Pantry`, `Seasoning`, `Default`), plus one easter egg, `Savor()`, tucked away in `Taste.Savoring`.

The name is an anagram of *state* — that pun is the origin of the food metaphor, which is deliberate and pervasive from there (`TTaste` for the state value, `Preserve()` for the write, `recipe` for the factory, `dish` for the file path). Keep API and identifier naming in that vocabulary.

## Commands

```bash
dotnet build                                    # build solution
dotnet test                                     # run all tests
dotnet test --filter LearnTeachesTheCookToMakeIt  # run a single test by method name
dotnet pack Taste/Taste.csproj -c Release       # produce the NuGet package
```

`Taste.csproj` sets `TreatWarningsAsErrors` and `GenerateDocumentationFile`, so every public member needs an XML doc comment or the build fails. The test project does not have those settings.

## Architecture notes

**The entry point cannot be named `Taste`.** The namespace is `Taste`, so a non-generic `Taste` class would be `Taste.Taste`, and a consumer writing `Taste.Savor(x)` gets CS0234 — the namespace wins the lookup and the type is only reachable as `Taste.Taste.Savor(x)`. v1's `Taste<TFlavour>` got away with it only because generic arity disambiguates. That is why the entry point is `Cook`, and why the `Savor` easter egg is an extension method rather than a static one.

**`Savor` lives in `Taste.Savoring` on purpose.** It extends `TTaste` unconstrained, so a plain `using Taste;` would put `.Savor()` on every type in scope. The separate namespace keeps it opt-in.

**Per-closed-generic memory.** `Cook.Dish<TTaste>` is a private static generic class holding the learned recipe, the served taste, and whether it has been served. Each `TTaste` gets its own — there is no registry or dictionary.

**`Serve` is idempotent, and that is why there is no `Reheat`.** v2's earlier shape had a strict `Serve` that threw on the second call, plus a `Reheat` for every later call site. The strictness existed for one reason: a second `Serve` would silently ignore the `recipe` and `pantry` passed to it. Neither is a per-call argument any more — the recipe comes from `Learn`, the pantry from `UseKitchen` — so there is nothing left to ignore, and `Serve` can just hand back what it already served. That collapse removed `Reheat`, the sitting, and `Serving<T>` altogether.

**`Preserve` takes the taste by value, deliberately.** A record replaced with `with` is a different object than the one `Serve` handed out, so a `Preserve<T>()` that looked the taste up in `Dish<T>` would silently write the stale one. Do not "simplify" it into a no-argument form.

**Arrangements are settled on first contact with the pantry.** `Cook.Working` returns the kitchen and sets `hasCooked`; `UseKitchen` throws once that flag is set. `Kitchen` is init-only, so there is no way to change arrangements out from under a taste that has already been served. The cost is that there is exactly one kitchen per process.

**File location.** `Kitchen.LocateDish<T>()` builds `{entry-assembly-name}.{taste-type-name}.json` (both `ToLowerInvariant`) inside the kitchen's `Pantry`, which defaults to the directory of `Environment.ProcessPath`. The default is resolved lazily on first read, so a null `ProcessPath` only throws for callers who actually rely on it. Two things to know: the name depends on the *entry* assembly (under `dotnet test` that is the test host), and two `TTaste` types with the same simple name collide on one file.

**Making a taste from scratch.** `Cook.Make<T>()` uses the learned recipe if there is one, else `Activator.CreateInstance<T>()`. `Serve<T>` deliberately has **no** `where TTaste : new()` constraint — that constraint is checked at compile time and would make `Serve<Snack>()` uncompilable for a positional record even after `Learn` had taught the cook how to make one. The trade is that a missing recipe is a runtime failure, so the `MissingMethodException` is caught and rethrown as an `InvalidOperationException` naming `Cook.Learn`. Keep that message pointed at the fix.

**Read on first serve, write on `Preserve`.** `Serve` reads and deserializes, falling back to `Make<T>()` when the file is absent or deserializes to null. A corrupt file throws `JsonException` on purpose — do not "helpfully" fall back there, it would silently destroy user state.

## Tests

MSTest runs all tests in one process, so the cook's memory and the on-disk JSON persist across test methods and across runs. Two consequences:

- **One taste type per test.** Each test declares its own record so the per-closed-generic memory stays isolated. Follow that rather than reusing an existing taste.
- **One pantry for the whole run.** Arrangements are settled once per process, so `[AssemblyInitialize]` in `CookTests` calls `UseKitchen` with a GUID temp directory, and tests share it. Isolation comes from the taste type, not from the pantry — the old per-test `FreshPantry()` pattern is not possible here. `UseKitchenAfterTheCookHasStartedThrows` relies on `AssemblyInitialize` having already run.

Tests that need to read a dish off disk build the path with `DishFor<T>()`, which mirrors `Kitchen.LocateDish<T>()` from the test side. If the naming scheme changes, that helper has to change with it.

## Docs

`Readme.md` lives at the repo root and is packed into the NuGet package via `Taste.csproj`. Public API changes should be reflected there.
