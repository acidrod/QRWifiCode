using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FrontEnd.Models;

public class WifiViewModel
{
    public int Id { get; set; }

    private static readonly List<SelectListItem> _authProtocol = new()
    {
        new SelectListItem { Value = "WEP", Text = "WEP", Selected = true},
        new SelectListItem { Value = "WPA", Text = "WPA" },
        new SelectListItem { Value = "nopass", Text = "nopass" },
        new SelectListItem { Value = "WPA2", Text = "WPA2" }
    };

    [Required, DisplayName("Nombre Red:")]
    public string Ssid { get; set; } = string.Empty;

    [Required, DisplayName("Password:"), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, DisplayName("Protocolo:")]
    public string Auth { get; set; } = string.Empty;

    public List<SelectListItem> AuthProtocol
    {
        get
        {
            return _authProtocol;
        }
    }

    [DisplayName("Email:")]
    public string MailTo { get; set; } = string.Empty;
}