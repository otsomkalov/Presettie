module Bot.Resources

open Microsoft.FSharp.Core

// String constants mirroring Resources project keys with kebab-case values
// These are [<Literal>] so they can be used in attributes and active patterns

[<RequireQualifiedAccess>]
module Messages =
  [<Literal>]
  let PresetInfo = "messages.preset-info"

  [<Literal>]
  let PresetSettingsInfo = "messages.preset-settings-info"

  [<Literal>]
  let LoginToSpotify = "messages.login-to-spotify"

  [<Literal>]
  let SendPresetSize = "messages.send-preset-size"

  [<Literal>]
  let PresetSizeSet = "messages.preset-size-set"

  [<Literal>]
  let WrongPresetSize = "messages.wrong-preset-size"

  [<Literal>]
  let PresetSizeTooSmall = "messages.preset-size-too-small"

  [<Literal>]
  let PresetSizeTooBig = "messages.preset-size-too-big"

  [<Literal>]
  let PresetSizeNotANumber = "messages.preset-size-not-a-number"

  [<Literal>]
  let LikedTracksIncluded = "messages.liked-tracks-included"

  [<Literal>]
  let LikedTracksExcluded = "messages.liked-tracks-excluded"

  [<Literal>]
  let LikedTracksIgnored = "messages.liked-tracks-ignored"

  [<Literal>]
  let SendIncludedPlaylist = "messages.send-included-playlist"

  [<Literal>]
  let SendExcludedPlaylist = "messages.send-excluded-playlist"

  [<Literal>]
  let SendExcludedArtist = "messages.send-excluded-artist"

  [<Literal>]
  let SendTargetedPlaylist = "messages.send-targeted-playlist"

  [<Literal>]
  let PlaylistIdCannotBeParsed = "messages.playlist-id-cannot-be-parsed"

  [<Literal>]
  let ArtistIdCannotBeParsed = "messages.artist-id-cannot-be-parsed"

  [<Literal>]
  let PlaylistNotFoundInSpotify = "messages.playlist-not-found-in-spotify"

  [<Literal>]
  let ArtistNotFoundInSpotify = "messages.artist-not-found-in-spotify"

  [<Literal>]
  let PlaylistIsReadonly = "messages.playlist-is-readonly"

  [<Literal>]
  let SendPresetName = "messages.send-preset-name"

  [<Literal>]
  let ArtistsAlbumsRecommendation = "messages.artists-albums-recommendation"

  [<Literal>]
  let ReccoBeatsRecommendation = "messages.recco-beats-recommendation"

  [<Literal>]
  let SpotifyRecommendation = "messages.spotify-recommendation"

  [<Literal>]
  let RecommendationsDisabled = "messages.recommendations-disabled"

  [<Literal>]
  let UniqueArtistsEnabled = "messages.unique-artists-enabled"

  [<Literal>]
  let UniqueArtistsDisabled = "messages.unique-artists-disabled"

  [<Literal>]
  let Welcome = "messages.welcome"

  [<Literal>]
  let Help = "messages.help"

  [<Literal>]
  let Privacy = "messages.privacy"

  [<Literal>]
  let FAQ = "messages.faq"

  [<Literal>]
  let Guide = "messages.guide"

  [<Literal>]
  let IncludedPlaylistDetails = "messages.included-playlist-details"

  [<Literal>]
  let ExcludedPlaylistDetails = "messages.excluded-playlist-details"

  [<Literal>]
  let ExcludedArtistDetails = "messages.excluded-artist-details"

  [<Literal>]
  let TargetedPlaylistDetails = "messages.targeted-playlist-details"

  [<Literal>]
  let UnknownCommand = "messages.unknown-command"

  [<Literal>]
  let NoCurrentPreset = "messages.no-current-preset"

  [<Literal>]
  let PresetValidationError = "messages.preset-validation-error"

  [<Literal>]
  let SuccessfulLogin = "messages.successful-login"

  [<Literal>]
  let StateNotFound = "messages.state-not-found"

  [<Literal>]
  let OtherUserState = "messages.other-user-state"

  [<Literal>]
  let PlaylistIncluded = "messages.playlist-included"

  [<Literal>]
  let PlaylistExcluded = "messages.playlist-excluded"

  [<Literal>]
  let ArtistExcluded = "messages.artist-excluded"

  [<Literal>]
  let PlaylistTargeted = "messages.playlist-targeted"

  [<Literal>]
  let PresetQueued = "messages.preset-queued"

  [<Literal>]
  let NoIncludedPlaylists = "messages.no-included-playlists"

  [<Literal>]
  let NoTargetedPlaylists = "messages.no-targeted-playlists"

  [<Literal>]
  let IncludedPlaylists = "messages.included-playlists"

  [<Literal>]
  let ExcludedPlaylists = "messages.excluded-playlists"

  [<Literal>]
  let ExcludedArtists = "messages.excluded-artists"

  [<Literal>]
  let TargetedPlaylists = "messages.targeted-playlists"

  [<Literal>]
  let PresetExecuted = "messages.preset-executed"

  [<Literal>]
  let RunningPreset = "messages.running-preset"

  [<Literal>]
  let NotAuthorized = "messages.not-authorized"

  [<Literal>]
  let NoIncludedTracks = "messages.no-included-tracks"

  [<Literal>]
  let NoPotentialTracks = "messages.no-potential-tracks"

  [<Literal>]
  let YourPresets = "messages.your-presets"

  [<Literal>]
  let IncludedContent = "messages.included-content"

  [<Literal>]
  let ExcludedContent = "messages.excluded-content"

