module Web.Views

open Bolero
open Bolero.Html
open Elmish
open Web.Models
open Web.Repos
open Web.Router
open Web.Util
open Web.Messages
open Domain.Core

[<RequireQualifiedAccess>]
module Preset =
  [<RequireQualifiedAccess>]
  module private IncludedPlaylist =
    let view (playlist: IncludedPlaylist) dispatch = div {
      attr.``class`` "card"

      div {
        attr.``class`` "card-header"

        playlist.Name
      }
    }

  [<RequireQualifiedAccess>]
  module private ExcludedPlaylist =
    let view (playlist: ExcludedPlaylist) dispatch = div {
      attr.``class`` "card"

      div {
        attr.``class`` "card-header"

        playlist.Name
      }
    }

  [<RequireQualifiedAccess>]
  module private TargetedPlaylist =
    let view (playlist: TargetedPlaylist) dispatch = div {
      attr.``class`` "card"

      div {
        attr.``class`` "card-header"

        playlist.Name
      }
    }

  [<RequireQualifiedAccess>]
  module private PresetTile =
    let view (preset: SimplePreset) dispatch = div {
      attr.``class`` "col-md-4"

      div {
        attr.``class`` "card"

        div {
          attr.``class`` "card-header"

          a {
            router.HRef(Page.Preset(preset.Id.Value, { Model = { Preset = AsyncOp.Loading } }))

            preset.Name
          }
        }

        div {
          attr.``class`` "card-footer"

          button {
            attr.``class`` "btn btn-danger"
            on.click (fun _ -> preset.Id |> Preset.Remove'.Message.RemovePreset |> Preset.Message.Remove |> dispatch)

            "Delete"
          }
        }
      }
    }

  [<RequireQualifiedAccess>]
  module List =
    let init () : Preset.List.Model = { Presets = AsyncOp.Loading }

    let update (env: #IListPresets) (message: Preset.List'.Message) (model: Preset.List.Model) : Preset.List.Model * Cmd<Preset.List'.Message> =
      match message, model with
      | Preset.List'.Message.PresetsLoaded presets, _ ->
        { model with
            Presets = AsyncOp.Finished presets },
        Cmd.none

    let view (model: Preset.List.Model) (dispatch: Preset.Message -> unit) =
      match model.Presets with
      | AsyncOp.Loading -> div { text "Loading presets..." }
      | AsyncOp.Finished presets -> concat {
          div {
            attr.``class`` "row justify-content-end"

            div {
              attr.``class`` "col-sm-12 col-md-2"

              a {
                attr.``class`` "btn btn-success w-100"
                attr.href "/presets/create"

                "New preset"
              }
            }
          }

          div {
            attr.``class`` "row"

            for preset in presets do
              PresetTile.view preset dispatch
          }
        }

  [<RequireQualifiedAccess>]
  module Details =
    let init _ : Preset.Details.Model = { Preset = AsyncOp.Loading }

    let update (message: Preset.Details'.Message) model : Preset.Details.Model * Cmd<Preset.Details'.Message> =

      match message with
      | Preset.Details'.Message.PresetLoaded preset ->
        { model with
            Preset = AsyncOp.Finished preset },
        Cmd.none

    let view (model: Preset.Details.Model) dispatch =
      match model.Preset with
      | AsyncOp.Loading -> div { text "Loading..." }
      | AsyncOp.Finished preset -> div {
          h1 { text preset.Name }

          div {
            attr.``class`` "row"

            div {
              attr.``class`` "col-lg-4"

              div {
                attr.``class`` "card"

                h2 {
                  attr.``class`` "card-header"

                  "Included Playlists"
                }

                div {
                  attr.``class`` "card-body"

                  for includedPlaylist in preset.IncludedPlaylists do
                    IncludedPlaylist.view includedPlaylist dispatch
                }
              }
            }

            div {
              attr.``class`` "col-lg-4"

              div {
                attr.``class`` "card"

                h2 {
                  attr.``class`` "card-header"

                  "Excluded Playlists"
                }

                div {
                  attr.``class`` "card-body"

                  for excludedPlaylist in preset.ExcludedPlaylists do
                    ExcludedPlaylist.view excludedPlaylist dispatch
                }
              }
            }

            div {
              attr.``class`` "col-lg-4"

              div {
                attr.``class`` "card"

                h2 {
                  attr.``class`` "card-header"

                  "Targeted Playlists"
                }

                div {
                  attr.``class`` "card-body"

                  for targetedPlaylist in preset.TargetedPlaylists do
                    TargetedPlaylist.view targetedPlaylist dispatch
                }
              }
            }
          }
        }