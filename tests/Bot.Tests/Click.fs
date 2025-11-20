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
    // Arrange

    presetRepoMock.Setup(_.LoadPreset(It.IsAny<PresetId>())).ReturnsAsync(Some Mocks.preset)

    let click = createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "i" ]

    task {
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

    let click = createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "r" ]

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

    let click = createClick [ CallbackQueryConstants.preset ]

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

    let click =
      createClick
        [ CallbackQueryConstants.preset
          presetId
          CallbackQueryConstants.artistsAlbumsRecommendations ]

    presetService.Setup(_.SetRecommendationsEngine(Mocks.presetId, Some RecommendationsEngine.ArtistAlbums)).ReturnsAsync(())
    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    presetRepo.Setup(_.LoadPreset(PresetId presetId)).ReturnsAsync(Some Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns(Notifications.Updated)

    // PresetSettings.show is called, but we don't need to mock its internals for this test

    task {
      let! result = handler click
      result |> should equal (Some())

      presetService.VerifyAll()
      botService.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

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

    let click =
      createClick
        [ CallbackQueryConstants.preset
          presetId
          CallbackQueryConstants.reccoBeatsRecommendations ]

    presetService.Setup(_.SetRecommendationsEngine(Mocks.presetId, Some RecommendationsEngine.ReccoBeats)).ReturnsAsync(())
    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    presetRepo.Setup(_.LoadPreset(PresetId presetId)).ReturnsAsync(Some Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns(Notifications.Updated)

    task {
      let! result = handler click
      result |> should equal (Some())
      presetService.VerifyAll()
      botService.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

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

    let click =
      createClick
        [ CallbackQueryConstants.preset
          presetId
          CallbackQueryConstants.spotifyRecommendations ]

    presetService.Setup(_.SetRecommendationsEngine(Mocks.presetId, Some RecommendationsEngine.Spotify)).ReturnsAsync(())
    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    presetRepo.Setup(_.LoadPreset(PresetId presetId)).ReturnsAsync(Some Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns(Notifications.Updated)

    task {
      let! result = handler click
      result |> should equal (Some())
      presetService.VerifyAll()
      botService.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

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

    let click =
      createClick
        [ CallbackQueryConstants.preset
          presetId
          CallbackQueryConstants.disableRecommendations ]

    presetService.Setup(_.SetRecommendationsEngine(Mocks.presetId, None)).ReturnsAsync(())
    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    presetRepo.Setup(_.LoadPreset(PresetId presetId)).ReturnsAsync(Some Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns(Notifications.Updated)

    task {
      let! result = handler click
      result |> should equal (Some())
      presetService.VerifyAll()
      botService.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

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

    let click =
      createClick
        [ CallbackQueryConstants.preset
          presetId
          CallbackQueryConstants.enableUniqueArtists ]

    presetService.Setup(_.EnableUniqueArtists(Mocks.presetId)).ReturnsAsync(())
    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    presetRepo.Setup(_.LoadPreset(PresetId presetId)).ReturnsAsync(Some Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns(Notifications.Updated)

    task {
      let! result = handler click
      result |> should equal (Some())
      presetService.VerifyAll()
      botService.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

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

    let click =
      createClick
        [ CallbackQueryConstants.preset
          presetId
          CallbackQueryConstants.disableUniqueArtists ]

    presetService.Setup(_.DisableUniqueArtists(Mocks.presetId)).ReturnsAsync(())
    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    presetRepo.Setup(_.LoadPreset(PresetId presetId)).ReturnsAsync(Some Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns(Notifications.Updated)

    task {
      let! result = handler click
      result |> should equal (Some())
      presetService.VerifyAll()
      botService.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

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

    let click =
      createClick
        [ CallbackQueryConstants.preset
          presetId
          CallbackQueryConstants.includeLikedTracks ]

    presetService.Setup(_.IncludeLikedTracks(Mocks.presetId)).ReturnsAsync(())
    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    presetRepo.Setup(_.LoadPreset(PresetId presetId)).ReturnsAsync(Some Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns(Notifications.Updated)

    task {
      let! result = handler click
      result |> should equal (Some())
      presetService.VerifyAll()
      botService.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

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

    let click =
      createClick
        [ CallbackQueryConstants.preset
          presetId
          CallbackQueryConstants.excludeLikedTracks ]

    presetService.Setup(_.ExcludeLikedTracks(Mocks.presetId)).ReturnsAsync(())
    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    presetRepo.Setup(_.LoadPreset(PresetId presetId)).ReturnsAsync(Some Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns(Notifications.Updated)

    task {
      let! result = handler click
      result |> should equal (Some())
      presetService.VerifyAll()
      botService.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

    task {
      let! result = handler click
      result |> should equal None
      presetService.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
    }


open MusicPlatform

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

    let click =
      createClick
        [ CallbackQueryConstants.preset
          presetId
          CallbackQueryConstants.ignoreLikedTracks ]

    presetService.Setup(_.IgnoreLikedTracks(Mocks.presetId)).ReturnsAsync(())
    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    presetRepo.Setup(_.LoadPreset(PresetId presetId)).ReturnsAsync(Some Mocks.preset)
    resourceProvider.Setup(fun x -> x[Notifications.Updated]).Returns(Notifications.Updated)

    task {
      let! result = handler click
      result |> should equal (Some())
      presetService.VerifyAll()
      botService.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

    task {
      let! result = handler click
      result |> should equal None
      presetService.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
    }

type overwriteTargetedPlaylistClickHandler() =
  let presetRepo = Mock<IPresetRepo>()
  let presetService = Mock<IPresetService>()
  let musicPlatformFactory = Mock<IMusicPlatformFactory>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.overwriteTargetedPlaylistClickHandler
      presetRepo.Object
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
    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny<string>(), It.IsAny<MessageButtons>())).ReturnsAsync(())

    // TargetedPlaylist.show is called, but we don't need to mock its internals for this test

    task {
      let! result = handler click
      result |> should equal (Some())
      presetService.VerifyAll()
      musicPlatformFactory.VerifyAll()
      presetRepo.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick
        [ CallbackQueryConstants.preset
          Mocks.presetId.Value
          "tp"
          Mocks.targetedPlaylistId.Value
          "invalid" ]

    task {
      let! result = handler click
      result |> should equal None
      presetService.VerifyNoOtherCalls()
      musicPlatformFactory.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
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
      let! result = handler click
      result |> should equal (Some())
      userService.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

    task {
      let! result = handler click
      result |> should equal None
      userService.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
    }

type appendToTargetedPlaylistClickHandler() =
  let presetRepo = Mock<IPresetRepo>()
  let presetService = Mock<IAppendToTargetedPlaylist>()
  let musicPlatformFactory = Mock<IMusicPlatformFactory>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.appendToTargetedPlaylistClickHandler
      presetRepo.Object
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
    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny<string>(), It.IsAny<MessageButtons>())).ReturnsAsync(())

    // TargetedPlaylist.show is called, but we don't need to mock its internals for this test

    task {
      let! result = handler click
      result |> should equal (Some())
      presetService.VerifyAll()
      musicPlatformFactory.VerifyAll()
      presetRepo.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick
        [ CallbackQueryConstants.preset
          Mocks.presetId.Value
          "tp"
          Mocks.targetedPlaylistId.Value
          "invalid" ]

    task {
      let! result = handler click
      result |> should equal None
      presetService.VerifyNoOtherCalls()
      musicPlatformFactory.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
    }

type runPresetClickHandler() =
  let presetService = Mock<Domain.Core.IQueueRun>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.runPresetClickHandler presetService.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data and queue preset successfully``() =
    let presetId = Mocks.presetId.Value
    let click = createClick [ CallbackQueryConstants.preset; presetId; "r" ]

    presetService.Setup(_.QueueRun(Mocks.chat.UserId, Mocks.presetId)).ReturnsAsync(Ok Mocks.preset)
    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    botService.Setup(_.SendMessage(It.IsAny<string>())).ReturnsAsync(Mocks.botMessageId)
    resourceProvider.Setup(fun x -> x[Notifications.PresetQueued, It.IsAny<obj array>()]).Returns(Notifications.PresetQueued)
    resourceProvider.Setup(fun x -> x[Messages.PresetQueued, It.IsAny<obj array>()]).Returns(Messages.NoIncludedPlaylists)

    task {
      let! result = handler click
      result |> should equal (Some())
      presetService.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member _.``should handle validation error NoIncludedPlaylists``() =
    let presetId = Mocks.presetId.Value
    let click = createClick [ CallbackQueryConstants.preset; presetId; "r" ]

    presetService.Setup(_.QueueRun(Mocks.chat.UserId, Mocks.presetId)).ReturnsAsync(Error [ Preset.ValidationError.NoIncludedPlaylists ])
    botService.Setup(_.SendMessage(It.IsAny<string>())).ReturnsAsync(Mocks.botMessageId)
    resourceProvider.Setup(fun x -> x[Messages.NoIncludedPlaylists]).Returns(Messages.NoIncludedPlaylists)

    task {
      let! result = handler click
      result |> should equal (Some())
      presetService.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member _.``should handle validation error NoTargetedPlaylists``() =
    let presetId = Mocks.presetId.Value
    let click = createClick [ CallbackQueryConstants.preset; presetId; "r" ]

    presetService.Setup(_.QueueRun(Mocks.chat.UserId, Mocks.presetId)).ReturnsAsync(Error [ Preset.ValidationError.NoTargetedPlaylists ])
    botService.Setup(_.SendMessage(It.IsAny<string>())).ReturnsAsync(Mocks.botMessageId)
    resourceProvider.Setup(fun x -> x[Messages.NoTargetedPlaylists]).Returns(Messages.NoTargetedPlaylists)

    task {
      let! result = handler click
      result |> should equal (Some())
      presetService.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

    task {
      let! result = handler click
      result |> should equal None
      presetService.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
    }

type presetSettingsClickHandler() =
  let presetRepo = Mock<IPresetRepo>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IEditMessageButtons>()

  let handler =
    Click.presetSettingsClickHandler presetRepo.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value
    let click = createClick [ CallbackQueryConstants.preset; presetId; "s" ]

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny<string>(), It.IsAny<MessageButtons>())).ReturnsAsync(())

    // PresetSettings.show is called, but we don't need to mock its internals for this test

    task {
      let! result = handler click
      result |> should equal (Some())
      presetRepo.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

    task {
      let! result = handler click
      result |> should equal None
      presetRepo.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
    }

type setOnlyLikedIncludedPlaylistClickHandler() =
  let presetRepo = Mock<IPresetRepo>()
  let presetService = Mock<ISetOnlyLiked>()
  let musicPlatformFactory = Mock<IMusicPlatformFactory>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.setOnlyLikedIncludedPlaylistClickHandler
      presetRepo.Object
      presetService.Object
      musicPlatformFactory.Object
      resourceProvider.Object
      botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value
    let playlistId = Mocks.includedPlaylistId.Value

    let click =
      createClick
        [ CallbackQueryConstants.preset
          presetId
          CallbackQueryConstants.includedPlaylists
          playlistId
          "o" ]

    presetService.Setup(_.SetOnlyLiked(Mocks.presetId, ReadablePlaylistId(Mocks.includedPlaylistId))).ReturnsAsync(())
    musicPlatformFactory.Setup(_.GetMusicPlatform(Mocks.chat.UserId.ToMusicPlatformId())).ReturnsAsync(None)
    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny<string>(), It.IsAny<MessageButtons>())).ReturnsAsync(())

    // IncludedPlaylist.show is called, but we don't need to mock its internals for this test

    task {
      let! result = handler click
      result |> should equal (Some())
      presetService.VerifyAll()
      musicPlatformFactory.VerifyAll()
      presetRepo.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick
        [ CallbackQueryConstants.preset
          Mocks.presetId.Value
          CallbackQueryConstants.includedPlaylists
          Mocks.includedPlaylistId.Value
          "invalid" ]

    task {
      let! result = handler click
      result |> should equal None
      presetService.VerifyNoOtherCalls()
      musicPlatformFactory.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
    }

type setAllTracksIncludedPlaylistClickHandler() =
  let presetRepo = Mock<IPresetRepo>()
  let presetService = Mock<ISetAll>()
  let musicPlatformFactory = Mock<IMusicPlatformFactory>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.setAllTracksIncludedPlaylistClickHandler
      presetRepo.Object
      presetService.Object
      musicPlatformFactory.Object
      resourceProvider.Object
      botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value
    let playlistId = Mocks.includedPlaylistId.Value

    let click =
      createClick
        [ CallbackQueryConstants.preset
          presetId
          CallbackQueryConstants.includedPlaylists
          playlistId
          "a" ]

    presetService.Setup(_.SetAll(Mocks.presetId, ReadablePlaylistId(Mocks.includedPlaylistId))).ReturnsAsync(())
    musicPlatformFactory.Setup(_.GetMusicPlatform(Mocks.chat.UserId.ToMusicPlatformId())).ReturnsAsync(None)
    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny<string>(), It.IsAny<MessageButtons>())).ReturnsAsync(())

    // IncludedPlaylist.show is called, but we don't need to mock its internals for this test

    task {
      let! result = handler click
      result |> should equal (Some())
      presetService.VerifyAll()
      musicPlatformFactory.VerifyAll()
      presetRepo.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick
        [ CallbackQueryConstants.preset
          Mocks.presetId.Value
          CallbackQueryConstants.includedPlaylists
          Mocks.includedPlaylistId.Value
          "invalid" ]

    task {
      let! result = handler click
      result |> should equal None
      presetService.VerifyNoOtherCalls()
      musicPlatformFactory.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
    }

type removePresetClickHandler() =
  let presetRepo = Mock<IPresetRepo>()
  let userService = Mock<IRemoveUserPreset>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.removePresetClickHandler presetRepo.Object userService.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle successful remove and list presets``() =
    let presetId = Mocks.presetId.Value
    let click = createClick [ CallbackQueryConstants.preset; presetId; "rm" ]

    // RemoveUserPreset is called with RawPresetId constructed from the click data; match any RawPresetId
    userService.Setup(_.RemoveUserPreset(Mocks.chat.UserId, It.IsAny<RawPresetId>())).ReturnsAsync(Ok())
    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    presetRepo.Setup(_.ListUserPresets(Mocks.chat.UserId)).ReturnsAsync([ Mocks.simplePreset ])
    resourceProvider.Setup(fun x -> x[Notifications.PresetRemoved]).Returns(Notifications.PresetRemoved)
    resourceProvider.Setup(fun x -> x[Messages.YourPresets]).Returns(Messages.YourPresets)
    // When listing presets, the handler will call EditMessageButtons via the bot service - mock it
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny<string>(), It.IsAny<MessageButtons>())).ReturnsAsync(())

    task {
      let! result = handler click
      result |> should equal (Some())

      userService.VerifyAll()
      botService.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``should notify when preset not found``() =
    let presetId = Mocks.presetId.Value
    let click = createClick [ CallbackQueryConstants.preset; presetId; "rm" ]

    userService.Setup(_.RemoveUserPreset(Mocks.chat.UserId, It.IsAny<RawPresetId>())).ReturnsAsync(Error Preset.GetPresetError.NotFound)
    botService.Setup(_.SendNotification(Mocks.clickId, It.IsAny<string>())).ReturnsAsync(())
    resourceProvider.Setup(fun x -> x[Notifications.PresetNotFound]).Returns(Notifications.PresetNotFound)

    task {
      let! result = handler click
      result |> should equal (Some())

      userService.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

    task {
      let! result = handler click
      result |> should equal None

      userService.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
      presetRepo.VerifyNoOtherCalls()
    }

type showIncludedContentClickHandler() =
  let presetRepo = Mock<IPresetRepo>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.showIncludedContentClickHandler presetRepo.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value

    let click =
      createClick
        [ CallbackQueryConstants.preset
          presetId
          CallbackQueryConstants.includedContent ]

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny<string>(), It.IsAny<MessageButtons>())).ReturnsAsync(())
    resourceProvider.Setup(fun x -> x[Buttons.Playlists, It.IsAny<obj array>()]).Returns(Buttons.Playlists)
    resourceProvider.Setup(fun x -> x[Buttons.Artists]).Returns(Buttons.Artists)
    resourceProvider.Setup(fun x -> x[Buttons.Back]).Returns(Buttons.Back)

    task {
      let! result = handler click
      result |> should equal (Some())

      presetRepo.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

    task {
      let! result = handler click
      result |> should equal None

      presetRepo.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
    }

type showExcludedContentClickHandler() =
  let presetRepo = Mock<IPresetRepo>()
  let resourceProvider = Mock<IResourceProvider>()
  let botService = Mock<IBotService>()

  let handler =
    Click.showExcludedContentClickHandler presetRepo.Object resourceProvider.Object botService.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    let presetId = Mocks.presetId.Value

    let click =
      createClick
        [ CallbackQueryConstants.preset
          presetId
          CallbackQueryConstants.excludedContent ]

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)
    botService.Setup(_.EditMessageButtons(Mocks.botMessageId, It.IsAny<string>(), It.IsAny<MessageButtons>())).ReturnsAsync(())

    task {
      let! result = handler click
      result |> should equal (Some())

      presetRepo.VerifyAll()
      botService.VerifyAll()
    }

  [<Fact>]
  member _.``should return None for invalid click data``() =
    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; "invalid" ]

    task {
      let! result = handler click
      result |> should equal None

      presetRepo.VerifyNoOtherCalls()
      botService.VerifyNoOtherCalls()
    }

type listExcludedArtistsClickHandler() =
  let presetRepo = Mock<ILoadPreset>()
  let resourceProviderMock = Mock<IResourceProvider>()
  let botServiceMock = Mock<IBotService>()

  let handler =
    Click.listExcludedArtistsClickHandler presetRepo.Object resourceProviderMock.Object botServiceMock.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    presetRepo.Setup(_.LoadPreset(It.IsAny<PresetId>())).ReturnsAsync(Some Mocks.preset)
    resourceProviderMock.Setup(fun x -> x[Messages.ExcludedArtists]).Returns(Messages.ExcludedArtists)

    let click =
      createClick
        [ CallbackQueryConstants.preset
          Mocks.presetId.Value
          CallbackQueryConstants.excludedArtists
          string 0 ]

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

type showExcludedArtistClickHandler() =
  let presetRepo = Mock<ILoadPreset>()

  let resourceProviderMock = Mock<IResourceProvider>()
  let botServiceMock = Mock<IEditMessageButtons>()

  let handler =
    Click.showExcludedArtistClickHandler presetRepo.Object resourceProviderMock.Object botServiceMock.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    presetRepo.Setup(_.LoadPreset(It.IsAny<PresetId>())).ReturnsAsync(Some Mocks.preset)
    resourceProviderMock.Setup(fun x -> x[Messages.ExcludedArtists]).Returns(Messages.ExcludedArtists)

    let click =
      createClick
        [ CallbackQueryConstants.preset
          Mocks.presetId.Value
          CallbackQueryConstants.excludedArtists
          Mocks.artist2.Id.Value
          "i" ]

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

type removeExcludedArtistClickHandler() =
  let presetService = Mock<IRemoveExcludedArtist>()
  let resourceProviderMock = Mock<IResourceProvider>()
  let botServiceMock = Mock<IEditMessageButtons>()

  let handler =
    Click.removeExcludedArtistClickHandler presetService.Object resourceProviderMock.Object botServiceMock.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    presetService.Setup(_.RemoveExcludedArtist(It.IsAny<PresetId>(), It.IsAny<ArtistId>())).ReturnsAsync(Mocks.preset)
    resourceProviderMock.Setup(fun x -> x[Notifications.ExcludedArtistRemoved]).Returns(Notifications.ExcludedArtistRemoved)

    let click =
      createClick
        [ CallbackQueryConstants.preset
          Mocks.presetId.Value
          CallbackQueryConstants.excludedArtists
          Mocks.artist2.Id.Value
          "rm" ]

    task {
      // Act
      let! result = handler click

      // Assert
      result |> should equal (Some())

      presetService.VerifyAll()
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

      presetService.VerifyNoOtherCalls()
      botServiceMock.VerifyNoOtherCalls()
    }

type showIncludedArtistClickHandler() =
  let presetRepo = Mock<ILoadPreset>()

  let resourceProviderMock = Mock<IResourceProvider>()
  let botServiceMock = Mock<IEditMessageButtons>()

  let handler =
    Click.showIncludedArtistClickHandler presetRepo.Object resourceProviderMock.Object botServiceMock.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    presetRepo.Setup(_.LoadPreset(It.IsAny<PresetId>())).ReturnsAsync(Some Mocks.preset)
    resourceProviderMock.Setup(fun x -> x[Messages.IncludedArtists]).Returns(Messages.IncludedArtists)

    let click =
      createClick
        [ CallbackQueryConstants.preset
          Mocks.presetId.Value
          CallbackQueryConstants.includedArtists
          Mocks.artist1.Id.Value
          "i" ]

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

type removeIncludedArtistClickHandler() =
  let presetService = Mock<IRemoveIncludedArtist>()
  let resourceProviderMock = Mock<IResourceProvider>()
  let botServiceMock = Mock<IEditMessageButtons>()

  let handler =
    Click.removeIncludedArtistClickHandler presetService.Object resourceProviderMock.Object botServiceMock.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    presetService.Setup(_.RemoveIncludedArtist(It.IsAny<PresetId>(), It.IsAny<ArtistId>())).ReturnsAsync(Mocks.preset)
    resourceProviderMock.Setup(fun x -> x[Notifications.IncludedArtistRemoved]).Returns(Notifications.IncludedArtistRemoved)

    let click =
      createClick
        [ CallbackQueryConstants.preset
          Mocks.presetId.Value
          CallbackQueryConstants.includedArtists
          Mocks.artist1.Id.Value
          "rm" ]

    task {
      // Act
      let! result = handler click

      // Assert
      result |> should equal (Some())

      presetService.VerifyAll()
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

      presetService.VerifyNoOtherCalls()
      botServiceMock.VerifyNoOtherCalls()
    }

type listIncludedArtistsClickHandler() =
  let presetRepo = Mock<ILoadPreset>()
  let resourceProviderMock = Mock<IResourceProvider>()
  let botServiceMock = Mock<IEditMessageButtons>()

  let handler =
    Click.listIncludedArtistsClickHandler presetRepo.Object resourceProviderMock.Object botServiceMock.Object

  [<Fact>]
  member _.``should handle valid click data``() =
    presetRepo.Setup(_.LoadPreset(It.IsAny<PresetId>())).ReturnsAsync(Some Mocks.preset)
    resourceProviderMock.Setup(fun x -> x[Messages.IncludedArtists]).Returns(Messages.IncludedArtists)

    let click =
      createClick [ CallbackQueryConstants.preset; Mocks.presetId.Value; CallbackQueryConstants.includedArtists; "0" ]

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