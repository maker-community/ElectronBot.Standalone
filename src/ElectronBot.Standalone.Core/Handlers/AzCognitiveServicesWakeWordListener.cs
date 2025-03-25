using ElectronBot.Standalone.Core.Contracts;
using ElectronBot.Standalone.Core.Models;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ElectronBot.Standalone.Core.Handlers;

/// <summary>
/// A wake word listener using Azure Cognitive Services keyword recognition.
/// </summary>
public class AzCognitiveServicesWakeWordListener : IWakeWordListener
{
    private readonly ILogger _logger;
    private readonly BotSpeechSetting _options;
    private readonly AudioConfig _audioConfig;
    private readonly KeywordRecognizer _keywordRecognizer;
    private readonly KeywordRecognitionModel _keywordModel;
    private readonly IMemoryCache _memoryCache;
    private readonly IBotPlayer _botPlayer;
    public AzCognitiveServicesWakeWordListener(
        BotSpeechSetting options,
        ILogger<AzCognitiveServicesWakeWordListener> logger,
        IMemoryCache memoryCache,
        IBotPlayer botPlayer)
    {
        _logger = logger;
        _options = options;
        _keywordModel = KeywordRecognitionModel.FromFile(Path.Combine(AppContext.BaseDirectory, _options.KeywordModelFilePath));

        _audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        _keywordRecognizer = new KeywordRecognizer(_audioConfig);
        _memoryCache = memoryCache;
        _botPlayer = botPlayer;
    }

    /// <summary>
    /// Wait for the wake word or phrase to be detected before returning.
    /// </summary>
    public async Task<bool> WaitForWakeWordAsync(CancellationToken cancellationToken)
    {
        KeywordRecognitionResult result;
        do
        {
            try
            {
                // 启动动画但不阻塞当前执行流程
                var animationTask = _botPlayer.PlayLottieByNameIdAsync("look", -1);

                // 可以选择添加异常处理
                animationTask?.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        _logger.LogError($"Animation playback failed: {t.Exception}");
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
            catch (Exception ex)
            {
                await _botPlayer.StopLottiePlaybackAsync();
                _logger.LogError($"Failed to start animation: {ex.Message}");
                // 根据需要处理异常
            }
            _logger.LogInformation($"Waiting for wake phrase...");
            result = await _keywordRecognizer.RecognizeOnceAsync(_keywordModel);
            _logger.LogInformation("Wake phrase detected.");
            _logger.LogDebug($"{result.Reason}");
            await _botPlayer.StopLottiePlaybackAsync();
        } while (result.Reason != ResultReason.RecognizedKeyword);
        return true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _audioConfig.Dispose();
    }
}