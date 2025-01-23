using ElectronBot.Standalone.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IBotSpeech _botSpeech;
    private readonly ILogger<PeriodicTaskService> _logger;
    private Timer? _timer;
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    public PeriodicTaskService(IBotPlayer botPlayer,
        ILogger<PeriodicTaskService> logger,
        IServiceProvider serviceProvider, IBotSpeech botSpeech, IServiceScopeFactory serviceScopeFactory)
    {
        _botPlayer = botPlayer;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _botSpeech = botSpeech;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("YourBackgroundService is starting.");
        await ShowDateTimeAsync();
        _ = Task.Run(() => _botPlayer.PlayEmojiToMainScreenByJsonFileAsync("normal"));

        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

        stoppingToken.Register(() => _logger.LogInformation("YourBackgroundService is stopping."));

        var botPlayer = _serviceProvider.GetRequiredService<IBotPlayer>();

        var botSpeech = _serviceProvider.GetRequiredService<IBotSpeech>();

        botSpeech.KeywordRecognized += BotSpeech_KeywordRecognized;

        botSpeech.SpeechPlaybackCompleted += BotSpeech_SpeechPlaybackCompleted;

        botSpeech.ContinuousRecognitionStarted += BotSpeech_ContinuousRecognitionStarted;
        botSpeech.ContinuousRecognitionCompleted += BotSpeech_ContinuousRecognitionCompleted;
        await botSpeech.KeywordWakeupAndDialogAsync();
    }
    private async void DoWork(object? state)
    {
        _logger.LogInformation("YourBackgroundService is doing background work.");
        await ShowDateTimeAsync();
    }

    private async Task ShowDateTimeAsync()
    {
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

    async void BotSpeech_KeywordRecognized(object? sender, EventArgs e)
    {
        _ = Task.Run(() => _botPlayer.PlayEmojiToMainScreenByJsonFileAsync("anger"));
        await _botSpeech.PlayTextToSpeakerAsync("主人我在呢");
        await _botSpeech.StartContinuousRecognitionAsync();
    }

    async void BotSpeech_ContinuousRecognitionCompleted(object? sender, string e)
    {
        Console.WriteLine($"用户的问题：{e}");
        _ = Task.Run(() => _botPlayer.PlayEmojiToMainScreenByJsonFileAsync("anger"));

        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var botCopilot = scope.ServiceProvider.GetRequiredService<IBotCopilot>();
            var llmResult = await botCopilot.ChatToCopilotAsync(e);
            await _botSpeech.PlayTextToSpeakerAsync(llmResult);
            _ = Task.Run(() => _botPlayer.PlayEmojiToMainScreenByJsonFileAsync("anger"));
        }
        await _botSpeech.KeywordWakeupAndDialogAsync();
    }
    void BotSpeech_ContinuousRecognitionStarted(object? sender, EventArgs e)
    {
        _ = Task.Run(() => _botPlayer.PlayEmojiToMainScreenByJsonFileAsync("anger"));
    }

    void BotSpeech_SpeechPlaybackCompleted(object? sender, EventArgs e)
    {
        Console.WriteLine("音频播放完成");
    }
}
