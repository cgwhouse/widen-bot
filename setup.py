"""
setup.py

Script to inject all the sensitive info needed to connect and run the bot.
Before executing, ensure that values for all properties in config.json have been provided.

Cristian W.
"""

import os, shutil, sys
import json


def get_file_contents(path):
    f = open(path, "r")
    raw = f.read()
    f.close()
    return raw


def write_file_contents(path, contents):
    f = open(path, "w")
    f.write(contents)
    f.close()


def main():
    # Get config.json
    user_config = json.loads(get_file_contents("config.json"))

    # Validate config
    for key in user_config:
        if user_config[key] == "":
            print("All values must be supplied in config.json. Exiting")

    # Handle .NET by injecting config values into Constants.cs
    rawConstants = get_file_contents("WidenBot/Constants.cs")

    updatedConstants = (
        rawConstants.replace("<DISCORD_BOT_TOKEN>", user_config["DiscordBotToken"])
        .replace("<DISCORD_SERVER_ID>", user_config["DiscordServerID"])
        .replace("<LAVALINK_PASSWORD>", user_config["LavalinkPassword"])
    )

    # Write updated
    write_file_contents("WidenBot/Constants.cs", updatedConstants)

    # rawDiscordClientHost.replace("<DISCORD_BOT_TOKEN>")


#    f.write(f"{full_package_name} abi_x86_32")
#    f.close()
# This script should never need to be run with elevated privileges, ensure


#    try:
#        is_admin = os.getuid() == 0
#        if is_admin:
#            print("\nThis script should not be executed with root privileges\n")
#            return
#    except AttributeError:
#        print(
#            "\nUnable to check for root privileges, is this a Windows machine? If not, please contact the developer\n"
#        )
#        return

# Retrieve and validate input
# try:
#    output_dir = sys.argv[1]
# except IndexError:
#    # Prompt user if not provided via CLI
#    output_dir = input(
#        "\nEnter desired path of script output, absolute or relative (to the current directory): "
#    )

# Create output directory
# try:
#    os.makedirs(output_dir)
# except FileNotFoundError:
#    print("\nInvalid path\n")
#    return
# except PermissionError:
#    print(
#        "\nInvalid path, permission was denied while attempting to create the directory\n"
#    )
#    return
# except FileExistsError:
#    choice = input(
#        "\nThis directory already exists, do you want to wipe its contents? Type 'yes' to confirm: "
#    )

#    if choice.lower() != "yes":
#        print("\nExiting\n")
#        return

#    # Wipe directory contents and recreate empty
#    shutil.rmtree(output_dir)
#    os.makedirs(output_dir)

## Init console output
# emerge_command = "sudo emerge -av "

# for full_package_name in package_names:
#    # Add full name to emerge command
#    emerge_command += f"{full_package_name} "

#    short_package_name = full_package_name.split("/")[1]

#    # Create file named short_package_name, need to write the full name with 32-bit flag into the file
#    f = open(f"{output_dir}/{short_package_name}", "w")
#    f.write(f"{full_package_name} abi_x86_32")
#    f.close()

## Output emerge command
# print(f"\n{emerge_command.strip()}")

# print(
#    f"\nAll done. Copy the contents of {output_dir} into /etc/portage/package.use, then execute the emerge command above!\n"
# )


main()
