module Domain.Tests.Preset

open Domain.Core.PresetSettings
open Domain.Repos
open Domain.Workflows
open Moq
open Xunit
open FsUnit.Xunit
open MusicPlatform
open Domain.Core

module Run =
  let shuffler: Shuffler<Track> = id

  [<Fact>]
  let ``takes only liked tracks from included playlists if configured`` () =
    let includedPlaylist =
      { Mocks.includedPlaylist with
          LikedOnly = true }

    let preset =
      { Mocks.preset with
          IncludedPlaylists = [ includedPlaylist ] }

    let platform = Mock<IMusicPlatform>()

    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([ Mocks.includedTrack; Mocks.likedTrack ])

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([ Mocks.excludedTrack ])

    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.likedTrack ])).ReturnsAsync(())

    let presetRepo = Mock<IPresetRepo>()

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    let parseId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)

    let musicPlatformFactory = Mock<IMusicPlatformFactory>()

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)

    let recommender = Mock<IRecommender>()

    let sut: IPresetService =
      PresetService(parseId, presetRepo.Object, musicPlatformFactory.Object, shuffler, recommender.Object)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<Preset, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  let ``takes all tracks from included playlists if configured`` () =
    let platform = Mock<IMusicPlatform>()

    platform
      .Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId))
      .ReturnsAsync([ Mocks.includedTrack; Mocks.likedTrack; Mocks.recommendedTrack ])

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([])

    platform
      .Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack; Mocks.likedTrack; Mocks.recommendedTrack ]))
      .ReturnsAsync(())

    let presetRepo = Mock<IPresetRepo>()

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    let parseId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)

    let musicPlatformFactory = Mock<IMusicPlatformFactory>()

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)

    let recommender = Mock<IRecommender>()

    let sut: IPresetService =
      PresetService(parseId, presetRepo.Object, musicPlatformFactory.Object, shuffler, recommender.Object)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<Preset, Preset.RunError>.Ok(Mocks.preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  let ``returns error if no tracks in included playlists and liked tracks are not included`` () =
    let platform = Mock<IMusicPlatform>()

    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([])

    let presetRepo = Mock<IPresetRepo>()

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    let parseId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)

    let musicPlatformFactory = Mock<IMusicPlatformFactory>()

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)

    let recommender = Mock<IRecommender>()

    let sut: IPresetService =
      PresetService(parseId, presetRepo.Object, musicPlatformFactory.Object, shuffler, recommender.Object)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result
      |> should equal (Result<Preset, Preset.RunError>.Error(Preset.RunError.NoIncludedTracks))

      platform.VerifyAll()
    }

  [<Fact>]
  let ``returns error if all potential tracks has been excluded`` () =
    let platform = Mock<IMusicPlatform>()

    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([ Mocks.includedTrack ])

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([ Mocks.includedTrack ])

    let presetRepo = Mock<IPresetRepo>()

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    let parseId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)

    let musicPlatformFactory = Mock<IMusicPlatformFactory>()

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)

    let recommender = Mock<IRecommender>()

    let sut: IPresetService =
      PresetService(parseId, presetRepo.Object, musicPlatformFactory.Object, shuffler, recommender.Object)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result
      |> should equal (Result<Preset, Preset.RunError>.Error(Preset.RunError.NoPotentialTracks))

      platform.VerifyAll()
    }

  [<Fact>]
  let ``excludes recommended tracks if in excluded playlist`` () =
    let platform = Mock<IMusicPlatform>()

    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([ Mocks.includedTrack ])

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([ Mocks.recommendedTrack ])

    platform.Setup(_.ListArtistTracks(Mocks.artist1.Id)).ReturnsAsync([ Mocks.recommendedTrack ])

    platform.Setup(_.ListArtistTracks(Mocks.artist2.Id)).ReturnsAsync([])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack ])).ReturnsAsync(())

    let presetRepo = Mock<IPresetRepo>()

    let preset =
      { Mocks.preset with
          Settings.RecommendationsEngine = Some RecommendationsEngine.ArtistAlbums }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    let parseId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)

    let musicPlatformFactory = Mock<IMusicPlatformFactory>()

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)

    let recommender = Mock<IRecommender>()

    let sut: IPresetService =
      PresetService(parseId, presetRepo.Object, musicPlatformFactory.Object, shuffler, recommender.Object)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<Preset, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
    }

  [<Fact>]
  let ``excludes liked tracks if in excluded playlist`` () =
    let platform = Mock<IMusicPlatform>()

    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([ Mocks.includedTrack ])

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([ Mocks.likedTrack ])

    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack ])).ReturnsAsync(())

    let presetRepo = Mock<IPresetRepo>()

    let preset =
      { Mocks.preset with
          Settings.LikedTracksHandling = PresetSettings.LikedTracksHandling.Include }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    let parseId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)

    let musicPlatformFactory = Mock<IMusicPlatformFactory>()

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)

    let recommender = Mock<IRecommender>()

    let sut: IPresetService =
      PresetService(parseId, presetRepo.Object, musicPlatformFactory.Object, shuffler, recommender.Object)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<_, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  let ``excludes liked tracks if configured`` () =
    let platform = Mock<IMusicPlatform>()

    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([ Mocks.includedTrack; Mocks.likedTrack ])

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([])

    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack ])).ReturnsAsync(())

    let presetRepo = Mock<IPresetRepo>()

    let preset =
      { Mocks.preset with
          Settings.LikedTracksHandling = PresetSettings.LikedTracksHandling.Exclude }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    let parseId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)

    let musicPlatformFactory = Mock<IMusicPlatformFactory>()

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)

    let recommender = Mock<IRecommender>()

    let sut: IPresetService =
      PresetService(parseId, presetRepo.Object, musicPlatformFactory.Object, shuffler, recommender.Object)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<_, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }


  [<Fact>]
  let ``excludes included tracks if in excluded playlist`` () =
    let platform = Mock<IMusicPlatform>()

    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([ Mocks.includedTrack; Mocks.recommendedTrack ])

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([ Mocks.recommendedTrack ])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack ])).ReturnsAsync(())

    let presetRepo = Mock<IPresetRepo>()

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    let parseId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)

    let musicPlatformFactory = Mock<IMusicPlatformFactory>()

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)

    let recommender = Mock<IRecommender>()

    let sut: IPresetService =
      PresetService(parseId, presetRepo.Object, musicPlatformFactory.Object, shuffler, recommender.Object)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<_, Preset.RunError>.Ok(Mocks.preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  let ``includes liked tracks if configured`` () =
    let preset =
      { Mocks.preset with
          Settings.LikedTracksHandling = PresetSettings.LikedTracksHandling.Include }

    let platform = Mock<IMusicPlatform>()

    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([])
    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([])
    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.likedTrack ])).ReturnsAsync(())

    let presetRepo = Mock<IPresetRepo>()

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    let parseId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)

    let musicPlatformFactory = Mock<IMusicPlatformFactory>()

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)

    let recommender = Mock<IRecommender>()

    let sut: IPresetService =
      PresetService(parseId, presetRepo.Object, musicPlatformFactory.Object, shuffler, recommender.Object)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<_, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  let ``saves included tracks with recommendations`` () =
    let preset =
      { Mocks.preset with
          Settings.RecommendationsEngine = Some RecommendationsEngine.ArtistAlbums }

    let platform = Mock<IMusicPlatform>()

    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([ Mocks.includedTrack ])

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([])

    platform.Setup(_.ListArtistTracks(Mocks.artist1.Id)).ReturnsAsync([ Mocks.recommendedTrack ])

    platform.Setup(_.ListArtistTracks(Mocks.artist2.Id)).ReturnsAsync([])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.recommendedTrack; Mocks.includedTrack ])).ReturnsAsync(())

    let presetRepo = Mock<IPresetRepo>()

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    let parseId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)

    let musicPlatformFactory = Mock<IMusicPlatformFactory>()

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)

    let recommender = Mock<IRecommender>()

    let sut: IPresetService =
      PresetService(parseId, presetRepo.Object, musicPlatformFactory.Object, shuffler, recommender.Object)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<_, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
      recommender.VerifyAll()
    }

  [<Fact>]
  let ``includes liked tracks with recommendations if configured`` () =
    let preset =
      { Mocks.preset with
          Settings =
            { Mocks.preset.Settings with
                RecommendationsEngine = Some RecommendationsEngine.ArtistAlbums
                LikedTracksHandling = LikedTracksHandling.Include } }

    let platform = Mock<IMusicPlatform>()

    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([])

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([])

    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ])

    platform.Setup(_.ListArtistTracks(Mocks.artist3.Id)).ReturnsAsync([ Mocks.recommendedTrack ])

    platform.Setup(_.ListArtistTracks(Mocks.artist4.Id)).ReturnsAsync([])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.recommendedTrack; Mocks.likedTrack ])).ReturnsAsync(())

    let presetRepo = Mock<IPresetRepo>()

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    let parseId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)

    let musicPlatformFactory = Mock<IMusicPlatformFactory>()

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)

    let recommender = Mock<IRecommender>()

    let sut: IPresetService =
      PresetService(parseId, presetRepo.Object, musicPlatformFactory.Object, shuffler, recommender.Object)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<_, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

