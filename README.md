# WidenBot

WidenBot is a private music bot for Discord.

## TODO

- Tell the dev channel about the new stuff
- Handle playlists / albums, i.e. queuing multiple things at once
- Document commands, high-level features, prior to setup guide

### Contributing

Contributions are welcome! Please autoformat any .NET code using [CSharpier](https://csharpier.com/).

## Setup Guide

**Disclaimer: This project is not intended to be a plug-and-play music bot that can be added to a server in a couple of clicks. It requires a bit of manual setup, and hosting is up to you. A single instance of WidenBot cannot serve more than once server simultaneously.**

WidenBot consists of three components:

- A discord bot, configured via the Discord developer portal
- The audio server ([Lavalink](https://github.com/lavalink-devs/Lavalink)), a standalone Java application
- The discord client (WidenBot), a .NET service

### Dependencies

- Python 3
- .NET SDK 8 or newer
- JRE 17 or newer (OpenJDK recommended)

Make sure the outputs of `python3 --version`, `dotnet --list-sdks`, and `java --version` each look correct before continuing.

### Discord Developer Portal

1. Go to the Discord Developer Portal, login as the Discord account should own the bot, and create a new application
2. Within the Bot settings:

   a. Disable "Public Bot" (optional, but recommended)

   b. Enable "Server Members Intent" and "Message Content Intent"

   c. Click the "Reset Token" button and **save the resulting token for later**

3. Within the OAuth settings:

   a. Add a redirect for `https://discord.com` (Under "General" sub-category)

   b. Generate an invite URL with the `bot` scope, and the following permissions:

   - Read Messages/View Channels
   - Send Messages
   - Manage Messages
   - Use Slash Commands
   - Connect
   - Speak

4. Use the generated URL to invite the bot to a server of your choice

### Spotify

1. Go to the [Spotify developer dashboard](https://developer.spotify.com/dashboard) and sign in with whatever Spotify account you want to use

2. Create a new app (Development mode, other defaults should be sufficient), and save the client ID and secret for later

### WidenBot Config

1. Clone this repository
2. Copy the contents of `WidenBot/config.template.json` into a new file called `WidenBot/config.json`, and provide values as follows:

   ```json
   {
     "DiscordBotToken": "Bot token from Discord Developer Portal",
     "DiscordServerID": "Right-click on the server you invited the bot to, select 'Copy Server ID'",
     "LavalinkPassword": "An arbitrary alphanumeric passphrase, anything you want",
     "SpotifyClientID": "Client ID from Spotify developer dashboard",
     "SpotifyClientSecret": "Client secret from Spotify developer dashboard",
     "YouTubeEmail": "Email for a Google account you have access to",
     "YouTubePassword": "Password for the Google account"
   }
   ```

### Running the Bot

From the root of the repository, execute `python3 run.py`.

**NOTE: On first run, inspect the Lavalink output. If you see any log messages about failing OAuth to Google / Youtube, follow the instructions in the log message, you may need to grant permissions to YouTube. This OAuth portion should be a one-time step per WidenBot setup.**

The bot should be able to be hosted from any machine that can install the [dependencies](#dependencies). If you need a hosting platform, DigitalOcean makes it pretty easy to set up a server, feel free to use the referral badge below:

[![DigitalOcean Referral Badge](https://web-platforms.sfo2.cdn.digitaloceanspaces.com/WWW/Badge%201.svg)](https://www.digitalocean.com/?refcode=eb2eb2fc76ce&utm_campaign=Referral_Invite&utm_medium=Referral_Program&utm_source=badge)
