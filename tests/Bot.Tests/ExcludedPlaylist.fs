#nowarn "20"

namespace Bot.Tests

open Bot.Constants
open Domain.Core
open Domain.Repos
open Domain.Tests
open Moq
open MusicPlatform
open Bot.Core
open Bot.Handlers.Click
open Xunit
open otsom.fs.Bot
open otsom.fs.Resources
open FsUnit.Xunit

type ExcludedPlaylist() =
  let presetRepo = Mock<IPresetRepo>()
  let botService = Mock<IBotService>()
  let resourceProvider = Mock<IResourceProvider>()
  let musicPlatform = Mock<IMusicPlatform>()
  let musicPlatformFactory = Mock<IMusicPlatformFactory>()
  let presetService = Mock<IPresetService>()

  do
    presetRepo.Setup(_.LoadPreset(Mocks.preset.Id)).ReturnsAsync(Some Mocks.preset)
    |> ignore

  let createClick data : Click =
    { Id = Mocks.clickId
      Chat = Mocks.chat
      MessageId = Mocks.botMessageId
      Data = data }

  [<Fact>]
  member _.``list click should list excluded playlists if data match``() = task {
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny())).ReturnsAsync(())

    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.preset.Id.Value; CallbackQueryConstants.excludedPlaylists; "0" ]

    let! result = listExcludedPlaylistsClickHandler presetRepo.Object resourceProvider.Object botService.Object click

    result |> should equal (Some())

    presetRepo.VerifyAll()
    botService.VerifyAll()
  }

  [<Fact>]
  member _.``list click should not list excluded playlists if data does not match``() = task {
    let click = createClick []

    let! result = listExcludedPlaylistsClickHandler presetRepo.Object resourceProvider.Object botService.Object click

    result |> should equal None

    presetRepo.VerifyNoOtherCalls()
    botService.VerifyNoOtherCalls()
  }

  [<Fact>]
  member _.``show click should send excluded playlist details``() = task {
    musicPlatform.Setup(_.LoadPlaylist(Mocks.excludedPlaylistId)).ReturnsAsync(Ok Mocks.readablePlatformPlaylist)

    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny())).ReturnsAsync(())

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some musicPlatform.Object)

    let click =
      createClick
        [ CallbackQueryConstants.preset
          Mocks.preset.Id.Value
          CallbackQueryConstants.excludedPlaylists
          Mocks.excludedPlaylistId.Value
          "i" ]

    let! result =
      showExcludedPlaylistClickHandler presetRepo.Object musicPlatformFactory.Object resourceProvider.Object botService.Object click

    result |> should equal (Some())

    presetRepo.VerifyAll()
    botService.VerifyAll()
    musicPlatform.VerifyAll()
  }

  [<Fact>]
  member _.``show click should not send playlist details if data does not match``() = task {
    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some musicPlatform.Object)

    let click = createClick []

    let! result =
      showExcludedPlaylistClickHandler presetRepo.Object musicPlatformFactory.Object resourceProvider.Object botService.Object click

    result |> should equal None

    presetRepo.VerifyNoOtherCalls()
    botService.VerifyNoOtherCalls()
    musicPlatform.VerifyNoOtherCalls()
  }

  [<Fact>]
  member _.``remove click should delete excluded playlist and show excluded playlists``() = task {
    presetService
      .Setup(_.RemoveExcludedPlaylist(Mocks.presetId, Mocks.excludedPlaylist.Id))
      .ReturnsAsync(
        { Mocks.preset with
            ExcludedPlaylists = [] }
      )

    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny())).ReturnsAsync(())

    let click =
      createClick
        [ CallbackQueryConstants.preset
          Mocks.preset.Id.Value
          CallbackQueryConstants.excludedPlaylists
          Mocks.excludedPlaylistId.Value
          "rm" ]

    let! result = removeExcludedPlaylistClickHandler presetService.Object resourceProvider.Object botService.Object click

    result |> should equal (Some())

    botService.VerifyAll()
    presetService.VerifyAll()
  }

  [<Fact>]
  member _.``remove click should not delete playlist``() = task {
    let click = createClick []

    let! result = removeExcludedPlaylistClickHandler presetService.Object resourceProvider.Object botService.Object click

    result |> should equal None

    botService.VerifyAll()
    presetService.VerifyAll()
  }