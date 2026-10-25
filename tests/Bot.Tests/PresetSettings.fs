namespace Bot.Tests

#nowarn "20"

open Domain.Core
open Moq
open Bot.Constants
open Bot.Handlers.Click
open Runners.Shared
open Tests.Shared
open Xunit
open otsom.fs.Bot
open otsom.fs.Resources
open Bot.Resources

type PresetSettings() =
  let apiClient = Mock<IApiClient>()
  let botService = Mock<IBotService>()
  let resourceProvider = Mock<IResourceProvider>()

  let createClick data : Click =
    {
      Id = Mocks.clickId
      MessageId = Mocks.botMessageId
      Data = data
    }

  [<Fact>]
  member this.``enableUniqueArtists should update preset and show updated if data matched``() =
    apiClient.Setup(_.PatchPresetSettings(Mocks.presetId, [ PatchUniqueArtists(true) ])).ReturnsAsync(Ok Mocks.preset)
    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny())).ReturnsAsync(())
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny())).ReturnsAsync(())
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns(Notifications.Updated)

    let sut =
      enableUniqueArtistsClickHandler apiClient.Object resourceProvider.Object botService.Object

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          Mocks.presetId.Value
          CallbackQueryConstants.enableUniqueArtists
        ]

    task {
      let! result = sut Mocks.chat click
      Assert.Equal(Some(), result)
      apiClient.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member this.``enableUniqueArtists should not update preset if data does not match``() =
    let sut =
      enableUniqueArtistsClickHandler apiClient.Object resourceProvider.Object botService.Object

    let click = createClick []

    task {
      let! result = sut Mocks.chat click
      Assert.Equal(None, result)
      apiClient.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member this.``disableUniqueArtists should update preset and show updated if data matched``() =
    apiClient.Setup(_.PatchPresetSettings(Mocks.presetId, [ PatchUniqueArtists(false) ])).ReturnsAsync(Ok Mocks.preset)
    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny())).ReturnsAsync(())
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny(), It.IsAny())).ReturnsAsync(())
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns(Notifications.Updated)

    let sut =
      disableUniqueArtistsClickHandler apiClient.Object resourceProvider.Object botService.Object

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          Mocks.presetId.Value
          CallbackQueryConstants.disableUniqueArtists
        ]

    task {
      let! result = sut Mocks.chat click
      Assert.Equal(Some(), result)
      apiClient.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member this.``disableUniqueArtists should not update preset if data does not match``() =
    let sut =
      disableUniqueArtistsClickHandler apiClient.Object resourceProvider.Object botService.Object

    let click = createClick []

    task {
      let! result = sut Mocks.chat click
      Assert.Equal(None, result)
      apiClient.VerifyAll()
      botService.VerifyAll()
    }