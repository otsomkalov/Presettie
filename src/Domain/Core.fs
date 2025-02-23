module Domain.Core

open System
open System.Threading.Tasks
open MusicPlatform
open otsom.fs.Core

type UserId with
  member this.ToMusicPlatformId() = this.Value |> MusicPlatform.UserId

type ReadablePlaylistId =
  | ReadablePlaylistId of PlaylistId

  member this.Value = let (ReadablePlaylistId id) = this in id

type WritablePlaylistId =
  | WritablePlaylistId of PlaylistId

  member this.Value = let (WritablePlaylistId id) = this in id

type IncludedPlaylistId = ReadablePlaylistId

type IncludedPlaylist =
  { Id: IncludedPlaylistId
    Name: string
    LikedOnly: bool }

type ExcludedPlaylist =
  { Id: ReadablePlaylistId; Name: string }

type TargetedPlaylistId = WritablePlaylistId

type TargetedPlaylist =
  { Id: TargetedPlaylistId
    Name: string
    Overwrite: bool }

type PresetId =
  | PresetId of string

  member this.Value = let (PresetId id) = this in id

[<RequireQualifiedAccess>]
module PresetSettings =
  [<RequireQualifiedAccess>]
  type LikedTracksHandling =
    | Include
    | Exclude
    | Ignore

  type RawPresetSize =
    | RawPresetSize of string
    member this.Value = let (RawPresetSize va) = this in va

  type ParsingError =
    | NotANumber
    | TooSmall
    | TooBig

  [<RequireQualifiedAccess>]
  type Size =
    | Size of int

    static member TryParse(size: RawPresetSize) =
      match Int32.TryParse size.Value with
      | true, s when s >= 10000 -> Error(TooBig)
      | true, s when s <= 0 -> Error(TooSmall)
      | true, s -> Ok(Size(s))
      | _ -> Error(NotANumber)

    member this.Value = let (Size size) = this in size

  type PresetSettings =
    { LikedTracksHandling: LikedTracksHandling
      Size: Size
      RecommendationsEnabled: bool
      UniqueArtists: bool }

  type EnableUniqueArtists = PresetId -> Task<unit>
  type DisableUniqueArtists = PresetId -> Task<unit>

  type IncludeLikedTracks = PresetId -> Task<unit>
  type ExcludeLikedTracks = PresetId -> Task<unit>
  type IgnoreLikedTracks = PresetId -> Task<unit>

type SimplePreset = { Id: PresetId; Name: string }

type Preset =
  { Id: PresetId
    Name: string
    Settings: PresetSettings.PresetSettings
    IncludedPlaylists: IncludedPlaylist list
    ExcludedPlaylists: ExcludedPlaylist list
    TargetedPlaylists: TargetedPlaylist list }

type User =
  { Id: UserId
    CurrentPresetId: PresetId option
    Presets: SimplePreset list }

[<RequireQualifiedAccess>]
module Preset =
  [<RequireQualifiedAccess>]
  type ValidationError =
    | NoIncludedPlaylists
    | NoTargetedPlaylists

  type Validate = Preset -> Result<Preset, ValidationError list>

  type RunError =
    | NoIncludedTracks
    | NoPotentialTracks
    | Unauthorized

  type IncludePlaylistError =
    | IdParsing of Playlist.IdParsingError
    | Load of Playlist.LoadError
    | Unauthorized

  type ExcludePlaylistError =
    | IdParsing of Playlist.IdParsingError
    | Load of Playlist.LoadError
    | Unauthorized

  type AccessError = AccessError of unit

  type TargetPlaylistError =
    | IdParsing of Playlist.IdParsingError
    | Load of Playlist.LoadError
    | AccessError of AccessError
    | Unauthorized

[<RequireQualifiedAccess>]
module IncludedPlaylist =
  let fromSpotifyPlaylist =
    function
    | Readable({ Id = id; Name = name }) ->
      { Id = (id |> ReadablePlaylistId)
        Name = name
        LikedOnly = false }
      : IncludedPlaylist
    | Writable({ Id = id; Name = name }) ->
      { Id = (id |> ReadablePlaylistId)
        Name = name
        LikedOnly = false }

[<RequireQualifiedAccess>]
module ExcludedPlaylist =
  let fromSpotifyPlaylist =
    function
    | Readable({ Id = id; Name = name }) ->
      { Id = (id |> ReadablePlaylistId)
        Name = name }
      : ExcludedPlaylist
    | Writable({ Id = id; Name = name }) ->
      { Id = (id |> ReadablePlaylistId)
        Name = name }

