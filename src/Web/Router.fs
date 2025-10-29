module Web.Router

open Bolero
open Microsoft.AspNetCore.Components.Authorization
open Web.Messages
open Web.Models
open Web.Util
open System

let defaultModel =
  function
  | Page.Home -> ()
  | Page.About -> ()
  | Page.Presets m -> Router.definePageModel m { Presets = AsyncOp.Loading }
  | Page.CreatePreset m -> Router.definePageModel m { Name = String.Empty }
  | Page.Preset(_, m) -> Router.definePageModel m { Preset = AsyncOp.Loading }
  | Page.NotFound -> ()
  | Page.Loading -> ()
  | Page.Auth action -> ()
  | Page.Profile -> ()

type Model =
  { Page: Page
    AuthState: AuthenticationState option }

let router =
  Router.inferWithModel PageChanged _.Page defaultModel
  |> Router.withNotFound Page.NotFound