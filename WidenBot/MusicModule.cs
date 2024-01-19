using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Lavalink4NET;
using Lavalink4NET.Clients;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Integrations.Lavasearch;
using Lavalink4NET.Integrations.Lavasearch.Extensions;
using Lavalink4NET.Integrations.SponsorBlock;
using Lavalink4NET.Integrations.SponsorBlock.Extensions;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Preconditions;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;

namespace WidenBot;

[RequireContext(ContextType.Guild)]
public sealed class MusicModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IAudioService _audioService;

    public MusicModule(IAudioService audioService)
    {
        _audioService = audioService;
    }

    [SlashCommand("creed", description: "Hold me down", runMode: RunMode.Async)]
    public async Task PlayAsync()
    {
        await DeferAsync().ConfigureAwait(false);

        var player = await TryGetPlayerAsync(allowConnect: true, isDeferred: true)
            .ConfigureAwait(false);

        if (player == null)
            return;

        // Stop the player (stops current music and clears queue)
        await player.StopAsync().ConfigureAwait(false);

        // Repeat song after it finishes
        player.RepeatMode = TrackRepeatMode.Track;

        var track = await _audioService
            .Tracks.LoadTrackAsync("One Last Breath Creed", TrackSearchMode.YouTube)
            .ConfigureAwait(false);

        if (track == null)
        {
            await FollowupAsync("😖 No results.").ConfigureAwait(false);

            return;
        }

        await player.PlayAsync(track).ConfigureAwait(false);

        await FollowupAsync("Good choice.").ConfigureAwait(false);
    }

    [SlashCommand("play", description: "Plays music", runMode: RunMode.Async)]
    public async Task PlayAsync(string query)
    {
        await DeferAsync().ConfigureAwait(false);

        var player = await TryGetPlayerAsync(allowConnect: true, isDeferred: true)
            .ConfigureAwait(false);

        if (player == null)
            return;

        // Determine search mode we'll initially start with
        var bestGuessSearchMode = DetermineSearchMode(query);

        var multiItemCheck = IsMultiItem(query, bestGuessSearchMode);

        // Couldn't determine that we needed to queue multiple things, do standard approach
        if (!multiItemCheck)
        {
            await HandleTrackQuery(player, query, bestGuessSearchMode).ConfigureAwait(false);
            return;
        }

        var success = await HandleMultiItemQuery(player, query, bestGuessSearchMode);

        if (!success)
            await HandleTrackQuery(player, query, bestGuessSearchMode).ConfigureAwait(false);
    }

    private async Task HandleTrackQuery(
        QueuedLavalinkPlayer player,
        string query,
        TrackSearchMode bestGuessSearchMode
    )
    {
        var track =
            await _audioService
                .Tracks.LoadTrackAsync(query, bestGuessSearchMode)
                .ConfigureAwait(false)
            // If we didn't get anything, fall back to YouTube search
            ?? await _audioService
                .Tracks.LoadTrackAsync(query, TrackSearchMode.YouTube)
                .ConfigureAwait(false);

        if (track == null)
        {
            await FollowupAsync("😖 No results.").ConfigureAwait(false);

            return;
        }

        var position = await player.PlayAsync(track).ConfigureAwait(false);

        if (position == 0)
            await FollowupAsync($"🔈 Playing: {track.Uri}").ConfigureAwait(false);
        else
            await FollowupAsync($"🔈 Added to queue: {track.Uri}").ConfigureAwait(false);
    }

    private async Task<bool> HandleMultiItemQuery(
        QueuedLavalinkPlayer player,
        string query,
        TrackSearchMode bestGuessSearchMode
    )
    {
        var searchResult = await _audioService
            .Tracks.SearchAsync(
                query,
                loadOptions: new TrackLoadOptions(SearchMode: bestGuessSearchMode),
                categories: ImmutableArray.Create(SearchCategory.Playlist) // also Album
            )
            .ConfigureAwait(false);

        if (searchResult == null)
            return false;

        foreach (var track in searchResult.Tracks)
            await player.PlayAsync(track).ConfigureAwait(false);

        return true;
    }

    [SlashCommand("skip", description: "Skips the current track", runMode: RunMode.Async)]
    public async Task SkipAsync()
    {
        var player = await TryGetPlayerAsync(allowConnect: false).ConfigureAwait(false);

        if (player == null)
            return;

        if (player.CurrentItem == null)
        {
            await RespondAsync("Nothing to skip.").ConfigureAwait(false);
            return;
        }

        await player.SkipAsync().ConfigureAwait(false);

        var track = player.CurrentItem;

        if (track != null)
            await RespondAsync($"Skipped. Now playing: {track.Track!.Uri}").ConfigureAwait(false);
        else
            await RespondAsync("Skipped. Stopped playing because the queue is now empty.")
                .ConfigureAwait(false);
    }

    [SlashCommand("pause", description: "Pauses the player", runMode: RunMode.Async)]
    public async Task PauseAsync()
    {
        var player = await TryGetPlayerAsync(
                allowConnect: false,
                preconditions: ImmutableArray.Create(
                    PlayerPrecondition.NotPaused,
                    PlayerPrecondition.Playing
                )
            )
            .ConfigureAwait(false);

        if (player == null)
            return;

        await player.PauseAsync().ConfigureAwait(false);

        await RespondAsync("Paused.").ConfigureAwait(false);
    }

    [SlashCommand("resume", description: "Resumes the player", runMode: RunMode.Async)]
    public async Task ResumeAsync()
    {
        var player = await TryGetPlayerAsync(
                allowConnect: false,
                preconditions: ImmutableArray.Create(PlayerPrecondition.Paused)
            )
            .ConfigureAwait(false);

        if (player == null)
            return;

        await player.ResumeAsync().ConfigureAwait(false);

        await RespondAsync("Resumed.").ConfigureAwait(false);
    }

    [SlashCommand(
        "stop",
        description: "Stops the current track and clears the queue",
        runMode: RunMode.Async
    )]
    public async Task StopAsync()
    {
        var player = await TryGetPlayerAsync(allowConnect: false).ConfigureAwait(false);

        if (player == null)
            return;

        if (player.CurrentItem == null)
        {
            await RespondAsync("Nothing to stop.").ConfigureAwait(false);
            return;
        }

        await player.StopAsync().ConfigureAwait(false);

        await RespondAsync("Stopped playing.").ConfigureAwait(false);
    }

    [SlashCommand("shuffle", description: "Toggles shuffle mode", runMode: RunMode.Async)]
    public async Task ShuffleAsync()
    {
        var player = await TryGetPlayerAsync(
                allowConnect: false,
                preconditions: ImmutableArray.Create(PlayerPrecondition.QueueNotEmpty)
            )
            .ConfigureAwait(false);

        if (player == null)
            return;

        player.Shuffle = !player.Shuffle;

        await RespondAsync($"Shuffle has been {(player.Shuffle ? "enabled" : "disabled")}.")
            .ConfigureAwait(false);
    }

    [SlashCommand("repeat", description: "Sets repeat mode of the player", runMode: RunMode.Async)]
    public async Task RepeatAsync(TrackRepeatMode repeatMode)
    {
        var player = await TryGetPlayerAsync(allowConnect: false).ConfigureAwait(false);

        if (player == null)
            return;

        player.RepeatMode = repeatMode;

        await RespondAsync($"Player repeat mode has been updated to {repeatMode}.")
            .ConfigureAwait(false);
    }

    [SlashCommand(
        "show",
        description: "Prints the current queue and other player info",
        runMode: RunMode.Async
    )]
    public async Task ShowAsync()
    {
        var player = await TryGetPlayerAsync(allowConnect: false).ConfigureAwait(false);

        if (player == null)
            return;

        var result = string.Empty;

        result += $"Shuffle: {player.Shuffle}\n";

        result += $"Repeat: {player.RepeatMode}\n\n";

        result += $"Queue:\n";

        var queueEmpty = true;

        if (player.CurrentItem != null)
        {
            queueEmpty = false;

            result += $"{player.CurrentItem.Track?.Title ?? "Unknown title"}\n";
        }

        if (player.Queue.Any())
        {
            queueEmpty = false;

            foreach (var track in player.Queue)
                result += $"{track.Track?.Title ?? "Unknown title"}\n";
        }

        if (queueEmpty)
            result += "Queue is empty.";

        await RespondAsync(result).ConfigureAwait(false);
    }

    private async Task<QueuedLavalinkPlayer?> TryGetPlayerAsync(
        bool allowConnect = false,
        bool requireChannel = true,
        ImmutableArray<IPlayerPrecondition> preconditions = default,
        bool isDeferred = false,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = new PlayerRetrieveOptions(
            ChannelBehavior: allowConnect ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None,
            VoiceStateBehavior: requireChannel
                ? MemberVoiceStateBehavior.RequireSame
                : MemberVoiceStateBehavior.Ignore,
            Preconditions: preconditions
        );

        var result = await _audioService
            .Players.RetrieveAsync(
                Context,
                playerFactory: PlayerFactory.Queued,
                options,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            var player = result.Player;

            // Ensure reasonable volume
            if (player.Volume != 0.25f)
                await player
                    .SetVolumeAsync(0.25f, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

            // Ensure SponsorBlock
            try
            {
                var categories = await player
                    .GetSponsorBlockCategoriesAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (!categories.SequenceEqual(sponsorBlockCategories))
                    await player
                        .UpdateSponsorBlockCategoriesAsync(
                            sponsorBlockCategories,
                            cancellationToken: cancellationToken
                        )
                        .ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
                // Endpoint returns 404 when no SponsorBlock categories are set yet
                await player
                    .UpdateSponsorBlockCategoriesAsync(
                        sponsorBlockCategories,
                        cancellationToken: cancellationToken
                    )
                    .ConfigureAwait(false);
            }

            return player;
        }

        // See the error handling section for more information
        var errorMessage = CreateErrorEmbed(result);

        if (isDeferred)
            await FollowupAsync(embed: errorMessage).ConfigureAwait(false);
        else
            await RespondAsync(embed: errorMessage).ConfigureAwait(false);

        return null;
    }

    private static Embed CreateErrorEmbed(PlayerResult<QueuedLavalinkPlayer> result)
    {
        var title = result.Status switch
        {
            PlayerRetrieveStatus.UserNotInVoiceChannel => "You must be in a voice channel.",
            PlayerRetrieveStatus.BotNotConnected => "The bot is not connected to any channel.",
            PlayerRetrieveStatus.VoiceChannelMismatch
                => "You must be in the same voice channel as the bot.",

            PlayerRetrieveStatus.PreconditionFailed
                when result.Precondition == PlayerPrecondition.Playing
                => "Failed, player must be playing something.",
            PlayerRetrieveStatus.PreconditionFailed
                when result.Precondition == PlayerPrecondition.NotPaused
                => "Failed, player must not be paused.",
            PlayerRetrieveStatus.PreconditionFailed
                when result.Precondition == PlayerPrecondition.Paused
                => "Failed, player must be paused.",
            PlayerRetrieveStatus.PreconditionFailed
                when result.Precondition == PlayerPrecondition.QueueNotEmpty
                => "Failed, queue must not be empty",
            _ => "Unknown error.",
        };

        return new EmbedBuilder().WithTitle(title).Build();
    }

    private static TrackSearchMode DetermineSearchMode(string query)
    {
        if (query.ToLower().Contains("spotify"))
            return TrackSearchMode.Spotify;

        if (query.ToLower().Contains("soundcloud"))
            return TrackSearchMode.SoundCloud;

        if (query.ToLower().Contains("music.youtube"))
            return TrackSearchMode.YouTubeMusic;

        return TrackSearchMode.YouTube;
    }

    private static readonly ImmutableArray<SegmentCategory> sponsorBlockCategories =
        ImmutableArray.Create(
            SegmentCategory.Sponsor,
            SegmentCategory.SelfPromotion,
            SegmentCategory.Interaction,
            SegmentCategory.Intro,
            SegmentCategory.Outro,
            SegmentCategory.Preview,
            SegmentCategory.OfftopicMusic,
            SegmentCategory.Filler
        );

    private static bool IsMultiItem(string query, TrackSearchMode bestGuessSearchMode)
    {
        // Reject if not a direct link. We should only queue multiple things
        // at once if we know they meant to do it
        if (!query.Contains("https"))
            return false;

        if (
            // Spotify playlist and albums
            (
                bestGuessSearchMode == TrackSearchMode.Spotify
                && (query.Contains("playlist") || query.Contains("album"))
            )
            // Youtube playlists
            || (bestGuessSearchMode == TrackSearchMode.YouTube && query.Contains("list="))
            // SoundCloud playlists and albums
            || (bestGuessSearchMode == TrackSearchMode.SoundCloud && query.Contains("/sets/"))
        )
            return true;

        return false;
    }
}
