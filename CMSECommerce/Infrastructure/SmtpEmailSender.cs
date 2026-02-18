using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MailKit.Security;
using MailKit.Net.Smtp;
using MimeKit;

namespace CMSECommerce.Infrastructure
{
 public class SmtpEmailSender : IEmailSender
 {
 private readonly IConfiguration _config;
 private readonly ILogger<SmtpEmailSender> _logger;

 public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
 {
 _config = config;
 _logger = logger;
 }

 public async Task SendEmailAsync(string email, string subject, string htmlMessage)
 {
 await SendEmailInternalAsync(email, subject, htmlMessage);
 }

 // Send using a named template. Templates are simple strings with {0} placeholders.
 public async Task SendTemplateAsync(string email, string subject, string template, params object[] args)
 {
 var body = string.Format(template ?? string.Empty, args);
 await SendEmailInternalAsync(email, subject, body);
 }

 private async Task SendEmailInternalAsync(string to, string subject, string htmlMessage)
 {
 try
 {
 var section = _config.GetSection("EmailSettings");
 var host = section["SmtpServer"] ?? _config["Smtp:Host"];
 var portStr = section["SmtpPort"] ?? _config["Smtp:Port"];
 var user = section["Username"] ?? section["SenderEmail"] ?? _config["Smtp:Username"];
 var pass = section["SenderPassword"] ?? _config["Smtp:Password"];
 var from = section["SenderEmail"] ?? user;
 var displayName = section["SenderName"] ?? from;
 var enableSslStr = section["EnableSsl"] ?? _config["Smtp:EnableSsl"];

 var port = int.TryParse(portStr, out var p) ? p : 587;
 var enableSsl = bool.TryParse(enableSslStr, out var s) ? s : true;

 var message = new MimeMessage();
 message.From.Add(new MailboxAddress(displayName, from));
 message.To.Add(MailboxAddress.Parse(to));
 message.Subject = subject;
 message.Body = new BodyBuilder { HtmlBody = htmlMessage }.ToMessageBody();

 using var client = new MailKit.Net.Smtp.SmtpClient();
 client.Timeout = 30000;

 await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
 await client.AuthenticateAsync(user, pass);
 await client.SendAsync(message);
 await client.DisconnectAsync(true);

 _logger.LogInformation("Email sent to {Email} subject={Subject}", to, subject);
 }
 catch (SmtpCommandException ex)
 {
 _logger.LogError(ex, "SMTP command failed: {StatusCode} - {Message}", ex.StatusCode, ex.Message);
 throw;
 }
 catch (SmtpProtocolException ex)
 {
 _logger.LogError(ex, "SMTP protocol error: {Message}", ex.Message);
 throw;
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Unexpected error sending email to {Email}", to);
 throw;
 }
 }
 }
}
