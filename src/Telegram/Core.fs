﻿module Telegram.Core

open System.Threading.Tasks
open Domain.Core
open Microsoft.FSharp.Core
open MusicPlatform
open otsom.fs.Bot
open otsom.fs.Core

type AnswerCallbackQuery = unit -> Task<unit>
type Page = Page of int

type ChatMessageId = ChatMessageId of int

type SendLoginMessage = UserId -> Task<BotMessageId>

type Chat = { Id: otsom.fs.Bot.ChatId; UserId: UserId }

type ClickId = ClickId of string
type Click = { Id: ClickId; ChatId: otsom.fs.Bot.ChatId; Data: string }

type ShowNotification = ClickId -> string -> Task<unit>

[<RequireQualifiedAccess>]
module Playlist =
  type Include = UserId -> Playlist.RawPlaylistId -> Task<unit>
  type Exclude = UserId -> Playlist.RawPlaylistId -> Task<unit>
  type Target = UserId -> Playlist.RawPlaylistId -> Task<unit>

[<RequireQualifiedAccess>]
module User =
  type ShowPresets = UserId -> Task<unit>
  type SendCurrentPreset = UserId -> Task<unit>
  type SendCurrentPresetSettings = UserId -> Task<unit>
  type RemovePreset = UserId -> PresetId -> Task<unit>
  type SetCurrentPreset = UserId -> PresetId -> Task<unit>
  type SetCurrentPresetSize = UserId -> PresetSettings.RawPresetSize -> Task<unit>
  type QueueCurrentPresetRun = UserId -> Task<unit>
  type RunCurrentPreset = UserId -> Task<unit>
  type CreatePreset = UserId -> string -> Task<unit>

[<RequireQualifiedAccess>]
type IncludedPlaylistActions =
  | List of presetId: PresetId * page: Page

[<RequireQualifiedAccess>]
type ExcludedPlaylistActions =
  | List of presetId: PresetId * page: Page

[<RequireQualifiedAccess>]
type TargetedPlaylistActions =
  | List of presetId: PresetId * page: Page

[<RequireQualifiedAccess>]
type UserActions =
  | SendCurrentPresetSettings of userId: UserId
  | QueueCurrentPresetGeneration of userId: UserId
  | GenerateCurrentPreset of presetId: PresetId

[<RequireQualifiedAccess>]
type PresetActions =
  | Run of presetId: PresetId

[<RequireQualifiedAccess>]
type Action =

  | IncludedPlaylist of IncludedPlaylistActions
  | ExcludedPlaylist of ExcludedPlaylistActions
  | TargetedPlaylist of TargetedPlaylistActions
  | Preset of PresetActions
  | User of UserActions

  | SetCurrentPreset of presetId: PresetId
  | RemovePreset of presetId: PresetId

  | AskForPresetSize

type ParseAction = string -> Action

type AuthState =
  | Authorized
  | Unauthorized

[<RequireQualifiedAccess>]
module IncludedPlaylist =
  type List = PresetId -> Page -> Task<unit>
  type Show = PresetId -> ReadablePlaylistId -> Task<unit>

[<RequireQualifiedAccess>]
module ExcludedPlaylist =
  type List = PresetId -> Page -> Task<unit>
  type Show = PresetId -> ReadablePlaylistId -> Task<unit>

[<RequireQualifiedAccess>]
module TargetedPlaylist =
  type List = PresetId -> Page -> Task<unit>
  type Show = PresetId -> WritablePlaylistId -> Task<unit>

[<RequireQualifiedAccess>]
module Preset =
  type Show = PresetId -> Task<unit>
  type Run = PresetId -> Task<unit>

type ReplyMessage = {
  Text: string
}

type Message = {
  Id: otsom.fs.Bot.ChatMessageId
  ChatId: ChatId
  Text: string
  ReplyMessage: ReplyMessage option
}

type MessageHandler = Message -> Task<unit option>

type MessageHandlerFactory = IChatContext -> MessageHandler

type ClickHandler = Click -> Task<unit option>

type ClickHandlerFactory = IBotMessageContext -> ClickHandler