module Bolero.Web.Main

open System
open Blazored.Modal
open Domain.Core
open Elmish
open Bolero
open Bolero.Html
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Authorization
open Microsoft.AspNetCore.Components.Routing
open Microsoft.AspNetCore.Components.Web
open Microsoft.AspNetCore.Components.WebAssembly.Authentication
open Bolero.Web.Messages
open Bolero.Web.Models
open Bolero.Web.Repos
open Bolero.Web.Router
open Bolero.Web.Util
open Bolero.Web.Views

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
  fun _ -> { Page = Page.Presets { Model = { Presets = AsyncOp.Loading } } }, Cmd.none

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

let authUpdate (env: #IListPresets & #IGetPreset) model =
  match model.Page with
  | Page.Presets p ->
    model, Cmd.OfTask.perform env.ListPresets () (Preset.List'.Message.PresetsLoaded >> Preset.Message.List >> Message.Preset)
  | Page.Preset(id, p) ->
    model,
    Cmd.OfTask.perform env.GetPreset' (RawPresetId id) (Preset.Details'.Message.PresetLoaded >> Preset.Message.Details >> Message.Preset)
  | _ -> model, Cmd.none

let presetUpdate (env: #ICreatePreset & #IRemovePreset) (message: Preset.Message) (model: Model) =
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
  | Preset.Message.Create(msg), { Page = Page.CreatePreset m } ->
    let model', cmd' = Preset.Create.update env msg m.Model

    { model with
        Page = Page.CreatePreset { Model = model' } },
    cmd'
  | Preset.Message.Remove(Preset.Remove'.Message.RemovePreset id), { Page = Page.Presets m } ->
    model, Cmd.OfTask.perform env.RemovePreset id (fun _ -> Preset.Remove'.Message.PresetRemoved id |> Preset.Message.Remove |> Preset)
  | Preset.Message.Remove(Preset.Remove'.Message.PresetRemoved removedPresetId),
    { Page = Page.Presets { Model = { Presets = AsyncOp.Finished presets } } } ->
    let presets' = presets |> List.filter (fun p -> p.Id <> removedPresetId)

    { model with
        Page = Page.Presets { Model = { Presets = AsyncOp.Finished presets' } } },
    Cmd.none
  | _ -> model, Cmd.none

let update env (message: Message) (model: Model) =
  match message, model with
  | PageChanged page, model -> authorizedPageUpdate env page model
  | Preset(msg), m -> presetUpdate env msg m
  | _ -> model, Cmd.none

let view model dispatch = concat {
  div {
    attr.``class`` "container-fluid"

    match model.Page with
    | Page.Presets m -> Preset.List.view m.Model (Message.Preset >> dispatch)
    | Page.Preset(id, m) -> Preset.Details.view m.Model dispatch
    | Page.CreatePreset m -> Preset.Create.view m.Model dispatch
  }
}

[<Route("")>]
type Home() =
  inherit Component()

  override this.Render() = div { "Home" }

[<Route("about")>]
type About() =
  inherit Component()

  override this.Render() = div { "About" }

[<Route("profile")>]
[<Authorize>]
type Profile() =
  inherit Component()

  override this.Render() = div { $"Hello" }

[<Route("presets*")>]
[<Authorize>]
type Presets() =
  inherit ProgramComponent<Model, Message>()

  [<Inject>]
  member val Env: IEnv = Unchecked.defaultof<IEnv> with get, set

  override this.Program =
    Program.mkProgram initModel (update this.Env) view
    |> Program.withConsoleTrace
    |> Program.withRouter router

[<Route("/authentication/{action}")>]
[<AllowAnonymous>]
type Authentication() =
  inherit Component()

  [<Parameter>]
  member val Action: string | null = null with get, set

  override this.Render() = comp<RemoteAuthenticatorView> { "Action" => this.Action }

type RedirectToLogin() =
  inherit Component()

  [<Inject>]
  member val NavigationManager = Unchecked.defaultof<NavigationManager> with get, set

  override this.OnInitialized() =
    this.NavigationManager.NavigateToLogin("authentication/login")

    ()

  override this.Render() = empty ()

type NotFound() =
  inherit Component()

  override this.Render() = div {
    comp<PageTitle> { "Not found" }

    h1 { "Not Found" }
  }

type Root() =
  inherit Component()

  let unauthorizedView (authenticationState: AuthenticationState) =
    match
      authenticationState.User.Identity
      |> Option.ofObj
      |> Option.map _.IsAuthenticated
    with
    | Some true -> div { "You are not authorized to access this page." }
    | _ -> comp<RedirectToLogin> { attr.empty () }

  override this.Render() = comp<CascadingAuthenticationState> {
    comp<CascadingBlazoredModal> {
      comp<Router> {
        "AppAssembly" => typeof<Root>.Assembly

        attr.fragmentWith "Found" (fun (routeData: RouteData) -> concat {
          comp<AuthorizeRouteView> {
            "RouteData" => routeData
            "DefaultLayout" => typeof<Layout.Layout>

            attr.fragmentWith "NotAuthorized" unauthorizedView
          }

          comp<FocusOnNavigate> {
            "RouteData" => routeData
            "Selector" => "h1"
          }
        })

        attr.fragment "NotFound" (comp<NotFound> { attr.empty () })
      }
    }
  }