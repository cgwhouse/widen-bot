import argparse
import json
import os
import random
import string
import subprocess
import sys


def main():
    parser = get_parser()
    args = parser.parse_args()
    user_config_list = handle_user_config()

    if user_config_list is None:
        print(
            "config.json must exist in main directory (same as this script), and must be fully specified. See 'WidenBot Config' section of README.md"
        )
        return

    if args.action == "start":
        run_all_bots(user_config_list)
    elif args.action == "stop":
        stop_all_bots(user_config_list)
    else:
        # Validate provided label
        labels = list()
        for user_config in user_config_list:
            labels.append(user_config["label"])

        if args.label not in labels:
            parser.print_help()
            return

        subprocess.run(
            ["docker", "logs", get_container_name(args.label, args.type), "--follow"]
        )


def get_parser():
    parser = argparse.ArgumentParser(
        prog="run.py",
        description="Run script for WidenBot. If no arguments are provided, all instances in config.json will be rebuilt and restarted.",
        epilog="Visit https://github.com/cgwhouse/widen-bot for setup instructions.",
    )

    parser.add_argument(
        "action",
        type=str,
        choices=["start", "stop", "logs"],
        help="The 'start' / 'stop' actions start or stop all WidenBots in config.json, and 'logs' shows specific client or server container logs in --follow mode.",
    )

    action_is_logs = "run.py logs" in " ".join(sys.argv)

    parser.add_argument(
        "-l",
        "--label",
        required=action_is_logs,
        type=str,
        help="The WidenBot instance whose logs should be viewed.",
    )

    parser.add_argument(
        "-t",
        "--type",
        required=action_is_logs,
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

            if user_config["isEnabled"] == "":
                return None

            if user_config["useSponsorBlock"] == "":
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


def run_all_bots(user_config_list):
    print("Starting WidenBot...")

    os.chdir("./src")

    labels = list()

    for user_config in user_config_list:

        # Check enabled flag
        if not user_config["isEnabled"]:
            print(f"...Skipping {user_config['label']} because 'isEnabled' is false")
            continue

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


def stop_all_bots(user_config_list):
    for user_config in user_config_list:
        label = user_config["label"]

        for type in ["client", "server"]:
            subprocess.run(
                [
                    "docker",
                    "container",
                    "kill",
                    get_container_name(label, type),
                ]
            )

        print(f"WidenBot instance {label} has been stopped.")


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

    env_file_contents += f"USE_SPONSORBLOCK={user_config['useSponsorBlock']}\n"

    # If provided, inject requiredChannel too
    if (
        "requiredChannel" in user_config["discord"]
        and user_config["discord"]["requiredChannel"] != None
    ):
        env_file_contents += (
            f"REQUIRED_CHANNEL={user_config['discord']['requiredChannel']}"
        )

    write_file_contents(".env", env_file_contents)


def get_file_contents(path):
    with open(path, "r") as f:
        raw = f.read()
        return raw


def write_file_contents(path, contents):
    with open(path, "w") as f:
        f.write(contents)


def get_container_name(label, type):
    return f"{label}-widenbot-{type}"


if __name__ == "__main__":
    main()
