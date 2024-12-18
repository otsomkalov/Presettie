﻿module Domain.Repos

open System.Threading.Tasks
open Domain.Core
open MusicPlatform
open otsom.fs.Core

[<RequireQualifiedAccess>]
module PresetRepo =
  type Load = PresetId -> Task<Preset>

  type Save = Preset -> Task<unit>
  type Remove = PresetId -> Task<unit>

  type ListExcludedTracks = ExcludedPlaylist list -> Task<Track list>

  type QueueRun = PresetId -> Task<unit>

[<RequireQualifiedAccess>]
module UserRepo =
  type Load = UserId -> Task<User>
  type Update = User -> Task<unit>
  type Exists = UserId -> Task<bool>
  type Create = User -> Task<unit>

  type ListLikedTracks = unit -> Task<Track list>

type IListPlaylistTracks =
  abstract member ListPlaylistTracks: PlaylistId -> Task<Track list>

type IListLikedTracks =
  abstract member ListLikedTracks : unit -> Task<Track list>