using System;
using Papermail.Core.Entities;
using Xunit;

namespace Papermail.Core.Tests;

public class AttachmentTests
{
    [Fact]
    public void Constructor_ValidArguments_CreatesInstance()
    {
        var attachment = new Attachment("file.txt", 1234, "text/plain");
        Assert.Equal("file.txt", attachment.FileName);
        Assert.Equal(1234, attachment.SizeBytes);
        Assert.Equal("text/plain", attachment.ContentType);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_EmptyFileName_ThrowsArgumentException(string fileName)
    {
        Assert.Throws<ArgumentException>(() => new Attachment(fileName, 100, "text/plain"));
    }

    [Fact]
    public void Constructor_NegativeSize_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Attachment("file.txt", -1, "text/plain"));
    }

    [Fact]
    public void Constructor_EmptyContentType_DefaultsToOctetStream()
    {
        var attachment = new Attachment("file.txt", 100, "");
        Assert.Equal("application/octet-stream", attachment.ContentType);
    }
}
