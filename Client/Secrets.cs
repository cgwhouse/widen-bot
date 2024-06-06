using System;
//using System.IO;
//using System.Reflection;
//using System.Text.Json;

namespace WidenBot;

internal class Secrets
{
    public readonly string Label;
    public readonly string LavalinkPassword;
    public readonly string DiscordServerID;
    public readonly string DiscordBotToken;

    private record Config(string Label, string Password, DiscordConfig Discord);

    private record DiscordConfig(string ServerID, string BotToken);

    public Secrets()
    {
        //var binFolder =
        //    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        //    ?? throw new Exception("binFolder is null");

        //var config =
        //    JsonSerializer.Deserialize<Config>(
        //        File.ReadAllText(Path.Combine(binFolder, "config.json")),
        //        new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }
        //    ) ?? throw new Exception("config is null after deserializing");

        Label =
            Environment.GetEnvironmentVariable("INSTANCE_LABEL")
            ?? throw new Exception("INSTANCE_LABEL is null");

        LavalinkPassword =
            Environment.GetEnvironmentVariable("LAVALINK_PASSWORD")
            ?? throw new Exception("LAVALINK_PASSWORD is null");

        DiscordServerID =
            Environment.GetEnvironmentVariable("DISCORD_SERVER_ID")
            ?? throw new Exception("DISCORD_SERVER_ID is null");

        DiscordBotToken =
            Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN")
            ?? throw new Exception("DISCORD_BOT_TOKEN is null");
    }
}
