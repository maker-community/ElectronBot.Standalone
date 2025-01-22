using ElectronBot.Standalone.Core.Models;
using ElectronBot.Standalone.Core.Repositories;
using ElectronBot.Standalone.DataStorage.Mappers;
using Microsoft.Extensions.DependencyInjection;

namespace ElectronBot.Standalone.DataStorage.Repository;

public class BraincaseRepository : IBraincaseRepository
{
    private readonly IServiceProvider _serviceProvider;

    public BraincaseRepository(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task InitDataAsync()
    {
        return Task.CompletedTask;
    }

    public Task SaveSettingAsync(BotStandaloneSetting setting)
    {
        var doc = setting.ToDoc();
        var db = _serviceProvider.GetRequiredService<BraincaseLiteDBContext>();
        db.BotStandaloneSettings.Insert(doc);
        return Task.CompletedTask;
    }

    public Task<BotStandaloneSetting> GetSettingAsync()
    {
        var db = _serviceProvider.GetRequiredService<BraincaseLiteDBContext>();
        var doc = db.BotStandaloneSettings.FindOne(b => b.CurrentConversationId != null);
        return Task.FromResult(doc.ToModel());
    }
}
