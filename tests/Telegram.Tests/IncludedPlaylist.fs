module Telegram.Tests.IncludedPlaylist

open System.Threading.Tasks
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

  presetRepo.Setup(_.LoadPreset(Mocks.preset.Id)).ReturnsAsync(Mocks.preset)

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

  presetRepo.Setup(_.LoadPreset(Mocks.preset.Id)).ReturnsAsync(Mocks.preset)

  let musicPlatform = Mock<IMusicPlatform>()

  musicPlatform
    .Setup(_.LoadPlaylist(Mocks.includedPlaylistId))
    .ReturnsAsync(Ok Mocks.readablePlatformPlaylist)

  let botService = Mock<IBotService>()

  botService
    .Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny()))
    .ReturnsAsync(())

  let resourceProvider = Mock<IResourceProvider>()

  let buildMusicPlatform _ =
    Task.FromResult(Some musicPlatform.Object)

  let click =
    createClick [ "p"; Mocks.preset.Id.Value; "ip"; Mocks.includedPlaylistId.Value; "i" ]

  task {
    let! result = showIncludedPlaylistClickHandler presetRepo.Object buildMusicPlatform resourceProvider.Object botService.Object click

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

  let buildMusicPlatform _ =
    Task.FromResult(Some musicPlatform.Object)

  let resourceProvider = Mock<IResourceProvider>()

  task {
    let! result = showIncludedPlaylistClickHandler presetRepo.Object buildMusicPlatform resourceProvider.Object botService.Object click

    result |> should equal None

    presetRepo.VerifyAll()
    botService.VerifyAll()
    musicPlatform.VerifyAll()
  }

[<Fact>]
let ``remove click should delete playlist and show included playlists`` () =
  let presetRepo = Mock<IPresetRepo>()

  presetRepo.Setup(_.LoadPreset(Mocks.preset.Id)).ReturnsAsync(Mocks.preset)

  let presetService = Mock<IPresetService>()

  presetService
    .Setup(_.RemoveIncludedPlaylist(Mocks.presetId, Mocks.includedPlaylist.Id))
    .ReturnsAsync(())

  let botService = Mock<IBotService>()

  botService
    .Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny()))
    .ReturnsAsync(())

  let click =
    createClick [ "p"; Mocks.preset.Id.Value; "ip"; Mocks.includedPlaylistId.Value; "rm" ]

  let resourceProvider = Mock<IResourceProvider>()

  task {
    let! result = removeIncludedPlaylistClickHandler presetRepo.Object presetService.Object resourceProvider.Object botService.Object click

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

  let click = createClick []

  let resourceProvider = Mock<IResourceProvider>()

  task {
    let! result = removeIncludedPlaylistClickHandler presetRepo.Object presetService.Object resourceProvider.Object botService.Object click

    result |> should equal None

    presetRepo.VerifyAll()
    botService.VerifyAll()
    presetService.VerifyAll()
  }