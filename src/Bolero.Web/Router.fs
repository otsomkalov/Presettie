module Bolero.Web.Router

open Bolero
open Microsoft.AspNetCore.Components.Authorization
open Bolero.Web.Messages
open Bolero.Web.Models
open Bolero.Web.Util
open System

let defaultModel =
  function
  | Page.Presets m -> Router.definePageModel m { Presets = AsyncOp.Loading }
  | Page.CreatePreset m -> Router.definePageModel m { Name = String.Empty }
  | Page.Preset(_, m) -> Router.definePageModel m { Preset = AsyncOp.Loading }

type Model = { Page: Page }

let router = Router.inferWithModel PageChanged _.Page defaultModel