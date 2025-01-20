using ElectronBot.Standalone.Core.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ElectronBot.Standalone.Core.Services;

public class PeriodicTaskService : BackgroundService
{
    private readonly IBotPlayer _botPlayer;
    private readonly ILogger<PeriodicTaskService> _logger;
    private Timer? _timer;

    public PeriodicTaskService(IBotPlayer botPlayer, ILogger<PeriodicTaskService> logger)
    {
        _botPlayer = botPlayer;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("YourBackgroundService is starting.");

        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

        stoppingToken.Register(() => _logger.LogInformation("YourBackgroundService is stopping."));

        return Task.CompletedTask;
    }
    private void DoWork(object? state)
    {
        _logger.LogInformation("YourBackgroundService is doing background work.");
        // 在这里添加你的定时任务逻辑
        //await _botPlayer.PlayEmojiToMainScreenAsync("smile");
    }
    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("YourBackgroundService is stopping.");

        _timer?.Change(Timeout.Infinite, 0);

        await base.StopAsync(stoppingToken);
    }

    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}
