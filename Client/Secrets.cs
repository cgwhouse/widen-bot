using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace WidenBot;

internal class Secrets
{
    public readonly string DiscordBotToken;
    public readonly string DiscordServerID;
    public readonly string LavalinkPassword;
    public readonly string Label;

    private record Config(string Label, string Password, DiscordConfig Discord);

    private record DiscordConfig(string ServerID, string BotToken);

    public Secrets()
    {
        var binFolder =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? throw new Exception("binFolder is null");

        var config =
            JsonSerializer.Deserialize<Config>(
                File.ReadAllText(Path.Combine(binFolder, "config.json")),
                new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }
            ) ?? throw new Exception("config is null after deserializing");

        DiscordBotToken = config.Discord.BotToken;
        DiscordServerID = config.Discord.ServerID;
        LavalinkPassword = config.Password;
        Label = config.Label;
    }
}
