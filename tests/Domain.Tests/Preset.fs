module Domain.Tests.Preset

open System.Threading.Tasks
open Domain.Core
open Domain.Repos
open Domain.Workflows
open Moq
open MusicPlatform
open Xunit
open FsUnit.Xunit
open NSubstitute

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

    platform
      .Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId))
      .ReturnsAsync([ Mocks.includedTrack; Mocks.likedTrack ])

    platform
      .Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId))
      .ReturnsAsync([ Mocks.excludedTrack ])

    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ])

    platform
      .Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.likedTrack ]))
      .ReturnsAsync(())

    let presetRepo = Mock<IPresetRepo>()

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(preset)

    let parseId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)

    let musicPlatformFactory = Substitute.For<IMusicPlatformFactory>()

    musicPlatformFactory.GetMusicPlatform(Arg.Any()).Returns(Some platform.Object)

    let sut: IPresetService =
      PresetService(parseId, presetRepo.Object, musicPlatformFactory, shuffler)

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

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Mocks.preset)

    let parseId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)

    let musicPlatformFactory = Substitute.For<IMusicPlatformFactory>()

    musicPlatformFactory.GetMusicPlatform(Arg.Any()).Returns(Some platform.Object)

    let sut: IPresetService =
      PresetService(parseId, presetRepo.Object, musicPlatformFactory, shuffler)

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

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Mocks.preset)

    let parseId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)

    let musicPlatformFactory = Substitute.For<IMusicPlatformFactory>()

    musicPlatformFactory.GetMusicPlatform(Arg.Any()).Returns(Some platform.Object)

    let sut: IPresetService =
      PresetService(parseId, presetRepo.Object, musicPlatformFactory, shuffler)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result
      |> should equal (Result<Preset, Preset.RunError>.Error(Preset.RunError.NoIncludedTracks))

      platform.VerifyAll()
    }

  [<Fact>]
  let ``returns error if all potential tracks has been excluded`` () =
    let platform = Mock<IMusicPlatform>()

    platform
      .Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId))
      .ReturnsAsync([ Mocks.includedTrack ])

    platform
      .Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId))
      .ReturnsAsync([ Mocks.includedTrack ])

    let presetRepo = Mock<IPresetRepo>()

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Mocks.preset)

    let parseId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)

    let musicPlatformFactory = Substitute.For<IMusicPlatformFactory>()

    musicPlatformFactory.GetMusicPlatform(Arg.Any()).Returns(Some platform.Object)

    let sut: IPresetService =
      PresetService(parseId, presetRepo.Object, musicPlatformFactory, shuffler)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result
      |> should equal (Result<Preset, Preset.RunError>.Error(Preset.RunError.NoPotentialTracks))

      platform.VerifyAll()
    }

  [<Fact>]
  let ``excludes recommended tracks if in excluded playlist`` () =
    let platform = Mock<IMusicPlatform>()

    platform
      .Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId))
      .ReturnsAsync([ Mocks.includedTrack ])

    platform
      .Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId))
      .ReturnsAsync([ Mocks.recommendedTrack ])

    platform
      .Setup(_.GetRecommendations([ Mocks.includedTrack.Id ]))
      .ReturnsAsync([ Mocks.recommendedTrack ])

    platform
      .Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack ]))
      .ReturnsAsync(())

    let presetRepo = Mock<IPresetRepo>()

    let preset =
      { Mocks.preset with
          Settings.RecommendationsEnabled = true }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(preset)

    let parseId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)

    let musicPlatformFactory = Substitute.For<IMusicPlatformFactory>()

    musicPlatformFactory.GetMusicPlatform(Arg.Any()).Returns(Some platform.Object)

    let sut: IPresetService =
      PresetService(parseId, presetRepo.Object, musicPlatformFactory, shuffler)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<Preset, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
    }

  [<Fact>]
  let ``excludes liked tracks if in excluded playlist`` () =
    let platform = Mock<IMusicPlatform>()

    platform
      .Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId))
      .ReturnsAsync([ Mocks.includedTrack ])

    platform
      .Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId))
      .ReturnsAsync([ Mocks.likedTrack ])

    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ])

    platform
      .Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack ]))
      .ReturnsAsync(())

    let presetRepo = Mock<IPresetRepo>()

    let preset =
      { Mocks.preset with
          Settings.LikedTracksHandling = PresetSettings.LikedTracksHandling.Include }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(preset)

    let parseId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)

    let musicPlatformFactory = Substitute.For<IMusicPlatformFactory>()

    musicPlatformFactory.GetMusicPlatform(Arg.Any()).Returns(Some platform.Object)

    let sut: IPresetService =
      PresetService(parseId, presetRepo.Object, musicPlatformFactory, shuffler)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<_, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  let ``excludes liked tracks if configured`` () =
    let platform = Mock<IMusicPlatform>()

    platform
      .Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId))
      .ReturnsAsync([ Mocks.includedTrack; Mocks.likedTrack ])

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([])

    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ])

    platform
      .Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack ]))
      .ReturnsAsync(())

    let presetRepo = Mock<IPresetRepo>()

    let preset =
      { Mocks.preset with
          Settings.LikedTracksHandling = PresetSettings.LikedTracksHandling.Exclude }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(preset)

    let parseId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)

    let musicPlatformFactory = Substitute.For<IMusicPlatformFactory>()

    musicPlatformFactory.GetMusicPlatform(Arg.Any()).Returns(Some platform.Object)

    let sut: IPresetService =
      PresetService(parseId, presetRepo.Object, musicPlatformFactory, shuffler)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<_, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }


  [<Fact>]
  let ``excludes included tracks if in excluded playlist`` () =
    let platform = Mock<IMusicPlatform>()

    platform
      .Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId))
      .ReturnsAsync([ Mocks.includedTrack; Mocks.recommendedTrack ])

    platform
      .Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId))
      .ReturnsAsync([ Mocks.recommendedTrack ])

    platform
      .Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.includedTrack ]))
      .ReturnsAsync(())

    let presetRepo = Mock<IPresetRepo>()

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Mocks.preset)

    let parseId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)

    let musicPlatformFactory = Substitute.For<IMusicPlatformFactory>()

    musicPlatformFactory.GetMusicPlatform(Arg.Any()).Returns(Some platform.Object)

    let sut: IPresetService =
      PresetService(parseId, presetRepo.Object, musicPlatformFactory, shuffler)

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

    platform
      .Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.likedTrack ]))
      .ReturnsAsync(())

    let presetRepo = Mock<IPresetRepo>()

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(preset)

    let parseId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)

    let musicPlatformFactory = Substitute.For<IMusicPlatformFactory>()

    musicPlatformFactory.GetMusicPlatform(Arg.Any()).Returns(Some platform.Object)

    let sut: IPresetService =
      PresetService(parseId, presetRepo.Object, musicPlatformFactory, shuffler)

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
          Settings.RecommendationsEnabled = true }

    let platform = Mock<IMusicPlatform>()

    platform
      .Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId))
      .ReturnsAsync([ Mocks.includedTrack ])

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([])

    platform
      .Setup(_.GetRecommendations([ Mocks.includedTrack.Id ]))
      .ReturnsAsync([ Mocks.recommendedTrack ])

    platform
      .Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.recommendedTrack; Mocks.includedTrack ]))
      .ReturnsAsync(())

    let presetRepo = Mock<IPresetRepo>()

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(preset)

    let parseId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)

    let musicPlatformFactory = Substitute.For<IMusicPlatformFactory>()

    musicPlatformFactory.GetMusicPlatform(Arg.Any()).Returns(Some platform.Object)

    let sut: IPresetService =
      PresetService(parseId, presetRepo.Object, musicPlatformFactory, shuffler)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<_, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  let ``includes liked tracks with recommendations if configured`` () =
    let preset =
      { Mocks.preset with
          Settings =
            { Mocks.preset.Settings with
                RecommendationsEnabled = true
                LikedTracksHandling = PresetSettings.LikedTracksHandling.Include } }

    let platform = Mock<IMusicPlatform>()

    platform.Setup(_.ListPlaylistTracks(Mocks.includedPlaylistId)).ReturnsAsync([])

    platform.Setup(_.ListPlaylistTracks(Mocks.excludedPlaylistId)).ReturnsAsync([])

    platform.Setup(_.ListLikedTracks()).ReturnsAsync([ Mocks.likedTrack ])

    platform
      .Setup(_.GetRecommendations([ Mocks.likedTrack.Id ]))
      .ReturnsAsync([ Mocks.recommendedTrack ])

    platform
      .Setup(_.ReplaceTracks(Mocks.targetedPlaylistId, [ Mocks.recommendedTrack; Mocks.likedTrack ]))
      .ReturnsAsync(())

    let presetRepo = Mock<IPresetRepo>()

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(preset)

    let parseId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)

    let musicPlatformFactory = Substitute.For<IMusicPlatformFactory>()

    musicPlatformFactory.GetMusicPlatform(Arg.Any()).Returns(Some platform.Object)

    let sut: IPresetService =
      PresetService(parseId, presetRepo.Object, musicPlatformFactory, shuffler)

    task {
      let! result = sut.RunPreset(Mocks.userId, Mocks.presetId)

      result |> should equal (Result<_, Preset.RunError>.Ok(preset))

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }