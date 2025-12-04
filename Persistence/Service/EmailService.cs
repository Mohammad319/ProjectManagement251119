using Application.Interfaces.Email;
using Application.Model;
using Domain.Settings;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Persistence.Service
{
    public class EmailService : IEmailService
    {
        private readonly MailSettings _mailSettings;

        public EmailService(IOptions<MailSettings> tokenOptions)
        {
            _mailSettings = tokenOptions.Value;
        }
        public async Task SendEmailAsync(MailRequest request)
        {
            using (var client = new SmtpClient())
            {
                var credential = new NetworkCredential
                {
                    UserName = _mailSettings.Mail,
                    Password = _mailSettings.Password,
                };

                client.Credentials = credential;
                client.Host = _mailSettings.Host;
                client.Port = _mailSettings.Port;
                client.EnableSsl = true;

                using (var emailMessage = new MailMessage())
                {
                    emailMessage.To.Add(new MailAddress(request.ToEmail));
                    emailMessage.From = new MailAddress(_mailSettings.Mail, request.DisplayName ?? _mailSettings.DisplayName);
                    emailMessage.Subject = request.Subject;
                    emailMessage.Body = request.Body;
                    emailMessage.IsBodyHtml = true;
                    if (!string.IsNullOrEmpty(request.Attachment))
                        emailMessage.Attachments.Add(new Attachment(request.Attachment));
                    await Task.Delay(5);
                    //await client.SendMailAsync(emailMessage);
                }
            }
        }
        public async Task SendEmailAsync(MailRequest request, MailSettings mailSettings)
        {
            using (var client = new SmtpClient())
            {
                var credential = new NetworkCredential
                {
                    UserName = mailSettings.Mail,
                    Password = mailSettings.Password,
                };

                client.Credentials = credential;
                client.Host = mailSettings.Host;
                client.Port = mailSettings.Port;
                client.EnableSsl = true;

                using (var emailMessage = new MailMessage())
                {
                    emailMessage.To.Add(new MailAddress(request.ToEmail));
                    emailMessage.From = new MailAddress(mailSettings.Mail, mailSettings.DisplayName);
                    emailMessage.Subject = request.Subject;
                    emailMessage.Body = request.Body;
                    //emailMessage.Attachments.Add(new Attachment("C:\\file.zip"));
                    //await client.SendMailAsync(emailMessage);
                    await Task.Delay(5);
                }
            }
        }
    }
}
