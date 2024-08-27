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
using Lavalink4NET.Integrations.SponsorBlock;
using Lavalink4NET.Integrations.SponsorBlock.Extensions;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Preconditions;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;
using Microsoft.Extensions.Configuration;

namespace WidenBot;

public interface IPlayerService
{
    public Task<(QueuedLavalinkPlayer? player, Embed? errorEmbed)> TryGetPlayerAsync(
        SocketInteractionContext interactionContext,
        bool allowConnect = false,
        bool requireChannel = true,
        ImmutableArray<IPlayerPrecondition> preconditions = default,
        CancellationToken cancellationToken = default
    );
}

public class PlayerService(IAudioService audioService, IConfiguration config) : IPlayerService
{
    private bool UseSponsorBlock => config.GetValue<bool>("USE_SPONSORBLOCK");

    // TODO: just make it a ulong here?
    private string? RequiredChannel => config.GetValue<string?>("REQUIRED_CHANNEL");

    private static readonly ImmutableArray<SegmentCategory> sponsorBlockCategories =
    [
        SegmentCategory.Sponsor,
        SegmentCategory.SelfPromotion,
        SegmentCategory.Interaction,
        SegmentCategory.Intro,
        SegmentCategory.Outro,
        SegmentCategory.Preview,
        SegmentCategory.OfftopicMusic,
        SegmentCategory.Filler,
    ];

    public async Task<(QueuedLavalinkPlayer? player, Embed? errorEmbed)> TryGetPlayerAsync(
        SocketInteractionContext interactionContext,
        bool allowConnect = false,
        bool requireChannel = true,
        ImmutableArray<IPlayerPrecondition> preconditions = default,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Validate channel if RequiredChannel is set
        if (
            ulong.TryParse(RequiredChannel, out var reqChannelLong)
            && interactionContext.Channel.Id != reqChannelLong
        )
        {
            var channelName =
                interactionContext.Guild.Channels.FirstOrDefault(x => x.Id == reqChannelLong)?.Name
                ?? "unknown";

            var errorText = $"This WidenBot wants you to use #{channelName} for commands.";

            return (player: null, errorEmbed: new EmbedBuilder().WithTitle(errorText).Build());
        }

        var options = new PlayerRetrieveOptions(
            ChannelBehavior: allowConnect ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None,
            VoiceStateBehavior: requireChannel
                ? MemberVoiceStateBehavior.RequireSame
                : MemberVoiceStateBehavior.Ignore,
            Preconditions: preconditions
        );

        var result = await audioService
            .Players.RetrieveAsync(
                interactionContext,
                playerFactory: PlayerFactory.Queued,
                options,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        // See the error handling section for more information
        if (!result.IsSuccess)
            return (player: null, errorEmbed: CreateErrorEmbed(result));

        var player = result.Player;

        // Ensure reasonable volume
        if (player.Volume != 0.25f)
            await player
                .SetVolumeAsync(0.25f, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

        // Ensure SponsorBlock if enabled
        if (UseSponsorBlock)
        {
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
        }

        return (player: player, errorEmbed: null);
    }

    public static TrackSearchMode DetermineSearchMode(string query)
    {
        if (query.Contains("spotify", StringComparison.CurrentCultureIgnoreCase))
            return TrackSearchMode.Spotify;

        if (query.Contains("soundcloud", StringComparison.CurrentCultureIgnoreCase))
            return TrackSearchMode.SoundCloud;

        if (query.Contains("music.youtube", StringComparison.CurrentCultureIgnoreCase))
            return TrackSearchMode.YouTubeMusic;

        return TrackSearchMode.YouTube;
    }

    public static bool IsMultiItem(string query, TrackSearchMode bestGuessSearchMode)
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

    private static Embed CreateErrorEmbed(PlayerResult<QueuedLavalinkPlayer> result)
    {
        var title = result.Status switch
        {
            PlayerRetrieveStatus.UserNotInVoiceChannel => "You must be in a voice channel.",
            PlayerRetrieveStatus.BotNotConnected => "The bot is not connected to any channel.",
            PlayerRetrieveStatus.VoiceChannelMismatch =>
                "You must be in the same voice channel as the bot.",

            PlayerRetrieveStatus.PreconditionFailed
                when result.Precondition == PlayerPrecondition.Playing =>
                "Failed, player must be playing something.",
            PlayerRetrieveStatus.PreconditionFailed
                when result.Precondition == PlayerPrecondition.NotPaused =>
                "Failed, player must not be paused.",
            PlayerRetrieveStatus.PreconditionFailed
                when result.Precondition == PlayerPrecondition.Paused =>
                "Failed, player must be paused.",
            PlayerRetrieveStatus.PreconditionFailed
                when result.Precondition == PlayerPrecondition.QueueNotEmpty =>
                "Failed, queue must not be empty",
            _ => "Unknown error.",
        };

        return new EmbedBuilder().WithTitle(title).Build();
    }
}
