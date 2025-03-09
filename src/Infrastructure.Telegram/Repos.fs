module Infrastructure.Telegram.Repos

open MongoDB.Bson
open MongoDB.Driver
open otsom.fs.Bot
open Telegram.Repos
open MongoDB.Driver.Linq

type ChatRepo(collection: IMongoCollection<Entities.Chat>) =
  interface IChatRepo with
    member this.LoadChat(ChatId chatId) = task {
      let! chat = collection.AsQueryable().FirstOrDefaultAsync(fun c -> c.Id = chatId)

      return chat |> Option.ofObj |> Option.map _.ToDomain()
    }

    member this.SaveChat(chat) = task {
      let filter = Builders<Entities.Chat>.Filter.Eq(_.Id, chat.Id.Value)
      let entity = Entities.Chat.FromDomain chat

      let! _ = collection.ReplaceOneAsync(filter, entity, ReplaceOptions(IsUpsert = true))

      return ()
    }

    member this.LoadUserChat(userId) = task {
      let id = userId.Value |> ObjectId
      let! chat = collection.AsQueryable().FirstOrDefaultAsync(fun c -> c.UserId = id)

      return chat |> Option.ofObj |> Option.map _.ToDomain()
    }