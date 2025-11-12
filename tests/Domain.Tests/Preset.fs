namespace Domain.Tests.Preset

open Domain.Core.PresetSettings
open Domain.Repos
open Domain.Workflows
open Moq
open Xunit
open FsUnit.Xunit
open MusicPlatform
open Domain.Core
open Domain.Tests

type Run() =
  let shuffler: Shuffler<Track> = id
  let parsePlaylistId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)
  let parseArtistId: Artist.ParseId = fun p -> Ok(ArtistId p.Value)
  let platform = Mock<IMusicPlatform>()
  let presetRepo = Mock<IPresetRepo>()
  let musicPlatformFactory = Mock<IMusicPlatformFactory>()
  let recommender = Mock<IRecommender>()

  [<Fact>]
  member _.``takes only liked tracks from included playlists if configured``() =
    let includedPlaylist =
      { Mocks.includedPlaylist with
          LikedOnly = true }

    let preset =
      { Mocks.preset with
          IncludedPlaylists = [ includedPlaylist ] }

    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([ Mocks.includedTrack; Mocks.likedTrack ])
    |> ignore

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([ Mocks.excludedTrack ])
    |> ignore

    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ]) |> ignore

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.likedTrack ])).ReturnsAsync(())
    |> ignore

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)
    |> ignore

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)
    |> ignore

    let sut: IPresetService =
      PresetService(parsePlaylistId, parseArtistId, presetRepo.Object, musicPlatformFactory.Object, shuffler, recommender.Object)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<Preset, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``takes all tracks from included playlists if configured``() =
    platform
      .Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId))
      .ReturnsAsync([ Mocks.includedTrack; Mocks.likedTrack; Mocks.recommendedTrack ])
    |> ignore

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([])
    |> ignore

    platform
      .Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack; Mocks.likedTrack; Mocks.recommendedTrack ]))
      .ReturnsAsync(())
    |> ignore

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)
    |> ignore

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)
    |> ignore

    let sut: IPresetService =
      PresetService(parsePlaylistId, parseArtistId, presetRepo.Object, musicPlatformFactory.Object, shuffler, recommender.Object)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<Preset, Preset.RunError>.Ok(Mocks.preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``returns error if no tracks in included playlists and liked tracks are not included``() =
    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([])
    |> ignore

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)
    |> ignore

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)
    |> ignore

    let sut: IPresetService =
      PresetService(parsePlaylistId, parseArtistId, presetRepo.Object, musicPlatformFactory.Object, shuffler, recommender.Object)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result
      |> should equal (Result<Preset, Preset.RunError>.Error(Preset.RunError.NoIncludedTracks))

      platform.VerifyAll()
    }

  [<Fact>]
  member _.``returns error if all potential tracks has been excluded``() =
    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([ Mocks.includedTrack ])
    |> ignore

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([ Mocks.includedTrack ])
    |> ignore

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)
    |> ignore

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)
    |> ignore

    let sut: IPresetService =
      PresetService(parsePlaylistId, parseArtistId, presetRepo.Object, musicPlatformFactory.Object, shuffler, recommender.Object)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result
      |> should equal (Result<Preset, Preset.RunError>.Error(Preset.RunError.NoPotentialTracks))

      platform.VerifyAll()
    }

  [<Fact>]
  member _.``excludes recommended tracks if in excluded playlist``() =
    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([ Mocks.includedTrack ])
    |> ignore

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([ Mocks.recommendedTrack ])
    |> ignore

    platform.Setup(_.ListArtistTracks(Mocks.artist1.Id)).ReturnsAsync([ Mocks.recommendedTrack ])
    |> ignore

    platform.Setup(_.ListArtistTracks(Mocks.artist2.Id)).ReturnsAsync([]) |> ignore

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack ])).ReturnsAsync(())
    |> ignore

    let preset =
      { Mocks.preset with
          Settings.RecommendationsEngine = Some RecommendationsEngine.ArtistAlbums }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)
    |> ignore

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)
    |> ignore

    let sut: IPresetService =
      PresetService(parsePlaylistId, parseArtistId, presetRepo.Object, musicPlatformFactory.Object, shuffler, recommender.Object)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<Preset, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
    }

  [<Fact>]
  member _.``excludes liked tracks if in excluded playlist``() =
    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([ Mocks.includedTrack ])
    |> ignore

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([ Mocks.likedTrack ])
    |> ignore

    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ]) |> ignore

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack ])).ReturnsAsync(())
    |> ignore

    let preset =
      { Mocks.preset with
          Settings.LikedTracksHandling = LikedTracksHandling.Include }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)
    |> ignore

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)
    |> ignore

    let sut: IPresetService =
      PresetService(parsePlaylistId, parseArtistId, presetRepo.Object, musicPlatformFactory.Object, shuffler, recommender.Object)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<_, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``excludes liked tracks if configured``() =
    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([ Mocks.includedTrack; Mocks.likedTrack ])
    |> ignore

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([])
    |> ignore

    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ]) |> ignore

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack ])).ReturnsAsync(())
    |> ignore

    let preset =
      { Mocks.preset with
          Settings.LikedTracksHandling = LikedTracksHandling.Exclude }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)
    |> ignore

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)
    |> ignore

    let sut: IPresetService =
      PresetService(parsePlaylistId, parseArtistId, presetRepo.Object, musicPlatformFactory.Object, shuffler, recommender.Object)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<_, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }


  [<Fact>]
  member _.``excludes included tracks if in excluded playlist``() =
    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([ Mocks.includedTrack; Mocks.recommendedTrack ])
    |> ignore

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([ Mocks.recommendedTrack ])
    |> ignore

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack ])).ReturnsAsync(())
    |> ignore

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)
    |> ignore

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)
    |> ignore

    let sut: IPresetService =
      PresetService(parsePlaylistId, parseArtistId, presetRepo.Object, musicPlatformFactory.Object, shuffler, recommender.Object)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<_, Preset.RunError>.Ok(Mocks.preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``includes liked tracks if configured``() =
    let preset =
      { Mocks.preset with
          Settings.LikedTracksHandling = LikedTracksHandling.Include }

    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([])
    |> ignore

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([])
    |> ignore

    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ]) |> ignore

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.likedTrack ])).ReturnsAsync(())
    |> ignore

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)
    |> ignore

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)
    |> ignore

    let sut: IPresetService =
      PresetService(parsePlaylistId, parseArtistId, presetRepo.Object, musicPlatformFactory.Object, shuffler, recommender.Object)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<_, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``saves included tracks with recommendations``() =
    let preset =
      { Mocks.preset with
          Settings.RecommendationsEngine = Some RecommendationsEngine.ArtistAlbums }

    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([ Mocks.includedTrack ])
    |> ignore

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([])
    |> ignore

    platform.Setup(_.ListArtistTracks(Mocks.artist1.Id)).ReturnsAsync([ Mocks.recommendedTrack ])
    |> ignore

    platform.Setup(_.ListArtistTracks(Mocks.artist2.Id)).ReturnsAsync([]) |> ignore

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.recommendedTrack; Mocks.includedTrack ])).ReturnsAsync(())
    |> ignore

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)
    |> ignore

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)
    |> ignore

    let sut: IPresetService =
      PresetService(parsePlaylistId, parseArtistId, presetRepo.Object, musicPlatformFactory.Object, shuffler, recommender.Object)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<_, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
      recommender.VerifyAll()
    }

  [<Fact>]
  member _.``includes liked tracks with recommendations if configured``() =
    let preset =
      { Mocks.preset with
          Settings =
            { Mocks.preset.Settings with
                RecommendationsEngine = Some RecommendationsEngine.ArtistAlbums
                LikedTracksHandling = LikedTracksHandling.Include } }

    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([])
    |> ignore

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([])
    |> ignore

    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ]) |> ignore

    platform.Setup(_.ListArtistTracks(Mocks.artist3.Id)).ReturnsAsync([ Mocks.recommendedTrack ])
    |> ignore

    platform.Setup(_.ListArtistTracks(Mocks.artist4.Id)).ReturnsAsync([]) |> ignore

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.recommendedTrack; Mocks.likedTrack ])).ReturnsAsync(())
    |> ignore

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)
    |> ignore

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)
    |> ignore

    let sut: IPresetService =
      PresetService(parsePlaylistId, parseArtistId, presetRepo.Object, musicPlatformFactory.Object, shuffler, recommender.Object)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<_, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }