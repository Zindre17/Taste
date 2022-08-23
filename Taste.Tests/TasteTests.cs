using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Taste.Tests;

[TestClass]
public class TasteTests
{
    public record Flavour1(string Hello);
    public record Flavour2(string World);

    public record PersistedFlavour(string Persisted);

    // Don't take a bite of this!
    public record UntastedFlavour(string Never);

    [TestMethod]
    public void CannotChewBeforeYouTakeABite()
    {
        Assert.ThrowsException<InvalidOperationException>(() => Taste<UntastedFlavour>.Chew);
    }

    [TestMethod]
    public void EnsureSameInstance()
    {
        var taste = Taste<Flavour1>.Bite();
        var otherTaste = Taste<Flavour1>.Chew;
        var thirdTaste = Taste<Flavour1>.Bite();

        Assert.AreEqual(taste, otherTaste);
        Assert.AreEqual(otherTaste, thirdTaste);
        Assert.AreEqual(taste, thirdTaste);
    }

    [TestMethod]
    public void EnsureDifferentTastesCanCoexist()
    {
        var taste = Taste<Flavour1>.Bite();
        var otherTaste = Taste<Flavour2>.Bite();
        var firstTasteAgain = Taste<Flavour1>.Chew;

        Assert.AreNotEqual(taste, otherTaste);
        Assert.AreEqual(taste, firstTasteAgain);
    }

    [TestMethod]
    public void EnsurePeristed()
    {
        var taste = Taste<PersistedFlavour>.Bite();
        taste.Flavour = new("I was savored!");
        taste.Savor();

        var name = Assembly.GetEntryAssembly()?.GetName().Name
                ?? throw new InvalidOperationException("Could not find name of executing assembly.");

        var directory = Path.GetDirectoryName(Environment.ProcessPath)
            ?? throw new InvalidOperationException("Could not find current directory.");

        var filePath = Path.Combine(directory, $"{name.ToLower()}.{typeof(PersistedFlavour).Name.ToLower()}.json");
        var flavourFromFile = JsonSerializer.Deserialize<PersistedFlavour>(File.ReadAllText(filePath));

        Assert.AreEqual(taste.Flavour, flavourFromFile);
    }
}
