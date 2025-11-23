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

    platform.Setup(_.ListArtistTracks(Mocks.artist1.Id)).ReturnsAsync([])
    platform.Setup(_.ListArtistTracks(Mocks.artist2.Id)).ReturnsAsync([])

    let preset =
      { Mocks.preset with
          IncludedPlaylists = Set.singleton includedPlaylist }

    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([ Mocks.includedTrack; Mocks.likedTrack ])

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([ Mocks.excludedTrack ])

    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.likedTrack ])).ReturnsAsync(())

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)

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

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([])

    platform
      .Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack; Mocks.likedTrack; Mocks.recommendedTrack ]))
      .ReturnsAsync(())

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)

    platform.Setup(_.ListArtistTracks(Mocks.artist1.Id)).ReturnsAsync([])
    platform.Setup(_.ListArtistTracks(Mocks.artist2.Id)).ReturnsAsync([])

    let sut: IPresetService =
      PresetService(parsePlaylistId, parseArtistId, presetRepo.Object, musicPlatformFactory.Object, shuffler, recommender.Object)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<Preset, Preset.RunError>.Ok(Mocks.preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``includes tracks from included artists``() =
    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([])

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([])

    platform.Setup(_.ListArtistTracks(Mocks.artist1.Id)).ReturnsAsync([ Mocks.includedTrack ])

    platform.Setup(_.ListArtistTracks(Mocks.artist2.Id)).ReturnsAsync([])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack ])).ReturnsAsync(())

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)

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

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    platform.Setup(_.ListArtistTracks(Mocks.artist1.Id)).ReturnsAsync([])

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)

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

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([ Mocks.includedTrack ])

    platform.Setup(_.ListArtistTracks(Mocks.artist1.Id)).ReturnsAsync([])
    platform.Setup(_.ListArtistTracks(Mocks.artist2.Id)).ReturnsAsync([])

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)

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

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([ Mocks.recommendedTrack ])

    platform.Setup(_.ListArtistTracks(Mocks.artist1.Id)).ReturnsAsync([ Mocks.recommendedTrack ])

    platform.Setup(_.ListArtistTracks(Mocks.artist2.Id)).ReturnsAsync([])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack ])).ReturnsAsync(())

    let preset =
      { Mocks.preset with
          Settings.RecommendationsEngine = Some RecommendationsEngine.ArtistAlbums }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)

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

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([ Mocks.likedTrack ])

    platform.Setup(_.ListArtistTracks(Mocks.artist1.Id)).ReturnsAsync([])
    platform.Setup(_.ListArtistTracks(Mocks.artist2.Id)).ReturnsAsync([])

    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack ])).ReturnsAsync(())

    let preset =
      { Mocks.preset with
          Settings.LikedTracksHandling = LikedTracksHandling.Include }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)

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

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([])

    platform.Setup(_.ListArtistTracks(Mocks.artist1.Id)).ReturnsAsync([])
    platform.Setup(_.ListArtistTracks(Mocks.artist2.Id)).ReturnsAsync([])

    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack ])).ReturnsAsync(())

    let preset =
      { Mocks.preset with
          Settings.LikedTracksHandling = LikedTracksHandling.Exclude }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)

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

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([ Mocks.recommendedTrack ])

    platform.Setup(_.ListArtistTracks(Mocks.artist1.Id)).ReturnsAsync([])
    platform.Setup(_.ListArtistTracks(Mocks.artist2.Id)).ReturnsAsync([])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack ])).ReturnsAsync(())

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)

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

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([])

    platform.Setup(_.ListArtistTracks(Mocks.artist1.Id)).ReturnsAsync([])
    platform.Setup(_.ListArtistTracks(Mocks.artist2.Id)).ReturnsAsync([])

    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.likedTrack ])).ReturnsAsync(())

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)

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

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([])

    platform.Setup(_.ListArtistTracks(Mocks.artist1.Id)).ReturnsAsync([ Mocks.recommendedTrack ])

    platform.Setup(_.ListArtistTracks(Mocks.artist2.Id)).ReturnsAsync([])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack; Mocks.recommendedTrack ])).ReturnsAsync(())

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)

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

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([])

    platform.Setup(_.ListArtistTracks(Mocks.artist1.Id)).ReturnsAsync([])
    platform.Setup(_.ListArtistTracks(Mocks.artist2.Id)).ReturnsAsync([])

    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ])

    platform.Setup(_.ListArtistTracks(Mocks.artist3.Id)).ReturnsAsync([ Mocks.recommendedTrack ])

    platform.Setup(_.ListArtistTracks(Mocks.artist4.Id)).ReturnsAsync([])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.recommendedTrack; Mocks.likedTrack ])).ReturnsAsync(())

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)

    let sut: IPresetService =
      PresetService(parsePlaylistId, parseArtistId, presetRepo.Object, musicPlatformFactory.Object, shuffler, recommender.Object)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<_, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``excludes tracks of artist``() =
    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([ Mocks.includedTrack ])

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([])

    platform.Setup(_.ListArtistTracks(Mocks.artist1.Id)).ReturnsAsync([])
    platform.Setup(_.ListArtistTracks(Mocks.artist2.Id)).ReturnsAsync([ Mocks.includedTrack ])

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)

    let sut: IPresetService =
      PresetService(parsePlaylistId, parseArtistId, presetRepo.Object, musicPlatformFactory.Object, shuffler, recommender.Object)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<Preset, _>.Error(Preset.NoPotentialTracks))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

