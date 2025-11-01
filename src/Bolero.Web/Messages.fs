module Bolero.Web.Messages

open Domain.Core
open Microsoft.AspNetCore.Components.Authorization
open Bolero.Web.Models

[<RequireQualifiedAccess>]
module Preset =
  [<RequireQualifiedAccess>]
  module Remove' =
    [<RequireQualifiedAccess>]
    type Message =
      | RemovePreset of PresetId
      | PresetRemoved of PresetId

  [<RequireQualifiedAccess>]
  module Details' =
    [<RequireQualifiedAccess>]
    type Message = PresetLoaded of Preset

  [<RequireQualifiedAccess>]
  module List' =
    [<RequireQualifiedAccess>]
    type Message = PresetsLoaded of SimplePreset list

  [<RequireQualifiedAccess>]
  module Create' =
    [<RequireQualifiedAccess>]
    type Message =
      | CreatePreset
      | NameChanged of string
      | CreatePresetError of exn

  [<RequireQualifiedAccess>]
  type Message =
    | List of List'.Message
    | Details of Details'.Message
    | Remove of Remove'.Message
    | Create of Create'.Message

type AuthMsg = AuthChecked of AuthenticationState

type Message =
  | PageChanged of Page
  | Auth of AuthMsg
  | Preset of Preset.Message