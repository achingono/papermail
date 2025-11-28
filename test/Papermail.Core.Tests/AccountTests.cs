using System;
using Papermail.Core.Entities;
using Xunit;

namespace Papermail.Core.Tests;

public class AccountTests
{
    [Fact]
    public void Account_DefaultValues_AreCorrect()
    {
        var account = new Account();
        Assert.NotEqual(Guid.Empty, account.Id);
        Assert.Equal(string.Empty, account.UserId);
        Assert.Equal(string.Empty, account.EmailAddress);
        Assert.Equal(string.Empty, account.RefreshToken);
        Assert.True(account.IsActive);
        Assert.NotNull(account.Scopes);
        Assert.Empty(account.Scopes);
        Assert.True(account.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Account_SetProperties_UpdatesValues()
    {
        var provider = new Provider { Name = "TestProvider" };
        var account = new Account
        {
            UserId = "user-id",
            EmailAddress = "user@example.com",
            Provider = provider,
            ProviderId = provider.Id,
            RefreshToken = "refresh-token",
            AccessToken = "access-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            DisplayName = "User Name",
            IsActive = false
        };
        account.Scopes.Add("email.read");
        Assert.Equal("user-id", account.UserId);
        Assert.Equal("user@example.com", account.EmailAddress);
        Assert.Equal(provider, account.Provider);
        Assert.Equal(provider.Id, account.ProviderId);
        Assert.Equal("refresh-token", account.RefreshToken);
        Assert.Equal("access-token", account.AccessToken);
        Assert.NotNull(account.ExpiresAt);
        Assert.Equal("User Name", account.DisplayName);
        Assert.False(account.IsActive);
        Assert.Contains("email.read", account.Scopes);
    }
}
