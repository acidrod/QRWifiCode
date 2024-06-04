using QRCode.Models;
using System.ComponentModel;

namespace QRCode.Services;

public class BackgroundMailQueueService : BackgroundService
{
    private readonly IBackgroundQueue<Email> _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundMailQueueService> _logger;

    public BackgroundMailQueueService(ILogger<BackgroundMailQueueService> logger, IServiceScopeFactory scopeFactory, IBackgroundQueue<Email> queue)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _queue = queue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{Type} is not in the background", nameof(BackgroundMailQueueService));
        await MailProcessing(stoppingToken);
    }

    private async Task MailProcessing(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                var email = _queue.Dequeue();
                
                if(email == null)
                    continue;

                _logger.LogInformation("Email item found! Completing it...");

                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                await emailService.SendEmail(email.To, email.Subject, email.Body,email.QrCodeBase64String);

                _logger.LogInformation("Email item is completed...");
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error occured while completing an email. Exception {@Exception}",ex);
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogCritical(
            "The {Type} is stopping; queued items might not be processed anymore.",
            nameof(BackgroundMailQueueService)
        );
        return base.StopAsync(cancellationToken);
    }
}
