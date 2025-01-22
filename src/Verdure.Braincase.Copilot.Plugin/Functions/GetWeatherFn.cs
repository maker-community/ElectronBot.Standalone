using BotSharp.Abstraction.Conversations.Models;

namespace Verdure.Braincase.Copilot.Plugin.Functions;

public class GetWeatherFn : IFunctionCallback
{
    public GetWeatherFn()
    {
    }
    public string Name => "get_weather";

    public async Task<bool> Execute(RoleDialogModel message)
    {
        return true;
    }
}
