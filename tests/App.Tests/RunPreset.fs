namespace App.Tests

open App
open Domain.Core
open Domain.Core.PresetSettings
open Domain.Repos
open Domain.Workflows
open FSharp.Control
open Microsoft.Extensions.Logging
open Moq
open MusicPlatform
open Xunit
open Tests.Shared

type RunPreset() =
  let shuffler: Shuffler<Track> = id
  let parsePlaylistId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)
  let parseArtistId: Artist.ParseId = fun p -> Ok(ArtistId p.Value)
  let platform = Mock<IMusicPlatform>()
  let presetRepo = Mock<IPresetRepo>()
  let musicPlatformFactory = Mock<IMusicPlatformFactory>()
  let recommender = Mock<IRecommender>()
  let recommenderFactory = Mock<IRecommenderFactory>()
  let logger = Mock<ILogger<PresetService>>()
  let loggerFactory = Mock<ILoggerFactory>()

  do
    loggerFactory.Setup(_.CreateLogger(It.IsAny<string>())).Returns(logger.Object)
    |> ignore

  do
    recommenderFactory.Setup(_.Create(It.IsAny<IMusicPlatform>(), It.IsAny<RecommendationsEngine>())).Returns(recommender.Object)
    |> ignore

  do
    musicPlatformFactory.Setup(_.GetMusicPlatform(It.IsAny())).ReturnsAsync(Some platform.Object)
    |> ignore

  let sut: RunPreset.Handler =
    RunPreset.handler presetRepo.Object musicPlatformFactory.Object shuffler recommenderFactory.Object loggerFactory.Object

  [<Fact>]
  member _.``takes only liked tracks from included playlists if configured``() =
    let includedPlaylist =
      { Mocks.includedPlaylist with
          LikedOnly = true
      }

    let preset =
      { Mocks.preset with
          IncludedPlaylists = [ includedPlaylist ]
      }

    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([ Mocks.includedTrack; Mocks.likedTrack ])

    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.likedTrack ])).ReturnsAsync(())

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    task {
      let! result =
        sut
          {
            UserId = Mocks.userId
            PresetId = Mocks.presetId
          }

      Assert.Equal(Result<Preset, RunPreset.Error>.Ok(preset), result)

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``includes tracks from included artists``() =
    let preset =
      { Mocks.preset with
          IncludedArtists = [ Mocks.artist1 ]
      }

    platform.Setup(_.ListArtistTracks(Mocks.artist1.Id)).Returns(TaskSeq.singleton Mocks.includedTrack)

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack ])).ReturnsAsync(())

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    task {
      let! result =
        sut
          {
            UserId = Mocks.userId
            PresetId = Mocks.presetId
          }

      Assert.Equal(Result<Preset, RunPreset.Error>.Ok(preset), result)

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``returns error if no tracks in included playlists and liked tracks are not included``() =
    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    task {
      let! result =
        sut
          {
            UserId = Mocks.userId
            PresetId = Mocks.presetId
          }

      Assert.Equal(Result<Preset, RunPreset.Error>.Error(RunPreset.Error.NoIncludedTracks), result)

      platform.VerifyAll()
    }

  [<Fact>]
  member _.``returns error if all potential tracks has been excluded``() =
    let preset =
      { Mocks.preset with
          IncludedPlaylists = [ Mocks.includedPlaylist ]
          ExcludedPlaylists = [ Mocks.excludedPlaylist ]
      }

    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([ Mocks.includedTrack ])
    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([ Mocks.includedTrack ])

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    task {
      let! result =
        sut
          {
            UserId = Mocks.userId
            PresetId = Mocks.presetId
          }

      Assert.Equal(Result<Preset, RunPreset.Error>.Error(RunPreset.Error.NoPotentialTracks), result)

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
          Settings.RecommendationsEngine = Some RecommendationsEngine.ReccoBeats
      }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    task {
      let! result =
        sut
          {
            UserId = Mocks.userId
            PresetId = Mocks.presetId
          }

      Assert.Equal(Result<Preset, RunPreset.Error>.Ok(preset), result)

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
          Settings.LikedTracksHandling = LikedTracksHandling.Include
      }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    task {
      let! result =
        sut
          {
            UserId = Mocks.userId
            PresetId = Mocks.presetId
          }

      Assert.Equal(Result<_, RunPreset.Error>.Ok(preset), result)

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
          Settings.LikedTracksHandling = LikedTracksHandling.Exclude
      }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    task {
      let! result =
        sut
          {
            UserId = Mocks.userId
            PresetId = Mocks.presetId
          }

      Assert.Equal(Result<_, RunPreset.Error>.Ok(preset), result)

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
          ExcludedPlaylists = [ Mocks.excludedPlaylist ]
      }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    task {
      let! result =
        sut
          {
            UserId = Mocks.userId
            PresetId = Mocks.presetId
          }

      Assert.Equal(Result<_, RunPreset.Error>.Ok(preset), result)

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``includes liked tracks if configured``() =
    let preset =
      { Mocks.preset with
          IncludedPlaylists = [ Mocks.includedPlaylist ]
          Settings.LikedTracksHandling = LikedTracksHandling.Include
      }

    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([])

    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.likedTrack ])).ReturnsAsync(())

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    task {
      let! result =
        sut
          {
            UserId = Mocks.userId
            PresetId = Mocks.presetId
          }

      Assert.Equal(Result<_, RunPreset.Error>.Ok(preset), result)

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
                LikedTracksHandling = LikedTracksHandling.Include
            }
      }

    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ])

    recommender.Setup(_.Recommend([ Mocks.likedTrack ])).ReturnsAsync([ Mocks.recommendedTrack ])

    platform.Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.recommendedTrack; Mocks.likedTrack ])).ReturnsAsync(())

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    task {
      let! result =
        sut
          {
            UserId = Mocks.userId
            PresetId = Mocks.presetId
          }

      Assert.Equal(Result<_, RunPreset.Error>.Ok(preset), result)

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
          ExcludedArtists = [ Mocks.artist2 ]
      }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    task {
      let! result =
        sut
          {
            UserId = Mocks.userId
            PresetId = Mocks.presetId
          }

      Assert.Equal(Result<Preset, _>.Error(RunPreset.Error.NoPotentialTracks), result)

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }