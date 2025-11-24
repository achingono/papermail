using FluentAssertions;
using PaperMail.Infrastructure.Authentication;

namespace PaperMail.Infrastructure.Tests;

public class PkceHelperTests
{
    [Fact]
    public void GenerateCodeVerifier_ReturnsNonEmptyString()
    {
        // Act
        var verifier = PkceHelper.GenerateCodeVerifier();

        // Assert
        verifier.Should().NotBeNullOrEmpty();
        verifier.Length.Should().BeGreaterThanOrEqualTo(43);
        verifier.Length.Should().BeLessThanOrEqualTo(128);
    }

    [Fact]
    public void GenerateCodeVerifier_ReturnsUrlSafeBase64()
    {
        // Act
        var verifier = PkceHelper.GenerateCodeVerifier();

        // Assert
        verifier.Should().NotContain("+");
        verifier.Should().NotContain("/");
        verifier.Should().NotContain("=");
        verifier.Should().MatchRegex("^[A-Za-z0-9_-]+$");
    }

    [Fact]
    public void GenerateCodeVerifier_GeneratesUniqueValues()
    {
        // Act
        var verifier1 = PkceHelper.GenerateCodeVerifier();
        var verifier2 = PkceHelper.GenerateCodeVerifier();

        // Assert
        verifier1.Should().NotBe(verifier2);
    }

    [Fact]
    public void GenerateCodeChallenge_ReturnsNonEmptyString()
    {
        // Arrange
        var verifier = PkceHelper.GenerateCodeVerifier();

        // Act
        var challenge = PkceHelper.GenerateCodeChallenge(verifier);

        // Assert
        challenge.Should().NotBeNullOrEmpty();
        challenge.Should().MatchRegex("^[A-Za-z0-9_-]+$");
    }

    [Fact]
    public void GenerateCodeChallenge_IsDeterministic()
    {
        // Arrange
        var verifier = PkceHelper.GenerateCodeVerifier();

        // Act
        var challenge1 = PkceHelper.GenerateCodeChallenge(verifier);
        var challenge2 = PkceHelper.GenerateCodeChallenge(verifier);

        // Assert
        challenge1.Should().Be(challenge2);
    }

    [Fact]
    public void GenerateCodeChallenge_DifferentForDifferentVerifiers()
    {
        // Arrange
        var verifier1 = PkceHelper.GenerateCodeVerifier();
        var verifier2 = PkceHelper.GenerateCodeVerifier();

        // Act
        var challenge1 = PkceHelper.GenerateCodeChallenge(verifier1);
        var challenge2 = PkceHelper.GenerateCodeChallenge(verifier2);

        // Assert
        challenge1.Should().NotBe(challenge2);
    }

    [Fact]
    public void GenerateCodeChallenge_ProducesValidSHA256Hash()
    {
        // Arrange
        var verifier = "test-verifier-with-known-value";

        // Act
        var challenge = PkceHelper.GenerateCodeChallenge(verifier);

        // Assert - SHA256 base64url encoded is 43 characters
        challenge.Length.Should().Be(43);
        challenge.Should().NotContain("=");
    }
}
