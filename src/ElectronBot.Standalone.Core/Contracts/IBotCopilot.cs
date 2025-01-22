using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectronBot.Standalone.Core.Contracts;

public interface IBotCopilot
{
   Task InitCopilotAsync();

    Task<string> ChatToCopilotAsync(string question);
}
