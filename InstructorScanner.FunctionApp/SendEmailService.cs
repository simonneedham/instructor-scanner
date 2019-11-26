using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Net;

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

        public Task SendEmailAsync(string subject, IList<string> messageBody)
        {
            return SendEmailAsync(subject, messageBody, MimeType.Text);
        }

        public Task SendEmailAsync(string subject, IList<string> messageBody, string mimeType)
        {
            return SendEmailAsync(subject, BuildMessageBody(messageBody, mimeType), mimeType);
        }

        public async Task SendEmailAsync(string subject, string messageBody, string mimeType)
        {
            try
            {
                var message = new SendGridMessage();
                message.AddTo(_appSettings.Value.EmailAddress);
                message.SetFrom(_appSettings.Value.EmailAddress);
                message.SetSubject(subject);
                message.AddContent(mimeType, messageBody);

                var response = await _sendGridClient.SendEmailAsync(message);
                if (response == null) throw new Exception("Send Grid failed to return a response");

                var statusCode = (int)response.StatusCode;
                if(statusCode >= 200 && statusCode<= 299)
                {
                    _logger.LogInformation("Sent email with subject '{subject}' to {emailAddress} at {sentDateTime:dd-MMM-yyyy HH:mm:ss}", subject, _appSettings.Value.EmailAddress, DateTime.UtcNow);
                }
                else
                {
                    var errorMsg = $"SendGrid request failed. Status code {statusCode} was returned.";
                    
                    var responseBody = response.DeserializeResponseBody(response.Body);
                    if(responseBody != null && responseBody.TryGetValue("errors", out var errors))
                    {
                        errorMsg += "\n" + errors.ToString();
                    }

                    throw new Exception(errorMsg);
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Unable to send an email");
                throw;
            }
        }

        private string BuildMessageBody(IList<string> messageBody, string mimeType)
        {
            if (mimeType == MimeType.Text) return string.Join("\r\n", messageBody);

            var htmlParas = messageBody.Select(s => $"{s}<br>\r\n").ToList();
            return string.Join("", htmlParas);
        }
    }
}
