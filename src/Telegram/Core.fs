﻿module Telegram.Core

open System.Threading.Tasks
open Microsoft.FSharp.Core
open otsom.fs.Auth
open otsom.fs.Bot
open otsom.fs.Core

type Page = Page of int

type Chat = { Id: ChatId; UserId: UserId }
type Click = { Id: ButtonClickId; MessageId: BotMessageId; Chat: Chat; Data: string list }

type ReplyMessage = {
  Text: string
}

type Message = {
  Id: ChatMessageId
  Chat: Chat
  Text: string
  ReplyMessage: ReplyMessage option
}

type MessageHandler = Message -> Task<unit option>

type MessageHandlerFactory = IBotService -> MessageHandler

type ClickHandler = Click -> Task<unit option>

type ClickHandlerFactory = IBotService -> ClickHandler

type UserId with
  member this.ToAccountId() = this.Value |> AccountId

type ICreateChat =
  abstract CreateChat: ChatId -> Task<Chat>

type IChatService =
  inherit ICreateChat