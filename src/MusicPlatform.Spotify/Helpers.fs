namespace MusicPlatform.Spotify

open System
open System.Text.Json
open System.Text.Json.Serialization
open MusicPlatform
open SpotifyAPI.Web

module Helpers =
  let getTracksIds (tracks: FullTrack seq) : Track list =
    tracks
    |> Seq.filter (isNull >> not)
    |> Seq.filter (_.Id >> isNull >> not)
    |> Seq.map (fun st ->
      { Id = TrackId st.Id
        Artists = st.Artists |> Seq.map (fun a -> { Id = ArtistId a.Id }) |> Set.ofSeq })
    |> Seq.toList

  let (|ApiException|_|) (ex: exn) =
    match ex with
    | :? AggregateException as aggregateException ->
      aggregateException.InnerExceptions
      |> Seq.tryPick (fun e -> e :?> APIException |> Option.ofObj)
    | :? APIException as e -> Some e
    | _ -> None

module JSON =
  let options =
    JsonFSharpOptions.Default().WithUnionExternalTag().WithUnionUnwrapRecordCases().ToJsonSerializerOptions()

  let serialize value =
    JsonSerializer.Serialize(value, options)

  let deserialize<'a> (json: string) =
    JsonSerializer.Deserialize<'a>(json, options)