[<RequireQualifiedAccess>]
module TargetedPlaylist =
  let fromSpotifyPlaylist =
    function
    | Readable _ -> None
    | Writable({ Id = id; Name = name }) ->
      { Id = (id |> WritablePlaylistId)
        Name = name
        Overwrite = false }
      |> Some

type ISetPresetSize =
  abstract SetPresetSize: PresetId * PresetSettings.RawPresetSize -> Task<Result<unit, PresetSettings.ParsingError>>

type IQueueRun =
  abstract QueueRun: UserId * PresetId -> Task<Result<Preset, Preset.ValidationError list>>

type ICreatePreset =
  abstract CreatePreset: string -> Task<Preset>

type IIncludePlaylist =
  abstract IncludePlaylist: UserId * PresetId * Playlist.RawPlaylistId -> Task<Result<IncludedPlaylist, Preset.IncludePlaylistError>>

type IExcludePlaylist =
  abstract ExcludePlaylist: UserId * PresetId * Playlist.RawPlaylistId -> Task<Result<ExcludedPlaylist, Preset.ExcludePlaylistError>>

type ITargetPlaylist =
  abstract TargetPlaylist: UserId * PresetId * Playlist.RawPlaylistId -> Task<Result<TargetedPlaylist, Preset.TargetPlaylistError>>

type IEnableRecommendations =
  abstract EnableRecommendations: PresetId -> Task<unit>

type IDisableRecommendations =
  abstract DisableRecommendations: PresetId -> Task<unit>

type IEnableUniqueArtists =
  abstract EnableUniqueArtists: PresetId -> Task<unit>

type IDisableUniqueArtists =
  abstract DisableUniqueArtists: PresetId -> Task<unit>

type IIncludeLikedTracks =
  abstract IncludeLikedTracks: PresetId -> Task<unit>

type IExcludeLikedTracks =
  abstract ExcludeLikedTracks: PresetId -> Task<unit>

type IIgnoreLikedTracks =
  abstract IgnoreLikedTracks: PresetId -> Task<unit>

type IAppendToTargetedPlaylist =
  abstract AppendToTargetedPlaylist: PresetId * TargetedPlaylistId -> Task<unit>

type IOverwriteTargetedPlaylist =
  abstract OverwriteTargetedPlaylist: PresetId * TargetedPlaylistId -> Task<unit>

type IRemoveIncludedPlaylist =
  abstract RemoveIncludedPlaylist: PresetId * IncludedPlaylistId -> Task<unit>

type IRemoveExcludedPlaylist =
  abstract RemoveExcludedPlaylist: PresetId * ReadablePlaylistId -> Task<unit>

type IRemoveTargetedPlaylist =
  abstract RemoveTargetedPlaylist: PresetId * TargetedPlaylistId -> Task<unit>

type ISetOnlyLiked =
  abstract SetOnlyLiked: PresetId * IncludedPlaylistId -> Task<unit>

type ISetAll =
  abstract SetAll: PresetId * IncludedPlaylistId -> Task<unit>

type IRunPreset =
  abstract RunPreset: UserId * PresetId -> Task<Result<Preset, Preset.RunError>>

type IRemovePreset =
  abstract RemovePreset: PresetId -> Task<unit>

type IPresetService =
  inherit IQueueRun
  inherit IRunPreset

  inherit ISetPresetSize
  inherit ICreatePreset
  inherit IRemovePreset

  inherit IIncludePlaylist
  inherit IExcludePlaylist
  inherit ITargetPlaylist

  inherit IEnableRecommendations
  inherit IDisableRecommendations

  inherit IEnableUniqueArtists
  inherit IDisableUniqueArtists

  inherit IIncludeLikedTracks
  inherit IExcludeLikedTracks
  inherit IIgnoreLikedTracks

  inherit IAppendToTargetedPlaylist
  inherit IOverwriteTargetedPlaylist

  inherit IRemoveIncludedPlaylist
  inherit IRemoveExcludedPlaylist
  inherit IRemoveTargetedPlaylist

  inherit ISetOnlyLiked
  inherit ISetAll

type ISetCurrentPresetSize =
  abstract SetCurrentPresetSize: UserId * PresetSettings.RawPresetSize -> Task<Result<unit, PresetSettings.ParsingError>>

type ICreateUserPreset =
  abstract CreateUserPreset: UserId * string -> Task<Preset>

type ISetCurrentPreset =
  abstract SetCurrentPreset: UserId * PresetId -> Task<unit>

type IRemoveUserPreset =
  abstract RemoveUserPreset: UserId * PresetId -> Task<unit>

type ICreateUser =
  abstract CreateUser: unit -> Task<User>

type IUserService =
  inherit ISetCurrentPresetSize
  inherit ICreateUserPreset
  inherit ISetCurrentPreset
  inherit IRemoveUserPreset
  inherit ICreateUser