module Bot.Core

open System.Threading.Tasks
open Domain.Core
open Microsoft.FSharp.Core
open otsom.fs.Auth
open otsom.fs.Bot
open otsom.fs.Resources

type Page = Page of int

type Chat =
  { Id: ChatId
    UserId: UserId
    Lang: string }

  interface IChat with
    member this.Id = this.Id

type ReplyMessage = { Text: string }

type Message =
  { Id: ChatMessageId
    Text: string
    ReplyMessage: ReplyMessage option }

  interface IMessage with
    member this.Id = this.Id
    member this.Text = this.Text

type UserId with
  member this.ToAccountId() = this.Value |> string |> AccountId

type ICreateChat =
  abstract CreateChat: ChatId * string option -> Task<Chat>

type IChatService =
  inherit ICreateChat

[<RequireQualifiedAccess>]
module Resources =
  type GetResourceProvider = string option -> Task<IResourceProvider>