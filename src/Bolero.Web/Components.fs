module Bolero.Web.Components

open Bolero.Web.Programs
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
open Bolero.Web.Repos
open Microsoft.Extensions.Logging

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

  override this.Render() = comp<AuthorizeView> {
    attr.fragmentWith "Authorized" (fun (state: AuthenticationState) -> div { sprintf "Hello %s" state.User.Identity.Name })
    attr.fragmentWith "NotAuthorized" (fun (_: AuthenticationState) -> p { "You are not authorized" })
  }

[<Route("presets")>]
[<Authorize>]
type Presets() =
  inherit ProgramComponent<Preset.List.Model, Preset.List.Message>()

  [<Inject>]
  member val Env: IEnv = Unchecked.defaultof<IEnv> with get, set

  [<Inject>]
  member val Logger = Unchecked.defaultof<ILogger<Presets>> with get, set

  override this.Program =
    Program.mkProgram Programs.Preset.List.init (Programs.Preset.List.update this.Logger this.Env) Programs.Preset.List.view
    |> Program.withConsoleTrace

[<Route("presets/create")>]
[<Authorize>]
type CreatePreset() =
  inherit ProgramComponent<Preset.Create.Model, Preset.Create.Message>()

  [<Inject>]
  member val Env: IEnv = Unchecked.defaultof<IEnv> with get, set

  [<Inject>]
  member val Logger = Unchecked.defaultof<ILogger<CreatePreset>> with get, set

  [<Inject>]
  member val NavigationManager = Unchecked.defaultof<NavigationManager> with get, set

  override this.Program =
    Program.mkProgram
      Programs.Preset.Create.init
      (Programs.Preset.Create.update this.Logger this.NavigationManager this.Env)
      Programs.Preset.Create.view
    |> Program.withConsoleTrace

[<Route("presets/{presetId}")>]
[<Authorize>]
type Preset() =
  inherit ProgramComponent<Preset.Details.Model, Preset.Details.Message>()

  [<Inject>]
  member val Env: IEnv = Unchecked.defaultof<IEnv> with get, set

  [<Inject>]
  member val Logger = Unchecked.defaultof<ILogger<Preset>> with get, set

  [<Parameter>]
  member val PresetId = Unchecked.defaultof<string> with get, set

  override this.Program =
    Program.mkProgram
      (Programs.Preset.Details.init (RawPresetId this.PresetId))
      (Programs.Preset.Details.update this.Logger this.Env)
      Programs.Preset.Details.view
    |> Program.withConsoleTrace

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

[<RequireQualifiedAccess>]
module NotFound =
  let view () = div {
    comp<PageTitle> { "Not found" }

    comp<LayoutView> {
      "Layout" => typeof<Layout.Layout>

      p {
        "role" => "alert"

        "Sorry, there's nothing at this address."
      }
    }
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

      attr.fragment "NotFound" (NotFound.view ())
    }
  }