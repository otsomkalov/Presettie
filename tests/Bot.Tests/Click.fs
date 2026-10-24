module Bot.Tests.Click

open Bot.Constants
open Bot.Core
open Bot.Handlers
open Bot.Resources
open Domain.Core.PresetSettings
open Domain.Tests
open Moq
open Runners.Shared
open Tests.Shared
open Xunit
open otsom.fs.Bot
open otsom.fs.Resources
open Domain.Core

let createClick data : Click =
  {
    Id = Mocks.clickId
    MessageId = Mocks.botMessageId
    Data = data
  }

type presetInfoClickHandler() =
  let apiClient = Mock<IApiClient>()
  let resourceProviderMock = Mock<IResourceProvider>()
  let botServiceMock = Mock<IBotService>()

  let handler =
    Click.presetInfoClickHandler apiClient.Object resourceProviderMock.Object botServiceMock.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    // Arrange

    apiClient.Setup(_.GetPreset(It.IsAny<PresetId>())).ReturnsAsync(Ok Mocks.preset)

    let click = createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "i" ]

    task {
      // Act
      let! result = handler Mocks.chat click

      // Assert
      Assert.Equal(Some(), result)

      apiClient.VerifyAll()
      botServiceMock.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    // Arrange

    let click = createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "r" ]

    task {
      // Act
      let! result = handler Mocks.chat click

      // Assert
      Assert.Equal(None, result)

      apiClient.VerifyNoOtherCalls()
      botServiceMock.VerifyNoOtherCalls()
    }

type listPresetsClickHandler() =
  let resourceProviderMock = Mock<IResourceProvider>()
  let botServiceMock = Mock<IBotService>()
  let apiClient = Mock<IApiClient>()

  let handler =
    Click.listPresetsClickHandler apiClient.Object resourceProviderMock.Object botServiceMock.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    apiClient.Setup(_.ListPresets()).ReturnsAsync([ Mocks.simplePreset ])
    resourceProviderMock.Setup(fun x -> x[Messages.YourPresets]).Returns(Messages.YourPresets)

    let click = createClick [ CallbackQueryConstants.preset ]

    task {
      // Act
      let! result = handler Mocks.chat click

      // Assert
      Assert.Equal(Some(), result)

      apiClient.VerifyAll()
      botServiceMock.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click = createClick [ "invalid" ]

    task {
      // Act
      let! result = handler Mocks.chat click

      // Assert
      Assert.Equal(None, result)

      apiClient.VerifyNoOtherCalls()
      botServiceMock.VerifyNoOtherCalls()
    }

type artistsAlbumsRecommendationsClickHandler() =
  let apiClient = Mock<IApiClient>()
  let presetService = Mock<ISetRecommendationsEngine>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.artistsAlbumsRecommendationsClickHandler apiClient.Object presetService.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          presetId
          CallbackQueryConstants.artistsAlbumsRecommendations
        ]

    presetService.Setup(_.SetRecommendationsEngine(Mocks.presetId, Some RecommendationsEngine.ArtistAlbums)).ReturnsAsync(())
    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    apiClient.Setup(_.GetPreset(PresetId presetId)).ReturnsAsync(Ok Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns(Notifications.Updated)

    // PresetSettings.show is called, but we don't need to mock its internals for this test

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(Some(), result)

      presetService.VerifyAll()
      botService.VerifyAll()
      apiClient.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(None, result)

      presetService.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
      apiClient.VerifyNoOtherCalls()
    }

type reccoBeatsRecommendationsClickHandler() =
  let apiClient = Mock<IApiClient>()
  let presetService = Mock<ISetRecommendationsEngine>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.reccoBeatsRecommendationsClickHandler apiClient.Object presetService.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          presetId
          CallbackQueryConstants.reccoBeatsRecommendations
        ]

    presetService.Setup(_.SetRecommendationsEngine(Mocks.presetId, Some RecommendationsEngine.ReccoBeats)).ReturnsAsync(())
    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    apiClient.Setup(_.GetPreset(PresetId presetId)).ReturnsAsync(Ok Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns(Notifications.Updated)

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(Some(), result)
      presetService.VerifyAll()
      botService.VerifyAll()
      apiClient.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(None, result)
      presetService.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
      apiClient.VerifyNoOtherCalls()
    }

