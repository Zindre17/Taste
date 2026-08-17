# Taste
A tasteful way to persist state for console applications.

## How to taste

```csharp
// The state I want to persist
record Snack(string Type);

...

// Serve the snack: whatever was kept last time, or the recipe if the pantry is empty
Serving<Snack> serving = Kitchen.Serve(() => new Snack("Chocolate"));

// The flavour is never null, so there is nothing to check
Console.WriteLine(serving.Flavour.Type);

// Set a new snack
serving.Flavour = new Snack("Liquorice");

// Persist it
serving.Savor();

// Somewhere else entirely, take up the same serving
Serving<Snack> sameServing = Kitchen.Reheat<Snack>();
```

`Serve` reads the pantry the first time a flavour is asked for, and hands back that same
serving every time after. The recipe only runs when there is nothing kept — on later
calls it is not even looked at.

## Where the snacks are kept

Json file(s), one per flavour, named `{your app}.{flavour}.json`.

By default they go next to your executable, which is fine for a tool you run out of a
folder and wrong for one installed somewhere read-only. Two ways to say otherwise:

```csharp
// For everything
Pantry.Location = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

// Or just for this flavour
var serving = Kitchen.Serve(() => new Snack("Chocolate"), "/some/other/cupboard");
```

Either way, say it before the first `Serve` of that flavour — a serving keeps the pantry
it was dished up from. The directory is created for you when you `Savor`.

## Savoring by disposing

`Serving<T>` is `IDisposable`, so the whole thing can be a scope:

```csharp
using var serving = Kitchen.Serve(() => new Snack("Chocolate"));
serving.Flavour = new Snack("Liquorice");
// savored on the way out
```

**Be deliberate about this one.** Disposing writes to the pantry, and `using` disposes on
*every* exit from the scope — including one caused by an exception. A flavour that was
half-updated when something threw gets savored exactly the same as one you finished
happily.

So `using` is the right shape when "whatever this looks like at the end of the scope is
worth keeping" is genuinely what you mean — a settings file, a cursor, a play count. When
it is not, hold the serving without `using` and call `Savor()` yourself at the point where
you know the flavour is good.

Disposing also ends the sitting: `Reheat` throws afterwards, and the next `Serve` reads
the pantry afresh. Disposing twice savors once.

## The boring stuff

What if two flavours are named the same?
* They collide — the file name uses the *simple* type name. Two `Settings` records from
  different namespaces would share a file.

What if the file is there but is not valid Json?
* `Serve` throws. The recipe is deliberately not used as a fallback: state you cannot
  read is a thing to go and look at, not to quietly overwrite.

What if I never `Savor`?
* Nothing is written. Loading happens on `Serve`, writing only on `Savor` or `Dispose`.
