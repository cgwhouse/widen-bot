using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Interactions;

namespace WidenBot.Modules;

[RequireContext(ContextType.Guild)]
public sealed class GeneralModule(IPlayerService playerService)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand(
        "disconnect",
        description: "Disconnects the bot from the voice channel",
        runMode: RunMode.Async
    )]
    public async Task DisconnectAsync()
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

        await player.DisconnectAsync().ConfigureAwait(false);

        await RespondAsync("Disconnected from voice channel.").ConfigureAwait(false);
    }

    [SlashCommand(
        "show",
        description: "Prints the current queue and other player info",
        runMode: RunMode.Async
    )]
    public async Task ShowAsync()
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

        // Build response starting with queue info
        var result = $"Queue:\n";

        // Also go ahead and build the final portion of response
        var finalPortion = $"\nShuffle: {player.Shuffle}\nRepeat: {player.RepeatMode}\n";
        if (player.CurrentItem?.Track != null)
            finalPortion +=
                $"Now playing: {player.CurrentItem.Track.Title} ({player.CurrentItem.Track.Uri})\n";

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
}
