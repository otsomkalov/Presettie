﻿namespace MusicPlatform.Spotify

open System
open System.Net
open System.Text.RegularExpressions
open FSharp
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open Microsoft.FSharp.Control
open MusicPlatform
open MusicPlatform.Spotify.Mappings
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

  let rec listTracks' (client: ISpotifyClient) playlistId (offset: int) = async {
    let! tracks =
      client.Playlists.GetItems(playlistId, PlaylistGetItemsRequest(Offset = offset))
      |> Async.AwaitTask

    return
      (tracks.Items
       |> Seq.choose (fun x ->
         match x.Track with
         | :? FullTrack as t -> Some t
         | _ -> None)
       |> filterValidTracks
       |> Seq.map Track.fromFull
       |> List.ofSeq,
       tracks.Total)
  }

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

    return
      (tracks.Items
       |> Seq.map _.Track
       |> filterValidTracks
       |> Seq.map Track.fromFull
       |> List.ofSeq,
       tracks.Total)
  }

  let listLikedTracks' (client: ISpotifyClient) : User.ListLikedTracks =
    let likedTacksLimit = 50

    let listLikedTracks' = listLikedTracks'' client
    let loadTracks' = Playlist.loadTracks' likedTacksLimit

    fun () -> loadTracks' listLikedTracks' |> Async.StartAsTask

[<RequireQualifiedAccess>]
module Track =
  let getRecommendations (client: ISpotifyClient) : Track.GetRecommendations =
    let recommendationsLimit = 100

    fun tracks ->
      let request = RecommendationsRequest()

      for trackId in tracks |> List.takeSafe 5 do
        request.SeedTracks.Add(trackId.Value)

      request.Limit <- recommendationsLimit

      client.Browse.GetRecommendations(request)
      |> Task.map _.Tracks
      |> Task.map (Seq.map Track.fromFull >> Seq.toList)

type SpotifyMusicPlatform(client: ISpotifyClient, logger: ILogger<SpotifyMusicPlatform>) =
  let playlistTracksLimit = 100


  interface IMusicPlatform with
    member this.AddTracks(PlaylistId playlistId, tracks) =
      client.Playlists.AddItems(playlistId, tracks |> mapToSpotifyTracksIds |> PlaylistAddItemsRequest)
      &|> ignore

    member this.ListLikedTracks() = User.listLikedTracks' client ()

    member this.ListPlaylistTracks(PlaylistId playlistId) =
      let listPlaylistTracks = Playlist.listTracks' client playlistId
      let loadTracks' = Playlist.loadTracks' playlistTracksLimit

      task {
        try
          return! loadTracks' listPlaylistTracks

        with ApiException e when e.Response.StatusCode = HttpStatusCode.NotFound ->
          Logf.logfw logger "Playlist with id %s{PlaylistId} not found in Spotify" playlistId

          return []
      }

    member this.LoadPlaylist(playlistId) = Playlist.load client playlistId

    member this.ReplaceTracks(PlaylistId playlistId, tracks) =
      client.Playlists.ReplaceItems(playlistId, tracks |> mapToSpotifyTracksIds |> PlaylistReplaceItemsRequest)
      &|> ignore

    member this.ListArtistTracks(ArtistId artistId) = task {
      let request =
        ArtistsAlbumsRequest(IncludeGroupsParam = ArtistsAlbumsRequest.IncludeGroups.Album, Limit = 50)

      let! artistAlbums = client.Artists.GetAlbums(artistId, request)

      if artistAlbums.Items.Count = 0 then
        return []
      else
        let request =
          AlbumsRequest(
            artistAlbums.Items
            |> Seq.map (_.Id >> AlbumId)
            |> Seq.takeSafe 20
            |> Seq.map _.Value
            |> List<string>
          )

        let! albums = client.Albums.GetSeveral(request)

        return albums.Albums |> Seq.map Album.fromFull |> Seq.collect _.Tracks |> List.ofSeq
    }

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
            SimpleRetryHandler(RetryAfter = TimeSpan.FromSeconds(30L), RetryTimes = 3, TooManyRequestsConsumesARetry = true)

          let config =
            SpotifyClientConfig.CreateDefault().WithRetryHandler(retryHandler).WithToken(tokenResponse.AccessToken)

          return config |> SpotifyClient :> ISpotifyClient
        })
        |> TaskOption.tap (fun client -> clients.TryAdd(userId, client) |> ignore)

type SpotifyMusicPlatformFactory(authService: IAuthRepo, authOptions, logger) =
  interface IMusicPlatformFactory with
    member this.GetMusicPlatform(userId) =
      Library.getClient authService authOptions userId
      &|> Option.map (fun client -> SpotifyMusicPlatform(client, logger))