type spotifyRecommendationsClickHandler() =
  let apiClient = Mock<IApiClient>()
  let presetService = Mock<ISetRecommendationsEngine>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.spotifyRecommendationsClickHandler apiClient.Object presetService.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          presetId
          CallbackQueryConstants.spotifyRecommendations
        ]

    presetService.Setup(_.SetRecommendationsEngine(Mocks.presetId, Some RecommendationsEngine.Spotify)).ReturnsAsync(())
    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    apiClient.Setup(_.GetPreset(PresetId presetId)).ReturnsAsync(Ok Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns(Notifications.Updated)

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(Some(), result)
      presetService.VerifyAll()
      botService.VerifyAll()
      apiClient.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(None, result)
      presetService.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
      apiClient.VerifyNoOtherCalls()
    }

type disableRecommendationsClickHandler() =
  let apiClient = Mock<IApiClient>()
  let presetService = Mock<ISetRecommendationsEngine>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.disableRecommendationsClickHandler apiClient.Object presetService.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          presetId
          CallbackQueryConstants.disableRecommendations
        ]

    presetService.Setup(_.SetRecommendationsEngine(Mocks.presetId, None)).ReturnsAsync(())
    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    apiClient.Setup(_.GetPreset(PresetId presetId)).ReturnsAsync(Ok Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns(Notifications.Updated)

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(Some(), result)
      presetService.VerifyAll()
      botService.VerifyAll()
      apiClient.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(None, result)
      presetService.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
      apiClient.VerifyNoOtherCalls()
    }

type enableUniqueArtistsClickHandler() =
  let apiClient = Mock<IApiClient>()
  let presetService = Mock<IEnableUniqueArtists>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.enableUniqueArtistsClickHandler apiClient.Object presetService.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          presetId
          CallbackQueryConstants.enableUniqueArtists
        ]

    presetService.Setup(_.EnableUniqueArtists(Mocks.presetId)).ReturnsAsync(())
    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    apiClient.Setup(_.GetPreset(PresetId presetId)).ReturnsAsync(Ok Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns(Notifications.Updated)

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(Some(), result)
      presetService.VerifyAll()
      botService.VerifyAll()
      apiClient.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(None, result)
      presetService.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
      apiClient.VerifyNoOtherCalls()
    }

type disableUniqueArtistsClickHandler() =
  let apiClient = Mock<IApiClient>()
  let presetService = Mock<IDisableUniqueArtists>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.disableUniqueArtistsClickHandler apiClient.Object presetService.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          presetId
          CallbackQueryConstants.disableUniqueArtists
        ]

    presetService.Setup(_.DisableUniqueArtists(Mocks.presetId)).ReturnsAsync(())
    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    apiClient.Setup(_.GetPreset(PresetId presetId)).ReturnsAsync(Ok Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns(Notifications.Updated)

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(Some(), result)
      presetService.VerifyAll()
      botService.VerifyAll()
      apiClient.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(None, result)
      presetService.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
      apiClient.VerifyNoOtherCalls()
    }

type includeLikedTracksClickHandler() =
  let apiClient = Mock<IApiClient>()
  let presetService = Mock<IIncludeLikedTracks>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.includeLikedTracksClickHandler apiClient.Object presetService.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          presetId
          CallbackQueryConstants.includeLikedTracks
        ]

    presetService.Setup(_.IncludeLikedTracks(Mocks.presetId)).ReturnsAsync(())
    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    apiClient.Setup(_.GetPreset(PresetId presetId)).ReturnsAsync(Ok Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns(Notifications.Updated)

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(Some(), result)
      presetService.VerifyAll()
      botService.VerifyAll()
      apiClient.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(None, result)
      presetService.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
      apiClient.VerifyNoOtherCalls()
    }

type excludeLikedTracksClickHandler() =
  let apiClient = Mock<IApiClient>()
  let presetService = Mock<IExcludeLikedTracks>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.excludeLikedTracksClickHandler apiClient.Object presetService.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          presetId
          CallbackQueryConstants.excludeLikedTracks
        ]

    presetService.Setup(_.ExcludeLikedTracks(Mocks.presetId)).ReturnsAsync(())
    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    apiClient.Setup(_.GetPreset(PresetId presetId)).ReturnsAsync(Ok Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns(Notifications.Updated)

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(Some(), result)
      presetService.VerifyAll()
      botService.VerifyAll()
      apiClient.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(None, result)
      presetService.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
      apiClient.VerifyNoOtherCalls()
    }


