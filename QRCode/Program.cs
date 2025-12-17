using Asp.Versioning;
using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Mvc;
using QRCode.Models;
using QRCode.Services;
using QRCoder;
using System.Text.Json;

string Policy = "MyPolicy";
string storedWifiEncrypted = Path.Combine(Directory.GetCurrentDirectory(), "Data", "WifiData.cry");

var builder = WebApplication.CreateBuilder(args);
var encrypyionKey = builder.Configuration.GetSection("EncryptionKey").Value;
var encryptionKeyBytes = ParseEncryptionKey(encrypyionKey);
var NameFrom = builder.Configuration.GetSection("EmailSender:NameFrom").Value;

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddHostedService<BackgroundWorkerService>(); //TIMED BackgroundWorker
builder.Services.AddSingleton<IBackgroundQueue<Email>, BackgroundMailQueue<Email>>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHostedService<BackgroundMailQueueService>(); //QUEUED BackgroundWorker
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "QRCode API", 
        Version = "v1", 
        Description = "WebAPI v1 para creación de códigos QR, almacenamiento de datos y despacho de emails en cola usando un BackgroundWorker" });
});

builder.Services.AddCors(opt =>
{
    opt.AddPolicy(Policy, build =>
    {
        build.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(Policy);

// API Version Set
var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

var v1 = app.MapGroup("/v1").WithApiVersionSet(apiVersionSet);

v1.MapGet("/wifi", (ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("GET /v1/wifi endpoint called");
        var data = GetAllWifi();
        logger.LogInformation("Retrieved {Count} wifi records", data?.Count ?? 0);

        if (data == null)
            return Results.NotFound();

        return Results.Ok(data);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in GET /v1/wifi endpoint");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
}).WithTags("WiFi");

v1.MapGet("/wifi/{id}", (int id) =>
{
    var data = GetWifiById(id);

    if (data == null)
        return Results.NotFound();

    return Results.Ok(data);
}).WithTags("WiFi");

v1.MapGet("/wifi/{id}/print", (int id) => GetBase64QrCode(GetWifiById(id))).WithTags("WiFi");

v1.MapPost("/wifi", ([FromServices]IConfiguration configuration, [FromServices]IBackgroundQueue<Email> queue, WifiParams wifiParams) =>
{
    var qrCode = GetBase64QrCode(wifiParams);

    List<WifiParams> existingDataList;

    existingDataList = GetAllWifi()!;

    try
    {
        wifiParams.Id = existingDataList.Last().Id + 1;
    }
    catch (Exception)
    {
        wifiParams.Id = 1;
    }

    existingDataList.Add(wifiParams);
    WriteWifiData(existingDataList);

    var email = new Email()
    {
        To = wifiParams.MailTo,
        Body = $"<h1>Este es tu código QR para tu Wifi</h1><p>Hola, te recomiendo guardar el QR e imprimirlo para que puedas usarlo fácilmente con tu visitas que quieran conectarse a tu Wifi.</p><br><p>Saludos {NameFrom}</p>",
        Subject = $"Código QR para tu Wifi {wifiParams.Ssid}",
        QrCodeBase64String = qrCode
    };

    queue.Enqueue(email);

    return qrCode;
}).WithTags("WiFi");

v1.MapPost("/wifi/{id}", (int id) =>
{
    var qrGenerator = new QRCodeGenerator();
    var storedWifi = GetWifiById(id);

    var authentication = Enum.Parse<PayloadGenerator.WiFi.Authentication>(storedWifi.Auth);

    var WiFiPayLoad = new PayloadGenerator.WiFi(
        storedWifi.Ssid,
        storedWifi.Password,
        authentication);

    QRCodeData qrCodeData = qrGenerator.CreateQrCode(WiFiPayLoad.ToString(), QRCodeGenerator.ECCLevel.Q);
    BitmapByteQRCode bitmapByteQRCode = new(qrCodeData);
    var bitmap = bitmapByteQRCode.GetGraphic(20);
    using var ms = new MemoryStream();
    ms.Write(bitmap);
    byte[] data = ms.ToArray();

    var base64String = Convert.ToBase64String(data);

    return base64String;
}).WithTags("WiFi");

v1.MapPut("/wifi/{id}", ([FromServices] IConfiguration configuration, [FromServices] IBackgroundQueue<Email> queue, int id, WifiParams wifiParams) =>
{
    var existingData = GetAllWifi();
    var toUpdate = existingData.FirstOrDefault(c => c.Id == id);

    if (toUpdate != null)
    {
        toUpdate.Ssid = wifiParams.Ssid;
        toUpdate.Password = wifiParams.Password;
        toUpdate.Auth = wifiParams.Auth;
        toUpdate.MailTo = wifiParams.MailTo;

        WriteWifiData(existingData);

        var qrCode = GetBase64QrCode(wifiParams);

        var email = new Email()
        {
            To = wifiParams.MailTo,
            Body = $"<h1>Este es tu código QR para tu Wifi</h1><p>Hola, te recomiendo guardar el QR e imprimirlo para que puedas usarlo fácilmente con tu visitas que quieran conectarse a tu Wifi.</p><br><p>Saludos {NameFrom}</p>",
            Subject = $"Código QR para tu Wifi {wifiParams.Ssid}",
            QrCodeBase64String = qrCode
        };

        queue.Enqueue(email);

        return qrCode;
    }

    return null;
}).WithTags("WiFi");

v1.MapDelete("/wifi/{id}", (int id) =>
{
    var existingData = GetAllWifi();
    var toDelete = existingData.FirstOrDefault(c => c.Id == id);

    if (toDelete != null)
    {
        existingData.Remove(toDelete);
        WriteWifiData(existingData);
    }

    return Results.NoContent();
}).WithTags("WiFi");

List<WifiParams> GetAllWifi()
{
    try
    {
        var base64ExistingDate = File.Exists(storedWifiEncrypted) ? File.ReadAllText(storedWifiEncrypted) : string.Empty;
        List<WifiParams> existingDataList = [];

        if (string.IsNullOrEmpty(base64ExistingDate))
        {
            app.Logger.LogInformation("No existing wifi data file found at {Path}", storedWifiEncrypted);
            return existingDataList;
        }

        if (encryptionKeyBytes == null)
            throw new Exception("Encryption Key is not configured.");

        var securityService = new RandomIvEncryptionService(encryptionKeyBytes);
        var decripted = securityService.Decrypt(base64ExistingDate);
        existingDataList = JsonSerializer.Deserialize<List<WifiParams>>(decripted)!;

        return existingDataList;
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error in GetAllWifi. File path: {Path}", storedWifiEncrypted);
        throw;
    }
}

WifiParams GetWifiById(int id)
{
    var validWifiData = GetAllWifi().FirstOrDefault(x => x.Id == id);
    if (validWifiData != null)
    {
        return validWifiData;
    }

    return new WifiParams();
}

void WriteWifiData(List<WifiParams> wifiParams)
{
    var json = JsonSerializer.Serialize(wifiParams);
    var securityService = new RandomIvEncryptionService(encryptionKeyBytes!);
    var encrypted = securityService.Encrypt(json);
    File.WriteAllText(storedWifiEncrypted, encrypted);
}

string GetBase64QrCode(WifiParams wifiParams)
{
    var qrGenerator = new QRCodeGenerator();
    var authentication = Enum.Parse<PayloadGenerator.WiFi.Authentication>(wifiParams.Auth);

    var WiFiPayLoad = new PayloadGenerator.WiFi(
        wifiParams.Ssid,
        wifiParams.Password,
        authentication);

    QRCodeData qrCodeData = qrGenerator.CreateQrCode(WiFiPayLoad.ToString(), QRCodeGenerator.ECCLevel.Q);
    BitmapByteQRCode bitmapByteQRCode = new(qrCodeData);
    var bitmap = bitmapByteQRCode.GetGraphic(20);
    using var ms = new MemoryStream();
    ms.Write(bitmap);
    byte[] data = ms.ToArray();
    var base64String = Convert.ToBase64String(data);

    return base64String;
}

byte[] ParseEncryptionKey(string? keyString)
{
    if (string.IsNullOrWhiteSpace(keyString))
        throw new InvalidOperationException("EncryptionKey is not configured");

    // Si la key viene en formato JSON array: [1,2,3,4,...]
    if (keyString.StartsWith("[") && keyString.EndsWith("]"))
    {
        var cleanString = keyString.Trim('[', ']');
        var numbers = cleanString.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return numbers.Select(n => byte.Parse(n.Trim())).ToArray();
    }

    // Si la key viene en formato Base64
    return Convert.FromBase64String(keyString);
}

app.Run();