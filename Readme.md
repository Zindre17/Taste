# Taste

A tasteful way to persist state for console applications.

## How to taste

```csharp
// The state I want to persist, and what a fresh one looks like
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

## A taste has to be able to make itself

When the pantry has nothing kept, `Serve<T>` makes a fresh taste — so `T` needs a
parameterless constructor, and says what a fresh one looks like with property
initialisers:

```csharp
record Snack
{
    public string Type { get; init; } = "Chocolate";
}
```

That is still immutable if you want it to be; use `init` and change it with `with`. What
you cannot use is the positional form, `record Snack(string Type)` — it has no
parameterless constructor, so the compiler stops you at the call site rather than letting
you find out on a machine where the pantry happens to be empty.

A `record struct` works too, as long as you write the parameterless constructor out —
without it the initialisers never run:

```csharp
record struct Snack
{
    public Snack() { }

    public string Type { get; init; } = "Chocolate";
}
```

Keeping the starting state on the taste itself means there is one place to look for it,
and no registration call to forget.

## Where the snacks are kept

Json file(s), one per taste, named `{your app}.{taste}.json` — where the taste is named in
full, namespace and all, so `myapp.myapp.settings.json` rather than `myapp.settings.json`.
Long, but two tastes never land in the same jar by accident.

By default they go next to your executable, which is fine for a tool you run out of a
folder and wrong for one installed somewhere read-only. Build a kitchen to say otherwise:

```csharp
Cook.UseKitchen(new Kitchen
{
    Pantry = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
});
```

Do it before the first `Serve` or `Preserve`. If you leave it later, `UseKitchen` throws
rather than leaving some tastes in the old pantry and some in the new. A `Kitchen` cannot
be changed once built, and there is one kitchen per process. The directory is created for
you when you `Preserve`.

## The boring stuff

What if two tastes are named the same?

* They do not collide. The file name uses the full type name, so `Billing.Settings` and
  `Display.Settings` get a jar each. Nested tastes are written with dots rather than the
  `+` the runtime uses.

What if I rename a taste, or move it to another namespace?

* Its jar is no longer found, and `Serve` hands out a fresh taste. Rename the file in
  the pantry to match if the kept state matters.

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
