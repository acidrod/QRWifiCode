using FrontEnd.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;

namespace FrontEnd.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> WiFi()
    {
        HttpClient httpClient = new();
        var response = await httpClient.GetAsync("https://localhost:7044/wifi");
        var data = await response.Content.ReadAsStringAsync();
        return View(JsonSerializer.Deserialize<List<WifiViewModel>>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!);
    }

    public IActionResult CreateWifiQR()
    {
        return View("WifiForm", new WifiViewModel());
    }

    public async Task<IActionResult> EditarAsync(int id)
    {
        HttpClient httpClient = new();
        var respose = await httpClient.GetAsync($"https://localhost:7044/wifi/{id}");
        var data = await respose.Content.ReadAsStringAsync();

        var model = JsonSerializer.Deserialize<WifiViewModel>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return View("WifiForm", model);
    }

    public async Task<IActionResult> Borrar(string id)
    {
        HttpClient httpClient = new();
        await httpClient.DeleteAsync("https://localhost:7044/wifi/" + id);
        return RedirectToAction("WiFi");
    }

    public async Task<IActionResult> Imprimir(string id)
    {
        HttpClient httpClient = new();
        var respose = await httpClient.GetAsync($"https://localhost:7044/wifi/{id}/print/");
        var data = await respose.Content.ReadAsStringAsync();

        var viewModel = new ImprimirViewModel()
        {
            QrCode = data
        };

        return View("ImprimirPage", viewModel);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}