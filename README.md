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
- Python 3.9+
- Docker

### Hosting

I host my personal instance on a Raspberry Pi, but if you need or want a hosting platform, I recommend DigitalOcean. Here is my referral badge:

[![DigitalOcean Referral Badge](https://web-platforms.sfo2.cdn.digitaloceanspaces.com/WWW/Badge%201.svg)](https://www.digitalocean.com/?refcode=eb2eb2fc76ce&utm_campaign=Referral_Invite&utm_medium=Referral_Program&utm_source=badge)

### Configuration

1. Clone this repository:

   ```bash
   git clone https://github.com/cgwhouse/widen-bot && cd widen-bot
   ```

2. Create a `config.json` by running the interactive setup wizard (no manual JSON editing required):

   ```bash
   ./widenbot init
   ```

   To add another server later, run `./widenbot add`.

   Prefer to write the file by hand? Create `config.json` yourself using the shape below. The generated and hand-written files both reference [`config.schema.json`](config.schema.json) via the `"$schema"` key, which gives editors such as VS Code autocomplete and live validation; the schema documents every field:

   ```json
   {
     "$schema": "./config.schema.json",
     "maxMemory": "1G",
     "discordServers": [
       {
         "label": "myserver",
         "isEnabled": true,
         "serverID": "",
         "botToken": ""
       }
     ]
   }
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

3. Add the clientID and secret to `config.json` (leave both blank to keep Spotify disabled)

### Server memory

Each WidenBot instance runs its own Lavalink server, which is a Java process. The top-level `"maxMemory"` setting in `config.json` controls its max JVM heap (`-Xmx`), and is **required**. The `init` wizard prompts for it and suggests a value based on your host's RAM — if you'd rather not decide, just press Enter to accept the suggestion.

```json
"maxMemory": "1G"
```

How to choose a value:

- **No less than `512M`.** Below this, Lavalink can become unstable or get killed under load.
- **No more than ~50% of the host's total RAM.** Leave at least ~1G free for the operating system, the bot client, and anything else running on the machine. Never set it at or above total RAM — the container will be OOM-killed.
- **It applies per instance.** Each enabled server gets its own Lavalink server with this heap, so running N servers uses roughly N times this amount. Budget for that if you run several.

For example, on a 2 GB Raspberry Pi running one bot, `512M`–`1G` is a sensible range; on an 8 GB host running a single bot, `2G`–`4G` is comfortable. (The previous fixed `6G` value would not even fit on most of these hosts.)

## Run

- If on a Linux host, ensure the `src/plugins` directory is writable by Docker so Lavalink can download its plugins, e.g. `chmod -R u+rwX src/plugins`.
- From the root of the repository, execute:

```bash
./widenbot start
```

By default this pulls the prebuilt client image from GitHub Container Registry. To build the image locally from the Dockerfile instead (for development, or if you've modified the C# client), pass `--build`:

```bash
./widenbot start --build
```

Use `./widenbot stop` to stop all instances, and `./widenbot --help` to see every command.

## Troubleshooting

- **Inspect logs.** Most problems surface in the server logs:

  ```bash
  ./widenbot logs -l <label> -t server   # or: -t client
  ```

- **Bot is online but won't play / connect.** Check the server logs first — playback issues are usually source-related (e.g. a YouTube change) and originate in Lavalink, not the C# client.
- **Server container keeps restarting or gets OOM-killed.** Lower `"maxMemory"` in `config.json` and run `start` again.
- **`config.json is not a valid JSON document`.** A hand-edited config has a syntax error (e.g. a trailing comma or a stray comment — JSON does not allow comments). Re-run `./widenbot init` to regenerate it, or validate it against `config.schema.json` in your editor.
