module Bot.Repos

open System.Threading.Tasks
open Domain.Core
open Bot.Core
open otsom.fs.Bot
open otsom.fs.Core

[<RequireQualifiedAccess>]
module PresetRepo =
  type QueueGeneration = UserId -> PresetId -> Task<unit>

type ILoadChat =
  abstract LoadChat: ChatId -> Task<Chat option>

type ILoadUserChat =
  abstract LoadUserChat: UserId -> Task<Chat option>

type ISaveChat =
  abstract SaveChat: Chat -> Task<unit>

type ICreateChat =
  abstract CreateChat: ChatId -> Task<Chat>

type IChatRepo =
  inherit ILoadChat
  inherit ISaveChat
  inherit ILoadUserChat