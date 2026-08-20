namespace Taste.Tests.Cupboard;

// Same short name as Taste.Tests.CookTests.Twin, in a different namespace. Under the old
// scheme these two shared one dish.
public record Twin
{
    public string Where { get; init; } = "nowhere";
}
