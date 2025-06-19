module Telegram.Tests.ExcludedPlaylist

#nowarn "20"

open Domain.Core
open Domain.Repos
open Moq
open MusicPlatform
open NSubstitute
open Telegram.Core
open Telegram.Handlers.Click
open Xunit
open otsom.fs.Bot
open FsUnit
open Telegram.Tests
open Domain.Tests
open otsom.fs.Resources

let private createClick data : Click =
  { Id = Mocks.clickId
    Chat = Mocks.chat
    MessageId = Mocks.botMessageId
    Data = data }

[<Fact>]
let ``list click should list excluded playlists if data match`` () =
  let presetRepo = Mock<IPresetRepo>()

  presetRepo.Setup(_.LoadPreset(Mocks.preset.Id)).ReturnsAsync(Mocks.preset)

  let botService = Mock<IBotService>()

  botService
    .Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny()))
    .ReturnsAsync(())

  let resourceProvider = Mock<IResourceProvider>()

  let click = createClick [ "p"; Mocks.preset.Id.Value; "ep"; "0" ]

  task {
    let! result = listExcludedPlaylistsClickHandler presetRepo.Object resourceProvider.Object botService.Object click

    result |> should equal (Some())

    presetRepo.VerifyAll()
    botService.VerifyAll()
  }

[<Fact>]
let ``list click should not list excluded playlists if data does not match`` () =
  let presetRepo = Mock<IPresetRepo>()

  let botService = Mock<IBotService>()

  let resourceProvider = Mock<IResourceProvider>()

  let click = createClick []

  task {
    let! result = listExcludedPlaylistsClickHandler presetRepo.Object resourceProvider.Object botService.Object click

    result |> should equal None

    presetRepo.VerifyAll()
    botService.VerifyAll()
  }

[<Fact>]
let ``show click should send excluded playlist details`` () =
  let presetRepo = Mock<IPresetRepo>()

  presetRepo.Setup(_.LoadPreset(Mocks.preset.Id)).ReturnsAsync(Mocks.preset)

  let musicPlatform = Mock<IMusicPlatform>()

  musicPlatform
    .Setup(_.LoadPlaylist(Mocks.excludedPlaylistId))
    .ReturnsAsync(Ok Mocks.readablePlatformPlaylist)

  let botService = Mock<IBotService>()

  botService
    .Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny()))
    .ReturnsAsync(())

  let musicPlatformFactory = Substitute.For<IMusicPlatformFactory>()

  musicPlatformFactory.GetMusicPlatform(Arg.Any()).Returns(Some musicPlatform.Object)

  let resourceProvider = Mock<IResourceProvider>()

  let click =
    createClick [ "p"; Mocks.preset.Id.Value; "ep"; Mocks.excludedPlaylistId.Value; "i" ]

  task {
    let! result = showExcludedPlaylistClickHandler presetRepo.Object musicPlatformFactory resourceProvider.Object botService.Object click

    result |> should equal (Some())

    presetRepo.VerifyAll()
    botService.VerifyAll()
    musicPlatform.VerifyAll()
  }

[<Fact>]
let ``show click should not send playlist details if data does not match`` () =
  let presetRepo = Mock<IPresetRepo>()

  let botService = Mock<IBotService>()
  let musicPlatform = Mock<IMusicPlatform>()

  let resourceProvider = Mock<IResourceProvider>()

  let click = createClick []

  let musicPlatformFactory = Substitute.For<IMusicPlatformFactory>()

  musicPlatformFactory.GetMusicPlatform(Arg.Any()).Returns(Some musicPlatform.Object)

  task {
    let! result = showExcludedPlaylistClickHandler presetRepo.Object musicPlatformFactory resourceProvider.Object botService.Object click

    result |> should equal None

    presetRepo.VerifyAll()
    botService.VerifyAll()
    musicPlatform.VerifyAll()
  }

[<Fact>]
let ``remove click should delete excluded playlist and show excluded playlists`` () =
  let presetRepo = Mock<IPresetRepo>()

  presetRepo.Setup(_.LoadPreset(Mocks.preset.Id)).ReturnsAsync(Mocks.preset)

  let presetService = Mock<IPresetService>()

  presetService
    .Setup(_.RemoveExcludedPlaylist(Mocks.presetId, Mocks.excludedPlaylist.Id))
    .ReturnsAsync(())

  let botService = Mock<IBotService>()

  botService
    .Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny()))
    .ReturnsAsync(())

  let resourceProvider = Mock<IResourceProvider>()

  let click =
    createClick [ "p"; Mocks.preset.Id.Value; "ep"; Mocks.excludedPlaylistId.Value; "rm" ]

  task {
    let! result = removeExcludedPlaylistClickHandler presetRepo.Object presetService.Object resourceProvider.Object botService.Object click

    result |> should equal (Some())

    presetRepo.VerifyAll()
    botService.VerifyAll()
    presetService.VerifyAll()
  }

[<Fact>]
let ``remove click should not delete playlist`` () =
  let presetRepo = Mock<IPresetRepo>()
  let presetService = Mock<IPresetService>()
  let botService = Mock<IBotService>()

  let resourceProvider = Mock<IResourceProvider>()

  let click = createClick []

  task {
    let! result = removeExcludedPlaylistClickHandler presetRepo.Object presetService.Object resourceProvider.Object botService.Object click

    result |> should equal None

    presetRepo.VerifyAll()
    botService.VerifyAll()
    presetService.VerifyAll()
  }