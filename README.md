# WidenBot

[![DigitalOcean Referral Badge](https://web-platforms.sfo2.cdn.digitaloceanspaces.com/WWW/Badge%201.svg)](https://www.digitalocean.com/?refcode=eb2eb2fc76ce&utm_campaign=Referral_Invite&utm_medium=Referral_Program&utm_source=badge)

## TODO

- Write setup guide
- Better handling for queries, multiple sources + playlists
- Implement more commands
- Go through rest of docs and add more fancy / relevant things
- Consider SponsorBlock plugin
- Test systemd service on server

## Get Started

not intended to be available to more than one server at a time. widenbot is an introvert
requires a Discord bot already configured and invited to server
really two components, the sound server and the client
remember note about lavalink google oauth prompt

### Dependencies

- .NET SDK 7 or newer
- JRE 17 or newer

Make sure the outputs of both `dotnet --list-sdks` and `java --version` look correct before continuing.

### Setup Guide

1. Clone this repo on the machine that will be running WidenBot
2. Change current directory to the root of this repo
3. That's it!
