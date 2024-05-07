"""
run.py

Script to inject all the sensitive info needed to connect and run the bot, then run it.
Before executing, ensure that values for all properties in config.json have been provided.
"""

import json
import os
import shutil
import subprocess
import sys


def main():
    run_target = handle_client_server_arg()

    if run_target == None:
        print(
            "Usage: python3 run.py run_target, where run_target = 'client' or 'server'"
        )
        return

    user_config = handle_user_config()

    if user_config == None:
        print(
            "config.json must exist in main directory (same as this script), and must be fully specified. See README.md"
        )
        return

    if run_target == "client":
        run_client()
    else:
        run_server(user_config)


def handle_client_server_arg():
    if len(sys.argv) < 2:
        return None

    run_target = sys.argv[1]

    if run_target not in ["client", "server"]:
        return None

    return run_target


def handle_user_config():
    try:
        user_config = json.loads(get_file_contents("config.json"))
    except FileNotFoundError:
        return None

    for key in user_config:
        if user_config[key] == "":
            return None

    return user_config


def run_client():
    # Copy config.json to the client working directory
    shutil.copyfile("./config.json", "Client/config.json")

    # Change current working directory to client
    os.chdir("./Client")

    print("Building and running WidenBot client...")

    # Run container
    subprocess.run(["docker", "compose", "up", "--build"])


def run_server(user_config):
    # Change current working directory to server
    os.chdir("./Server")

    # Create application.yml and docker-compose.yaml with injected secrets from config
    lavalink_password = "<LAVALINK_PASSWORD>"
    spotify_client_id = "<SPOTIFY_CLIENTID>"
    spotify_client_secret = "<SPOTIFY_CLIENTSECRET>"

    lavalinkConfigRaw = get_file_contents("application.template.yml")

    lavalinkConfigUpdated = (
        lavalinkConfigRaw.replace(lavalink_password, user_config["LavalinkPassword"])
        .replace(spotify_client_id, user_config["SpotifyClientID"])
        .replace(spotify_client_secret, user_config["SpotifyClientSecret"])
    )

    write_file_contents("application.yml", lavalinkConfigUpdated)

    print("application.yml has been created / overwritten...")

    dockerComposeRaw = get_file_contents("docker-compose.template.yaml")

    dockerComposeUpdated = dockerComposeRaw.replace(
        lavalink_password, user_config["LavalinkPassword"]
    )

    write_file_contents("docker-compose.yaml", dockerComposeUpdated)

    print("docker-compose.yaml has been created / overwritten...")

    # Run container
    subprocess.run(["docker", "compose", "up", "--build"])


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
