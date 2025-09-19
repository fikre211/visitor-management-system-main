using GatePass.MS.Domain;
using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace GatePass.MS.ClientApp.Controllers
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
        Task SendEmailWithAttachementAsync(string email, string subject, string htmlMessage, System.Net.Mail.Attachment attachment);
    }

    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;

        public SmtpEmailSender(EmailSettings emailSettings)
        {
            _emailSettings = emailSettings;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            Console.WriteLine($"Email sending Async:");

            var smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.Port)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password),
                EnableSsl = _emailSettings.UseSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromAddress),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

            try
            {
                return smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // Log exception to a file or monitoring system
                Console.WriteLine($"Email sending failed: {ex.Message}");
                throw;
            }
        }

        public Task SendEmailWithAttachementAsync(string email, string subject, string htmlMessage, System.Net.Mail.Attachment attachment)
        {
            Console.WriteLine($"Email sending Async:");

            var smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.Port)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password),
                EnableSsl = _emailSettings.UseSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromAddress),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

            if (attachment != null)
            {
                mailMessage.Attachments.Add(attachment);
            }

            try
            {
                return smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // Log exception to a file or monitoring system
                Console.WriteLine($"Email sending failed: {ex.Message}");
                throw;
            }

        }
    }
}