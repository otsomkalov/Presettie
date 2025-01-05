module Infrastructure.Telegram.Repos

open System.Text.RegularExpressions
open System.Threading.Tasks
open MongoDB.Driver
open Telegram.Bot
open otsom.fs.Bot
open Telegram.Bot.Types
open Telegram.Bot.Types.Enums
open Telegram.Bot.Types.ReplyMarkups
open Telegram.Repos
open otsom.fs.Core
open otsom.fs.Extensions
open MongoDB.Driver.Linq

let private escapeMarkdownString (str: string) =
  Regex.Replace(str, "([\(\)`\.#\-!+])", "\$1")

let sendLink (bot: ITelegramBotClient) : SendLink =
  fun userId text linkText link ->
    bot.SendTextMessageAsync(
      (userId |> UserId.value |> ChatId),
      text |> escapeMarkdownString,
      parseMode = ParseMode.MarkdownV2,
      replyMarkup =
        (InlineKeyboardButton(linkText, Url = link)
         |> Seq.singleton
         |> Seq.singleton
         |> InlineKeyboardMarkup)
    )
    &|> (_.MessageId >> BotMessageId)

type MongoChatRepo(collection: IMongoCollection<Entities.Chat>) =
  interface IChatRepo with
    member this.LoadChat(otsom.fs.Bot.ChatId chatId) = task {
      let! chat = collection.AsQueryable().FirstOrDefaultAsync(fun c -> c.Id = chatId)

      return chat.ToDomain()
    }

type MockChatRepo() =
  interface IChatRepo with
    member this.LoadChat(chatId) =
      let chat: Telegram.Core.Chat =
        { Id = chatId
          UserId = UserId(chatId.Value) }

      chat |> Task.FromResult
