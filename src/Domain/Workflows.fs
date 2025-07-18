module Domain.Workflows

open System.Threading.Tasks
open Domain.Core
open Domain.Repos
open Microsoft.FSharp.Control
open Microsoft.FSharp.Core
open MusicPlatform
open otsom.fs.Core
open otsom.fs.Extensions
open Domain.Extensions

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

  let private setRecommendations (presetRepo: #ILoadPreset & #ISavePreset) =
    fun enabled ->
      presetRepo.LoadPreset
      >> Task.map Option.get
      >> Task.map (fun preset ->
        { preset with
            Settings.RecommendationsEnabled = enabled })
      >> Task.bind presetRepo.SavePreset

  let enableRecommendations presetRepo = setRecommendations presetRepo true

  let disableRecommendations presetRepo = setRecommendations presetRepo false

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
    setLikedTracksHandling presetRepo PresetSettings.LikedTracksHandling.Include

  let excludeLikedTracks presetRepo =
    setLikedTracksHandling presetRepo PresetSettings.LikedTracksHandling.Exclude

  let ignoreLikedTracks presetRepo =
    setLikedTracksHandling presetRepo PresetSettings.LikedTracksHandling.Ignore

[<RequireQualifiedAccess>]
module IncludedPlaylist =
  let private listPlaylistTracks (env: #IListPlaylistTracks & #IListLikedTracks) =
    fun (playlist: IncludedPlaylist) -> task {
      let! tracks = playlist.Id.Value |> env.ListPlaylistTracks |> Task.map Set.ofSeq

      if playlist.LikedOnly then
        return! env.ListLikedTracks() |> Task.map (Set.ofList >> Set.intersect tracks)
      else
        return tracks
    }

  let internal listTracks env =
    fun (playlists: IncludedPlaylist list) ->
      playlists
      |> List.map (listPlaylistTracks env)
      |> Task.WhenAll
      |> Task.map Seq.concat
      |> Task.map List.ofSeq

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
  let private listPlaylistTracks (env: #IListPlaylistTracks & #IListLikedTracks) =
    fun (playlist: ExcludedPlaylist) -> playlist.Id.Value |> env.ListPlaylistTracks |> Task.map Set.ofSeq

  let internal listTracks env =
    fun (playlists: ExcludedPlaylist list) ->
      playlists
      |> List.map (listPlaylistTracks env)
      |> Task.WhenAll
      |> Task.map Seq.concat
      |> Task.map List.ofSeq

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
module Preset =
  type UpdateSettings = PresetId -> PresetSettings.PresetSettings -> Task<unit>

  let validate: Preset.Validate =
    fun preset ->
      match preset.IncludedPlaylists, preset.Settings.LikedTracksHandling, preset.TargetedPlaylists with
      | [], PresetSettings.LikedTracksHandling.Include, [] -> [ Preset.ValidationError.NoTargetedPlaylists ] |> Error
      | [], PresetSettings.LikedTracksHandling.Exclude, [] ->
        [ Preset.ValidationError.NoIncludedPlaylists
          Preset.ValidationError.NoTargetedPlaylists ]
        |> Error
      | [], PresetSettings.LikedTracksHandling.Ignore, [] ->
        [ Preset.ValidationError.NoIncludedPlaylists
          Preset.ValidationError.NoTargetedPlaylists ]
        |> Error
      | _, PresetSettings.LikedTracksHandling.Include, [] -> [ Preset.ValidationError.NoTargetedPlaylists ] |> Error
      | _, PresetSettings.LikedTracksHandling.Exclude, [] -> [ Preset.ValidationError.NoTargetedPlaylists ] |> Error
      | _, PresetSettings.LikedTracksHandling.Ignore, [] -> [ Preset.ValidationError.NoTargetedPlaylists ] |> Error
      | [], PresetSettings.LikedTracksHandling.Exclude, _ -> [ Preset.ValidationError.NoIncludedPlaylists ] |> Error
      | [], PresetSettings.LikedTracksHandling.Ignore, _ -> [ Preset.ValidationError.NoIncludedPlaylists ] |> Error
      | _ -> Ok preset

  let create (presetRepo: #ISavePreset & #IIdGenerator) =
    fun userId name -> task {
      let newPreset =
        { Id = PresetId(presetRepo.GenerateId())
          Name = name
          OwnerId = userId
          IncludedPlaylists = []
          ExcludedPlaylists = []
          TargetedPlaylists = []
          Settings =
            { Size = PresetSettings.Size.Size 20
              RecommendationsEnabled = false
              LikedTracksHandling = PresetSettings.LikedTracksHandling.Include
              UniqueArtists = false } }

      do! presetRepo.SavePreset newPreset

      return newPreset
    }

  let run (presetRepo: #ILoadPreset) shuffler platform =

    let saveTracks (platform: #IAddTracks & #IReplaceTracks) =
      fun preset (tracks: Track list) ->
        preset.TargetedPlaylists
        |> Seq.map (fun p ->
          match p.Overwrite with
          | true -> platform.ReplaceTracks(p.Id.Value, tracks)
          | false -> platform.AddTracks(p.Id.Value, tracks))
        |> Task.WhenAll
        |> Task.ignore

    let includeLiked (platform: #IListLikedTracks) =
      fun (preset: Preset) tracks ->
        match preset.Settings.LikedTracksHandling with
        | PresetSettings.LikedTracksHandling.Include -> platform.ListLikedTracks() |> Task.map (List.append tracks)
        | _ -> Task.FromResult tracks

    let excludeLiked (platform: #IListLikedTracks) =
      fun (preset: Preset) tracks ->
        match preset.Settings.LikedTracksHandling with
        | PresetSettings.LikedTracksHandling.Exclude -> platform.ListLikedTracks() |> Task.map (List.append tracks)
        | _ -> Task.FromResult tracks

    let getRecommendations (platform: #IListArtistTracks) =
      fun (preset: Preset) (tracks: Track list) ->
        match preset.Settings.RecommendationsEnabled with
        | true ->
          tracks
          |> List.takeSafe preset.Settings.Size.Value
          |> List.collect (fun t -> t.Artists |> List.ofSeq)
          |> List.map (fun a -> platform.ListArtistTracks a.Id)
          |> Task.WhenAll
          |> Task.map (List.concat >> List.prepend tracks)
        | false -> tracks |> Task.FromResult

    presetRepo.LoadPreset
    >> Task.map Option.get
    >> Task.bind (fun preset ->
      IncludedPlaylist.listTracks platform preset.IncludedPlaylists
      &|&> includeLiked platform preset
      &|> Result.errorIf List.isEmpty Preset.RunError.NoIncludedTracks
      &=|> shuffler
      &=|&> getRecommendations platform preset
      &=|> shuffler
      &=|&> (fun includedTracks ->
        preset.ExcludedPlaylists |> ExcludedPlaylist.listTracks platform
        &|&> (excludeLiked platform preset)
        &|> (fun excludedTracks -> List.except excludedTracks includedTracks))
      &|> (Result.bind (Result.errorIf List.isEmpty Preset.RunError.NoPotentialTracks))
      &=|> (fun (tracks: Track list) ->
        match preset.Settings.UniqueArtists with
        | true -> tracks |> Tracks.uniqueByArtists
        | false -> tracks)
      &=|> (List.takeSafe preset.Settings.Size.Value)
      &=|&> (saveTracks platform preset)
      &=|> (fun _ -> preset))

  let queueRun (presetRepo: #ILoadPreset & #IQueueRun) =
    fun userId ->
      presetRepo.LoadPreset
      >> Task.map Option.get
      >> Task.map validate
      >> TaskResult.taskTap (fun p -> presetRepo.QueueRun(userId, p.Id))

  let includePlaylist (parseId: Playlist.ParseId) (presetRepo: #ILoadPreset & #ISavePreset) (musicPlatformFactory: IMusicPlatformFactory) =
    let parseId = parseId >> Result.mapError Preset.IncludePlaylistError.IdParsing

    let loadPlaylist (mp: #ILoadPlaylist) =
      mp.LoadPlaylist >> TaskResult.mapError Preset.IncludePlaylistError.Load

    let includePlaylist' mp =
      fun presetId rawPlaylistId ->
        let updatePreset playlist = task {
          let! preset = presetRepo.LoadPreset presetId |> Task.map Option.get

          let updatedIncludedPlaylists = preset.IncludedPlaylists |> List.append [ playlist ]

          let updatedPreset =
            { preset with
                IncludedPlaylists = updatedIncludedPlaylists }

          do! presetRepo.SavePreset updatedPreset

          return playlist
        }

        rawPlaylistId
        |> parseId
        |> Result.taskBind (loadPlaylist mp)
        |> TaskResult.map IncludedPlaylist.fromSpotifyPlaylist
        |> TaskResult.taskMap updatePreset

    fun (userId: UserId) presetId rawPlaylistId ->
      musicPlatformFactory.GetMusicPlatform(userId.ToMusicPlatformId())
      |> Task.bind (function
        | Some mp -> includePlaylist' mp presetId rawPlaylistId
        | None -> Preset.IncludePlaylistError.Unauthorized |> Error |> Task.FromResult)

  let excludePlaylist (parseId: Playlist.ParseId) (presetRepo: #ILoadPreset & #ISavePreset) (musicPlatformFactory: IMusicPlatformFactory) =
    let parseId = parseId >> Result.mapError Preset.ExcludePlaylistError.IdParsing

    let loadPlaylist (mp: #ILoadPlaylist) =
      mp.LoadPlaylist >> TaskResult.mapError Preset.ExcludePlaylistError.Load

    let excludePlaylist' mp =
      fun presetId rawPlaylistId ->
        let updatePreset playlist = task {
          let! preset = presetRepo.LoadPreset presetId |> Task.map Option.get

          let updatedExcludedPlaylists = preset.ExcludedPlaylists |> List.append [ playlist ]

          let updatedPreset =
            { preset with
                ExcludedPlaylists = updatedExcludedPlaylists }

          do! presetRepo.SavePreset updatedPreset

          return playlist
        }

        rawPlaylistId
        |> parseId
        |> Result.taskBind (loadPlaylist mp)
        |> TaskResult.map ExcludedPlaylist.fromSpotifyPlaylist
        |> TaskResult.taskMap updatePreset

    fun (UserId userId) presetId rawPlaylistId ->
      musicPlatformFactory.GetMusicPlatform(userId |> MusicPlatform.UserId)
      |> Task.bind (function
        | Some mp -> excludePlaylist' mp presetId rawPlaylistId
        | None -> Preset.ExcludePlaylistError.Unauthorized |> Error |> Task.FromResult)

  let targetPlaylist (parseId: Playlist.ParseId) (presetRepo: #ILoadPreset & #ISavePreset) (musicPlatformFactory: IMusicPlatformFactory) =
    let parseId = parseId >> Result.mapError Preset.TargetPlaylistError.IdParsing

    let loadPlaylist (mp: #ILoadPlaylist) =
      mp.LoadPlaylist >> TaskResult.mapError Preset.TargetPlaylistError.Load

    let checkAccess playlist =
      playlist
      |> TargetedPlaylist.fromSpotifyPlaylist
      |> Result.ofOption (Preset.AccessError() |> Preset.TargetPlaylistError.AccessError)

    let targetPlaylist' mp =
      fun presetId rawPlaylistId ->
        let updatePreset playlist = task {
          let! preset = presetRepo.LoadPreset presetId |> Task.map Option.get

          let updatedTargetedPlaylists = preset.TargetedPlaylists |> List.append [ playlist ]

          let updatedPreset =
            { preset with
                TargetedPlaylists = updatedTargetedPlaylists }

          do! presetRepo.SavePreset updatedPreset

          return playlist
        }

        rawPlaylistId
        |> parseId
        |> Result.taskBind (loadPlaylist mp)
        |> Task.map (Result.bind checkAccess)
        |> TaskResult.taskMap updatePreset

    fun (UserId userId) presetId rawPlaylistId ->
      musicPlatformFactory.GetMusicPlatform(userId |> MusicPlatform.UserId)
      |> Task.bind (function
        | Some mp -> targetPlaylist' mp presetId rawPlaylistId
        | None -> Preset.TargetPlaylistError.Unauthorized |> Error |> Task.FromResult)

  let setSize (presetRepo: #ILoadPreset & #ISavePreset) =
    fun presetId size ->
      size
      |> PresetSettings.Size.TryParse
      |> Result.taskMap (fun s ->
        presetId
        |> presetRepo.LoadPreset
        |> Task.map Option.get
        |> Task.map (fun p -> { p with Settings.Size = s })
        |> Task.bind presetRepo.SavePreset)

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

  let removePreset (userRepo: #ILoadUser & #ISaveUser) (presetService: #Core.IRemovePreset) =
    fun userId presetId ->
      userId
      |> userRepo.LoadUser
      |> Task.map (fun u ->
        { u with
            CurrentPresetId =
              if u.CurrentPresetId = Some presetId then
                None
              else
                u.CurrentPresetId })
      |> Task.bind userRepo.SaveUser
      |> Task.bind (fun _ -> presetService.RemovePreset presetId)

  let setCurrentPresetSize (userRepo: #ILoadUser) (presetService: #ISetPresetSize) =
    fun userId size ->
      userId
      |> userRepo.LoadUser
      |> Task.map (fun u -> u.CurrentPresetId |> Option.get)
      |> Task.bind (fun presetId -> presetService.SetPresetSize(presetId, size))

  let create (userRepo: #ISaveUser & #IIdGenerator) =
    fun () -> task {
      let newUserId = userRepo.GenerateId() |> UserId

      let newUser: User =
        { Id = newUserId
          CurrentPresetId = None }

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

type Shuffler<'a> = 'a list -> 'a list

type PresetService
  (parseId: Playlist.ParseId, presetRepo: IPresetRepo, musicPlatformFactory: IMusicPlatformFactory, shuffler: Shuffler<Track>) =
  interface IPresetService with
    member this.QueueRun(userId, presetId) =
      Preset.queueRun presetRepo userId presetId

    member this.SetPresetSize(presetId, size) = Preset.setSize presetRepo presetId size
    member this.CreatePreset(userId, name) = Preset.create presetRepo userId name

    member this.EnableRecommendations(presetId) =
      PresetSettings.enableRecommendations presetRepo presetId

    member this.DisableRecommendations(presetId) =
      PresetSettings.disableRecommendations presetRepo presetId

    member this.IncludePlaylist(userId, presetId, rawPlaylistId) =
      Preset.includePlaylist parseId presetRepo musicPlatformFactory userId presetId rawPlaylistId

    member this.ExcludePlaylist(userId, presetId, rawPlaylistId) =
      Preset.excludePlaylist parseId presetRepo musicPlatformFactory userId presetId rawPlaylistId

    member this.TargetPlaylist(userId, presetId, rawPlaylistId) =
      Preset.targetPlaylist parseId presetRepo musicPlatformFactory userId presetId rawPlaylistId

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

    member this.RemoveIncludedPlaylist(presetId, playlistId) =
      IncludedPlaylist.remove presetRepo presetId playlistId

    member this.RemoveTargetedPlaylist(presetId, playlistId) =
      TargetedPlaylist.remove presetRepo presetId playlistId

    member this.SetAll(presetId, playlistId) =
      IncludedPlaylist.setAll presetRepo presetId playlistId

    member this.SetOnlyLiked(presetId, playlistId) =
      IncludedPlaylist.setLikedOnly presetRepo presetId playlistId

    member this.RunPreset(userId, presetId) = task {
      let! musicPlatform = musicPlatformFactory.GetMusicPlatform(userId.ToMusicPlatformId())

      match musicPlatform with
      | Some platform -> return! Preset.run presetRepo shuffler platform presetId
      | None -> return Preset.RunError.Unauthorized |> Error
    }

    member this.RemovePreset(presetId) = presetRepo.RemovePreset presetId

type UserService(userRepo: IUserRepo, presetService: IPresetService) =
  interface IUserService with
    member this.SetCurrentPresetSize(userId, size) =
      User.setCurrentPresetSize userRepo presetService userId size

    member this.SetCurrentPreset(userId, presetId) =
      User.setCurrentPreset userRepo userId presetId

    member this.RemoveUserPreset(userId, presetId) =
      User.removePreset userRepo presetService userId presetId

    member this.CreateUser() = User.create userRepo ()