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

//await botPlayer.PlayEmojiToMainScreenAsync("fish");

//await Task.Delay(1000);

await botPlayer.PlayEmojiToMainScreenAsync("ask");

var botSpeech = host.Services.GetRequiredService<IBotSpeech>();

await botSpeech.PlayTextToSpeakerAsync("主人你好呀");

await botSpeech.KeywordWakeupAndDialogAsync();

await host.RunAsync();
