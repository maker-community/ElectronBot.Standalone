using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectronBot.Standalone.Core.Contracts;

public interface IBotSpeech
{
    Task KeywordWakeupAndDialogAsync();
    Task StartKeywordRecognitionAsync();
    Task StartContinuousRecognitionAsync();
    Task PlayTextToSpeakerAsync(string text);

    // 定义事件
    event EventHandler KeywordRecognized;
    event EventHandler ContinuousRecognitionStarted;
    event EventHandler SpeechPlaybackCompleted; // 声音播放完成事件
    event EventHandler<string> ContinuousRecognitionCompleted; // ContinuousRecognitionStarted识别完成事件
}
