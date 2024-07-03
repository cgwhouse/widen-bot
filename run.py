"""
run.py
WidenBot Dev Team
"""

import argparse
import json
import os
import random
import string
import subprocess
import sys


def main():
    # Handle args
    parser = get_parser()
    args = parser.parse_args()

    # Handle config.json
    user_config_list = handle_user_config()

    if user_config_list is None:
        print(
            "config.json must exist in main directory (same as this script), and must be fully specified. See 'WidenBot Config' section of README.md"
        )
        return

    # If no label, start / restart all bots and exit
    if args.label is None:
        run_bots(user_config_list)
        return

    # Validate provided label
    labels = list()
    for user_config in user_config_list:
        labels.append(user_config["label"])

    if args.label not in labels:
        parser.print_help()
        return

    client_container = f"{args.label}-widenbot-client"
    server_container = f"{args.label}-widenbot-server"

    # Perform stop action and exit
    if args.action == "stop":
        subprocess.run(["docker", "container", "kill", client_container])
        subprocess.run(["docker", "container", "kill", server_container])

        print(f"WidenBot instance {args.label} has been stopped.")
        return

    # Assume logs action
    if args.type == "client":
        subprocess.run(["docker", "logs", client_container, "--follow"])
    else:
        subprocess.run(["docker", "logs", server_container, "--follow"])


def get_parser():
    parser = argparse.ArgumentParser(
        prog="run.py",
        description="Run script for WidenBot.",
        epilog="Visit https://github.com/cgwhouse/widen-bot for setup instructions.",
    )

    parser.add_argument(
        "-l",
        "--label",
        required=False,
        type=str,
        help="Use to direct an --action at a given WidenBot instance. If not provided, all bots specified in config.json will be rebuilt and restarted.",
    )

    action_is_required = "--label" in sys.argv or "-l" in sys.argv

    parser.add_argument(
        "-a",
        "--action",
        required=action_is_required,
        type=str,
        choices=["stop", "logs"],
        help="The 'stop' action stops the client and server containers for the given label, and 'logs' shows current client or server container logs in --follow mode.",
    )

    joined_args = " ".join(sys.argv)
    type_is_required = "--action logs" in joined_args or "-a logs" in joined_args

    parser.add_argument(
        "-t",
        "--type",
        required=type_is_required,
        type=str,
        choices=["client", "server"],
        help="Whether to view client or server logs.",
    )

    return parser


def handle_user_config():
    try:
        user_config_list = json.loads(get_file_contents("config.json"))

        # Start at 80 and increment by 1 for each bot in the array
        client_port = 80

        # Validate each config in the array
        for user_config in user_config_list:
            if user_config["label"] == "" or not user_config["label"].isalnum():
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

            user_config["clientPort"] = client_port
            client_port += 1

        return user_config_list
    except (FileNotFoundError, KeyError, ValueError):
        return None


def run_bots(user_config_list):
    print("Starting WidenBot...")

    os.chdir("./src")

    labels = list()
    for user_config in user_config_list:
        labels.append(user_config["label"])

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

    print(f"\nWidenBot instance(s) {', '.join(labels)} are now running!")


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
