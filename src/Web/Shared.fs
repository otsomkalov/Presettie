module Web.Shared

open Bolero
open Microsoft.AspNetCore.Components.Authorization

[<RequireQualifiedAccess>]
type Page =
  | [<EndPoint("/")>] Home
  | [<EndPoint("/about")>] About
  | [<EndPoint("/presets")>] Presets
  | [<EndPoint("/profile")>] Profile
  | [<EndPoint("/authentication/{*action}")>] Auth of action: string
  | [<EndPoint("/not-found")>] NotFound
  | [<EndPoint("**")>] Loading

let defaultModel =
  function
  | Page.Home -> ()
  | Page.About -> ()
  | Page.Presets -> ()
  | Page.NotFound -> ()
  | Page.Loading -> ()
  | Page.Auth action -> ()
  | Page.Profile -> ()

type AuthMsg =
  | AuthChecked of AuthenticationState

type Message =
  | PageChanged of Page
  | Auth of AuthMsg

type Model = { Page: Page; AuthState: AuthenticationState option }

let router =
  Router.inferWithModel PageChanged _.Page defaultModel
  |> Router.withNotFound Page.NotFound