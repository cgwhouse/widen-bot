"""
widenbot.py

This script serves as the primary orchestrator for deploying and managing
instances of WidenBot via Docker. It parses a local `config.json` file to
dynamically configure and spin up independent bot instances across multiple
Discord servers.

Key functionalities include:
* Action routing: Supports `start`, `stop`, and `logs` operations for bot instances.
* Dynamic configuration: Generates necessary `.env` and `application.yml` files.
* Port allocation: Automatically finds and binds to available local ports for the client container.
* Docker orchestration: Interfaces directly with Docker Compose to build, recreate, and tear down containers.

Dependencies:
    * Docker and Docker Compose installed and running on the host machine.
    * A valid `config.json` file residing in the same directory as this script.

See widenbot.py --help for usage instructions.
"""

# TODO:
# full E2E

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

    argument_parser = get_argument_parser()
    args = argument_parser.parse_args()

    if args.action == "logs":
        view_logs(get_container_name(args.label, args.type))
        return

    config = get_config_json()
    if config is None:
        return

    if args.action == "start":
        write_application_yml(config)

    server_list = get_config_server_list(config)
    if server_list is None:
        return

    for i in range(len(server_list)):
        server_config = server_list[i]

        if not validate_label_and_enabled_flag(server_config):
            print(
                f"WARNING: Config #{i + 1} in config.json is not enabled, or not labeled correctly - skipping.\n"
            )
            continue

        if args.action == "stop":
            stop_widenbot_instance(server_config["label"])
            continue

        if not validate_server_config_for_start(server_config):
            print(
                f"WARNING: Config '{server_config["label"]}' in config.json is missing a required field - skipping.\n"
            )
            continue

        start_widenbot_instance(server_config)


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


def get_config_json():
    try:
        return json.loads(get_file_contents("config.json"))
    except FileNotFoundError:
        print("ERROR: config.json must exist alongside this script.\n")
        return None
    except JSONDecodeError:
        print("ERROR: config.json is not a valid JSON document.\n")
        return None


def get_config_server_list(config):
    if not (
        "discordServers" in config
        and isinstance(config["discordServers"], list)
        and len(config["discordServers"]) > 0
    ):
        print(
            "ERROR: 'discordServers' is missing or empty / malformed, refer to config.template.jsonc.\n"
        )
        return None

    return config["discordServers"]


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
            ],
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
        )

    print(f"INFO: WidenBot instance {label} has been stopped.\n")


def validate_server_config_for_start(server_config):
    return (
        "serverID" in server_config
        and isinstance(server_config["serverID"], str)
        and len(server_config["serverID"]) > 0
        and "botToken" in server_config
        and isinstance(server_config["botToken"], str)
        and len(server_config["botToken"]) > 0
    )


def start_widenbot_instance(server_config):
    # Generate password that client + server will use to talk to each other
    password = ""

    alphanumerics = list(
        string.ascii_lowercase + string.ascii_uppercase + string.digits
    )

    for _ in range(15):
        password += alphanumerics[random.randint(0, len(alphanumerics) - 1)]

    # We need to find an available port for the client container to bind to on the host
    client_port = 80
    while True:
        with contextlib.closing(socket(AF_INET, SOCK_STREAM)) as sock:
            if sock.connect_ex(("127.0.0.1", client_port)) == 0:
                client_port += 1
            else:
                break

    print(
        f"INFO: Using port {client_port} for WidenBot instance '{server_config["label"]}'.\n"
    )

    # Write .env file for Docker
    write_env_file(server_config, password, client_port)

    subprocess.run(
        [
            "docker",
            "compose",
            "-p",
            # NOTE: Discord apparently doesn't allow caps in project names
            server_config["label"].lower(),
            "up",
            "--build",
            "--force-recreate",
            "--detach",
        ],
        cwd="./src",
    )

    print(f"INFO: WidenBot instance '{server_config["label"]}' has been started.\n")


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


def write_env_file(server_config, password, client_port):
    env_file_contents = f"CLIENT_PORT={client_port}\n"
    env_file_contents += f"INSTANCE_LABEL={server_config["label"]}\n"
    env_file_contents += f"WIDENBOT_PASSWORD={password}\n"
    env_file_contents += f"DISCORD_SERVER_ID={server_config["serverID"]}\n"
    env_file_contents += f"DISCORD_BOT_TOKEN={server_config["botToken"]}\n"

    # NOTE: Inject config for requiredChannel if provided, set to initial dummy value to prevent Docker warning
    if (
        "requiredChannel" in server_config
        and isinstance(server_config["requiredChannel"], str)
        and len(server_config["requiredChannel"]) > 0
    ):
        required_channel = server_config["requiredChannel"]
    else:
        required_channel = "none"

    env_file_contents += f"REQUIRED_CHANNEL={required_channel}\n"

    write_file_contents("src/.env", env_file_contents)


def get_file_contents(path):
    with open(path, "r") as f:
        raw = f.read()
        return raw


def write_file_contents(path, contents):
    with open(path, "w") as f:
        f.write(contents)


def get_container_name(label, type):
    return f"{label}-widenbot-{type}"


def get_argument_parser():
    parser = ArgumentParser(
        prog="widenbot.py",
        description="Run script for WidenBot.",
        epilog="Visit https://github.com/cgwhouse/widen-bot for setup instructions.",
    )

    action_help_text = """
The 'start' and 'stop' actions will start or stop all WidenBots in config.json.
If any WidenBots are already running and 'start' is specified, they will be restarted.
The 'logs' action shows specific client or server container logs in --follow mode.
Typically, if there is a problem, the server logs will be the more helpful of the two.
    """

    parser.add_argument(
        "action", type=str, choices=["start", "stop", "logs"], help=action_help_text
    )

    action_is_logs = "widenbot.py logs" in " ".join(sys.argv)

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


if __name__ == "__main__":
    main()
