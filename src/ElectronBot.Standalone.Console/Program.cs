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

await host.RunAsync();

var botPlayer = host.Services.GetRequiredService<IBotPlayer>();

await botPlayer.PlayEmojiToMainScreenAsync("activity");

await Task.Delay(1000);

await botPlayer.PlayEmojiToMainScreenAsync("ask");