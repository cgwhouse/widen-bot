# WidenBot

WidenBot is a simple, self-hosted music bot for Discord.

The purpose of this project is to serve as a reliable, personal jukebox
for you and your Discord servers.

Inspired by past and present titans such as Groovy, Rythm, ProBot, etc.
But this time: no unnecessary features, misbehavior, or monetization.

**Due to the nature of what a music bot is trying to accomplish,
it can be prone to breakage which is often outside of WidenBot's direct control.
In any case, please feel free to reach out directly or open an issue if you run into trouble.**

## Setup Guide

### Dependencies

- Git
- Python 3
- [Docker](https://docs.docker.com/engine/install/)

### Hosting

I host my personal instance on a Raspberry Pi, but if you need or want a hosting platform, I recommend DigitalOcean. Here is my referral badge:

[![DigitalOcean Referral Badge](https://web-platforms.sfo2.cdn.digitaloceanspaces.com/WWW/Badge%201.svg)](https://www.digitalocean.com/?refcode=eb2eb2fc76ce&utm_campaign=Referral_Invite&utm_medium=Referral_Program&utm_source=badge)

### Configuration

1. Clone this repository:

   ```bash
   git clone https://github.com/cgwhouse/widen-bot && cd widen-bot
   ```

2. Create a new `config.json` using the template:

   ```bash
   cp config.template.jsonc config.json
   ```

3. Login to the [Discord Developer Portal](https://discord.com/developers/applications). For each Discord server you want to add a WidenBot to, do the following:
   1. Create a new application

   2. Within the Bot settings:
      - Disable "Public Bot"
      - Enable "Server Members Intent" and "Message Content Intent"
      - Click the "Reset Token" button and add the token to your `config.json`

   3. Within the OAuth2 settings:
      - Add a redirect for `https://discord.com`
      - Generate an invite URL with the `applications.commands` and `bot` scopes,
        and the following Bot permissions:
        - View Channels
        - Send Messages
        - Manage Messages
        - Use Slash Commands
        - Connect
        - Speak
      - Use the generated URL to invite the bot to your server (paste in web browser)

   4. Right-click on your server in Discord, select "Copy Server ID", and add the value to your `config.json`

### Spotify Integration (optional, requires Spotify Premium)

1. Sign in to the [Spotify Developer Dashboard](https://developer.spotify.com/dashboard)

2. Create a new Spotify app (Development mode, other defaults should be sufficient)

3. Add the clientID and secret to `config.json`

## Run

- If on a Linux host, ensure the `src/plugins` directory has sufficient permissions.
- Remove template comments from `config.json`
- From the root of the repository, execute:

```bash
python3 widenbot.py start
```
