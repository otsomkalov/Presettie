module Telegram.Constants

open Microsoft.FSharp.Core

module Commands =
  [<Literal>]
  let start = "/start"

  [<Literal>]
  let faq = "/faq"

  [<Literal>]
  let privacy = "/privacy"

  [<Literal>]
  let guide = "/guide"

  [<Literal>]
  let help = "/help"

  [<Literal>]
  let presets = "/presets"

  [<Literal>]
  let size = "/size"

  [<Literal>]
  let newPreset = "/new"

  [<Literal>]
  let includePlaylist = "/include"

  [<Literal>]
  let excludePlaylist = "/exclude"

  [<Literal>]
  let targetPlaylist = "/target"

  [<Literal>]
  let runPreset = "/run"

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
  let artistsAlbumsRecommendations = "aar"

  [<Literal>]
  let reccoBeatsRecommendations = "rbr"

  [<Literal>]
  let disableRecommendations = "dr"

  [<Literal>]
  let enableUniqueArtists = "eua"

  [<Literal>]
  let disableUniqueArtists = "dua"