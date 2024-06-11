//using System;
//
//namespace WidenBot;
//
//internal class Secrets
//{
//    public readonly string Label;
//    public readonly string LavalinkPassword;
//    public readonly string DiscordServerID;
//    public readonly string DiscordBotToken;
//
//    public Secrets()
//    {
//        Label =
//            Environment.GetEnvironmentVariable("INSTANCE_LABEL")
//            ?? throw new Exception("INSTANCE_LABEL is null");
//
//        LavalinkPassword =
//            Environment.GetEnvironmentVariable("LAVALINK_PASSWORD")
//            ?? throw new Exception("LAVALINK_PASSWORD is null");
//
//        DiscordServerID =
//            Environment.GetEnvironmentVariable("DISCORD_SERVER_ID")
//            ?? throw new Exception("DISCORD_SERVER_ID is null");
//
//        DiscordBotToken =
//            Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN")
//            ?? throw new Exception("DISCORD_BOT_TOKEN is null");
//    }
//}
