using System.Threading.Tasks;
using Discord.Interactions;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;

namespace WidenBot.Services
{
    public class MyCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IAudioService _audioService;

        public MyCommands(IAudioService audioService)
        {
            _audioService = audioService;
        }

        // XXX does not match example
        [SlashCommand("play", description: "Plays music", runMode: RunMode.Async)]
        public async Task Play(string query)
        {
            // Defer the response to indicate that the bot is working on the command.
            // Resolving tracks from YouTube may take some time, so we want to let the user
            // know that the bot is working on the command.
            await DeferAsync().ConfigureAwait(false);

            // Retrieve the player using the method we created earlier.
            // We allow to connect to the voice channel if the user is not connected.
            var player = await GetPlayerAsync(connectToVoiceChannel: true).ConfigureAwait(false);

            // If the player is null, something failed. We already sent an error message to the user
            if (player is null)
            {
                return;
            }

            // Load the track from YouTube. This may take some time, so we await the result.
            var track = await _audioService
                .Tracks
                .LoadTrackAsync(query, TrackSearchMode.YouTube)
                .ConfigureAwait(false);

            // If no track was found, we send an error message to the user.
            if (track is null)
            {
                await FollowupAsync("😖 No results.").ConfigureAwait(false);
                return;
            }

            // Play the track and inform the user about the track that is being played.
            await player.PlayAsync(track).ConfigureAwait(false);
            await FollowupAsync($"🔈 Playing: {track.Uri}").ConfigureAwait(false);
        }

        // XXX does not match example
        private async ValueTask<QueuedLavalinkPlayer?> GetPlayerAsync(
            bool connectToVoiceChannel = true
        )
        {
            var channelBehavior = connectToVoiceChannel
                ? PlayerChannelBehavior.Join
                : PlayerChannelBehavior.None;

            var retrieveOptions = new PlayerRetrieveOptions(ChannelBehavior: channelBehavior);

            var result = await _audioService
                .Players
                .RetrieveAsync(Context, playerFactory: PlayerFactory.Queued, retrieveOptions)
                .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                var errorMessage = result.Status switch
                {
                    PlayerRetrieveStatus.UserNotInVoiceChannel
                        => "You are not connected to a voice channel.",
                    PlayerRetrieveStatus.BotNotConnected => "The bot is currently not connected.",
                    _ => "Unknown error.",
                };

                await FollowupAsync(errorMessage).ConfigureAwait(false);
                return null;
            }

            return result.Player;
        }
    }
}
