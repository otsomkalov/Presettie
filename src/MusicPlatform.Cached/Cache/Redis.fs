module internal MusicPlatform.Spotify.Cache.Redis

open System
open Microsoft.ApplicationInsights
open Microsoft.ApplicationInsights.DataContracts
open MusicPlatform
open MusicPlatform.Cached.Helpers
open StackExchange.Redis
open System.Threading.Tasks
open otsom.fs.Extensions

let private prependList (telemetryClient: TelemetryClient) (cache: IDatabase) =
  fun (key: string) values ->
    task {
      let dependency = DependencyTelemetry("Redis", key, "prependList", key)

      use operation = telemetryClient.StartOperation dependency

      let! _ = cache.ListLeftPushAsync(key, values |> List.toArray)

      operation.Telemetry.Success <- true

      return ()
    }

let private replaceList (telemetryClient: TelemetryClient) (cache: IDatabase) =
  fun (key: string) values ->
    task {
      let dependency = DependencyTelemetry("Redis", key, "replaceList", key)

      use operation = telemetryClient.StartOperation dependency

      let transaction = cache.CreateTransaction()

      let _ = transaction.KeyDeleteAsync(key)
      let _ = transaction.ListLeftPushAsync(key, values |> List.toArray)
      let _ = transaction.KeyExpireAsync(key, TimeSpan.FromDays(1))

      let! _ = transaction.ExecuteAsync() |> Task.map ignore

      operation.Telemetry.Success <- true

      return ()
    }

let private loadList (telemetryClient: TelemetryClient) (cache: IDatabase) =
  fun key ->
    task {
      let dependency = DependencyTelemetry("Redis", key, "loadList", key)

      use operation = telemetryClient.StartOperation dependency

      let! values = key |> cache.ListRangeAsync

      operation.Telemetry.Success <- true

      return values
    }

let private listLength (telemetryClient: TelemetryClient) (cache: IDatabase) =
  fun key ->
    task {
      let dependency = DependencyTelemetry("Redis", key, "listLength", key)

      use operation = telemetryClient.StartOperation dependency

      let! value = key |> cache.ListLengthAsync

      operation.Telemetry.Success <- true

      return value |> int
    }

let listCachedTracks telemetryClient cache =
  fun key ->
    loadList telemetryClient cache key
    |> Task.map (List.ofArray >> List.map (string >> JSON.deserialize<Track>))

let private serializeTracks tracks =
  tracks |> List.map (JSON.serialize >> RedisValue)

[<RequireQualifiedAccess>]
module UserRepo =
  let usersTracksDatabase = 1

  let listLikedTracks telemetryClient (multiplexer: IConnectionMultiplexer) listLikedTracks (userId: UserId) =
    let database = multiplexer.GetDatabase(usersTracksDatabase)
    let listCachedTracks = listCachedTracks telemetryClient database
    let key = userId.Value

    fun () ->
      listCachedTracks key
      |> Task.bind (function
        | [] ->
          task {
            let! likedTracks = listLikedTracks ()

            do! replaceList telemetryClient database key (serializeTracks likedTracks)

            return likedTracks
          }
        | tracks -> Task.FromResult tracks)

[<RequireQualifiedAccess>]
module Playlist =
  let playlistsDatabase = 0

  let private getPlaylistsDatabase (multiplexer: IConnectionMultiplexer) =
    multiplexer.GetDatabase playlistsDatabase

  let appendTracks (telemetryClient: TelemetryClient) multiplexer =
    let prependList = prependList telemetryClient (getPlaylistsDatabase multiplexer)

    fun (playlistId: PlaylistId) tracks ->
      prependList playlistId.Value (serializeTracks tracks)

  let replaceTracks (telemetryClient: TelemetryClient) multiplexer =
    let replaceList = replaceList telemetryClient (getPlaylistsDatabase multiplexer)

    fun (playlistId: PlaylistId) tracks ->
      replaceList playlistId.Value (serializeTracks tracks)

  let listTracks telemetryClient multiplexer =
    fun (PlaylistId playlistId) ->
      listCachedTracks telemetryClient (getPlaylistsDatabase multiplexer) playlistId

  let countTracks telemetryClient multiplexer =
    let listLength = listLength telemetryClient (getPlaylistsDatabase multiplexer)

    fun (PlaylistId playlistId) -> listLength playlistId
