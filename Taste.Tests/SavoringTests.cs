using Microsoft.VisualStudio.TestTools.UnitTesting;
using Taste.Savoring;

namespace Taste.Tests;

[TestClass]
public class SavoringTests
{
    public record Savored
    {
        public string How { get; init; } = "not yet";
    }

    [TestMethod]
    public void SavoringATasteIsPreservingIt()
    {
        new Savored { How = "the diner's way" }.Savor();

        Assert.AreEqual("the diner's way", Cook.Serve<Savored>().How);
    }
}
