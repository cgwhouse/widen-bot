using System;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET.Extensions;
using Lavalink4NET.InactivityTracking;
using Lavalink4NET.InactivityTracking.Extensions;
using Lavalink4NET.InactivityTracking.Trackers.Idle;
using Lavalink4NET.InactivityTracking.Trackers.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WidenBot;

var builder = new HostApplicationBuilder(args);

builder
    .Services
    // .NET stuff
    .AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Trace))
    .AddMemoryCache()
    .AddSingleton<DiscordSocketClient>()
    .AddSingleton<InteractionService>()
    .AddHostedService<DiscordClientHost>()
    // Lavalink general settings
    .AddLavalink()
    .ConfigureLavalink(config =>
    {
        config.ReadyTimeout = TimeSpan.FromSeconds(10);
        config.Label = "WidenBot";
        config.Passphrase = Constants.LavalinkPassword;
    })
    // Lavalink inactivity tracking general settings
    .AddInactivityTracking()
    .ConfigureInactivityTracking(options =>
    {
        options.DefaultTimeout = TimeSpan.FromSeconds(30); // Timeout before player is disconnected from voice channel
        options.DefaultPollInterval = TimeSpan.FromSeconds(5); // Increase this to use less resources, sets how often the tracker(s) are checked
        options.TrackingMode = InactivityTrackingMode.All; // If any inactivity tracker reports inactivity, it counts as inactive
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

builder.Build().Run();
