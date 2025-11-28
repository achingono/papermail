using Papermail.Core.Validation;
using Papermail.Core.Entities;
using Xunit;

namespace Papermail.Core.Tests;

public class ProviderValidatorTests
{
    private readonly ProviderValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        var provider = new Provider { Name = "" };
        var result = _validator.Validate(provider);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Name_Is_Valid()
    {
        var provider = new Provider { Name = "Microsoft" };
        var result = _validator.Validate(provider);
        Assert.True(result.IsValid);
    }
}
