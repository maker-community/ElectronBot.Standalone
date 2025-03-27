using ElectronBot.Standalone.Core.Contracts;
using ElectronBot.Standalone.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
        services.AddSingleton<IBotPlayer, DefaultBotPlayer>())
    .Build();

var botPlayer = host.Services.GetRequiredService<IBotPlayer>();

await host.RunAsync();