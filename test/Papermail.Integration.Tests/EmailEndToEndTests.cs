using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Xunit;

namespace Papermail.Integration.Tests;

/// <summary>
/// End-to-end integration tests that verify email sending and receiving
/// through the Docker mail server.
/// Set RUN_DOCKER_TESTS=true to enable these tests.
/// </summary>
[Collection("Docker")]
public class EmailEndToEndTests
{
    private readonly bool _runTests;
    private const string SmtpHost = "localhost";
    private const int SmtpPort = 587;
    private const string ImapHost = "localhost";
    private const int ImapPort = 143;
    private const string TestUser = "admin@papermail.local";
    private const string TestPassword = "P@ssw0rd";

    public EmailEndToEndTests()
    {
        _runTests = Environment.GetEnvironmentVariable("RUN_DOCKER_TESTS") == "true";
    }

    [Fact]
    public async Task CanSendEmail_ThroughSmtpServer()
    {
        if (!_runTests) return;

        using var client = new SmtpClient();
        
        try
        {
            // Connect to SMTP server
            await client.ConnectAsync(SmtpHost, SmtpPort, SecureSocketOptions.StartTls);
            client.AuthenticationMechanisms.Remove("XOAUTH2");
            await client.AuthenticateAsync(TestUser, TestPassword);

            // Create test message
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Test Sender", TestUser));
            message.To.Add(new MailboxAddress("Test Recipient", TestUser));
            message.Subject = $"Integration Test - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
            message.Body = new TextPart("plain")
            {
                Text = "This is a test email from integration tests."
            };

            // Send email
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            Assert.True(true, "Email sent successfully");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Failed to send email: {ex.Message}");
        }
    }

    [Fact]
    public async Task CanReceiveEmails_ThroughImapServer()
    {
        if (!_runTests) return;

        using var client = new ImapClient();
        
        try
        {
            // Connect to IMAP server
            await client.ConnectAsync(ImapHost, ImapPort, SecureSocketOptions.StartTls);
            client.AuthenticationMechanisms.Remove("XOAUTH2");
            await client.AuthenticateAsync(TestUser, TestPassword);

            // Open inbox
            var inbox = client.Inbox;
            await inbox.OpenAsync(MailKit.FolderAccess.ReadOnly);

            Assert.True(inbox.Count >= 0, "Should be able to read inbox message count");

            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Failed to receive emails: {ex.Message}");
        }
    }

    [Fact]
    public async Task CanSendAndReceiveEmail_EndToEnd()
    {
        if (!_runTests) return;

        var uniqueSubject = $"E2E Test - {Guid.NewGuid()}";

        // Step 1: Send email via SMTP
        using (var smtpClient = new SmtpClient())
        {
            await smtpClient.ConnectAsync(SmtpHost, SmtpPort, SecureSocketOptions.StartTls);
            smtpClient.AuthenticationMechanisms.Remove("XOAUTH2");
            await smtpClient.AuthenticateAsync(TestUser, TestPassword);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Integration Test", TestUser));
            message.To.Add(new MailboxAddress("Integration Test", TestUser));
            message.Subject = uniqueSubject;
            message.Body = new TextPart("plain")
            {
                Text = "This email tests the complete send and receive cycle."
            };

            await smtpClient.SendAsync(message);
            await smtpClient.DisconnectAsync(true);
        }

        // Wait a moment for email to be delivered
        await Task.Delay(2000);

        // Step 2: Retrieve email via IMAP
        using (var imapClient = new ImapClient())
        {
            await imapClient.ConnectAsync(ImapHost, ImapPort, SecureSocketOptions.StartTls);
            imapClient.AuthenticationMechanisms.Remove("XOAUTH2");
            await imapClient.AuthenticateAsync(TestUser, TestPassword);

            var inbox = imapClient.Inbox;
            await inbox.OpenAsync(MailKit.FolderAccess.ReadOnly);

            // Search for our test email
            var found = false;
            for (int i = inbox.Count - 1; i >= Math.Max(0, inbox.Count - 10); i--)
            {
                var message = await inbox.GetMessageAsync(i);
                if (message.Subject == uniqueSubject)
                {
                    found = true;
                    break;
                }
            }

            await imapClient.DisconnectAsync(true);

            Assert.True(found, "Sent email should be retrievable via IMAP");
        }
    }
}
