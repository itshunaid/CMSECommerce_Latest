using System.Threading.Tasks;

namespace CMSECommerce.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