open MusicPlatform

type ignoreLikedTracksClickHandler() =
  let apiClient = Mock<IApiClient>()
  let presetService = Mock<IIgnoreLikedTracks>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.ignoreLikedTracksClickHandler apiClient.Object presetService.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          presetId
          CallbackQueryConstants.ignoreLikedTracks
        ]

    presetService.Setup(_.IgnoreLikedTracks(Mocks.presetId)).ReturnsAsync(())
    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    apiClient.Setup(_.GetPreset(PresetId presetId)).ReturnsAsync(Ok Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns(Notifications.Updated)

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(Some(), result)
      presetService.VerifyAll()
      botService.VerifyAll()
      apiClient.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(None, result)
      presetService.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
      apiClient.VerifyNoOtherCalls()
    }

type overwriteTargetedPlaylistClickHandler() =
  let apiClient = Mock<IApiClient>()
  let presetService = Mock<IPresetService>()
  let musicPlatformFactory = Mock<IMusicPlatformFactory>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.overwriteTargetedPlaylistClickHandler
      apiClient.Object
      presetService.Object
      musicPlatformFactory.Object
      resourceProvider.Object
      botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value
    let playlistId = Mocks.targetedPlaylistId.Value

    let click =
      createClick [ CallbackQueryConstants.preset; presetId; "tp"; playlistId; "o" ]

    presetService.Setup(_.OverwriteTargetedPlaylist(Mocks.presetId, WritablePlaylistId(Mocks.targetedPlaylistId))).ReturnsAsync(())
    musicPlatformFactory.Setup(_.GetMusicPlatform(Mocks.chat.UserId.ToMusicPlatformId())).ReturnsAsync(None)
    apiClient.Setup(_.GetPreset(Mocks.presetId)).ReturnsAsync(Ok Mocks.preset)
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny<string>(), It.IsAny<MessageButtons>())).ReturnsAsync(())

    // TargetedPlaylist.show is called, but we don't need to mock its internals for this test

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(Some(), result)
      presetService.VerifyAll()
      musicPlatformFactory.VerifyAll()
      apiClient.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick
        [
          CallbackQueryConstants.preset
          Mocks.presetId.Value
          "tp"
          Mocks.targetedPlaylistId.Value
          "invalid"
        ]

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(None, result)
      presetService.VerifyNoOtherCalls()
      musicPlatformFactory.VerifyNoOtherCalls()
      apiClient.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
    }

type setCurrentPresetClickHandler() =
  let userService = Mock<ISetCurrentPreset>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<ISendNotification>()

  let handler =
    Click.setCurrentPresetClickHandler userService.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value
    let click = createClick [ CallbackQueryConstants.preset; presetId; "c" ]

    userService.Setup(_.SetCurrentPreset(Mocks.chat.UserId, Mocks.presetId)).ReturnsAsync(())
    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    resourceProvider.Setup(fun x -> x[Notifications.CurrentPresetSet]).Returns(Notifications.CurrentPresetSet)

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(Some(), result)
      userService.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(None, result)
      userService.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
    }

type appendToTargetedPlaylistClickHandler() =
  let apiClient = Mock<IApiClient>()
  let presetService = Mock<IAppendToTargetedPlaylist>()
  let musicPlatformFactory = Mock<IMusicPlatformFactory>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.appendToTargetedPlaylistClickHandler
      apiClient.Object
      presetService.Object
      musicPlatformFactory.Object
      resourceProvider.Object
      botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value
    let playlistId = Mocks.targetedPlaylistId.Value

    let click =
      createClick [ CallbackQueryConstants.preset; presetId; "tp"; playlistId; "a" ]

    presetService.Setup(_.AppendToTargetedPlaylist(Mocks.presetId, WritablePlaylistId(Mocks.targetedPlaylistId))).ReturnsAsync(())
    musicPlatformFactory.Setup(_.GetMusicPlatform(Mocks.chat.UserId.ToMusicPlatformId())).ReturnsAsync(None)
    apiClient.Setup(_.GetPreset(Mocks.presetId)).ReturnsAsync(Ok Mocks.preset)
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny<string>(), It.IsAny<MessageButtons>())).ReturnsAsync(())

    // TargetedPlaylist.show is called, but we don't need to mock its internals for this test

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(Some(), result)
      presetService.VerifyAll()
      musicPlatformFactory.VerifyAll()
      apiClient.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick
        [
          CallbackQueryConstants.preset
          Mocks.presetId.Value
          "tp"
          Mocks.targetedPlaylistId.Value
          "invalid"
        ]

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(None, result)
      presetService.VerifyNoOtherCalls()
      musicPlatformFactory.VerifyNoOtherCalls()
      apiClient.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
    }

