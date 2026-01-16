using Microsoft.AspNetCore.Identity.UI.Services;

namespace CMSECommerce.Services
{
    public interface IAccountNotificationService
    {
        Task SendUnlockStatusEmailAsync(string email, string userName, string status, string adminNotes = "");
    }

    public class AccountNotificationService : IAccountNotificationService
    {
        private readonly IEmailSender _emailSender; // Your existing SMTP/SendGrid wrapper

        public AccountNotificationService(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public async Task SendUnlockStatusEmailAsync(string email, string userName, string status, string adminNotes = "")
        {
            string subject = status == "Approved" ? "Account Restored - WEypaari" : "Update on your Unlock Request - WEypaari";
            string body = status == "Approved"
                ? GetApprovedHtml(userName)
                : GetRejectedHtml(userName, adminNotes);

            await _emailSender.SendEmailAsync(email, subject, body);
        }

        private string GetApprovedHtml(string userName) => $@"
        <div style=""font-family: 'Segoe UI', Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;"">
            <div style=""background-color: #232f3e; padding: 20px; text-align: center;"">
                <h2 style=""color: #ffffff; margin: 0;"">WE<span style=""color: #e47911;"">y</span>paari</h2>
            </div>
            <div style=""padding: 40px; background-color: #ffffff;"">
                <h1 style=""color: #28a745; font-size: 24px; margin-top: 0;"">Your account is ready, {userName}!</h1>
                <p style=""color: #555; line-height: 1.6;"">Our team has reviewed and <strong>unlocked your account</strong>. You can now sign in using your existing credentials.</p>
                <div style=""text-align: center; margin: 30px 0;"">
                    <a href=""https://weypaari.com/Account/Login"" style=""background-color: #f0c14b; border: 1px solid #a88734; color: #111; padding: 12px 30px; text-decoration: none; border-radius: 3px; font-weight: bold; display: inline-block;"">Sign In Now</a>
                </div>
            </div>
        </div>";

        private string GetRejectedHtml(string userName, string notes) => $@"
        <div style=""font-family: 'Segoe UI', Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;"">
            <div style=""background-color: #232f3e; padding: 20px; text-align: center;"">
                <h2 style=""color: #ffffff; margin: 0;"">WE<span style=""color: #e47911;"">y</span>paari</h2>
            </div>
            <div style=""padding: 40px; background-color: #ffffff;"">
                <h1 style=""color: #d9534f; font-size: 24px; margin-top: 0;"">Unlock Request Update</h1>
                <p style=""color: #555; line-height: 1.6;"">Hello {userName}, we are unable to restore access to your account at this time.</p>
                <div style=""background-color: #f8f9fa; border-left: 4px solid #d9534f; padding: 15px; margin: 20px 0;"">
                    <p style=""margin: 0; font-size: 14px; color: #333;""><strong>Reason:</strong> {notes}</p>
                </div>
                <p style=""color: #555; font-size: 13px;"">Please contact support@weypaari.com for manual verification.</p>
            </div>
        </div>";
    }
}
