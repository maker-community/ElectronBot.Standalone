using ElectronBot.Standalone.Core.Contracts;
using ElectronBot.Standalone.Core.Models;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace ElectronBot.Standalone.Core.Handlers;
public class AzBotSpeecher : IBotSpeecher
{
    private readonly ILogger _logger;
    private readonly BotSpeechSetting _options;
    private readonly AudioConfig _audioConfig;
    private readonly SpeechRecognizer _speechRecognizer;
    private readonly SpeechSynthesizer _speechSynthesizer;
    private readonly IMemoryCache _memoryCache;
    private readonly IBotPlayer _botPlayer;
    /// <summary>
    /// Regex for extracting style cues from OpenAI responses.
    /// (not currently supported after the migrations to ChatGPT models)
    /// </summary>
    private static readonly Regex _styleRegex = new Regex(@"(~~(.+)~~)");

    public string Provider => "AzureVoice";

    public AzBotSpeecher(ILogger<AzBotSpeecher> logger, IMemoryCache memoryCache, BotSpeechSetting options, IBotPlayer botPlayer)
    {
        _logger = logger;
        _memoryCache = memoryCache;
        _options = options;
        _botPlayer = botPlayer;
        _audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        SpeechConfig speechConfig = SpeechConfig.FromSubscription(_options.SubscriptionKey, _options.Region);
        speechConfig.SpeechRecognitionLanguage = _options.SpeechRecognitionLanguage;
        speechConfig.SetProperty(PropertyId.SpeechServiceResponse_PostProcessingOption, "TrueText");
        speechConfig.SpeechSynthesisVoiceName = _options.SpeechSynthesisVoiceName;

        _speechRecognizer = new SpeechRecognizer(speechConfig, _audioConfig);
        _speechSynthesizer = new SpeechSynthesizer(speechConfig);
    }

    public async Task<string> ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
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

                _logger.LogInformation("Listening...");

                var result = await _speechRecognizer.RecognizeOnceAsync();
                switch (result.Reason)
                {
                    case ResultReason.RecognizedSpeech:
                        _logger.LogInformation($"Recognized: {result.Text}");
                        // 停止动画
                        await _botPlayer.StopLottiePlaybackAsync();
                        return result.Text;
                    case ResultReason.NoMatch:
                        _logger.LogWarning("No speech could be recognized.");
                        break;
                    case ResultReason.Canceled:
                        _logger.LogWarning("Speech recognizer session canceled.");

                        CancellationDetails cancelDetails = CancellationDetails.FromResult(result);
                        _logger.LogWarning($"{cancelDetails.Reason}: {cancelDetails.ErrorCode}");
                        _logger.LogDebug(cancelDetails.ToString());
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception during speech recognition: {ex.Message}");
            }
        }
        return string.Empty;
    }
    public async Task SpeakAsync(string text, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            try
            {
                // 启动动画但不阻塞当前执行流程
                var animationTask = _botPlayer.PlayLottieByNameIdAsync("speak", -1);

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
                _logger.LogError($"Failed to start animation: {ex.Message}");
                // 根据需要处理异常
            }

            // 移除text中的emojis表情
            text = RemoveEmojis(text);

            // Parse speaking style, if any
            text = ExtractStyle(text, out var style);
            if (string.IsNullOrWhiteSpace(style))
            {
                _logger.LogInformation($"Speaking (none): {text}");
            }
            else
            {
                _logger.LogInformation($"Speaking ({style}): {text}");
            }

            var ssml = GenerateCoquettishSsml(
                text,
                _options.SpeechSynthesisVoiceName);

            _logger.LogDebug(ssml);
            await _speechSynthesizer.SpeakSsmlAsync(ssml);
            // 停止动画
            await _botPlayer.StopLottiePlaybackAsync();
        }
    }

    private string RemoveEmojis(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // 匹配Unicode Emoji的正则表达式
        // 包括:
        // - 基本Emoji符号
        // - 扩展Emoji符号
        // - 补充符号
        // - 国旗等特殊组合
        return Regex.Replace(
            text,
            @"(\u00a9|\u00ae|[\u2000-\u3300]|\ud83c[\ud000-\udfff]|\ud83d[\ud000-\udfff]|\ud83e[\ud000-\udfff]|[\u2702-\u27b0])",
            string.Empty);
    }
    /// <summary>
    /// Extract style cues from a message.
    /// </summary>
    private string ExtractStyle(string message, out string style)
    {
        style = string.Empty;
        Match match = _styleRegex.Match(message);
        if (match.Success)
        {
            style = match.Groups[2].Value.Trim();
            message = message.Replace(match.Groups[1].Value, string.Empty).Trim();
        }
        return message;
    }

    /// <summary>
    /// Generate speech synthesis markup language (SSML) from a message.
    /// </summary>
    private string GenerateCoquettishSsml(string message, string voiceName)
        => "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xmlns:mstts=\"https://www.w3.org/2001/mstts\" xml:lang=\"zh-CN\">" +
            $"<voice name=\"{voiceName}\">" +
                "<prosody rate=\"1.1\" pitch=\"high\">" +
                    "<mstts:express-as style=\"cheerful\">" +
                        $"{message}" +
                    "</mstts:express-as>" +
                "</prosody>" +
            "</voice>" +
        "</speak>";

    public void Dispose()
    {
        _speechRecognizer?.Dispose();
        _audioConfig?.Dispose();
        GC.SuppressFinalize(this);
    }
}
