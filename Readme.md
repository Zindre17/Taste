# Taste
A tasteful way to persist state for console applications.

## How to taste

```csharp
// The state I want to persist
record Snack
{
    public string Type { get; set; } = "Chocolate";
}

...

// Serve the snack: whatever was kept last time, or a fresh one if the pantry is empty
Snack snack = Cook.Serve<Snack>();

// It is never null, so there is nothing to check
Console.WriteLine(snack.Type);

// Change it
snack.Type = "Liquorice";

// Keep it
Cook.Preserve(snack);
```

That is the whole thing. `Serve` anywhere, as often as you like — the cook hands back the
taste already served rather than reading the pantry again, so there is no handle to pass
around and no second call to be careful about.

**`Preserve` takes the taste itself.** That matters for records:

```csharp
var snack = Cook.Serve<Snack>();
snack = snack with { Type = "Liquorice" };   // a different object
Cook.Preserve(snack);                        // so hand it over
```

If `Preserve` looked the taste up by type instead, the line above would silently keep the
old snack.

## Tastes that cannot make themselves

`Serve<T>` makes a fresh taste when the pantry has nothing, which needs a parameterless
constructor. A positional record has none, so teach the cook first:

```csharp
record Snack(string Type);

Cook.Learn(() => new Snack("Chocolate"));

var snack = Cook.Serve<Snack>();
```

Teach before you serve. If you forget, `Serve` throws and says so — it will not quietly
hand you something you did not ask for. A recipe is only ever used when the pantry is
empty, so it runs at most once.

## Where the snacks are kept

Json file(s), one per taste, named `{your app}.{taste}.json`.

By default they go next to your executable, which is fine for a tool you run out of a
folder and wrong for one installed somewhere read-only. Build a kitchen to say otherwise:

```csharp
Cook.UseKitchen(new Kitchen
{
    Pantry = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    Seasoning = new JsonSerializerOptions { WriteIndented = true },
});
```

Do it before the first `Serve` or `Preserve`. If you leave it later, `UseKitchen` throws
rather than leaving some tastes in the old pantry and some in the new. A `Kitchen` cannot
be changed once built, and there is one kitchen per process. The directory is created for
you when you `Preserve`.

## The boring stuff

What if two tastes are named the same?
* They collide — the file name uses the *simple* type name. Two `Settings` records from
  different namespaces would share a file.

What if the file is there but is not valid Json?
* `Serve` throws. A fresh taste is deliberately not used as a fallback: state you cannot
  read is a thing to go and look at, not to quietly overwrite.

What if I never `Preserve`?
* Nothing is written. Reading happens on the first `Serve`, writing only on `Preserve`.

Is the cook thread-safe?
* No. It is meant for a console app settling its state on one thread.

## One more thing

`Taste` is an anagram of *state*, which is where all of this comes from. If `Preserve`
feels too much like housekeeping, there is another way to say it:

```csharp
using Taste.Savoring;

snack.Savor();
```

Same thing, said the way a diner would say it. It lives in its own namespace because it
extends every type, so it only turns up for people who go looking.
