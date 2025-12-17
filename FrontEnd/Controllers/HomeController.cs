using FrontEnd.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;

namespace FrontEnd.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> WiFi()
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("BackendApi");
            var response = await httpClient.GetAsync("/wifi");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Backend returned status code: {StatusCode}", response.StatusCode);
                return View(new List<WifiViewModel>());
            }
            
            var data = await response.Content.ReadAsStringAsync();
            
            if (string.IsNullOrWhiteSpace(data))
            {
                return View(new List<WifiViewModel>());
            }
            
            return View(JsonSerializer.Deserialize<List<WifiViewModel>>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling backend API");
            return View(new List<WifiViewModel>());
        }
    }

    public IActionResult CreateWifiQR()
    {
        return View("WifiForm", new WifiViewModel());
    }

    public async Task<IActionResult> EditarAsync(int id)
    {
        var httpClient = _httpClientFactory.CreateClient("BackendApi");
        var respose = await httpClient.GetAsync($"/wifi/{id}");
        var data = await respose.Content.ReadAsStringAsync();

        var model = JsonSerializer.Deserialize<WifiViewModel>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return View("WifiForm", model);
    }

    public async Task<IActionResult> Borrar(string id)
    {
        var httpClient = _httpClientFactory.CreateClient("BackendApi");
        await httpClient.DeleteAsync("/wifi/" + id);
        return RedirectToAction("WiFi");
    }

    public async Task<IActionResult> Imprimir(string id)
    {
        var httpClient = _httpClientFactory.CreateClient("BackendApi");
        var respose = await httpClient.GetAsync($"/wifi/{id}/print/");
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