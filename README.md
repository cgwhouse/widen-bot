# WidenBot

WidenBot is your private music bot for Discord.

**Disclaimer: Due to the nature of what a music bot is trying to accomplish, it
can be prone to occasional breakage.**

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
2. Copy the contents of `config.template.json` into a new file called `config.json`
3. For each instance of WidenBot you want to run, add an object to the array
   like so:

   ```json
   {
     "label": "An arbitrary label for this instance of WidenBot",
     "isEnabled": true,
     "useSponsorBlock": true,
     "discord": {
       "serverID": "Right-click on server in Discord and select 'Copy Server ID'",
       "botToken": "Bot token from Discord Developer Portal",
       "requiredChannel": "Right-click on channel and select 'Copy Channel ID' to restrict channel usage, set to null if you want to handle via server roles instead"
     },
     "spotify": {
       "clientID": "Client ID from Spotify developer dashboard",
       "clientSecret": "Client secret from Spotify developer dashboard"
     }
   }
   ```

_NOTE: The `useSponsorBlock` flag optionally enables integration with [SponsorBlock](https://sponsor.ajay.app/)
for YouTube. Mileage may vary, disable if you are having issues._

## Running the Bot

1. If on a Linux host, ensure the `src/plugins` directory has sufficient permissions.

2. From the root of the repository, execute:

   ```bash
   python3 run.py start
   ```

The bot can be hosted from any machine that can install the [dependencies](#dependencies).
If you need a hosting platform, DigitalOcean makes it easy to set up a server,
feel free to use the referral badge below which apparently provides a credit:

[![DigitalOcean Referral Badge](https://web-platforms.sfo2.cdn.digitaloceanspaces.com/WWW/Badge%201.svg)](https://www.digitalocean.com/?refcode=eb2eb2fc76ce&utm_campaign=Referral_Invite&utm_medium=Referral_Program&utm_source=badge)
