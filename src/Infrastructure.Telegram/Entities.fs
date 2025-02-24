module Infrastructure.Telegram.Entities

open MongoDB.Bson
open MongoDB.Bson.Serialization.Attributes
open Telegram
open otsom.fs.Core

type Chat() =
  [<BsonElement>]
  member val Id: int64 = 0L with get, set

  [<BsonElement>]
  member val UserId: ObjectId = ObjectId.Empty with get, set

  static member FromDomain(chat: Core.Chat) =
    Chat(Id = chat.Id.Value, UserId = (chat.UserId.Value |> ObjectId))

  member this.ToDomain() : Core.Chat =
    { Id = otsom.fs.Bot.ChatId this.Id
      UserId = UserId(this.UserId.ToString()) }