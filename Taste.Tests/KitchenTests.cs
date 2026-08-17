using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Taste.Tests;

[TestClass]
public class KitchenTests
{
    // One flavour per test: servings are cached per closed generic, and MSTest shares
    // a process, so a shared record would leak state between tests.
    public record Unserved(string Never);
    public record SameServing(string Hello);
    public record Coexisting(string World);
    public record Reciped(string Made);
    public record Kept(string Stored);
    public record Persisted(string Savored);
    public record Elsewhere(string Away);
    public record Nested(string Deep);
    public record Disposable(string Gone);
    public record Reserved(string Again);
    public record Untouched(string Original);
    public record SelfMade
    {
        public string Filling { get; set; } = "empty";
    }

    private static string FreshPantry()
    {
        return Path.Combine(Path.GetTempPath(), $"taste-tests-{Guid.NewGuid():N}");
    }

    [TestMethod]
    public void CannotReheatBeforeYouServe()
    {
        Assert.Throws<InvalidOperationException>(() => Kitchen.Reheat<Unserved>());
    }

    [TestMethod]
    public void EnsureSameServing()
    {
        var serving = Kitchen.Serve(() => new SameServing("hi"));
        var reheated = Kitchen.Reheat<SameServing>();

        Assert.AreSame(serving, reheated);
        Assert.AreSame(serving, Kitchen.Serve(() => new SameServing("hi")));
    }

    [TestMethod]
    public void EnsureDifferentFlavoursCoexist()
    {
        var one = Kitchen.Serve(() => new SameServing("hi"));
        var other = Kitchen.Serve(() => new Coexisting("world"));

        Assert.AreNotSame<object>(one, other);
        Assert.AreSame(one, Kitchen.Reheat<SameServing>());
    }

    [TestMethod]
    public void RecipeMakesTheFlavourWhenThePantryIsEmpty()
    {
        var serving = Kitchen.Serve(() => new Reciped("from the recipe"), FreshPantry());

        Assert.AreEqual(new Reciped("from the recipe"), serving.Flavour);
    }

    [TestMethod]
    public void WhatWasKeptWinsOverTheRecipe()
    {
        var pantry = FreshPantry();
        using (var first = Kitchen.Serve(() => new Kept("first"), pantry))
        {
            first.Flavour = new Kept("kept");
        }

        var second = Kitchen.Serve(() => new Kept("the recipe should not run"), pantry);

        Assert.AreEqual(new Kept("kept"), second.Flavour);
    }

    [TestMethod]
    public void SavorWritesTheFlavourToThePantry()
    {
        var pantry = FreshPantry();
        var serving = Kitchen.Serve(() => new Persisted("I was savored!"), pantry);

        serving.Savor();

        var dish = Directory.GetFiles(pantry).Single();
        Assert.AreEqual(
            serving.Flavour,
            JsonSerializer.Deserialize<Persisted>(File.ReadAllText(dish)));
    }

    [TestMethod]
    public void ServingsCanBeKeptSomewhereElse()
    {
        var pantry = FreshPantry();
        var serving = Kitchen.Serve(() => new Elsewhere("over here"), pantry);

        serving.Savor();

        Assert.IsTrue(Directory.GetFiles(pantry).Single().EndsWith("elsewhere.json"));
    }

    [TestMethod]
    public void SavorMakesThePantryIfItIsNotThereYet()
    {
        var pantry = Path.Combine(FreshPantry(), "shelf", "back");
        Assert.IsFalse(Directory.Exists(pantry));

        Kitchen.Serve(() => new Nested("deep"), pantry).Savor();

        Assert.IsTrue(Directory.Exists(pantry));
    }

    [TestMethod]
    public void DisposingSavors()
    {
        var pantry = FreshPantry();

        using (var serving = Kitchen.Serve(() => new Disposable("start"), pantry))
        {
            serving.Flavour = new Disposable("saved by dispose");
        }

        var dish = Directory.GetFiles(pantry).Single();
        Assert.AreEqual(
            new Disposable("saved by dispose"),
            JsonSerializer.Deserialize<Disposable>(File.ReadAllText(dish)));
    }

    [TestMethod]
    public void DisposingClearsTheTable()
    {
        var pantry = FreshPantry();
        var serving = Kitchen.Serve(() => new Reserved("first"), pantry);

        serving.Dispose();

        Assert.Throws<InvalidOperationException>(() => Kitchen.Reheat<Reserved>());
        Assert.Throws<ObjectDisposedException>(() => serving.Savor());
        Assert.AreNotSame(serving, Kitchen.Serve(() => new Reserved("second"), pantry));
    }

    [TestMethod]
    public void ServingAgainIgnoresTheSecondRecipeAndPantry()
    {
        var pantry = FreshPantry();
        var first = Kitchen.Serve(() => new Untouched("first"), pantry);

        var second = Kitchen.Serve(() => new Untouched("second"), FreshPantry());

        Assert.AreSame(first, second);
        Assert.AreEqual(new Untouched("first"), second.Flavour);
    }

    [TestMethod]
    public void FlavoursThatCanMakeThemselvesNeedNoRecipe()
    {
        var serving = Kitchen.Serve<SelfMade>(FreshPantry());

        Assert.AreEqual("empty", serving.Flavour.Filling);
    }
}
