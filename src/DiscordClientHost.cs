using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace WidenBot;

internal sealed class DiscordClientHost : IHostedService
{
    private readonly DiscordSocketClient _discordSocketClient;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _config;

    private string BotToken =>
        _config.GetValue<string>("DISCORD_BOT_TOKEN")
        ?? throw new Exception("DISCORD_BOT_TOKEN is null");

    private string ServerID =>
        _config.GetValue<string>("DISCORD_SERVER_ID")
        ?? throw new Exception("DISCORD_SERVER_ID is null");

    public DiscordClientHost(
        DiscordSocketClient discordSocketClient,
        InteractionService interactionService,
        IServiceProvider serviceProvider,
        IConfiguration config
    )
    {
        _discordSocketClient = discordSocketClient;
        _interactionService = interactionService;
        _serviceProvider = serviceProvider;
        _config = config;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _discordSocketClient.InteractionCreated += InteractionCreated;
        _discordSocketClient.Ready += ClientReady;

        await _discordSocketClient.LoginAsync(TokenType.Bot, BotToken).ConfigureAwait(false);

        await _discordSocketClient.StartAsync().ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _discordSocketClient.InteractionCreated -= InteractionCreated;
        _discordSocketClient.Ready -= ClientReady;

        await _discordSocketClient.StopAsync().ConfigureAwait(false);
    }

    private Task InteractionCreated(SocketInteraction interaction)
    {
        var interactionContext = new SocketInteractionContext(_discordSocketClient, interaction);

        return _interactionService.ExecuteCommandAsync(interactionContext, _serviceProvider);
    }

    private async Task ClientReady()
    {
        await _interactionService
            .AddModulesAsync(Assembly.GetExecutingAssembly(), _serviceProvider)
            .ConfigureAwait(false);

        await _interactionService
            .RegisterCommandsToGuildAsync(ulong.Parse(ServerID))
            .ConfigureAwait(false);
    }
}
