namespace Domain.Tests.Preset

open Domain.Core.PresetSettings
open Domain.Repos
open Domain.Workflows
open FSharp.Control
open Microsoft.Extensions.Logging
open Moq
open Tests.Shared
open Xunit
open MusicPlatform
open Domain.Core
open Domain.Tests

type IncludeArtist() =
  let parseArtistId: Artist.ParseId = fun p -> Ok(ArtistId p.Value)
  let presetRepo = Mock<IPresetRepo>()
  let musicPlatformFactory = Mock<IMusicPlatformFactory>()
  let platform = Mock<IMusicPlatform>()

  [<Fact>]
  member _.``should return error when artist is already included``() =
    let preset =
      { Mocks.preset with
          IncludedArtists = [ Mocks.artist1 ]
      }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    let sut =
      Preset.includeArtist parseArtistId presetRepo.Object musicPlatformFactory.Object

    task {
      let! result =
        sut
          {
            IncludeArtist.Cmd.UserId = Mocks.userId
            PresetId = Mocks.presetId
            ArtistId = Artist.RawArtistId Mocks.artist1.Id.Value
          }

      match result with
      | Error(IncludeArtist.Error.Duplicate artistId) -> Assert.Equal(Mocks.artist1.Id, artistId)
      | _ -> failwith "Expected Duplicate error"

      presetRepo.VerifyAll()
      musicPlatformFactory.VerifyNoOtherCalls()
      platform.VerifyNoOtherCalls()
    }

  [<Fact>]
  member _.``should include artist successfully``() =
    let rawArtistId = Artist.RawArtistId "artist-raw-id"

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    let updatedPreset =
      { Mocks.preset with
          IncludedArtists = Mocks.artist3 :: Mocks.preset.IncludedArtists
      }

    presetRepo.Setup(_.SavePreset(updatedPreset)).ReturnsAsync(())

    platform.Setup(_.LoadArtist(It.IsAny<ArtistId>())).ReturnsAsync(Ok Mocks.artist3)

    musicPlatformFactory.Setup(_.GetMusicPlatform(Mocks.userId.ToMusicPlatformId())).ReturnsAsync(Some platform.Object)

    let sut =
      Preset.includeArtist parseArtistId presetRepo.Object musicPlatformFactory.Object

    task {
      let! result =
        sut
          {
            IncludeArtist.Cmd.UserId = Mocks.userId
            PresetId = Mocks.presetId
            ArtistId = rawArtistId
          }

      Assert.Equal(Result<_, IncludeArtist.Error>.Ok Mocks.artist3, result)

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
      let! result =
        sut
          {
            IncludeArtist.Cmd.UserId = Mocks.userId
            PresetId = Mocks.presetId
            ArtistId = rawArtistId
          }

      match result with
      | Error(IncludeArtist.Error.IdParsing(Artist.IdParsingError msg)) -> Assert.Equal("invalid", msg)
      | _ -> failwith "Expected IdParsing error"

      presetRepo.VerifyNoOtherCalls()
      platform.VerifyNoOtherCalls()
    }

  [<Fact>]
  member _.``should return error when artist not found``() =
    let rawArtistId = Artist.RawArtistId "not-found-id"

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    platform.Setup(_.LoadArtist(It.IsAny<ArtistId>())).ReturnsAsync(Error Artist.LoadError.NotFound)

    musicPlatformFactory.Setup(_.GetMusicPlatform(Mocks.userId.ToMusicPlatformId())).ReturnsAsync(Some platform.Object)

    let sut =
      Preset.includeArtist parseArtistId presetRepo.Object musicPlatformFactory.Object

    task {
      let! result =
        sut
          {
            IncludeArtist.Cmd.UserId = Mocks.userId
            PresetId = Mocks.presetId
            ArtistId = rawArtistId
          }

      match result with
      | Error(IncludeArtist.Error.Load Artist.LoadError.NotFound) -> ()
      | _ -> failwith "Expected Load NotFound error"

      platform.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``should return error when user unauthorized``() =
    let rawArtistId = Artist.RawArtistId "some-id"

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    musicPlatformFactory.Setup(_.GetMusicPlatform(Mocks.userId.ToMusicPlatformId())).ReturnsAsync(None)

    let sut =
      Preset.includeArtist parseArtistId presetRepo.Object musicPlatformFactory.Object

    task {
      let! result =
        sut
          {
            IncludeArtist.Cmd.UserId = Mocks.userId
            PresetId = Mocks.presetId
            ArtistId = rawArtistId
          }

      match result with
      | Error IncludeArtist.Error.Unauthorized -> ()
      | _ -> failwith "Expected Unauthorized error"

      presetRepo.VerifyAll()
      platform.VerifyNoOtherCalls()
    }

type ExcludeArtist() =
  let parseArtistId: Artist.ParseId = fun p -> Ok(ArtistId p.Value)
  let presetRepo = Mock<IPresetRepo>()
  let musicPlatformFactory = Mock<IMusicPlatformFactory>()
  let platform = Mock<IMusicPlatform>()

  [<Fact>]
  member _.``should return error when artist is already excluded``() =
    let preset =
      { Mocks.preset with
          ExcludedArtists = [ Mocks.artist1 ]
      }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    let sut =
      Preset.excludeArtist parseArtistId presetRepo.Object musicPlatformFactory.Object

    task {
      let! result =
        sut
          {
            ExcludeArtist.Cmd.UserId = Mocks.userId
            PresetId = Mocks.presetId
            ArtistId = Artist.RawArtistId Mocks.artist1.Id.Value
          }

      match result with
      | Error(ExcludeArtist.Error.Duplicate artistId) -> Assert.Equal(Mocks.artist1.Id, artistId)
      | _ -> failwith "Expected Duplicate error"

      presetRepo.VerifyAll()
      musicPlatformFactory.VerifyNoOtherCalls()
      platform.VerifyNoOtherCalls()
    }

type IncludePlaylist() =
  let parsePlaylistId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)
  let presetRepo = Mock<IPresetRepo>()
  let musicPlatformFactory = Mock<IMusicPlatformFactory>()
  let platform = Mock<IMusicPlatform>()

  [<Fact>]
  member _.``should return error when playlist is already included``() =
    let preset =
      { Mocks.preset with
          IncludedPlaylists = [ Mocks.includedPlaylist ]
      }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    let sut =
      Preset.includePlaylist parsePlaylistId presetRepo.Object musicPlatformFactory.Object

    task {
      let! result =
        sut
          {
            IncludePlaylist.Cmd.UserId = Mocks.userId
            PresetId = Mocks.presetId
            PlaylistId = Playlist.RawPlaylistId Mocks.includedPlaylistId.Value
          }

      match result with
      | Error(IncludePlaylist.Error.Duplicate playlistId) -> Assert.Equal(Mocks.includedPlaylistId, playlistId)
      | _ -> failwith "Expected Duplicate error"

      presetRepo.VerifyAll()
      musicPlatformFactory.VerifyNoOtherCalls()
      platform.VerifyNoOtherCalls()
    }

