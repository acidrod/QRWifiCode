namespace QRCode.Models;

public class WifiParams
{
    public int Id { get; set; }
    public string Ssid { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public string MailTo { get; set; } = string.Empty;
}
