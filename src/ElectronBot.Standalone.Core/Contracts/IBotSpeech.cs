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
}
