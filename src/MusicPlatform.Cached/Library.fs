namespace MusicPlatform.Cached

open System.Collections.Generic
open FSharp.Control
open System.Collections.Concurrent
open System.Threading.Tasks
open Microsoft.ApplicationInsights
open MusicPlatform
open MusicPlatform.Spotify.Cache
open StackExchange.Redis
open FsToolkit.ErrorHandling

type RedisMusicPlatform
  (musicPlatform: IMusicPlatform, telemetryClient: TelemetryClient, multiplexer: IConnectionMultiplexer, userId: UserId) =
  interface IMusicPlatform with
    member this.AddTracks(playlistId, tracks) = task {
      do! Redis.Playlist.appendTracks telemetryClient multiplexer playlistId tracks

      do! musicPlatform.AddTracks(playlistId, tracks)
    }

    member this.ListLikedTracks() =
      Redis.UserRepo.listLikedTracks telemetryClient multiplexer musicPlatform.ListLikedTracks userId ()

    member this.ListPlaylistTracks(playlistId) = task {
      let! tracks = Redis.Playlist.listTracks telemetryClient multiplexer playlistId

      match tracks with
      | [] ->
        let! tracks = musicPlatform.ListPlaylistTracks playlistId

        do! Redis.Playlist.replaceTracks telemetryClient multiplexer playlistId tracks

        return tracks
      | _ -> return tracks
    }

    member this.LoadPlaylist(playlistId) = musicPlatform.LoadPlaylist playlistId

    member this.ReplaceTracks(playlistId, tracks) = task {
      do! Redis.Playlist.replaceTracks telemetryClient multiplexer playlistId tracks

      do! musicPlatform.ReplaceTracks(playlistId, tracks)
    }

    member this.ListArtistTracks(artistId) =
      let artistsTracksDatabase = 2
      let database = multiplexer.GetDatabase artistsTracksDatabase
      let loadList = Redis.listCachedTracks telemetryClient database
      let replaceList = Redis.replaceList telemetryClient database

      taskSeq {
        let! tracks = loadList artistId.Value

        match tracks with
        | [] ->
          let! tracks = musicPlatform.ListArtistTracks artistId |> TaskSeq.toListAsync

          do! replaceList artistId.Value (tracks |> Redis.serializeTracks)

          yield! tracks
        | tracks -> yield! tracks
      }

    member this.Recommend(tracks) = musicPlatform.Recommend tracks
    member this.LoadArtist(id) = musicPlatform.LoadArtist id

type MemoryCachedMusicPlatform(musicPlatform: IMusicPlatform) =
  interface IMusicPlatform with
    member this.AddTracks(playlistId, tracks) =
      musicPlatform.AddTracks(playlistId, tracks)

    member this.ListLikedTracks() =
      Memory.UserRepo.listLikedTracks musicPlatform.ListLikedTracks ()

    member this.ListPlaylistTracks(playlistId) =
      musicPlatform.ListPlaylistTracks playlistId

    member this.LoadPlaylist(playlistId) = musicPlatform.LoadPlaylist playlistId

    member this.ReplaceTracks(playlistId, tracks) =
      musicPlatform.ReplaceTracks(playlistId, tracks)

    member this.ListArtistTracks(artistId) = musicPlatform.ListArtistTracks artistId
    member this.Recommend(tracks) = musicPlatform.Recommend tracks
    member this.LoadArtist(id) = musicPlatform.LoadArtist id

type RedisMusicPlatformFactory
  (getMusicPlatform: IMusicPlatformFactory, telemetryClient: TelemetryClient, multiplexer: IConnectionMultiplexer) =
  interface IMusicPlatformFactory with
    member this.GetMusicPlatform(userId) = task {
      let! musicPlatform = getMusicPlatform.GetMusicPlatform userId

      match musicPlatform with
      | Some platform ->
        return
          RedisMusicPlatform(platform, telemetryClient, multiplexer, userId) :> IMusicPlatform
          |> Some
      | None -> return None
    }

type MemoryCachedMusicPlatformFactory(getMusicPlatform: IMusicPlatformFactory) =
  let inFlight = ConcurrentDictionary<UserId, Lazy<Task<IMusicPlatform option>>>()
  let cache = ConcurrentDictionary<UserId, MemoryCachedMusicPlatform>()

  interface IMusicPlatformFactory with
    member this.GetMusicPlatform(userId) = task {
      match cache.TryGetValue userId with
      | true, platform -> return Some platform
      | false, _ ->
        let candidate = Lazy.Create(fun () -> getMusicPlatform.GetMusicPlatform userId)

        let actual = inFlight.GetOrAdd(userId, candidate)

        try
          let! result = actual.Value |> TaskOption.map MemoryCachedMusicPlatform

          match result with
          | Some value -> cache.TryAdd(userId, value) |> ignore
          | None -> ()

          return result |> Option.map (fun p -> p :> IMusicPlatform)
        finally
          inFlight.TryRemove(KeyValuePair(userId, actual)) |> ignore
    }