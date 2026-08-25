using System.Collections.Frozen;

namespace Taste;

/// <summary>
///     The arrangements a cook works under. Build one and hand it to <see cref="Cook.UseKitchen" />
///     if the standard arrangements are not what you want.
/// </summary>
/// <remarks>
///     A kitchen is fixed once built. Nothing about it can be changed afterwards, so
///     there is no question of a taste being served under arrangements that have since
///     moved.
/// </remarks>
public sealed class Kitchen
{
    private static readonly FrozenDictionary<Type, string> NoOtherPantries =
        FrozenDictionary<Type, string>.Empty;

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

    /// <summary>
    ///     Pantries for tastes that are not kept with the rest. A taste listed here goes in
    ///     its own pantry; everything else goes in <see cref="Pantry" />.
    /// </summary>
    /// <remarks>
    ///     For the app whose settings belong in one place and whose records belong in
    ///     another — the two the operating system keeps apart. Copied when the kitchen is
    ///     built, so the dictionary handed in cannot be changed afterwards. A taste kept
    ///     here never reads <see cref="Pantry" />, so an app that gives every taste a
    ///     pantry of its own never needs the default and never trips its exception.
    /// </remarks>
    /// <example>
    ///     <code>
    ///     Cook.UseKitchen(new Kitchen
    ///     {
    ///         Pantry = dataDirectory,
    ///         Pantries = new Dictionary&lt;Type, string&gt;
    ///         {
    ///             [typeof(Settings)] = configDirectory,
    ///         },
    ///     });
    ///     </code>
    /// </example>
    public IReadOnlyDictionary<Type, string> Pantries
    {
        get;
        init => field = value.ToFrozenDictionary();
    } = NoOtherPantries;
}
