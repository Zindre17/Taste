using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Taste.Tests;

[TestClass]
public class CookTests
{
    // One taste per test: the cook remembers per closed generic, and MSTest shares a
    // process, so a shared record would leak state between tests.
    public record Recited(string Made);
    public record Kept(string Stored);
    public record Preserved(string Savored);
    public record Replaced(string Which);
    public record Untaught(string Never);
    public record Taught(string Learned);
    public record TooLate
    {
        public string Missed { get; set; } = "served without a recipe";
    }
    public record Seasoned(string Flavouring);
    public record SameTwice
    {
        public string Filling { get; set; } = "empty";
    }
    public record SelfMade
    {
        public string Filling { get; set; } = "empty";
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
            Seasoning = new JsonSerializerOptions { WriteIndented = true },
        });
    }

    private static string DishFor<TTaste>()
    {
        var app = Assembly.GetEntryAssembly()!.GetName().Name!.ToLowerInvariant();
        return Path.Combine(pantry, $"{app}.{typeof(TTaste).Name.ToLowerInvariant()}.json");
    }

    [TestMethod]
    public void ATasteThatCanMakeItselfNeedsNoRecipe()
    {
        var taste = Cook.Serve<SelfMade>();

        Assert.AreEqual("empty", taste.Filling);
    }

    [TestMethod]
    public void ATasteThatCannotMakeItselfNeedsARecipe()
    {
        var e = Assert.Throws<InvalidOperationException>(() => Cook.Serve<Untaught>());

        StringAssert.Contains(e.Message, "Cook.Learn");
    }

    [TestMethod]
    public void LearnTeachesTheCookToMakeIt()
    {
        Cook.Learn(() => new Taught("from the recipe"));

        Assert.AreEqual(new Taught("from the recipe"), Cook.Serve<Taught>());
    }

    [TestMethod]
    public void LearningAfterServingThrows()
    {
        Cook.Serve<TooLate>();

        Assert.Throws<InvalidOperationException>(
            () => Cook.Learn(() => new TooLate { Missed = "too late" }));
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
        Cook.Preserve(new Preserved("I was preserved!"));

        Assert.AreEqual(
            new Preserved("I was preserved!"),
            JsonSerializer.Deserialize<Preserved>(File.ReadAllText(DishFor<Preserved>())));
    }

    [TestMethod]
    public void PreservedTasteIsWhatServeHandsOutAfterwards()
    {
        Cook.Learn(() => new Replaced("the original"));
        Cook.Serve<Replaced>();

        // A record replaced with `with` is a different object — this is exactly why
        // Preserve takes the taste rather than looking it up.
        Cook.Preserve(new Replaced("the replacement"));

        Assert.AreEqual(new Replaced("the replacement"), Cook.Serve<Replaced>());
    }

    [TestMethod]
    public void WhatWasKeptWinsOverAFreshTaste()
    {
        // Written straight to the pantry, so the cook has to read it rather than
        // remember it.
        Directory.CreateDirectory(pantry);
        File.WriteAllText(DishFor<Kept>(), JsonSerializer.Serialize(new Kept("kept")));

        Assert.AreEqual(new Kept("kept"), Cook.Serve<Kept>());
    }

    [TestMethod]
    public void ACorruptDishThrowsRatherThanStartingOver()
    {
        Directory.CreateDirectory(pantry);
        File.WriteAllText(DishFor<Recited>(), "{ this is not json");

        Assert.Throws<JsonException>(() => Cook.Serve<Recited>());
    }

    [TestMethod]
    public void TheKitchenSaysWhereAndHowTastesAreKept()
    {
        Cook.Preserve(new Seasoned("indented"));

        var dish = DishFor<Seasoned>();
        Assert.IsTrue(File.Exists(dish), "the kitchen's pantry was not used");
        StringAssert.Contains(File.ReadAllText(dish), Environment.NewLine);
    }

    [TestMethod]
    public void UseKitchenAfterTheCookHasStartedThrows()
    {
        // AssemblyInitialize already served this process's kitchen.
        Assert.Throws<InvalidOperationException>(
            () => Cook.UseKitchen(new Kitchen { Pantry = Path.GetTempPath() }));
    }
}
