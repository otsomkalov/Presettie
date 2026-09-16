module Bot.Tests.Mocks

open Bot.Core
open Tests.Shared
open otsom.fs.Bot

let botMessageId = BotMessageId 1
let chatMessageId = ChatMessageId 1
let clickId = ButtonClickId "click-id"

let chat: Chat =
  {
    Id = ChatId 1
    UserId = Mocks.userId
    Lang = "en"
  }