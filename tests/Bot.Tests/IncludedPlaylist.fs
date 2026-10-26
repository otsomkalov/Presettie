namespace Bot.Tests

open Bot.Constants
open Domain.Core
open Moq
open Bot.Handlers.Click
open Bot.Tests
open Runners.Shared
open Tests.Shared
open Xunit
open otsom.fs.Bot
open otsom.fs.Resources

type IncludedPlaylist() =
  let botService = Mock<IBotService>()
  let resourceProvider = Mock<IResourceProvider>()
  let apiClient = Mock<IApiClient>()

  let preset =
    { Mocks.preset with
        IncludedPlaylists = [ Mocks.includedPlaylist ]
    }

  let createClick data : Click =
    {
      Id = Mocks.clickId
      MessageId = Mocks.botMessageId
      Data = data
    }

  [<Fact>]
  member this.``list click should list included playlists if data match``() = task {
    do apiClient.Setup(_.GetPreset(Mocks.preset.Id)).ReturnsAsync(Ok preset) |> ignore
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny())).ReturnsAsync(())

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          Mocks.preset.Id.Value
          CallbackQueryConstants.includedPlaylists
          "0"
        ]

    let! result =
      listIncludedPlaylistsClickHandler apiClient.Object resourceProvider.Object botService.Object Mocks.chat click

    Assert.Equal(Some(), result)

    apiClient.VerifyAll()
    botService.VerifyAll()
  }

  [<Fact>]
  member this.``list click should not list included playlists if data does not match``() = task {
    do apiClient.Setup(_.GetPreset(Mocks.preset.Id)).ReturnsAsync(Ok preset) |> ignore
    let click = createClick []

    let! result =
      listIncludedPlaylistsClickHandler apiClient.Object resourceProvider.Object botService.Object Mocks.chat click

    Assert.Equal(None, result)

    apiClient.VerifyNoOtherCalls()
    botService.VerifyNoOtherCalls()
  }

  [<Fact>]
  member this.``show click should send included playlist details``() = task {
    do apiClient.Setup(_.GetPreset(Mocks.preset.Id)).ReturnsAsync(Ok preset) |> ignore

    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny())).ReturnsAsync(())

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          Mocks.preset.Id.Value
          CallbackQueryConstants.includedPlaylists
          Mocks.includedPlaylistId.Value
          "i"
        ]

    let! result =
      showIncludedPlaylistClickHandler apiClient.Object resourceProvider.Object botService.Object Mocks.chat click

    Assert.Equal(Some(), result)

    apiClient.VerifyAll()
    botService.VerifyAll()
  }

  [<Fact>]
  member this.``show click should not send included playlist details if data does not match``() = task {
    let click = createClick []

    let! result =
      showIncludedPlaylistClickHandler apiClient.Object resourceProvider.Object botService.Object Mocks.chat click

    Assert.Equal(None, result)

    apiClient.VerifyNoOtherCalls()
    botService.VerifyNoOtherCalls()
  }

  [<Fact>]
  member this.``remove click should delete playlist and show included playlists``() = task {
    apiClient
      .Setup(_.RemoveIncludedPlaylist(Mocks.presetId, Mocks.includedPlaylistId))
      .ReturnsAsync(
        { Mocks.preset with
            IncludedPlaylists = []
        }
      )

    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny())).ReturnsAsync(())

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          Mocks.preset.Id.Value
          CallbackQueryConstants.includedPlaylists
          Mocks.includedPlaylistId.Value
          "rm"
        ]

    let! result =
      removeIncludedPlaylistClickHandler apiClient.Object resourceProvider.Object botService.Object Mocks.chat click

    Assert.Equal(Some(), result)

    botService.VerifyAll()
    apiClient.VerifyAll()
  }

  [<Fact>]
  member this.``remove click should not delete playlist``() = task {
    let click = createClick []

    let! result =
      removeIncludedPlaylistClickHandler apiClient.Object resourceProvider.Object botService.Object Mocks.chat click

    Assert.Equal(None, result)

    botService.VerifyAll()
    apiClient.VerifyAll()
  }