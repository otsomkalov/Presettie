module Web.Models

open Bolero
open Domain.Core
open Web.Util

[<RequireQualifiedAccess>]
module Preset =
  [<RequireQualifiedAccess>]
  module List =
    type Model = { Presets: AsyncOp<SimplePreset list> }

  [<RequireQualifiedAccess>]
  module Details =
    type Model = { Preset: AsyncOp<Preset> }

  [<RequireQualifiedAccess>]
  module Create =
    type Model = { Name: string }

[<RequireQualifiedAccess>]
type Page =
  | [<EndPoint("/")>] Home
  | [<EndPoint("/about")>] About
  | [<EndPoint("/presets")>] Presets of PageModel<Preset.List.Model>
  | [<EndPoint("/presets/create")>] CreatePreset of PageModel<Preset.Create.Model>
  | [<EndPoint("/presets/{id}")>] Preset of id: string * PageModel<Preset.Details.Model>
  | [<EndPoint("/profile")>] Profile
  | [<EndPoint("/authentication/{*action}")>] Auth of action: string
  | [<EndPoint("/not-found")>] NotFound
  | [<EndPoint("**")>] Loading