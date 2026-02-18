using System.Threading.Tasks;

namespace CMSECommerce.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        
        /// <summary>
        /// Send email with optional attachment
        /// </summary>
        /// <param name="toEmail">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body (HTML supported)</param>
        /// <param name="attachmentPath">Full path to attachment file (optional)</param>
        Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body, string attachmentPath = null);
    }
}
