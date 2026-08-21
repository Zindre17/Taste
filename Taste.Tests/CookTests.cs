using System.Reflection;
using System.Text.Json;

namespace Taste.Tests;

[TestClass]
public class CookTests
{
    // One taste per test: the cook remembers per closed generic, and MSTest shares a
    // process, so a shared record would leak state between tests. Every taste can make
    // itself, and says what a fresh one looks like with property initialisers.
    public record Corrupt
    {
        public string Unreadable { get; init; } = "never got this far";
    }

    public record Kept
    {
        public string Stored { get; init; } = "a fresh one";
    }

    public record Preserved
    {
        public string Savored { get; init; } = "not yet";
    }

    public record Replaced
    {
        public string Which { get; init; } = "the original";
    }

    public record Seasoned
    {
        public string Flavouring { get; init; } = "plain";
    }

    public record Fresh
    {
        public string Filling { get; init; } = "chocolate";
    }

    public record SameTwice
    {
        public string Filling { get; set; } = "empty";
    }

    // Deliberately the same short name as Cupboard.Twin below.
    public record Twin
    {
        public string Where { get; init; } = "nowhere";
    }

    // The cook's arrangements are settled once per process, so the whole run shares one
    // pantry. Tests stay isolated by using a taste type of their own, not a pantry of
    // their own.
    private static string pantry = null!;

    [AssemblyInitialize]
    public static void SetTheKitchen(TestContext context)
    {
        pantry = Path.Combine(Path.GetTempPath(), $"taste-tests-{Guid.NewGuid():N}");

        Cook.UseKitchen(new Kitchen
        {
            Pantry = pantry,
        });
    }

    // Mirrors Kitchen.LocateDish from the test side. If the naming scheme changes, this
    // has to change with it.
    private static string DishFor<TTaste>()
    {
        var app = Assembly.GetEntryAssembly()!.GetName().Name!;
        var taste = typeof(TTaste).FullName!.Replace('+', '.').Replace('`', '.');
        return Path.Combine(pantry, $"{app}.{taste}.json".ToLowerInvariant());
    }

    [TestMethod]
    public void AFreshTasteComesFromItsPropertyInitialisers()
    {
        Assert.AreEqual("chocolate", Cook.Serve<Fresh>().Filling);
    }

    [TestMethod]
    public void ServingTwiceHandsBackTheSameTaste()
    {
        var first = Cook.Serve<SameTwice>();
        first.Filling = "changed but not preserved";

        // No Reheat needed: Serve is how you reach it from anywhere.
        Assert.AreSame(first, Cook.Serve<SameTwice>());
        Assert.AreEqual("changed but not preserved", Cook.Serve<SameTwice>().Filling);
    }

    [TestMethod]
    public void PreserveWritesTheTasteToThePantry()
    {
        Cook.Preserve(new Preserved { Savored = "I was preserved!" });

        Assert.AreEqual(
            new Preserved { Savored = "I was preserved!" },
            JsonSerializer.Deserialize<Preserved>(File.ReadAllText(DishFor<Preserved>())));
    }

    [TestMethod]
    public void PreservedTasteIsWhatServeHandsOutAfterwards()
    {
        var taste = Cook.Serve<Replaced>();

        // `with` makes a different object than the one served — this is exactly why
        // Preserve takes the taste rather than looking it up.
        Cook.Preserve(taste with { Which = "the replacement" });

        Assert.AreEqual("the replacement", Cook.Serve<Replaced>().Which);
    }

    [TestMethod]
    public void WhatWasKeptWinsOverAFreshTaste()
    {
        // Written straight to the pantry, so the cook has to read it rather than
        // remember it.
        Directory.CreateDirectory(pantry);
        File.WriteAllText(
            DishFor<Kept>(), JsonSerializer.Serialize(new Kept { Stored = "kept" }));

        Assert.AreEqual("kept", Cook.Serve<Kept>().Stored);
    }

    [TestMethod]
    public void ACorruptDishThrowsRatherThanStartingOver()
    {
        Directory.CreateDirectory(pantry);
        File.WriteAllText(DishFor<Corrupt>(), "{ this is not json");

        Assert.Throws<JsonException>(() => Cook.Serve<Corrupt>());
    }

    [TestMethod]
    public void TheKitchenSaysWhereAndHowTastesAreKept()
    {
        Cook.Preserve(new Seasoned { Flavouring = "indented" });

        var dish = DishFor<Seasoned>();
        Assert.IsTrue(File.Exists(dish), "the kitchen's pantry was not used");
        StringAssert.Contains(File.ReadAllText(dish), Environment.NewLine);
    }

    [TestMethod]
    public void TastesWithTheSameShortNameGetTheirOwnDish()
    {
        Cook.Preserve(new Twin { Where = "nested in the test class" });
        Cook.Preserve(new Cupboard.Twin { Where = "in the cupboard" });

        Assert.AreNotEqual(DishFor<Twin>(), DishFor<Cupboard.Twin>());
        Assert.AreEqual("nested in the test class", Cook.Serve<Twin>().Where);
        Assert.AreEqual("in the cupboard", Cook.Serve<Cupboard.Twin>().Where);
    }

    [TestMethod]
    public void ANestedTasteIsNamedWithDotsRatherThanAPlus()
    {
        // Type.FullName says Taste.Tests.CookTests+Twin; a dish should not.
        Assert.IsFalse(Path.GetFileName(DishFor<Twin>()).Contains('+'));
        StringAssert.Contains(Path.GetFileName(DishFor<Twin>()), "cooktests.twin");
    }

    [TestMethod]
    public void UseKitchenAfterTheCookHasStartedThrows()
    {
        // AssemblyInitialize already served this process's kitchen.
        Assert.Throws<InvalidOperationException>(
            () => Cook.UseKitchen(new Kitchen { Pantry = Path.GetTempPath() }));
    }
}
