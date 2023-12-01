using System;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WidenBot;

var builder = new HostApplicationBuilder(args);

builder
    .Services
    .AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Trace))
    .AddSingleton<DiscordSocketClient>()
    .AddSingleton<InteractionService>()
    .AddHostedService<DiscordClientHost>()
    .AddLavalink()
    .ConfigureLavalink(config =>
    {
        config.ReadyTimeout = TimeSpan.FromSeconds(10);
        config.Label = "WidenBot";
        config.Passphrase = Constants.LavalinkPassword;
    });

builder.Build().Run();
