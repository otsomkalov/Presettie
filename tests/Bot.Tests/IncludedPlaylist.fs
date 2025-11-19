namespace Bot.Tests

open Bot.Constants
open Domain.Core
open Domain.Repos
open Moq
open MusicPlatform
open Bot.Core
open FsUnit.Xunit
open Bot.Handlers.Click
open Bot.Tests
open Xunit
open otsom.fs.Bot
open otsom.fs.Resources
open Domain.Tests

type IncludedPlaylist() =
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
  member this.``list click should list included playlists if data match``() = task {
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny())).ReturnsAsync(())

    let click =
      createClick [ "p"; Mocks.preset.Id.Value; CallbackQueryConstants.includedPlaylists; "0" ]

    let! result = listIncludedPlaylistsClickHandler presetRepo.Object resourceProvider.Object botService.Object click

    result |> should equal (Some())

    presetRepo.VerifyAll()
    botService.VerifyAll()
  }

  [<Fact>]
  member this.``list click should not list included playlists if data does not match``() = task {
    let click = createClick []

    let! result = listIncludedPlaylistsClickHandler presetRepo.Object resourceProvider.Object botService.Object click

    result |> should equal None

    presetRepo.VerifyNoOtherCalls()
    botService.VerifyNoOtherCalls()
  }

  [<Fact>]
  member this.``show click should send included playlist details``() = task {
    musicPlatform.Setup(_.LoadPlaylist(Mocks.includedPlaylistId)).ReturnsAsync(Ok Mocks.readablePlatformPlaylist)

    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny())).ReturnsAsync(())

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some musicPlatform.Object)

    let click =
      createClick
        [ "p"
          Mocks.preset.Id.Value
          CallbackQueryConstants.includedPlaylists
          Mocks.includedPlaylistId.Value
          "i" ]

    let! result =
      showIncludedPlaylistClickHandler presetRepo.Object musicPlatformFactory.Object resourceProvider.Object botService.Object click

    result |> should equal (Some())

    presetRepo.VerifyAll()
    botService.VerifyAll()
    musicPlatform.VerifyAll()
  }

  [<Fact>]
  member this.``show click should not send included playlist details if data does not match``() = task {
    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some musicPlatform.Object)

    let click = createClick []

    let! result =
      showIncludedPlaylistClickHandler presetRepo.Object musicPlatformFactory.Object resourceProvider.Object botService.Object click

    result |> should equal None

    presetRepo.VerifyNoOtherCalls()
    botService.VerifyNoOtherCalls()
    musicPlatform.VerifyNoOtherCalls()
  }

  [<Fact>]
  member this.``remove click should delete playlist and show included playlists``() = task {
    presetService
      .Setup(_.RemoveIncludedPlaylist(Mocks.presetId, Mocks.includedPlaylist.Id))
      .ReturnsAsync(
        { Mocks.preset with
            IncludedPlaylists = [] }
      )

    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny())).ReturnsAsync(())

    let click =
      createClick
        [ "p"
          Mocks.preset.Id.Value
          CallbackQueryConstants.includedPlaylists
          Mocks.includedPlaylistId.Value
          "rm" ]

    let! result = removeIncludedPlaylistClickHandler presetService.Object resourceProvider.Object botService.Object click

    result |> should equal (Some())

    botService.VerifyAll()
    presetService.VerifyAll()
  }

  [<Fact>]
  member this.``remove click should not delete playlist``() = task {
    let click = createClick []

    let! result = removeIncludedPlaylistClickHandler presetService.Object resourceProvider.Object botService.Object click

    result |> should equal None

    botService.VerifyAll()
    presetService.VerifyAll()
  }