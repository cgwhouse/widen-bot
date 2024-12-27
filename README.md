# WidenBot

WidenBot is a simple, self-hosted (_by you!_) music bot for Discord.

Inspired by past and present titans such as Groovy, Rythm, ProBot, etc.
But this time: no unnecessary features, misbehavior, or monetization.

The purpose of this project is to serve as:

1. A reliable, personal jukebox for you and your Discord server(s)
2. A simple reference implementation of a Discord bot
   built with .NET and hosted via Docker

**Due to the nature of what a music bot is trying to accomplish,
it can be prone to breakage which is often outside of WidenBot's direct control.
In any case, please feel free to reach out directly or open an issue
if you run into trouble.**

## Quick Start

Hosting a WidenBot requires Python 3 and Docker.
See [Docker install](https://docs.docker.com/engine/install/)
for instructions on installing Docker.

1. Go to the [Discord Developer Portal](https://discord.com/developers/applications),
   login as the Discord account that should own the bot, and create a new application
2. Within the Bot settings:

   - Disable "Public Bot" (optional)
   - Enable "Server Members Intent" and "Message Content Intent"
   - Click the "Reset Token" button and save the resulting token for later

3. Within the OAuth settings:

   - Add a redirect for `https://discord.com` (Under "General" sub-category)
   - Generate an invite URL with the `application.commands` and `bot` scopes,
     and the following permissions:
     - Read Messages/View Channels
     - Send Messages
     - Manage Messages
     - Use Slash Commands
     - Connect
     - Speak

4. Use the generated URL to invite the bot to a server of your choice
5. Go to the [Spotify developer dashboard](https://developer.spotify.com/dashboard)
   and sign in with whatever Spotify account you want to use
6. Create a new Spotify app for OAuth purposes
   (Development mode and other defaults should be sufficient),
   and save the client ID and secret for later
7. Clone this repository, and copy the contents of
   `config.template.json` into a new file called `config.json`
8. For each instance of WidenBot you want to run, add an object to the array
   in your new `config.json` like so:

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

9. If on a Linux host, ensure the `src/plugins` directory has sufficient permissions.

10. From the root of the repository, execute:

```bash
python3 run.py start
```

The bot can be hosted from any machine that can install Docker and Python 3.
If you need a hosting platform, DigitalOcean makes it easy to set up a server,
feel free to use the referral badge below which apparently provides a credit:

[![DigitalOcean Referral Badge](https://web-platforms.sfo2.cdn.digitaloceanspaces.com/WWW/Badge%201.svg)](https://www.digitalocean.com/?refcode=eb2eb2fc76ce&utm_campaign=Referral_Invite&utm_medium=Referral_Program&utm_source=badge)
