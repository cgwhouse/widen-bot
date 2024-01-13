"""
run.py

Script to inject all the sensitive info needed to connect and run the bot, then run it.
Before executing, ensure that values for all properties in config.json have been provided.

WidenBot Team
"""

import json, os, subprocess, urllib.request


def main():
    print("Performing WidenBot setup...")

    # Get config.json and validate
    try:
        user_config = json.loads(get_file_contents("WidenBot/config.json"))
    except FileNotFoundError:
        print("ERROR: config.json was not found. Exiting")
        return

    for key in user_config:
        if user_config[key] == "":
            print("ERROR: All values must be supplied in config.json. Exiting")
            return

    # Download Lavalink if needed
    handle_lavalink_binary()

    # Create fresh copy of application.yml with injected secrets for Lavalink server
    handle_lavalink_config(user_config)

    # Run the .NET client and suppress output
    subprocess.Popen(
        ["dotnet", "run", "-c", "Release", "--project", "WidenBot"],
        stdout=subprocess.DEVNULL,
        stderr=subprocess.STDOUT,
    )

    # Run the Lavalink server and print output, mainly because user may need to OAuth with Google
    lavalink_cmd = ["java", "-jar", "Lavalink/Lavalink.jar"]

    for lavalink_output in execute_and_print_output(lavalink_cmd):
        print(lavalink_output, end="")


def execute_and_print_output(cmd):
    popen = subprocess.Popen(cmd, stdout=subprocess.PIPE, universal_newlines=True)
    assert popen.stdout is not None

    for stdout_line in iter(popen.stdout.readline, ""):
        yield stdout_line

    popen.stdout.close()
    return_code = popen.wait()

    if return_code:
        raise subprocess.CalledProcessError(return_code, cmd)


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


def handle_lavalink_config(user_config):
    youtube_email_placeholder = "<YOUTUBE_EMAIL>"
    youtube_password_placeholder = "<YOUTUBE_PASSWORD>"
    spotify_client_id_placeholder = "<SPOTIFY_CLIENTID>"
    spotify_client_secret_placeholder = "<SPOTIFY_CLIENTSECRET>"
    lavalink_password_placeholder = "<LAVALINK_PASSWORD>"

    lavalinkRaw = get_file_contents("Lavalink/application.template.yml")

    lavalinkUpdated = (
        lavalinkRaw.replace(youtube_email_placeholder, user_config["YouTubeEmail"])
        .replace(youtube_password_placeholder, user_config["YouTubePassword"])
        .replace(spotify_client_id_placeholder, user_config["SpotifyClientID"])
        .replace(spotify_client_secret_placeholder, user_config["SpotifyClientSecret"])
        .replace(lavalink_password_placeholder, user_config["LavalinkPassword"])
    )

    write_file_contents("application.yml", lavalinkUpdated)

    print("application.yml has been created / overwritten...")


main()
