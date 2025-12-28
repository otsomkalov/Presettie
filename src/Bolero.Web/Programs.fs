module Bolero.Web.Programs

open System
open Bolero.Web.Util
open Bolero.Web.Views
open Domain.Core
open Elmish
open Bolero.Html
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Routing
open Microsoft.Extensions.Logging
open Bolero.Web.Repos
open BlazorBootstrap

[<RequireQualifiedAccess>]
module Preset =
  [<RequireQualifiedAccess>]
  module List =
    type Model = { Presets: AsyncOp<SimplePreset list> }

    type Message =
      | LoadPresets
      | PresetsLoaded of SimplePreset list

    [<RequireQualifiedAccess>]
    module private PresetTile =
      let view (preset: SimplePreset) dispatch = div {
        attr.``class`` "col-md-4"

        div {
          attr.``class`` "card"

          div {
            attr.``class`` "card-header"

            navLink NavLinkMatch.All {
              attr.href (sprintf "presets/%s" preset.Id.Value)
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

    let init _ =
      { Presets = AsyncOp.Loading }, Cmd.ofMsg LoadPresets

    let update (logger: ILogger) (env: #IListPresets & #IRemovePreset) (message: Message) (model: Model) : Model * Cmd<Message> =
      match message with
      | LoadPresets -> model, Cmd.OfTask.perform env.ListPresets () PresetsLoaded
      | PresetsLoaded presets ->
        { model with
            Presets = AsyncOp.Finished presets },
        Cmd.none

    let view (model: Model) (dispatch: Message -> unit) =
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
    type Model = { Preset: AsyncOp<Preset> }

    type Message =
      | LoadPreset of RawPresetId
      | PresetLoaded of Preset

    let init presetId =
      fun _ -> { Preset = AsyncOp.Loading }, Cmd.ofMsg (LoadPreset presetId)

    let update (logger: ILogger) (env: #IGetPreset) (message: Message) model : Model * Cmd<Message> =
      match message with
      | LoadPreset(RawPresetId presetId) ->
        let parsedPresetId = PresetId presetId

        { model with Preset = AsyncOp.Loading }, Cmd.OfTask.perform env.GetPreset' parsedPresetId PresetLoaded
      | PresetLoaded preset ->
        { model with
            Preset = AsyncOp.Finished preset },
        Cmd.none

    let view (model: Model) dispatch =
      match model.Preset with
      | AsyncOp.Loading -> div { text "Loading..." }
      | AsyncOp.Finished preset -> div {
          h1 { text preset.Name }

          div {
            comp<Accordion> {
              comp<AccordionItem> {
                "Title" => "Included Playlists"

                attr.fragment "Content" (IncludedPlaylists.view preset dispatch)
              }

              comp<AccordionItem> {
                "Title" => "Excluded Playlists"

                attr.fragment "Content" (ExcludedPlaylists.view preset dispatch)
              }

              comp<AccordionItem> {
                "Title" => "Targeted Playlists"

                attr.fragment "Content" (TargetedPlaylists.view preset dispatch)
              }

              comp<AccordionItem> {
                "Title" => "Included Artists"

                attr.fragment "Content" (IncludedArtists.view preset dispatch)
              }

              comp<AccordionItem> {
                "Title" => "Excluded Artists"

                attr.fragment "Content" (ExcludedArtists.view preset dispatch)
              }
            }
          }
        }

  [<RequireQualifiedAccess>]
  module Create =
    type Model = { Name: string }

    type Message =
      | CreatePreset
      | NameChanged of string
      | CreatePresetError of exn
      | Redirect of string

    let init = fun _ -> { Name = String.Empty }, Cmd.none

    let update
      (logger: ILogger)
      (navigationManager: NavigationManager)
      (env: #ICreatePreset)
      (message: Message)
      (model: Model)
      : Model * Cmd<Message> =
      match message with
      | Message.NameChanged name -> { model with Name = name }, Cmd.none
      | Message.CreatePreset when model.Name |> String.IsNullOrEmpty |> not ->
        model,
        Cmd.OfTask.either
          env.CreatePreset
          model.Name
          (fun (PresetId presetId) -> Redirect(sprintf "/presets/%s" presetId))
          CreatePresetError
      | Message.Redirect url ->
        navigationManager.NavigateTo url
        model, Cmd.none
      | Message.CreatePresetError exn ->
        logger.LogError(exn, "Error during creating Preset:")

        model, Cmd.none
      | _ -> model, Cmd.none

    let view (model: Model) dispatch = div {
      attr.``class`` "container"

      h1 { text "Create New Preset" }

      form {
        attrs { "onsubmit" => "return false" }

        attr.``class`` "form"

        on.submit (fun _ -> Message.CreatePreset |> dispatch)

        div {
          attr.``class`` "mb-3"

          label {
            attr.``for`` "presetName"
            attr.``class`` "form-label"
            text "Preset Name"
          }

          input {
            attr.``type`` "text"
            attr.id "presetName"
            attr.value model.Name
            attr.required true

            on.input (_.Value >> string >> NameChanged >> dispatch)

            attr.``class`` "form-control"
          }
        }

        button {
          attr.``type`` "button"
          attr.``class`` "btn btn-primary"

          on.click (fun _ -> CreatePreset |> dispatch)

          text "Create Preset"
        }
      }
    }