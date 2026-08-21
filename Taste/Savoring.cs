namespace Taste.Savoring;

/// <summary>
///     For those who would rather savor than preserve.
/// </summary>
/// <remarks>
///     Lives in its own namespace on purpose: <c>Savor</c> extends every taste that can
///     make itself, so it only turns up for those who ask for it with
///     <c>using Taste.Savoring;</c>.
/// </remarks>
public static class Tasting
{
    /// <summary>
    ///     Savor a taste, so it can be tasted again at a later sitting. The same thing as
    ///     <see cref="Cook.Preserve{TTaste}" />, said the way a diner would say it.
    /// </summary>
    /// <typeparam name="TTaste">The type of taste to savor.</typeparam>
    /// <param name="taste">The taste to keep.</param>
    public static void Savor<TTaste>(this TTaste taste)
        where TTaste : class, new()
    {
        Cook.Preserve(taste);
    }
}
