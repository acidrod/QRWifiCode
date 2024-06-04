using MailKit.Net.Smtp;
using MimeKit;

namespace QRCode.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmail(string toEmail, string subject, string body, string qrCodeBase64String)
    {
        //Convert Base64 string to byte array
        var imageByteArray = Convert.FromBase64String(qrCodeBase64String);

        //Email message
        var email = new MimeMessage();
        var builder = new BodyBuilder();

        email.From.Add(new MailboxAddress(_configuration.GetSection("EmailSender:NameFrom").Value, _configuration.GetSection("EmailSender:EmailFrom").Value));
        email.To.Add(new MailboxAddress(toEmail, toEmail));
        email.Subject = subject;

        //Attachments
        builder.Attachments.Add("qr.png", imageByteArray);
        builder.HtmlBody = body;

        email.Body = builder.ToMessageBody();

        //Authentication
        using var googleSmtp = new SmtpClient();
        googleSmtp.Connect(_configuration.GetSection("EmailSender:SmtpServer").Value, int.Parse(_configuration.GetSection("EmailSender:SmtpPort").Value), false);
        await googleSmtp.AuthenticateAsync(_configuration.GetSection("EmailSender:EmailFrom").Value, _configuration.GetSection("EmailSender:EmailAccountPassword").Value);

        await googleSmtp.SendAsync(email);
        googleSmtp.Disconnect(true);
    }
}