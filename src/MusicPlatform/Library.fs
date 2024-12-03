﻿namespace MusicPlatform

open System.Threading.Tasks

type PlaylistId = PlaylistId of string
type TrackId = TrackId of string

type ArtistId = ArtistId of string

type Artist = { Id: ArtistId }

type Track = { Id: TrackId; Artists: Set<Artist> }

[<RequireQualifiedAccess>]
module TrackId =
  let value (TrackId id) = id

[<RequireQualifiedAccess>]
module Playlist =
  type ListTracks = PlaylistId -> Task<Track list>
  type AddTracks = PlaylistId -> Track list -> Task<unit>
  type ReplaceTracks = PlaylistId -> Track list -> Task<unit>

[<RequireQualifiedAccess>]
module User =
  type ListLikedTracks = unit -> Task<Track list>