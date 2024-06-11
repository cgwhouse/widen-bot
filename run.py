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
    # Get config.json
    user_config = handle_user_config()

    if user_config is None:
        print(
            "config.json must exist in main directory (same as this script), and must be fully specified. See 'WidenBot Config' section of README.md"
        )
        return

    # Check for action
    if len(sys.argv) > 1:
        handle_action(user_config, sys.argv[1].lower())
        return

    run_bot(user_config)


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

        # Generate new password for this run
        alphanumerics = list(
            string.ascii_lowercase + string.ascii_uppercase + string.digits
        )

        password = ""

        for _ in range(15):
            password += alphanumerics[random.randint(0, len(alphanumerics) - 1)]

        user_config["password"] = password

        return user_config
    except (FileNotFoundError, KeyError, ValueError):
        return None


def handle_action(user_config, action):
    label = user_config["label"]
    client_container = f"{label}-widenbot-client"
    server_container = f"{label}-widenbot-server"

    if action == "stop":
        subprocess.run(["docker", "container", "kill", client_container])
        subprocess.run(["docker", "container", "kill", server_container])
        print(f"WidenBot instance {label} has been stopped.")

    elif action == "client-logs":
        subprocess.run(["docker", "logs", client_container, "--follow"])

    elif action == "server-logs":
        subprocess.run(["docker", "logs", server_container, "--follow"])

    else:
        print("Unrecognized action.")


def run_bot(user_config):
    print("Starting WidenBot...")

    os.chdir("./src")

    # Lavalink application.yml
    write_application_yml(
        user_config["spotify"]["clientID"], user_config["spotify"]["clientSecret"]
    )

    print("...Created audio server config")

    # Docker .env file
    write_env_file(user_config)

    print("...Created environment variables")

    subprocess.run(
        [
            "docker",
            "compose",
            "-p",
            user_config["label"],
            "up",
            "--build",
            "--force-recreate",
            "--detach",
        ]
    )

    print(f"\nWidenBot instance {user_config['label']} is now running!")
    print("To view logs: 'python3 run.py client-logs' or 'python3 run.py server-logs'")
    print("To stop the bot: 'python3 run.py stop'")


def write_application_yml(client_id, client_secret):
    spotify_client_id = "SPOTIFY_CLIENT_ID"
    spotify_client_secret = "SPOTIFY_CLIENT_SECRET"

    lavalink_config_raw = get_file_contents("application.template.yml")

    lavalink_config_updated = lavalink_config_raw.replace(
        spotify_client_id, client_id
    ).replace(spotify_client_secret, client_secret)

    write_file_contents("application.yml", lavalink_config_updated)


def write_env_file(user_config):
    env_file_contents = f"CLIENT_PORT={user_config['clientPort']}\n"
    env_file_contents += f"INSTANCE_LABEL={user_config['label']}\n"

    env_file_contents += f"DISCORD_SERVER_ID={user_config['discord']['serverID']}\n"
    env_file_contents += f"DISCORD_BOT_TOKEN={user_config['discord']['botToken']}\n"

    env_file_contents += f"LAVALINK_PASSWORD={user_config['password']}\n"

    write_file_contents(".env", env_file_contents)


def get_file_contents(path):
    with open(path, "r") as f:
        raw = f.read()
        return raw


def write_file_contents(path, contents):
    with open(path, "w") as f:
        f.write(contents)


if __name__ == "__main__":
    main()
