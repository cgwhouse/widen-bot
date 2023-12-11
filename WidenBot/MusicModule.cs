using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Lavalink4NET;
using Lavalink4NET.Clients;
using Lavalink4NET.DiscordNet;
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

    [SlashCommand("play", description: "Plays music", runMode: RunMode.Async)]
    public async Task PlayAsync(string query)
    {
        await DeferAsync().ConfigureAwait(false);

        var player = await TryGetPlayerAsync(allowConnect: true).ConfigureAwait(false);

        if (player == null)
            return;

        var track = await _audioService
            .Tracks
            .LoadTrackAsync(query, TrackSearchMode.YouTube)
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

    [SlashCommand("skip", description: "Skips the current track.", runMode: RunMode.Async)]
    public async Task SkipAsync()
    {
        var player = await TryGetPlayerAsync(
                allowConnect: false,
                preconditions: ImmutableArray.Create(PlayerPrecondition.Playing)
            )
            .ConfigureAwait(false);

        if (player == null)
            return;

        await player.SkipAsync().ConfigureAwait(false);

        var track = player.CurrentItem;

        if (track != null)
            await RespondAsync($"Skipped. Now playing: {track.Track!.Uri}").ConfigureAwait(false);
        else
            await RespondAsync("Skipped. Stopped playing because the queue is now empty.")
                .ConfigureAwait(false);
    }

    [SlashCommand("pause", description: "Pauses the player.", runMode: RunMode.Async)]
    public async Task PauseAsync()
    {
        var player = await TryGetPlayerAsync(
                allowConnect: false,
                preconditions: ImmutableArray.Create(PlayerPrecondition.NotPaused)
            )
            .ConfigureAwait(false);

        if (player == null)
            return;

        await player.PauseAsync().ConfigureAwait(false);

        await RespondAsync("Paused.").ConfigureAwait(false);
    }

    [SlashCommand("resume", description: "Resumes the player.", runMode: RunMode.Async)]
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
        description: "Stops the current track and clears the queue.",
        runMode: RunMode.Async
    )]
    public async Task StopAsync()
    {
        var player = await TryGetPlayerAsync(
                allowConnect: false,
                preconditions: ImmutableArray.Create(PlayerPrecondition.Playing)
            )
            .ConfigureAwait(false);

        if (player == null)
            return;

        await player.StopAsync().ConfigureAwait(false);

        await RespondAsync("Stopped playing.").ConfigureAwait(false);
    }

    [SlashCommand("shuffle", description: "Toggles shuffle mode.", runMode: RunMode.Async)]
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

    [SlashCommand("repeat", description: "Sets repeat mode of the player.", runMode: RunMode.Async)]
    public async Task RepeatAsync(TrackRepeatMode repeatMode)
    {
        var player = await TryGetPlayerAsync(
                allowConnect: false,
                preconditions: ImmutableArray.Create(PlayerPrecondition.Playing)
            )
            .ConfigureAwait(false);

        if (player == null)
            return;

        player.RepeatMode = repeatMode;

        await RespondAsync($"Player repeat mode has been updated to {repeatMode}.")
            .ConfigureAwait(false);
    }

    [SlashCommand(
        "show",
        description: "Prints the current queue and other player info.",
        runMode: RunMode.Async
    )]
    public async Task ShowAsync()
    {
        var player = await TryGetPlayerAsync(allowConnect: false).ConfigureAwait(false);

        if (player == null)
            return;

        var result = string.Empty;

        result += $"Shuffle: {player.Shuffle}\n";

        result += $"Repeat: {player.RepeatMode}\n";

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

    private async ValueTask<QueuedLavalinkPlayer?> TryGetPlayerAsync(
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
            .Players
            .RetrieveAsync(
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
                await player.SetVolumeAsync(0.25f, cancellationToken);

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
}
