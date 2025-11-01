namespace Bot.Tests

open Domain.Core
open Domain.Repos
open Domain.Tests
open Moq
open Xunit
open Bot.Workflows
open otsom.fs.Bot
open otsom.fs.Resources

type User() =
  let botService = Mock<IBotService>()
  let presetRepo = Mock<IPresetRepo>()
  let userRepo = Mock<IUserRepo>()
  let resp = Mock<IResourceProvider>()

  [<Fact>]
  member this.``should list presets``() =
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny())).ReturnsAsync(())

    presetRepo
      .Setup(_.ListUserPresets(Mocks.userId))
      .ReturnsAsync(
        [ { Id = Mocks.presetId
            Name = Mocks.preset.Name } ]
      )

    task {
      do! User.listPresets resp.Object botService.Object presetRepo.Object Mocks.botMessageId Mocks.userId

      botService.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member this.``sendCurrentPreset should show current preset details with actions keyboard if current preset is set``() =
    userRepo.Setup(fun m -> m.LoadUser Mocks.userId).ReturnsAsync(Mocks.user)

    presetRepo.Setup(fun m -> m.LoadPreset Mocks.presetId).ReturnsAsync(Some Mocks.preset)

    botService.Setup(_.SendKeyboard(It.IsAny(), It.IsAny())).ReturnsAsync(Mocks.botMessageId)

    let sut =
      User.sendCurrentPreset resp.Object userRepo.Object presetRepo.Object botService.Object

    task {
      do! sut Mocks.userId

      userRepo.VerifyAll()
      presetRepo.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member this.``sendCurrentPreset should send "create preset" button if current preset is not set``() =
    userRepo
      .Setup(fun m -> m.LoadUser Mocks.userId)
      .ReturnsAsync(
        { Mocks.user with
            CurrentPresetId = None }
      )

    botService.Setup(_.SendKeyboard(It.IsAny(), It.IsAny())).ReturnsAsync(Mocks.botMessageId)

    let sut =
      User.sendCurrentPreset resp.Object userRepo.Object presetRepo.Object botService.Object

    task {
      do! sut Mocks.userId

      userRepo.VerifyAll()
      presetRepo.VerifyAll()
      botService.VerifyAll()
    }