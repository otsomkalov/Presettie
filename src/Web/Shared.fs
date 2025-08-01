module Web.Shared

open Bolero

[<RequireQualifiedAccess>]
type Page =
  | [<EndPoint("/")>] Home
  | [<EndPoint("/not-found")>] NotFound
  | [<EndPoint("/presets")>] Presets
  | [<EndPoint("**")>] Loading

let defaultModel =
  function
  | Page.Home -> ()
  | Page.Presets -> ()
  | Page.NotFound -> ()
  | Page.Loading -> ()

type Message = PageChanged of Page

type Model = { Page: Page }

let router =
  Router.inferWithModel PageChanged _.Page defaultModel
  |> Router.withNotFound Page.NotFound