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