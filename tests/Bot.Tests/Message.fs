module Bot.Tests.Message

open Bot.Constants
open Bot.Core
open Bot.Handlers
open Bot.Resources
open MusicPlatform
open Domain.Core
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
    Message.startMessageHandler userRepo.Object presetRepo.Object authService.Object resourceProvider.Object chatCtx.Object

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

    authService.Setup(_.CompleteAuth(It.IsAny<AccountId>(), It.IsAny<State>())).ReturnsAsync(Error CompleteError.StateDoesntBelongToUser)

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
  let chatCtx = Mock<IBotService>()

  let handler = Message.faqMessageHandler resourceProvider.Object chatCtx.Object

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
  let chatCtx = Mock<IBotService>()

  let handler = Message.privacyMessageHandler resourceProvider.Object chatCtx.Object

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
  let chatCtx = Mock<IBotService>()

  let handler = Message.guideMessageHandler resourceProvider.Object chatCtx.Object

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
  let chatCtx = Mock<IBotService>()

  let handler = Message.helpMessageHandler resourceProvider.Object chatCtx.Object

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

  do
    resourceProvider.Setup(_.Item(Buttons.MyPresets)).Returns(Buttons.MyPresets)
    |> ignore

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

type BackMessageButtonHandler() =
  let userRepo = Mock<ILoadUser>()
  let presetRepo = Mock<ILoadPreset>()
  let resourceProvider = Mock<IResourceProvider>()
  let chatCtx = Mock<ISendKeyboard>()

  do resourceProvider.Setup(_.Item(Buttons.Back)).Returns(Buttons.Back) |> ignore

  let handler =
    Message.backMessageButtonHandler userRepo.Object presetRepo.Object resourceProvider.Object chatCtx.Object

  [<Fact>]
  member _.``should handle Back button and send current preset``() =
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)
    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)
    chatCtx.Setup(_.SendKeyboard(It.IsAny(), It.IsAny())).ReturnsAsync(Mocks.botMessageId)

    let message = createMessage Buttons.Back

    task {
      let! result = handler message

      result |> should equal (Some())

      userRepo.VerifyAll()
      presetRepo.VerifyAll()
      chatCtx.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for non-back message``() =
    let message = createMessage "some random text"

    task {
      let! result = handler message

      result |> should equal None

      resourceProvider.Verify(_.Item(Buttons.Back))
      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
      userRepo.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
    }

  [<Fact>]
  member _.``should return None for empty message``() =
    let message = createMessage ""

    task {
      let! result = handler message

      result |> should equal None

      resourceProvider.Verify(_.Item(Buttons.Back))
      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
      userRepo.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
    }

  [<Fact>]
  member _.``should return None when Back button has extra data``() =
    let message = createMessage $"{Buttons.Back} extra data"

    task {
      let! result = handler message

      result |> should equal None

      resourceProvider.Verify(_.Item(Buttons.Back))
      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
      userRepo.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
    }


type PresetSettingsMessageHandler() =
  let userRepo = Mock<ILoadUser>()
  let presetRepo = Mock<IPresetRepo>()
  let resourceProvider = Mock<IResourceProvider>()
  let chatCtx = Mock<IBotService>()

  do
    resourceProvider.Setup(_.Item(Buttons.Settings)).Returns(Buttons.Settings)
    |> ignore

  let handler =
    Message.presetSettingsMessageHandler userRepo.Object presetRepo.Object resourceProvider.Object chatCtx.Object

  [<Fact>]
  member _.``should handle Settings button and send preset settings``() =
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)
    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)
    chatCtx.Setup(_.SendKeyboard(It.IsAny(), It.IsAny())).ReturnsAsync(Mocks.botMessageId)

    let message = createMessage Buttons.Settings

    task {
      let! result = handler message

      result |> should equal (Some())

      userRepo.VerifyAll()
      presetRepo.VerifyAll()
      chatCtx.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for non-settings message``() =
    let message = createMessage "some random text"

    task {
      let! result = handler message

      result |> should equal None

      resourceProvider.Verify(_.Item(Buttons.Settings))
      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
      userRepo.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
    }

  [<Fact>]
  member _.``should return None for empty message``() =
    let message = createMessage ""

    task {
      let! result = handler message

      result |> should equal None

      resourceProvider.Verify(_.Item(Buttons.Settings))
      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
      userRepo.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
    }

  [<Fact>]
  member _.``should return None when Settings button has extra data``() =
    let message = createMessage $"{Buttons.Settings} extra data"

    task {
      let! result = handler message

      result |> should equal None

      resourceProvider.Verify(_.Item(Buttons.Settings))
      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
      userRepo.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
    }

type SetPresetSizeMessageButtonHandler() =
  let resourceProvider = Mock<IResourceProvider>()
  let chatCtx = Mock<IBotService>()

  do
    resourceProvider.Setup(_.Item(Buttons.SetPresetSize)).Returns(Buttons.SetPresetSize)
    |> ignore

  do chatCtx.Setup(_.AskForReply(It.IsAny())).ReturnsAsync(()) |> ignore

  let handler =
    Message.setPresetSizeMessageButtonHandler resourceProvider.Object chatCtx.Object

  [<Fact>]
  member _.``should handle SetPresetSize button and ask for reply``() =
    let message = createMessage Buttons.SetPresetSize

    task {
      let! result = handler message
      result |> should equal (Some())
      resourceProvider.Verify(_.Item(Buttons.SetPresetSize))
      chatCtx.Verify(_.AskForReply(It.IsAny()))
    }

  [<Fact>]
  member _.``should return None for non-setPresetSize message``() =
    let message = createMessage "some random text"

    task {
      let! result = handler message
      result |> should equal None
      resourceProvider.Verify(_.Item(Buttons.SetPresetSize))
      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
    }

  [<Fact>]
  member _.``should return None for empty message``() =
    let message = createMessage ""

    task {
      let! result = handler message
      result |> should equal None
      resourceProvider.Verify(_.Item(Buttons.SetPresetSize))
      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
    }

  [<Fact>]
  member _.``should return None when SetPresetSize button has extra data``() =
    let message = createMessage $"{Buttons.SetPresetSize} extra data"

    task {
      let! result = handler message
      result |> should equal None
      resourceProvider.Verify(_.Item(Buttons.SetPresetSize))
      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
    }

type SetPresetSizeMessageHandler() =
  let userService = Mock<IUserService>()
  let userRepo = Mock<ILoadUser>()
  let presetRepo = Mock<IPresetRepo>()
  let resourceProvider = Mock<IResourceProvider>()
  let chatCtx = Mock<IBotService>()

  do
    resourceProvider.Setup(_.Item(Buttons.SetPresetSize)).Returns(Buttons.SetPresetSize)
    |> ignore

  do
    resourceProvider.Setup(_.Item(Messages.SendPresetSize)).Returns("Send preset size")
    |> ignore

  do
    resourceProvider.Setup(_.Item(Messages.PresetSizeTooSmall)).Returns("Too small")
    |> ignore

  do
    resourceProvider.Setup(_.Item(Messages.PresetSizeTooBig)).Returns("Too big")
    |> ignore

  do
    resourceProvider.Setup(_.Item(Messages.PresetSizeNotANumber)).Returns("Not a number")
    |> ignore

  let handler =
    Message.setPresetSizeMessageHandler userService.Object userRepo.Object presetRepo.Object resourceProvider.Object chatCtx.Object

  [<Fact>]
  member _.``should handle reply with valid size and call onSuccess``() =
    userService.Setup(_.SetCurrentPresetSize(Mocks.userId, It.IsAny())).ReturnsAsync(Ok())
    chatCtx.Setup(_.SendKeyboard(It.IsAny(), It.IsAny())).ReturnsAsync(Mocks.botMessageId)
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)
    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    let message =
      { createMessage "42" with
          ReplyMessage = Some { Text = resourceProvider.Object.Item(Buttons.SetPresetSize) } }

    task {
      let! result = handler message
      result |> should equal (Some())
      userService.VerifyAll()
      chatCtx.VerifyAll()
      userRepo.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``should handle /size command and call onSuccess``() =
    userService.Setup(_.SetCurrentPresetSize(Mocks.userId, It.IsAny())).ReturnsAsync(Ok())
    chatCtx.Setup(_.SendKeyboard(It.IsAny(), It.IsAny())).ReturnsAsync(Mocks.botMessageId)
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)
    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    let message = createMessage $"{Commands.size} 42"

    task {
      let! result = handler message
      result |> should equal (Some())
      userService.VerifyAll()
      chatCtx.VerifyAll()
      userRepo.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``should handle reply with too small size and call onError``() =
    userService.Setup(_.SetCurrentPresetSize(Mocks.userId, It.IsAny())).ReturnsAsync(Error PresetSettings.ParsingError.TooSmall)
    chatCtx.Setup(_.SendMessage(resourceProvider.Object.Item(Messages.PresetSizeTooSmall))).ReturnsAsync(Mocks.botMessageId)

    let message =
      { createMessage "1" with
          ReplyMessage = Some { Text = resourceProvider.Object.Item(Buttons.SetPresetSize) } }

    task {
      let! result = handler message
      result |> should equal (Some())
      userService.VerifyAll()
      chatCtx.VerifyAll()
    }

  [<Fact>]
  member _.``should handle reply with too big size and call onError``() =
    userService.Setup(_.SetCurrentPresetSize(Mocks.userId, It.IsAny())).ReturnsAsync(Error PresetSettings.ParsingError.TooBig)
    chatCtx.Setup(_.SendMessage(resourceProvider.Object.Item(Messages.PresetSizeTooBig))).ReturnsAsync(Mocks.botMessageId)

    let message =
      { createMessage "1000" with
          ReplyMessage = Some { Text = resourceProvider.Object.Item(Buttons.SetPresetSize) } }

    task {
      let! result = handler message
      result |> should equal (Some())
      userService.VerifyAll()
      chatCtx.VerifyAll()
    }

  [<Fact>]
  member _.``should handle reply with not a number and call onError``() =
    userService.Setup(_.SetCurrentPresetSize(Mocks.userId, It.IsAny())).ReturnsAsync(Error PresetSettings.ParsingError.NotANumber)
    chatCtx.Setup(_.SendMessage(resourceProvider.Object.Item(Messages.PresetSizeNotANumber))).ReturnsAsync(Mocks.botMessageId)

    let message =
      { createMessage "abc" with
          ReplyMessage = Some { Text = resourceProvider.Object.Item(Buttons.SetPresetSize) } }

    task {
      let! result = handler message
      result |> should equal (Some())
      userService.VerifyAll()
      chatCtx.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for unmatched message``() =
    let message = createMessage "some random text"

    task {
      let! result = handler message
      result |> should equal None
      userService.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
      userRepo.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
    }

type CreatePresetButtonMessageHandler() =
  let resourceProvider = Mock<IResourceProvider>()
  let chatCtx = Mock<IBotService>()

  do
    resourceProvider.Setup(_.Item(Buttons.CreatePreset)).Returns(Buttons.CreatePreset)
    |> ignore

  do
    resourceProvider.Setup(_.Item(Messages.SendPresetName)).Returns("Send preset name")
    |> ignore

  do chatCtx.Setup(_.AskForReply("Send preset name")).ReturnsAsync(()) |> ignore

  let handler =
    Message.createPresetButtonMessageHandler resourceProvider.Object chatCtx.Object

  [<Fact>]
  member _.``should handle CreatePreset button and ask for reply``() =
    let message = createMessage Buttons.CreatePreset

    task {
      let! result = handler message
      result |> should equal (Some())
      resourceProvider.Verify(_.Item(Buttons.CreatePreset))
      chatCtx.Verify(_.AskForReply("Send preset name"))
    }

  [<Fact>]
  member _.``should return None for non-createPreset message``() =
    let message = createMessage "some random text"

    task {
      let! result = handler message
      result |> should equal None
      resourceProvider.Verify(_.Item(Buttons.CreatePreset))
      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
    }

  [<Fact>]
  member _.``should return None for empty message``() =
    let message = createMessage ""

    task {
      let! result = handler message
      result |> should equal None
      resourceProvider.Verify(_.Item(Buttons.CreatePreset))
      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
    }

  [<Fact>]
  member _.``should return None when CreatePreset button has extra data``() =
    let message = createMessage $"{Buttons.CreatePreset} extra data"

    task {
      let! result = handler message
      result |> should equal None
      resourceProvider.Verify(_.Item(Buttons.CreatePreset))
      resourceProvider.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
    }

type CreatePresetMessageHandler() =
  let presetService = Mock<IPresetService>()
  let resourceProvider = Mock<IResourceProvider>()
  let chatCtx = Mock<IBotService>()

  do
    resourceProvider.Setup(_.Item(Messages.SendPresetName)).Returns("Send preset name")
    |> ignore

  let handler =
    Message.createPresetMessageHandler presetService.Object resourceProvider.Object chatCtx.Object

  [<Fact>]
  member _.``should handle reply with preset name and create preset``() =
    presetService.Setup(_.CreatePreset(Mocks.userId, "MyPreset")).ReturnsAsync(Mocks.preset)
    chatCtx.Setup(_.SendKeyboard(It.IsAny(), It.IsAny())).ReturnsAsync(Mocks.botMessageId)

    let message =
      { createMessage "MyPreset" with
          ReplyMessage = Some { Text = resourceProvider.Object.Item(Messages.SendPresetName) } }

    task {
      let! result = handler message

      result |> should equal (Some())

      presetService.VerifyAll()
      chatCtx.VerifyAll()
    }

  [<Fact>]
  member _.``should handle /newpreset command and create preset``() =
    presetService.Setup(_.CreatePreset(Mocks.userId, "Another")).ReturnsAsync(Mocks.preset)
    chatCtx.Setup(_.SendKeyboard(It.IsAny(), It.IsAny())).ReturnsAsync(Mocks.botMessageId)

    let message = createMessage $"{Commands.newPreset} Another"

    task {
      let! result = handler message

      result |> should equal (Some())

      presetService.VerifyAll()
      chatCtx.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for unmatched message``() =
    let message = createMessage "some random text"

    task {
      let! result = handler message

      result |> should equal None

      presetService.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
    }

type IncludePlaylistButtonMessageHandler() =
  let musicPlatform = Mock<IMusicPlatform>()
  let musicPlatformFactory = Mock<IMusicPlatformFactory>()

  let authService = Mock<IInitAuth>()
  let resourceProvider = Mock<IResourceProvider>()
  let chatCtx = Mock<IBotService>()

  do
    resourceProvider.Setup(_.Item(Buttons.IncludePlaylist)).Returns(Buttons.IncludePlaylist)
    |> ignore

  do
    resourceProvider.Setup(_.Item(Messages.SendIncludedPlaylist)).Returns("Send included playlist")
    |> ignore

  let handler =
    Message.includePlaylistButtonMessageHandler musicPlatformFactory.Object authService.Object resourceProvider.Object chatCtx.Object

  [<Fact>]
  member _.``should ask for included playlist when platform present``() =
    musicPlatformFactory.Setup(_.GetMusicPlatform(Mocks.userId.ToMusicPlatformId())).ReturnsAsync(Some musicPlatform.Object)
    |> ignore

    chatCtx.Setup(_.AskForReply(It.IsAny())).ReturnsAsync(()) |> ignore

    let message = createMessage Buttons.IncludePlaylist

    task {
      let! result = handler message

      result |> should equal (Some())

      chatCtx.Verify(_.AskForReply(resourceProvider.Object.Item(Messages.SendIncludedPlaylist)))
    }

  [<Fact>]
  member _.``should send login when platform missing``() =
    musicPlatformFactory.Setup(_.GetMusicPlatform(Mocks.userId.ToMusicPlatformId())).ReturnsAsync(None)
    |> ignore

    chatCtx.Setup(_.SendLink(It.IsAny(), It.IsAny(), It.IsAny())).ReturnsAsync(Mocks.botMessageId)

    let message = createMessage Buttons.IncludePlaylist

    task {
      let! result = handler message

      result |> should equal (Some())

      chatCtx.VerifyAll()
      chatCtx.VerifyNoOtherCalls()
    }

type ExcludePlaylistButtonMessageHandler() =
  let musicPlatform = Mock<IMusicPlatform>()
  let musicPlatformFactory = Mock<IMusicPlatformFactory>()

  let authService = Mock<IInitAuth>()
  let resourceProvider = Mock<IResourceProvider>()
  let chatCtx = Mock<IBotService>()

  do
    resourceProvider.Setup(_.Item(Buttons.ExcludePlaylist)).Returns(Buttons.ExcludePlaylist)
    |> ignore

  do
    resourceProvider.Setup(_.Item(Messages.SendExcludedPlaylist)).Returns("Send excluded playlist")
    |> ignore

  let handler =
    Message.excludePlaylistButtonMessageHandler musicPlatformFactory.Object authService.Object resourceProvider.Object chatCtx.Object

  [<Fact>]
  member _.``should ask for excluded playlist when platform present``() =
    musicPlatformFactory.Setup(_.GetMusicPlatform(Mocks.userId.ToMusicPlatformId())).ReturnsAsync(Some musicPlatform.Object)
    |> ignore

    chatCtx.Setup(_.AskForReply(It.IsAny())).ReturnsAsync(()) |> ignore

    let message = createMessage Buttons.ExcludePlaylist

    task {
      let! result = handler message

      result |> should equal (Some())

      chatCtx.Verify(_.AskForReply(resourceProvider.Object.Item(Messages.SendExcludedPlaylist)))
    }

  [<Fact>]
  member _.``should send login when platform missing``() =
    musicPlatformFactory.Setup(_.GetMusicPlatform(Mocks.userId.ToMusicPlatformId())).ReturnsAsync(None)
    |> ignore

    chatCtx.Setup(_.SendLink(It.IsAny(), It.IsAny(), It.IsAny())).ReturnsAsync(Mocks.botMessageId)

    let message = createMessage Buttons.ExcludePlaylist

    task {
      let! result = handler message

      result |> should equal (Some())

      chatCtx.VerifyAll()
      chatCtx.VerifyNoOtherCalls()
    }

type TargetPlaylistButtonMessageHandler() =
  let musicPlatform = Mock<IMusicPlatform>()
  let musicPlatformFactory = Mock<IMusicPlatformFactory>()

  let authService = Mock<IInitAuth>()
  let resourceProvider = Mock<IResourceProvider>()
  let chatCtx = Mock<IBotService>()

  do
    resourceProvider.Setup(_.Item(Buttons.TargetPlaylist)).Returns(Buttons.TargetPlaylist)
    |> ignore

  do
    resourceProvider.Setup(_.Item(Messages.SendTargetedPlaylist)).Returns("Send targeted playlist")
    |> ignore

  let handler =
    Message.targetPlaylistButtonMessageHandler musicPlatformFactory.Object authService.Object resourceProvider.Object chatCtx.Object

  [<Fact>]
  member _.``should ask for targeted playlist when platform present``() =
    musicPlatformFactory.Setup(_.GetMusicPlatform(Mocks.userId.ToMusicPlatformId())).ReturnsAsync(Some musicPlatform.Object)
    |> ignore

    chatCtx.Setup(_.AskForReply(It.IsAny())).ReturnsAsync(()) |> ignore

    let message = createMessage Buttons.TargetPlaylist

    task {
      let! result = handler message

      result |> should equal (Some())

      chatCtx.Verify(_.AskForReply(resourceProvider.Object.Item(Messages.SendTargetedPlaylist)))
    }

  [<Fact>]
  member _.``should send login when platform missing``() =
    musicPlatformFactory.Setup(_.GetMusicPlatform(Mocks.userId.ToMusicPlatformId())).ReturnsAsync(None)
    |> ignore

    chatCtx.Setup(_.SendLink(It.IsAny(), It.IsAny(), It.IsAny())).ReturnsAsync(Mocks.botMessageId)

    let message = createMessage Buttons.TargetPlaylist

    task {
      let! result = handler message

      result |> should equal (Some())

      chatCtx.VerifyAll()
      chatCtx.VerifyNoOtherCalls()
    }

type IncludePlaylistMessageHandler() =
  let userRepo = Mock<ILoadUser>()
  let presetService = Mock<IIncludePlaylist>()
  let authService = Mock<IInitAuth>()
  let resourceProvider = Mock<IResourceProvider>()
  let chatCtx = Mock<IBotService>()

  do
    resourceProvider.Setup(_.Item(Messages.SendIncludedPlaylist)).Returns("Send included playlist")
    |> ignore

  let handler =
    Message.includePlaylistMessageHandler userRepo.Object presetService.Object authService.Object resourceProvider.Object chatCtx.Object

  [<Fact>]
  member _.``should handle reply with included playlist and send message``() =
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)

    presetService.Setup(_.IncludePlaylist(Mocks.userId, It.IsAny(), It.IsAny())).ReturnsAsync(Ok Mocks.includedPlaylist)
    |> ignore

    chatCtx.Setup(_.SendMessage(It.IsAny<string>())).ReturnsAsync(Mocks.botMessageId)
    |> ignore

    let message =
      { createMessage "raw-id" with
          ReplyMessage = Some { Text = resourceProvider.Object.Item(Messages.SendIncludedPlaylist) } }

    task {
      let! result = handler message

      result |> should equal (Some())

      userRepo.Verify(_.LoadUser(Mocks.userId))
      presetService.Verify(_.IncludePlaylist(Mocks.userId, It.IsAny(), It.IsAny()))
      chatCtx.Verify(_.SendMessage(It.IsAny<string>()))
    }

  [<Fact>]
  member _.``should handle /includeplaylist command and send message``() =
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)

    presetService.Setup(_.IncludePlaylist(Mocks.userId, It.IsAny(), It.IsAny())).ReturnsAsync(Ok Mocks.includedPlaylist)
    |> ignore

    chatCtx.Setup(_.SendMessage(It.IsAny<string>())).ReturnsAsync(Mocks.botMessageId)
    |> ignore

    let message = createMessage $"{Commands.includePlaylist} raw-id"

    task {
      let! result = handler message

      result |> should equal (Some())

      userRepo.Verify(_.LoadUser(Mocks.userId))
      presetService.Verify(_.IncludePlaylist(Mocks.userId, It.IsAny(), It.IsAny()))
      chatCtx.Verify(_.SendMessage(It.IsAny<string>()))
    }

  [<Fact>]
  member _.``should send IdCannotBeParsed on id parsing error``() =
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)

    presetService
      .Setup(_.IncludePlaylist(Mocks.userId, It.IsAny(), It.IsAny()))
      .ReturnsAsync(Error(Preset.IncludePlaylistError.IdParsing(Playlist.IdParsingError "bad")))
    |> ignore

    resourceProvider.Setup(_.Item(Messages.PlaylistIdCannotBeParsed, It.IsAny())).Returns("Id cannot be parsed")
    |> ignore

    chatCtx.Setup(_.SendMessage(It.IsAny<string>())).ReturnsAsync(Mocks.botMessageId)
    |> ignore

    let message = createMessage $"{Commands.includePlaylist} bad-id"

    task {
      let! result = handler message

      result |> should equal (Some())

      chatCtx.Verify(_.SendMessage(It.IsAny<string>()))
    }

  [<Fact>]
  member _.``should send NotFound when playlist not found``() =
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)

    presetService
      .Setup(_.IncludePlaylist(Mocks.userId, It.IsAny(), It.IsAny()))
      .ReturnsAsync(Error(Preset.IncludePlaylistError.Load(Playlist.LoadError.NotFound)))
    |> ignore

    resourceProvider.Setup(_.Item(Messages.PlaylistNotFoundInSpotify, It.IsAny())).Returns("Not found")
    |> ignore

    chatCtx.Setup(_.SendMessage(It.IsAny<string>())).ReturnsAsync(Mocks.botMessageId)
    |> ignore

    let message = createMessage $"{Commands.includePlaylist} nf-id"

    task {
      let! result = handler message

      result |> should equal (Some())

      chatCtx.Verify(_.SendMessage(It.IsAny<string>()))
    }

  [<Fact>]
  member _.``should trigger login when unauthorized``() =
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)

    presetService
      .Setup(_.IncludePlaylist(Mocks.userId, It.IsAny(), It.IsAny()))
      .ReturnsAsync(Error Preset.IncludePlaylistError.Unauthorized)
    |> ignore

    chatCtx.Setup(_.SendLink(It.IsAny(), It.IsAny(), It.IsAny())).ReturnsAsync(Mocks.botMessageId)
    |> ignore

    let message = createMessage $"{Commands.includePlaylist} some-id"

    task {
      let! result = handler message

      result |> should equal (Some())

      chatCtx.Verify(_.SendLink(It.IsAny(), It.IsAny(), It.IsAny()))
    }

