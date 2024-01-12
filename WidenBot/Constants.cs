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

    private record Config(string DiscordBotToken, string DiscordServerID, string LavalinkPassword);

    public Secrets()
    {
        var binFolder =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? throw new Exception("binFolder is null");

        var config =
            JsonSerializer.Deserialize<Config>(
                File.ReadAllText(Path.Combine(binFolder, "config.json"))
            ) ?? throw new Exception("config is null after deserializing");

        DiscordBotToken = config.DiscordBotToken;
        DiscordServerID = config.DiscordServerID;
        LavalinkPassword = config.LavalinkPassword;
    }
}
