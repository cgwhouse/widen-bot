using System;
using System.Collections.Immutable;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET.Extensions;
using Lavalink4NET.InactivityTracking;
using Lavalink4NET.InactivityTracking.Extensions;
using Lavalink4NET.InactivityTracking.Trackers.Idle;
using Lavalink4NET.InactivityTracking.Trackers.Users;
using Lavalink4NET.Players;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WidenBot;

var builder = new HostApplicationBuilder(args);

builder
    .Services
    .AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Trace))
    .AddMemoryCache()
    .AddSingleton<DiscordSocketClient>()
    .AddSingleton<InteractionService>()
    .AddHostedService<DiscordClientHost>()
    .AddLavalink()
    .ConfigureLavalink(config =>
    {
        config.ReadyTimeout = TimeSpan.FromSeconds(10);
        config.Label = "WidenBot";
        config.Passphrase = Constants.LavalinkPassword;
    })
    .Configure<UsersInactivityTrackerOptions>(config =>
    {
        config.Label = "WidenBotUsersInactivityTracker";
        config.Timeout = TimeSpan.FromSeconds(10); // Timeout after which player is reported as inactive
        config.Threshold = 1; // Number of users that must be in voice channel for player to be considered active
        config.ExcludeBots = true; // Whether to exclude bots from the above count
    })
    .Configure<IdleInactivityTrackerOptions>(config =>
    {
        config.Label = "WidenBotIdleInactivityTracker";
        config.Timeout = TimeSpan.FromDays(1); // Don't want bot joining / leaving because music not being played, set long timeout
        config.IdleStates = ImmutableArray<PlayerState> // States that should be considered idle by this tracker
            .Empty
            .Add(PlayerState.Paused)
            .Add(PlayerState.NotPlaying);
    })
    .ConfigureInactivityTracking(options =>
    {
        options.DefaultTimeout = TimeSpan.FromSeconds(30); // Timeout before player is disconnected from voice channel
        options.DefaultPollInterval = TimeSpan.FromSeconds(5); // Increase this to use less resources, sets how often the tracker(s) are checked
        options.TrackingMode = InactivityTrackingMode.Any; // If any inactivity tracker reports inactivity, it counts as inactive
        options.UseDefaultTrackers = true;
        options.TimeoutBehavior = InactivityTrackingTimeoutBehavior.Lowest; // Lowest timeout of all trackers will be used
        options.InactivityBehavior = PlayerInactivityBehavior.Pause; // If player is being considered inactive, pause, will resume if it becomes active again
    });

builder.Build().Run();
