using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using QRCode.Models;
using QRCode.Services;
using QRCoder;
using System.Text.Json;

string Policy = "MyPolicy";
string storedWifiEncrypted = $"{Directory.GetCurrentDirectory()}\\Data\\WifiData.cry";

var builder = WebApplication.CreateBuilder(args);
var encrypyionKey = builder.Configuration.GetSection("EncryptionKey").Value;
var NameFrom = builder.Configuration.GetSection("EmailSender:NameFrom").Value;
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddHostedService<BackgroundWorkerService>(); //TIMED BackgroundWorker
builder.Services.AddSingleton<IBackgroundQueue<Email>, BackgroundMailQueue<Email>>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHostedService<BackgroundMailQueueService>(); //QUEUED BackgroundWorker
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "QRCode", 
        Version = "v1", 
        Description = "WebAPI para creación de códigos QR, almacenamiento de datos y despacho de emails en cola usando un BackgroundWorker" });
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

app.MapGet("/wifi", () =>
{
    var data = GetAllWifi();

    if (data == null)
        return Results.NotFound();

    return Results.Ok(data);
});

app.MapGet("/wifi/{id}", (int id) =>
{
    var data = GetWifiById(id);

    if (data == null)
        return Results.NotFound();

    return Results.Ok(data);
});

app.MapGet("/wifi/{id}/print", (int id) => GetBase64QrCode(GetWifiById(id)));

app.MapPost("/wifi", ([FromServices]IConfiguration configuration, [FromServices]IBackgroundQueue<Email> queue, WifiParams wifiParams) =>
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
});

app.MapPost("/wifi/{id}", (int id) =>
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
});

app.MapPut("/wifi/{id}", ([FromServices] IConfiguration configuration, [FromServices] IBackgroundQueue<Email> queue, int id, WifiParams wifiParams) =>
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
});

app.MapDelete("/wifi/{id}", (int id) =>
{
    var existingData = GetAllWifi();
    var toDelete = existingData.FirstOrDefault(c => c.Id == id);

    if (toDelete != null)
    {
        existingData.Remove(toDelete);
        WriteWifiData(existingData);
    }

    return Results.NoContent();
});

List<WifiParams> GetAllWifi()
{
    var base64ExistingDate = File.Exists(storedWifiEncrypted) ? File.ReadAllText(storedWifiEncrypted) : string.Empty;
    List<WifiParams> existingDataList = new();

    if (string.IsNullOrEmpty(base64ExistingDate))
        return existingDataList;

    var securityService = new RandomIvEncryptionService(Convert.FromBase64String(builder.Configuration.GetSection("EncryptionKey").Value));
    var decripted = securityService.Decrypt(base64ExistingDate);
    existingDataList = JsonSerializer.Deserialize<List<WifiParams>>(decripted)!;

    return existingDataList;
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
    var securityService = new RandomIvEncryptionService(Convert.FromBase64String(encrypyionKey));
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

app.Run();