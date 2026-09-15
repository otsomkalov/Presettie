namespace App

open System
open System.Threading.Tasks
open Domain.Core.PresetSettings
open Domain.Workflows
open FSharp.Control
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open MusicPlatform
open Domain.Core
open Domain.Repos
open FsToolkit.ErrorHandling
open otsom.fs.Extensions

[<RequireQualifiedAccess>]
module rec RunPreset =
  type Cmd = { UserId: UserId; PresetId: PresetId }

  [<RequireQualifiedAccess>]
  type Error =
    | Preset of Preset.GetPresetError
    | NoMusicPlatform
    | NoIncludedTracks
    | NoPotentialTracks

  type Handler = Cmd -> TaskResult<Preset, Error>

  module private IncludedPlaylist =
    let private listPlaylistTracks (musicPlatform: #IListPlaylistTracks & #IListLikedTracks) =
      fun (playlist: IncludedPlaylist) -> task {
        let! tracks = playlist.Id.Value |> musicPlatform.ListPlaylistTracks |> Task.map Set.ofSeq

        if playlist.LikedOnly then
          return! musicPlatform.ListLikedTracks() |> Task.map (Set.ofList >> Set.intersect tracks)
        else
          return tracks
      }

    let listTracks (env: #IListPlaylistTracks) =
      fun (playlists: IncludedPlaylist list) ->
        playlists
        |> List.map (listPlaylistTracks env)
        |> Task.WhenAll
        |> Task.map Seq.concat
        |> Task.map List.ofSeq

  module private IncludedArtist =
    let internal listTracks (platform: #IListArtistTracks) =
      fun (artists: IncludedArtist list) ->
        artists
        |> TaskSeq.ofList
        |> TaskSeq.collect (_.Id >> platform.ListArtistTracks)
        |> TaskSeq.toListAsync

  module private ExcludedPlaylist =
    let listTracks (platform: #IListPlaylistTracks) =
      fun (playlists: ExcludedPlaylist list) ->
        playlists
        |> List.map (_.Id.Value >> platform.ListPlaylistTracks)
        |> Task.WhenAll
        |> Task.map List.concat

  module private ExcludedArtist =
    let internal listTracks (platform: #IListArtistTracks) =
      fun (artists: ExcludedArtist list) ->
        artists
        |> TaskSeq.ofList
        |> TaskSeq.collect (_.Id >> platform.ListArtistTracks)
        |> TaskSeq.toListAsync

  let private listIncludedTracks (musicPlatform: #IListPlaylistTracks & #IListLikedTracks) =
    fun preset -> task {
      let! includedByPlaylists = preset.IncludedPlaylists |> IncludedPlaylist.listTracks musicPlatform
      let! includedByArtists = preset.IncludedArtists |> IncludedArtist.listTracks musicPlatform

      let! includedLiked =
        match preset.Settings.LikedTracksHandling with
        | LikedTracksHandling.Include -> musicPlatform.ListLikedTracks()
        | _ -> Task.FromResult []

      return List.concat [ includedByPlaylists; includedByArtists; includedLiked ]
    }

  let private listExcludedTracks (platform: #IListLikedTracks) =
    fun preset -> task {
      let! excludedByPlaylists = preset.ExcludedPlaylists |> ExcludedPlaylist.listTracks platform
      let! excludedByArtists = preset.ExcludedArtists |> ExcludedArtist.listTracks platform

      let! excludedLiked =
        match preset.Settings.LikedTracksHandling with
        | LikedTracksHandling.Exclude -> platform.ListLikedTracks()
        | _ -> Task.FromResult []

      return List.concat [ excludedByPlaylists; excludedByArtists; excludedLiked ]
    }

  let private saveTracks (platform: #IAddTracks & #IReplaceTracks) =
    fun preset (tracks: Track list) ->
      preset.TargetedPlaylists
      |> Seq.map (fun p ->
        match p.Overwrite with
        | true -> platform.ReplaceTracks(p.Id.Value, tracks)
        | false -> platform.AddTracks(p.Id.Value, tracks))
      |> Task.WhenAll
      |> Task.ignore

  let private getRecommendations (recommenderFactory: IRecommenderFactory) musicPlatform =
    fun (preset: Preset) (tracks: Track list) ->
      match preset.Settings.RecommendationsEngine with
      | Some engine ->
        let recommender = recommenderFactory.Create(musicPlatform, engine)

        recommender.Recommend tracks
      | None -> Task.FromResult []

  let handler
    (presetRepo: IPresetRepo)
    (musicPlatformFactory: IMusicPlatformFactory)
    (shuffler: Shuffler<Track>)
    (recommenderFactory: IRecommenderFactory)
    (loggerFactory: ILoggerFactory)
    : Handler =
    let getRecommendations = getRecommendations recommenderFactory
    let logger = loggerFactory.CreateLogger(nameof RunPreset)

    fun (cmd: Cmd) -> taskResult {
      let! preset =
        Preset.get presetRepo cmd.UserId cmd.PresetId
        |> TaskResult.mapError Error.Preset

      let! musicPlatform =
        musicPlatformFactory.GetMusicPlatform(cmd.UserId.ToMusicPlatformId())
        |> TaskResult.requireSome Error.NoMusicPlatform

      // Shuffle first to get recommendations based on different tracks each time
      let! includedTracks = listIncludedTracks musicPlatform preset |> Task.map shuffler

      logger.LogInformation("Loaded {IncludedTracksCount} included tracks", includedTracks.Length)

      do! includedTracks |> Result.requireNotEmpty Error.NoIncludedTracks

      let! recommendedTracks = getRecommendations musicPlatform preset includedTracks |> Task.map shuffler

      logger.LogInformation("Loaded {RecommendedTracksCount} recommended tracks", recommendedTracks.Length)

      let! excludedTracks = listExcludedTracks musicPlatform preset

      let potentialTracks =
        (recommendedTracks @ includedTracks) |> List.except excludedTracks

      let filteredPotentialTracks =
        match preset.Settings.UniqueArtists with
        | true -> potentialTracks |> Tracks.uniqueByArtists
        | false -> potentialTracks

      logger.LogInformation("{PotentialTracksCount} potential tracks", potentialTracks.Length)

      do! filteredPotentialTracks |> Result.requireNotEmpty Error.NoPotentialTracks

      let tracksToSave =
        filteredPotentialTracks |> List.takeSafe preset.Settings.Size.Value

      do! saveTracks musicPlatform preset tracksToSave

      return preset
    }

type IMediator =
  abstract Send: 'cmd -> Task<'result>

type Mediator(sp: IServiceProvider) =
  interface IMediator with
    member this.Send(cmd) =
      let handler = sp.GetRequiredService<'cmd -> Task<'result>>()

      handler cmd