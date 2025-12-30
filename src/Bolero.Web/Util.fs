module Bolero.Web.Util

open System.Text.Json
open System.Text.Json.Serialization
open BlazorBootstrap
open Bolero
open FsToolkit.ErrorHandling
open otsom.fs.Extensions

type AsyncOp<'r> =
  | Loading
  | Finished of 'r

module JSON =
  let settings = JsonFSharpOptions.Default().WithUnionUnwrapFieldlessTags()

  let options =
    let options = settings.ToJsonSerializerOptions()

    options.PropertyNameCaseInsensitive <- true

    options

  let serialize value =
    JsonSerializer.Serialize(value, options)

  let deserialize<'a> (json: string) =
    JsonSerializer.Deserialize<'a>(json, options)

[<RequireQualifiedAccess>]
module Modal =
  let show (ref: Ref<Modal>) = task {
    match ref.Value with
    | Some m -> do! m.ShowAsync()
    | None -> ()
  }

  let hide (ref: Ref<Modal>) = task {
    match ref.Value with
    | Some m -> do! m.HideAsync()
    | None -> ()
  }