module Telegram.Resources

open Microsoft.FSharp.Core

// String constants mirroring Resources project keys with kebab-case values
// These are [<Literal>] so they can be used in attributes and active patterns

[<RequireQualifiedAccess>]
module Messages =
  [<Literal>]
  let PresetInfo = "messages.preset-info"

  [<Literal>]
  let Updated = "messages.updated"

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
  let SendTargetedPlaylist = "messages.send-targeted-playlist"

  [<Literal>]
  let PlaylistIdCannotBeParsed = "messages.playlist-id-cannot-be-parsed"

  [<Literal>]
  let PlaylistNotFoundInSpotify = "messages.playlist-not-found-in-spotify"

  [<Literal>]
  let PlaylistIsReadonly = "messages.playlist-is-readonly"

  [<Literal>]
  let SendPresetName = "messages.send-preset-name"

  [<Literal>]
  let ArtistsAlbumsRecommendation = "messages.artists-albums-recommendation"

  [<Literal>]
  let ReccoBeatsRecommendation = "messages.recco-beats-recommendation"

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
  let TargetedPlaylistDetails = "messages.targeted-playlist-details"

  [<Literal>]
  let UnknownCommand = "messages.unknown-command"

  [<Literal>]
  let NoCurrentPreset = "messages.no-current-preset"

  [<Literal>]
  let PresetValidationError = "messages.preset-validation-error"

  [<Literal>]
  let PresetRemoved = "messages.preset-removed"

  [<Literal>]
  let PresetNotFound = "messages.preset-not-found"

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
  let DisableRecommendations = "buttons.disable-recommendations"

  [<Literal>]
  let EnableUniqueArtists = "buttons.enable-unique-artists"

  [<Literal>]
  let DisableUniqueArtists = "buttons.disable-unique-artists"

  [<Literal>]
  let Back = "buttons.back"
