//using FluentCMS.Providers.Email.Abstractions;
//using Microsoft.Extensions.Options;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace FluentCMS.Providers.Email.Smtp;

//public sealed class SmtpEmailProvider(IOptionsMonitor<SmtpSettings> opt) : IEmailProvider
//{
//    private SmtpSettings Settings => opt.CurrentValue;

//    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
//    {
//        var msg = new MimeMessage();
//        msg.From.Add(new MailboxAddress(Settings.FromDisplayName ?? "", Settings.FromAddress));
//        msg.To.Add(MailboxAddress.Parse(to));
//        msg.Subject = subject;
//        msg.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

//        var retry: AsyncRetryPolicy = Policy
//            .Handle<Exception>()
//            .WaitAndRetryAsync(Settings.MaxRetries, attempt => TimeSpan.FromMilliseconds(500 * attempt));

//        await retry.ExecuteAsync(async token =>
//        {
//            using var client = new SmtpClient
//            {
//                Timeout = Settings.TimeoutMs
//            };

//            SecureSocketOptions socketOptions =
//                Settings.UseSsl ? SecureSocketOptions.SslOnConnect :
//                Settings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;

//            await client.ConnectAsync(Settings.Host, Settings.Port, socketOptions, token);

//            if (!string.IsNullOrEmpty(Settings.UserName))
//            {
//                await client.AuthenticateAsync(Settings.UserName, Settings.Password, token);
//            }

//            await client.SendAsync(msg, token);
//            await client.DisconnectAsync(true, token);
//        }, ct);
//    }
//}
