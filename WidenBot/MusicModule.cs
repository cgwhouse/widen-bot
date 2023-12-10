using System.Threading.Tasks;
using Discord.Interactions;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Players;
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
    public async Task Play(string query)
    {
        await DeferAsync().ConfigureAwait(false);

        var player = await GetPlayerAsync(connectToVoiceChannel: true).ConfigureAwait(false);

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

    [SlashCommand("pause", description: "Pauses the player.", runMode: RunMode.Async)]
    public async Task PauseAsync()
    {
        var player = await GetPlayerAsync(connectToVoiceChannel: false);

        if (player == null)
            return;

        if (player.State == PlayerState.Paused)
        {
            await RespondAsync("Player is already paused.").ConfigureAwait(false);

            return;
        }

        await player.PauseAsync().ConfigureAwait(false);

        await RespondAsync("Paused.").ConfigureAwait(false);
    }

    [SlashCommand("resume", description: "Resumes the player.", runMode: RunMode.Async)]
    public async Task ResumeAsync()
    {
        var player = await GetPlayerAsync(connectToVoiceChannel: false);

        if (player == null)
            return;

        if (player.State != PlayerState.Paused)
        {
            await RespondAsync("Player is not paused.").ConfigureAwait(false);

            return;
        }

        await player.ResumeAsync().ConfigureAwait(false);

        await RespondAsync("Resumed.").ConfigureAwait(false);
    }

    [SlashCommand("stop", description: "Stops the current track", runMode: RunMode.Async)]
    public async Task Stop()
    {
        var player = await GetPlayerAsync(connectToVoiceChannel: false);

        if (player == null)
            return;

        if (player.CurrentItem == null)
        {
            await RespondAsync("Nothing playing!").ConfigureAwait(false);

            return;
        }

        await player.StopAsync().ConfigureAwait(false);

        await RespondAsync("Stopped playing.").ConfigureAwait(false);
    }

    private async ValueTask<QueuedLavalinkPlayer?> GetPlayerAsync(bool connectToVoiceChannel = true)
    {
        var result = await _audioService
            .Players
            .RetrieveAsync(
                Context,
                playerFactory: PlayerFactory.Queued,
                new PlayerRetrieveOptions(
                    ChannelBehavior: connectToVoiceChannel
                        ? PlayerChannelBehavior.Join
                        : PlayerChannelBehavior.None
                )
            )
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            var player = result.Player;

            // Ensure volume is a reasonable value before returning the player
            if (player.Volume != 0.5f)
                await player.SetVolumeAsync(0.5f).ConfigureAwait(false);

            return player;
        }

        // Something went wrong
        string errorMessage = result.Status switch
        {
            PlayerRetrieveStatus.UserNotInVoiceChannel
                => "You are not connected to a voice channel.",
            PlayerRetrieveStatus.BotNotConnected => "The bot is currently not connected.",
            _ => "Unknown error.",
        };

        await FollowupAsync(errorMessage).ConfigureAwait(false);

        return null;
    }
}
