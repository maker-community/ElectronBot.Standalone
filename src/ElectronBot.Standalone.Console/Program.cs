using ElectronBot.Standalone.Core.Contracts;
using ElectronBot.Standalone.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
        services.AddSingleton<IBotPlayer, DefaultBotPlayer>()
                .AddSingleton<IBotSpeech, DefaultBotSpeech>()
                .AddHostedService<PeriodicTaskService>())
    .Build();

var botPlayer = host.Services.GetRequiredService<IBotPlayer>();

var botSpeech = host.Services.GetRequiredService<IBotSpeech>();

botSpeech.KeywordRecognized += BotSpeech_KeywordRecognized;

botSpeech.SpeechPlaybackCompleted += BotSpeech_SpeechPlaybackCompleted;

botSpeech.ContinuousRecognitionStarted += BotSpeech_ContinuousRecognitionStarted;
botSpeech.ContinuousRecognitionCompleted += BotSpeech_ContinuousRecognitionCompleted;

//while (true)
//{
//    await botPlayer.PlayEmojiToMainScreenAsync("speak");
//    await botPlayer.PlayEmojiToMainScreenAsync("think");
//    await botPlayer.PlayEmojiToMainScreenAsync("look");
//    await botPlayer.PlayEmojiToMainScreenAsync("ask");
//}


await botSpeech.KeywordWakeupAndDialogAsync();

await host.RunAsync();


async void BotSpeech_KeywordRecognized(object? sender, EventArgs e)
{
    var playEmojiTask = botPlayer.PlayEmojiToMainScreenAsync("ask");
    var playTextTask = botSpeech.PlayTextToSpeakerAsync("主人我在呢");

    await Task.WhenAll(playEmojiTask, playTextTask);

    await botSpeech.StartContinuousRecognitionAsync();
}

async void BotSpeech_ContinuousRecognitionCompleted(object? sender, string e)
{
    Console.WriteLine($"用户的问题：{e}");
    await botPlayer.PlayEmojiToMainScreenAsync("think");
    var playEmojiTask = botPlayer.PlayEmojiToMainScreenAsync("speak");
    var playTextTask = botSpeech.PlayTextToSpeakerAsync($"我是一段示例的问答回答，我是晓晓的声音，但是我是小娜，我的声音很甜，我的主人是绿荫阿广。主人的问题是{e}");

    await Task.WhenAll(playEmojiTask, playTextTask);
}
void BotSpeech_ContinuousRecognitionStarted(object? sender, EventArgs e)
{
    botPlayer.PlayEmojiToMainScreenAsync("look");
}

void BotSpeech_SpeechPlaybackCompleted(object? sender, EventArgs e)
{
    Console.WriteLine("音频播放完成");
}