type runPresetClickHandler() =
  let apiClient = Mock<IApiClient>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.runPresetClickHandler apiClient.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data and queue preset successfully``() =
    let presetId = Mocks.presetId.Value
    let click = createClick [ CallbackQueryConstants.preset; presetId; "r" ]

    apiClient.Setup(_.RunPreset(Mocks.presetId)).ReturnsAsync(Ok Mocks.simplePreset)
    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    botService.Setup(_.SendMessage(It.IsAny<string>())).ReturnsAsync(Mocks.botMessageId)
    resourceProvider.Setup(fun x -> x[Notifications.PresetQueued, It.IsAny<obj array>()]).Returns(Notifications.PresetQueued)
    resourceProvider.Setup(fun x -> x[Messages.PresetQueued, It.IsAny<obj array>()]).Returns(Messages.NoIncludedPlaylists)

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(Some(), result)
      apiClient.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member _.``should handle validation error NoIncludedPlaylists``() =
    let presetId = Mocks.presetId.Value
    let click = createClick [ CallbackQueryConstants.preset; presetId; "r" ]

    apiClient.Setup(_.RunPreset(Mocks.presetId)).ReturnsAsync(Error [ Preset.ValidationError.NoIncludedPlaylists ])
    botService.Setup(_.SendMessage(It.IsAny<string>())).ReturnsAsync(Mocks.botMessageId)
    resourceProvider.Setup(fun x -> x[Messages.NoIncludedPlaylists]).Returns(Messages.NoIncludedPlaylists)

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(Some(), result)
      apiClient.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member _.``should handle validation error NoTargetedPlaylists``() =
    let presetId = Mocks.presetId.Value
    let click = createClick [ CallbackQueryConstants.preset; presetId; "r" ]

    apiClient.Setup(_.RunPreset(Mocks.presetId)).ReturnsAsync(Error [ Preset.ValidationError.NoTargetedPlaylists ])
    botService.Setup(_.SendMessage(It.IsAny<string>())).ReturnsAsync(Mocks.botMessageId)
    resourceProvider.Setup(fun x -> x[Messages.NoTargetedPlaylists]).Returns(Messages.NoTargetedPlaylists)

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(Some(), result)
      apiClient.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(None, result)
      apiClient.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
    }

type presetSettingsClickHandler() =
  let apiClient = Mock<IApiClient>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.presetSettingsClickHandler apiClient.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value
    let click = createClick [ CallbackQueryConstants.preset; presetId; "s" ]

    apiClient.Setup(_.GetPreset(Mocks.presetId)).ReturnsAsync(Ok Mocks.preset)
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny<string>(), It.IsAny<MessageButtons>())).ReturnsAsync(())

    // PresetSettings.show is called, but we don't need to mock its internals for this test

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(Some(), result)
      apiClient.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(None, result)
      apiClient.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
    }

type setOnlyLikedIncludedPlaylistClickHandler() =
  let apiClient = Mock<IApiClient>()
  let presetService = Mock<ISetOnlyLiked>()
  let musicPlatformFactory = Mock<IMusicPlatformFactory>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.setOnlyLikedIncludedPlaylistClickHandler
      apiClient.Object
      presetService.Object
      musicPlatformFactory.Object
      resourceProvider.Object
      botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let playlistId = Mocks.includedPlaylistId.Value

    let preset =
      { Mocks.preset with
          IncludedPlaylists = [ Mocks.includedPlaylist ]
      }

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          Mocks.presetId.Value
          CallbackQueryConstants.includedPlaylists
          playlistId
          "o"
        ]

    presetService.Setup(_.SetOnlyLiked(Mocks.presetId, ReadablePlaylistId(Mocks.includedPlaylistId))).ReturnsAsync(())
    musicPlatformFactory.Setup(_.GetMusicPlatform(Mocks.chat.UserId.ToMusicPlatformId())).ReturnsAsync(None)
    apiClient.Setup(_.GetPreset(Mocks.presetId)).ReturnsAsync(Ok preset)
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny<string>(), It.IsAny<MessageButtons>())).ReturnsAsync(())

    // IncludedPlaylist.show is called, but we don't need to mock its internals for this test

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(Some(), result)
      presetService.VerifyAll()
      musicPlatformFactory.VerifyAll()
      apiClient.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick
        [
          CallbackQueryConstants.preset
          Mocks.presetId.Value
          CallbackQueryConstants.includedPlaylists
          Mocks.includedPlaylistId.Value
          "invalid"
        ]

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(None, result)
      presetService.VerifyNoOtherCalls()
      musicPlatformFactory.VerifyNoOtherCalls()
      apiClient.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
    }