type IncludeArtist() =
  let parseArtistId: Artist.ParseId = fun p -> Ok(ArtistId p.Value)
  let presetRepo = Mock<IPresetRepo>()
  let musicPlatformFactory = Mock<IMusicPlatformFactory>()
  let platform = Mock<IMusicPlatform>()

  [<Fact>]
  member _.``should include artist successfully``() =
    let rawArtistId = Artist.RawArtistId "artist-raw-id"

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    let updatedPreset =
      { Mocks.preset with
          IncludedArtists = Mocks.preset.IncludedArtists |> Set.add Mocks.artist3 }

    presetRepo.Setup(_.SavePreset(updatedPreset)).ReturnsAsync(())

    platform.Setup(_.LoadArtist(It.IsAny<ArtistId>())).ReturnsAsync(Ok Mocks.artist3)

    musicPlatformFactory.Setup(_.GetMusicPlatform(Mocks.userId.ToMusicPlatformId())).ReturnsAsync(Some platform.Object)

    let sut =
      Domain.Workflows.Preset.includeArtist parseArtistId presetRepo.Object musicPlatformFactory.Object

    task {
      let! result = sut Mocks.userId Mocks.presetId rawArtistId

      match result with
      | Ok r -> r.Artist |> should equal Mocks.artist3
      | Error e -> failwithf "Expected success, got error %A" e

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``should return error when artist id parsing fails``() =
    let invalidParseArtistId: Artist.ParseId =
      fun _ -> Error(Artist.IdParsingError "invalid")

    let rawArtistId = Artist.RawArtistId "invalid-id"

    musicPlatformFactory.Setup(_.GetMusicPlatform(Mocks.userId.ToMusicPlatformId())).ReturnsAsync(Some platform.Object)

    let sut =
      Domain.Workflows.Preset.includeArtist invalidParseArtistId presetRepo.Object musicPlatformFactory.Object

    task {
      let! result = sut Mocks.userId Mocks.presetId rawArtistId

      match result with
      | Error(Preset.IncludeArtistError.IdParsing(Artist.IdParsingError msg)) -> msg |> should equal "invalid"
      | _ -> failwith "Expected IdParsing error"

      presetRepo.VerifyNoOtherCalls()
      platform.VerifyNoOtherCalls()
    }

  [<Fact>]
  member _.``should return error when artist not found``() =
    let rawArtistId = Artist.RawArtistId "not-found-id"

    platform.Setup(_.LoadArtist(It.IsAny<ArtistId>())).ReturnsAsync(Error Artist.LoadError.NotFound)

    musicPlatformFactory.Setup(_.GetMusicPlatform(Mocks.userId.ToMusicPlatformId())).ReturnsAsync(Some platform.Object)

    let sut =
      Domain.Workflows.Preset.includeArtist parseArtistId presetRepo.Object musicPlatformFactory.Object

    task {
      let! result = sut Mocks.userId Mocks.presetId rawArtistId

      match result with
      | Error(Preset.IncludeArtistError.Load Artist.LoadError.NotFound) -> ()
      | _ -> failwith "Expected Load NotFound error"

      platform.VerifyAll()
      presetRepo.VerifyNoOtherCalls()
    }

  [<Fact>]
  member _.``should return error when user unauthorized``() =
    let rawArtistId = Artist.RawArtistId "some-id"

    musicPlatformFactory.Setup(_.GetMusicPlatform(Mocks.userId.ToMusicPlatformId())).ReturnsAsync(None)

    let sut =
      Domain.Workflows.Preset.includeArtist parseArtistId presetRepo.Object musicPlatformFactory.Object

    task {
      let! result = sut Mocks.userId Mocks.presetId rawArtistId

      match result with
      | Error Preset.IncludeArtistError.Unauthorized -> ()
      | _ -> failwith "Expected Unauthorized error"

      presetRepo.VerifyNoOtherCalls()
      platform.VerifyNoOtherCalls()
    }