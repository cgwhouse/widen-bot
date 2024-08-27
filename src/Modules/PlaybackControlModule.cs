using System.Threading.Tasks;
using Discord.Interactions;
using Lavalink4NET.Players.Preconditions;
using Lavalink4NET.Players.Queued;

namespace WidenBot;

[RequireContext(ContextType.Guild)]
public sealed class PlaybackControlModule(IPlayerService playerService)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("pause", description: "Pauses the player", runMode: RunMode.Async)]
    public async Task PauseAsync()
    {
        var (player, errorEmbed) = await playerService
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
        var (player, errorEmbed) = await playerService
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

    [SlashCommand("skip", description: "Skips the current track", runMode: RunMode.Async)]
    public async Task SkipAsync()
    {
        var (player, errorEmbed) = await playerService
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
        var newCurrentItem = queueCopy.Peek()?.Track;

        await player.SkipAsync().ConfigureAwait(false);

        var responseMessage = "Skipped.";

        if (newCurrentItem == null)
            responseMessage += " Stopped playing because the queue is now empty.";
        else if (!player.Shuffle)
            responseMessage += $" Now playing: {newCurrentItem.Title} ({newCurrentItem.Uri})";

        await RespondAsync(responseMessage).ConfigureAwait(false);
    }

    [SlashCommand("shuffle", description: "Toggles shuffle mode", runMode: RunMode.Async)]
    public async Task ShuffleAsync()
    {
        var (player, errorEmbed) = await playerService
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
        var (player, errorEmbed) = await playerService
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
        "stop",
        description: "Stops the current track and clears the queue",
        runMode: RunMode.Async
    )]
    public async Task StopAsync()
    {
        var (player, errorEmbed) = await playerService
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
}
