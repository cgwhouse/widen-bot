"""
run.py

Script to inject all the sensitive info needed to connect and run the bot, then run it.
Before executing, ensure that values for all properties in config.json have been provided.
"""

import json
import os
import subprocess
import sys
import urllib.request


def main():
    # Get command line arg and validate
    run_target = handle_script_arg()

    if run_target == None:
        print(
            "Usage: python3 run.py run_target, where run_target = 'client' or 'server'"
        )
        return

    # Get config.json and validate
    user_config = handle_user_config()

    if user_config == None:
        print(
            "config.json must exist in WidenBot directory, and must be fully specified"
        )
        return

    if run_target == "client":
        handle_client()
    else:
        handle_server(user_config)


def handle_script_arg():
    if len(sys.argv) < 2:
        return None

    run_target = sys.argv[1]

    if run_target not in ["client", "server"]:
        return None

    return run_target


def handle_user_config():
    try:
        user_config = json.loads(get_file_contents("WidenBot/config.json"))
    except FileNotFoundError:
        return None

    for key in user_config:
        if user_config[key] == "":
            return None

    return user_config


def handle_client():
    print("Building and running WidenBot client...")

    subprocess.run(
        ["dotnet", "restore", "WidenBot"],
        stdout=subprocess.DEVNULL,
        stderr=subprocess.STDOUT,
    )
    print("...Packages restored successfully")

    subprocess.run(
        ["dotnet", "clean", "-c", "Release", "WidenBot"],
        stdout=subprocess.DEVNULL,
        stderr=subprocess.STDOUT,
    )
    print("...Project cleaned successfully")

    subprocess.run(["dotnet", "run", "-c", "Release", "--project", "WidenBot"])


def handle_server(user_config):
    # Download Lavalink if needed
    handle_lavalink_binary()

    # Create fresh copy of application.yml, with injected secrets for Lavalink server
    handle_lavalink_config(user_config)

    subprocess.run(["java", "-jar", "Lavalink/Lavalink.jar"])


def handle_lavalink_binary():
    print("Handling Lavalink binary...")

    if os.path.isfile("Lavalink/Lavalink.jar"):
        print("...Lavalink binary already exists, skipping download")
        return

    print("...Downloading jar")

    urllib.request.urlretrieve(
        "https://github.com/lavalink-devs/Lavalink/releases/latest/download/Lavalink.jar",
        "Lavalink/Lavalink.jar",
    )

    print("...Lavalink binary successfully downloaded")


def handle_lavalink_config(user_config):
    lavalink_password = "<LAVALINK_PASSWORD>"
    spotify_client_id = "<SPOTIFY_CLIENTID>"
    spotify_client_secret = "<SPOTIFY_CLIENTSECRET>"
    youtube_email = "<YOUTUBE_EMAIL>"
    youtube_password = "<YOUTUBE_PASSWORD>"

    lavalinkRaw = get_file_contents("Lavalink/application.template.yml")

    lavalinkUpdated = (
        lavalinkRaw.replace(lavalink_password, user_config["LavalinkPassword"])
        .replace(spotify_client_id, user_config["SpotifyClientID"])
        .replace(spotify_client_secret, user_config["SpotifyClientSecret"])
        .replace(youtube_email, user_config["YouTubeEmail"])
        .replace(youtube_password, user_config["YouTubePassword"])
    )

    write_file_contents("application.yml", lavalinkUpdated)

    print("application.yml has been created / overwritten...")


def get_file_contents(path):
    f = open(path, "r")
    raw = f.read()
    f.close()
    return raw


def write_file_contents(path, contents):
    f = open(path, "w")
    f.write(contents)
    f.close()


if __name__ == "__main__":
    main()