type ExcludePlaylist() =
  let parsePlaylistId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)
  let presetRepo = Mock<IPresetRepo>()
  let musicPlatformFactory = Mock<IMusicPlatformFactory>()
  let platform = Mock<IMusicPlatform>()

  [<Fact>]
  member _.``should return error when playlist is already excluded``() =
    let preset =
      { Mocks.preset with
          ExcludedPlaylists = [ Mocks.excludedPlaylist ]
      }

    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some preset)

    let sut =
      Preset.excludePlaylist parsePlaylistId presetRepo.Object musicPlatformFactory.Object

    task {
      let! result =
        sut
          {
            ExcludePlaylist.Cmd.UserId = Mocks.userId
            PresetId = Mocks.presetId
            PlaylistId = Playlist.RawPlaylistId Mocks.excludedPlaylistId.Value
          }

      match result with
      | Error(ExcludePlaylist.Error.Duplicate playlistId) -> Assert.Equal(Mocks.excludedPlaylistId, playlistId)
      | _ -> failwith "Expected Duplicate error"

      presetRepo.VerifyAll()
      musicPlatformFactory.VerifyNoOtherCalls()
      platform.VerifyNoOtherCalls()
    }

type TargetPlaylist() =
  let parsePlaylistId: Playlist.ParseId = fun p -> Ok(PlaylistId p.Value)
  let presetRepo = Mock<IPresetRepo>()
  let musicPlatformFactory = Mock<IMusicPlatformFactory>()
  let platform = Mock<IMusicPlatform>()

  [<Fact>]
  member _.``should return error when playlist is already targeted``() =
    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    let sut =
      Preset.targetPlaylist parsePlaylistId presetRepo.Object musicPlatformFactory.Object

    task {
      let! result =
        sut
          {
            TargetPlaylist.Cmd.UserId = Mocks.userId
            PresetId = Mocks.presetId
            PlaylistId = Playlist.RawPlaylistId Mocks.targetedPlaylistId.Value
          }

      match result with
      | Error(TargetPlaylist.Error.Duplicate playlistId) -> Assert.Equal(Mocks.targetedPlaylistId, playlistId)
      | _ -> failwith "Expected Duplicate error"

      presetRepo.VerifyAll()
      musicPlatformFactory.VerifyNoOtherCalls()
      platform.VerifyNoOtherCalls()
    }