module Bot.Telegram.Entities

open System
open Domain.Core
open MongoDB.Bson
open MongoDB.Bson.Serialization.Attributes
open Bot

type Chat() =
  [<BsonElement>]
  member val Id: int64 = 0L with get, set

  [<BsonElement; BsonGuidRepresentation(GuidRepresentation.Standard)>]
  member val UserId: Guid = Guid.Empty with get, set

  [<BsonElement>]
  member val Lang: string = "" with get, set

  static member FromDomain(chat: Core.Chat) =
    Chat(Id = chat.Id.Value, UserId = (chat.UserId.Value))

  member this.ToDomain() : Core.Chat =
    { Id = otsom.fs.Bot.ChatId this.Id
      UserId = UserId(this.UserId)
      Lang = this.Lang }