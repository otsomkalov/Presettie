namespace MusicPlatform.Spotify

open System
open MusicPlatform
open SpotifyAPI.Web
open System.Collections.Generic

module Helpers =
  let mapToSpotifyTracksIds =
    fun (tracks: Track list) ->
      tracks
      |> List.map _.Id
      |> List.map (fun (TrackId id) -> $"spotify:track:{id}")
      |> List<string>

  let inline filterValidTracks<'a when 'a: (member Id: string) and 'a: null> =
    fun (tracks: 'a seq) -> tracks |> Seq.filter (isNull >> not) |> Seq.filter (_.Id >> isNull >> not)

  let (|ApiException|_|) (ex: exn) =
    match ex with
    | :? AggregateException as aggregateException ->
      aggregateException.InnerExceptions
      |> Seq.tryPick (fun e -> e :?> APIException |> Option.ofObj)
    | :? APIException as e -> Some e
    | _ -> None

module Seq =
  let takeSafe (n: int) (source: seq<_>) = seq {
    use e = source.GetEnumerator()

    for i = 1 to n do
      if e.MoveNext() then
        yield e.Current
  }