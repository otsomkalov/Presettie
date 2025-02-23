﻿module Telegram.Tests.TargetedPlaylist

#nowarn "20"

open System.Threading.Tasks
open Domain.Core
open Domain.Repos
open Moq
open MusicPlatform
open Telegram.Core
open Telegram.Handlers.Click
open Xunit
open otsom.fs.Bot
open FsUnit
open Telegram.Tests
open Domain.Tests

let private createClick data : Click =
  { Id = Mocks.clickId
    Chat = Mocks.chat
    MessageId = Mocks.botMessageId
    Data = data }

[<Fact>]
let ``list click should list targeted playlists if data match`` () =
  let presetRepo = Mock<IPresetRepo>()

  presetRepo.Setup(_.LoadPreset(Mocks.preset.Id)).ReturnsAsync(Mocks.preset)

  let botService = Mock<IBotService>()

  botService
    .Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny()))
    .ReturnsAsync(())

  let click = createClick [ "p"; Mocks.preset.Id.Value; "tp"; "0" ]

  task {
    let! result = listTargetedPlaylistsClickHandler presetRepo.Object botService.Object click

    result |> should equal (Some())

    presetRepo.VerifyAll()
    botService.VerifyAll()
  }

[<Fact>]
let ``list click should not list targeted playlists if data does not match`` () =
  let presetRepo = Mock<IPresetRepo>()

  let botService = Mock<IBotService>()

  let click = createClick []

  task {
    let! result = listTargetedPlaylistsClickHandler presetRepo.Object botService.Object click

    result |> should equal None

    presetRepo.VerifyAll()
    botService.VerifyAll()
  }

[<Fact>]
let ``show click should send targeted playlist details`` () =
  let presetRepo = Mock<IPresetRepo>()

  presetRepo.Setup(_.LoadPreset(Mocks.preset.Id)).ReturnsAsync(Mocks.preset)

  let musicPlatform = Mock<IMusicPlatform>()

  musicPlatform
    .Setup(_.LoadPlaylist(Mocks.targetedPlaylistId))
    .ReturnsAsync(Ok Mocks.writablePlatformPlaylist)

  let botService = Mock<IBotService>()

  botService
    .Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny()))
    .ReturnsAsync(())

  let buildMusicPlatform _ =
    Task.FromResult(Some musicPlatform.Object)

  let click =
    createClick [ "p"; Mocks.preset.Id.Value; "tp"; Mocks.targetedPlaylistId.Value; "i" ]

  task {
    let! result = showTargetedPlaylistClickHandler presetRepo.Object buildMusicPlatform botService.Object click

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

  let click = createClick []

  let buildMusicPlatform _ =
    Task.FromResult(Some musicPlatform.Object)

  task {
    let! result = showTargetedPlaylistClickHandler presetRepo.Object buildMusicPlatform botService.Object click

    result |> should equal None

    presetRepo.VerifyAll()
    botService.VerifyAll()
    musicPlatform.VerifyAll()
  }

[<Fact>]
let ``remove click should delete targeted playlist and show excluded playlists`` () =
  let presetRepo = Mock<IPresetRepo>()

  presetRepo.Setup(_.LoadPreset(Mocks.preset.Id)).ReturnsAsync(Mocks.preset)

  let presetService = Mock<IPresetService>()

  presetService
    .Setup(_.RemoveTargetedPlaylist(Mocks.presetId, Mocks.targetedPlaylist.Id))
    .ReturnsAsync(())

  let botService = Mock<IBotService>()

  botService
    .Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny()))
    .ReturnsAsync(())

  let click =
    createClick [ "p"; Mocks.preset.Id.Value; "tp"; Mocks.targetedPlaylistId.Value; "rm" ]

  task {
    let! result = removeTargetedPlaylistClickHandler presetRepo.Object presetService.Object botService.Object click

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

  task {
    let! result = removeTargetedPlaylistClickHandler presetRepo.Object presetService.Object botService.Object click

    result |> should equal None

    presetRepo.VerifyAll()
    botService.VerifyAll()
    presetService.VerifyAll()
  }