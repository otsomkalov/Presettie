module Bot.Tests.Message

open Bot.Constants
open Bot.Core
open Bot.Handlers
open Bot.Resources
open Domain.Tests
open FsUnit.Xunit
open Moq
open Xunit
open otsom.fs.Bot
open otsom.fs.Resources
open Domain.Repos
open otsom.fs.Auth
open Bot.Tests

let private createMessage (text: string) : Message =
  { Id = Mocks.chatMessageId
    Text = text
    Chat = Mocks.chat
    ReplyMessage = None }

let private createMessageWithReply (text: string) (replyText: string) : Message =
  { Id = Mocks.chatMessageId
    Text = text
    Chat = Mocks.chat
    ReplyMessage = Some { Text = replyText } }

type StartMessageHandler() =
  let userRepo = Mock<ILoadUser>()
  let presetRepo = Mock<ILoadPreset>()
  let authService = Mock<ICompleteAuth>()
  let resourceProvider = Mock<IResourceProvider>()
  let chatCtx = Mock<IBotService>()

  let handler =
    Message.startMessageHandler
      userRepo.Object
      presetRepo.Object
      authService.Object
      resourceProvider.Object
      chatCtx.Object

  [<Fact>]
  member _.``should handle /start command and send current preset``() =
    chatCtx.Setup(_.SendKeyboard(It.IsAny(), It.IsAny())).ReturnsAsync(Mocks.botMessageId)
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)
    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    let message = createMessage Commands.start

    task {
      let! result = handler message

      result |> should equal (Some())

      userRepo.VerifyAll()
      presetRepo.VerifyAll()
      chatCtx.VerifyAll()
    }

  [<Fact>]
  member _.``should return Some when processing /start command``() =
    chatCtx.Setup(_.SendKeyboard(It.IsAny(), It.IsAny())).ReturnsAsync(Mocks.botMessageId)
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)
    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    let message = createMessage Commands.start

    task {
      let! result = handler message

      result |> should equal (Some())
    }

  [<Fact>]
  member _.``should handle /start command with state and complete auth successfully``() =
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)
    chatCtx.Setup(_.SendMessage(It.IsAny())).ReturnsAsync(Mocks.botMessageId)
    authService.Setup(_.CompleteAuth(It.IsAny<AccountId>(), It.IsAny<State>())).ReturnsAsync(Ok())

    let testState = "test-state"
    let message = createMessage $"{Commands.start} {testState}"

    task {
      let! result = handler message

      result |> should equal (Some())

      userRepo.VerifyAll()
      chatCtx.VerifyAll()
      authService.VerifyAll()
    }

  [<Fact>]
  member _.``should handle /start command with invalid state and send StateNotFound error``() =
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)
    chatCtx.Setup(_.SendMessage(It.IsAny())).ReturnsAsync(Mocks.botMessageId)
    authService.Setup(_.CompleteAuth(It.IsAny<AccountId>(), It.IsAny<State>())).ReturnsAsync(Error CompleteError.StateNotFound)

    let testState = "invalid-state"
    let message = createMessage $"{Commands.start} {testState}"

    task {
      let! result = handler message

      result |> should equal (Some())

      userRepo.VerifyAll()
      chatCtx.VerifyAll()
      authService.VerifyAll()
    }

  [<Fact>]
  member _.``should handle /start command with state that doesn't belong to user``() =
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)
    chatCtx.Setup(_.SendMessage(It.IsAny())).ReturnsAsync(Mocks.botMessageId)

    authService
      .Setup(_.CompleteAuth(It.IsAny<AccountId>(), It.IsAny<State>()))
      .ReturnsAsync(Error CompleteError.StateDoesntBelongToUser)

    let testState = "other-user-state"
    let message = createMessage $"{Commands.start} {testState}"

    task {
      let! result = handler message

      result |> should equal (Some())

      userRepo.VerifyAll()
      chatCtx.VerifyAll()
      authService.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for unmatched message``() =
    let message = createMessage "some random text"

    task {
      let! result = handler message

      result |> should equal None

      userRepo.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
      authService.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
    }

type FaqMessageHandler() =
  let resourceProvider = Mock<IResourceProvider>()
  let chatCtx = Mock<ISendMessage>()

  let handler =
    Message.faqMessageHandler resourceProvider.Object chatCtx.Object

  [<Fact>]
  member _.``should handle /faq command and send FAQ message``() =
    resourceProvider.Setup(_.Item(Messages.FAQ)).Returns("FAQ content")
    chatCtx.Setup(_.SendMessage("FAQ content")).ReturnsAsync(Mocks.botMessageId)

    let message = createMessage Commands.faq

    task {
      let! result = handler message

      result |> should equal (Some())

      resourceProvider.VerifyAll()
      chatCtx.VerifyAll()
    }

  [<Fact>]
  member _.``should return Some when processing /faq command``() =
    resourceProvider.Setup(_.Item(Messages.FAQ)).Returns("FAQ content")
    chatCtx.Setup(_.SendMessage(It.IsAny<string>())).ReturnsAsync(Mocks.botMessageId)

    let message = createMessage Commands.faq

    task {
      let! result = handler message

      result |> should equal (Some())
    }

  [<Fact>]
  member _.``should return None for non-faq message``() =
    let message = createMessage "some random text"

    task {
      let! result = handler message

      result |> should equal None

      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
    }

  [<Fact>]
  member _.``should return None for empty message``() =
    let message = createMessage ""

    task {
      let! result = handler message

      result |> should equal None

      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
    }

  [<Fact>]
  member _.``should return None when /faq has extra data``() =
    let message = createMessage $"{Commands.faq} extra data"

    task {
      let! result = handler message

      result |> should equal None

      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
    }

type PrivacyMessageHandler() =
  let resourceProvider = Mock<IResourceProvider>()
  let chatCtx = Mock<ISendMessage>()

  let handler =
    Message.privacyMessageHandler resourceProvider.Object chatCtx.Object

  [<Fact>]
  member _.``should handle /privacy command and send privacy message``() =
    resourceProvider.Setup(_.Item(Messages.Privacy)).Returns("Privacy content")
    chatCtx.Setup(_.SendMessage("Privacy content")).ReturnsAsync(Mocks.botMessageId)

    let message = createMessage Commands.privacy

    task {
      let! result = handler message

      result |> should equal (Some())

      resourceProvider.VerifyAll()
      chatCtx.VerifyAll()
    }

  [<Fact>]
  member _.``should return Some when processing /privacy command``() =
    resourceProvider.Setup(_.Item(Messages.Privacy)).Returns("Privacy content")
    chatCtx.Setup(_.SendMessage(It.IsAny<string>())).ReturnsAsync(Mocks.botMessageId)

    let message = createMessage Commands.privacy

    task {
      let! result = handler message

      result |> should equal (Some())
    }

  [<Fact>]
  member _.``should return None for non-privacy message``() =
    let message = createMessage "some random text"

    task {
      let! result = handler message

      result |> should equal None

      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
    }

  [<Fact>]
  member _.``should return None for empty message``() =
    let message = createMessage ""

    task {
      let! result = handler message

      result |> should equal None

      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
    }

  [<Fact>]
  member _.``should return None when /privacy has extra data``() =
    let message = createMessage $"{Commands.privacy} extra data"

    task {
      let! result = handler message

      result |> should equal None

      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
    }

type GuideMessageHandler() =
  let resourceProvider = Mock<IResourceProvider>()
  let chatCtx = Mock<ISendMessage>()

  let handler =
    Message.guideMessageHandler resourceProvider.Object chatCtx.Object

  [<Fact>]
  member _.``should handle /guide command and send guide message``() =
    resourceProvider.Setup(_.Item(Messages.Guide)).Returns("Guide content")
    chatCtx.Setup(_.SendMessage("Guide content")).ReturnsAsync(Mocks.botMessageId)

    let message = createMessage Commands.guide

    task {
      let! result = handler message

      result |> should equal (Some())

      resourceProvider.VerifyAll()
      chatCtx.VerifyAll()
    }

  [<Fact>]
  member _.``should return Some when processing /guide command``() =
    resourceProvider.Setup(_.Item(Messages.Guide)).Returns("Guide content")
    chatCtx.Setup(_.SendMessage(It.IsAny<string>())).ReturnsAsync(Mocks.botMessageId)

    let message = createMessage Commands.guide

    task {
      let! result = handler message

      result |> should equal (Some())
    }

  [<Fact>]
  member _.``should return None for non-guide message``() =
    let message = createMessage "some random text"

    task {
      let! result = handler message

      result |> should equal None

      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
    }

  [<Fact>]
  member _.``should return None for empty message``() =
    let message = createMessage ""

    task {
      let! result = handler message

      result |> should equal None

      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
    }

  [<Fact>]
  member _.``should return None when /guide has extra data``() =
    let message = createMessage $"{Commands.guide} extra data"

    task {
      let! result = handler message

      result |> should equal None

      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
    }

type HelpMessageHandler() =
  let resourceProvider = Mock<IResourceProvider>()
  let chatCtx = Mock<ISendMessage>()

  let handler =
    Message.helpMessageHandler resourceProvider.Object chatCtx.Object

  [<Fact>]
  member _.``should handle /help command and send help message``() =
    resourceProvider.Setup(_.Item(Messages.Help)).Returns("Help content")
    chatCtx.Setup(_.SendMessage("Help content")).ReturnsAsync(Mocks.botMessageId)

    let message = createMessage Commands.help

    task {
      let! result = handler message

      result |> should equal (Some())

      resourceProvider.VerifyAll()
      chatCtx.VerifyAll()
    }

  [<Fact>]
  member _.``should return Some when processing /help command``() =
    resourceProvider.Setup(_.Item(Messages.Help)).Returns("Help content")
    chatCtx.Setup(_.SendMessage(It.IsAny<string>())).ReturnsAsync(Mocks.botMessageId)

    let message = createMessage Commands.help

    task {
      let! result = handler message

      result |> should equal (Some())
    }

  [<Fact>]
  member _.``should return None for non-help message``() =
    let message = createMessage "some random text"

    task {
      let! result = handler message

      result |> should equal None

      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
    }

  [<Fact>]
  member _.``should return None for empty message``() =
    let message = createMessage ""

    task {
      let! result = handler message

      result |> should equal None

      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
    }

  [<Fact>]
  member _.``should return None when /help has extra data``() =
    let message = createMessage $"{Commands.help} extra data"

    task {
      let! result = handler message

      result |> should equal None

      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
    }

type MyPresetsMessageHandler() =
  let presetRepo = Mock<IListUserPresets>()
  let resourceProvider = Mock<IResourceProvider>()
  let chatCtx = Mock<ISendMessageButtons>()

  do resourceProvider.Setup(_.Item(Buttons.MyPresets)).Returns(Buttons.MyPresets) |> ignore

  let handler =
    Message.myPresetsMessageHandler presetRepo.Object resourceProvider.Object chatCtx.Object

  [<Fact>]
  member _.``should handle /presets command and send user presets``() =
    presetRepo.Setup(_.ListUserPresets(Mocks.userId)).ReturnsAsync([ Mocks.simplePreset ])

    let message = createMessage Commands.presets

    task {
      let! result = handler message

      result |> should equal (Some())
    }

  [<Fact>]
  member _.``should return Some when processing /presets command``() =
    presetRepo.Setup(_.ListUserPresets(Mocks.userId)).ReturnsAsync([ Mocks.simplePreset ])

    let message = createMessage Commands.presets

    task {
      let! result = handler message

      result |> should equal (Some())
    }

  [<Fact>]
  member _.``should handle My Presets button click and send user presets``() =
    presetRepo.Setup(_.ListUserPresets(Mocks.userId)).ReturnsAsync([ Mocks.simplePreset ])

    let message = createMessage Buttons.MyPresets

    task {
      let! result = handler message

      result |> should equal (Some())
    }

  [<Fact>]
  member _.``should return None for non-presets message``() =

    let message = createMessage "some random text"

    task {
      let! result = handler message

      result |> should equal None

      resourceProvider.Verify(_.Item(Buttons.MyPresets))
      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
    }

  [<Fact>]
  member _.``should return None for empty message``() =
    let message = createMessage ""

    task {
      let! result = handler message

      result |> should equal None

      resourceProvider.Verify(_.Item(Buttons.MyPresets))
      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
    }

  [<Fact>]
  member _.``should return None when /presets has extra data``() =
    let message = createMessage $"{Commands.presets} extra data"

    task {
      let! result = handler message

      result |> should equal None

      resourceProvider.Verify(_.Item(Buttons.MyPresets))
      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
    }