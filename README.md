# WidenBot

WidenBot is your private music bot for Discord.

**Disclaimers:**

- **This project is not intended to be a plug-and-play music bot that can be
  added to a Discord server in a couple of clicks. It requires a bit of manual
  setup, and hosting is up to you.**
- **A single instance of WidenBot cannot serve more than one Discord server simultaneously.**
- **Due to the nature of what a music bot is trying to accomplish, it can be
  prone to occasional breakage.**

A WidenBot instance consists of three components:

- A discord bot, configured via the Discord developer portal
- The audio server ([Lavalink](https://github.com/lavalink-devs/Lavalink)), a
  standalone Java application
- The client, a .NET service

## Dependencies

- Python 3
- Docker

Make sure the outputs of `python3 --version`, `docker version`, and
`docker compose version` each look correct before continuing. Most OSes come
with Python, see [Docker install](https://docs.docker.com/engine/install/) for
instructions on installing Docker.

## Discord Developer Portal

1. Go to the Discord Developer Portal, login as the Discord account that should
   own the bot, and create a new application
2. Within the Bot settings:

   a. Disable "Public Bot" (optional, but recommended)

   b. Enable "Server Members Intent" and "Message Content Intent"

   c. Click the "Reset Token" button and save the resulting token for later

3. Within the OAuth settings:

   a. Add a redirect for `https://discord.com` (Under "General" sub-category)

   b. Generate an invite URL with the `application.commands` and `bot` scopes,
   and the following permissions:

   - Read Messages/View Channels
   - Send Messages
   - Manage Messages
   - Use Slash Commands
   - Connect
   - Speak

4. Use the generated URL to invite the bot to a server of your choice

## Spotify

1. Go to the [Spotify developer dashboard](https://developer.spotify.com/dashboard)
   and sign in with whatever Spotify account you want to use

2. Create a new app (Development mode, other defaults should be sufficient), and
   save the client ID and secret for later

## WidenBot Config

1. Clone this repository
2. Copy the contents of `config.template.json` into a new file called
   `config.json`, and provide values as follows:

   ```json
   {
     "clientPort": "Start with 80 for this, and for each instance of WidenBot
     hosted on the same machine, increment by 1"
     "label": "An arbitrary label for this instance of WidenBot",
     "discord": {
       "serverID": "Right-click on the server you invited the bot to, select
       'Copy Server ID'",
       "botToken": "Bot token from Discord Developer Portal"
     },
     "spotify": {
       "clientID": "Client ID from Spotify developer dashboard",
       "clientSecret": "Client secret from Spotify developer dashboard"
     }
   }
   ```

## Running the Bot

1. If on a Linux host, ensure the `Server/plugins` directory has at least 322 permissions.

2. From the root of the repository, execute:

   ```bash
   python3 run.py server
   ```

3. Without interrupting the running server command, open another terminal and execute:

   ```bash
   python3 run.py client
   ```

The bot can be hosted from any machine that can install the [dependencies](#dependencies).
If you need a hosting platform, DigitalOcean makes it easy to set up a server,
feel free to use the referral badge below which apparently provides a credit:

[![DigitalOcean Referral Badge](https://web-platforms.sfo2.cdn.digitaloceanspaces.com/WWW/Badge%201.svg)](https://www.digitalocean.com/?refcode=eb2eb2fc76ce&utm_campaign=Referral_Invite&utm_medium=Referral_Program&utm_source=badge)
