module Infrastructure.Core

open MusicPlatform

[<RequireQualifiedAccess>]
module RawPlaylistId =
  let value (Playlist.RawPlaylistId rawId) = rawId