type GetPreset() =
  let parseId = fun _ -> Ok Mocks.includedPlaylistId

  let presetRepo = Mock<IPresetRepo>()

  do
    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)
    |> ignore

  do
    presetRepo.Setup(_.ParseId(Mocks.rawPresetId)).Returns(Some Mocks.presetId)
    |> ignore

  let musicPlatformFactory = Mock<IMusicPlatformFactory>()

  let recommender = Mock<IRecommender>()

  let sut: IPresetService =
    PresetService(parseId, presetRepo.Object, musicPlatformFactory.Object, id, recommender.Object)

  [<Fact>]
  let ``returns Preset if it belongs to User`` () =

    task {
      let! preset = sut.GetPreset(Mocks.userId, Mocks.rawPresetId)

      preset |> should equal (Result<_, Preset.GetPresetError>.Ok(Mocks.preset))

      presetRepo.VerifyAll()
    }

  [<Fact>]
  let ``returns NotFound when preset does not exist`` () =
    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(None)

    task {
      let! preset = sut.GetPreset(Mocks.userId, Mocks.rawPresetId)

      preset |> should equal (Result<Preset, _>.Error Preset.GetPresetError.NotFound)

      presetRepo.VerifyAll()
    }

  [<Fact>]
  let ``returns NotFound when preset belongs to another user`` () =
    let otherUserId = otsom.fs.Core.UserId "other-user"

    task {
      let! preset = sut.GetPreset(otherUserId, Mocks.rawPresetId)

      preset |> should equal (Result<Preset, _>.Error Preset.GetPresetError.NotFound)

      presetRepo.VerifyAll()
    }

