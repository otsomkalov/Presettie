namespace Domain.Tests.Preset

open Domain.Core.PresetSettings
open Domain.Repos
open Domain.Workflows
open FSharp.Control
open Microsoft.Extensions.Logging
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
  let logger = Mock<ILogger<PresetService>>()

  do
    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)
    |> ignore

  let sut: IPresetService =
    PresetService(
      parsePlaylistId,
      parseArtistId,
      presetRepo.Object,
      musicPlatformFactory.Object,
      shuffler,
      recommender.Object,
      logger.Object
    )

  [<Fact>]
  member _.``takes only liked tracks from included playlists if configured``() =
    let includedPlaylist =
      { Mocks.includedPlaylist with
          LikedOnly = true }

    let preset =
      { Mocks.preset with
          IncludedPlaylists = [ includedPlaylist ] }

    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([ Mocks.includedTrack; Mocks.likedTrack ])

    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.likedTrack ])).ReturnsAsync(())

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<Preset, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``includes tracks from included artists``() =
    let preset =
      { Mocks.preset with
          IncludedArtists = [ Mocks.artist1 ] }

    platform.Setup(_.ListArtistTracks(Mocks.artist1.Id)).Returns(TaskSeq.singleton Mocks.includedTrack)

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack ])).ReturnsAsync(())

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<Preset, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``returns error if no tracks in included playlists and liked tracks are not included``() =
    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result
      |> should equal (Result<Preset, Preset.RunError>.Error(Preset.RunError.NoIncludedTracks))

      platform.VerifyAll()
    }

  [<Fact>]
  member _.``returns error if all potential tracks has been excluded``() =
    let preset =
      { Mocks.preset with
          IncludedPlaylists = [ Mocks.includedPlaylist ]
          ExcludedPlaylists = [ Mocks.excludedPlaylist ] }

    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([ Mocks.includedTrack ])
    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([ Mocks.includedTrack ])

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

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

    recommender.Setup(_.Recommend([ Mocks.includedTrack ])).ReturnsAsync([ Mocks.recommendedTrack ])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack ])).ReturnsAsync(())

    let preset =
      { Mocks.preset with
          IncludedPlaylists = [ Mocks.includedPlaylist ]
          ExcludedPlaylists = [ Mocks.excludedPlaylist ]
          Settings.RecommendationsEngine = Some RecommendationsEngine.ReccoBeats }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<Preset, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
    }

  [<Fact>]
  member _.``excludes liked tracks if in excluded playlist``() =
    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ])

    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([ Mocks.includedTrack ])
    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([ Mocks.likedTrack ])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack ])).ReturnsAsync(())

    let preset =
      { Mocks.preset with
          IncludedPlaylists = [ Mocks.includedPlaylist ]
          ExcludedPlaylists = [ Mocks.excludedPlaylist ]
          Settings.LikedTracksHandling = LikedTracksHandling.Include }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<_, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``excludes liked tracks if configured``() =
    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([ Mocks.includedTrack; Mocks.likedTrack ])

    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack ])).ReturnsAsync(())

    let preset =
      { Mocks.preset with
          IncludedPlaylists = [ Mocks.includedPlaylist ]
          Settings.LikedTracksHandling = LikedTracksHandling.Exclude }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<_, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }


  [<Fact>]
  member _.``excludes included tracks if in excluded playlist``() =
    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([ Mocks.includedTrack; Mocks.excludedTrack ])
    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([ Mocks.excludedTrack ])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack ])).ReturnsAsync(())

    let preset =
      { Mocks.preset with
          IncludedPlaylists = [ Mocks.includedPlaylist ]
          ExcludedPlaylists = [ Mocks.excludedPlaylist ] }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<_, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``includes liked tracks if configured``() =
    let preset =
      { Mocks.preset with
          IncludedPlaylists = [ Mocks.includedPlaylist ]
          Settings.LikedTracksHandling = LikedTracksHandling.Include }

    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([])

    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.likedTrack ])).ReturnsAsync(())

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<_, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``includes liked tracks with recommendations if configured``() =
    let preset =
      { Mocks.preset with
          Settings =
            { Mocks.preset.Settings with
                RecommendationsEngine = Some RecommendationsEngine.ReccoBeats
                LikedTracksHandling = LikedTracksHandling.Include } }

    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ])

    recommender.Setup(_.Recommend([ Mocks.likedTrack ])).ReturnsAsync([ Mocks.recommendedTrack ])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.recommendedTrack; Mocks.likedTrack ])).ReturnsAsync(())

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<_, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``excludes tracks of excluded artist``() =
    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([ Mocks.excludedTrack ])

    platform.Setup(_.ListArtistTracks(Mocks.artist2.Id)).Returns(TaskSeq.singleton Mocks.excludedTrack)

    let preset =
      { Mocks.preset with
          IncludedPlaylists = [ Mocks.includedPlaylist ]
          ExcludedArtists = [ Mocks.artist2 ] }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

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
          IncludedArtists = Mocks.preset.IncludedArtists @ [ Mocks.artist3 ] }

    presetRepo.Setup(_.SavePreset(updatedPreset)).ReturnsAsync(())

    platform.Setup(_.LoadArtist(It.IsAny<ArtistId>())).ReturnsAsync(Ok Mocks.artist3)

    musicPlatformFactory.Setup(_.GetMusicPlatform(Mocks.userId.ToMusicPlatformId())).ReturnsAsync(Some platform.Object)

    let sut =
      Preset.includeArtist parseArtistId presetRepo.Object musicPlatformFactory.Object

    task {
      let! result = sut Mocks.userId Mocks.presetId rawArtistId

      result |> should equal (Result<_, Preset.IncludeArtistError>.Ok Mocks.artist3)

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
      Preset.includeArtist invalidParseArtistId presetRepo.Object musicPlatformFactory.Object

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
      Preset.includeArtist parseArtistId presetRepo.Object musicPlatformFactory.Object

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
      Preset.includeArtist parseArtistId presetRepo.Object musicPlatformFactory.Object

    task {
      let! result = sut Mocks.userId Mocks.presetId rawArtistId

      match result with
      | Error Preset.IncludeArtistError.Unauthorized -> ()
      | _ -> failwith "Expected Unauthorized error"

      presetRepo.VerifyNoOtherCalls()
      platform.VerifyNoOtherCalls()
    }