type ExcludePlaylistMessageHandler() =
  let userRepo = Mock<ILoadUser>()
  let presetService = Mock<IExcludePlaylist>()
  let authService = Mock<IInitAuth>()
  let resourceProvider = Mock<IResourceProvider>()
  let chatCtx = Mock<IBotService>()

  do
    resourceProvider.Setup(_.Item(Messages.SendExcludedPlaylist)).Returns("Send excluded playlist")
    |> ignore

  let handler =
    Message.excludePlaylistMessageHandler userRepo.Object presetService.Object authService.Object resourceProvider.Object chatCtx.Object

  [<Fact>]
  member _.``should handle reply with excluded playlist and send message``() =
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)

    presetService.Setup(_.ExcludePlaylist(Mocks.userId, It.IsAny(), It.IsAny())).ReturnsAsync(Ok(Mocks.excludedPlaylist))
    |> ignore

    chatCtx.Setup(_.SendMessage(It.IsAny<string>())).ReturnsAsync(Mocks.botMessageId)
    |> ignore

    let message =
      { createMessage "raw-id" with
          ReplyMessage = Some { Text = resourceProvider.Object.Item(Messages.SendExcludedPlaylist) } }

    task {
      let! result = handler message

      result |> should equal (Some())

      userRepo.Verify(_.LoadUser(Mocks.userId))
      presetService.Verify(_.ExcludePlaylist(Mocks.userId, It.IsAny(), It.IsAny()))
      chatCtx.Verify(_.SendMessage(It.IsAny<string>()))
    }

  [<Fact>]
  member _.``should handle /excludeplaylist command and send message``() =
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)

    presetService.Setup(_.ExcludePlaylist(Mocks.userId, It.IsAny(), It.IsAny())).ReturnsAsync(Ok(Mocks.excludedPlaylist))
    |> ignore

    chatCtx.Setup(_.SendMessage(It.IsAny<string>())).ReturnsAsync(Mocks.botMessageId)
    |> ignore

    let message = createMessage $"{Commands.excludePlaylist} raw-id"

    task {
      let! result = handler message

      result |> should equal (Some())

      userRepo.Verify(_.LoadUser(Mocks.userId))
      presetService.Verify(_.ExcludePlaylist(Mocks.userId, It.IsAny(), It.IsAny()))
      chatCtx.Verify(_.SendMessage(It.IsAny<string>()))
    }

  [<Fact>]
  member _.``should send IdCannotBeParsed on id parsing error``() =
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)

    presetService
      .Setup(_.ExcludePlaylist(Mocks.userId, It.IsAny(), It.IsAny()))
      .ReturnsAsync(Error(Preset.ExcludePlaylistError.IdParsing(Playlist.IdParsingError "bad")))
    |> ignore

    resourceProvider.Setup(_.Item(Messages.PlaylistIdCannotBeParsed, It.IsAny())).Returns("Id cannot be parsed")
    |> ignore

    chatCtx.Setup(_.SendMessage(It.IsAny<string>())).ReturnsAsync(Mocks.botMessageId)
    |> ignore

    let message = createMessage $"{Commands.excludePlaylist} bad-id"

    task {
      let! result = handler message

      result |> should equal (Some())

      chatCtx.Verify(_.SendMessage(It.IsAny<string>()))
    }

  [<Fact>]
  member _.``should send NotFound when playlist not found``() =
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)

    presetService
      .Setup(_.ExcludePlaylist(Mocks.userId, It.IsAny(), It.IsAny()))
      .ReturnsAsync(Error(Preset.ExcludePlaylistError.Load(Playlist.LoadError.NotFound)))
    |> ignore

    resourceProvider.Setup(_.Item(Messages.PlaylistNotFoundInSpotify, It.IsAny())).Returns("Not found")
    |> ignore

    chatCtx.Setup(_.SendMessage(It.IsAny<string>())).ReturnsAsync(Mocks.botMessageId)
    |> ignore

    let message = createMessage $"{Commands.excludePlaylist} nf-id"

    task {
      let! result = handler message

      result |> should equal (Some())

      chatCtx.Verify(_.SendMessage(It.IsAny<string>()))
    }

  [<Fact>]
  member _.``should trigger login when unauthorized``() =
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)

    presetService
      .Setup(_.ExcludePlaylist(Mocks.userId, It.IsAny(), It.IsAny()))
      .ReturnsAsync(Error Preset.ExcludePlaylistError.Unauthorized)
    |> ignore

    chatCtx.Setup(_.SendLink(It.IsAny(), It.IsAny(), It.IsAny())).ReturnsAsync(Mocks.botMessageId)
    |> ignore

    let message = createMessage $"{Commands.excludePlaylist} some-id"

    task {
      let! result = handler message

      result |> should equal (Some())

      chatCtx.Verify(_.SendLink(It.IsAny(), It.IsAny(), It.IsAny()))
    }

