﻿module Infrastructure.Mapping

open System
open Database.Entities
open Infrastructure.Core
open Domain.Core
open Infrastructure.Core

[<RequireQualifiedAccess>]
module ReadablePlaylistId =
  let filterMapPlaylistsIds (playlists: SourcePlaylist seq) type' =
    playlists
    |> Seq.where (fun p -> p.PlaylistType = type')
    |> Seq.map (fun p -> ReadablePlaylistId p.Url)
    |> Seq.toList

[<RequireQualifiedAccess>]
module WritablePlaylistId =
  let filterMapPlaylistsIds (playlists: TargetPlaylist seq) =
    playlists
    |> Seq.map (fun p -> WritablePlaylistId p.Url)
    |> Seq.toList

module UserSettings =
  let fromDb (settings: Settings) : UserSettings.UserSettings =
    { LikedTracksHandling =
        (match settings.IncludeLikedTracks |> Option.ofNullable with
         | Some v when v = true -> LikedTracksHandling.Include
         | Some v when v = false -> LikedTracksHandling.Exclude
         | None -> LikedTracksHandling.Ignore)
      PlaylistSize = settings.PlaylistSize }

  let toDb (settings: UserSettings.UserSettings) : Settings =
    Settings(
      IncludeLikedTracks =
        (match settings.LikedTracksHandling with
         | LikedTracksHandling.Include -> Nullable true
         | LikedTracksHandling.Exclude -> Nullable false
         | LikedTracksHandling.Ignore -> Nullable<bool>()),
      PlaylistSize = settings.PlaylistSize
    )

[<RequireQualifiedAccess>]
module User =
  let fromDb userId (playlists: SourcePlaylist seq) (targetPlaylists: TargetPlaylist seq) =
    { Id = userId
      IncludedPlaylists = ReadablePlaylistId.filterMapPlaylistsIds playlists PlaylistType.Source
      TargetPlaylists = WritablePlaylistId.filterMapPlaylistsIds targetPlaylists }

module UserSettings =
  let fromDb (settings: Settings) : UserSettings.UserSettings =
    { LikedTracksHandling =
        (match settings.IncludeLikedTracks |> Option.ofNullable with
         | Some v when v = true -> UserSettings.LikedTracksHandling.Include
         | Some v when v = false -> UserSettings.LikedTracksHandling.Exclude
         | None -> UserSettings.LikedTracksHandling.Ignore)
      PlaylistSize = PlaylistSize.create settings.PlaylistSize }

  let toDb (settings: UserSettings.UserSettings) : Settings =
    Settings(
      IncludeLikedTracks =
        (match settings.LikedTracksHandling with
         | UserSettings.LikedTracksHandling.Include -> Nullable true
         | UserSettings.LikedTracksHandling.Exclude -> Nullable false
         | UserSettings.LikedTracksHandling.Ignore -> Nullable<bool>()),
      PlaylistSize = (settings.PlaylistSize |> PlaylistSize.value)
    )

  let toDb userId settings =
    User(
      Id = (userId |> UserId.value),
      Settings = UserSettings.toDb settings)