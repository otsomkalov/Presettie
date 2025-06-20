namespace MusicPlatform

open System.Threading.Tasks

type UserId =
  | UserId of string

  member this.Value = let (UserId id) = this in id

type PlaylistId =
  | PlaylistId of string

  member this.Value = let (PlaylistId id) = this in id

type TrackId =
  | TrackId of string

  member this.Value = let (TrackId id) = this in id

type AlbumId =
  | AlbumId of string

  member this.Value = let (AlbumId id) = this in id

type ArtistId =
  | ArtistId of string

  member this.Value = let (ArtistId id) = this in id

type Artist = { Id: ArtistId }

type Track = { Id: TrackId; Artists: Set<Artist> }

type Album = { Id: AlbumId; Tracks: Track list }

type PlaylistData =
  { Id: PlaylistId
    Name: string
    TracksCount: int }

type Playlist =
  | Readable of PlaylistData
  | Writable of PlaylistData

[<RequireQualifiedAccess>]
module Playlist =
  type LoadError = | NotFound

  type RawPlaylistId =
    | RawPlaylistId of string

    member this.Value = let (RawPlaylistId id) = this in id

  type IdParsingError = IdParsingError of string

  type ParseId = RawPlaylistId -> Result<PlaylistId, IdParsingError>

[<RequireQualifiedAccess>]
module User =
  type ListLikedTracks = unit -> Task<Track list>

[<RequireQualifiedAccess>]
module Track =
  type GetRecommendations = TrackId list -> Task<Track list>

type ILoadPlaylist =
  abstract LoadPlaylist: PlaylistId -> Task<Result<Playlist, Playlist.LoadError>>

type IReplaceTracks =
  abstract ReplaceTracks: PlaylistId * Track list -> Task<unit>

type IAddTracks =
  abstract AddTracks: PlaylistId * Track list -> Task<unit>

type IListPlaylistTracks =
  abstract ListPlaylistTracks: PlaylistId -> Task<Track list>

type IListLikedTracks =
  abstract ListLikedTracks: unit -> Task<Track list>

type IListArtistTracks =
  abstract ListArtistTracks: ArtistId -> Task<Track list>

type IGetRecommendations =
  abstract GetRecommendations: TrackId list -> Task<Track list>

type IMusicPlatform =
  inherit ILoadPlaylist
  inherit IReplaceTracks
  inherit IAddTracks
  inherit IListPlaylistTracks
  inherit IListLikedTracks
  inherit IListArtistTracks

type IMusicPlatformFactory =
  abstract GetMusicPlatform: UserId -> Task<IMusicPlatform option>