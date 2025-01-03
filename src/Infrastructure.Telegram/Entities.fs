module Infrastructure.Telegram.Entities

open MongoDB.Bson.Serialization.Attributes
open Telegram
open otsom.fs.Core

type Chat() =
  [<BsonElement>]
  member val Id: int64 = 0L with get, set

  [<BsonElement>]
  member val UserId: string = null with get, set

  member this.FromDomain(chat: Core.Chat) =
    Chat(Id = chat.Id.Value, UserId = (chat.UserId |> UserId.value |> string))

  member this.ToDomain() : Core.Chat =
    { Id = otsom.fs.Bot.ChatId this.Id
      UserId = UserId(int64 this.UserId) }