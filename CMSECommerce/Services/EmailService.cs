using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Threading;

namespace CMSECommerce.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpSettings = _configuration.GetSection("EmailSettings");
            try
            {
                // Read settings
                var smtpServer = smtpSettings["SmtpServer"];
                var smtpPort = smtpSettings["SmtpPort"];
                var senderEmail = smtpSettings["SenderEmail"] ?? smtpSettings["Username"];
                var senderName = smtpSettings["SenderName"] ?? senderEmail;
                var senderPassword = smtpSettings["SenderPassword"];
                var enableSsl = smtpSettings["EnableSsl"];

                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(smtpPort) ||
                    string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
                {
                    throw new InvalidOperationException("SMTP settings are not properly configured.");
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = body };
                message.Body = bodyBuilder.ToMessageBody();

                var host = smtpServer;
                var port = int.TryParse(smtpPort, out var p) ? p : 587;

                await SendEmailWithRetryAndFallbackAsync(message, host, port, senderEmail, senderPassword, bool.Parse(enableSsl ?? "true"));

                _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            }
            catch (TimeoutException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                throw;
            }
        }

        private async Task SendEmailWithRetryAndFallbackAsync(MimeMessage message, string host, int port,
            string username, string password, bool enableSsl)
        {
            var maxRetries = 3;
            var baseDelayMs = 1000; // 1 second

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    using var client = new MailKit.Net.Smtp.SmtpClient();
                    client.Timeout = 30000; // 30s

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                    _logger.LogInformation("SMTP attempt {Attempt}/{MaxRetries} connecting to {Host}:{Port}",
                        attempt + 1, maxRetries, host, port);

                    // Try StartTLS first (port 587)
                    try
                    {
                        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, cts.Token);
                    }
                    catch (Exception ex) when (attempt == 0) // Only try fallback on first attempt
                    {
                        _logger.LogWarning(ex, "StartTLS failed, trying SSL on port 465");

                        // Fallback to SSL on port 465
                        try
                        {
                            await client.ConnectAsync(host, 465, SecureSocketOptions.SslOnConnect, cts.Token);
                        }
                        catch (Exception sslEx)
                        {
                            _logger.LogError(sslEx, "Both StartTLS and SSL connections failed");
                            throw new Exception("Failed to connect to SMTP server using both StartTLS and SSL", sslEx);
                        }
                    }

                    try
                    {
                        await client.AuthenticateAsync(username, password, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogError("SMTP authenticate timed out for {User}", username);
                        throw new TimeoutException("Timed out during SMTP authentication.");
                    }

                    try
                    {
                        await client.SendAsync(message, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogError("SMTP send timed out for recipient {To}", message.To);
                        throw new TimeoutException("Timed out while sending email via SMTP.");
                    }
                    finally
                    {
                        try { await client.DisconnectAsync(true); } catch { /* ignore */ }
                    }

                    _logger.LogInformation("Email sent successfully on attempt {Attempt}", attempt + 1);
                    return; // Success, exit retry loop
                }
                catch (TimeoutException)
                {
                    throw; // Don't retry timeout exceptions
                }
                catch (MailKit.Security.AuthenticationException)
                {
                    throw; // Don't retry authentication failures
                }
                catch (Exception ex) when (attempt < maxRetries - 1)
                {
                    // Exponential backoff for transient errors
                    var delayMs = baseDelayMs * Math.Pow(2, attempt);
                    _logger.LogWarning(ex, "SMTP attempt {Attempt} failed, retrying in {Delay}ms", attempt + 1, delayMs);
                    await Task.Delay((int)delayMs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "All SMTP retry attempts failed");
                    throw;
                }
            }
        }
    }
}
