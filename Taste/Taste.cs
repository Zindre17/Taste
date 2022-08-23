using System.Reflection;
using System.Text.Json;

namespace Taste;

/// <summary>
///     A tasteful persistable state handler for console applications.
/// </summary>
/// <typeparam name="TFlavour">The type of flavour to taste.</typeparam>
public class Taste<TFlavour>
{
    /// <summary>
    ///     If you have taken a bite <see cref="Bite" />, you can chew at any time to experience the flavour.
    /// </summary>
    /// <returns>
    ///     The same taste as when you took a bite earlier.
    /// </returns>
    public static Taste<TFlavour> Chew => taste
        ?? throw new InvalidOperationException("You need to take a bite before you can chew!");

    /// <summary>
    ///     The current flavour of the bite that was taken.
    /// </summary>
    public TFlavour? Flavour { get; set; }

    /// <summary>
    ///     Take a bite and feel the taste.
    /// </summary>
    /// <returns>
    ///     The taste.
    /// </returns>
    public static Taste<TFlavour> Bite()
    {
        return taste ?? new();
    }

    /// <summary>
    ///     Savor the taste, so you can remember the taste at a later time.
    /// </summary>
    public void Savor()
    {
        File.WriteAllText(
            LocateSnack(),
            JsonSerializer.Serialize(Flavour)
        );
    }

    private static Taste<TFlavour>? taste;

    private readonly TFlavour? initialFlavour;

    private Taste()
    {
        initialFlavour = File.Exists(LocateSnack())
            ? JsonSerializer.Deserialize<TFlavour>(
                File.ReadAllText(LocateSnack()))
            : default;
        Flavour = initialFlavour;
        taste = this;
    }

    private static string? snackDrawer = null;
    private static string LocateSnack()
    {
        if (snackDrawer is null)
        {
            var name = Assembly.GetEntryAssembly()?.GetName().Name
                ?? throw new InvalidOperationException("Could not find name of entry assembly.");

            var drawer = Path.GetDirectoryName(Environment.ProcessPath)
                ?? throw new InvalidOperationException("Could not find current directory.");

            snackDrawer = Path.Combine(drawer, $"{name.ToLower()}.{typeof(TFlavour).Name.ToLower()}.json");
        }
        return snackDrawer;
    }
}
