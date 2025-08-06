module Web.Messages

open Domain.Core
open Microsoft.AspNetCore.Components.Authorization
open Web.Models

[<RequireQualifiedAccess>]
module Preset =
  [<RequireQualifiedAccess>]
  module Details' =
    [<RequireQualifiedAccess>]
    type Message = PresetLoaded of Preset

  [<RequireQualifiedAccess>]
  module List' =
    [<RequireQualifiedAccess>]
    type Message = PresetsLoaded of SimplePreset list

  [<RequireQualifiedAccess>]
  type Message =
    | List of List'.Message
    | Details of Details'.Message

type AuthMsg = AuthChecked of AuthenticationState

type Message =
  | PageChanged of Page
  | Auth of AuthMsg
  | Preset of Preset.Message