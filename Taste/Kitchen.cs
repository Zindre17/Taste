namespace Taste;

/// <summary>
///     A tasteful persistable state handler for console applications.
/// </summary>
public static class Kitchen
{
    /// <summary>
    ///     Serve a flavour: what the pantry kept last time, or whatever
    ///     <paramref name="recipe" /> makes if the pantry has nothing.
    /// </summary>
    /// <typeparam name="TFlavour">The type of flavour to serve.</typeparam>
    /// <param name="recipe">
    ///     Makes the flavour when the pantry has nothing kept for it yet.
    /// </param>
    /// <param name="pantry">
    ///     Directory to keep this flavour in. Defaults to <see cref="Pantry.Location" />.
    /// </param>
    /// <returns>The serving, whose <see cref="Serving{TFlavour}.Flavour" /> is never null.</returns>
    /// <exception cref="InvalidOperationException">
    ///     This flavour is already being served. Serve starts a sitting, and there is one
    ///     sitting at a time: use <see cref="Reheat{TFlavour}" /> to take up the serving
    ///     already on the table, or dispose it to start a new sitting.
    /// </exception>
    /// <exception cref="System.Text.Json.JsonException">
    ///     The pantry holds a file for this flavour that is not valid JSON. The recipe is
    ///     deliberately <i>not</i> used as a fallback here: a flavour that cannot be read
    ///     is a problem to look at, not to quietly overwrite.
    /// </exception>
    public static Serving<TFlavour> Serve<TFlavour>(Func<TFlavour> recipe, string? pantry = null)
    {
        return Serving<TFlavour>.DishUp(recipe, pantry);
    }

    /// <summary>
    ///     Serve a flavour that can make itself, for when the recipe is just
    ///     <c>new TFlavour()</c>.
    /// </summary>
    /// <typeparam name="TFlavour">The type of flavour to serve.</typeparam>
    /// <param name="pantry">
    ///     Directory to keep this flavour in. Defaults to <see cref="Pantry.Location" />.
    /// </param>
    /// <returns>The serving, whose <see cref="Serving{TFlavour}.Flavour" /> is never null.</returns>
    /// <exception cref="InvalidOperationException">
    ///     This flavour is already being served. See the other overload.
    /// </exception>
    public static Serving<TFlavour> Serve<TFlavour>(string? pantry = null)
        where TFlavour : new()
    {
        return Serving<TFlavour>.DishUp(() => new TFlavour(), pantry);
    }

    /// <summary>
    ///     Take up a flavour that is already being served, from wherever you happen to be.
    ///     This is how you reach a flavour after the one <c>Serve</c> that started it.
    /// </summary>
    /// <typeparam name="TFlavour">The type of flavour to reheat.</typeparam>
    /// <returns>The same serving that <c>Serve</c> handed out earlier.</returns>
    /// <exception cref="InvalidOperationException">
    ///     This flavour is not being served — it has not been served yet, or its serving
    ///     has been disposed. You cannot reheat what was never cooked.
    /// </exception>
    public static Serving<TFlavour> Reheat<TFlavour>()
    {
        return Serving<TFlavour>.Reheat();
    }
}
