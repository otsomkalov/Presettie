namespace Bot.Tests

#nowarn "20"

open Bot.Constants
open Domain.Core
open Domain.Repos
open Moq
open MusicPlatform
open Bot.Core
open Bot.Handlers.Click
open Xunit
open otsom.fs.Bot
open FsUnit.Xunit
open Domain.Tests
open otsom.fs.Resources

type TargetedPlaylist() =
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
  member _.``list click should list targeted playlists if data match``() = task {
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny())).ReturnsAsync(())

    let click = createClick [ CallbackQueryConstants.preset; Mocks.preset.Id.Value; "tp"; "0" ]

    let! result = listTargetedPlaylistsClickHandler presetRepo.Object resourceProvider.Object botService.Object click

    result |> should equal (Some())

    presetRepo.VerifyAll()
    botService.VerifyAll()
  }

  [<Fact>]
  member _.``list click should not list targeted playlists if data does not match``() = task {
    let click = createClick []

    let! result = listTargetedPlaylistsClickHandler presetRepo.Object resourceProvider.Object botService.Object click

    result |> should equal None

    presetRepo.VerifyNoOtherCalls()
    botService.VerifyNoOtherCalls()
  }

  [<Fact>]
  member _.``show click should send targeted playlist details``() = task {
    musicPlatform.Setup(_.LoadPlaylist(Mocks.targetedPlaylistId)).ReturnsAsync(Ok Mocks.writablePlatformPlaylist)

    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny())).ReturnsAsync(())

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some musicPlatform.Object)

    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.preset.Id.Value; "tp"; Mocks.targetedPlaylistId.Value; "i" ]

    let! result =
      showTargetedPlaylistClickHandler presetRepo.Object musicPlatformFactory.Object resourceProvider.Object botService.Object click

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
      showTargetedPlaylistClickHandler presetRepo.Object musicPlatformFactory.Object resourceProvider.Object botService.Object click

    result |> should equal None

    presetRepo.VerifyNoOtherCalls()
    botService.VerifyNoOtherCalls()
    musicPlatform.VerifyNoOtherCalls()
  }

  [<Fact>]
  member _.``remove click should delete targeted playlist and show excluded playlists``() = task {
    presetService
      .Setup(_.RemoveTargetedPlaylist(Mocks.presetId, Mocks.targetedPlaylist.Id))
      .ReturnsAsync(
        { Mocks.preset with
            TargetedPlaylists = [] }
      )

    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny())).ReturnsAsync(())

    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.preset.Id.Value; "tp"; Mocks.targetedPlaylistId.Value; "rm" ]

    let! result = removeTargetedPlaylistClickHandler presetService.Object resourceProvider.Object botService.Object click

    result |> should equal (Some())

    botService.VerifyAll()
    presetService.VerifyAll()
  }

  [<Fact>]
  member _.``remove click should not delete playlist``() = task {
    let click = createClick []

    let! result = removeTargetedPlaylistClickHandler presetService.Object resourceProvider.Object botService.Object click

    result |> should equal None

    botService.VerifyAll()
    presetService.VerifyAll()
  }