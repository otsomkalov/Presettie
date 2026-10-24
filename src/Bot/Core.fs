module Bot.Core

open Domain.Core
open Microsoft.FSharp.Core
open otsom.fs.Bot
open otsom.fs.OAuth

type Page = Page of int

type Chat =
  {
    Id: ChatId
    UserId: UserId
    Lang: string
  }

  interface IChat with
    member this.Id = this.Id

type ReplyMessage = { Text: string }

type Message =
  {
    Id: ChatMessageId
    Text: string
    ReplyMessage: ReplyMessage option
  }

  interface IMessage with
    member this.Id = this.Id

type UserId with
  member this.ToOAuthAccountId() = this.Value |> string |> AccountId

  member this.ToAuthAccountId() =
    this.Value |> string |> otsom.fs.Auth.AccountId

type UpdateData =
  | Msg of Message
  | Click of Click

type Update =
  {
    ChatId: ChatId
    Lang: string option
    Data: UpdateData
  }

type ChatId with
  member this.ToOAuthAccountId() = this.Value |> string |> AccountId