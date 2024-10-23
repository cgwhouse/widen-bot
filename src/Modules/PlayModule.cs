using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Discord.Interactions;
using Lavalink4NET;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;

namespace WidenBot;

[RequireContext(ContextType.Guild)]
public sealed class PlayModule(IPlayerService playerService, IAudioService audioService)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("play", description: "Plays music", runMode: RunMode.Async)]
    public async Task PlayAsync(string query)
    {
        await DeferAsync().ConfigureAwait(false);

        var (player, errorEmbed) = await playerService
            .TryGetPlayerAsync(Context, allowConnect: true)
            .ConfigureAwait(false);

        if (player == null)
        {
            if (errorEmbed != null)
                await FollowupAsync(embed: errorEmbed).ConfigureAwait(false);

            return;
        }

        // Determine search mode we'll initially start with
        var bestGuessSearchMode = PlayerService.DetermineSearchMode(query);

        var multiItemCheck = PlayerService.IsMultiItem(query, bestGuessSearchMode);

        if (multiItemCheck)
        {
            await HandleMultiItemQuery(player, query, bestGuessSearchMode).ConfigureAwait(false);
            return;
        }

        await HandleTrackQuery(player, query, bestGuessSearchMode, playNext: false)
            .ConfigureAwait(false);
    }

    [SlashCommand(
        "playnext",
        description: "Plays music, skipping the queue (if any)",
        runMode: RunMode.Async
    )]
    public async Task PlayNextAsync(string query)
    {
        await DeferAsync().ConfigureAwait(false);

        var (player, errorEmbed) = await playerService
            .TryGetPlayerAsync(Context, allowConnect: true)
            .ConfigureAwait(false);

        if (player == null)
        {
            if (errorEmbed != null)
                await FollowupAsync(embed: errorEmbed).ConfigureAwait(false);

            return;
        }

        // Determine search mode we'll initially start with
        var bestGuessSearchMode = PlayerService.DetermineSearchMode(query);

        var multiItemCheck = PlayerService.IsMultiItem(query, bestGuessSearchMode);

        if (multiItemCheck)
        {
            await FollowupAsync("Sorry, /playnext cannot be used with album or playlist queries.")
                .ConfigureAwait(false);

            return;
        }

        await HandleTrackQuery(player, query, bestGuessSearchMode, playNext: true)
            .ConfigureAwait(false);
    }

    [SlashCommand(
        "umbrella",
        description: "When the sun shine, we shine together",
        runMode: RunMode.Async
    )]
    public async Task UmbrellaAsync()
    {
        await DeferAsync().ConfigureAwait(false);

        var (player, errorEmbed) = await playerService
            .TryGetPlayerAsync(Context, allowConnect: true)
            .ConfigureAwait(false);

        if (player == null)
        {
            if (errorEmbed != null)
                await FollowupAsync(embed: errorEmbed).ConfigureAwait(false);

            return;
        }

        var track = await audioService
            .Tracks.LoadTrackAsync("Umbrella Rhianna", TrackSearchMode.YouTube)
            .ConfigureAwait(false);

        if (track == null)
        {
            await FollowupAsync("😖 No results.").ConfigureAwait(false);
            return;
        }

        await player.PlayAsync(track).ConfigureAwait(false);

        await FollowupAsync("Ooh, baby, it's rainin'!").ConfigureAwait(false);
    }

    [SlashCommand("holes", description: "I'm tired of this, Grandpa!", runMode: RunMode.Async)]
    public async Task HolesAsync()
    {
        await DeferAsync().ConfigureAwait(false);

        var (player, errorEmbed) = await playerService
            .TryGetPlayerAsync(Context, allowConnect: true)
            .ConfigureAwait(false);

        if (player == null)
        {
            if (errorEmbed != null)
                await FollowupAsync(embed: errorEmbed).ConfigureAwait(false);

            return;
        }

        var holesSoundtrack = ImmutableList.Create(
            "https://www.youtube.com/watch?v=no0Q92S-k0g",
            "https://www.youtube.com/watch?v=kZ1A5w8szqo",
            "https://www.youtube.com/watch?v=-ANbCnTQOvU",
            "https://www.youtube.com/watch?v=J2TTwV_ItIc",
            "https://www.youtube.com/watch?v=jBdCSR6WtbQ",
            "https://www.youtube.com/watch?v=vA_XqkqY-e4",
            "https://www.youtube.com/watch?v=MoZwMwFIhN0",
            "https://www.youtube.com/watch?v=_uoo-h6cnoQ",
            "https://www.youtube.com/watch?v=fRwUN3E_tyI",
            "https://www.youtube.com/watch?v=ziDG8bBqp38",
            "https://www.youtube.com/watch?v=6jbGEJH3NQU",
            "https://www.youtube.com/watch?v=lOEpdIvbmds",
            "https://www.youtube.com/watch?v=2-MHXtjR7Hk",
            "https://www.youtube.com/watch?v=7P78_Lr8EUQ",
            "https://www.youtube.com/watch?v=a-DhqDgHZlk"
        );

        foreach (var url in holesSoundtrack)
        {
            var track = await audioService
                .Tracks.LoadTrackAsync(url, TrackSearchMode.YouTube)
                .ConfigureAwait(false);

            if (track != null)
                await player.PlayAsync(track).ConfigureAwait(false);
        }

        await FollowupAsync("The old man's been stealin!").ConfigureAwait(false);
    }

    [SlashCommand("creed", description: "Hold me down", runMode: RunMode.Async)]
    public async Task CreedAsync()
    {
        await DeferAsync().ConfigureAwait(false);

        var (player, errorEmbed) = await playerService
            .TryGetPlayerAsync(Context, allowConnect: true)
            .ConfigureAwait(false);

        if (player == null)
        {
            if (errorEmbed != null)
                await FollowupAsync(embed: errorEmbed).ConfigureAwait(false);

            return;
        }

        var track = await audioService
            .Tracks.LoadTrackAsync("One Last Breath Creed", TrackSearchMode.YouTube)
            .ConfigureAwait(false);

        if (track == null)
        {
            await FollowupAsync("😖 No results.").ConfigureAwait(false);
            return;
        }

        await player.PlayAsync(track).ConfigureAwait(false);

        await FollowupAsync("I cried out, \"Heaven save me\"").ConfigureAwait(false);
    }

    private async Task HandleTrackQuery(
        QueuedLavalinkPlayer player,
        string query,
        TrackSearchMode bestGuessSearchMode,
        bool playNext
    )
    {
        // If this is a direct YouTube link and happens to be from
        // a video within a playlist, trim the rest of the query string, otherwise
        // it will just queue the first item in the playlist
        if (
            bestGuessSearchMode == TrackSearchMode.YouTube
            && query.Contains("https")
            && query.Contains("&")
        )
            query = query.Substring(0, query.IndexOf('&'));

        var track = await audioService
            .Tracks.LoadTrackAsync(query, bestGuessSearchMode)
            .ConfigureAwait(false);

        // If we didn't get anything, fall back to YouTube search
        if (track == null && bestGuessSearchMode != TrackSearchMode.YouTube)
            track = await audioService
                .Tracks.LoadTrackAsync(query, TrackSearchMode.YouTube)
                .ConfigureAwait(false);

        if (track == null)
        {
            await FollowupAsync("😖 No results.").ConfigureAwait(false);
            return;
        }

        if (playNext)
        {
            if (player.CurrentItem == null)
            {
                await player.PlayAsync(track).ConfigureAwait(false);

                await FollowupAsync($"🔈 Playing: {track.Title} ({track.Uri})")
                    .ConfigureAwait(false);
            }
            else
            {
                await player.Queue.InsertAsync(0, new TrackQueueItem(track)).ConfigureAwait(false);

                await FollowupAsync($"🔈 Added to front of queue: {track.Title} ({track.Uri})")
                    .ConfigureAwait(false);
            }
        }
        else
        {
            var position = await player.PlayAsync(track).ConfigureAwait(false);

            if (position == 0)
                await FollowupAsync($"🔈 Playing: {track.Title} ({track.Uri})")
                    .ConfigureAwait(false);
            else
                await FollowupAsync($"🔈 Added to queue: {track.Title} ({track.Uri})")
                    .ConfigureAwait(false);
        }
    }

    private async Task HandleMultiItemQuery(
        QueuedLavalinkPlayer player,
        string query,
        TrackSearchMode bestGuessSearchMode
    )
    {
        var searchResult = await audioService
            .Tracks.LoadTracksAsync(
                query,
                loadOptions: new TrackLoadOptions(
                    SearchMode: bestGuessSearchMode,
                    SearchBehavior: StrictSearchBehavior.Passthrough
                )
            )
            .ConfigureAwait(false);

        if (!searchResult.Tracks.Any() || searchResult.Playlist == null)
        {
            await FollowupAsync("😖 No results.").ConfigureAwait(false);
            return;
        }

        // Queue the tracks
        foreach (var track in searchResult.Tracks)
        {
            await player.PlayAsync(track).ConfigureAwait(false);
        }

        // Display the url for the playlist we got back, fallback to name
        string? playlistUri;

        try
        {
            playlistUri = searchResult
                .Playlist.AdditionalInformation.FirstOrDefault(x => x.Key == "url")
                .Value.GetString();
        }
        catch (InvalidOperationException)
        {
            playlistUri = "unknown";
        }

        await FollowupAsync($"🔈 Added to queue: {searchResult.Playlist.Name} ({playlistUri})")
            .ConfigureAwait(false);
    }
}
