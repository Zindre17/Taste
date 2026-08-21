using System.Reflection;
using System.Text.Json;

namespace Taste;

/// <summary>
///     A tasteful persistable state handler for console applications. The cook serves a
///     taste from the pantry and preserves it again when you ask for it.
/// </summary>
public static class Cook
{
    private static Kitchen kitchen = Kitchen.Default;
    private static bool hasEnteredKitchen = false;

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
        if (hasEnteredKitchen)
        {
            throw new InvalidOperationException(
                "The cook has already been to the pantry. Call UseKitchen before the "
               + "first Serve or Preserve, so every taste is kept in the same place.");
        }

        Cook.kitchen = kitchen;
    }

    /// <summary>
    ///     Serve a taste: what the pantry kept last time, or a fresh one if the pantry
    ///     has nothing. Serving the same taste again hands back the one already served,
    ///     so this is safe to call from anywhere, as often as you like.
    /// </summary>
    /// <remarks>
    ///     A taste makes itself when the pantry is empty, so it needs a parameterless
    ///     constructor. Say what a fresh one looks like with property initializers — that
    ///     keeps the starting state on the taste itself, where it is hard to miss.
    /// </remarks>
    /// <typeparam name="TTaste">The type of taste to serve.</typeparam>
    /// <returns>The taste, never <see langword="null" />.</returns>
    /// <exception cref="JsonException">
    ///     The pantry holds a file for this taste that is not valid Json. A fresh taste is
    ///     deliberately <i>not</i> used as a fallback here: a taste that cannot be read is
    ///     a problem to look at, not to quietly overwrite.
    /// </exception>
    public static TTaste Serve<TTaste>()
        where TTaste : class, new()
    {
        if (Dish<TTaste>.IsServed)
        {
            return Dish<TTaste>.Taste!;
        }

        var kitchen = EnterKitchen();
        var jar = JarFor<TTaste>(kitchen);
        var taste = ReheatOrCook<TTaste>(jar);
        Remember(taste);
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
        where TTaste : class, new()
    {
        var kitchen = EnterKitchen();
        PreparePantrySpace(kitchen);
        var jar = JarFor<TTaste>(kitchen);
        PlaceInPantry(jar, taste);
        Remember(taste);
    }

    private static TTaste ReheatOrCook<TTaste>(string jar)
        where TTaste : class, new()
    {
        return GrabFromPantry<TTaste>(jar) ?? new TTaste();
    }

    private static void PreparePantrySpace(Kitchen kitchen)
    {
        var pantry = kitchen.Pantry;
        if (!string.IsNullOrEmpty(pantry))
        {
            Directory.CreateDirectory(pantry);
        }
    }

    private static void PlaceInPantry<TTaste>(string jar, TTaste taste)
        where TTaste : class, new()
    {
        File.WriteAllText(jar, JsonSerializer.Serialize(taste));
    }

    private static TTaste? GrabFromPantry<TTaste>(string jar)
        where TTaste : class, new()
    {
        if (!File.Exists(jar))
        {
            return null;
        }
        return JsonSerializer.Deserialize<TTaste>(File.ReadAllText(jar));
    }

    private static void Remember<TTaste>(TTaste taste)
        where TTaste : class, new()
    {
        Dish<TTaste>.Taste = taste;
        Dish<TTaste>.IsServed = true;
    }

    private static Kitchen EnterKitchen()
    {
        hasEnteredKitchen = true;
        return kitchen;
    }

    /// <summary>
    ///     The file a taste is kept in: <c>{entry assembly}.{taste}.json</c>, in this
    ///     kitchen's pantry. The taste is named in full, namespace and all,
    ///     so two tastes with the same short name do not end up in the same jar.
    /// </summary>
    private static string JarFor<TTaste>(Kitchen kitchen)
        where TTaste : class, new()
    {
        var app = Assembly.GetEntryAssembly()?.GetName().Name
            ?? throw new InvalidOperationException("Could not find name of entry assembly.");

        return Path.Combine(kitchen.Pantry, $"{app}.{NameOf<TTaste>()}.json".ToLowerInvariant());
    }

    /// <summary>
    ///     A taste's full name, tidied into something that can be a file name.
    /// </summary>
    private static string NameOf<TTaste>()
        where TTaste : class, new()
    {
        var taste = typeof(TTaste);
        var name = taste.FullName ?? taste.Name;

        // A generic taste arrives assembly-qualified — Ns.Held`1[[System.Int32, ...]].
        // Keep the readable head; the arity is enough to tell Held<T> apart from Held.
        var arguments = name.IndexOf('[', StringComparison.Ordinal);
        if (arguments >= 0)
        {
            name = name[..arguments];
        }

        // A nested taste comes through as Outer+Inner, and the arity as Held`1.
        return name.Replace('+', '.').Replace('`', '.');
    }

    /// <summary>
    ///     What the cook knows about one taste. A static field on a generic type, so each
    ///     taste gets its own — there is no registry to keep.
    /// </summary>
    private static class Dish<TTaste>
        where TTaste : class, new()
    {
        public static TTaste? Taste;
        public static bool IsServed;
    }
}
