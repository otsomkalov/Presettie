namespace Shared

open System.Text.Json
open System.Text.Json.Serialization

module JSON =
  let settings =
    JsonFSharpOptions.Default().WithUnionInternalTag().WithUnionUnwrapRecordCases().WithUnionUnwrapSingleFieldCases()

  let options =
    let options = settings.ToJsonSerializerOptions()

    options.PropertyNameCaseInsensitive <- true

    options

  let serialize value =
    JsonSerializer.Serialize(value, options)

  let deserialize<'a> (json: string) =
    JsonSerializer.Deserialize<'a>(json, options)