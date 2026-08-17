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
    ///     Makes the first flavour, for when nothing has been savored yet. Only consulted
    ///     on the first serving of this flavour — later servings hand back what is
    ///     already on the table, recipe untouched.
    /// </param>
    /// <param name="pantry">
    ///     Directory to keep this flavour in. Defaults to
    ///     <see cref="Pantry.Location" />. Only honoured on the first serving.
    /// </param>
    /// <returns>The serving, whose <see cref="Serving{TFlavour}.Flavour" /> is never null.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="recipe" /> is null.</exception>
    /// <exception cref="System.Text.Json.JsonException">
    ///     The pantry holds a file for this flavour that is not valid JSON. The recipe is
    ///     deliberately <i>not</i> used as a fallback here: a flavour that cannot be read
    ///     is a problem to look at, not to quietly overwrite.
    /// </exception>
    public static Serving<TFlavour> Serve<TFlavour>(Func<TFlavour> recipe, string? pantry = null)
    {
        if (recipe is null)
        {
            throw new ArgumentNullException(nameof(recipe));
        }

        return Serving<TFlavour>.DishUp(recipe, pantry);
    }

    /// <summary>
    ///     Serve a flavour that can make itself, for when the recipe is just
    ///     <c>new TFlavour()</c>.
    /// </summary>
    /// <typeparam name="TFlavour">The type of flavour to serve.</typeparam>
    /// <param name="pantry">
    ///     Directory to keep this flavour in. Defaults to
    ///     <see cref="Pantry.Location" />. Only honoured on the first serving.
    /// </param>
    /// <returns>The serving, whose <see cref="Serving{TFlavour}.Flavour" /> is never null.</returns>
    public static Serving<TFlavour> Serve<TFlavour>(string? pantry = null)
        where TFlavour : new()
    {
        return Serving<TFlavour>.DishUp(() => new TFlavour(), pantry);
    }

    /// <summary>
    ///     Take up a flavour that is already being served, from wherever you happen to be.
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
