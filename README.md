# WidenBot

[![DigitalOcean Referral Badge](https://web-platforms.sfo2.cdn.digitaloceanspaces.com/WWW/Badge%201.svg)](https://www.digitalocean.com/?refcode=eb2eb2fc76ce&utm_campaign=Referral_Invite&utm_medium=Referral_Program&utm_source=badge)

WidenBot is a private music bot for Discord.

## Setup Guide

**Disclaimers:**

**WidenBot is introverted, and not intended to be a plug-and-play music bot that can be added to a server in a couple of clicks. It requires some manual setup, and hosting is up to you. For an out-of-the-box experience, the WidenBot team recommends [CakeyBot](https://cakey.bot/), or Tanner Gabriel's [discord-bot](https://github.com/TannerGabriel/discord-bot) repo as alternatives to this project.**

**A single instance of WidenBot cannot serve more than one server at a time. This is by design; WidenBot just wants to hang out with you and your friends, no one else.**

WidenBot consists of three components:

- A discord bot, configured via the Discord developer portal
- The server (Lavalink), a standalone Java application
- The client (WidenBot), a .NET service

Requires a Discord bot already configured and invited to server

Remember note about lavalink google oauth prompt

### Part 1: Discord Bot

1. Go to the Discord Developer Portal, login as yourself / whichever Discord account should own the bot, and create a new application

2. Within the Bot settings:

   a. Disable "Public Bot" (optional, but recommended)

   b. Enable "Server Members Intent" and "Message Content Intent"

   c. Click the "Reset Token" button and save the resulting token for later

3. Within the OAuth settings:

   a. Add a redirect for `https://discord.com` (Under "General" sub-category)

   b. Generate an invite URL with the `bot` scope, and the following bot permissions:

   - Read Messages/View Channels
   - Send Messages
   - Manage Messages
   - Add Reactions
   - Use Slash Commands
   - Connect
   - Speak

4. Use the generated URL to invite the bot to a server of your choice

5. In Discord, right-click on the server you invited the bot to, select "Copy Server ID", and paste the server ID somewhere for later

### Part 2: WidenBot

1. Clone this repository

2. Copy the contents of `config.template.json` into a new file called `config.json`

3. Provide the bot token from step 2c above as the value for `DiscordBotToken`

4. Provide the server ID from step 6 above as the value for `DiscordServerID`

5. Using a Google account that you have access to, enter the email + password combo into `YoutubeEmail` and `YouTubePassword` respectively

### Dependencies

- .NET SDK 7 or newer
- JRE 17 or newer (OpenJDK recommended)

Make sure the outputs of both `dotnet --list-sdks` and `java --version` look correct before continuing.

### Setup Guide

1. Clone this repo on the machine that will be running WidenBot
2. Change current directory to the root of this repo
3. That's it!

## TODO

- Write setup guide
- More commands: skip, pause and resume
- Better default volume on join
- Leave when voice channel empty
- Handle Spotify / other sources, handle playlists
- SponsorBlock plugin
