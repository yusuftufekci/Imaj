using Imaj.Core.Interfaces.Repositories;
using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Service.Options;
using Imaj.Service.Results;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Imaj.Service.Services
{
    public class EmailService : BaseService, IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(
            IUnitOfWork unitOfWork,
            ILogger<EmailService> logger,
            IConfiguration configuration,
            IOptions<EmailSettings> emailSettings)
            : base(unitOfWork, logger, configuration)
        {
            _emailSettings = emailSettings.Value ?? new EmailSettings();
        }

        public async Task<ServiceResult> SendAsync(EmailMessageDto message, CancellationToken cancellationToken = default)
        {
            if (message == null)
            {
                return ServiceResult.Fail("E-posta bilgisi boş olamaz.");
            }

            if (string.IsNullOrWhiteSpace(message.To))
            {
                return ServiceResult.Fail("E-posta adresi zorunludur.");
            }

            if (string.IsNullOrWhiteSpace(message.Subject))
            {
                return ServiceResult.Fail("E-posta konusu zorunludur.");
            }

            if (string.IsNullOrWhiteSpace(message.HtmlBody))
            {
                return ServiceResult.Fail("E-posta içeriği zorunludur.");
            }

            SmtpSettingsSnapshot smtpSettings;
            try
            {
                smtpSettings = ResolveSmtpSettings();
            }
            catch (InvalidOperationException)
            {
                return ServiceResult.Fail("Şirket e-posta ayarları eksik.");
            }

            if (!TryParseRecipients(message.To, out var recipients))
            {
                return ServiceResult.Fail("Geçerli bir e-posta adresi giriniz.");
            }

            try
            {
                var mailMessage = new MimeMessage();
                mailMessage.From.Add(new MailboxAddress(smtpSettings.FromName, smtpSettings.FromAddress));
                mailMessage.Subject = message.Subject.Trim();

                foreach (var recipient in recipients)
                {
                    mailMessage.To.Add(recipient);
                }

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = message.HtmlBody
                };

                foreach (var attachment in message.Attachments.Where(x => x.Content.Length > 0))
                {
                    var fileName = string.IsNullOrWhiteSpace(attachment.FileName)
                        ? "attachment"
                        : attachment.FileName.Trim();
                    var contentType = string.IsNullOrWhiteSpace(attachment.ContentType)
                        ? "application/octet-stream"
                        : attachment.ContentType.Trim();

                    bodyBuilder.Attachments.Add(fileName, attachment.Content, ContentType.Parse(contentType));
                }

                mailMessage.Body = bodyBuilder.ToMessageBody();

                using var smtpClient = new SmtpClient();
                await smtpClient.ConnectAsync(
                    smtpSettings.Host,
                    smtpSettings.Port,
                    ResolveSocketOptions(smtpSettings),
                    cancellationToken);

                if (!string.IsNullOrWhiteSpace(smtpSettings.Username))
                {
                    await smtpClient.AuthenticateAsync(
                        smtpSettings.Username,
                        smtpSettings.Password,
                        cancellationToken);
                }

                await smtpClient.SendAsync(mailMessage, cancellationToken);
                await smtpClient.DisconnectAsync(true, cancellationToken);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "E-posta gönderilirken hata oluştu. Host={Host}, Port={Port}, Recipient={Recipient}",
                    smtpSettings.Host,
                    smtpSettings.Port,
                    message.To);
                return ServiceResult.Fail("E-posta gönderilirken bir hata oluştu.");
            }
        }

        private SmtpSettingsSnapshot ResolveSmtpSettings()
        {
            var host = _emailSettings.Host?.Trim() ?? string.Empty;
            var fromAddress = !string.IsNullOrWhiteSpace(_emailSettings.FromAddress)
                ? _emailSettings.FromAddress.Trim()
                : _emailSettings.Username.Trim();
            var username = !string.IsNullOrWhiteSpace(_emailSettings.Username)
                ? _emailSettings.Username.Trim()
                : fromAddress;

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(fromAddress) ||
                string.IsNullOrWhiteSpace(username))
            {
                throw new InvalidOperationException("Şirket e-posta ayarları eksik.");
            }

            return new SmtpSettingsSnapshot
            {
                Host = host,
                Port = _emailSettings.Port > 0 ? _emailSettings.Port : 587,
                EnableSsl = _emailSettings.EnableSsl,
                Username = username,
                Password = _emailSettings.Password ?? string.Empty,
                FromAddress = fromAddress,
                FromName = ResolveFallbackFromName()
            };
        }

        private static SecureSocketOptions ResolveSocketOptions(SmtpSettingsSnapshot settings)
        {
            if (settings.Port == 465)
            {
                return SecureSocketOptions.SslOnConnect;
            }

            return settings.EnableSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;
        }

        private string ResolveFallbackFromName()
        {
            return !string.IsNullOrWhiteSpace(_emailSettings.FromName)
                ? _emailSettings.FromName.Trim()
                : "Imaj";
        }

        private static bool TryParseRecipients(string rawRecipients, out List<MailboxAddress> recipients)
        {
            recipients = new List<MailboxAddress>();

            var parts = rawRecipients
                .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (parts.Count == 0)
            {
                return false;
            }

            foreach (var part in parts)
            {
                try
                {
                    recipients.Add(MailboxAddress.Parse(part));
                }
                catch
                {
                    recipients.Clear();
                    return false;
                }
            }

            return recipients.Count > 0;
        }

        private sealed class SmtpSettingsSnapshot
        {
            public string Host { get; init; } = string.Empty;
            public int Port { get; init; }
            public bool EnableSsl { get; init; }
            public string Username { get; init; } = string.Empty;
            public string Password { get; init; } = string.Empty;
            public string FromAddress { get; init; } = string.Empty;
            public string FromName { get; init; } = string.Empty;
        }
    }
}
