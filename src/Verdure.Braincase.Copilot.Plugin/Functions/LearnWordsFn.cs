using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using System.Text.Encodings.Web;
using System.Text.Json;
using Verdure.Braincase.Copilot.Plugin.Models;

namespace Verdure.Braincase.Copilot.Plugin.Functions;

public class LearnWordsFn : IFunctionCallback
{
    public string Name => "learn_words";

    private readonly IServiceProvider _service;
    private readonly JsonSerializerOptions _options;
    private readonly IConversationService _conversationService;
    public LearnWordsFn(IServiceProvider service,
        IConversationService conversationService)
    {
        _service = service;
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            AllowTrailingCommas = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        _conversationService = conversationService;
    }

    public Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<LearnWordsFunctionArgs>(message.FunctionArgs ?? "", _options) ?? new LearnWordsFunctionArgs();
        return Task.FromResult(true);
    }
}
