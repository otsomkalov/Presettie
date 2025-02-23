namespace MusicPlatform.Spotify

open System
open System.Net
open System.Text.RegularExpressions
open FSharp
open Microsoft.ApplicationInsights
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open Microsoft.FSharp.Control
open MusicPlatform
open MusicPlatform.Spotify.Cache
open SpotifyAPI.Web
open MusicPlatform.Spotify.Helpers
open otsom.fs.Auth
open otsom.fs.Auth.Repo
open otsom.fs.Auth.Settings
open otsom.fs.Extensions
open System.Collections.Generic
open System.Threading.Tasks

[<RequireQualifiedAccess>]
module Playlist =
  let loadTracks' limit loadBatch = async {
    let! initialBatch, totalCount = loadBatch 0

    return!
      match totalCount |> Option.ofNullable with
      | Some count ->
        [ limit..limit..count ]
        |> List.map (loadBatch >> Async.map fst)
        |> Async.Sequential
        |> Async.map (List.concat >> (List.append initialBatch))
      | None -> initialBatch |> async.Return
  }

  let rec private listTracks' (client: ISpotifyClient) playlistId (offset: int) = async {
    let! tracks =
      client.Playlists.GetItems(playlistId, PlaylistGetItemsRequest(Offset = offset))
      |> Async.AwaitTask

    return
      (tracks.Items
       |> Seq.choose (fun x ->
         match x.Track with
         | :? FullTrack as t -> Some t
         | _ -> None)
       |> getTracksIds,
       tracks.Total)
  }

  let listTracks (logger: ILogger) client =
    let playlistTracksLimit = 100

    fun (PlaylistId playlistId) ->
      let listPlaylistTracks = listTracks' client playlistId
      let loadTracks' = loadTracks' playlistTracksLimit

      task {
        try
          return! loadTracks' listPlaylistTracks

        with ApiException e when e.Response.StatusCode = HttpStatusCode.NotFound ->
          Logf.logfw logger "Playlist with id %s{PlaylistId} not found in Spotify" playlistId

          return []
      }

  let private getSpotifyIds =
    fun (tracks: Track list) ->
      tracks
      |> List.map _.Id
      |> List.map (fun (TrackId id) -> $"spotify:track:{id}")
      |> List<string>

  let addTracks (client: ISpotifyClient) =
    fun (PlaylistId playlistId) tracks ->
      client.Playlists.AddItems(playlistId, tracks |> getSpotifyIds |> PlaylistAddItemsRequest)
      &|> ignore

  let replaceTracks (client: ISpotifyClient) =
    fun (PlaylistId playlistId) tracks ->
      client.Playlists.ReplaceItems(playlistId, tracks |> getSpotifyIds |> PlaylistReplaceItemsRequest)
      &|> ignore

  let load (client: ISpotifyClient) =
    fun (PlaylistId playlistId) -> task {
      try
        let! playlist = playlistId |> client.Playlists.Get

        let! currentUser = client.UserProfile.Current()

        let playlist =
          if playlist.Owner.Id = currentUser.Id then
            Writable(
              { Id = playlist.Id |> PlaylistId
                Name = playlist.Name
                TracksCount = playlist.Tracks.Total.Value }
            )
          else
            Readable(
              { Id = playlist.Id |> PlaylistId
                Name = playlist.Name
                TracksCount = playlist.Tracks.Total.Value }
            )

        return playlist |> Ok
      with ApiException e when e.Response.StatusCode = HttpStatusCode.NotFound ->
        return Playlist.LoadError.NotFound |> Error
    }

  let parseId: Playlist.ParseId =
    fun (Playlist.RawPlaylistId rawPlaylistId) ->
      let getPlaylistIdFromUri (uri: Uri) = uri.Segments |> Array.last

      let (|Uri|_|) text =
        match Uri.TryCreate(text, UriKind.Absolute) with
        | true, uri -> Some uri
        | _ -> None

      let (|PlaylistId|_|) (text: string) =
        if Regex.IsMatch(text, "^[A-z0-9]{22}$") then
          Some text
        else
          None

      let (|SpotifyUri|_|) (text: string) =
        match text.Split(":") with
        | [| "spotify"; "playlist"; id |] -> Some(id)
        | _ -> None

      match rawPlaylistId with
      | SpotifyUri id -> id |> PlaylistId |> Ok
      | Uri uri -> uri |> getPlaylistIdFromUri |> PlaylistId |> Ok
      | PlaylistId id -> id |> PlaylistId |> Ok
      | id -> Playlist.IdParsingError(id) |> Error

[<RequireQualifiedAccess>]
module User =
  let rec private listLikedTracks'' (client: ISpotifyClient) (offset: int) = async {
    let! tracks =
      client.Library.GetTracks(LibraryTracksRequest(Offset = offset, Limit = 50))
      |> Async.AwaitTask

    return (tracks.Items |> Seq.map _.Track |> getTracksIds, tracks.Total)
  }

  let listLikedTracks' (client: ISpotifyClient) : User.ListLikedTracks =
    let likedTacksLimit = 50

    let listLikedTracks' = listLikedTracks'' client
    let loadTracks' = Playlist.loadTracks' likedTacksLimit

    fun () -> loadTracks' listLikedTracks' |> Async.StartAsTask

  let listLikedTracks telemetryClient multiplexer client userId =
    let listSpotifyTracks = listLikedTracks' client

    let listRedisTracks =
      Redis.UserRepo.listLikedTracks telemetryClient multiplexer listSpotifyTracks userId

    Memory.UserRepo.listLikedTracks listRedisTracks

