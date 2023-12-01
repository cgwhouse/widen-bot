using System.Threading.Tasks;
using Discord.Interactions;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;

namespace WidenBot
{
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

        private async ValueTask<QueuedLavalinkPlayer?> GetPlayerAsync(
            bool connectToVoiceChannel = true
        )
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
                return result.Player;

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
}
