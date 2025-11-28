using System;
using Papermail.Core.Entities;
using Xunit;

namespace Papermail.Core.Tests;

public class ProviderTests
{
    [Fact]
    public void Provider_DefaultValues_AreCorrect()
    {
        var provider = new Provider();
        Assert.NotEqual(Guid.Empty, provider.Id);
        Assert.Equal(string.Empty, provider.Name);
        Assert.NotNull(provider.Accounts);
        Assert.Empty(provider.Accounts);
    }

    [Fact]
    public void Provider_SetName_UpdatesValue()
    {
        var provider = new Provider { Name = "Microsoft" };
        Assert.Equal("Microsoft", provider.Name);
    }

    [Fact]
    public void Provider_AddAccount_UpdatesCollection()
    {
        var provider = new Provider { Name = "Google" };
        var account = new Account { UserId = "user", EmailAddress = "user@google.com", Provider = provider };
        provider.Accounts.Add(account);
        Assert.Single(provider.Accounts);
        Assert.Equal(account, provider.Accounts.First());
    }
}
