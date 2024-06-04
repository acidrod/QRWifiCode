namespace QRCode.Services;

public interface IEmailService
{
    Task SendEmail(string to, string subject, string body, string qrCodeBase64String);
}
