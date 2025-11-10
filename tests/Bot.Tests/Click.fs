module Bot.Tests.Click

open Bot.Constants
open Bot.Core
open Bot.Handlers
open Bot.Resources
open Domain.Core.PresetSettings
open Domain.Tests
open FsUnit.Xunit
open Moq
open Xunit
open otsom.fs.Bot
open otsom.fs.Core
open otsom.fs.Resources
open Domain.Core
open Domain.Repos

let createClick data : Click =
  { Id = Mocks.clickId
    Chat = Mocks.chat
    MessageId = Mocks.botMessageId
    Data = data }

type presetInfoClickHandler() =
  let presetRepoMock = Mock<IPresetRepo>()
  let resourceProviderMock = Mock<IResourceProvider>()
  let botServiceMock = Mock<IBotService>()

  let handler =
    Click.presetInfoClickHandler presetRepoMock.Object resourceProviderMock.Object botServiceMock.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    presetRepoMock.Setup(_.LoadPreset(It.IsAny<PresetId>())).ReturnsAsync(Some Mocks.preset)

    let click = createClick [ "p"; Mocks.presetId.Value; "i" ]

    task {
      // Arrange



      // Act
      let! result = handler click

      // Assert
      result |> should equal (Some())

      presetRepoMock.VerifyAll()
      botServiceMock.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    // Arrange

    let click = createClick [ "p"; Mocks.presetId.Value; "r" ]

    task {
      // Act
      let! result = handler click

      // Assert
      result |> should equal None

      presetRepoMock.VerifyNoOtherCalls()
      botServiceMock.VerifyNoOtherCalls()
    }

type listPresetsClickHandler() =
  let presetRepo = Mock<IPresetRepo>()
  let resourceProviderMock = Mock<IResourceProvider>()
  let botServiceMock = Mock<IBotService>()

  let handler =
    Click.listPresetsClickHandler presetRepo.Object resourceProviderMock.Object botServiceMock.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    presetRepo.Setup(_.ListUserPresets(It.IsAny<UserId>())).ReturnsAsync([ Mocks.simplePreset ])
    resourceProviderMock.Setup(fun x -> x[Messages.YourPresets]).Returns(Messages.YourPresets)

    let click = createClick [ "p" ]

    task {
      // Act
      let! result = handler click

      // Assert
      result |> should equal (Some())

      presetRepo.VerifyAll()
      botServiceMock.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click = createClick [ "invalid" ]

    task {
      // Act
      let! result = handler click

      // Assert
      result |> should equal None

      presetRepo.VerifyNoOtherCalls()
      botServiceMock.VerifyNoOtherCalls()
    }

type artistsAlbumsRecommendationsClickHandler() =
  let presetRepo = Mock<IPresetRepo>()
  let presetService = Mock<ISetRecommendationsEngine>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.artistsAlbumsRecommendationsClickHandler presetRepo.Object presetService.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value
    let click = createClick [ "p"; presetId; CallbackQueryConstants.artistsAlbumsRecommendations ]
    presetService.Setup(fun x -> x.SetRecommendationsEngine(Mocks.presetId, Some RecommendationsEngine.ArtistAlbums)).ReturnsAsync(())
    botService.Setup(fun x -> x.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    presetRepo.Setup(fun x -> x.LoadPreset(PresetId presetId)).ReturnsAsync(Some Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns("Updated")
    task {
      let! result = handler click
      result |> should equal (Some())
      presetService.VerifyAll()
      botService.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click = createClick [ "p"; Mocks.presetId.Value; "invalid" ]
    task {
      let! result = handler click
      result |> should equal None
      presetService.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
    }

type reccoBeatsRecommendationsClickHandler() =
  let presetRepo = Mock<IPresetRepo>()
  let presetService = Mock<ISetRecommendationsEngine>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.reccoBeatsRecommendationsClickHandler presetRepo.Object presetService.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value
    let click = createClick [ "p"; presetId; CallbackQueryConstants.reccoBeatsRecommendations ]
    presetService.Setup(fun x -> x.SetRecommendationsEngine(Mocks.presetId, Some RecommendationsEngine.ReccoBeats)).ReturnsAsync(())
    botService.Setup(fun x -> x.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    presetRepo.Setup(fun x -> x.LoadPreset(PresetId presetId)).ReturnsAsync(Some Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns("Updated")
    task {
      let! result = handler click
      result |> should equal (Some())
      presetService.VerifyAll()
      botService.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click = createClick [ "p"; Mocks.presetId.Value; "invalid" ]
    task {
      let! result = handler click
      result |> should equal None
      presetService.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
    }

type spotifyRecommendationsClickHandler() =
  let presetRepo = Mock<IPresetRepo>()
  let presetService = Mock<ISetRecommendationsEngine>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.spotifyRecommendationsClickHandler presetRepo.Object presetService.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value
    let click = createClick [ "p"; presetId; CallbackQueryConstants.spotifyRecommendations ]
    presetService.Setup(fun x -> x.SetRecommendationsEngine(Mocks.presetId, Some RecommendationsEngine.Spotify)).ReturnsAsync(())
    botService.Setup(fun x -> x.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    presetRepo.Setup(fun x -> x.LoadPreset(PresetId presetId)).ReturnsAsync(Some Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns("Updated")
    task {
      let! result = handler click
      result |> should equal (Some())
      presetService.VerifyAll()
      botService.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click = createClick [ "p"; Mocks.presetId.Value; "invalid" ]
    task {
      let! result = handler click
      result |> should equal None
      presetService.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
    }

type disableRecommendationsClickHandler() =
  let presetRepo = Mock<IPresetRepo>()
  let presetService = Mock<ISetRecommendationsEngine>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.disableRecommendationsClickHandler presetRepo.Object presetService.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value
    let click = createClick [ "p"; presetId; CallbackQueryConstants.disableRecommendations ]
    presetService.Setup(fun x -> x.SetRecommendationsEngine(Mocks.presetId, None)).ReturnsAsync(())
    botService.Setup(fun x -> x.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    presetRepo.Setup(fun x -> x.LoadPreset(PresetId presetId)).ReturnsAsync(Some Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns("Updated")
    task {
      let! result = handler click
      result |> should equal (Some())
      presetService.VerifyAll()
      botService.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click = createClick [ "p"; Mocks.presetId.Value; "invalid" ]
    task {
      let! result = handler click
      result |> should equal None
      presetService.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
    }

type enableUniqueArtistsClickHandler() =
  let presetRepo = Mock<IPresetRepo>()
  let presetService = Mock<IEnableUniqueArtists>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.enableUniqueArtistsClickHandler presetRepo.Object presetService.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value
    let click = createClick [ "p"; presetId; CallbackQueryConstants.enableUniqueArtists ]
    presetService.Setup(fun x -> x.EnableUniqueArtists(Mocks.presetId)).ReturnsAsync(())
    botService.Setup(fun x -> x.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    presetRepo.Setup(fun x -> x.LoadPreset(PresetId presetId)).ReturnsAsync(Some Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns("Updated")
    task {
      let! result = handler click
      result |> should equal (Some())
      presetService.VerifyAll()
      botService.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click = createClick [ "p"; Mocks.presetId.Value; "invalid" ]
    task {
      let! result = handler click
      result |> should equal None
      presetService.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
    }

type disableUniqueArtistsClickHandler() =
  let presetRepo = Mock<IPresetRepo>()
  let presetService = Mock<IDisableUniqueArtists>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.disableUniqueArtistsClickHandler presetRepo.Object presetService.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value
    let click = createClick [ "p"; presetId; CallbackQueryConstants.disableUniqueArtists ]
    presetService.Setup(fun x -> x.DisableUniqueArtists(Mocks.presetId)).ReturnsAsync(())
    botService.Setup(fun x -> x.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    presetRepo.Setup(fun x -> x.LoadPreset(PresetId presetId)).ReturnsAsync(Some Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns("Updated")
    task {
      let! result = handler click
      result |> should equal (Some())
      presetService.VerifyAll()
      botService.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click = createClick [ "p"; Mocks.presetId.Value; "invalid" ]
    task {
      let! result = handler click
      result |> should equal None
      presetService.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
    }

type includeLikedTracksClickHandler() =
  let presetRepo = Mock<IPresetRepo>()
  let presetService = Mock<IIncludeLikedTracks>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.includeLikedTracksClickHandler presetRepo.Object presetService.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value
    let click = createClick [ "p"; presetId; CallbackQueryConstants.includeLikedTracks ]
    presetService.Setup(fun x -> x.IncludeLikedTracks(Mocks.presetId)).ReturnsAsync(())
    botService.Setup(fun x -> x.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    presetRepo.Setup(fun x -> x.LoadPreset(PresetId presetId)).ReturnsAsync(Some Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns("Updated")
    task {
      let! result = handler click
      result |> should equal (Some())
      presetService.VerifyAll()
      botService.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click = createClick [ "p"; Mocks.presetId.Value; "invalid" ]
    task {
      let! result = handler click
      result |> should equal None
      presetService.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
    }

type excludeLikedTracksClickHandler() =
  let presetRepo = Mock<IPresetRepo>()
  let presetService = Mock<IExcludeLikedTracks>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.excludeLikedTracksClickHandler presetRepo.Object presetService.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value
    let click = createClick [ "p"; presetId; CallbackQueryConstants.excludeLikedTracks ]
    presetService.Setup(fun x -> x.ExcludeLikedTracks(Mocks.presetId)).ReturnsAsync(())
    botService.Setup(fun x -> x.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    presetRepo.Setup(fun x -> x.LoadPreset(PresetId presetId)).ReturnsAsync(Some Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns("Updated")
    task {
      let! result = handler click
      result |> should equal (Some())
      presetService.VerifyAll()
      botService.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click = createClick [ "p"; Mocks.presetId.Value; "invalid" ]
    task {
      let! result = handler click
      result |> should equal None
      presetService.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
    }

type ignoreLikedTracksClickHandler() =
  let presetRepo = Mock<IPresetRepo>()
  let presetService = Mock<IIgnoreLikedTracks>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.ignoreLikedTracksClickHandler presetRepo.Object presetService.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value
    let click = createClick [ "p"; presetId; CallbackQueryConstants.ignoreLikedTracks ]
    presetService.Setup(fun x -> x.IgnoreLikedTracks(Mocks.presetId)).ReturnsAsync(())
    botService.Setup(fun x -> x.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    presetRepo.Setup(fun x -> x.LoadPreset(PresetId presetId)).ReturnsAsync(Some Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns("Updated")
    task {
      let! result = handler click
      result |> should equal (Some())
      presetService.VerifyAll()
      botService.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click = createClick [ "p"; Mocks.presetId.Value; "invalid" ]
    task {
      let! result = handler click
      result |> should equal None
      presetService.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
    }