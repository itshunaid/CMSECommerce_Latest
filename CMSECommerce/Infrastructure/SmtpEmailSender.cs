using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

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
 var host = _config["Smtp:Host"];
 var port = int.TryParse(_config["Smtp:Port"], out var p) ? p :25;
 var user = _config["Smtp:Username"];
 var pass = _config["Smtp:Password"];
 var from = _config["Smtp:From"] ?? user;
 var enableSsl = bool.TryParse(_config["Smtp:EnableSsl"], out var s) ? s : true;

 using var client = new SmtpClient(host, port)
 {
 EnableSsl = enableSsl,
 Credentials = new NetworkCredential(user, pass),
 Timeout =10000
 };
 using var mail = new MailMessage(from, to, subject, htmlMessage) { IsBodyHtml = true };
 await client.SendMailAsync(mail);
 _logger.LogInformation("Email sent to {Email} subject={Subject}", to, subject);
 }
 catch (SmtpException ex)
 {
 _logger.LogError(ex, "SMTP error sending email to {Email}", to);
 // swallow or rethrow depending on desired behavior
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Unexpected error sending email to {Email}", to);
 }
 }
 }
}
