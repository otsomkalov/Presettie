module Domain.Tests.Mocks

open Domain.Core
open MusicPlatform

let includedTrack =
  { Id = TrackId "included-track-id"
    Artists = Set.ofList [ { Id = ArtistId "1" }; { Id = ArtistId "2" } ] }

let excludedTrack =
  { Id = TrackId "excluded-track-id"
    Artists = Set.ofList [ { Id = ArtistId "2" }; { Id = ArtistId "3" } ] }

let likedTrack =
  { Id = TrackId "liked-track-id"
    Artists = Set.ofList [ { Id = ArtistId "3" }; { Id = ArtistId "4" } ] }

let recommendedTrack =
  { Id = TrackId "recommended-track-id"
    Artists = Set.ofList [ { Id = ArtistId "3" }; { Id = ArtistId "4" } ] }

let readablePlaylistId = PlaylistId("readable-playlist-id")

let readablePlatformPlaylist: Playlist =
  Readable
    { Id = readablePlaylistId
      Name = "playlist-name"
      TracksCount = 1 }

let writablePlaylistId = PlaylistId("writable-playlist-id")

let writablePlatformPlaylist: Playlist =
  Writable
    { Id = writablePlaylistId
      Name = "playlist-name"
      TracksCount = 1 }

let includedPlaylistId = PlaylistId("included-playlist-id")

let includedPlaylist: IncludedPlaylist =
  { Id = ReadablePlaylistId(includedPlaylistId)
    Name = "included-playlist-name"
    LikedOnly = false }

let excludedPlaylistId = PlaylistId("excluded-playlist-id")

let excludedPlaylist: ExcludedPlaylist =
  { Id = ReadablePlaylistId(excludedPlaylistId)
    Name = "excluded-playlist-name" }

let targetedPlaylistId = PlaylistId("targeted-playlist-id")

let targetedPlaylist: TargetedPlaylist =
  { Id = WritablePlaylistId(targetedPlaylistId)
    Name = "targeted-playlist-name"
    Overwrite = true }

let presetSettingsMock: PresetSettings.PresetSettings =
  { Size = PresetSettings.Size.Size 10
    RecommendationsEnabled = false
    LikedTracksHandling = PresetSettings.LikedTracksHandling.Ignore
    UniqueArtists = false }

let presetId = PresetId("1")

let preset =
  { Id = presetId
    Name = "test-preset-name"
    IncludedPlaylists = [ includedPlaylist ]
    ExcludedPlaylists = [ excludedPlaylist ]
    TargetedPlaylists = [ targetedPlaylist ]
    Settings = presetSettingsMock }

let userPreset: SimplePreset =
  { Id = presetId
    Name = "user-preset-name" }

let userId = otsom.fs.Core.UserId("user-id")

let user: User =
  { Id = userId
    CurrentPresetId = Some presetId
    Presets = [ userPreset ] }