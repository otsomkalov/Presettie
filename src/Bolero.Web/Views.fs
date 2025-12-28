module Bolero.Web.Views

open Bolero.Html
open Domain.Core

[<RequireQualifiedAccess>]
module IncludedPlaylist =
  let view (playlist: IncludedPlaylist) dispatch = div {
    attr.``class`` "card"

    div {
      attr.``class`` "card-header"

      playlist.Name
    }
  }

[<RequireQualifiedAccess>]
module ExcludedPlaylist =
  let view (playlist: ExcludedPlaylist) dispatch = div {
    attr.``class`` "card"

    div {
      attr.``class`` "card-header"

      playlist.Name
    }
  }

[<RequireQualifiedAccess>]
module TargetedPlaylist =
  let view (playlist: TargetedPlaylist) dispatch = div {
    attr.``class`` "card"

    div {
      attr.``class`` "card-header"

      playlist.Name
    }
  }

[<RequireQualifiedAccess>]
module IncludedArtist =
  let view (artist: IncludedArtist) dispatch = div {
    attr.``class`` "card"

    div {
      attr.``class`` "card-header"

      artist.Name
    }
  }

[<RequireQualifiedAccess>]
module ExcludedArtist =
  let view (artist: ExcludedArtist) dispatch = div {
    attr.``class`` "card"

    div {
      attr.``class`` "card-header"

      artist.Name
    }
  }

[<RequireQualifiedAccess>]
module IncludedPlaylists =
  let view preset dispatch = div {
    attr.``class`` "row"

    for includedPlaylist in preset.IncludedPlaylists do
      div {
        attr.``class`` "col-md-4 mb-3"
        IncludedPlaylist.view includedPlaylist dispatch
      }
  }

[<RequireQualifiedAccess>]
module ExcludedPlaylists =
  let view preset dispatch = div {
    attr.``class`` "row"

    for excludedPlaylist in preset.ExcludedPlaylists do
      div {
        attr.``class`` "col-md-4 mb-3"
        ExcludedPlaylist.view excludedPlaylist dispatch
      }
  }

[<RequireQualifiedAccess>]
module TargetedPlaylists =
  let view preset dispatch = div {
    attr.``class`` "row"

    for targetedPlaylist in preset.TargetedPlaylists do
      div {
        attr.``class`` "col-md-4 mb-3"
        TargetedPlaylist.view targetedPlaylist dispatch
      }
  }

[<RequireQualifiedAccess>]
module IncludedArtists =
  let view preset dispatch = div {
    attr.``class`` "row"

    for includedArtist in preset.IncludedArtists do
      div {
        attr.``class`` "col-md-4 mb-3"
        IncludedArtist.view includedArtist dispatch
      }
  }

[<RequireQualifiedAccess>]
module ExcludedArtists =
  let view preset dispatch = div {
    attr.``class`` "row"

    for excludedArtist in preset.ExcludedArtists do
      div {
        attr.``class`` "col-md-4 mb-3"
        ExcludedArtist.view excludedArtist dispatch
      }
  }