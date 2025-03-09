module Telegram.Constants

open Microsoft.FSharp.Core

module CallbackQueryConstants =
  [<Literal>]
  let includeLikedTracks = "ilt"
  [<Literal>]
  let excludeLikedTracks = "elt"
  [<Literal>]
  let ignoreLikedTracks = "ignore-liked-tracks"
  [<Literal>]
  let setPresetSize = "sps"
  [<Literal>]
  let enableRecommendations = "er"
  [<Literal>]
  let disableRecommendations = "dr"
  [<Literal>]
  let enableUniqueArtists = "eua"
  [<Literal>]
  let disableUniqueArtists = "dua"

[<RequireQualifiedAccess>]
module MessageCommands =
  let start = "/start"
  let faq = "/faq"
  let privacy = "/privacy"
  let guide = "/guide"
  let help = "/help"
  let presets = "/presets"
  let size = "/size"
  let newPreset = "/new"
  let includePlaylist = "/include"
  let excludePlaylist = "/exclude"
  let targetPlaylist = "/target"
  let runPreset = "/run"

[<RequireQualifiedAccess>]
module Messages =
  let successfulLogin = "successful-login"
  let stateNotFound = "state-not-found"
  let notUserState = "not-user-state"