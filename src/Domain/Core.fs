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

type IncludedArtist = Artist

type ExcludedArtist = Artist

type TargetedPlaylistId = WritablePlaylistId

type TargetedPlaylist =
  { Id: TargetedPlaylistId
    Name: string
    Overwrite: bool }

type PresetId =
  | PresetId of string

  member this.Value = let (PresetId id) = this in id

type RawPresetId = RawPresetId of string

type SimplePreset = { Id: PresetId; Name: string }

module PresetSettings =
  [<RequireQualifiedAccess>]
  type LikedTracksHandling =
    | Include
    | Exclude
    | Ignore

  type RawPresetSize =
    | RawPresetSize of string

    member this.Value = let (RawPresetSize va) = this in va

  type RecommendationsEngine =
    | ArtistAlbums
    | ReccoBeats
    | Spotify

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
      RecommendationsEngine: RecommendationsEngine option
      UniqueArtists: bool }

type Preset =
  { Id: PresetId
    Name: string
    OwnerId: UserId
    Settings: PresetSettings.PresetSettings
    IncludedPlaylists: IncludedPlaylist list
    ExcludedPlaylists: ExcludedPlaylist Set
    IncludedArtists: IncludedArtist Set
    ExcludedArtists: ExcludedArtist Set
    TargetedPlaylists: TargetedPlaylist list }

type User =
  { Id: UserId
    CurrentPresetId: PresetId option
    MusicPlatforms: MusicPlatform.UserId list }

[<RequireQualifiedAccess>]
module ExcludedPlaylist =
  let fromPlatform =
    function
    | Readable({ Id = id; Name = name }) ->
      { Id = (id |> ReadablePlaylistId)
        Name = name }
      : ExcludedPlaylist
    | Writable({ Id = id; Name = name }) ->
      { Id = (id |> ReadablePlaylistId)
        Name = name }

[<RequireQualifiedAccess>]
module ExcludedArtist =
  let fromPlatform (artist: MusicPlatform.Artist) : ExcludedArtist =
    { Id = artist.Id
      Name = artist.Name }

[<RequireQualifiedAccess>]
module IncludedArtist =
  let fromPlatform (artist: MusicPlatform.Artist) : IncludedArtist =
    { Id = artist.Id
      Name = artist.Name }

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
    | AlreadyExcluded
    | Unauthorized

  type IncludeArtistError =
    | Load of Artist.LoadError
    | IdParsing of Artist.IdParsingError
    | AlreadyIncluded
    | Unauthorized

  type ExcludeArtistError =
    | Load of Artist.LoadError
    | IdParsing of Artist.IdParsingError
    | AlreadyExcluded
    | Unauthorized

  type AccessError = AccessError of unit

  type TargetPlaylistError =
    | IdParsing of Playlist.IdParsingError
    | Load of Playlist.LoadError
    | AccessError of AccessError
    | Unauthorized

  type GetPresetError = | NotFound

  type ExcludePlaylistResult =
    { Preset: Preset
      Playlist: ExcludedPlaylist }

  let excludePlaylist (preset: Preset) (playlist: Playlist) =
    let existingPlaylist =
      preset.ExcludedPlaylists
      |> Seq.tryFind (fun p -> p.Id = ReadablePlaylistId playlist.Id)

    match existingPlaylist with
    | Some _ -> Error(ExcludePlaylistError.AlreadyExcluded)
    | None ->
      let excludedPlaylist = ExcludedPlaylist.fromPlatform playlist

      let updatedPreset =
        { preset with
            ExcludedPlaylists = preset.ExcludedPlaylists |> Set.add excludedPlaylist }

      Ok(
        { Preset = updatedPreset
          Playlist = excludedPlaylist }
      )

  type RemoveExcludedPlaylistError = | NotExcluded

  let removeExcludedPlaylist (preset: Preset) (playlistId: ReadablePlaylistId) =
    let existingPlaylist =
      preset.ExcludedPlaylists |> Seq.tryFind (fun p -> p.Id = playlistId)

    match existingPlaylist with
    | None -> Error(NotExcluded)
    | Some playlist ->
      Ok
        { preset with
            ExcludedPlaylists = preset.ExcludedPlaylists |> Set.remove playlist }

  type ExcludeArtistResult =
    { Preset: Preset
      Artist: ExcludedArtist }

  let excludeArtist (preset: Preset) (artist: MusicPlatform.Artist) =
    let existingArtist =
      preset.ExcludedArtists
      |> Seq.tryFind (fun a -> a.Id = artist.Id)

    match existingArtist with
    | Some _ -> Error(ExcludeArtistError.AlreadyExcluded)
    | None ->
      let excludedArtist = ExcludedArtist.fromPlatform artist

      let updatedPreset =
        { preset with
            ExcludedArtists = preset.ExcludedArtists |> Set.add excludedArtist }

      Ok(
        { Preset = updatedPreset
          Artist = excludedArtist }
      )

  type RemoveExcludedArtistError = | NotExcluded

  let removeExcludedArtist (preset: Preset) (artistId: ArtistId) =
    let existingArtist =
      preset.ExcludedArtists |> Seq.tryFind (fun a -> a.Id = artistId)

    match existingArtist with
    | None -> Error(NotExcluded)
    | Some artist ->
      Ok
        { preset with
            ExcludedArtists = preset.ExcludedArtists |> Set.remove artist }

  type IncludeArtistResult =
    { Preset: Preset
      Artist: IncludedArtist }

  let includeArtist (preset: Preset) (artist: MusicPlatform.Artist) =
    let existingArtist =
      preset.IncludedArtists
      |> Seq.tryFind (fun a -> a.Id = artist.Id)

    match existingArtist with
    | Some _ -> Error(IncludeArtistError.AlreadyIncluded)
    | None ->
      let includedArtist = IncludedArtist.fromPlatform artist

      let updatedPreset =
        { preset with
            IncludedArtists = preset.IncludedArtists |> Set.add includedArtist }

      Ok(
        { Preset = updatedPreset
          Artist = includedArtist }
      )

  type RemoveIncludedArtistError = | NotIncluded

  let removeIncludedArtist (preset: Preset) (artistId: ArtistId) =
    let existingArtist =
      preset.IncludedArtists |> Seq.tryFind (fun a -> a.Id = artistId)

    match existingArtist with
    | None -> Error(NotIncluded)
    | Some artist ->
      Ok
        { preset with
            IncludedArtists = preset.IncludedArtists |> Set.remove artist }

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
  abstract CreatePreset: UserId * string -> Task<Preset>

