module Bolero.Web.Util

open System.Text.Json
open System.Text.Json.Serialization

type AsyncOp<'r> =
  | Loading
  | Finished of 'r