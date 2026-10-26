namespace Bot.Tests

#nowarn "20"

open Bot.Constants
open Domain.Core
open Moq
open Bot.Handlers.Click
open Runners.Shared
open Tests.Shared
open Xunit
open otsom.fs.Bot
open otsom.fs.Resources

type TargetedPlaylist() =
  let botService = Mock<IBotService>()
  let resourceProvider = Mock<IResourceProvider>()
  let apiClient = Mock<IApiClient>()

  let createClick data : Click =
    {
      Id = Mocks.clickId
      MessageId = Mocks.botMessageId
      Data = data
    }

  [<Fact>]
  member _.``list click should list targeted playlists if data match``() = task {
    do
      apiClient.Setup(_.GetPreset(Mocks.preset.Id)).ReturnsAsync(Ok Mocks.preset)
      |> ignore

    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny())).ReturnsAsync(())

    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.preset.Id.Value; "tp"; "0" ]

    let! result =
      listTargetedPlaylistsClickHandler apiClient.Object resourceProvider.Object botService.Object Mocks.chat click

    Assert.Equal(Some(), result)

    apiClient.VerifyAll()
    botService.VerifyAll()
  }

  [<Fact>]
  member _.``list click should not list targeted playlists if data does not match``() = task {
    do
      apiClient.Setup(_.GetPreset(Mocks.preset.Id)).ReturnsAsync(Ok Mocks.preset)
      |> ignore

    let click = createClick []

    let! result =
      listTargetedPlaylistsClickHandler apiClient.Object resourceProvider.Object botService.Object Mocks.chat click

    Assert.Equal(None, result)

    apiClient.VerifyNoOtherCalls()
    botService.VerifyNoOtherCalls()
  }

  [<Fact>]
  member _.``show click should send targeted playlist details``() = task {
    do
      apiClient.Setup(_.GetPreset(Mocks.preset.Id)).ReturnsAsync(Ok Mocks.preset)
      |> ignore

    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny())).ReturnsAsync(())

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          Mocks.preset.Id.Value
          "tp"
          Mocks.targetedPlaylistId.Value
          "i"
        ]

    let! result =
      showTargetedPlaylistClickHandler
        apiClient.Object
        resourceProvider.Object
        botService.Object
        Mocks.chat
        click

    Assert.Equal(Some(), result)

    apiClient.VerifyAll()
    botService.VerifyAll()
  }

  [<Fact>]
  member _.``show click should not send playlist details if data does not match``() = task {
    let click = createClick []

    let! result =
      showTargetedPlaylistClickHandler
        apiClient.Object
        resourceProvider.Object
        botService.Object
        Mocks.chat
        click

    Assert.Equal(None, result)

    apiClient.VerifyNoOtherCalls()
    botService.VerifyNoOtherCalls()
  }

  [<Fact>]
  member _.``remove click should delete targeted playlist and show excluded playlists``() = task {
    apiClient
      .Setup(_.RemoveTargetedPlaylist(Mocks.presetId, Mocks.targetedPlaylistId))
      .ReturnsAsync(
        { Mocks.preset with
            TargetedPlaylists = []
        }
      )

    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny())).ReturnsAsync(())

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          Mocks.preset.Id.Value
          "tp"
          Mocks.targetedPlaylistId.Value
          "rm"
        ]

    let! result =
      removeTargetedPlaylistClickHandler apiClient.Object resourceProvider.Object botService.Object Mocks.chat click

    Assert.Equal(Some(), result)

    botService.VerifyAll()
    apiClient.VerifyAll()
  }

  [<Fact>]
  member _.``remove click should not delete playlist``() = task {
    let click = createClick []

    let! result =
      removeTargetedPlaylistClickHandler apiClient.Object resourceProvider.Object botService.Object Mocks.chat click

    Assert.Equal(None, result)

    botService.VerifyAll()
    apiClient.VerifyAll()
  }