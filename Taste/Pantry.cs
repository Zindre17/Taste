using System.Reflection;

namespace Taste;

/// <summary>
///     Where servings are kept between meals.
/// </summary>
public static class Pantry
{
    private static string? location;

    /// <summary>
    ///     The directory servings are read from and written to when no pantry is named
    ///     at the counter. Defaults to the directory of the running executable.
    /// </summary>
    /// <remarks>
    ///     Set this before the first <see cref="Kitchen.Serve{TFlavour}(Func{TFlavour}, string?)" />.
    ///     A flavour resolves its location once, when it is first served, and keeps it
    ///     until that serving is disposed; moving the pantry afterwards does not move a
    ///     serving that has already been dished up.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///     The default location was needed, but the directory of the running executable
    ///     could not be determined.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    ///     An attempt was made to set the location to <see langword="null" />.
    /// </exception>
    public static string Location
    {
        get => location ??= Path.GetDirectoryName(Environment.ProcessPath)
            ?? throw new InvalidOperationException(
                "Could not find the directory of the running executable. "
                + "Set Pantry.Location to say where servings should be kept.");
        set => location = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    ///     The file a flavour is kept in: <c>{entry assembly}.{flavour}.json</c>, in the
    ///     given pantry or in <see cref="Location" />.
    /// </summary>
    internal static string LocateDish<TFlavour>(string? pantry)
    {
        var name = Assembly.GetEntryAssembly()?.GetName().Name
            ?? throw new InvalidOperationException("Could not find name of entry assembly.");

        return Path.Combine(
            pantry ?? Location,
            $"{name.ToLowerInvariant()}.{typeof(TFlavour).Name.ToLowerInvariant()}.json");
    }
}
