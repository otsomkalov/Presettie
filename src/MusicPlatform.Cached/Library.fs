namespace MusicPlatform.Cached

open Microsoft.ApplicationInsights
open MusicPlatform
open MusicPlatform.Spotify.Cache
open StackExchange.Redis

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

      task {
        let! tracks = loadList artistId.Value

        match tracks with
        | [] ->
          let! tracks = musicPlatform.ListArtistTracks artistId

          do! replaceList artistId.Value (tracks |> Redis.serializeTracks)

          return tracks
        | tracks -> return tracks
      }

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
  interface IMusicPlatformFactory with
    member this.GetMusicPlatform(var0) = task {
      let! musicPlatform = getMusicPlatform.GetMusicPlatform var0

      match musicPlatform with
      | Some platform -> return MemoryCachedMusicPlatform(platform) :> IMusicPlatform |> Some
      | None -> return None
    }