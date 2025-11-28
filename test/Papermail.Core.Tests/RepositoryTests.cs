using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq.Expressions;
using Papermail.Core.Entities;
using Xunit;
using Moq;

namespace Papermail.Core.Tests;

public class RepositoryTests
{
    [Fact]
    public async Task AddAsync_AddsEntity()
    {
        var repo = new Mock<IRepository<Account>>();
        var account = new Account { UserId = "user", EmailAddress = "user@example.com" };
        repo.Setup(r => r.AddAsync(account, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        var result = await repo.Object.AddAsync(account, CancellationToken.None);
        Assert.Equal(account, result);
    }

    [Fact]
    public async Task DeleteAsync_DeletesEntity()
    {
        var repo = new Mock<IRepository<Account>>();
        var account = new Account { UserId = "user", EmailAddress = "user@example.com" };
        repo.Setup(r => r.DeleteAsync(account, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var result = await repo.Object.DeleteAsync(account, CancellationToken.None);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsEntity()
    {
        var repo = new Mock<IRepository<Account>>();
        var account = new Account { Id = Guid.NewGuid(), UserId = "user", EmailAddress = "user@example.com" };
        repo.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        var result = await repo.Object.GetByIdAsync(account.Id, CancellationToken.None);
        Assert.Equal(account, result);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEntity()
    {
        var repo = new Mock<IRepository<Account>>();
        var account = new Account { UserId = "user", EmailAddress = "user@example.com" };
        repo.Setup(r => r.UpdateAsync(account, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        var result = await repo.Object.UpdateAsync(account, CancellationToken.None);
        Assert.Equal(account, result);
    }

    [Fact]
    public async Task UpdateAsync_WithId_UpdatesEntity()
    {
        var repo = new Mock<IRepository<Account>>();
        var account = new Account { Id = Guid.NewGuid(), UserId = "user", EmailAddress = "user@example.com" };
        repo.Setup(r => r.UpdateAsync(account.Id, account, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        var result = await repo.Object.UpdateAsync(account.Id, account, CancellationToken.None);
        Assert.Equal(account, result);
    }
}
