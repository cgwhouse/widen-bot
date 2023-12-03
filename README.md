# WidenBot

[![DigitalOcean Referral Badge](https://web-platforms.sfo2.cdn.digitaloceanspaces.com/WWW/Badge%201.svg)](https://www.digitalocean.com/?refcode=eb2eb2fc76ce&utm_campaign=Referral_Invite&utm_medium=Referral_Program&utm_source=badge)

## TODO

- Write setup guide
- Update setup script to download Lavalink for you
- Update setup script to check whether it actually needs to do the config injections
- Play with updating the default volume on channel join
- Better handling for queries, multiple sources + playlists
- Go through rest of docs and add more fancy / relevant things
  - top priority commands: skip, pause, resume
- Consider SponsorBlock plugin

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
