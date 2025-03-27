using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Users;
using BotSharp.Abstraction.Users.Enums;
using BotSharp.Abstraction.Users.Models;
using BotSharp.Abstraction.Utilities;
using BotSharp.Core.Plugins;
using ElectronBot.Standalone.Core.Contracts;
using ElectronBot.Standalone.Core.Enums;
using ElectronBot.Standalone.Core.Models;
using ElectronBot.Standalone.Core.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace ElectronBot.Standalone.Core.Services;

public class DefaultBotCopilot : IBotCopilot
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IUserIdentity _userIdentity;
    private readonly IUserService _userService;
    public DefaultBotCopilot(IServiceProvider serviceProvider, IUserIdentity userIdentity, IUserService userService)
    {
        _serviceProvider = serviceProvider;
        _userIdentity = userIdentity;
        _userService = userService;
    }
    public async Task InitCopilotAsync()
    {
        var user = await _userService.GetUser(_userIdentity.Id);

        if (user == null)
        {
            await _userService.CreateUser(new User
            {
                Id = _userIdentity.Id,
                Email = _userIdentity.Email,
                UserName = _userIdentity.UserName,
                FirstName = _userIdentity.FirstName,
                LastName = _userIdentity.LastName,
                Role = UserRole.Admin,
                Type = UserType.Client,
            });

            var loader = _serviceProvider.GetRequiredService<PluginLoader>();
            var plugins = loader.GetPagedPlugins(_serviceProvider, new BotSharp.Abstraction.Plugins.Models.PluginFilter
            {
                Pager = new Pagination
                {
                    Page = 1,
                    Size = 100
                }
            }).Items.ToList();

            var agentService = _serviceProvider.GetRequiredService<IAgentService>();

            var resultData = await agentService.RefreshAgents();


            foreach (var plugin in plugins)
            {
                _ = loader.UpdatePluginStatus(_serviceProvider, plugin.Id, true);
            }
        }
        var conv = _serviceProvider.GetRequiredService<IConversationService>();
        var lastConv = (await conv.GetLastConversations()).FirstOrDefault() ?? await conv.NewConversation(new Conversation
        {
            AgentId = VerdureAgentId.VerdureChatId,
            UserId = _userIdentity.Id
        });

        var date = lastConv.CreatedTime;
        var localDate = DateTime.Now;

        if (date.Day != localDate.Day)
        {
            lastConv = await conv.NewConversation(new Conversation
            {
                AgentId = VerdureAgentId.VerdureChatId,
                UserId = _userIdentity.Id
            });
            var repo = _serviceProvider.GetRequiredService<IBraincaseRepository>();
            await repo.SaveSettingAsync(new BotStandaloneSetting { CurrentConversationId = lastConv.Id });
        }
    }
}
