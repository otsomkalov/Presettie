﻿module Domain.Workflows

open System.Threading.Tasks
open Domain.Core
open Domain.Extensions
open Microsoft.FSharp.Control

[<RequireQualifiedAccess>]
module User =
  type ListLikedTracks = Async<string list>
  type Load = UserId -> Async<User>

[<RequireQualifiedAccess>]
module ValidateUserPlaylists =
  let validateUserPlaylists (loadUser: User.Load) : ValidateUserPlaylists.Action =
    fun userId ->
      task {
        let! user = loadUser userId

        return
          match user.IncludedPlaylists, user.TargetPlaylists with
          | [], [] ->
            [ ValidateUserPlaylists.NoIncludedPlaylists
              ValidateUserPlaylists.NoTargetPlaylists ]
            |> ValidateUserPlaylists.Errors
          | [], _ -> [ ValidateUserPlaylists.NoIncludedPlaylists ] |> ValidateUserPlaylists.Errors
          | _, [] -> [ ValidateUserPlaylists.NoTargetPlaylists ] |> ValidateUserPlaylists.Errors
          | _, _ -> ValidateUserPlaylists.Ok
      }

[<RequireQualifiedAccess>]
module UserSettings =
  type Load = UserId -> Task<UserSettings.UserSettings>

  type Update = UserId -> UserSettings.UserSettings -> Task

  let setPlaylistSize (loadUserSettings: Load) (updateUserSettings: Update) : UserSettings.SetPlaylistSize =
    fun userId playlistSize ->
      task {
        let! userSettings = loadUserSettings userId

        let updatedSettings = { userSettings with PlaylistSize = playlistSize }

        do! updateUserSettings userId updatedSettings
      }

  let setLikedTracksHandling (loadUserSettings: Load) (updateInStorage: Update) : UserSettings.SetLikedTracksHandling =
    fun userId likedTracksHandling ->
      task {
        let! userSettings = loadUserSettings userId

        let updatedSettings =
          { userSettings with LikedTracksHandling = likedTracksHandling }

        do! updateInStorage userId updatedSettings
      }

[<RequireQualifiedAccess>]
module Playlist =
  type ListTracks = ReadablePlaylistId -> Async<string list>

  type Update = TargetPlaylist -> TrackId list -> Async<unit>

  type ParsedPlaylistId = ParsedPlaylistId of string

  type ParseId = Playlist.RawPlaylistId -> Result<ParsedPlaylistId, Playlist.IdParsingError>

  type TryParseId = Playlist.RawPlaylistId -> Result<ParsedPlaylistId, Playlist.IncludePlaylistError>

  type CheckExistsInSpotify = ParsedPlaylistId -> Async<Result<SpotifyPlaylist, Playlist.MissingFromSpotifyError>>

  type CheckWriteAccess = SpotifyPlaylist -> Async<Result<WritablePlaylistId, Playlist.AccessError>>

  type IncludeInStorage = ReadablePlaylistId -> Async<unit>
  type ExcludeInStorage = ReadablePlaylistId -> Async<unit>
  type TargetInStorage = WritablePlaylistId -> Async<WritablePlaylistId>

  let includePlaylist
    (parseId: ParseId)
    (existsInSpotify: CheckExistsInSpotify)
    (includeInStorage: IncludeInStorage)
    : Playlist.IncludePlaylist =
    let parseId = parseId >> Result.mapError Playlist.IncludePlaylistError.IdParsing

    let existsInSpotify =
      existsInSpotify
      >> AsyncResult.mapError Playlist.IncludePlaylistError.MissingFromSpotify

    parseId
    >> Result.asyncBind existsInSpotify
    >> AsyncResult.map (fun p -> ReadablePlaylistId p.Id)
    >> AsyncResult.asyncMap includeInStorage

  let excludePlaylist
    (parseId: ParseId)
    (existsInSpotify: CheckExistsInSpotify)
    (excludeInStorage: ExcludeInStorage)
    : Playlist.ExcludePlaylist =
    let parseId = parseId >> Result.mapError Playlist.ExcludePlaylistError.IdParsing

    let existsInSpotify =
      existsInSpotify
      >> AsyncResult.mapError Playlist.ExcludePlaylistError.MissingFromSpotify

    parseId
    >> Result.asyncBind existsInSpotify
    >> AsyncResult.map (fun p -> ReadablePlaylistId p.Id)
    >> AsyncResult.asyncMap excludeInStorage

  let targetPlaylist
    (parseId: ParseId)
    (existsInSpotify: CheckExistsInSpotify)
    (checkWriteAccess: CheckWriteAccess)
    (targetInStorage: TargetInStorage)
    : Playlist.TargetPlaylist =
    let parseId = parseId >> Result.mapError Playlist.TargetPlaylistError.IdParsing

    let existsInSpotify =
      existsInSpotify
      >> AsyncResult.mapError Playlist.TargetPlaylistError.MissingFromSpotify

    let checkWriteAccess =
      checkWriteAccess
      >> AsyncResult.mapError Playlist.TargetPlaylistError.AccessError

    parseId
    >> Result.asyncBind existsInSpotify
    >> AsyncResult.bind checkWriteAccess
    >> AsyncResult.asyncMap targetInStorage

[<RequireQualifiedAccess>]
module TargetPlaylist =
  type AppendToTargetPlaylist = UserId -> WritablePlaylistId -> Task<unit>
  type OverwriteTargetPlaylist = UserId -> WritablePlaylistId -> Task<unit>