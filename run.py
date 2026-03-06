"""
TODO:

    try to remove any try catches that aren't actually needed
    Play around with malformed config.json full run
    full run of all commands / scenarios we can think of
    we don't want the logs to be too spaced out
"""

import contextlib
import json
import random
import string
import subprocess
import sys
from argparse import ArgumentParser
from json import JSONDecodeError
from socket import AF_INET, SOCK_STREAM, socket


def main():
    print("\nThank you for using WidenBot!\n")

    # Parse command line arguments
    argument_parser = get_argument_parser()
    args = argument_parser.parse_args()

    # Just cut to the chase if we're viewing logs
    if args.action == "logs":
        view_logs(get_container_name(args.label, args.type))
        return

    # We are either starting or stopping WidenBots, make sure config.json exists and load it
    try:
        config_json = json.loads(get_file_contents("config.json"))
    except FileNotFoundError:
        print("ERROR: config.json must exist alongside this script\n")
        return
    except JSONDecodeError:
        print("ERROR: config.json is not a valid JSON document\n")
        return

    # If we're starting WidenBots, write application.yml (once for all configured bots)
    # This includes adding configs for any optional integrations (Spotify, Apple Music)
    if args.action == "start":
        write_application_yml(config_json)

    # Get server list from the config
    if not (
        "discordServers" in config_json
        and isinstance(config_json["discordServers"], list)
        and len(config_json["discordServers"]) > 0
    ):
        print(
            "ERROR: 'discordServers' is missing or empty / malformed, refer to config.template.jsonc\n"
        )
        return

    server_list = config_json["discordServers"]

    for i in range(len(server_list)):
        server_config = server_list[i]

        # Whether starting or stopping, we need to check labels and IsEnabled flags
        if not validate_label_and_enabled_flag(server_config):
            print(
                f"WARNING: Config #{i + 1} in config.json is not enabled, or not labeled correctly - skipping\n"
            )
            continue

        # If desired action is stop, do that and move on
        if args.action == "stop":
            stop_widenbot_instance(server_config["label"])
            continue

        # We are starting, make sure we have the minimum configs required
        if not validate_server_config_for_start(server_list[i]):
            print(
                f"WARNING: Config '{server_config["label"]}' in config.json is missing a required field - skipping\n"
            )
            continue

        # TODO:
        # build out the remaining bits of the config that we need to start
        # write env file

        start_widenbot_instance(server_config)


