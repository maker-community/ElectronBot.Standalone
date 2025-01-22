namespace ElectronBot.Standalone.Core.Models;

public class BotSpeechSetting
{
    public string SubscriptionKey { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string SpeechSynthesisVoiceName { get; set; } = string.Empty;
    public string SpeechRecognitionLanguage { get; set; } = string.Empty;
    public string SpeechSynthesisLanguage { get; set; } = string.Empty;
    public string MicrophoneInput { get; set; } = string.Empty;
    public string KeywordModelFilePath { get; set; } = string.Empty;
}
