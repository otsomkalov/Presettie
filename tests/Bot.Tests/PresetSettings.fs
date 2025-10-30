namespace Bot.Tests

#nowarn "20"

open Domain.Core
open Domain.Repos
open Domain.Tests
open FsUnit.Xunit
open Moq
open Bot.Constants
open Bot.Core
open Bot.Handlers.Click
open Xunit
open otsom.fs.Bot
open otsom.fs.Resources

type PresetSettings() =
  let presetRepo = Mock<IPresetRepo>()
  let presetService = Mock<IPresetService>()
  let botService = Mock<IBotService>()
  let resourceProvider = Mock<IResourceProvider>()

  let createClick data : Click =
    { Id = Mocks.clickId
      Chat = Mocks.chat
      MessageId = Mocks.botMessageId
      Data = data }

  [<Fact>]
  member this.``enableUniqueArtists should update preset and show updated if data matched``() =
    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)
    presetService.Setup(_.EnableUniqueArtists(Mocks.presetId)).ReturnsAsync(())
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny())).ReturnsAsync(())

    let sut =
      enableUniqueArtistsClickHandler presetRepo.Object presetService.Object resourceProvider.Object botService.Object

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
  member this.``enableUniqueArtists should not update preset if data does not match``() =
    let sut =
      enableUniqueArtistsClickHandler presetRepo.Object presetService.Object resourceProvider.Object botService.Object

    let click = createClick []

    task {
      let! result = sut click
      result |> should equal None
      presetRepo.VerifyAll()
      botService.VerifyAll()
      presetService.VerifyAll()
    }

  [<Fact>]
  member this.``disableUniqueArtists should update preset and show updated if data matched``() =
    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)
    presetService.Setup(_.DisableUniqueArtists(Mocks.presetId)).ReturnsAsync(())
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny())).ReturnsAsync(())

    let sut =
      disableUniqueArtistsClickHandler presetRepo.Object presetService.Object resourceProvider.Object botService.Object

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
  member this.``disableUniqueArtists should not update preset if data does not match``() =
    let sut =
      disableUniqueArtistsClickHandler presetRepo.Object presetService.Object resourceProvider.Object botService.Object

    let click = createClick []

    task {
      let! result = sut click
      result |> should equal None
      presetRepo.VerifyAll()
      botService.VerifyAll()
      presetService.VerifyAll()
    }