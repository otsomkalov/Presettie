module Telegram.Tests.PresetSettings

#nowarn "20"

open Domain.Core
open Domain.Repos
open Domain.Tests
open FsUnit.Xunit
open Moq
open Telegram.Constants
open Telegram.Core
open Telegram.Handlers.Click
open Xunit
open otsom.fs.Bot

let private createClick data : Click =
  { Id = Mocks.clickId
    Chat = Mocks.chat
    MessageId = Mocks.botMessageId
    Data = data }

[<Fact>]
let ``enableUniqueArtists should update preset and show updated if data matched`` () =
  let presetRepo = Mock<IPresetRepo>()

  presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Mocks.preset)

  let presetService = Mock<IPresetService>()

  presetService.Setup(_.EnableUniqueArtists(Mocks.presetId)).ReturnsAsync(())

  let botService = Mock<IBotService>()

  botService
    .Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny()))
    .ReturnsAsync(())

  let sut =
    enableUniqueArtistsClickHandler presetRepo.Object presetService.Object botService.Object

  let click =
    createClick [ "p"; Mocks.presetId.Value; CallbackQueryConstants.enableUniqueArtists ]

  task {
    let! result = sut click

    result |> should equal (Some())

    presetRepo.VerifyAll()
    botService.VerifyAll()
    presetService.VerifyAll()
  }

[<Fact>]
let ``enableUniqueArtists should not update preset if data does not match`` () =
  let presetRepo = Mock<IPresetRepo>()
  let presetService = Mock<IPresetService>()
  let botService = Mock<IBotService>()

  let sut =
    enableUniqueArtistsClickHandler presetRepo.Object presetService.Object botService.Object

  let click = createClick []

  task {
    let! result = sut click

    result |> should equal None

    presetRepo.VerifyAll()
    botService.VerifyAll()
    presetService.VerifyAll()
  }

[<Fact>]
let ``disableUniqueArtists should update preset and show updated if data matched`` () =
  let presetRepo = Mock<IPresetRepo>()

  presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Mocks.preset)

  let presetService = Mock<IPresetService>()

  presetService.Setup(_.DisableUniqueArtists(Mocks.presetId)).ReturnsAsync(())

  let botService = Mock<IBotService>()

  botService
    .Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny()))
    .ReturnsAsync(())

  let sut =
    disableUniqueArtistsClickHandler presetRepo.Object presetService.Object botService.Object

  let click =
    createClick [ "p"; Mocks.presetId.Value; CallbackQueryConstants.disableUniqueArtists ]

  task {
    let! result = sut click

    result |> should equal (Some())

    presetRepo.VerifyAll()
    botService.VerifyAll()
    presetService.VerifyAll()
  }

[<Fact>]
let ``disableUniqueArtists should not update preset if data does not match`` () =
  let presetRepo = Mock<IPresetRepo>()
  let presetService = Mock<IPresetService>()
  let botService = Mock<IBotService>()

  let sut =
    disableUniqueArtistsClickHandler presetRepo.Object presetService.Object botService.Object

  let click = createClick []

  task {
    let! result = sut click

    result |> should equal None

    presetRepo.VerifyAll()
    botService.VerifyAll()
    presetService.VerifyAll()
  }