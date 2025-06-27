module Domain.Tests.Mocks

open Domain.Core
open MusicPlatform

let artist1 = { Id = ArtistId "artist-1" }
let artist2 = { Id = ArtistId "artist-2" }
let artist3 = { Id = ArtistId "artist-3" }
let artist4 = { Id = ArtistId "artist-4" }

let includedTrack =
  { Id = TrackId "included-track-id"
    Artists = Set.ofList [ artist1; artist2 ] }

let excludedTrack =
  { Id = TrackId "excluded-track-id"
    Artists = Set.ofList [ artist2; artist3 ] }

let likedTrack =
  { Id = TrackId "liked-track-id"
    Artists = Set.ofList [ artist3; artist4 ] }

let recommendedTrack =
  { Id = TrackId "recommended-track-id"
    Artists = Set.ofList [ artist1; artist3 ] }

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