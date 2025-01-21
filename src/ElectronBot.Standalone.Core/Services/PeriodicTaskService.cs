using ElectronBot.Standalone.Core.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Verdure.Iot.Device;

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
    private async void DoWork(object? state)
    {
        _logger.LogInformation("YourBackgroundService is doing background work.");
        // 在这里添加你的定时任务逻辑
        using (Image<Bgra32> image1inch47 = Image.Load<Bgra32>("Asserts/verdure.png"))
        {
            var collection = new FontCollection();
            var family = collection.Add("Asserts/SmileySans-Oblique.ttf");
            var font = family.CreateFont(24, FontStyle.Bold);

            var textOptions = new TextOptions(font)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                WrappingLength = 320
            };
            // 获取当前时间
            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            var displayText = $"当前时间:{currentTime}";

            var size = TextMeasurer.MeasureSize(displayText, textOptions);

            var position = new PointF((LCD1inch47.Height - size.Width) / 2, 8);
            // 在图片上绘制时间
            image1inch47.Mutate(ctx => ctx.DrawText(displayText, font, Color.White, position));

            //await image1inch47.SaveAsPngAsync(Path.Combine($"frame_{DateTime.Now.Ticks}.png"));
            await _botPlayer.ShowImageToSubScreenAsync(image1inch47);
        }
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