[<RequireQualifiedAccess>]
module Track =
  let getRecommendations (client: ISpotifyClient) : Track.GetRecommendations =
    let recommendationsLimit = 100

    fun tracks ->
      let request = RecommendationsRequest()

      for track in tracks |> List.takeSafe 5 do
        request.SeedTracks.Add(track |> TrackId.value)

      request.Limit <- recommendationsLimit

      client.Browse.GetRecommendations(request)
      |> Task.map _.Tracks
      |> Task.map (
        Seq.map (fun st ->
          { Id = TrackId st.Id
            Artists = st.Artists |> Seq.map (fun a -> { Id = ArtistId a.Id }) |> Set.ofSeq })
        >> Seq.toList
      )

[<RequireQualifiedAccess>]
module TargetedPlaylistRepo =
  let private applyTracks spotifyAction cacheAction =
    fun (playlistId: PlaylistId) (tracks: Track list) ->
      let spotifyTask: Task<unit> = spotifyAction playlistId tracks
      let cacheTask: Task<unit> = cacheAction playlistId tracks

      Task.WhenAll([ spotifyTask; cacheTask ]) |> Task.ignore

  let addTracks (telemetryClient: TelemetryClient) (spotifyClient: ISpotifyClient) multiplexer =
    let addInSpotify = Playlist.addTracks spotifyClient
    let addInCache = Redis.Playlist.appendTracks telemetryClient multiplexer

    applyTracks addInSpotify addInCache

  let replaceTracks (telemetryClient: TelemetryClient) (spotifyClient: ISpotifyClient) multiplexer =
    let replaceInSpotify = Playlist.replaceTracks spotifyClient
    let replaceInCache = Redis.Playlist.replaceTracks telemetryClient multiplexer

    applyTracks replaceInSpotify replaceInCache

[<RequireQualifiedAccess>]
module PlaylistRepo =
  let listTracks telemetryClient multiplexer logger client =
    let listCachedPlaylistTracks = Redis.Playlist.listTracks telemetryClient multiplexer
    let listSpotifyPlaylistTracks = Playlist.listTracks logger client
    let cachePlaylistTracks = Redis.Playlist.replaceTracks telemetryClient multiplexer

    fun playlistId ->
      listCachedPlaylistTracks playlistId
      |> Task.bind (function
        | [] ->
          listSpotifyPlaylistTracks playlistId
          |> Task.taskTap (cachePlaylistTracks playlistId)
        | tracks -> Task.FromResult tracks)

module Library =

  type UserId with
    member this.ToAccountId() = this.Value |> AccountId

  let getClient (authRepo: #ILoadCompletedAuth) (authOptions: IOptions<AuthSettings>) =
    let authSettings = authOptions.Value
    let clients = Dictionary<UserId, ISpotifyClient>()

    fun (userId: UserId) ->
      match clients.TryGetValue(userId) with
      | true, client -> client |> Some |> Task.FromResult
      | false, _ ->
        userId.ToAccountId()
        |> authRepo.LoadCompletedAuth
        |> TaskOption.taskMap (fun auth -> task {
          let! tokenResponse =
            AuthorizationCodeRefreshRequest(authSettings.ClientId, authSettings.ClientSecret, auth.Token.Value)
            |> OAuthClient().RequestToken

          let retryHandler =
            SimpleRetryHandler(RetryAfter = TimeSpan.FromSeconds(30), RetryTimes = 3, TooManyRequestsConsumesARetry = true)

          let config =
            SpotifyClientConfig
              .CreateDefault()
              .WithRetryHandler(retryHandler)
              .WithToken(tokenResponse.AccessToken)

          return config |> SpotifyClient :> ISpotifyClient
        })
        |> TaskOption.tap (fun client -> clients.TryAdd(userId, client) |> ignore)

  let buildMusicPlatform
    (authService: IAuthRepo)
    authOptions
    (logger: ILogger<BuildMusicPlatform>)
    telemetryClient
    multiplexer
    : BuildMusicPlatform =

    fun userId ->
      userId |> getClient authService authOptions
      &|> Option.map (fun client ->
        { new IMusicPlatform with
            member this.LoadPlaylist(playlistId) = Playlist.load client playlistId
            member this.GetRecommendations(tracks) = Track.getRecommendations client tracks

            member this.AddTracks(playlistId, tracks) =
              TargetedPlaylistRepo.addTracks telemetryClient client multiplexer playlistId tracks

            member this.ReplaceTracks(playlistId, tracks) =
              TargetedPlaylistRepo.replaceTracks telemetryClient client multiplexer playlistId tracks

            member this.ListLikedTracks() =
              User.listLikedTracks telemetryClient multiplexer client userId ()

            member this.ListPlaylistTracks(playlistId) =
              Playlist.listTracks logger client playlistId })