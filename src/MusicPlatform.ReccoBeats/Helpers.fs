[<RequireQualifiedAccess>]
module MusicPlatform.ReccoBeats.Helpers

open System

let extractId = fun uri -> uri |> Uri |> (_.Segments >> Seq.last)