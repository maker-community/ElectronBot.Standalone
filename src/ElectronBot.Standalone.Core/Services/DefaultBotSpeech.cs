using ElectronBot.Standalone.Core.Contracts;
using ElectronBot.Standalone.Core.Models;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Runtime.InteropServices;

namespace ElectronBot.Standalone.Core.Services;

public class DefaultBotSpeech : IBotSpeech, IDisposable
{
    private SpeechRecognizer keywordRecognizer;
    private SpeechRecognizer continuousRecognizer;
    private KeywordRecognitionModel keywordModel;
    private bool isKeywordDetected = false;
    private bool _disposed = false;
    private SpeechSynthesizer synthesizer;

    public event EventHandler? KeywordRecognized;
    public event EventHandler? ContinuousRecognitionStarted;
    public event EventHandler? SpeechPlaybackCompleted;
    public event EventHandler<string>? ContinuousRecognitionCompleted;

    public DefaultBotSpeech(BotSpeechSetting setting)
    {
        var config = SpeechConfig.FromSubscription(setting.SubscriptionKey, setting.Region);
        config.SpeechSynthesisVoiceName = setting.SpeechSynthesisVoiceName;
        config.SpeechRecognitionLanguage = setting.SpeechRecognitionLanguage;
        config.SpeechSynthesisLanguage = setting.SpeechSynthesisLanguage;

        using var audioConfig = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ?
            AudioConfig.FromMicrophoneInput(setting.MicrophoneInput) : AudioConfig.FromDefaultMicrophoneInput();

        keywordRecognizer = new SpeechRecognizer(config, audioConfig);
        continuousRecognizer = new SpeechRecognizer(config, audioConfig);
        synthesizer = new SpeechSynthesizer(config);
        keywordModel = KeywordRecognitionModel.FromFile(Path.Combine(AppContext.BaseDirectory, setting.KeywordModelFilePath));

        // 订阅事件
        keywordRecognizer.Recognized += KeywordRecognizer_Recognized;
        keywordRecognizer.Canceled += KeywordRecognizer_Canceled;
        keywordRecognizer.SessionStopped += KeywordRecognizer_SessionStopped;

        continuousRecognizer.Recognized += ContinuousRecognizer_Recognized;
        continuousRecognizer.Canceled += ContinuousRecognizer_Canceled;
        continuousRecognizer.SessionStopped += ContinuousRecognizer_SessionStopped;
    }

    public async Task KeywordWakeupAndDialogAsync()
    {
        // 启动关键字识别
        await StartKeywordRecognitionAsync();
    }

    public async Task StartKeywordRecognitionAsync()
    {
        Console.WriteLine($"等待关键字 \"小娜\"...");
        await keywordRecognizer.StartKeywordRecognitionAsync(keywordModel);
    }

    public async Task StartContinuousRecognitionAsync()
    {
        Console.WriteLine("请说话...");
        ContinuousRecognitionStarted?.Invoke(this, EventArgs.Empty);
        await continuousRecognizer.StartContinuousRecognitionAsync();
    }

    public async Task PlayTextToSpeakerAsync(string text)
    {
        using (var result = await synthesizer.SpeakTextAsync(text))
        {
            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                Console.WriteLine($"Speech synthesized to speaker for text [{text}]");
                SpeechPlaybackCompleted?.Invoke(this, EventArgs.Empty);
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                    Console.WriteLine($"CANCELED: Did you update the subscription info?");
                }
            }
        }
    }

    private async void KeywordRecognizer_Recognized(object? sender, SpeechRecognitionEventArgs e)
    {
        if (e.Result.Reason == ResultReason.RecognizedKeyword)
        {
            Console.WriteLine($"关键字检测到: {e.Result.Text}");
            isKeywordDetected = true;
            await keywordRecognizer.StopKeywordRecognitionAsync();
            KeywordRecognized?.Invoke(this, EventArgs.Empty);
            //await StartContinuousRecognitionAsync();
        }
    }

    private async void ContinuousRecognizer_Recognized(object? sender, SpeechRecognitionEventArgs e)
    {
        if (e.Result.Reason == ResultReason.RecognizedSpeech)
        {
            Console.WriteLine($"识别到: {e.Result.Text}");
            // 在这里处理用户的输入文本
            await continuousRecognizer.StopContinuousRecognitionAsync();
            isKeywordDetected = false;
            ContinuousRecognitionCompleted?.Invoke(this, e.Result.Text);
            //await StartKeywordRecognitionAsync();
        }
        else if (e.Result.Reason == ResultReason.NoMatch)
        {
            Console.WriteLine("未识别到有效语音");
        }
    }

    private async void ContinuousRecognizer_Canceled(object? sender, SpeechRecognitionCanceledEventArgs e)
    {
        Console.WriteLine($"识别取消: 原因={e.Reason}");
        if (e.Reason == CancellationReason.Error)
        {
            Console.Error.WriteLine($"错误代码: {e.ErrorCode}");
            Console.Error.WriteLine($"错误详情: {e.ErrorDetails}");
            if (!isKeywordDetected)
            {
                await StartKeywordRecognitionAsync();
            }
        }
    }

    private async void KeywordRecognizer_Canceled(object? sender, SpeechRecognitionCanceledEventArgs e)
    {
        Console.WriteLine($"关键词识别取消: 原因={e.Reason}");
        if (e.Reason == CancellationReason.Error)
        {
            Console.Error.WriteLine($"错误代码: {e.ErrorCode}");
            Console.Error.WriteLine($"错误详情: {e.ErrorDetails}");
            if (!isKeywordDetected)
            {
                await StartKeywordRecognitionAsync();
            }
        }
    }

    private void KeywordRecognizer_SessionStopped(object? sender, SessionEventArgs e)
    {
        Console.WriteLine("关键词会话结束");
    }

    private async void ContinuousRecognizer_SessionStopped(object? sender, SessionEventArgs e)
    {
        Console.WriteLine("会话结束");
        if (!isKeywordDetected)
        {
            await StartKeywordRecognitionAsync();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // 释放托管资源
                keywordRecognizer?.Dispose();
                continuousRecognizer?.Dispose();
                synthesizer?.Dispose();
                keywordModel?.Dispose();
            }

            // 释放非托管资源（如果有）

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
