﻿module internal Infrastructure.Mapping

open System
open Database
open Domain.Core
open Domain.Core.PresetSettings
open Domain.Workflows
open MongoDB.Bson
open MusicPlatform
open otsom.fs.Core

[<RequireQualifiedAccess>]
module SimplePreset =
  let fromDb (preset: {| Id: ObjectId; Name: string |}) : SimplePreset =
    { Id = preset.Id |> string |> PresetId
      Name = preset.Name }

[<RequireQualifiedAccess>]
module User =
  let fromDb (user: Entities.User) : User =
    { Id = user.Id.ToString() |> UserId
      CurrentPresetId = user.CurrentPresetId |> Option.ofNullable |> Option.map (string >> PresetId)
      MusicPlatforms = user.MusicPlatforms |> Seq.map MusicPlatform.UserId |> List.ofSeq }

  let toDb (user: User) : Entities.User =
    Entities.User(
      Id = (user.Id.Value |> ObjectId),
      CurrentPresetId =
        (user.CurrentPresetId
         |> Option.map (_.Value >> ObjectId.Parse)
         |> Option.toNullable)
    )

[<RequireQualifiedAccess>]
module IncludedPlaylist =
  let fromDb (playlist: Entities.IncludedPlaylist) : IncludedPlaylist =
    { Id = playlist.Id |> PlaylistId |> ReadablePlaylistId
      Name = playlist.Name
      LikedOnly = playlist.LikedOnly }

  let toDb (playlist: IncludedPlaylist) : Entities.IncludedPlaylist =
    Entities.IncludedPlaylist(Id = playlist.Id.Value.Value, Name = playlist.Name, LikedOnly = playlist.LikedOnly)

[<RequireQualifiedAccess>]
module ExcludedPlaylist =
  let fromDb (playlist: Entities.ExcludedPlaylist) : ExcludedPlaylist =
    { Id = playlist.Id |> PlaylistId |> ReadablePlaylistId
      Name = playlist.Name }

  let toDb (playlist: ExcludedPlaylist) : Entities.ExcludedPlaylist =
    Entities.ExcludedPlaylist(Id = playlist.Id.Value.Value, Name = playlist.Name)

[<RequireQualifiedAccess>]
module TargetedPlaylist =
  let private fromDb (playlist: Entities.TargetedPlaylist) : TargetedPlaylist =
    { Id = playlist.Id |> PlaylistId |> WritablePlaylistId
      Name = playlist.Name
      Overwrite = playlist.Overwrite }

  let toDb (playlist: TargetedPlaylist) : Entities.TargetedPlaylist =
    Entities.TargetedPlaylist(Id = playlist.Id.Value.Value, Name = playlist.Name, Overwrite = playlist.Overwrite)

  let mapPlaylists (playlists: Entities.TargetedPlaylist seq) =
    playlists |> Seq.map fromDb |> Seq.toList

module PresetSettings =
  let fromDb (settings: Entities.Settings) : PresetSettings.PresetSettings =
    { LikedTracksHandling =
        (match settings.IncludeLikedTracks |> Option.ofNullable with
         | Some true -> LikedTracksHandling.Include
         | Some false -> LikedTracksHandling.Exclude
         | None -> LikedTracksHandling.Ignore)
      Size = settings.Size |> Size.Size
      RecommendationsEngine =
        settings.RecommendationsEngine
        |> Option.ofNullable
        |> Option.map (function
          | Entities.RecommendationsEngine.ArtistsAlbums -> RecommendationsEngine.ArtistAlbums)
      UniqueArtists = settings.UniqueArtists }

  let toDb (settings: PresetSettings.PresetSettings) : Entities.Settings =
    Entities.Settings(
      IncludeLikedTracks =
        (match settings.LikedTracksHandling with
         | LikedTracksHandling.Include -> Nullable true
         | LikedTracksHandling.Exclude -> Nullable false
         | LikedTracksHandling.Ignore -> Nullable<bool>()),
      Size = settings.Size.Value,
      RecommendationsEngine =
        (match settings.RecommendationsEngine with
         | Some RecommendationsEngine.ArtistAlbums -> Nullable<_> Entities.RecommendationsEngine.ArtistsAlbums
         | None -> Nullable<Entities.RecommendationsEngine>()),
      UniqueArtists = settings.UniqueArtists
    )

[<RequireQualifiedAccess>]
module Preset =
  let fromDb (preset: Entities.Preset) : Preset =
    let mapIncludedPlaylist playlists =
      playlists |> Seq.map IncludedPlaylist.fromDb |> Seq.toList

    let mapExcludedPlaylist playlists =
      playlists |> Seq.map ExcludedPlaylist.fromDb |> Seq.toList

    { Id = preset.Id |> string |> PresetId
      Name = preset.Name
      OwnerId = preset.OwnerId |> string |> UserId
      IncludedPlaylists = mapIncludedPlaylist preset.IncludedPlaylists
      ExcludedPlaylists = mapExcludedPlaylist preset.ExcludedPlaylists
      TargetedPlaylists = TargetedPlaylist.mapPlaylists preset.TargetedPlaylists
      Settings = PresetSettings.fromDb preset.Settings }

  let toDb (preset: Domain.Core.Preset) : Entities.Preset =
    Entities.Preset(
      Id = (preset.Id.Value |> ObjectId.Parse),
      Name = preset.Name,
      Settings = (preset.Settings |> PresetSettings.toDb),
      OwnerId = (preset.OwnerId.Value |> ObjectId.Parse),
      IncludedPlaylists = (preset.IncludedPlaylists |> Seq.map IncludedPlaylist.toDb),
      ExcludedPlaylists = (preset.ExcludedPlaylists |> Seq.map ExcludedPlaylist.toDb),
      TargetedPlaylists = (preset.TargetedPlaylists |> Seq.map TargetedPlaylist.toDb)
    )