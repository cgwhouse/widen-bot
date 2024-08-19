using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Interactions;
using Lavalink4NET;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;

namespace WidenBot;

[RequireContext(ContextType.Guild)]
public sealed class PlayModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IPlayerService _playerService;
    private readonly IAudioService _audioService;

    public PlayModule(IPlayerService playerService, IAudioService audioService)
    {
        _playerService = playerService;
        _audioService = audioService;
    }

    [SlashCommand("play", description: "Plays music", runMode: RunMode.Async)]
    public async Task PlayAsync(string query)
    {
        await DeferAsync().ConfigureAwait(false);

        var (player, errorEmbed) = await _playerService
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

        var (player, errorEmbed) = await _playerService
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

        var (player, errorEmbed) = await _playerService
            .TryGetPlayerAsync(Context, allowConnect: true)
            .ConfigureAwait(false);

        if (player == null)
        {
            if (errorEmbed != null)
                await FollowupAsync(embed: errorEmbed).ConfigureAwait(false);

            return;
        }

        var track = await _audioService
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

    [SlashCommand("creed", description: "Hold me down", runMode: RunMode.Async)]
    public async Task CreedAsync()
    {
        await DeferAsync().ConfigureAwait(false);

        var (player, errorEmbed) = await _playerService
            .TryGetPlayerAsync(Context, allowConnect: true)
            .ConfigureAwait(false);

        if (player == null)
        {
            if (errorEmbed != null)
                await FollowupAsync(embed: errorEmbed).ConfigureAwait(false);

            return;
        }

        var track = await _audioService
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
        var track = await _audioService
            .Tracks.LoadTrackAsync(query, bestGuessSearchMode)
            .ConfigureAwait(false);

        // If we didn't get anything, fall back to YouTube search
        if (track == null && bestGuessSearchMode != TrackSearchMode.YouTube)
            track = await _audioService
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

                await FollowupAsync($"🔈 Playing: {track.Uri}").ConfigureAwait(false);
            }
            else
            {
                await player.Queue.InsertAsync(0, new TrackQueueItem(track)).ConfigureAwait(false);

                await FollowupAsync($"🔈 Added to front of queue: {track.Uri}")
                    .ConfigureAwait(false);
            }
        }
        else
        {
            var position = await player.PlayAsync(track).ConfigureAwait(false);

            if (position == 0)
                await FollowupAsync($"🔈 Playing: {track.Uri}").ConfigureAwait(false);
            else
                await FollowupAsync($"🔈 Added to queue: {track.Uri}").ConfigureAwait(false);
        }
    }

    private async Task HandleMultiItemQuery(
        QueuedLavalinkPlayer player,
        string query,
        TrackSearchMode bestGuessSearchMode
    )
    {
        var searchResult = await _audioService
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
        string displayText;

        try
        {
            var playlistUri = searchResult
                .Playlist.AdditionalInformation.FirstOrDefault(x => x.Key == "url")
                .Value.GetString();

            displayText = string.IsNullOrEmpty(playlistUri)
                ? searchResult.Playlist.Name
                : playlistUri;
        }
        catch (InvalidOperationException)
        {
            displayText = searchResult.Playlist.Name;
        }

        await FollowupAsync($"🔈 Added to queue: {displayText}").ConfigureAwait(false);
    }
}