type setAllTracksIncludedPlaylistClickHandler() =
  let apiClient = Mock<IApiClient>()
  let presetService = Mock<ISetAll>()
  let musicPlatformFactory = Mock<IMusicPlatformFactory>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.setAllTracksIncludedPlaylistClickHandler
      apiClient.Object
      presetService.Object
      musicPlatformFactory.Object
      resourceProvider.Object
      botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let playlistId = Mocks.includedPlaylistId.Value

    let preset =
      { Mocks.preset with
          IncludedPlaylists = [ Mocks.includedPlaylist ]
      }

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          Mocks.presetId.Value
          CallbackQueryConstants.includedPlaylists
          playlistId
          "a"
        ]

    presetService.Setup(_.SetAll(Mocks.presetId, ReadablePlaylistId(Mocks.includedPlaylistId))).ReturnsAsync(())
    musicPlatformFactory.Setup(_.GetMusicPlatform(Mocks.chat.UserId.ToMusicPlatformId())).ReturnsAsync(None)
    apiClient.Setup(_.GetPreset(Mocks.presetId)).ReturnsAsync(Ok preset)
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny<string>(), It.IsAny<MessageButtons>())).ReturnsAsync(())

    // IncludedPlaylist.show is called, but we don't need to mock its internals for this test

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(Some(), result)
      presetService.VerifyAll()
      musicPlatformFactory.VerifyAll()
      apiClient.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick
        [
          CallbackQueryConstants.preset
          Mocks.presetId.Value
          CallbackQueryConstants.includedPlaylists
          Mocks.includedPlaylistId.Value
          "invalid"
        ]

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(None, result)
      presetService.VerifyNoOtherCalls()
      musicPlatformFactory.VerifyNoOtherCalls()
      apiClient.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
    }

type removePresetClickHandler() =
  let apiClient = Mock<IApiClient>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.removePresetClickHandler apiClient.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle successful remove and list presets``() =
    let presetId = Mocks.presetId.Value
    let click = createClick [ CallbackQueryConstants.preset; presetId; "rm" ]

    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    apiClient.Setup(_.RemovePreset(Mocks.presetId)).ReturnsAsync(Result<unit, Preset.GetPresetError>.Ok())
    apiClient.Setup(_.ListPresets()).ReturnsAsync([ Mocks.simplePreset ])
    resourceProvider.Setup(fun x -> x[Notifications.PresetRemoved]).Returns(Notifications.PresetRemoved)
    resourceProvider.Setup(fun x -> x[Messages.YourPresets]).Returns(Messages.YourPresets)
    // When listing presets, the handler will call EditMessageButtons via the bot service - mock it
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny<string>(), It.IsAny<MessageButtons>())).ReturnsAsync(())

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(Some(), result)

      botService.VerifyAll()
      apiClient.VerifyAll()
    }

  [<Fact>]
  member _.``should notify when preset not found``() =
    let presetId = Mocks.presetId.Value
    let click = createClick [ CallbackQueryConstants.preset; presetId; "rm" ]

    apiClient.Setup(_.RemovePreset(Mocks.presetId)).ReturnsAsync(Result<unit, Preset.GetPresetError>.Error Preset.GetPresetError.NotFound)

    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    resourceProvider.Setup(fun x -> x[Notifications.PresetNotFound]).Returns(Notifications.PresetNotFound)

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(Some(), result)

      apiClient.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(None, result)

      botService.VerifyNoOtherCalls()
      apiClient.VerifyNoOtherCalls()
    }

