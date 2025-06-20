module MusicPlatform.Spotify.Mappings

open MusicPlatform
open MusicPlatform.Spotify.Helpers
open SpotifyAPI.Web

[<RequireQualifiedAccess>]
module Artist =
  let fromSimple (artist: SimpleArtist) : Artist = { Id = ArtistId artist.Id }

[<RequireQualifiedAccess>]
module Track =
  let fromFull (track: FullTrack) : Track =
    { Id = TrackId track.Id
      Artists = track.Artists |> Seq.map Artist.fromSimple |> Set.ofSeq }

  let fromSimple (track: SimpleTrack) : Track =
    { Id = TrackId track.Id
      Artists = track.Artists |> Seq.map Artist.fromSimple |> Set.ofSeq }

[<RequireQualifiedAccess>]
module Album =
  let fromFull (album: FullAlbum) : Album =
    { Id = AlbumId album.Id
      Tracks =
        album.Tracks.Items
        |> filterValidTracks
        |> Seq.map Track.fromSimple
        |> Seq.toList }