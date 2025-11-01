module Bolero.Web.Util

open System.Text.Json
open System.Text.Json.Serialization

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