[<RequireQualifiedAccess>]
module Buttons =
  [<Literal>]
  let RunPreset = "buttons.run-preset"

  [<Literal>]
  let Login = "buttons.login"

  [<Literal>]
  let Settings = "buttons.settings"

  [<Literal>]
  let SetPresetSize = "buttons.set-preset-size"

  [<Literal>]
  let IncludeLikedTracks = "buttons.include-liked-tracks"

  [<Literal>]
  let ExcludeLikedTracks = "buttons.exclude-liked-tracks"

  [<Literal>]
  let IgnoreLikedTracks = "buttons.ignore-liked-tracks"

  [<Literal>]
  let IncludePlaylist = "buttons.include-playlist"

  [<Literal>]
  let ExcludePlaylist = "buttons.exclude-playlist"

  [<Literal>]
  let ExcludeArtist = "buttons.exclude-artist"

  [<Literal>]
  let TargetPlaylist = "buttons.target-playlist"

  [<Literal>]
  let MyPresets = "buttons.my-presets"

  [<Literal>]
  let CreatePreset = "buttons.create-preset"

  [<Literal>]
  let ArtistsAlbumsRecommendations = "buttons.artists-albums-recommendations"

  [<Literal>]
  let ReccoBeatsRecommendations = "buttons.recco-beats-recommendations"

  [<Literal>]
  let SpotifyRecommendations = "buttons.spotify-recommendations"

  [<Literal>]
  let DisableRecommendations = "buttons.disable-recommendations"

  [<Literal>]
  let EnableUniqueArtists = "buttons.enable-unique-artists"

  [<Literal>]
  let DisableUniqueArtists = "buttons.disable-unique-artists"

  [<Literal>]
  let Back = "buttons.back"

  [<Literal>]
  let PrevPage = "buttons.prev-page"

  [<Literal>]
  let NextPage = "buttons.next-page"

  [<Literal>]
  let Remove = "buttons.remove"

  [<Literal>]
  let Append = "buttons.append"

  [<Literal>]
  let Overwrite = "buttons.overwrite"

  [<Literal>]
  let IncludedContent = "buttons.included-content"

  [<Literal>]
  let IncludedPlaylists = "buttons.included-playlists"

  [<Literal>]
  let ExcludedContent = "buttons.excluded-content"

  [<Literal>]
  let ExcludedPlaylists = "buttons.excluded-playlists"

  [<Literal>]
  let TargetedPlaylists = "buttons.targeted-playlists"

  [<Literal>]
  let SetCurrentPreset = "buttons.set-current-preset"

  [<Literal>]
  let IncludedPlaylistAll = "buttons.included-playlist-all"

  [<Literal>]
  let IncludedPlaylistLikedOnly = "buttons.included-playlist-liked-only"

  [<Literal>]
  let IncludedContent = "buttons.included-content"

  [<Literal>]
  let ExcludedContent = "buttons.excluded-content"

[<RequireQualifiedAccess>]
module Notifications =
  [<Literal>]
  let UnknownCommand = "notifications.unknown-command"

  [<Literal>]
  let Updated = "notifications.updated"

  [<Literal>]
  let PresetQueued = "notifications.preset-queued"

  [<Literal>]
  let CurrentPresetSet = "notifications.current-preset-set"

  [<Literal>]
  let PresetRemoved = "notifications.preset-removed"

  [<Literal>]
  let PresetNotFound = "notifications.preset-not-found"

  [<Literal>]
  let ExcludedArtistRemoved = "notifications.excluded-artist-removed"