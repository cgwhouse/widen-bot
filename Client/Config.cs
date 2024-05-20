using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace WidenBot;

public class Config
{
    public readonly string DiscordBotToken;
    public readonly string DiscordServerID;
    public readonly string LavalinkPassword;

    public readonly bool UseSponsorBlockIntegration = false;

    private record UserConfig(
        string DiscordBotToken,
        string DiscordServerID,
        string LavalinkPassword
    );

    public Config()
    {
        var binFolder =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? throw new Exception("binFolder is null");

        var userConfig =
            JsonSerializer.Deserialize<UserConfig>(
                File.ReadAllText(Path.Combine(binFolder, "config.json"))
            ) ?? throw new Exception("userConfig is null after deserializing");

        DiscordBotToken = userConfig.DiscordBotToken;
        DiscordServerID = userConfig.DiscordServerID;
        LavalinkPassword = userConfig.LavalinkPassword;
    }
}
