module Telegram.Tests.User

#nowarn "20"

open Domain.Core
open Domain.Repos
open Domain.Tests
open Moq
open Xunit
open Telegram.Workflows
open otsom.fs.Bot
open otsom.fs.Resources

[<Fact>]
let ``should list presets`` () =
  let botService = Mock<IBotService>()

  botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny())).ReturnsAsync(())

  let presetRepo = Mock<IPresetRepo>()

  presetRepo
    .Setup(_.ListUserPresets(Mocks.userId))
    .ReturnsAsync(
      [ { Id = Mocks.presetId
          Name = Mocks.preset.Name } ]
    )

  let resp = Mock<IResourceProvider>()

  task {
    do! User.listPresets resp.Object botService.Object presetRepo.Object Mocks.botMessageId Mocks.userId

    botService.VerifyAll()
    presetRepo.VerifyAll()
  }

[<Fact>]
let ``sendCurrentPreset should show current preset details with actions keyboard if current preset is set`` () =
  let userRepo = Mock<IUserRepo>()

  userRepo.Setup(fun m -> m.LoadUser Mocks.userId).ReturnsAsync(Mocks.user)

  let presetRepo = Mock<IPresetRepo>()

  presetRepo.Setup(fun m -> m.LoadPreset Mocks.presetId).ReturnsAsync(Some Mocks.preset)

  let botService = Mock<IBotService>()

  botService.Setup(_.SendKeyboard(It.IsAny(), It.IsAny())).ReturnsAsync(Mocks.botMessageId)

  let resp = Mock<IResourceProvider>()

  let sut =
    User.sendCurrentPreset resp.Object userRepo.Object presetRepo.Object botService.Object

  task {
    do! sut Mocks.userId

    userRepo.VerifyAll()
    presetRepo.VerifyAll()
    botService.VerifyAll()
  }

[<Fact>]
let ``sendCurrentPreset should send "create preset" button if current preset is not set`` () =
  let userRepo = Mock<IUserRepo>()

  userRepo
    .Setup(fun m -> m.LoadUser Mocks.userId)
    .ReturnsAsync(
      { Mocks.user with
          CurrentPresetId = None }
    )

  let presetRepo = Mock<IPresetRepo>()

  let botService = Mock<IBotService>()

  botService.Setup(_.SendKeyboard(It.IsAny(), It.IsAny())).ReturnsAsync(Mocks.botMessageId)

  let resp = Mock<IResourceProvider>()

  let sut =
    User.sendCurrentPreset resp.Object userRepo.Object presetRepo.Object botService.Object

  task {
    do! sut Mocks.userId

    userRepo.VerifyAll()
    presetRepo.VerifyAll()
    botService.VerifyAll()
  }