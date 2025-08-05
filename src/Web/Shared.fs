module Web.Shared

open Bolero
open Domain.Core
open Microsoft.AspNetCore.Components.Authorization
open Web.Util

[<RequireQualifiedAccess>]
type Page =
  | [<EndPoint("/")>] Home
  | [<EndPoint("/about")>] About
  | [<EndPoint("/presets")>] Presets of PageModel<Preset.List.Model>
  | [<EndPoint("/profile")>] Profile
  | [<EndPoint("/authentication/{*action}")>] Auth of action: string
  | [<EndPoint("/not-found")>] NotFound
  | [<EndPoint("**")>] Loading

let defaultModel =
  function
  | Page.Home -> ()
  | Page.About -> ()
  | Page.Presets m -> Router.definePageModel m { Presets = AsyncOp.Loading }
  | Page.NotFound -> ()
  | Page.Loading -> ()
  | Page.Auth action -> ()
  | Page.Profile -> ()

type AuthMsg = AuthChecked of AuthenticationState

type Message =
  | PageChanged of Page
  | Auth of AuthMsg
  | Preset of Preset.Message

type Model =
  { Page: Page
    AuthState: AuthenticationState option }

let router =
  Router.inferWithModel PageChanged _.Page defaultModel
  |> Router.withNotFound Page.NotFound