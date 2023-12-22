"""
setup.py

Script to inject all the sensitive info needed to connect and run the bot.
Before executing, ensure that values for all properties in config.json have been provided.

Cristian W.
"""

import json, os, urllib.request


def main():
    print("Performing WidenBot setup...")

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

    # Download Lavalink if needed
    handle_lavalink_binary()

    # This password needs to be injected in both places
    lavalink_password_placeholder = "<LAVALINK_PASSWORD>"

    # Handle audio server, inject values into application.yml
    handle_lavalink_injection(user_config, lavalink_password_placeholder)

    # Handle bot client, inject values into Constants.cs
    handle_dotnet_injection(user_config, lavalink_password_placeholder)

    print("Done!")


def get_file_contents(path):
    f = open(path, "r")
    raw = f.read()
    f.close()
    return raw


def write_file_contents(path, contents):
    f = open(path, "w")
    f.write(contents)
    f.close()


def handle_lavalink_binary():
    if not os.path.isfile("Lavalink/Lavalink.jar"):
        print("Downloading Lavalink...")

        urllib.request.urlretrieve(
            "https://github.com/lavalink-devs/Lavalink/releases/latest/download/Lavalink.jar",
            "Lavalink/Lavalink.jar",
        )

        print("Lavalink binary successfully downloaded...")
    else:
        print("Lavalink binary already exists, skipping download...")


def handle_dotnet_injection(user_config, lavalink_password_placeholder):
    discord_bot_token_placeholder = "<DISCORD_BOT_TOKEN>"
    discord_server_id_placeholder = "<DISCORD_SERVER_ID>"

    dotnetRaw = get_file_contents("WidenBot/Constants.cs")

    if (
        not discord_bot_token_placeholder in dotnetRaw
        or not discord_server_id_placeholder in dotnetRaw
        or not lavalink_password_placeholder in dotnetRaw
    ):
        print("Constants.cs has already been updated, skipping...")
        return

    dotnetUpdated = (
        dotnetRaw.replace(discord_bot_token_placeholder, user_config["DiscordBotToken"])
        .replace(discord_server_id_placeholder, user_config["DiscordServerID"])
        .replace(lavalink_password_placeholder, user_config["LavalinkPassword"])
    )

    write_file_contents("WidenBot/Constants.cs", dotnetUpdated)

    print("Constants.cs has been updated...")

    return


def handle_lavalink_injection(user_config, lavalink_password_placeholder):
    youtube_email_placeholder = "<YOUTUBE_EMAIL>"
    youtube_password_placeholder = "<YOUTUBE_PASSWORD>"
    spotify_client_id_placeholder = "<SPOTIFY_CLIENTID>"
    spotify_client_secret_placeholder = "<SPOTIFY_CLIENTSECRET>"

    lavalinkRaw = get_file_contents("Lavalink/application.yml")

    if (
        not youtube_email_placeholder in lavalinkRaw
        or not youtube_password_placeholder in lavalinkRaw
        or not spotify_client_id_placeholder in lavalinkRaw
        or not spotify_client_secret_placeholder in lavalinkRaw
        or not lavalink_password_placeholder in lavalinkRaw
    ):
        print("application.yml has already been updated, skipping...")
        return

    lavalinkUpdated = (
        lavalinkRaw.replace(youtube_email_placeholder, user_config["YouTubeEmail"])
        .replace(youtube_password_placeholder, user_config["YouTubePassword"])
        .replace(spotify_client_id_placeholder, user_config["SpotifyClientID"])
        .replace(spotify_client_secret_placeholder, user_config["SpotifyClientSecret"])
        .replace(lavalink_password_placeholder, user_config["LavalinkPassword"])
    )

    write_file_contents("Lavalink/application.yml", lavalinkUpdated)

    print("application.yml has been updated...")


main()
