#nowarn "20"

namespace Bot.Tests

open Bot.Constants
open Domain.Core
open Moq
open MusicPlatform
open Bot.Handlers.Click
open Bot.Tests
open Runners.Shared
open Tests.Shared
open Xunit
open otsom.fs.Bot
open otsom.fs.Resources

type ExcludedPlaylist() =
  let botService = Mock<IBotService>()
  let resourceProvider = Mock<IResourceProvider>()
  let musicPlatform = Mock<IMusicPlatform>()
  let musicPlatformFactory = Mock<IMusicPlatformFactory>()
  let apiClient = Mock<IApiClient>()

  let preset =
    { Mocks.preset with
        ExcludedPlaylists = [ Mocks.excludedPlaylist ]
    }

  let createClick data : Click =
    {
      Id = Mocks.clickId
      MessageId = Mocks.botMessageId
      Data = data
    }

  [<Fact>]
  member _.``list click should list excluded playlists if data match``() = task {
    do apiClient.Setup(_.GetPreset(Mocks.preset.Id)).ReturnsAsync(Ok preset) |> ignore

    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny())).ReturnsAsync(())

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          Mocks.preset.Id.Value
          CallbackQueryConstants.excludedPlaylists
          "0"
        ]

    let! result =
      listExcludedPlaylistsClickHandler apiClient.Object resourceProvider.Object botService.Object Mocks.chat click

    Assert.Equal(Some(), result)

    apiClient.VerifyAll()
    botService.VerifyAll()
  }

  [<Fact>]
  member _.``list click should not list excluded playlists if data does not match``() = task {
    do apiClient.Setup(_.GetPreset(Mocks.preset.Id)).ReturnsAsync(Ok preset) |> ignore
    let click = createClick []

    let! result =
      listExcludedPlaylistsClickHandler apiClient.Object resourceProvider.Object botService.Object Mocks.chat click

    Assert.Equal(None, result)

    apiClient.VerifyNoOtherCalls()
    botService.VerifyNoOtherCalls()
  }

  [<Fact>]
  member _.``show click should send excluded playlist details``() = task {
    do
      apiClient.Setup(_.GetPreset(Mocks.preset.Id)).ReturnsAsync(Ok preset)
      |> ignore

    musicPlatform.Setup(_.LoadPlaylist(Mocks.excludedPlaylistId)).ReturnsAsync(Ok Mocks.readablePlatformPlaylist)

    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny())).ReturnsAsync(())

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some musicPlatform.Object)

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          Mocks.preset.Id.Value
          CallbackQueryConstants.excludedPlaylists
          Mocks.excludedPlaylistId.Value
          "i"
        ]

    let! result =
      showExcludedPlaylistClickHandler
        apiClient.Object
        musicPlatformFactory.Object
        resourceProvider.Object
        botService.Object
        Mocks.chat
        click

    Assert.Equal(Some(), result)

    apiClient.VerifyAll()
    botService.VerifyAll()
    musicPlatform.VerifyAll()
  }

  [<Fact>]
  member _.``show click should not send playlist details if data does not match``() = task {
    do apiClient.Setup(_.GetPreset(Mocks.preset.Id)).ReturnsAsync(Ok preset) |> ignore

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some musicPlatform.Object)

    let click = createClick []

    let! result =
      showExcludedPlaylistClickHandler
        apiClient.Object
        musicPlatformFactory.Object
        resourceProvider.Object
        botService.Object
        Mocks.chat
        click

    Assert.Equal(None, result)

    apiClient.VerifyNoOtherCalls()
    botService.VerifyNoOtherCalls()
    musicPlatform.VerifyNoOtherCalls()
  }

  [<Fact>]
  member _.``remove click should delete excluded playlist and show excluded playlists``() = task {
    apiClient
      .Setup(_.RemoveExcludedPlaylist(Mocks.presetId, Mocks.excludedPlaylistId))
      .ReturnsAsync(
        { Mocks.preset with
            ExcludedPlaylists = []
        }
      )

    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny())).ReturnsAsync(())

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          Mocks.preset.Id.Value
          CallbackQueryConstants.excludedPlaylists
          Mocks.excludedPlaylistId.Value
          "rm"
        ]

    let! result =
      removeExcludedPlaylistClickHandler apiClient.Object resourceProvider.Object botService.Object Mocks.chat click

    Assert.Equal(Some(), result)

    botService.VerifyAll()
    apiClient.VerifyAll()
  }

  [<Fact>]
  member _.``remove click should not delete playlist``() = task {
    let click = createClick []

    let! result =
      removeExcludedPlaylistClickHandler apiClient.Object resourceProvider.Object botService.Object Mocks.chat click

    Assert.Equal(None, result)

    botService.VerifyAll()
    apiClient.VerifyAll()
  }