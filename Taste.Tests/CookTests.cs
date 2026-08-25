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

    public record Fresh
    {
        public string Filling { get; init; } = "chocolate";
    }

    public record struct AlsoFresh
    {
        public AlsoFresh() { }
        public string Filling { get; init; } = "vanilla cream";
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

    // Three tastes about where a taste is kept. The first two have a pantry of their own,
    // handed to the kitchen in SetTheKitchen — arrangements are settled once per process,
    // so there is nowhere else to declare them.
    public record Elsewhere
    {
        public string Where { get; init; } = "not yet anywhere";
    }

    public record Created
    {
        public string Where { get; init; } = "not yet anywhere";
    }

    public record AtHome
    {
        public string Where { get; init; } = "not yet anywhere";
    }

    // The cook's arrangements are settled once per process, so the whole run shares one
    // pantry. Tests stay isolated by using a taste type of their own, not a pantry of
    // their own.
    private static string pantry = null!;
    private static string otherPantry = null!;
    private static string madePantry = null!;

    [AssemblyInitialize]
    public static void SetTheKitchen(TestContext context)
    {
        var root = Path.Combine(Path.GetTempPath(), $"taste-tests-{Guid.NewGuid():N}");
        pantry = Path.Combine(root, "pantry");
        otherPantry = Path.Combine(root, "other");

        // Deliberately never created here: preserving the taste has to make it.
        madePantry = Path.Combine(root, "made");

        Cook.UseKitchen(new Kitchen
        {
            Pantry = pantry,
            Pantries = new Dictionary<Type, string>
            {
                [typeof(Elsewhere)] = otherPantry,
                [typeof(Created)] = madePantry,
            },
        });
    }

    // Mirrors Cook.JarFor from the test side. If the naming scheme changes, this
    // has to change with it. Takes the pantry, as the real one now does, since a taste
    // is no longer necessarily kept in the kitchen's.
    private static string JarFor<TTaste>(string? inPantry = null)
    {
        var app = Assembly.GetEntryAssembly()!.GetName().Name!;
        var taste = typeof(TTaste).FullName!.Replace('+', '.').Replace('`', '.');
        return Path.Combine(inPantry ?? pantry, $"{app}.{taste}.json".ToLowerInvariant());
    }

    [TestMethod]
    public void AFreshTasteComesFromItsPropertyInitialisers()
    {
        Assert.AreEqual("chocolate", Cook.Serve<Fresh>().Filling);
        Assert.AreEqual("vanilla cream", Cook.Serve<AlsoFresh>().Filling);
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

        var jar = JarFor<Preserved>();
        Assert.IsTrue(File.Exists(jar), "the kitchen's pantry was not used");
        Assert.AreEqual(
            new Preserved { Savored = "I was preserved!" },
            JsonSerializer.Deserialize<Preserved>(File.ReadAllText(jar)));
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
            JarFor<Kept>(), JsonSerializer.Serialize(new Kept { Stored = "kept" }));

        Assert.AreEqual("kept", Cook.Serve<Kept>().Stored);
    }

    [TestMethod]
    public void ACorruptJarThrowsRatherThanStartingOver()
    {
        Directory.CreateDirectory(pantry);
        File.WriteAllText(JarFor<Corrupt>(), "{ this is not json");

        Assert.Throws<JsonException>(() => Cook.Serve<Corrupt>());
    }

    [TestMethod]
    public void TastesWithTheSameShortNameGetTheirOwnJar()
    {
        Cook.Preserve(new Twin { Where = "nested in the test class" });
        Cook.Preserve(new Cupboard.Twin { Where = "in the cupboard" });

        Assert.AreNotEqual(JarFor<Twin>(), JarFor<Cupboard.Twin>());
        Assert.AreEqual("nested in the test class", Cook.Serve<Twin>().Where);
        Assert.AreEqual("in the cupboard", Cook.Serve<Cupboard.Twin>().Where);
    }

    [TestMethod]
    public void ANestedTasteIsNamedWithDotsRatherThanAPlus()
    {
        // Type.FullName says Taste.Tests.CookTests+Twin; a jar should not.
        Assert.IsFalse(Path.GetFileName(JarFor<Twin>()).Contains('+'));
        StringAssert.Contains(Path.GetFileName(JarFor<Twin>()), "cooktests.twin");
    }

    [TestMethod]
    public void ATasteWithAPantryOfItsOwnIsKeptThere()
    {
        Cook.Preserve(new Elsewhere { Where = "in a pantry of my own" });

        Assert.IsTrue(
            File.Exists(JarFor<Elsewhere>(otherPantry)), "the taste's own pantry was not used");
        Assert.IsFalse(
            File.Exists(JarFor<Elsewhere>()), "the taste was kept in the kitchen's pantry too");
        Assert.AreEqual("in a pantry of my own", Cook.Serve<Elsewhere>().Where);
    }

    [TestMethod]
    public void ATasteWithoutOneStaysInTheKitchensPantry()
    {
        Cook.Preserve(new AtHome { Where = "with the rest" });

        Assert.IsTrue(File.Exists(JarFor<AtHome>()), "the kitchen's pantry was not used");
    }

    [TestMethod]
    public void APantryOfItsOwnIsMadeWhenTheTasteIsPreserved()
    {
        // Preserve creates the pantry it writes into — which has to be the taste's own,
        // not the kitchen's, or the write lands in a directory that is not there.
        Assert.IsFalse(Directory.Exists(madePantry), "nothing to prove: it already exists");

        Cook.Preserve(new Created { Where = "somewhere that had to be made" });

        Assert.IsTrue(File.Exists(JarFor<Created>(madePantry)));
    }

    [TestMethod]
    public void TheKitchenKeepsItsOwnCopyOfThePantries()
    {
        // A kitchen is fixed once built, so the dictionary handed in cannot be a way
        // back in to move a pantry afterwards.
        var handedIn = new Dictionary<Type, string> { [typeof(Elsewhere)] = "first" };
        var kitchen = new Kitchen { Pantry = pantry, Pantries = handedIn };

        handedIn[typeof(Elsewhere)] = "second";
        handedIn[typeof(AtHome)] = "sneaked in";

        Assert.AreEqual("first", kitchen.Pantries[typeof(Elsewhere)]);
        Assert.IsFalse(kitchen.Pantries.ContainsKey(typeof(AtHome)));
    }

    [TestMethod]
    public void UseKitchenAfterTheCookHasStartedThrows()
    {
        // AssemblyInitialize already served this process's kitchen.
        Assert.Throws<InvalidOperationException>(
            () => Cook.UseKitchen(new Kitchen { Pantry = Path.GetTempPath() }));
    }
}