type RemovePreset() =
  let parseId = fun _ -> Ok Mocks.includedPlaylistId

  let presetRepo = Mock<IPresetRepo>()

  do
    presetRepo.Setup(_.ParseId(Mocks.rawPresetId)).Returns(Some Mocks.presetId)
    |> ignore

  let musicPlatformFactory = Mock<IMusicPlatformFactory>()

  let recommender = Mock<IRecommender>()

  let sut: IPresetService =
    PresetService(parseId, presetRepo.Object, musicPlatformFactory.Object, id, recommender.Object)

  [<Fact>]
  let ``removes preset if found and belongs to user`` () =
    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)
    presetRepo.Setup(_.RemovePreset(Mocks.presetId)).ReturnsAsync(())

    task {
      let! result = sut.RemovePreset(Mocks.userId, Mocks.rawPresetId)

      result |> should equal (Result<_, Preset.GetPresetError>.Ok(Mocks.preset))

      presetRepo.VerifyAll()
    }

  [<Fact>]
  let ``returns error if preset not found`` () =
    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(None)

    task {
      let! result = sut.RemovePreset(Mocks.userId, Mocks.rawPresetId)

      result |> should equal (Result<Preset, _>.Error(Preset.GetPresetError.NotFound))

      presetRepo.VerifyAll()
    }

  [<Fact>]
  let ``returns error if preset doesn't belong to user`` () =
    presetRepo
      .Setup(_.LoadPreset(Mocks.presetId))
      .ReturnsAsync(
        Some
          { Mocks.preset with
              OwnerId = Mocks.otherUserId }
      )

    task {
      let! result = sut.RemovePreset(Mocks.userId, Mocks.rawPresetId)

      result |> should equal (Result<Preset, _>.Error(Preset.GetPresetError.NotFound))

      presetRepo.VerifyAll()
    }