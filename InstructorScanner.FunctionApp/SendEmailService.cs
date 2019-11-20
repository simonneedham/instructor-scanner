using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace InstructorScanner.FunctionApp
{

    public interface ISendEmailService
    {
        Task SendEmailAsync(string subject, string messageBody);
        Task SendEmailAsync(string subject, string messageBody, string mimeType);
        Task SendEmailAsync(string subject, IList<string> messageBody);
        Task SendEmailAsync(string subject, IList<string> messageBody, string mimeType);
    }

    public class SendEmailService : ISendEmailService
    {
        private readonly IOptions<AppSettings> _appSettings;
        private readonly ILogger<SendEmailService> _logger;
        private readonly ISendGridClient _sendGridClient;

        public SendEmailService(
            IOptions<AppSettings> appSettings,
            ILogger<SendEmailService> logger,
            ISendGridClient sendGridClient
        )
        {
            _appSettings = appSettings;
            _logger = logger;
            _sendGridClient = sendGridClient;
        }

        public Task SendEmailAsync(string subject, string messageBody)
        {
            return SendEmailAsync(subject, messageBody, MimeType.Text);
        }

        public Task SendEmailAsync(string subject, string messageBody, string mimeType)
        {
            return SendEmailAsync(subject, new List<string> { messageBody }, mimeType);
        }

        public Task SendEmailAsync(string subject, IList<string> messageBody)
        {
            return SendEmailAsync(subject, messageBody, MimeType.Html);
        }

        public async Task SendEmailAsync(string subject, IList<string> messageBody, string mimeType)
        {
            try
            {
                var message = new SendGridMessage();
                message.AddTo(_appSettings.Value.EmailAddress);
                message.SetFrom(_appSettings.Value.EmailAddress);
                message.SetSubject(subject);

                foreach (var msgLine in messageBody)
                {
                    message.AddContent(mimeType, msgLine);
                }

                await _sendGridClient.SendEmailAsync(message);
                _logger.LogInformation("Sent email with subject '{subject}' to {emailAddress} at {sentDateTime:dd-MMM-yyyy HH:mm:ss}", subject, _appSettings.Value.EmailAddress, DateTime.UtcNow);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Unable to send an email");
                throw;
            }
        }
    }
}
