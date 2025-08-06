module Web.Main

open Domain.Core
open Elmish
open Bolero
open Bolero.Html
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Authorization
open Microsoft.AspNetCore.Components.WebAssembly.Authentication
open Web.Messages
open Web.Models
open Web.Repos
open Web.Router
open Web.Views

[<RequireQualifiedAccess>]
module Loading =
  let render () _ = div {
    attr.``class`` "d-flex flex-column align-items-center justify-content-center vh-100"

    div {
      attr.``class`` "spinner-border text-primary"
      "role" => "status"

      span {
        attr.``class`` "visually-hidden"
        text "Loading..."
      }
    }
  }

let initModel =
  fun _ ->
    { Page = Page.Loading
      AuthState = None },
    Cmd.none

let anonPageUpdate (authProvider: AuthenticationStateProvider) (page: Page) model =
  let withAuthCheck model page =
    { model with Page = page }, Cmd.OfTask.perform authProvider.GetAuthenticationStateAsync () (AuthChecked >> Auth)

  match page with
  | Page.Auth action -> { model with Page = Page.Auth action }, Cmd.none
  | Page.Loading -> { model with Page = Page.Loading }, Cmd.none
  | page -> withAuthCheck model page

let unauthorizedPageUpdate (page: Page) model =
  let withNoAuth model page = { model with Page = page }, Cmd.none

  match page with
  | p -> withNoAuth model p

let authorizedPageUpdate (env: #IListPresets & #IGetPreset) (page: Page) model =
  let withNoCommand model page = { model with Page = page }, Cmd.none

  match page with
  | Page.Presets p ->
    { model with Page = Page.Presets p },
    Cmd.OfTask.perform env.ListPresets () (Preset.List'.Message.PresetsLoaded >> Preset.Message.List >> Message.Preset)
  | Page.Preset(id, p) ->
    { model with Page = Page.Preset(id, p) },
    Cmd.OfTask.perform env.GetPreset' (RawPresetId id) (Preset.Details'.Message.PresetLoaded >> Preset.Message.Details >> Message.Preset)
  | p -> withNoCommand model p

let authUpdate (env: #IListPresets & #IGetPreset) (authResult: AuthenticationState) model =
  match model.Page with
  | Page.Presets p when authResult.User.Identity.IsAuthenticated ->
    { model with
        AuthState = Some authResult },
    Cmd.OfTask.perform env.ListPresets () (Preset.List'.Message.PresetsLoaded >> Preset.Message.List >> Message.Preset)
  | Page.Preset(id, p) when authResult.User.Identity.IsAuthenticated ->
    { model with
        AuthState = Some authResult },
    Cmd.OfTask.perform env.GetPreset' (RawPresetId id) (Preset.Details'.Message.PresetLoaded >> Preset.Message.Details >> Message.Preset)
  | _ ->
    { model with
        AuthState = Some authResult },
    Cmd.none

let presetUpdate env (message: Preset.Message) (model: Model) =
  match message, model with
  | Preset.Message.List(msg), { Page = Page.Presets m } ->
    let model', cmd' = Preset.List.update env msg m.Model

    { model with
        Page = Page.Presets { Model = model' } },
    Cmd.map (Preset.Message.List >> Message.Preset) cmd'
  | Preset.Message.Details(msg), { Page = Page.Preset(id, m) } ->
    let model', cmd' = Preset.Details.update msg m.Model

    { model with
        Page = Page.Preset(id, { Model = model' }) },
    Cmd.map (Preset.Message.Details >> Message.Preset) cmd'
  | _ -> model, Cmd.none

let update authProvider env (message: Message) (model: Model) =
  match message, model with
  | PageChanged page, { AuthState = None } -> anonPageUpdate authProvider page model
  | PageChanged page, { AuthState = Some(state) } when not state.User.Identity.IsAuthenticated -> unauthorizedPageUpdate page model
  | PageChanged page, { AuthState = Some(state) } when state.User.Identity.IsAuthenticated -> authorizedPageUpdate env page model
  | Auth(AuthMsg.AuthChecked result), m -> authUpdate env result m
  | Preset(msg), m -> presetUpdate env msg m
  | _ -> model, Cmd.none

let view model dispatch = concat {
  Layout.Header.view model.AuthState dispatch

  div {
    attr.``class`` "container-fluid"

    match model.Page, model.AuthState with
    | Page.Home, _ -> div { "Home" }
    | Page.About, _ -> div { "About" }
    | Page.Loading, _ -> Loading.render () dispatch
    | Page.NotFound, _ -> div { text "Not Found" }

    | Page.Presets m, Some(state) when state.User.Identity.IsAuthenticated -> Preset.List.view m.Model dispatch
    | Page.Presets _, Some(state) when not state.User.Identity.IsAuthenticated -> comp<RemoteAuthenticatorView> { "Action" => "login" }
    | Page.Presets _, _ -> Loading.render () dispatch

    | Page.Preset(id, m), Some(state) when state.User.Identity.IsAuthenticated -> Preset.Details.view m.Model dispatch
    | Page.Preset _, Some(state) when not state.User.Identity.IsAuthenticated -> comp<RemoteAuthenticatorView> { "Action" => "login" }
    | Page.Preset _, _ -> Loading.render () dispatch

    | Page.Profile, Some(state) when state.User.Identity.IsAuthenticated -> div { $"Hello {state.User.Identity.Name}" }
    | Page.Profile, Some(state) when not state.User.Identity.IsAuthenticated -> comp<RemoteAuthenticatorView> { "Action" => "login" }
    | Page.Profile, _ -> Loading.render () dispatch

    | Page.Auth action, _ -> comp<RemoteAuthenticatorView> { "Action" => action }
  }
}

type App() =
  inherit ProgramComponent<Model, Message>()

  [<Inject>]
  member val AuthenticationProvider: AuthenticationStateProvider = null with get, set

  [<Inject>]
  member val Env: IEnv = Unchecked.defaultof<IEnv> with get, set

  override this.Program =
    Program.mkProgram initModel (update this.AuthenticationProvider this.Env) view
    |> Program.withConsoleTrace
    |> Program.withRouter router