"""
run.py

Script to inject all the sensitive info needed to connect and run the bot, then run it.
Before executing, ensure that values for all properties in config.json have been provided.
"""

import json
import multiprocessing
import os
import signal
import subprocess
import sys
import time
import urllib.request


def main():
    if len(sys.argv) < 2:
        print(
            "Usage: python3 run.py run_target, where run_target = 'client' or 'server'"
        )
        return

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

    run_target = sys.argv[1]

    if run_target == "client":
        build_and_run_dotnet_client()
    elif run_target == "server":
        # Download Lavalink if needed
        handle_lavalink_binary()

        # Create fresh copy of application.yml, with injected secrets for Lavalink server
        handle_lavalink_config(user_config)

        run_lavalink_server_dedicated()
    else:
        print("fuck you")
        return

    # Download Lavalink if needed
    handle_lavalink_binary()

    # Create fresh copy of application.yml, with injected secrets for Lavalink server
    handle_lavalink_config(user_config)

    # TODO not sure why but try checking for lavalink plugins and running the client to download them?
    # if not os.path.isdir("plugins"):
    #    print(
    #        "No plugins detected, running the server for a sec in a dedicated thread so they download faster..."
    #    )

    #    signal.signal(signal.SIGALRM, handler)
    #    signal.alarm(60)

    #    try:
    #        run_lavalink_server_dedicated()
    #    except:
    #        print("done")

    #    # subprocess.run(["java", "-jar", "Lavalink/Lavalink.jar"])

    #    print("done process")

    #    # p = multiprocessing.Process(
    #    #    target=start_lavalink_and_yield_output, name="Lavalink"
    #    # )
    #    # p.start()

    #    # time.sleep(60)

    #    # p.terminate()
    #    # p.join()
    # else:
    #    print("test plugins")

    # .NET client start
    # build_and_run_dotnet_client()
    # prepare_dotnet_client()

    # run_lavalink_server_dedicated()
    # threading_test()

    # Run the Lavalink server and print output, because user may need to OAuth with Google and needs to see the url
    # for lavalink_output in start_lavalink_and_yield_output():
    #    print(lavalink_output, end="")


# def handler(signum, frame):
#    raise Exception("Timeout")


#def threading_test():
#    pool = multiprocessing.Pool()
#
#    pool.map(
#        run_command,
#        [
#            "java -jar Lavalink/Lavalink.jar",
#            "dotnet run -c  Release --project WidenBot",
#        ],
#    )


#def run_command(command):
#    if "dotnet" in command:
#        subprocess.Popen(
#            command,
#            shell=True,
#            stdout=subprocess.DEVNULL,
#            stderr=subprocess.STDOUT,
#        )
#    else:
#        subprocess.Popen(command, shell=True)


#def prepare_dotnet_client():
#    subprocess.run(
#        ["dotnet", "restore", "WidenBot"],
#        stdout=subprocess.DEVNULL,
#        stderr=subprocess.STDOUT,
#    )
#    print("...Packages restored successfully")
#
#    subprocess.run(
#        ["dotnet", "clean", "-c", "Release", "WidenBot"],
#        stdout=subprocess.DEVNULL,
#        stderr=subprocess.STDOUT,
#    )
#    print("...Project cleaned successfully")


def run_lavalink_server_dedicated():
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


def build_and_run_dotnet_client():
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

    # Run the client (without blocking)
    subprocess.run(
        ["dotnet", "run", "-c", "Release", "--project", "WidenBot"],
        stdout=subprocess.DEVNULL,
        stderr=subprocess.STDOUT,
    )
    print("...Client has been started")


#def start_lavalink_and_yield_output():
#    print("Starting Lavalink server...")
#
#    cmd = ["java", "-jar", "Lavalink/Lavalink.jar"]
#    popen = subprocess.Popen(cmd, stdout=subprocess.PIPE, universal_newlines=True)
#    assert popen.stdout is not None
#
#    for stdout_line in iter(popen.stdout.readline, ""):
#        yield stdout_line
#
#    popen.stdout.close()
#    return_code = popen.wait()
#
#    if return_code:
#        raise subprocess.CalledProcessError(return_code, cmd)


def get_file_contents(path):
    f = open(path, "r")
    raw = f.read()
    f.close()
    return raw


def write_file_contents(path, contents):
    f = open(path, "w")
    f.write(contents)
    f.close()


main()