type TargetPlaylistMessageHandler() =
  let userRepo = Mock<ILoadUser>()
  let presetService = Mock<ITargetPlaylist>()
  let authService = Mock<IInitAuth>()
  let resourceProvider = Mock<IResourceProvider>()
  let chatCtx = Mock<IBotService>()

  do
    resourceProvider.Setup(_.Item(Messages.SendTargetedPlaylist)).Returns("Send targeted playlist")
    |> ignore

  let handler =
    Message.targetPlaylistMessageHandler userRepo.Object presetService.Object authService.Object resourceProvider.Object chatCtx.Object

  [<Fact>]
  member _.``should handle reply with targeted playlist and send message``() =
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)

    presetService.Setup(_.TargetPlaylist(Mocks.userId, It.IsAny(), It.IsAny())).ReturnsAsync(Ok Mocks.targetedPlaylist)
    |> ignore

    resourceProvider.Setup(_.Item(Messages.PlaylistTargeted, It.IsAny())).Returns("Playlist targeted")
    |> ignore

    chatCtx.Setup(_.SendMessage("Playlist targeted")).ReturnsAsync(Mocks.botMessageId)
    |> ignore

    let message = createMessageWithReply "playlist-url" "Send targeted playlist"

    task {
      let! result = handler message

      result |> should equal (Some())

      chatCtx.Verify(_.SendMessage(It.IsAny<string>()))
    }

  [<Fact>]
  member _.``should handle /targetplaylist command and send message``() =
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)

    presetService.Setup(_.TargetPlaylist(Mocks.userId, It.IsAny(), It.IsAny())).ReturnsAsync(Ok Mocks.targetedPlaylist)
    |> ignore

    resourceProvider.Setup(_.Item(Messages.PlaylistTargeted, It.IsAny())).Returns("Playlist targeted")
    |> ignore

    chatCtx.Setup(_.SendMessage("Playlist targeted")).ReturnsAsync(Mocks.botMessageId)
    |> ignore

    let message = createMessage $"{Commands.targetPlaylist} playlist-url"

    task {
      let! result = handler message

      result |> should equal (Some())

      chatCtx.Verify(_.SendMessage(It.IsAny<string>()))
    }

  [<Fact>]
  member _.``should send IdCannotBeParsed on id parsing error``() =
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)

    presetService
      .Setup(_.TargetPlaylist(Mocks.userId, It.IsAny(), It.IsAny()))
      .ReturnsAsync(Error(Preset.TargetPlaylistError.IdParsing(Playlist.IdParsingError "invalid-id")))
    |> ignore

    resourceProvider.Setup(_.Item(Messages.PlaylistIdCannotBeParsed, It.IsAny())).Returns("Invalid ID")
    |> ignore

    chatCtx.Setup(_.SendMessage("Invalid ID")).ReturnsAsync(Mocks.botMessageId)
    |> ignore

    let message = createMessageWithReply "invalid-id" "Send targeted playlist"

    task {
      let! result = handler message

      result |> should equal (Some())

      chatCtx.Verify(_.SendMessage(It.IsAny<string>()))
    }

  [<Fact>]
  member _.``should send NotFound when playlist not found``() =
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)

    presetService
      .Setup(_.TargetPlaylist(Mocks.userId, It.IsAny(), It.IsAny()))
      .ReturnsAsync(Error(Preset.TargetPlaylistError.Load(Playlist.LoadError.NotFound)))
    |> ignore

    resourceProvider.Setup(_.Item(Messages.PlaylistNotFoundInSpotify, It.IsAny())).Returns("Not found")
    |> ignore

    chatCtx.Setup(_.SendMessage("Not found")).ReturnsAsync(Mocks.botMessageId)
    |> ignore

    let message = createMessageWithReply "playlist-url" "Send targeted playlist"

    task {
      let! result = handler message

      result |> should equal (Some())

      chatCtx.Verify(_.SendMessage(It.IsAny<string>()))
    }

  [<Fact>]
  member _.``should send PlaylistIsReadonly on access error``() =
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)

    presetService
      .Setup(_.TargetPlaylist(Mocks.userId, It.IsAny(), It.IsAny()))
      .ReturnsAsync(Error(Preset.TargetPlaylistError.AccessError(Preset.AccessError())))
    |> ignore

    resourceProvider.Setup(_.Item(Messages.PlaylistIsReadonly)).Returns("Playlist is readonly")
    |> ignore

    chatCtx.Setup(_.SendMessage("Playlist is readonly")).ReturnsAsync(Mocks.botMessageId)
    |> ignore

    let message = createMessageWithReply "playlist-url" "Send targeted playlist"

    task {
      let! result = handler message

      result |> should equal (Some())

      chatCtx.Verify(_.SendMessage(It.IsAny<string>()))
    }

  [<Fact>]
  member _.``should trigger login when unauthorized``() =
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)

    presetService.Setup(_.TargetPlaylist(Mocks.userId, It.IsAny(), It.IsAny())).ReturnsAsync(Error Preset.TargetPlaylistError.Unauthorized)
    |> ignore

    chatCtx.Setup(_.SendLink(It.IsAny(), It.IsAny(), It.IsAny())).ReturnsAsync(Mocks.botMessageId)
    |> ignore

    let message = createMessageWithReply "playlist-url" "Send targeted playlist"

    task {
      let! result = handler message

      result |> should equal (Some())

      chatCtx.Verify(_.SendLink(It.IsAny(), It.IsAny(), It.IsAny()))
    }

  [<Fact>]
  member _.``should return None for unmatched message``() =
    let message = createMessage "some random text"

    task {
      let! result = handler message

      result |> should equal None
      userRepo.VerifyNoOtherCalls()
      presetService.VerifyNoOtherCalls()
      chatCtx.VerifyNoOtherCalls()
    }