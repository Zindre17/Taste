# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

`Taste` is a small NuGet library that persists console-app state as JSON. The public surface is two types: `Cook` (the static entry point — `Serve<T>`, `Preserve<T>`, `UseKitchen`) and `Kitchen` (the arrangements — `Pantry`, `Seasoning`, `Default`), plus one easter egg, `Savor()`, tucked away in `Taste.Savoring`.

The name is an anagram of *state* — that pun is the origin of the food metaphor, which is deliberate and pervasive from there (`TTaste` for the state value, `Preserve()` for the write, `pantry` for the directory, `dish` for the file path). Keep API and identifier naming in that vocabulary.

## Commands

```bash
dotnet build                                    # build solution
dotnet test                                     # run all tests
dotnet test --filter PreserveWritesTheTasteToThePantry  # run a single test by method name
dotnet pack Taste/Taste.csproj -c Release       # produce the NuGet package
```

`Taste.csproj` sets `TreatWarningsAsErrors` and `GenerateDocumentationFile`, so every public member needs an XML doc comment or the build fails. The test project does not have those settings.

## Architecture notes

**The entry point cannot be named `Taste`.** The namespace is `Taste`, so a non-generic `Taste` class would be `Taste.Taste`, and a consumer writing `Taste.Savor(x)` gets CS0234 — the namespace wins the lookup and the type is only reachable as `Taste.Taste.Savor(x)`. v1's `Taste<TFlavour>` got away with it only because generic arity disambiguates. That is why the entry point is `Cook`, and why the `Savor` easter egg is an extension method rather than a static one.

**`Savor` lives in `Taste.Savoring` on purpose.** It extends `TTaste` unconstrained, so a plain `using Taste;` would put `.Savor()` on every type in scope. The separate namespace keeps it opt-in.

**Per-closed-generic memory.** `Cook.Dish<TTaste>` is a private static generic class holding the served taste and whether it has been served. Each `TTaste` gets its own — there is no registry or dictionary.

**`Serve` is idempotent, and that is why there is no `Reheat`.** v2's earlier shape had a strict `Serve` that threw on the second call, plus a `Reheat` for every later call site. The strictness existed for one reason: a second `Serve` would silently ignore the `recipe` and `pantry` passed to it. Neither is a per-call argument any more — a fresh taste comes from its own property initialisers, the pantry from `UseKitchen` — so there is nothing left to ignore, and `Serve` can just hand back what it already served. That collapse removed `Reheat`, the sitting, and `Serving<T>` altogether.

**`Preserve` takes the taste by value, deliberately.** A record replaced with `with` is a different object than the one `Serve` handed out, so a `Preserve<T>()` that looked the taste up in `Dish<T>` would silently write the stale one. Do not "simplify" it into a no-argument form.

**Arrangements are settled on first contact with the pantry.** `Cook.Working` returns the kitchen and sets `hasCooked`; `UseKitchen` throws once that flag is set. `Kitchen` is init-only, so there is no way to change arrangements out from under a taste that has already been served. The cost is that there is exactly one kitchen per process.

**File location.** `Kitchen.LocateDish<T>()` builds `{entry-assembly-name}.{taste-full-name}.json`, lowercased, inside the kitchen's `Pantry`, which defaults to the directory of `Environment.ProcessPath`. The default is resolved lazily on first read, so a null `ProcessPath` only throws for callers who actually rely on it. The name depends on the *entry* assembly — under `dotnet test` that is `testhost`.

**The taste is named in full, on purpose.** `Kitchen.NameOf<T>()` uses `Type.FullName`, so `Billing.Settings` and `Display.Settings` get a dish each. Under the old simple-name scheme they shared one, silently, and it was the hardest failure here to diagnose. `NameOf` tidies three things `FullName` produces: nested types arrive as `Outer+Inner`, generic arity as `Held\`1`, and closed generics as an assembly-qualified `Held\`1[[System.Int32, ...]]` that is cut at the first `[`. The trade is that renaming a taste or moving its namespace orphans its dish — `Serve` then hands out a fresh one.

`CookTests.DishFor<T>()` mirrors this from the test side and has to be kept in step.

**`where TTaste : new()`, and why there is no recipe.** An earlier shape had `Cook.Learn<T>(Func<T>)` supplying a factory for tastes that could not make themselves. It was dropped because it could only fail when the pantry was *empty* — that is, on a fresh install, on someone else's machine, never on the developer's after their first run. The `new()` constraint moves that to CS0310 at the call site instead. The cost is the positional record form (`record Snack(string Type)`); `init` properties with initialisers keep both immutability and a first-run default, on the type where there is one place to look for it. Do not reintroduce a factory parameter or a `Learn` method — it puts the fresh-install footgun straight back.

**Read on first serve, write on `Preserve`.** `Serve` reads and deserializes, falling back to `new TTaste()` when the file is absent or deserializes to null. A corrupt file throws `JsonException` on purpose — do not "helpfully" fall back there, it would silently destroy user state.

## Tests

MSTest runs all tests in one process, so the cook's memory and the on-disk JSON persist across test methods and across runs. Two consequences:

- **One taste type per test.** Each test declares its own record so the per-closed-generic memory stays isolated. Follow that rather than reusing an existing taste.
- **One pantry for the whole run.** Arrangements are settled once per process, so `[AssemblyInitialize]` in `CookTests` calls `UseKitchen` with a GUID temp directory, and tests share it. Isolation comes from the taste type, not from the pantry — the old per-test `FreshPantry()` pattern is not possible here. `UseKitchenAfterTheCookHasStartedThrows` relies on `AssemblyInitialize` having already run.

Tests that need to read a dish off disk build the path with `DishFor<T>()`, which mirrors `Kitchen.LocateDish<T>()` from the test side. If the naming scheme changes, that helper has to change with it.

`Taste.Tests.Cupboard.Twin` exists solely to share a short name with `CookTests.Twin`, so `TastesWithTheSameShortNameGetTheirOwnDish` has a real collision to not have. Do not merge it into the test class.

## Docs

`Readme.md` lives at the repo root and is packed into the NuGet package via `Taste.csproj`. Public API changes should be reflected there.
