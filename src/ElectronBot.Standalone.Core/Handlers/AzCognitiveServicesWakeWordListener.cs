using ElectronBot.Standalone.Core.Contracts;
using ElectronBot.Standalone.Core.Models;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
            _ = Task.Run(() => _botPlayer.PlayEmojiToMainScreenAsync("look"));
            _logger.LogInformation($"Waiting for wake phrase...");
            result = await _keywordRecognizer.RecognizeOnceAsync(_keywordModel);
            _logger.LogInformation("Wake phrase detected.");
            _logger.LogDebug($"{result.Reason}");
        } while (result.Reason != ResultReason.RecognizedKeyword);
        return true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _audioConfig.Dispose();
    }
}