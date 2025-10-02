using System;
using System.Collections.Immutable;
using System.Configuration.Assemblies;
using System.Linq;
using System.Threading.Tasks;
using Discord.Interactions;
using Lavalink4NET;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;

namespace WidenBot.Modules;

[RequireContext(ContextType.Guild)]
public sealed class PlayModule(IPlayerService playerService, IAudioService audioService)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("play", description: "Plays music", runMode: RunMode.Async)]
    public async Task PlayAsync(string originalQuery)
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

        // Query may contain multiple items, handle individually if so
        var queryList = originalQuery.Split(';');

        foreach (var queryItem in queryList)
        {
            // Get rid of any extra whitespace around the semicolons
            var query = queryItem.TrimStart().TrimEnd();

            // Determine search mode we'll initially start with
            var bestGuessSearchMode = PlayerService.DetermineSearchMode(query);

            var multiItemCheck = PlayerService.IsMultiItem(query, bestGuessSearchMode);

            if (multiItemCheck)
                await HandleMultiItemQuery(player, query, bestGuessSearchMode)
                    .ConfigureAwait(false);
            else
                await HandleTrackQuery(player, query, bestGuessSearchMode, playNext: false)
                    .ConfigureAwait(false);
        }
    }

    [SlashCommand(
        "playnext",
        description: "Plays music, skipping the queue (if any)",
        runMode: RunMode.Async
    )]
    public async Task PlayNextAsync(string originalQuery)
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

        // Query may contain multiple items, handle individually if so
        var queryList = originalQuery.Split(';');

        // If nothing currently playing, just need to go in order and queue each thing normally
        if (player.CurrentItem == null)
        {
            foreach (var queryItem in queryList)
            {
                var query = queryItem.TrimStart().TrimEnd();

                // Determine search mode we'll initially start with
                var bestGuessSearchMode = PlayerService.DetermineSearchMode(query);

                var multiItemCheck = PlayerService.IsMultiItem(query, bestGuessSearchMode);

                if (multiItemCheck)
                {
                    await FollowupAsync(
                            "Sorry, /playnext cannot be used with album or playlist queries."
                        )
                        .ConfigureAwait(false);

                    continue;
                }

                await HandleTrackQuery(player, query, bestGuessSearchMode, playNext: false)
                    .ConfigureAwait(false);
            }
        }
        else
        {
            // Need to insert each thing at the front, in reverse order
            for (int i = queryList.Length - 1; i >= 0; i--)
            {
                var query = queryList[i].TrimStart().TrimEnd();

                // Determine search mode we'll initially start with
                var bestGuessSearchMode = PlayerService.DetermineSearchMode(query);

                var multiItemCheck = PlayerService.IsMultiItem(query, bestGuessSearchMode);

                if (multiItemCheck)
                {
                    await FollowupAsync(
                            "Sorry, /playnext cannot be used with album or playlist queries."
                        )
                        .ConfigureAwait(false);

                    continue;
                }

                await HandleTrackQuery(player, query, bestGuessSearchMode, playNext: true)
                    .ConfigureAwait(false);
            }
        }
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
            "no0Q92S-k0g",
            "kZ1A5w8szqo",
            "-ANbCnTQOvU",
            "J2TTwV_ItIc",
            "jBdCSR6WtbQ",
            "vA_XqkqY-e4",
            "MoZwMwFIhN0",
            "_uoo-h6cnoQ",
            "fRwUN3E_tyI",
            "ziDG8bBqp38",
            "6jbGEJH3NQU",
            "lOEpdIvbmds",
            "2-MHXtjR7Hk",
            "7P78_Lr8EUQ",
            "a-DhqDgHZlk"
        );

        foreach (var videoID in holesSoundtrack)
        {
            var track = await audioService
                .Tracks.LoadTrackAsync(
                    $"https://www.youtube.com/watch?v={videoID}",
                    TrackSearchMode.YouTube
                )
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
            && query.Contains('&')
        )
            query = query[..query.IndexOf('&')];

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
            await player.PlayAsync(track).ConfigureAwait(false);

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
