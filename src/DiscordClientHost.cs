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

internal sealed class DiscordClientHost(
    DiscordSocketClient discordSocketClient,
    InteractionService interactionService,
    IServiceProvider serviceProvider,
    IConfiguration config
) : IHostedService
{
    private string BotToken =>
        config.GetValue<string>("DISCORD_BOT_TOKEN")
        ?? throw new Exception("DISCORD_BOT_TOKEN is null");

    private string ServerID =>
        config.GetValue<string>("DISCORD_SERVER_ID")
        ?? throw new Exception("DISCORD_SERVER_ID is null");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        discordSocketClient.InteractionCreated += InteractionCreated;
        discordSocketClient.Ready += ClientReady;

        await discordSocketClient.LoginAsync(TokenType.Bot, BotToken).ConfigureAwait(false);

        await discordSocketClient.StartAsync().ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        discordSocketClient.InteractionCreated -= InteractionCreated;
        discordSocketClient.Ready -= ClientReady;

        await discordSocketClient.StopAsync().ConfigureAwait(false);
    }

    private Task<IResult> InteractionCreated(SocketInteraction interaction)
    {
        var interactionContext = new SocketInteractionContext(discordSocketClient, interaction);

        return interactionService.ExecuteCommandAsync(interactionContext, serviceProvider);
    }

    private async Task ClientReady()
    {
        await interactionService
            .AddModulesAsync(Assembly.GetExecutingAssembly(), serviceProvider)
            .ConfigureAwait(false);

        await interactionService
            .RegisterCommandsToGuildAsync(ulong.Parse(ServerID))
            .ConfigureAwait(false);
    }
}
