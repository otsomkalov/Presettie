namespace Bot.Tests

open Domain.Core
open Domain.Repos
open Moq
open Runners.Shared
open Tests.Shared
open Xunit
open Bot.Workflows
open otsom.fs.Bot
open otsom.fs.Resources

type User() =
  let botService = Mock<IBotService>()
  let userRepo = Mock<IUserRepo>()
  let resp = Mock<IResourceProvider>()
  let apiClient = Mock<IApiClient>()

  [<Fact>]
  member this.``should list presets``() =
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny())).ReturnsAsync(())

    apiClient
      .Setup(_.ListPresets())
      .ReturnsAsync(
        [
          {
            Id = Mocks.presetId
            Name = Mocks.preset.Name
          }
        ]
      )

    task {
      do! User.listPresets resp.Object apiClient.Object botService.Object Mocks.botMessageId ()

      botService.VerifyAll()
      apiClient.VerifyAll()
    }

  [<Fact>]
  member this.``sendCurrentPreset should show current preset details with actions keyboard if current preset is set``() =
    userRepo.Setup(fun m -> m.LoadUser Mocks.userId).ReturnsAsync(Mocks.user)

    apiClient.Setup(fun m -> m.GetPreset Mocks.presetId).ReturnsAsync(Ok Mocks.preset)

    botService.Setup(_.SendKeyboard(It.IsAny(), It.IsAny())).ReturnsAsync(Mocks.botMessageId)

    let sut =
      User.sendCurrentPreset resp.Object userRepo.Object apiClient.Object botService.Object

    task {
      do! sut Mocks.chatMessageId Mocks.userId

      userRepo.VerifyAll()
      apiClient.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member this.``sendCurrentPreset should send "create preset" button if current preset is not set``() =
    userRepo
      .Setup(fun m -> m.LoadUser Mocks.userId)
      .ReturnsAsync(
        { Mocks.user with
            CurrentPresetId = None
        }
      )

    botService.Setup(_.SendKeyboard(It.IsAny(), It.IsAny())).ReturnsAsync(Mocks.botMessageId)

    let sut =
      User.sendCurrentPreset resp.Object userRepo.Object apiClient.Object botService.Object

    task {
      do! sut Mocks.chatMessageId Mocks.userId

      userRepo.VerifyAll()
      apiClient.VerifyAll()
      botService.VerifyAll()
    }