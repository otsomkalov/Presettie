module Domain.Extensions

open System.Threading.Tasks

[<RequireQualifiedAccess>]
module List =
  let errorIfEmpty (error: 'e) =
    function
    | [] -> Error error
    | v -> Ok v

[<RequireQualifiedAccess>]
module Result =
  let errorIf condition (error: 'e) =
    fun arg -> if (condition arg) then Error error else Ok arg

[<RequireQualifiedAccess>]
module TaskOption =
  let taskBind binder =
    function
    | None -> Task.FromResult None
    | Some v -> binder v

let flip f a b = f b a