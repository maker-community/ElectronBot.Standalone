using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectronBot.Standalone.DataStorage.Collections;

public class BotStandaloneSettingDocument: LiteDBBase
{
    public string CurrentConversationId { get; set; } = string.Empty;
}
