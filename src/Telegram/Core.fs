﻿module Telegram.Core

open System.Threading.Tasks
open Domain.Core

type AnswerCallbackQuery = string -> Task<unit>
type Page = Page of int

type SendUserPresets = UserId -> Task<unit>
type SendCurrentPresetInfo = UserId -> Task<unit>
type SetCurrentPreset = UserId -> PresetId -> Task<unit>

type SendSettingsMessage = UserId -> Task<unit>

type SendPresetInfo = PresetId -> Task<unit>
type SetLikedTracksHandling = PresetId -> PresetSettings.LikedTracksHandling -> Task<unit>
type GetPresetMessage = PresetId -> Task<string * string * string>

type ShowIncludedPlaylists = PresetId -> Page -> Task<unit>
type ShowIncludedPlaylist = PresetId -> ReadablePlaylistId -> Task<unit>
type EnableIncludedPlaylist = PresetId -> ReadablePlaylistId -> Task<unit>
type DisableIncludedPlaylist = PresetId -> ReadablePlaylistId -> Task<unit>
type RemoveIncludedPlaylist = PresetId -> ReadablePlaylistId -> Task<unit>

type ShowExcludedPlaylists = PresetId -> Page -> Task<unit>
type ShowExcludedPlaylist = PresetId -> ReadablePlaylistId -> Task<unit>
type RemoveExcludedPlaylist = PresetId -> ReadablePlaylistId -> Task<unit>

type ShowTargetedPlaylists = PresetId -> Page -> Task<unit>
type ShowTargetedPlaylist = PresetId -> WritablePlaylistId -> Task<unit>
type AppendToTargetedPlaylist = PresetId -> WritablePlaylistId -> Task<unit>
type OverwriteTargetedPlaylist = PresetId -> WritablePlaylistId -> Task<unit>
type RemoveTargetedPlaylist = PresetId -> WritablePlaylistId -> Task<unit>

[<RequireQualifiedAccess>]
type Action =

  | ShowIncludedPlaylists of presetId: PresetId * page: Page
  | ShowIncludedPlaylist of presetId: PresetId * playlistId: ReadablePlaylistId
  | EnableIncludedPlaylist of presetId: PresetId * playlistId: ReadablePlaylistId
  | DisableIncludedPlaylist of presetId: PresetId * playlistId: ReadablePlaylistId
  | RemoveIncludedPlaylist of presetId: PresetId * playlistId: ReadablePlaylistId

  | ShowExcludedPlaylists of presetId: PresetId * page: Page
  | ShowExcludedPlaylist of presetId: PresetId * playlistId: ReadablePlaylistId
  | RemoveExcludedPlaylist of presetId: PresetId * playlistId: ReadablePlaylistId

  | ShowTargetedPlaylists of presetId: PresetId * page: Page
  | ShowTargetedPlaylist of presetId: PresetId * playlistId: WritablePlaylistId
  | AppendToTargetedPlaylist of presetId: PresetId * playlistId: WritablePlaylistId
  | OverwriteTargetedPlaylist of presetId: PresetId * playlistId: WritablePlaylistId
  | RemoveTargetedPlaylist of presetId: PresetId * playlistId: WritablePlaylistId

  | ShowPresetInfo of presetId: PresetId
  | SetCurrentPreset of presetId: PresetId

  | AskForPlaylistSize

  | IncludeLikedTracks of presetId: PresetId
  | ExcludeLikedTracks of presetId: PresetId
  | IgnoreLikedTracks of presetId: PresetId

  | ShowUserPresets

type AuthState =
  | Authorized
  | Unauthorized

type CheckAuth = UserId -> Task<AuthState>