type showIncludedContentClickHandler() =
  let apiClient = Mock<IApiClient>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.showIncludedContentClickHandler apiClient.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          presetId
          CallbackQueryConstants.includedContent
        ]

    apiClient.Setup(_.GetPreset(Mocks.presetId)).ReturnsAsync(Ok Mocks.preset)
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny<string>(), It.IsAny<MessageButtons>())).ReturnsAsync(())
    resourceProvider.Setup(fun x -> x[Buttons.Playlists, It.IsAny<obj array>()]).Returns(Buttons.Playlists)
    resourceProvider.Setup(fun x -> x[Buttons.Artists]).Returns(Buttons.Artists)
    resourceProvider.Setup(fun x -> x[Buttons.Back]).Returns(Buttons.Back)

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(Some(), result)

      apiClient.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(None, result)

      apiClient.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
    }

type showExcludedContentClickHandler() =
  let apiClient = Mock<IApiClient>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.showExcludedContentClickHandler apiClient.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          presetId
          CallbackQueryConstants.excludedContent
        ]

    apiClient.Setup(_.GetPreset(Mocks.presetId)).ReturnsAsync(Ok Mocks.preset)
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny<string>(), It.IsAny<MessageButtons>())).ReturnsAsync(())

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(Some(), result)

      apiClient.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

    task {
      let! result = handler Mocks.chat click
      Assert.Equal(None, result)

      apiClient.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
    }

