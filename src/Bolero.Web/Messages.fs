module Bolero.Web.Messages

open Domain.Core
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

type Message =
  | PageChanged of Page
  | Preset of Preset.Message