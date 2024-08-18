using System;
using System.Threading.Tasks;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Lavalink4NET.Extensions;
using Lavalink4NET.InactivityTracking;
using Lavalink4NET.InactivityTracking.Extensions;
using Lavalink4NET.InactivityTracking.Trackers.Idle;
using Lavalink4NET.InactivityTracking.Trackers.Users;
using Lavalink4NET.Integrations.SponsorBlock.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WidenBot;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = new HostApplicationBuilder(args);

        builder.Configuration.AddEnvironmentVariables();

        builder
            .Services
            // .NET stuff
            .AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Information))
            .AddMemoryCache()
            .AddSingleton<DiscordSocketClient>()
            // NOTE: below DI is required as of Discord.Net 3.15.0, unintended break that will probably
            // be fixed in the future
            .AddSingleton<IRestClientProvider>(x =>
                (IRestClientProvider)x.GetRequiredService<DiscordSocketClient>()
            )
            .AddSingleton<InteractionService>()
            .AddTransient<IPlayerService, PlayerService>()
            .AddHostedService<DiscordClientHost>()
            // Lavalink general settings
            .AddLavalink()
            .ConfigureLavalink(config =>
            {
                var instanceLabel =
                    builder.Configuration.GetValue<string>("INSTANCE_LABEL")
                    ?? throw new Exception("INSTANCE_LABEL is null");

                var lavalinkPassword =
                    builder.Configuration.GetValue<string>("LAVALINK_PASSWORD")
                    ?? throw new Exception("LAVALINK_PASSWORD is null");

                // Comment out to debug against a locally running Lavalink server
                config.BaseAddress = new Uri($"http://{instanceLabel}-widenbot-server:2333");

                config.Label = $"WidenBot-{instanceLabel}";
                config.Passphrase = lavalinkPassword;
                config.ReadyTimeout = TimeSpan.FromSeconds(10);
            })
            // Lavalink inactivity tracking general settings
            .AddInactivityTracking()
            .ConfigureInactivityTracking(options =>
            {
                options.DefaultTimeout = TimeSpan.FromSeconds(30); // Timeout before player is disconnected from voice channel
                options.DefaultPollInterval = TimeSpan.FromSeconds(5); // Increase this to use less resources, sets how often the tracker(s) are checked
                options.TrackingMode = InactivityTrackingMode.All; // All inactivity trackers must report inactivity for it to count as inactive
                options.UseDefaultTrackers = true;
                options.TimeoutBehavior = InactivityTrackingTimeoutBehavior.Lowest; // Lowest timeout of all trackers will be used
                options.InactivityBehavior = PlayerInactivityBehavior.None; // Don't try to interpret / change the player's behavior during a period of temp perceived inactivity
            })
            // Inactivity tracker based on users in the voice channel
            .Configure<UsersInactivityTrackerOptions>(config =>
            {
                config.Label = "WidenBotUsersInactivityTracker";
                config.Timeout = TimeSpan.FromSeconds(10); // Timeout after which player is reported as inactive
                config.Threshold = 1; // Number of users that must be in voice channel for player to be considered active
                config.ExcludeBots = true; // Whether to exclude bots from the above count
            })
            // Inactivity tracker based on whether bot is playing music
            .Configure<IdleInactivityTrackerOptions>(config =>
            {
                config.Label = "WidenBotIdleInactivityTracker";
            });

        var app = builder.Build();

        var useSponsorBlock = builder.Configuration.GetValue<bool>("USE_SPONSORBLOCK");

        if (useSponsorBlock)
            app.UseSponsorBlock();

        await app.RunAsync();
    }
}
