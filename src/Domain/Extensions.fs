module Domain.Extensions


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

let flip f a b = f b a