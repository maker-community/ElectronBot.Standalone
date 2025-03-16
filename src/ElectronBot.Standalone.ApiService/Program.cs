using BotSharp.Abstraction.Messaging.JsonConverters;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Users;
using BotSharp.Core;
using BotSharp.Logger;
using ElectronBot.Standalone.Core.Contracts;
using ElectronBot.Standalone.Core.Handlers;
using ElectronBot.Standalone.Core.Models;
using ElectronBot.Standalone.Core.Repositories;
using ElectronBot.Standalone.Core.Services;
using ElectronBot.Standalone.DataStorage;
using ElectronBot.Standalone.DataStorage.Repository;
using Verdure.Braincase.Copilot.Plugin.Services.BotSharp;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var dbSettings = new BotSharpDatabaseSettings();
builder.Configuration.Bind("Database", dbSettings);
builder.Services.AddSingleton(dbSettings);

var brainSettings = new BraincaseDatabaseSettings();
builder.Configuration.Bind("Database", brainSettings);

brainSettings.BraincaseLiteDB = "braincase.db";

builder.Services.AddSingleton(brainSettings);

var botSpeechSettings = new BotSpeechSetting();
builder.Configuration.Bind("BotSpeechSetting", botSpeechSettings);
builder.Services.AddSingleton(botSpeechSettings);

builder.Services.AddScoped<BraincaseLiteDBContext>();
builder.Services.AddSingleton<IBotPlayer, DefaultBotPlayer>();
builder.Services.AddSingleton<IBotSpeech, DefaultBotSpeech>();
builder.Services.AddSingleton<IBotSpeecher, AzBotSpeecher>();
builder.Services.AddSingleton<IWakeWordListener, AzCognitiveServicesWakeWordListener>();
builder.Services.AddScoped<IBotCopilot, DefaultBotCopilot>();
builder.Services.AddScoped<IBraincaseRepository, BraincaseRepository>();
builder.Services.AddHostedService<HostedService>();
builder.Services.AddBotSharpCore(builder.Configuration, options =>
  {
      options.JsonSerializerOptions.Converters.Add(new RichContentJsonConverter());
      options.JsonSerializerOptions.Converters.Add(new TemplateMessageJsonConverter());
  }).AddBotSharpLogger(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserIdentity, BotUserIdentity>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();
// Use BotSharp
//app.UseBotSharp();

// Add startup code
app.Lifetime.ApplicationStarted.Register(async () =>
{
    // Your startup code here
    Console.WriteLine("Application has started.");

    // Retrieve IBotCopilot from DI container
    using (var scope = app.Services.CreateScope())
    {
        //var botCopilot = scope.ServiceProvider.GetRequiredService<IBotCopilot>();
        //await botCopilot.InitCopilotAsync();
    }
});

app.Run();
