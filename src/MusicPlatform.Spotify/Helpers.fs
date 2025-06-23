namespace MusicPlatform.Spotify

open System
open MusicPlatform
open SpotifyAPI.Web
open System.Collections.Generic

module Helpers =
  let mapTracks (tracks: FullTrack seq) : Track list =
    tracks
    |> Seq.filter (isNull >> not)
    |> Seq.filter (_.Id >> isNull >> not)
    |> Seq.map (fun st ->
      { Id = TrackId st.Id
        Artists = st.Artists |> Seq.map (fun a -> { Id = ArtistId a.Id }) |> Set.ofSeq })
    |> Seq.toList

  let mapToSpotifyTracksIds =
    fun (tracks: Track list) ->
      tracks
      |> List.map _.Id
      |> List.map (fun (TrackId id) -> $"spotify:track:{id}")
      |> List<string>

  let (|ApiException|_|) (ex: exn) =
    match ex with
    | :? AggregateException as aggregateException ->
      aggregateException.InnerExceptions
      |> Seq.tryPick (fun e -> e :?> APIException |> Option.ofObj)
    | :? APIException as e -> Some e
    | _ -> None