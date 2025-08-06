[<RequireQualifiedAccess>]
module Web.Preset

open Bolero
open Bolero.Html
open Domain.Core
open Elmish
open Web.Repos
open Web.Util

[<RequireQualifiedAccess>]
module private PresetTile =
  let view (preset: SimplePreset) dispatch = div {
    attr.``class`` "col-md-4"

    div {
      attr.``class`` "card"

      div {
        attr.``class`` "card-header"

        a {

          preset.Name
        }
      }

      div {
        attr.``class`` "card-footer"

        button {
          attr.``class`` "btn btn-danger"

          "Delete"
        }
      }
    }
  }

[<RequireQualifiedAccess>]
module List =
  type Model = { Presets: AsyncOp<SimplePreset list> }

  let init () : Model = { Presets = AsyncOp.Loading }

  type Message = PresetsLoaded of SimplePreset list

  let update (env: #IListPresets) (message: Message) (model: Model) : Model * Cmd<Message> =
    match message, model with
    | PresetsLoaded presets, _ ->
      { model with
          Presets = AsyncOp.Finished presets },
      Cmd.none

  let view (model: Model) dispatch =
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
type Message = List of List.Message