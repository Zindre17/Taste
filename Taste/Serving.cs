using System.Text.Json;

namespace Taste;

/// <summary>
///     A flavour that has been dished up, and the means to keep it.
/// </summary>
/// <typeparam name="TFlavour">The type of flavour being served.</typeparam>
public sealed class Serving<TFlavour> : IDisposable
{
    private static Serving<TFlavour>? current;

    private readonly string dish;

    private bool disposed;

    private Serving(TFlavour flavour, string dish)
    {
        Flavour = flavour;
        this.dish = dish;
    }

    /// <summary>
    ///     The flavour itself: what was kept in the pantry, or what the recipe made when
    ///     the pantry had nothing to offer. Never <see langword="null" />.
    /// </summary>
    public TFlavour Flavour { get; set; }

    /// <summary>
    ///     Savor the flavour, so it can be tasted again at a later sitting.
    /// </summary>
    /// <remarks>
    ///     Creates the pantry directory if it is not there yet. Writes the whole flavour
    ///     every time; there is no partial update.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">This serving has been disposed.</exception>
    public void Savor()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var pantry = Path.GetDirectoryName(dish);
        if (!string.IsNullOrEmpty(pantry))
        {
            Directory.CreateDirectory(pantry);
        }

        File.WriteAllText(dish, JsonSerializer.Serialize(Flavour));
    }

    /// <summary>
    ///     Savors the flavour, then clears the serving from the kitchen.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>Disposing writes to the pantry.</b> That is the point of the
    ///         <c>using</c> form — but it means <i>every</i> exit from the scope
    ///         persists, including an exit caused by an exception. A flavour that was
    ///         half-updated when something threw is savored just the same.
    ///     </para>
    ///     <para>
    ///         So reach for <c>using</c> only when "whatever the flavour looks like at
    ///         the end of this scope is worth keeping" is genuinely what you mean. If it
    ///         is not, hold the serving without <c>using</c> and call
    ///         <see cref="Savor" /> yourself, at the point where you know the flavour is
    ///         good.
    ///     </para>
    ///     <para>
    ///         Disposal also ends the sitting: <see cref="Kitchen.Reheat{TFlavour}" />
    ///         throws afterwards, and the next
    ///         <see cref="Kitchen.Serve{TFlavour}(Func{TFlavour}, string?)" /> reads the
    ///         pantry afresh. Disposing twice savors once.
    ///     </para>
    /// </remarks>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Savor();
        disposed = true;

        if (ReferenceEquals(current, this))
        {
            current = null;
        }
    }

    /// <summary>
    ///     Starts a sitting: reads the pantry and puts this flavour on the table. One
    ///     sitting at a time — see <see cref="Reheat" /> to take up the one already there.
    /// </summary>
    internal static Serving<TFlavour> DishUp(Func<TFlavour> recipe, string? pantry)
    {
        if (current is not null)
        {
            throw new InvalidOperationException(
                $"{typeof(TFlavour).Name} is already being served. Reheat it to take up "
                + "the serving that is on the table, or dispose that serving to start a "
                + "new sitting.");
        }

        var dish = Pantry.LocateDish<TFlavour>(pantry);

        TFlavour flavour;
        if (File.Exists(dish))
        {
            var kept = JsonSerializer.Deserialize<TFlavour>(File.ReadAllText(dish));
            flavour = kept is null ? recipe() : kept;
        }
        else
        {
            flavour = recipe();
        }

        current = new Serving<TFlavour>(flavour, dish);
        return current;
    }

    /// <summary>
    ///     The serving already on the table, if there is one.
    /// </summary>
    internal static Serving<TFlavour> Reheat()
    {
        return current
            ?? throw new InvalidOperationException(
                $"There is no {typeof(TFlavour).Name} on the table. "
                + "You need to serve a flavour before you can reheat it.");
    }
}