type listExcludedArtistsClickHandler() =
  let apiClient = Mock<IApiClient>()
  let resourceProviderMock = Mock<IResourceProvider>()
  let botServiceMock = Mock<IBotService>()

  let handler =
    Click.listExcludedArtistsClickHandler apiClient.Object resourceProviderMock.Object botServiceMock.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    apiClient.Setup(_.GetPreset(It.IsAny<PresetId>())).ReturnsAsync(Ok Mocks.preset)
    resourceProviderMock.Setup(fun x -> x[Messages.ExcludedArtists]).Returns(Messages.ExcludedArtists)

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          Mocks.presetId.Value
          CallbackQueryConstants.excludedArtists
          string 0
        ]

    task {
      // Act
      let! result = handler Mocks.chat click

      // Assert
      Assert.Equal(Some(), result)

      apiClient.VerifyAll()
      botServiceMock.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click = createClick [ "invalid" ]

    task {
      // Act
      let! result = handler Mocks.chat click

      // Assert
      Assert.Equal(None, result)

      apiClient.VerifyNoOtherCalls()
      botServiceMock.VerifyNoOtherCalls()
    }

type showExcludedArtistClickHandler() =
  let apiClient = Mock<IApiClient>()
  let resourceProviderMock = Mock<IResourceProvider>()
  let botServiceMock = Mock<IBotService>()

  let handler =
    Click.showExcludedArtistClickHandler apiClient.Object resourceProviderMock.Object botServiceMock.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let preset =
      { Mocks.preset with
          ExcludedArtists = [ Mocks.artist2 ]
      }

    apiClient.Setup(_.GetPreset(It.IsAny<PresetId>())).ReturnsAsync(Ok preset)
    resourceProviderMock.Setup(fun x -> x[Messages.ExcludedArtists]).Returns(Messages.ExcludedArtists)

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          Mocks.presetId.Value
          CallbackQueryConstants.excludedArtists
          Mocks.artist2.Id.Value
          "i"
        ]

    task {
      // Act
      let! result = handler Mocks.chat click

      // Assert
      Assert.Equal(Some(), result)

      apiClient.VerifyAll()
      botServiceMock.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click = createClick [ "invalid" ]

    task {
      // Act
      let! result = handler Mocks.chat click

      // Assert
      Assert.Equal(None, result)

      apiClient.VerifyNoOtherCalls()
      botServiceMock.VerifyNoOtherCalls()
    }

type removeExcludedArtistClickHandler() =
  let apiClient = Mock<IApiClient>()
  let resourceProviderMock = Mock<IResourceProvider>()
  let botServiceMock = Mock<IEditMessageButtons>()

  let handler =
    Click.removeExcludedArtistClickHandler apiClient.Object resourceProviderMock.Object botServiceMock.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    apiClient.Setup(_.RemoveExcludedArtist(It.IsAny<PresetId>(), It.IsAny<ArtistId>())).ReturnsAsync(Mocks.preset)
    resourceProviderMock.Setup(fun x -> x[Notifications.ExcludedArtistRemoved]).Returns(Notifications.ExcludedArtistRemoved)

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          Mocks.presetId.Value
          CallbackQueryConstants.excludedArtists
          Mocks.artist2.Id.Value
          "rm"
        ]

    task {
      // Act
      let! result = handler Mocks.chat click

      // Assert
      Assert.Equal(Some(), result)

      apiClient.VerifyAll()
      botServiceMock.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click = createClick [ "invalid" ]

    task {
      // Act
      let! result = handler Mocks.chat click

      // Assert
      Assert.Equal(None, result)

      apiClient.VerifyNoOtherCalls()
      botServiceMock.VerifyNoOtherCalls()
    }

type showIncludedArtistClickHandler() =
  let apiClient = Mock<IApiClient>()
  let resourceProviderMock = Mock<IResourceProvider>()
  let botServiceMock = Mock<IBotService>()

  let handler =
    Click.showIncludedArtistClickHandler apiClient.Object resourceProviderMock.Object botServiceMock.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let preset =
      { Mocks.preset with
          IncludedArtists = [ Mocks.artist1 ]
      }

    apiClient.Setup(_.GetPreset(Mocks.presetId)).ReturnsAsync(Ok preset)
    resourceProviderMock.Setup(fun x -> x[Messages.IncludedArtists]).Returns(Messages.IncludedArtists)

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          Mocks.presetId.Value
          CallbackQueryConstants.includedArtists
          Mocks.artist1.Id.Value
          "i"
        ]

    task {
      // Act
      let! result = handler Mocks.chat click

      // Assert
      Assert.Equal(Some(), result)

      apiClient.VerifyAll()
      botServiceMock.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click = createClick [ "invalid" ]

    task {
      // Act
      let! result = handler Mocks.chat click

      // Assert
      Assert.Equal(None, result)

      apiClient.VerifyNoOtherCalls()
      botServiceMock.VerifyNoOtherCalls()
    }

type removeIncludedArtistClickHandler() =
  let apiClient = Mock<IApiClient>()
  let resourceProviderMock = Mock<IResourceProvider>()
  let botServiceMock = Mock<IEditMessageButtons>()

  let handler =
    Click.removeIncludedArtistClickHandler apiClient.Object resourceProviderMock.Object botServiceMock.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    apiClient.Setup(_.RemoveIncludedArtist(It.IsAny<PresetId>(), It.IsAny<ArtistId>())).ReturnsAsync(Mocks.preset)
    resourceProviderMock.Setup(fun x -> x[Notifications.IncludedArtistRemoved]).Returns(Notifications.IncludedArtistRemoved)

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          Mocks.presetId.Value
          CallbackQueryConstants.includedArtists
          Mocks.artist1.Id.Value
          "rm"
        ]

    task {
      // Act
      let! result = handler Mocks.chat click

      // Assert
      Assert.Equal(Some(), result)

      apiClient.VerifyAll()
      botServiceMock.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click = createClick [ "invalid" ]

    task {
      // Act
      let! result = handler Mocks.chat click

      // Assert
      Assert.Equal(None, result)

      apiClient.VerifyNoOtherCalls()
      botServiceMock.VerifyNoOtherCalls()
    }

type listIncludedArtistsClickHandler() =
  let apiClient = Mock<IApiClient>()
  let resourceProviderMock = Mock<IResourceProvider>()
  let botServiceMock = Mock<IBotService>()

  let handler =
    Click.listIncludedArtistsClickHandler apiClient.Object resourceProviderMock.Object botServiceMock.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    apiClient.Setup(_.GetPreset(It.IsAny<PresetId>())).ReturnsAsync(Ok Mocks.preset)
    resourceProviderMock.Setup(fun x -> x[Messages.IncludedArtists]).Returns(Messages.IncludedArtists)

    let click =
      createClick
        [
          CallbackQueryConstants.preset
          Mocks.presetId.Value
          CallbackQueryConstants.includedArtists
          "0"
        ]

    task {
      // Act
      let! result = handler Mocks.chat click

      // Assert
      Assert.Equal(Some(), result)

      apiClient.VerifyAll()
      botServiceMock.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click = createClick [ "invalid" ]

    task {
      // Act
      let! result = handler Mocks.chat click

      // Assert
      Assert.Equal(None, result)

      apiClient.VerifyNoOtherCalls()
      botServiceMock.VerifyNoOtherCalls()
    }