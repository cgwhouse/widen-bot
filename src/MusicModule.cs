using System;
//using System.Collections.Immutable;
using System.Linq;
//using System.Net.Http;
//using System.Threading;
using System.Threading.Tasks;
//using Discord;
using Discord.Interactions;
using Lavalink4NET;
//using Lavalink4NET.Clients;
//using Lavalink4NET.DiscordNet;
//using Lavalink4NET.Integrations.SponsorBlock;
//using Lavalink4NET.Integrations.SponsorBlock.Extensions;
//using Lavalink4NET.Players;
using Lavalink4NET.Players.Preconditions;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;

//using Microsoft.Extensions.Configuration;

namespace WidenBot;

[RequireContext(ContextType.Guild)]
public sealed class MusicModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IPlayerService _playerService;
    private readonly IAudioService _audioService;

    //private readonly IConfiguration _config;

    //private bool UseSponsorBlock => _config.GetValue<bool>("USE_SPONSORBLOCK");

    public MusicModule(
        IPlayerService playerService,
        IAudioService audioService //,
    //IConfiguration config
    )
    {
        _playerService = playerService;
        _audioService = audioService;
        //_config = config;
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
        var bestGuessSearchMode = DetermineSearchMode(query);

        var multiItemCheck = IsMultiItem(query, bestGuessSearchMode);

        if (multiItemCheck)
        {
            await HandleMultiItemQuery(player, query, bestGuessSearchMode).ConfigureAwait(false);
            return;
        }

        await HandleTrackQuery(player, query, bestGuessSearchMode).ConfigureAwait(false);
    }

    [SlashCommand("skip", description: "Skips the current track", runMode: RunMode.Async)]
    public async Task SkipAsync()
    {
        var (player, errorEmbed) = await _playerService
            .TryGetPlayerAsync(Context, allowConnect: false)
            .ConfigureAwait(false);

        if (player == null)
        {
            if (errorEmbed != null)
                await RespondAsync(embed: errorEmbed).ConfigureAwait(false);

            return;
        }

        if (player.CurrentItem == null)
        {
            await RespondAsync("Nothing to skip.").ConfigureAwait(false);
            return;
        }

        // Peek at the next thing so we can potentially write a message about it
        var queueCopy = player.Queue;
        var newCurrentItemUri = queueCopy.Peek()?.Track?.Uri;

        await player.SkipAsync().ConfigureAwait(false);

        var responseMessage = "Skipped.";

        if (newCurrentItemUri == null)
            responseMessage += " Stopped playing because the queue is now empty.";
        else if (!player.Shuffle)
            responseMessage += $" Now playing: {newCurrentItemUri}";

        await RespondAsync(responseMessage).ConfigureAwait(false);
    }

    [SlashCommand("pause", description: "Pauses the player", runMode: RunMode.Async)]
    public async Task PauseAsync()
    {
        var (player, errorEmbed) = await _playerService
            .TryGetPlayerAsync(
                Context,
                allowConnect: false,
                preconditions: [PlayerPrecondition.NotPaused, PlayerPrecondition.Playing]
            )
            .ConfigureAwait(false);

        if (player == null)
        {
            if (errorEmbed != null)
                await RespondAsync(embed: errorEmbed).ConfigureAwait(false);

            return;
        }

        await player.PauseAsync().ConfigureAwait(false);

        await RespondAsync("Paused.").ConfigureAwait(false);
    }

    [SlashCommand("resume", description: "Resumes the player", runMode: RunMode.Async)]
    public async Task ResumeAsync()
    {
        var (player, errorEmbed) = await _playerService
            .TryGetPlayerAsync(
                Context,
                allowConnect: false,
                preconditions: [PlayerPrecondition.Paused]
            )
            .ConfigureAwait(false);

        if (player == null)
        {
            if (errorEmbed != null)
                await RespondAsync(embed: errorEmbed).ConfigureAwait(false);

            return;
        }

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
        var (player, errorEmbed) = await _playerService
            .TryGetPlayerAsync(Context, allowConnect: false)
            .ConfigureAwait(false);

        if (player == null)
        {
            if (errorEmbed != null)
                await RespondAsync(embed: errorEmbed).ConfigureAwait(false);

            return;
        }

        if (player.CurrentItem == null)
        {
            await RespondAsync("Nothing to stop.").ConfigureAwait(false);
            return;
        }

        await player.StopAsync().ConfigureAwait(false);

        await RespondAsync("Stopped playing.").ConfigureAwait(false);
    }

    [SlashCommand(
        "disconnect",
        description: "Disconnects the bot from the voice channel",
        runMode: RunMode.Async
    )]
    public async Task DisconnectAsync()
    {
        var (player, errorEmbed) = await _playerService
            .TryGetPlayerAsync(Context, allowConnect: false)
            .ConfigureAwait(false);

        if (player == null)
        {
            if (errorEmbed != null)
                await RespondAsync(embed: errorEmbed).ConfigureAwait(false);

            return;
        }

        await player.DisconnectAsync().ConfigureAwait(false);

        await RespondAsync("Disconnected from voice channel.").ConfigureAwait(false);
    }

    [SlashCommand("shuffle", description: "Toggles shuffle mode", runMode: RunMode.Async)]
    public async Task ShuffleAsync()
    {
        var (player, errorEmbed) = await _playerService
            .TryGetPlayerAsync(
                Context,
                allowConnect: false,
                preconditions: [PlayerPrecondition.QueueNotEmpty]
            )
            .ConfigureAwait(false);

        if (player == null)
        {
            if (errorEmbed != null)
                await RespondAsync(embed: errorEmbed).ConfigureAwait(false);

            return;
        }

        player.Shuffle = !player.Shuffle;

        await RespondAsync($"Shuffle has been {(player.Shuffle ? "enabled" : "disabled")}.")
            .ConfigureAwait(false);
    }

    [SlashCommand("repeat", description: "Sets repeat mode of the player", runMode: RunMode.Async)]
    public async Task RepeatAsync(TrackRepeatMode repeatMode)
    {
        var (player, errorEmbed) = await _playerService
            .TryGetPlayerAsync(Context, allowConnect: false)
            .ConfigureAwait(false);

        if (player == null)
        {
            if (errorEmbed != null)
                await RespondAsync(embed: errorEmbed).ConfigureAwait(false);

            return;
        }

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
        var (player, errorEmbed) = await _playerService
            .TryGetPlayerAsync(Context, allowConnect: false)
            .ConfigureAwait(false);

        if (player == null)
        {
            if (errorEmbed != null)
                await RespondAsync(embed: errorEmbed).ConfigureAwait(false);

            return;
        }

        // Build response starting with queue info
        var result = $"Queue:\n";

        // Also go ahead and build the final portion of response
        var finalPortion = $"\nShuffle: {player.Shuffle}\nRepeat: {player.RepeatMode}\n";
        if (player.CurrentItem?.Track != null)
            finalPortion += $"Now playing: {player.CurrentItem.Track.Uri}\n";

        if (player.Queue.Any())
        {
            // See if we can show the whole queue, 2000 char limit
            var queueContents =
                "\n"
                + string.Join("\n", player.Queue.Select(x => x.Track?.Title ?? "Unknown title"))
                + "\n";

            // Manufacture shorter message if needed
            if ((result + queueContents + finalPortion).Length > 2000)
                queueContents =
                    $"\nLots ({player.Queue.Count()}). Discord won't let us show this many tracks at once, the message would be too big.\n";

            result += queueContents;
        }
        else
            result += "Queue is empty.";

        await RespondAsync(result + finalPortion).ConfigureAwait(false);
    }

    //private async Task<QueuedLavalinkPlayer?> TryGetPlayerAsync(
    //    bool allowConnect = false,
    //    bool requireChannel = true,
    //    ImmutableArray<IPlayerPrecondition> preconditions = default,
    //    bool isDeferred = false,
    //    CancellationToken cancellationToken = default
    //)
    //{
    //    cancellationToken.ThrowIfCancellationRequested();

    //    var options = new PlayerRetrieveOptions(
    //        ChannelBehavior: allowConnect ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None,
    //        VoiceStateBehavior: requireChannel
    //            ? MemberVoiceStateBehavior.RequireSame
    //            : MemberVoiceStateBehavior.Ignore,
    //        Preconditions: preconditions
    //    );

    //    var result = await _audioService
    //        .Players.RetrieveAsync(
    //            Context,
    //            playerFactory: PlayerFactory.Queued,
    //            options,
    //            cancellationToken: cancellationToken
    //        )
    //        .ConfigureAwait(false);

    //    if (!result.IsSuccess)
    //    {
    //        // See the error handling section for more information
    //        var errorMessage = CreateErrorEmbed(result);

    //        if (isDeferred)
    //            await FollowupAsync(embed: errorMessage).ConfigureAwait(false);
    //        else
    //            await RespondAsync(embed: errorMessage).ConfigureAwait(false);

    //        return null;
    //    }

    //    var player = result.Player;

    //    // Ensure reasonable volume
    //    if (player.Volume != 0.25f)
    //        await player
    //            .SetVolumeAsync(0.25f, cancellationToken: cancellationToken)
    //            .ConfigureAwait(false);

    //    // Ensure SponsorBlock if enabled
    //    if (UseSponsorBlock)
    //    {
    //        try
    //        {
    //            var categories = await player
    //                .GetSponsorBlockCategoriesAsync(cancellationToken: cancellationToken)
    //                .ConfigureAwait(false);

    //            if (!categories.SequenceEqual(sponsorBlockCategories))
    //                await player
    //                    .UpdateSponsorBlockCategoriesAsync(
    //                        sponsorBlockCategories,
    //                        cancellationToken: cancellationToken
    //                    )
    //                    .ConfigureAwait(false);
    //        }
    //        catch (HttpRequestException)
    //        {
    //            // Endpoint returns 404 when no SponsorBlock categories are set yet
    //            await player
    //                .UpdateSponsorBlockCategoriesAsync(
    //                    sponsorBlockCategories,
    //                    cancellationToken: cancellationToken
    //                )
    //                .ConfigureAwait(false);
    //        }
    //    }

    //    return player;
    //}

    //private static Embed CreateErrorEmbed(PlayerResult<QueuedLavalinkPlayer> result)
    //{
    //    var title = result.Status switch
    //    {
    //        PlayerRetrieveStatus.UserNotInVoiceChannel => "You must be in a voice channel.",
    //        PlayerRetrieveStatus.BotNotConnected => "The bot is not connected to any channel.",
    //        PlayerRetrieveStatus.VoiceChannelMismatch
    //            => "You must be in the same voice channel as the bot.",

    //        PlayerRetrieveStatus.PreconditionFailed
    //            when result.Precondition == PlayerPrecondition.Playing
    //            => "Failed, player must be playing something.",
    //        PlayerRetrieveStatus.PreconditionFailed
    //            when result.Precondition == PlayerPrecondition.NotPaused
    //            => "Failed, player must not be paused.",
    //        PlayerRetrieveStatus.PreconditionFailed
    //            when result.Precondition == PlayerPrecondition.Paused
    //            => "Failed, player must be paused.",
    //        PlayerRetrieveStatus.PreconditionFailed
    //            when result.Precondition == PlayerPrecondition.QueueNotEmpty
    //            => "Failed, queue must not be empty",
    //        _ => "Unknown error.",
    //    };

    //    return new EmbedBuilder().WithTitle(title).Build();
    //}

    private static TrackSearchMode DetermineSearchMode(string query)
    {
        if (query.Contains("spotify", StringComparison.CurrentCultureIgnoreCase))
            return TrackSearchMode.Spotify;

        if (query.Contains("soundcloud", StringComparison.CurrentCultureIgnoreCase))
            return TrackSearchMode.SoundCloud;

        if (query.Contains("music.youtube", StringComparison.CurrentCultureIgnoreCase))
            return TrackSearchMode.YouTubeMusic;

        return TrackSearchMode.YouTube;
    }

    //private static readonly ImmutableArray<SegmentCategory> sponsorBlockCategories =
    //[
    //    SegmentCategory.Sponsor,
    //    SegmentCategory.SelfPromotion,
    //    SegmentCategory.Interaction,
    //    SegmentCategory.Intro,
    //    SegmentCategory.Outro,
    //    SegmentCategory.Preview,
    //    SegmentCategory.OfftopicMusic,
    //    SegmentCategory.Filler,
    //];

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
            // Youtube playlists, need to ensure that it's a link to the playlist itself, and not just a single item from within a playlist
            || (
                bestGuessSearchMode == TrackSearchMode.YouTube
                && query.Contains("list=")
                && !query.Contains("index=")
            )
            // SoundCloud playlists and albums
            || (bestGuessSearchMode == TrackSearchMode.SoundCloud && query.Contains("/sets/"))
        )
            return true;

        return false;
    }

    private async Task HandleTrackQuery(
        QueuedLavalinkPlayer player,
        string query,
        TrackSearchMode bestGuessSearchMode
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

        var position = await player.PlayAsync(track).ConfigureAwait(false);

        if (position == 0)
            await FollowupAsync($"🔈 Playing: {track.Uri}").ConfigureAwait(false);
        else
            await FollowupAsync($"🔈 Added to queue: {track.Uri}").ConfigureAwait(false);
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
            await player.PlayAsync(track).ConfigureAwait(false);

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
