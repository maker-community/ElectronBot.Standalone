using ElectronBot.Standalone.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectronBot.Standalone.Core.Repositories;

public interface IBraincaseRepository
{
    Task InitDataAsync();
    Task SaveSettingAsync(BotStandaloneSetting setting);
    Task<BotStandaloneSetting> GetSettingAsync();
}
