"""
run.py

Script to inject all the sensitive info needed to connect and run the bot, then run it.
Before executing, ensure that values for all properties in config.json have been provided.
"""

import json
import os
import random
import string
import subprocess
import sys


def main():
    # Handle run target
    try:
        run_target = sys.argv[1].lower()

        if run_target not in ["client", "server"]:
            raise IndexError
    except IndexError:
        print(
            "Usage: python3 run.py run_target, where run_target = 'client' or 'server'"
        )
        return

    # Extract config.json
    user_config = handle_user_config()

    if user_config is None:
        print(
            "config.json must exist in main directory (same as this script), and must be fully specified. See 'WidenBot Config' section of README.md"
        )
        return

    password = handle_password(run_target)

    if password == "":
        print("Unauthorized")
        return

    user_config["password"] = password

    if run_target == "client":
        run_client(user_config)
    else:
        run_server(user_config)


def handle_user_config():
    try:
        user_config = json.loads(get_file_contents("config.json"))

        # Validate config contents
        if user_config["label"] == "" or not user_config["label"].isalnum():
            return None

        if not int(user_config["clientPort"]):
            return None

        if (
            user_config["discord"]["serverID"] == ""
            or user_config["discord"]["botToken"] == ""
        ):
            return None

        if (
            user_config["spotify"]["clientID"] == ""
            or user_config["spotify"]["clientSecret"] == ""
        ):
            return None

        return user_config
    except (FileNotFoundError, KeyError, ValueError):
        return None


def handle_password(run_target):
    password = ""

    # If server, generate new password
    if run_target == "server":
        alphanumerics = list(
            string.ascii_lowercase + string.ascii_uppercase + string.digits
        )

        for _ in range(15):
            password += alphanumerics[random.randint(0, len(alphanumerics) - 1)]

    # If client, extract password from running server
    else:
        server_config = get_file_contents_as_lines("Server/.env")

        for line in server_config:
            if "LAVALINK_PASSWORD" in line:
                password = line.replace("LAVALINK_PASSWORD=", "").strip()
                break

    return password


def run_client(user_config):
    # Ensure server running first
    serverCheck = subprocess.run(
        ["docker", "container", "ls"], capture_output=True, text=True
    )

    if serverCheck.stdout.find(f"{user_config['label']}-widenbot-server") == -1:
        print(
            "Server must be running first, See 'Running the Bot' section of README.md"
        )
        return

    # Change current working directory to client
    os.chdir("./Client")

    # Run container
    subprocess.run(
        [
            "docker",
            "compose",
            "-p",
            user_config["label"],
            "up",
            "--build",
            "--force-recreate",
        ]
    )


def run_server(user_config):

    # Change current working directory to server
    os.chdir("./Server")

    # Create application.yml with injected Spotify secrets from config
    spotify_client_id = "SPOTIFY_CLIENT_ID"
    spotify_client_secret = "SPOTIFY_CLIENT_SECRET"

    lavalinkConfigRaw = get_file_contents("application.template.yml")

    lavalinkConfigUpdated = lavalinkConfigRaw.replace(
        spotify_client_id, user_config["spotify"]["clientID"]
    ).replace(spotify_client_secret, user_config["spotify"]["clientSecret"])

    write_file_contents("application.yml", lavalinkConfigUpdated)

    # Ensure client no longer running if one is currently
    # Since this is a new server run, the password will have changed
    # and the old client session is no longer valid
    kill_client_if_running(user_config)

    # Write user config contents to .env file
    write_env_file(user_config)

    # Run container
    subprocess.run(
        [
            "docker",
            "compose",
            "-p",
            user_config["label"],
            "up",
            "--build",
            "--force-recreate",
        ]
    )


def kill_client_if_running(user_config):
    # Check for client
    serverCheck = subprocess.run(
        ["docker", "container", "ls"], capture_output=True, text=True
    )

    clientName = f"{user_config['label']}-widenbot-client"

    if serverCheck.stdout.find(clientName) == -1:
        return

    subprocess.run(
        ["docker", "container", "kill", clientName],
        capture_output=True,
    )

    print("Currently running client has been killed...")


def write_env_file(user_config):
    env_file_contents = f"CLIENT_PORT={user_config['clientPort']}\n"
    env_file_contents += f"INSTANCE_LABEL={user_config['label']}\n"

    env_file_contents += f"DISCORD_SERVER_ID={user_config['discord']['serverID']}\n"
    env_file_contents += f"DISCORD_BOT_TOKEN={user_config['discord']['botToken']}\n"

    env_file_contents += f"LAVALINK_PASSWORD={user_config['password']}\n"

    write_file_contents("Server/.env", env_file_contents)
    write_file_contents("Client/.env", env_file_contents)


def get_file_contents(path):
    with open(path, "r") as f:
        raw = f.read()
        return raw


def get_file_contents_as_lines(path):
    with open(path, "r") as f:
        raw = f.readlines()
        return raw


def write_file_contents(path, contents):
    with open(path, "w") as f:
        f.write(contents)


if __name__ == "__main__":
    main()
