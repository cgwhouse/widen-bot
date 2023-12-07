"""
setup.py

Script to inject all the sensitive info needed to connect and run the bot.
Before executing, ensure that values for all properties in config.json have been provided.

Cristian W.
"""

import json, os, urllib.request


def main():
    discord_bot_token = "<DISCORD_BOT_TOKEN>"
    discord_server_id = "<DISCORD_SERVER_ID>"
    lavalink_password = "<LAVALINK_PASSWORD>"
    youtube_email = "<YOUTUBE_EMAIL>"
    youtube_password = "<YOUTUBE_PASSWORD>"

    print("Performing WidenBot setup...")

    # Download Lavalink.jar if needed
    if not os.path.isfile("Lavalink/Lavalink.jar"):
        print("Downloading Lavalink...")

        urllib.request.urlretrieve(
            "https://github.com/lavalink-devs/Lavalink/releases/latest/download/Lavalink.jar",
            "Lavalink/Lavalink.jar",
        )

        print("Lavalink binary successfully downloaded...")
    else:
        print("Lavalink binary already exists, skipping download...")

    # Get config.json and validate
    try:
        user_config = json.loads(get_file_contents("config.json"))
    except FileNotFoundError:
        print("ERROR: config.json was not found. Exiting")
        return

    for key in user_config:
        if user_config[key] == "":
            print("ERROR: All values must be supplied in config.json. Exiting")
            return

    # Handle .NET, inject values into Constants.cs
    dotnetRaw = get_file_contents("WidenBot/Constants.cs")

    if (
        not discord_bot_token in dotnetRaw
        or not discord_server_id in dotnetRaw
        or not lavalink_password in dotnetRaw
    ):
        print("Constants.cs has already been updated, skipping...")
    else:
        print("Injecting config values into Constants.cs...")

        dotnetUpdated = (
            dotnetRaw.replace(discord_bot_token, user_config["DiscordBotToken"])
            .replace(discord_server_id, user_config["DiscordServerID"])
            .replace(lavalink_password, user_config["LavalinkPassword"])
        )

        write_file_contents("WidenBot/Constants.cs", dotnetUpdated)

        print("Constants.cs has been updated...")

    # Handle Lavalink, inject values into application.yml
    lavalinkRaw = get_file_contents("Lavalink/application.yml")

    if (
        not youtube_email in lavalinkRaw
        or not youtube_password in lavalinkRaw
        or not lavalink_password in dotnetRaw
    ):
        print("application.yml has already been updated, skipping...")
    else:
        lavalinkUpdated = (
            lavalinkRaw.replace(youtube_email, user_config["YouTubeEmail"])
            .replace(youtube_password, user_config["YouTubePassword"])
            .replace(lavalink_password, user_config["LavalinkPassword"])
        )

        write_file_contents("Lavalink/application.yml", lavalinkUpdated)

        print("application.yml has been updated...")


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
