using ElectronBot.Standalone.Core.Contracts;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace ElectronBot.Standalone.Core.Services;

public class DefaultBotSpeech : IBotSpeech, IDisposable
{
    private SpeechRecognizer recognizer;
    private KeywordRecognitionModel keywordModel;
    private bool isKeywordDetected = false;
    private bool _disposed = false;
    private SpeechSynthesizer synthesizer;

    public DefaultBotSpeech()
    {
        var config = SpeechConfig.FromSubscription("key", "region");
        using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        recognizer = new SpeechRecognizer(config, audioConfig);
        synthesizer = new SpeechSynthesizer(config);
        keywordModel = KeywordRecognitionModel.FromFile("ModelFiles/keyword_cortana.table");
        // 订阅事件
        recognizer.Recognized += Recognizer_Recognized;
        recognizer.Canceled += Recognizer_Canceled;
        recognizer.SessionStopped += Recognizer_SessionStopped;

    }
    public async Task KeywordWakeupAndDialogAsync()
    {
        // 启动关键字识别
        await StartKeywordRecognitionAsync();
    }
    public async Task StartKeywordRecognitionAsync()
    {
        Console.WriteLine($"等待关键字 \"小娜\"...");
        await recognizer.StartKeywordRecognitionAsync(keywordModel);
    }

    public async Task StartContinuousRecognitionAsync()
    {
        Console.WriteLine("请说话...");
        await recognizer.StartContinuousRecognitionAsync();
    }

    public async Task PlayTextToSpeakerAsync(string text)
    {
        using (var result = await synthesizer.SpeakTextAsync(text))
        {
            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                Console.WriteLine($"Speech synthesized to speaker for text [{text}]");
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

    private async void Recognizer_Recognized(object? sender, SpeechRecognitionEventArgs e)
    {
        if (e.Result.Reason == ResultReason.RecognizedKeyword)
        {
            Console.WriteLine($"关键字检测到: {e.Result.Text}");
            isKeywordDetected = true;
            await PlayTextToSpeakerAsync("主人你想干嘛");
            //await recognizer.StopKeywordRecognitionAsync();
            //await StartContinuousRecognitionAsync();
        }
        else if (e.Result.Reason == ResultReason.RecognizedSpeech)
        {
            Console.WriteLine($"识别到: {e.Result.Text}");
            // 在这里处理用户的输入文本
            await recognizer.StopContinuousRecognitionAsync();
            isKeywordDetected = false;
            await StartKeywordRecognitionAsync();
        }
        else if (e.Result.Reason == ResultReason.NoMatch)
        {
            Console.WriteLine("未识别到有效语音");
        }
    }

    private void Recognizer_Canceled(object? sender, SpeechRecognitionCanceledEventArgs e)
    {
        Console.WriteLine($"识别取消: 原因={e.Reason}");
        if (e.Reason == CancellationReason.Error)
        {
            Console.Error.WriteLine($"错误代码: {e.ErrorCode}");
            Console.Error.WriteLine($"错误详情: {e.ErrorDetails}");
            if (!isKeywordDetected)
            {
                StartKeywordRecognitionAsync().Wait();
            }
        }
    }

    private void Recognizer_SessionStopped(object? sender, SessionEventArgs e)
    {
        Console.WriteLine("会话结束");
        if (!isKeywordDetected)
        {
            StartKeywordRecognitionAsync().Wait();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // 释放托管资源
                recognizer?.Dispose();
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
