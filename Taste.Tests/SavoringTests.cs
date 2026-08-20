using Microsoft.VisualStudio.TestTools.UnitTesting;
using Taste.Savoring;

namespace Taste.Tests;

[TestClass]
public class SavoringTests
{
    public record Savored(string How);

    [TestMethod]
    public void SavoringATasteIsPreservingIt()
    {
        var taste = new Savored("the diner's way");

        taste.Savor();

        Assert.AreEqual(new Savored("the diner's way"), Cook.Serve<Savored>());
    }
}
