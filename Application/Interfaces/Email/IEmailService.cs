using Application.Model;
using Domain.Settings;
using System.Threading.Tasks;

namespace Application.Interfaces.Email
{
    public interface IEmailService
    {
        Task SendEmailAsync(MailRequest request);
        Task SendEmailAsync(MailRequest request, MailSettings mailSettings);
    }
}