type IIncludePlaylist =
  abstract IncludePlaylist: UserId * PresetId * Playlist.RawPlaylistId -> Task<Result<IncludedPlaylist, Preset.IncludePlaylistError>>

type IExcludePlaylist =
  abstract ExcludePlaylist:
    UserId * PresetId * Playlist.RawPlaylistId -> Task<Result<Preset.ExcludePlaylistResult, Preset.ExcludePlaylistError>>

type IIncludeArtist =
  abstract IncludeArtist: UserId * PresetId * Artist.RawArtistId -> Task<Result<Preset.IncludeArtistResult, Preset.IncludeArtistError>>

type IExcludeArtist =
  abstract ExcludeArtist: UserId * PresetId * Artist.RawArtistId -> Task<Result<Preset.ExcludeArtistResult, Preset.ExcludeArtistError>>

type ITargetPlaylist =
  abstract TargetPlaylist: UserId * PresetId * Playlist.RawPlaylistId -> Task<Result<TargetedPlaylist, Preset.TargetPlaylistError>>

type ISetRecommendationsEngine =
  abstract SetRecommendationsEngine: PresetId * PresetSettings.RecommendationsEngine option -> Task<unit>

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
  abstract RemoveIncludedPlaylist: PresetId * IncludedPlaylistId -> Task<Preset>

type IRemoveExcludedPlaylist =
  abstract RemoveExcludedPlaylist: PresetId * ReadablePlaylistId -> Task<Result<Preset, Preset.RemoveExcludedPlaylistError>>

type IRemoveIncludedArtist =
  abstract RemoveIncludedArtist: PresetId * ArtistId -> Task<Result<Preset, Preset.RemoveIncludedArtistError>>

type IRemoveExcludedArtist =
  abstract RemoveExcludedArtist: PresetId * ArtistId -> Task<Result<Preset, Preset.RemoveExcludedArtistError>>

type IRemoveTargetedPlaylist =
  abstract RemoveTargetedPlaylist: PresetId * TargetedPlaylistId -> Task<Preset>

type ISetOnlyLiked =
  abstract SetOnlyLiked: PresetId * IncludedPlaylistId -> Task<unit>

type ISetAll =
  abstract SetAll: PresetId * IncludedPlaylistId -> Task<unit>

type IRunPreset =
  abstract RunPreset: UserId * PresetId -> Task<Result<Preset, Preset.RunError>>

type IRemovePreset =
  abstract RemovePreset: UserId * RawPresetId -> Task<Result<Preset, Preset.GetPresetError>>

type IGetPreset =
  abstract GetPreset: UserId * RawPresetId -> Task<Result<Preset, Preset.GetPresetError>>

type IPresetService =
  inherit IQueueRun
  inherit IRunPreset

  inherit ISetPresetSize
  inherit ICreatePreset
  inherit IRemovePreset
  inherit IGetPreset

  inherit IIncludePlaylist
  inherit IExcludePlaylist
  inherit IIncludeArtist
  inherit IExcludeArtist
  inherit ITargetPlaylist

  inherit ISetRecommendationsEngine

  inherit IEnableUniqueArtists
  inherit IDisableUniqueArtists

  inherit IIncludeLikedTracks
  inherit IExcludeLikedTracks
  inherit IIgnoreLikedTracks

  inherit IAppendToTargetedPlaylist
  inherit IOverwriteTargetedPlaylist

  inherit IRemoveIncludedPlaylist
  inherit IRemoveExcludedPlaylist
  inherit IRemoveIncludedArtist
  inherit IRemoveExcludedArtist
  inherit IRemoveTargetedPlaylist

  inherit ISetOnlyLiked
  inherit ISetAll

type ISetCurrentPresetSize =
  abstract SetCurrentPresetSize: UserId * PresetSettings.RawPresetSize -> Task<Result<unit, PresetSettings.ParsingError>>

type ISetCurrentPreset =
  abstract SetCurrentPreset: UserId * PresetId -> Task<unit>

type IRemoveUserPreset =
  abstract RemoveUserPreset: UserId * RawPresetId -> Task<Result<unit, Preset.GetPresetError>>

type ICreateUser =
  abstract CreateUser: unit -> Task<User>

type IUserService =
  inherit ISetCurrentPresetSize
  inherit ISetCurrentPreset
  inherit IRemoveUserPreset
  inherit ICreateUser

type IRecommenderFactory =
  abstract Create: PresetSettings.RecommendationsEngine -> IRecommender