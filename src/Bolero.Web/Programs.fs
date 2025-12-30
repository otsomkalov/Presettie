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
open Bolero

[<RequireQualifiedAccess>]
module Preset =
  [<RequireQualifiedAccess>]
  module List =
    type Model =
      { Presets: AsyncOp<SimplePreset list>
        DeletingPreset: SimplePreset option }

    type Message =
      | LoadPresets
      | PresetsLoaded of SimplePreset list
      | AskDeletePreset of SimplePreset
      | ConfirmDeletePreset
      | CancelDeletePreset
      | PresetRemoved of PresetId

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
            attr.``class`` "card-footer d-flex justify-content-end"

            button {
              attr.``class`` "btn btn-danger"

              on.click (fun _ -> AskDeletePreset preset |> dispatch)

              "Delete"
            }
          }
        }
      }

    let init _ =
      { Presets = AsyncOp.Loading
        DeletingPreset = None },
      Cmd.ofMsg LoadPresets

    let update
      (modalRef: Ref<Modal>)
      (toastService: ToastService)
      (logger: ILogger)
      (env: #IListPresets & #IRemovePreset)
      (message: Message)
      (model: Model)
      : Model * Cmd<Message> =
      match message with
      | LoadPresets -> model, Cmd.OfTask.perform env.ListPresets () PresetsLoaded
      | PresetsLoaded presets ->
        { model with
            Presets = AsyncOp.Finished presets },
        Cmd.none
      | AskDeletePreset preset ->
        { model with
            DeletingPreset = Some preset },
        Cmd.OfTask.attempt Modal.show modalRef (fun _ -> CancelDeletePreset)
      | ConfirmDeletePreset ->
        match model.DeletingPreset with
        | Some preset ->

          model,
          Cmd.batch
            [ Cmd.OfTask.perform env.RemovePreset preset.Id (fun () -> PresetRemoved preset.Id)
              Cmd.OfTask.attempt Modal.hide modalRef (fun _ -> CancelDeletePreset) ]
        | None -> model, Cmd.none
      | CancelDeletePreset -> { model with DeletingPreset = None }, Cmd.OfTask.attempt Modal.hide modalRef (fun _ -> CancelDeletePreset)
      | PresetRemoved presetId ->
        match model.Presets with
        | AsyncOp.Finished presets ->
          toastService.Notify(
            ToastMessage(Type = ToastType.Success, Title = "Preset deleted", Message = "Preset has been successfully deleted.")
          )

          let presets = presets |> List.filter (fun p -> p.Id <> presetId)

          { model with
              Presets = AsyncOp.Finished presets },
          Cmd.none
        | _ -> model, Cmd.none

    let view (modal: Bolero.Ref<Modal>) (model: Model) (dispatch: Message -> unit) =
      match model.Presets with
      | AsyncOp.Loading -> div { text "Loading presets..." }
      | AsyncOp.Finished presets -> div {
          attr.``class`` "d-flex flex-column gap-2"

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
            attr.``class`` "row gap-1"

            for preset in presets do
              PresetTile.view preset dispatch
          }

          comp<Modal> {
            "Title" => "Confirm deletion"

            attr.fragment
              "BodyTemplate"
              (match model.DeletingPreset with
               | Some preset -> p { sprintf "Are you sure you want to delete preset '%s'?" preset.Name }
               | None -> empty ())

            attr.fragment
              "FooterTemplate"
              (concat {
                button {
                  attr.``class`` "btn btn-secondary"
                  on.click (fun _ -> CancelDeletePreset |> dispatch)
                  "Cancel"
                }

                button {
                  attr.``class`` "btn btn-danger"
                  on.click (fun _ -> ConfirmDeletePreset |> dispatch)
                  "Delete"
                }
              })

            attr.ref modal
          }
        }

  [<RequireQualifiedAccess>]
  module Details =
    type Model = { Preset: AsyncOp<Preset> }

    type Message =
      | LoadPreset of RawPresetId
      | PresetLoaded of Preset
      | RunPreset of PresetId

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
      | RunPreset presetId ->

        model, Cmd.none

    let view (model: Model) dispatch =
      match model.Preset with
      | AsyncOp.Loading -> div { text "Loading..." }
      | AsyncOp.Finished preset -> div {
          attr.``class`` "d-flex flex-column gap-2"

          div {
            attr.``class`` "d-flex justify-content-between align-items-center"

            h1 { text preset.Name }

            div {
              attr.``class`` "d-flex gap-1"

              button {
                attr.``class`` "btn btn-outline-primary"

                "Settings"
              }

              button {
                attr.``class`` "btn btn-success"

                on.click (fun _ -> RunPreset preset.Id |> dispatch)

                "Run"
              }
            }
          }

          div {
            comp<Accordion> {
              comp<AccordionItem> {
                "Title" => "Included Playlists"

                attr.fragment "Content" (IncludedPlaylists.view preset dispatch)
              }

              comp<AccordionItem> {
                "Title" => "Included Artists"

                attr.fragment "Content" (IncludedArtists.view preset dispatch)
              }

              comp<AccordionItem> {
                "Title" => "Excluded Playlists"

                attr.fragment "Content" (ExcludedPlaylists.view preset dispatch)
              }

              comp<AccordionItem> {
                "Title" => "Excluded Artists"

                attr.fragment "Content" (ExcludedArtists.view preset dispatch)
              }

              comp<AccordionItem> {
                "Title" => "Targeted Playlists"

                attr.fragment "Content" (TargetedPlaylists.view preset dispatch)
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