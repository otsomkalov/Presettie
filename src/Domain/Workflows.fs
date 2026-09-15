module Domain.Workflows

open System
open System.Threading.Tasks
open Domain.Core.PresetSettings
open Domain.Repos
open FSharp.Control
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.FSharp.Core
open MusicPlatform
open otsom.fs.Extensions
open Domain.Extensions
open FsToolkit.ErrorHandling
open Domain.Core

type Shuffler<'a> = 'a list -> 'a list

[<RequireQualifiedAccess>]
module Tracks =
  let uniqueByArtists (tracks: Track seq) =
    let addUniqueTrack (knownArtists, uniqueTracks) currentTrack =
      if knownArtists |> Set.intersect currentTrack.Artists |> Set.isEmpty then
        (knownArtists |> Set.union currentTrack.Artists, currentTrack :: uniqueTracks)
      else
        knownArtists, uniqueTracks

    tracks |> Seq.fold addUniqueTrack (Set.empty, []) |> snd |> List.rev

[<RequireQualifiedAccess>]
module PresetSettings =
  let private setUniqueArtists (presetRepo: #ILoadPreset & #ISavePreset) =
    fun uniqueArtists ->
      presetRepo.LoadPreset
      >> Task.map Option.get
      >> Task.map (fun preset ->
        { preset with
            Settings.UniqueArtists = uniqueArtists })
      >> Task.bind presetRepo.SavePreset

  let enableUniqueArtists presetRepo = setUniqueArtists presetRepo true

  let disableUniqueArtists presetRepo = setUniqueArtists presetRepo false

  let setRecommendationsEngine (presetRepo: #ILoadPreset & #ISavePreset) =
    fun engine ->
      presetRepo.LoadPreset
      >> Task.map Option.get
      >> Task.map (fun preset ->
        { preset with
            Settings.RecommendationsEngine = engine })
      >> Task.bind presetRepo.SavePreset

  let private setLikedTracksHandling (presetRepo: #ILoadPreset & #ISavePreset) =
    fun handling presetId ->
      presetId
      |> presetRepo.LoadPreset
      |> Task.map Option.get
      |> Task.map (fun p ->
        { p with
            Settings.LikedTracksHandling = handling })
      |> Task.bind presetRepo.SavePreset

  let includeLikedTracks presetRepo =
    setLikedTracksHandling presetRepo LikedTracksHandling.Include

  let excludeLikedTracks presetRepo =
    setLikedTracksHandling presetRepo LikedTracksHandling.Exclude

  let ignoreLikedTracks presetRepo =
    setLikedTracksHandling presetRepo LikedTracksHandling.Ignore

[<RequireQualifiedAccess>]
module IncludedPlaylist =
  let remove (presetRepo: #ILoadPreset & #ISavePreset) =
    fun presetId includedPlaylistId -> task {
      let! preset = presetRepo.LoadPreset presetId |> Task.map Option.get

      let includedPlaylists =
        preset.IncludedPlaylists |> List.filter (fun p -> p.Id <> includedPlaylistId)

      let updatedPreset =
        { preset with
            IncludedPlaylists = includedPlaylists }

      do! presetRepo.SavePreset updatedPreset

      return updatedPreset
    }

  let setAll (presetRepo: #ILoadPreset & #ISavePreset) =
    fun presetId playlistId -> task {
      let! preset = presetRepo.LoadPreset presetId |> Task.map Option.get

      let includedPlaylist =
        preset.IncludedPlaylists |> List.find (fun p -> p.Id = playlistId)

      let updatedPlaylist =
        { includedPlaylist with
            LikedOnly = false }

      let includedPlaylists =
        preset.IncludedPlaylists
        |> List.filter (fun p -> p.Id <> playlistId)
        |> List.append [ updatedPlaylist ]

      let updatedPreset =
        { preset with
            IncludedPlaylists = includedPlaylists }

      return! presetRepo.SavePreset updatedPreset
    }

  let setLikedOnly (presetRepo: #ILoadPreset & #ISavePreset) =
    fun presetId playlistId -> task {
      let! preset = presetRepo.LoadPreset presetId |> Task.map Option.get

      let includedPlaylist =
        preset.IncludedPlaylists |> List.find (fun p -> p.Id = playlistId)

      let updatedPlaylist =
        { includedPlaylist with
            LikedOnly = true }

      let includedPlaylists =
        preset.IncludedPlaylists
        |> List.filter (fun p -> p.Id <> playlistId)
        |> List.append [ updatedPlaylist ]

      let updatedPreset =
        { preset with
            IncludedPlaylists = includedPlaylists }

      return! presetRepo.SavePreset updatedPreset
    }

[<RequireQualifiedAccess>]
module ExcludedPlaylist =
  let remove (presetRepo: #ILoadPreset & #ISavePreset) =
    fun presetId excludedPlaylistId -> task {
      let! preset = presetRepo.LoadPreset presetId |> Task.map Option.get

      let excludedPlaylists =
        preset.ExcludedPlaylists |> List.filter (fun p -> p.Id <> excludedPlaylistId)

      let updatedPreset =
        { preset with
            ExcludedPlaylists = excludedPlaylists }

      do! presetRepo.SavePreset updatedPreset

      return updatedPreset
    }

[<RequireQualifiedAccess>]
module ExcludedArtist =
  let remove (presetRepo: #ILoadPreset & #ISavePreset) =
    fun presetId excludedArtistId -> task {
      let! preset = presetRepo.LoadPreset presetId |> Task.map Option.get

      let excludedArtists =
        preset.ExcludedArtists |> List.filter (fun a -> a.Id <> excludedArtistId)

      let updatedPreset =
        { preset with
            ExcludedArtists = excludedArtists }

      do! presetRepo.SavePreset updatedPreset

      return updatedPreset
    }

[<RequireQualifiedAccess>]
module IncludedArtist =
  let remove (presetRepo: #ILoadPreset & #ISavePreset) =
    fun presetId includedArtistId -> task {
      let! preset = presetRepo.LoadPreset presetId |> Task.map Option.get

      let includedArtists =
        preset.IncludedArtists |> List.filter (fun a -> a.Id <> includedArtistId)

      let updatedPreset =
        { preset with
            IncludedArtists = includedArtists }

      do! presetRepo.SavePreset updatedPreset

      return updatedPreset
    }

[<RequireQualifiedAccess>]
module Preset =
  type UpdateSettings = PresetId -> PresetSettings.PresetSettings -> Task<unit>

  let validate: Preset.Validate =
    fun preset ->
      match preset.IncludedPlaylists, preset.Settings.LikedTracksHandling, preset.TargetedPlaylists with
      | [], LikedTracksHandling.Include, [] -> [ Preset.ValidationError.NoTargetedPlaylists ] |> Error
      | [], LikedTracksHandling.Exclude, [] ->
        [ Preset.ValidationError.NoIncludedPlaylists
          Preset.ValidationError.NoTargetedPlaylists ]
        |> Error
      | [], LikedTracksHandling.Ignore, [] ->
        [ Preset.ValidationError.NoIncludedPlaylists
          Preset.ValidationError.NoTargetedPlaylists ]
        |> Error
      | _, LikedTracksHandling.Include, [] -> [ Preset.ValidationError.NoTargetedPlaylists ] |> Error
      | _, LikedTracksHandling.Exclude, [] -> [ Preset.ValidationError.NoTargetedPlaylists ] |> Error
      | _, LikedTracksHandling.Ignore, [] -> [ Preset.ValidationError.NoTargetedPlaylists ] |> Error
      | [], LikedTracksHandling.Exclude, _ -> [ Preset.ValidationError.NoIncludedPlaylists ] |> Error
      | [], LikedTracksHandling.Ignore, _ -> [ Preset.ValidationError.NoIncludedPlaylists ] |> Error
      | _ -> Ok preset

  let create (presetRepo: #ISavePreset & #IIdGenerator) =
    fun userId name -> task {
      let newPreset =
        { Id = PresetId(presetRepo.GenerateId())
          Name = name
          OwnerId = userId
          IncludedPlaylists = []
          ExcludedPlaylists = []
          IncludedArtists = []
          ExcludedArtists = []
          TargetedPlaylists = []
          Settings =
            { Size = Size.Size 20
              RecommendationsEngine = None
              LikedTracksHandling = LikedTracksHandling.Include
              UniqueArtists = false } }

      do! presetRepo.SavePreset newPreset

      return newPreset
    }

  let queueRun (presetRepo: #ILoadPreset & #Repos.IQueueRun) =
    fun userId ->
      presetRepo.LoadPreset
      >> Task.map Option.get
      >> Task.map validate
      >> TaskResult.taskTap (fun p -> presetRepo.QueueRun(userId, p.Id))

  let includePlaylist (parseId: Playlist.ParseId) (presetRepo: #ILoadPreset & #ISavePreset) (musicPlatformFactory: IMusicPlatformFactory) =
    fun (cmd: IncludePlaylist.Cmd) -> taskResult {
      let! playlistId = parseId cmd.PlaylistId |> Result.mapError IncludePlaylist.Error.IdParsing

      let! preset = presetRepo.LoadPreset cmd.PresetId |> Task.map Option.get

      do!
        preset.IncludedPlaylists
        |> List.tryFind (fun p -> p.Id = ReadablePlaylistId playlistId)
        |> Result.requireNone (IncludePlaylist.Error.Duplicate playlistId)

      let! musicPlatform =
        musicPlatformFactory.GetMusicPlatform(cmd.UserId.ToMusicPlatformId())
        |> TaskResult.requireSome IncludePlaylist.Error.Unauthorized

      let! playlist =
        musicPlatform.LoadPlaylist playlistId
        |> TaskResult.mapError IncludePlaylist.Error.Load

      let playlistToInclude = IncludedPlaylist.fromSpotifyPlaylist playlist

      let updatedPreset =
        { preset with
            IncludedPlaylists = playlistToInclude :: preset.IncludedPlaylists }

      do! presetRepo.SavePreset updatedPreset

      return playlistToInclude
    }

  let excludePlaylist (parseId: Playlist.ParseId) (presetRepo: #ILoadPreset & #ISavePreset) (musicPlatformFactory: IMusicPlatformFactory) =
    fun (cmd: ExcludePlaylist.Cmd) -> taskResult {
      let! playlistId = parseId cmd.PlaylistId |> Result.mapError ExcludePlaylist.Error.IdParsing

      let! preset = presetRepo.LoadPreset cmd.PresetId |> Task.map Option.get

      do!
        preset.ExcludedPlaylists
        |> List.tryFind (fun p -> p.Id = ReadablePlaylistId playlistId)
        |> Result.requireNone (ExcludePlaylist.Error.Duplicate playlistId)

      let! musicPlatform =
        musicPlatformFactory.GetMusicPlatform(cmd.UserId.ToMusicPlatformId())
        |> TaskResult.requireSome ExcludePlaylist.Error.Unauthorized

      let! playlist =
        musicPlatform.LoadPlaylist playlistId
        |> TaskResult.mapError ExcludePlaylist.Error.Load

      let playlistToExclude = ExcludedPlaylist.fromSpotifyPlaylist playlist

      let updatedPreset =
        { preset with
            ExcludedPlaylists = playlistToExclude :: preset.ExcludedPlaylists }

      do! presetRepo.SavePreset updatedPreset

      return playlistToExclude
    }

  let excludeArtist (parseId: Artist.ParseId) (presetRepo: #ILoadPreset & #ISavePreset) (musicPlatformFactory: IMusicPlatformFactory) =
    fun (cmd: ExcludeArtist.Cmd) -> taskResult {
      let! artistId = parseId cmd.ArtistId |> Result.mapError ExcludeArtist.Error.IdParsing

      let! preset = presetRepo.LoadPreset cmd.PresetId |> Task.map Option.get

      do!
        preset.ExcludedArtists
        |> List.tryFind (fun p -> p.Id = artistId)
        |> Result.requireNone (ExcludeArtist.Error.Duplicate artistId)

      let! musicPlatform =
        musicPlatformFactory.GetMusicPlatform(cmd.UserId.ToMusicPlatformId())
        |> TaskResult.requireSome ExcludeArtist.Error.Unauthorized

      let! artist =
        musicPlatform.LoadArtist artistId
        |> TaskResult.mapError ExcludeArtist.Error.Load

      let updatedPreset =
        { preset with
            ExcludedArtists = artist :: preset.ExcludedArtists }

      do! presetRepo.SavePreset updatedPreset

      return artist
    }

  let includeArtist (parseId: Artist.ParseId) (presetRepo: #ILoadPreset & #ISavePreset) (musicPlatformFactory: IMusicPlatformFactory) =
    fun (cmd: IncludeArtist.Cmd) -> taskResult {
      let! artistId = parseId cmd.ArtistId |> Result.mapError IncludeArtist.Error.IdParsing

      let! preset = presetRepo.LoadPreset cmd.PresetId |> Task.map Option.get

      do!
        preset.IncludedArtists
        |> List.tryFind (fun p -> p.Id = artistId)
        |> Result.requireNone (IncludeArtist.Error.Duplicate artistId)

      let! musicPlatform =
        musicPlatformFactory.GetMusicPlatform(cmd.UserId.ToMusicPlatformId())
        |> TaskResult.requireSome IncludeArtist.Error.Unauthorized

      let! artist =
        musicPlatform.LoadArtist artistId
        |> TaskResult.mapError IncludeArtist.Error.Load

      let updatedPreset =
        { preset with
            IncludedArtists = artist :: preset.IncludedArtists }

      do! presetRepo.SavePreset updatedPreset

      return artist
    }

  let targetPlaylist (parseId: Playlist.ParseId) (presetRepo: #ILoadPreset & #ISavePreset) (musicPlatformFactory: IMusicPlatformFactory) =
    fun (cmd: TargetPlaylist.Cmd) -> taskResult {
      let! playlistId = parseId cmd.PlaylistId |> Result.mapError TargetPlaylist.Error.IdParsing

      let! preset = presetRepo.LoadPreset cmd.PresetId |> Task.map Option.get

      do!
        preset.TargetedPlaylists
        |> List.tryFind (fun p -> p.Id = WritablePlaylistId playlistId)
        |> Result.requireNone (TargetPlaylist.Error.Duplicate playlistId)

      let! musicPlatform =
        musicPlatformFactory.GetMusicPlatform(cmd.UserId.ToMusicPlatformId())
        |> TaskResult.requireSome TargetPlaylist.Error.Unauthorized

      let! playlist =
        musicPlatform.LoadPlaylist playlistId
        |> TaskResult.mapError TargetPlaylist.Error.Load

      let! playlistToTarget =
        playlist
        |> TargetedPlaylist.fromSpotifyPlaylist
        |> Result.requireSome TargetPlaylist.Error.AccessError

      let updatedPreset =
        { preset with
            TargetedPlaylists = playlistToTarget :: preset.TargetedPlaylists }

      do! presetRepo.SavePreset updatedPreset

      return playlistToTarget
    }

  let setSize (presetRepo: #ILoadPreset & #ISavePreset) =
    fun presetId size ->
      size
      |> Size.TryParse
      |> Result.taskMap (fun s ->
        presetId
        |> presetRepo.LoadPreset
        |> Task.map Option.get
        |> Task.map (fun p -> { p with Settings.Size = s })
        |> Task.bind presetRepo.SavePreset)

  let get (presetRepo: #ILoadPreset) =
    fun userId presetId -> taskResult {
      let! preset =
        presetRepo.LoadPreset presetId
        |> TaskResult.requireSome Preset.GetPresetError.NotFound

      do! Result.requireEqual preset.OwnerId userId Preset.GetPresetError.Forbidden

      return preset
    }

[<RequireQualifiedAccess>]
module User =
  let setCurrentPreset (userRepo: #ILoadUser & #ISaveUser) =
    fun userId presetId ->
      userId
      |> userRepo.LoadUser
      |> Task.map (fun u ->
        { u with
            CurrentPresetId = Some presetId })
      |> Task.bind userRepo.SaveUser

  let setCurrentPresetSize (userRepo: #ILoadUser) (presetService: #ISetPresetSize) =
    fun userId size ->
      userId
      |> userRepo.LoadUser
      |> Task.map (fun u -> u.CurrentPresetId |> Option.get)
      |> Task.bind (fun presetId -> presetService.SetPresetSize(presetId, size))

  let create (userRepo: #ISaveUser) =
    fun () -> task {
      let newUserId = Guid.CreateVersion7() |> UserId

      let newUser: User =
        { Id = newUserId
          CurrentPresetId = None
          MusicPlatforms = [] }

      do! userRepo.SaveUser newUser

      return newUser
    }

[<RequireQualifiedAccess>]
module TargetedPlaylist =
  let private setPlaylistOverwriting (presetRepo: #ILoadPreset & #ISavePreset) overwriting =
    fun presetId targetedPlaylistId -> task {
      let! preset = presetRepo.LoadPreset presetId |> Task.map Option.get

      let targetPlaylist =
        preset.TargetedPlaylists |> List.find (fun p -> p.Id = targetedPlaylistId)

      let updatedPlaylist =
        { targetPlaylist with
            Overwrite = overwriting }

      let updatedPreset =
        { preset with
            TargetedPlaylists =
              preset.TargetedPlaylists
              |> List.except [ targetPlaylist ]
              |> List.append [ updatedPlaylist ] }

      return! presetRepo.SavePreset updatedPreset
    }

  let overwriteTracks presetRepo = setPlaylistOverwriting presetRepo true

  let appendTracks presetRepo = setPlaylistOverwriting presetRepo false

  let remove (presetRepo: #ILoadPreset & #ISavePreset) =
    fun presetId targetPlaylistId -> task {
      let! preset = presetRepo.LoadPreset presetId |> Task.map Option.get

      let targetPlaylists =
        preset.TargetedPlaylists |> List.filter (fun p -> p.Id <> targetPlaylistId)

      let updatedPreset =
        { preset with
            TargetedPlaylists = targetPlaylists }

      do! presetRepo.SavePreset updatedPreset

      return updatedPreset
    }

type ArtistAlbumsRecommender(musicPlatform: IMusicPlatform) =
  [<Literal>]
  let seedTracksCount = 20

  interface IRecommender with
    member this.Recommend(tracks: Track list) =
      tracks
      |> List.takeSafe seedTracksCount
      |> Seq.collect _.Artists
      |> Seq.distinct
      |> TaskSeq.ofSeq
      |> TaskSeq.collect (fun a -> musicPlatform.ListArtistTracks a.Id)
      |> TaskSeq.distinct
      |> TaskSeq.toListAsync

type PresetService
  (parsePlaylistId: Playlist.ParseId, parseArtistId: Artist.ParseId, presetRepo: IPresetRepo, musicPlatformFactory: IMusicPlatformFactory) =
  interface IPresetService with
    member this.QueueRun(userId, presetId) =
      Preset.queueRun presetRepo userId presetId

    member this.SetPresetSize(presetId, size) = Preset.setSize presetRepo presetId size
    member this.CreatePreset(userId, name) = Preset.create presetRepo userId name

    member this.SetRecommendationsEngine(presetId, engine) =
      PresetSettings.setRecommendationsEngine presetRepo engine presetId

    member this.IncludePlaylist(cmd) =
      Preset.includePlaylist parsePlaylistId presetRepo musicPlatformFactory cmd

    member this.ExcludePlaylist(cmd) =
      Preset.excludePlaylist parsePlaylistId presetRepo musicPlatformFactory cmd

    member this.ExcludeArtist(cmd) =
      Preset.excludeArtist parseArtistId presetRepo musicPlatformFactory cmd

    member this.IncludeArtist(cmd) =
      Preset.includeArtist parseArtistId presetRepo musicPlatformFactory cmd

    member this.TargetPlaylist(cmd) =
      Preset.targetPlaylist parsePlaylistId presetRepo musicPlatformFactory cmd

    member this.EnableUniqueArtists(presetId) =
      PresetSettings.enableUniqueArtists presetRepo presetId

    member this.DisableUniqueArtists(presetId) =
      PresetSettings.disableUniqueArtists presetRepo presetId

    member this.IncludeLikedTracks(presetId) =
      PresetSettings.includeLikedTracks presetRepo presetId

    member this.ExcludeLikedTracks(presetId) =
      PresetSettings.excludeLikedTracks presetRepo presetId

    member this.IgnoreLikedTracks(presetId) =
      PresetSettings.ignoreLikedTracks presetRepo presetId

    member this.AppendToTargetedPlaylist(presetId, playlistId) =
      TargetedPlaylist.appendTracks presetRepo presetId playlistId

    member this.OverwriteTargetedPlaylist(presetId, playlistId) =
      TargetedPlaylist.overwriteTracks presetRepo presetId playlistId

    member this.RemoveExcludedPlaylist(presetId, playlistId) =
      ExcludedPlaylist.remove presetRepo presetId playlistId

    member this.RemoveExcludedArtist(presetId, artistId) =
      ExcludedArtist.remove presetRepo presetId artistId

    member this.RemoveIncludedArtist(presetId, artistId) =
      IncludedArtist.remove presetRepo presetId artistId

    member this.RemoveIncludedPlaylist(presetId, playlistId) =
      IncludedPlaylist.remove presetRepo presetId playlistId

    member this.RemoveTargetedPlaylist(presetId, playlistId) =
      TargetedPlaylist.remove presetRepo presetId playlistId

    member this.SetAll(presetId, playlistId) =
      IncludedPlaylist.setAll presetRepo presetId playlistId

    member this.SetOnlyLiked(presetId, playlistId) =
      IncludedPlaylist.setLikedOnly presetRepo presetId playlistId

    member this.GetPreset(userId, presetId) = Preset.get presetRepo userId presetId

type UserService(userRepo: IUserRepo, presetService: IPresetService) =
  interface IUserService with
    member this.SetCurrentPresetSize(userId, size) =
      User.setCurrentPresetSize userRepo presetService userId size

    member this.SetCurrentPreset(userId, presetId) =
      User.setCurrentPreset userRepo userId presetId

    member this.CreateUser() = User.create userRepo ()