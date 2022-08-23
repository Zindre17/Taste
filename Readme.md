# Taste
A tasteful way to persist state for console applications.

## How to taste

```csharp
// The state I want to persist
record Snack(string Type);

...

// Load snack state
var taste = Taste<Snack>.Bite();

// This will get you the same instance as the bite method gave you. However, you cannot chew until you take a bite!
var sameTaste = Taste<Snack>.Chew;

// get the current snack (Can be null)
var snack = taste.Flavour;

// set new snack
taste.Flavour = new Snack("Chocolate");

// persist the snack state
taste.Savor();
```


## The boring stuff
How is the state persisted? 
* Json file(s) at the same location as the executable

What is the difference between `Bite()` and `Chew`?
* Well, you cannot `Chew` untill you take a `Bite()`, but that aside, not much.
