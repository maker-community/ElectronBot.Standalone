using ElectronBot.Standalone.Core.Models;
using ElectronBot.Standalone.DataStorage.Collections;

namespace ElectronBot.Standalone.DataStorage.Mappers;

public static class BotStandaloneSettingMappers
{
    public static BotStandaloneSettingDocument ToDoc(this BotStandaloneSetting model)
    {
        return new BotStandaloneSettingDocument
        {
            CurrentConversationId = model.CurrentConversationId,
        };
    }

    public static BotStandaloneSetting ToModel(this BotStandaloneSettingDocument doc)
    {
        return new BotStandaloneSetting
        {
            CurrentConversationId = doc.CurrentConversationId,
        };
    }
}
