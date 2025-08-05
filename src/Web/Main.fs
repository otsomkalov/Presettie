module Web.Main

open Elmish
open Bolero
open Bolero.Html
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Authorization
open Microsoft.AspNetCore.Components.WebAssembly.Authentication
open Web.Repos
open Web.Shared

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

let presetUpdate env (message: Preset.Message) (model: Preset.Page) =
  match message, model with
  | Preset.Message.List(msg), Preset.Page.List m ->
    let model', cmd' = Preset.List.update env msg m.Model

    Page.Presets { Model = model' }, Cmd.map (Preset.Message.List >> Message.Preset) cmd'

let update (authProvider: AuthenticationStateProvider) (env: #IListPresets) (message: Message) (model: Model) =
  match message, model with
  | PageChanged(Page.Loading), m -> { m with Page = Page.Loading }, Cmd.none

  | PageChanged(Page.Home), { AuthState = None } ->
    { model with Page = Page.Home }, Cmd.OfTask.perform authProvider.GetAuthenticationStateAsync () (AuthChecked >> Auth)
  | PageChanged(Page.Home), m -> { m with Page = Page.Home }, Cmd.none
  | PageChanged(Page.NotFound), m -> { m with Page = Page.NotFound }, Cmd.none

  | PageChanged(Page.About), { AuthState = None } ->
    { model with Page = Page.About }, Cmd.OfTask.perform authProvider.GetAuthenticationStateAsync () (AuthChecked >> Auth)
  | PageChanged(Page.About), m -> { m with Page = Page.About }, Cmd.none

  | PageChanged(Page.Profile), { AuthState = None } ->
    { model with Page = Page.Profile }, Cmd.OfTask.perform authProvider.GetAuthenticationStateAsync () (AuthChecked >> Auth)
  | PageChanged(Page.Profile), m -> { m with Page = Page.Profile }, Cmd.none


  | PageChanged(Page.Presets { Model = p }), ({ AuthState = Some(state) } as m) when state.User.Identity.IsAuthenticated ->
    { m with
        Page = Page.Presets { Model = p } },
    Cmd.OfTask.perform env.ListPresets () (Preset.List.PresetsLoaded >> Preset.Message.List >> Message.Preset)
  | PageChanged(Page.Presets p), _ ->
    { model with Page = Page.Presets p }, Cmd.OfTask.perform authProvider.GetAuthenticationStateAsync () (AuthChecked >> Auth)

  | PageChanged(Page.Auth action), _ -> { model with Page = Page.Auth action }, Cmd.none

  | Auth(AuthMsg.AuthChecked result), m ->
    match m with
    | { Page = Page.Presets p } ->
      { m with AuthState = Some result },
      Cmd.OfTask.perform env.ListPresets () (Preset.List.PresetsLoaded >> Preset.Message.List >> Message.Preset)

    | _ -> { m with AuthState = Some result }, Cmd.none

  | Preset(presetMsg), { Page = Page.Presets p } ->
    match presetMsg with
    | Preset.Message.List msg ->
      let model', cmd = Preset.List.update env msg p.Model

      { model with
          Page = Page.Presets { Model = model' } },
      Cmd.map (Preset.Message.List >> Message.Preset) cmd

let view model dispatch = concat {
  Layout.Header.view model.AuthState dispatch

  match model.Page, model.AuthState with
  | Page.Home, _ -> div { "Home" }
  | Page.About, _ -> div { "About" }
  | Page.Loading, _ -> Loading.render () dispatch
  | Page.NotFound, _ -> div { text "Not Found" }

  | Page.Presets m, Some(state) when state.User.Identity.IsAuthenticated -> Preset.List.view m.Model dispatch
  | Page.Presets _, Some(state) when not state.User.Identity.IsAuthenticated -> comp<RemoteAuthenticatorView> { "Action" => "login" }
  | Page.Presets _, _ -> Loading.render () dispatch

  | Page.Profile, Some(state) when state.User.Identity.IsAuthenticated -> div { $"Hello {state.User.Identity.Name}" }
  | Page.Profile, Some(state) when not state.User.Identity.IsAuthenticated -> comp<RemoteAuthenticatorView> { "Action" => "login" }
  | Page.Profile, _ -> Loading.render () dispatch

  | Page.Auth action, _ -> comp<RemoteAuthenticatorView> { "Action" => action }
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