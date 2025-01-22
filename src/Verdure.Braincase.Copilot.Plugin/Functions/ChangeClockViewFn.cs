using BotSharp.Abstraction.Conversations.Models;
using System.Text.Encodings.Web;
using System.Text.Json;
using Verdure.Braincase.Copilot.Plugin.Models;

namespace Verdure.Braincase.Copilot.Plugin.Functions;

public class ChangeClockViewFn : IFunctionCallback
{
    public string Name => "change_clock_view";
    private readonly JsonSerializerOptions _options;
    public ChangeClockViewFn()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            AllowTrailingCommas = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }

    public Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<ChangeClockViewFunctionArgs>(message.FunctionArgs ?? "", _options) ?? new ChangeClockViewFunctionArgs();

        return Task.FromResult(true);
    }
}
