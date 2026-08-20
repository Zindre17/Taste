using System.Reflection;
using System.Text.Json;

namespace Taste;

/// <summary>
///     The arrangements a cook works under: where the pantry is, how tastes are written
///     down. Build one and hand it to <see cref="Cook.UseKitchen" /> if the standard
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
    ///     The standard arrangements, used until a cook is given a different kitchen.
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

    /// <summary>
    ///     How tastes are written down and read back. Defaults to plain Json.
    /// </summary>
    public JsonSerializerOptions? Seasoning { get; init; }

    /// <summary>
    ///     The file a taste is kept in: <c>{entry assembly}.{taste}.json</c>, in this
    ///     kitchen's <see cref="Pantry" />.
    /// </summary>
    internal string LocateDish<TTaste>()
    {
        var name = Assembly.GetEntryAssembly()?.GetName().Name
            ?? throw new InvalidOperationException("Could not find name of entry assembly.");

        return Path.Combine(
            Pantry,
            $"{name.ToLowerInvariant()}.{typeof(TTaste).Name.ToLowerInvariant()}.json");
    }
}
