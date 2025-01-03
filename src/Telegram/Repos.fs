module Telegram.Repos

open System.Threading.Tasks
open Domain.Core
open Telegram.Core
open otsom.fs.Bot
open otsom.fs.Core
open otsom.fs.Telegram.Bot.Core

[<RequireQualifiedAccess>]
module PresetRepo =
  type QueueGeneration = UserId -> PresetId -> Task<unit>

type SendLink = UserId -> string -> string -> string -> Task<BotMessageId>

type ILoadChat = abstract member LoadChat: chatId: ChatId -> Task<Chat>

type IChatRepo =
  inherit ILoadChat