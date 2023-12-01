"""
setup.py

Script to inject all the sensitive info needed to connect and run the bot.
Before executing, ensure that values for all properties in config.json have been provided.

Cristian W.
"""

import json


def main():
    # Get config.json
    try:
        user_config = json.loads(get_file_contents("config.json"))
    except FileNotFoundError:
        print("config.json was not found. Exiting")
        return

    # Validate config
    for key in user_config:
        if user_config[key] == "":
            print("All values must be supplied in config.json. Exiting")
            return

    # Handle .NET by injecting config values into Constants.cs
    dotnetRaw = get_file_contents("WidenBot/Constants.cs")

    dotnetUpdated = (
        dotnetRaw.replace("<DISCORD_BOT_TOKEN>", user_config["DiscordBotToken"])
        .replace("<DISCORD_SERVER_ID>", user_config["DiscordServerID"])
        .replace("<LAVALINK_PASSWORD>", user_config["LavalinkPassword"])
    )

    write_file_contents("WidenBot/Constants.cs", dotnetUpdated)

    # Handle Lavalink by injecting config values into application.yml
    lavalinkRaw = get_file_contents("Lavalink/application.yml")

    lavalinkUpdated = (
        lavalinkRaw.replace("<YOUTUBE_EMAIL>", user_config["YouTubeEmail"])
        .replace("<YOUTUBE_PASSWORD>", user_config["YouTubePassword"])
        .replace("<LAVALINK_PASSWORD>", user_config["LavalinkPassword"])
    )

    write_file_contents("Lavalink/application.yml", lavalinkUpdated)


def get_file_contents(path):
    f = open(path, "r")
    raw = f.read()
    f.close()
    return raw


def write_file_contents(path, contents):
    f = open(path, "w")
    f.write(contents)
    f.close()


main()
