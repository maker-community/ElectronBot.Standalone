using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Routing;
using ElectronBot.Standalone.Core.Contracts;
using ElectronBot.Standalone.Core.Enums;
using ElectronBot.Standalone.Core.Models;
using ElectronBot.Standalone.Core.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetCoreAudio;

namespace ElectronBot.Standalone.Core.Services;

/// <summary>
/// A hosted service providing the primary conversation loop for Semantic Kernel with OpenAI ChatGPT.
/// </summary>
public class HostedService : IHostedService, IDisposable
{
    private readonly ILogger<HostedService> _logger;

    private readonly IWakeWordListener _wakeWordListener;

    private readonly IServiceProvider _serviceProvider;

    private BotSpeechSetting _options;

    private Task? _executeTask;

    private readonly CancellationTokenSource _cancelToken = new();

    // Notification sound support
    private readonly string _notificationSoundFilePath;
    private readonly Player _player;

    private readonly IServiceScopeFactory _serviceScopeFactory;

    private readonly IBotPlayer _botPlayer;

    /// <summary>
    /// Constructor
    /// </summary>
    public HostedService(IWakeWordListener wakeWordListener,
        ILogger<HostedService> logger,
        IServiceProvider serviceProvider,
        BotSpeechSetting options,
        IServiceScopeFactory serviceScopeFactory,
        IBotPlayer botPlayer)
    {
        _logger = logger;
        _wakeWordListener = wakeWordListener;
        _notificationSoundFilePath = Path.Combine(AppContext.BaseDirectory, "Asserts", "bing.mp3");
        _player = new Player();
        _serviceProvider = serviceProvider;
        _options = options;
        _serviceScopeFactory = serviceScopeFactory;
        _botPlayer = botPlayer;
    }

    /// <summary>
    /// Start the service.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    { 
        _botPlayer.ShowDateToSubScreenAsync();
        _executeTask = ExecuteAsync(_cancelToken.Token);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Primary service logic loop.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Play a notification to let the user know we have started listening for the wake phrase.
                await _player.Play(_notificationSoundFilePath);

                var botSpeech = _serviceProvider.GetRequiredService<IBotSpeecher>();

                // Wait for wake word or phrase
                if (!await _wakeWordListener.WaitForWakeWordAsync(cancellationToken))
                {
                    continue;
                }
                await _player.Play(_notificationSoundFilePath);

                var helloString = _options.AnswerText;
                // Say hello on startup
                await botSpeech.SpeakAsync(helloString ?? "Hello!", cancellationToken);
                // Start listening
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Listen to the user
                    var userSpoke = await botSpeech.ListenAsync(cancellationToken);
                    await _botPlayer.StopLottiePlaybackAsync();

                    _logger.LogInformation($"User spoke: {userSpoke}");
                    // Get a reply from the AI and add it to the chat history.
                    var reply = string.Empty;

                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var serviceProvider = scope.ServiceProvider;
                        var repo = serviceProvider.GetRequiredService<IBraincaseRepository>();

                        var setting = await repo.GetSettingAsync();

                        var inputMsg = new RoleDialogModel(AgentRole.User, userSpoke)
                        {
                            MessageId = Guid.NewGuid().ToString(),
                            CreatedAt = DateTime.UtcNow
                        };

                        var conversationService = serviceProvider.GetRequiredService<IConversationService>();
                        var routing = serviceProvider.GetRequiredService<IRoutingService>();

                        routing.Context.SetMessageId(setting.CurrentConversationId, inputMsg.MessageId);
                        conversationService.SetConversationId(setting.CurrentConversationId, new());

                        try
                        {
                            // 启动动画但不阻塞当前执行流程
                            var animationTask = _botPlayer.PlayLottieByNameIdAsync("think", -1);

                            // 可以选择添加异常处理
                            animationTask?.ContinueWith(t =>
                            {
                                if (t.IsFaulted)
                                {
                                    _logger.LogError($"Animation playback failed: {t.Exception}");
                                }
                            }, TaskContinuationOptions.OnlyOnFaulted);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Failed to start animation: {ex.Message}");
                            await _botPlayer.StopLottiePlaybackAsync();
                            // 根据需要处理异常
                        }

                        await Task.Run(async () =>
                        {
                            await conversationService.SendMessage(VerdureAgentId.VerdureChatId, inputMsg,
                                replyMessage: null,
                                msg =>
                                {
                                    reply = msg.Content;
                                    return Task.CompletedTask;
                                });
                        });

                        await _botPlayer.StopLottiePlaybackAsync();
                        // Speak the AI's reply
                        await botSpeech.SpeakAsync(reply, cancellationToken);

                        // If the user said "Goodbye" - stop listening and wait for the wake work again.
                        if (userSpoke.StartsWith("再见") || userSpoke.StartsWith("goodbye", StringComparison.InvariantCultureIgnoreCase))
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception aiex)
            {
                await _botPlayer.StopLottiePlaybackAsync();
                _logger.LogError($"OpenAI returned an error.{aiex.Message}");
            }
        }
    }

    /// <summary>
    /// Stop a running service.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cancelToken.Cancel();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public virtual void Dispose()
    {
        _cancelToken.Dispose();
        _wakeWordListener.Dispose();
    }
}
