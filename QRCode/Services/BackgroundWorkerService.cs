
namespace QRCode.Services;

public class BackgroundWorkerService : BackgroundService
{
    private readonly ILogger<BackgroundWorkerService> _logger;
    private int _executionCount = 0;
    private TimeSpan _period;

    public BackgroundWorkerService(ILogger<BackgroundWorkerService> logger)
    {
        _logger = logger;
        _period = TimeSpan.FromSeconds(10);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new PeriodicTimer(_period);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                _executionCount++;
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss")} - Executed TodoTimedBackgroundService - Count: {_executionCount}");
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Failed to execute TodoTimedBackgroundService with exception message {ex.Message}.");
            }
        }
    }
}
