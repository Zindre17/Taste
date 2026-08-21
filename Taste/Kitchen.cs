namespace Taste;

/// <summary>
///     The arrangements a cook works under: where the pantry is down. Build
///     one and hand it to <see cref="Cook.UseKitchen" /> if the standard
///     arrangements are not what you want.
/// </summary>
/// <remarks>
///     A kitchen is fixed once built. Nothing about it can be changed afterwards, so
///     there is no question of a taste being served under arrangements that have since
///     moved.
/// </remarks>
public sealed class Kitchen
{
    /// <summary>
    ///     The standard arrangements, used unless a cook is given a different kitchen.
    /// </summary>
    public static Kitchen Default { get; } = new();

    /// <summary>
    ///     Directory tastes are read from and written to. Defaults to the directory of
    ///     the running executable, worked out the first time it is needed.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     The default was needed, but the directory of the running executable could not
    ///     be determined.
    /// </exception>
    public string Pantry
    {
        get => field ??= Path.GetDirectoryName(Environment.ProcessPath)
            ?? throw new InvalidOperationException(
                "Could not find the directory of the running executable. "
                + "Build a Kitchen with a Pantry to say where tastes should be kept.");
        init;
    }
}