def get_argument_parser():
    parser = ArgumentParser(
        prog="run.py",
        description="Run script for WidenBot.",
        epilog="Visit https://github.com/cgwhouse/widen-bot for setup instructions.",
    )

    action_help_text = """
The 'start' and 'stop' actions will start or stop all WidenBots in config.json.
If any WidenBots are already running and 'start' is specified, they will be restarted.
The 'logs' action shows specific client or server container logs in --follow mode.
Typically, the server logs will be the more helpful of the two if there is a problem.
    """

    parser.add_argument(
        "action", type=str, choices=["start", "stop", "logs"], help=action_help_text
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


def view_logs(container_name):
    try:
        subprocess.run(
            [
                "docker",
                "logs",
                container_name,
                "--follow",
            ]
        )
    except KeyboardInterrupt:
        pass


def validate_label_and_enabled_flag(server_config):
    return (
        # Label must be present + string + alphanumeric
        "label" in server_config
        and isinstance(server_config["label"], str)
        and server_config["label"].isalnum()
        # Max length of 112, because 16 chars are assumed and Docker max container name length is 128 chars
        and len(server_config["label"]) <= 112
        # Config must be enabled
        and "isEnabled" in server_config
        and isinstance(server_config["isEnabled"], bool)
        and server_config["isEnabled"]
    )


def stop_widenbot_instance(label):
    for type in ["client", "server"]:
        subprocess.run(
            [
                "docker",
                "container",
                "kill",
                get_container_name(label, type),
            ]
        )

    print(f"INFO: WidenBot instance {label} has been stopped.\n")


def validate_server_config_for_start(server_config):

    try:
        return len(server_config["serverID"]) > 0 and len(server_config["botToken"] > 0)
    except KeyError:
        return False


def validate_user_config(action):
    try:
        # # Open the config.json
        # user_config_list = loads(get_file_contents("config.json"))

        # # Only need to validate label and isEnabled if stopping bots or viewing logs
        # if action != "start":
        #     for user_config in user_config_list:
        #         if user_config["label"] == "" or not user_config["label"].isalnum():
        #             return None

        #         if user_config["isEnabled"] == "":
        #             return None

        #         if not user_config["isEnabled"]:
        #             print(
        #                 f"...Skipping {user_config['label']} because isEnabled is false"
        #             )
        #             continue

        #     return user_config_list

        # Start at 80 and increment by 1 for each bot in the array
        current_port = 80

        # Validate each config in the array
        for user_config in user_config_list:

            # if (
            #     user_config["discord"]["serverID"] == ""
            #     or user_config["discord"]["botToken"] == ""
            # ):
            #     return None

            # if (
            #     user_config["spotify"]["clientID"] == ""
            #     or user_config["spotify"]["clientSecret"] == ""
            # ):
            #     return None

            # Generate new password for this run
            alphanumerics = list(
                string.ascii_lowercase + string.ascii_uppercase + string.digits
            )

            password = ""

            for _ in range(15):
                password += alphanumerics[random.randint(0, len(alphanumerics) - 1)]

            user_config["password"] = password

            # Make sure current_port is available, otherwise move on to next
            while True:
                with contextlib.closing(socket(AF_INET, SOCK_STREAM)) as sock:
                    if sock.connect_ex(("127.0.0.1", current_port)) == 0:
                        current_port += 1
                    else:
                        print(
                            f"Found port {current_port} for WidenBot instance {user_config['label']}!"
                        )
                        break

            user_config["clientPort"] = current_port
            current_port += 1

        return user_config_list
    except (FileNotFoundError, KeyError, TypeError, ValueError):
        return None


def start_widenbot_instance(server_config):
    # print("Starting WidenBot...")

    # FIXME: can we do this without chdir?
    # chdir("./src")

    # labels = list()

    # for server_config in user_config_list:

    # Check enabled flag and skip

    # labels.append(server_config["label"])

    # Lavalink application.yml
    # write_application_yml(
    #     server_config["spotify"]["clientID"], server_config["spotify"]["clientSecret"]
    # )

    # Docker .env file
    write_env_file(server_config)

    subprocess.run(
        [
            "docker",
            "compose",
            "-p",
            server_config["label"],
            "up",
            "--build",
            "--force-recreate",
            "--detach",
        ]
    )


# print(f"\nWidenBot instance(s) {', '.join(labels)} are now running!")


def write_application_yml(server_config):
    application_yml = get_file_contents("src/application.template.yml")

    # If Spotify integration is configured, add required bits to the YAML
    if (
        "spotify" in server_config
        and server_config["spotify"] != None
        and "clientID" in server_config["spotify"]
        and "clientSecret" in server_config["spotify"]
        and isinstance(server_config["spotify"]["clientID"], str)
        and isinstance(server_config["spotify"]["clientSecret"], str)
    ):
        application_yml = (
            application_yml.replace("spotify: false", "spotify: true")
            .replace("SPOTIFY_CLIENT_ID", server_config["spotify"]["clientID"])
            .replace("SPOTIFY_CLIENT_SECRET", server_config["spotify"]["clientSecret"])
        )

    write_file_contents("src/application.yml", application_yml)
    print("INFO: Wrote updated src/application.yml\n")


def write_env_file(user_config):
    env_file_contents = f"INSTANCE_LABEL={user_config['label']}\n"

    env_file_contents += f"USE_SPONSORBLOCK={user_config['useSponsorBlock']}\n"

    env_file_contents += f"DISCORD_SERVER_ID={user_config['discord']['serverID']}\n"
    env_file_contents += f"DISCORD_BOT_TOKEN={user_config['discord']['botToken']}\n"

    # If provided, inject requiredChannel too
    # Set to initial dummy value to prevent Docker warning
    required_channel = "none"
    if (
        "requiredChannel" in user_config["discord"]
        and user_config["discord"]["requiredChannel"] is not None
    ):
        required_channel = user_config["discord"]["requiredChannel"]

    env_file_contents += f"REQUIRED_CHANNEL={required_channel}\n"

    # Internally managed env vars, users don't mess with these directly
    env_file_contents += f"CLIENT_PORT={user_config['clientPort']}\n"
    env_file_contents += f"LAVALINK_PASSWORD={user_config['password']}\n"

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
