using System.Text.Json;

namespace Taste;

/// <summary>
///     A tasteful persistable state handler for console applications. The cook serves a
///     taste from the pantry and preserves it again when it has changed.
/// </summary>
public static class Cook
{
    private static Kitchen? kitchen;

    private static bool hasCooked;

    /// <summary>
    ///     Work in a different kitchen than the standard one.
    /// </summary>
    /// <param name="kitchen">The arrangements to work under.</param>
    /// <exception cref="InvalidOperationException">
    ///     The cook has already been to the pantry. Arrangements are settled before the
    ///     first <see cref="Serve{TTaste}" /> or <see cref="Preserve{TTaste}" />, not
    ///     after — a kitchen swapped in later would leave tastes already served sitting
    ///     under the old one.
    /// </exception>
    public static void UseKitchen(Kitchen kitchen)
    {
        if (hasCooked)
        {
            throw new InvalidOperationException(
                "The cook has already been to the pantry. Call UseKitchen before the "
                + "first Serve or Preserve, so every taste is kept in the same place.");
        }

        Cook.kitchen = kitchen;
    }

    /// <summary>
    ///     Teach the cook how to make a taste from scratch, for when the pantry has
    ///     nothing kept for it yet. Only needed for tastes that cannot make themselves —
    ///     one without a parameterless constructor, such as a positional record.
    /// </summary>
    /// <typeparam name="TTaste">The type of taste this recipe makes.</typeparam>
    /// <param name="recipe">Makes the taste when the pantry has nothing.</param>
    /// <exception cref="InvalidOperationException">
    ///     This taste has already been served, so the recipe is too late to matter.
    /// </exception>
    public static void Learn<TTaste>(Func<TTaste> recipe)
    {
        if (Dish<TTaste>.Served)
        {
            throw new InvalidOperationException(
                $"{typeof(TTaste).Name} has already been served, so this recipe would "
                + "never be used. Teach the cook before serving the taste.");
        }

        Dish<TTaste>.Recipe = recipe;
    }

    /// <summary>
    ///     Serve a taste: what the pantry kept last time, or a fresh one if the pantry
    ///     has nothing. Serving the same taste again hands back the one already served,
    ///     so this is safe to call from anywhere, as often as you like.
    /// </summary>
    /// <typeparam name="TTaste">The type of taste to serve.</typeparam>
    /// <returns>The taste, never <see langword="null" />.</returns>
    /// <exception cref="InvalidOperationException">
    ///     The pantry had nothing kept, and this taste cannot make itself. Teach the cook
    ///     with <see cref="Learn{TTaste}" /> first.
    /// </exception>
    /// <exception cref="JsonException">
    ///     The pantry holds a file for this taste that is not valid Json. A fresh taste is
    ///     deliberately <i>not</i> used as a fallback here: a taste that cannot be read is
    ///     a problem to look at, not to quietly overwrite.
    /// </exception>
    public static TTaste Serve<TTaste>()
    {
        if (Dish<TTaste>.Served)
        {
            return Dish<TTaste>.Taste!;
        }

        var dish = Working.LocateDish<TTaste>();

        TTaste taste;
        if (File.Exists(dish))
        {
            var kept = JsonSerializer.Deserialize<TTaste>(
                File.ReadAllText(dish), Working.Seasoning);
            taste = kept is null ? Make<TTaste>() : kept;
        }
        else
        {
            taste = Make<TTaste>();
        }

        Keep(taste);
        return taste;
    }

    /// <summary>
    ///     Preserve a taste, so it can be served again at a later sitting. Pass the taste
    ///     itself: a record replaced with <c>with</c> is a different object than the one
    ///     served, and the cook has no way to notice that on its own.
    /// </summary>
    /// <remarks>
    ///     Creates the pantry directory if it is not there yet. Writes the whole taste
    ///     every time; there is no partial update. The preserved taste is what
    ///     <see cref="Serve{TTaste}" /> hands out from here on.
    /// </remarks>
    /// <typeparam name="TTaste">The type of taste to preserve.</typeparam>
    /// <param name="taste">The taste to keep.</param>
    public static void Preserve<TTaste>(TTaste taste)
    {
        var dish = Working.LocateDish<TTaste>();

        var pantry = Path.GetDirectoryName(dish);
        if (!string.IsNullOrEmpty(pantry))
        {
            Directory.CreateDirectory(pantry);
        }

        File.WriteAllText(dish, JsonSerializer.Serialize(taste, Working.Seasoning));
        Keep(taste);
    }

    /// <summary>
    ///     The kitchen in use, and the point at which arrangements are settled: once the
    ///     cook has been to the pantry, <see cref="UseKitchen" /> is too late.
    /// </summary>
    private static Kitchen Working
    {
        get
        {
            hasCooked = true;
            return kitchen ?? Kitchen.Default;
        }
    }

    /// <summary>
    ///     Makes a taste from scratch: the learned recipe if there is one, otherwise the
    ///     taste makes itself.
    /// </summary>
    private static TTaste Make<TTaste>()
    {
        if (Dish<TTaste>.Recipe is not null)
        {
            return Dish<TTaste>.Recipe();
        }

        try
        {
            return Activator.CreateInstance<TTaste>();
        }
        catch (MissingMethodException e)
        {
            throw new InvalidOperationException(
                $"The pantry has nothing kept for {typeof(TTaste).Name}, and it cannot "
                + "make itself because it has no parameterless constructor. Teach the "
                + $"cook first: Cook.Learn(() => new {typeof(TTaste).Name}(...));",
                e);
        }
    }

    private static void Keep<TTaste>(TTaste taste)
    {
        Dish<TTaste>.Taste = taste;
        Dish<TTaste>.Served = true;
    }

    /// <summary>
    ///     What the cook knows about one taste. A static field on a generic type, so each
    ///     taste gets its own — there is no registry to keep.
    /// </summary>
    private static class Dish<TTaste>
    {
        public static Func<TTaste>? Recipe;

        public static TTaste? Taste;

        public static bool Served;
    }
}
