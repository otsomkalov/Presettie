module Telegram.Tests.IncludedPlaylist

open Domain.Core
open Domain.Repos
open Domain.Tests
open Moq
open MusicPlatform
open Telegram.Core
open FsUnit.Xunit
open Telegram.Handlers.Click
open Xunit
open otsom.fs.Bot
open otsom.fs.Resources

#nowarn "20"

let private createClick data : Click =
  { Id = Mocks.clickId
    Chat = Mocks.chat
    MessageId = Mocks.botMessageId
    Data = data }

[<Fact>]
let ``list click should list included playlists if data match`` () =
  let presetRepo = Mock<IPresetRepo>()

  presetRepo.Setup(_.LoadPreset(Mocks.preset.Id)).ReturnsAsync(Some Mocks.preset)

  let botService = Mock<IBotService>()

  botService
    .Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny()))
    .ReturnsAsync(())

  let resourceProvider = Mock<IResourceProvider>()

  let click = createClick [ "p"; Mocks.preset.Id.Value; "ip"; "0" ]

  task {
    let! result = listIncludedPlaylistsClickHandler presetRepo.Object resourceProvider.Object botService.Object click

    result |> should equal (Some())

    presetRepo.VerifyAll()
    botService.VerifyAll()
  }

[<Fact>]
let ``list click should not list included playlists if data does not match`` () =
  let presetRepo = Mock<IPresetRepo>()

  let botService = Mock<IBotService>()

  let click = createClick []

  let resourceProvider = Mock<IResourceProvider>()

  task {
    let! result = listIncludedPlaylistsClickHandler presetRepo.Object resourceProvider.Object botService.Object click

    result |> should equal None

    presetRepo.VerifyAll()
    botService.VerifyAll()
  }

[<Fact>]
let ``show click should send included playlist details`` () =
  let presetRepo = Mock<IPresetRepo>()

  presetRepo.Setup(_.LoadPreset(Mocks.preset.Id)).ReturnsAsync(Some Mocks.preset)

  let musicPlatform = Mock<IMusicPlatform>()

  musicPlatform
    .Setup(_.LoadPlaylist(Mocks.includedPlaylistId))
    .ReturnsAsync(Ok Mocks.readablePlatformPlaylist)

  let botService = Mock<IBotService>()

  botService
    .Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny()))
    .ReturnsAsync(())

  let resourceProvider = Mock<IResourceProvider>()

  let musicPlatformFactory = Mock<IMusicPlatformFactory>()

  musicPlatformFactory
    .Setup(_.GetMusicPlatform(It.IsAny()))
    .ReturnsAsync(Some musicPlatform.Object)

  let click =
    createClick [ "p"; Mocks.preset.Id.Value; "ip"; Mocks.includedPlaylistId.Value; "i" ]

  task {
    let! result =
      showIncludedPlaylistClickHandler presetRepo.Object musicPlatformFactory.Object resourceProvider.Object botService.Object click

    result |> should equal (Some())

    presetRepo.VerifyAll()
    botService.VerifyAll()
    musicPlatform.VerifyAll()
  }

[<Fact>]
let ``show click should not send included playlist details if data does not match`` () =
  let presetRepo = Mock<IPresetRepo>()

  let botService = Mock<IBotService>()
  let musicPlatform = Mock<IMusicPlatform>()

  let click = createClick []

  let musicPlatformFactory = Mock<IMusicPlatformFactory>()

  musicPlatformFactory
    .Setup(_.GetMusicPlatform(It.IsAny()))
    .ReturnsAsync(Some musicPlatform.Object)

  let resourceProvider = Mock<IResourceProvider>()

  task {
    let! result =
      showIncludedPlaylistClickHandler presetRepo.Object musicPlatformFactory.Object resourceProvider.Object botService.Object click

    result |> should equal None

    presetRepo.VerifyAll()
    botService.VerifyAll()
    musicPlatform.VerifyAll()
  }

[<Fact>]
let ``remove click should delete playlist and show included playlists`` () =
  let presetService = Mock<IPresetService>()

  presetService
    .Setup(_.RemoveIncludedPlaylist(Mocks.presetId, Mocks.includedPlaylist.Id))
    .ReturnsAsync(
      { Mocks.preset with
          IncludedPlaylists = [] }
    )

  let botService = Mock<IBotService>()

  botService
    .Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny()))
    .ReturnsAsync(())

  let click =
    createClick [ "p"; Mocks.preset.Id.Value; "ip"; Mocks.includedPlaylistId.Value; "rm" ]

  let resourceProvider = Mock<IResourceProvider>()

  task {
    let! result = removeIncludedPlaylistClickHandler presetService.Object resourceProvider.Object botService.Object click

    result |> should equal (Some())

    botService.VerifyAll()
    presetService.VerifyAll()
  }

[<Fact>]
let ``remove click should not delete playlist`` () =
  let presetService = Mock<IPresetService>()
  let botService = Mock<IBotService>()

  let click = createClick []

  let resourceProvider = Mock<IResourceProvider>()

  task {
    let! result = removeIncludedPlaylistClickHandler presetService.Object resourceProvider.Object botService.Object click

    result |> should equal None

    botService.VerifyAll()
    presetService.VerifyAll()
  }