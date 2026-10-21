module Functions.API.Shared

open Domain.Core
open Microsoft.Azure.Functions.Worker
open otsom.fs.OAuth

type ValidationError = { Member: string; Error: string }

type UserId with
  member this.ToAccountId() = this.Value |> string |> AccountId

type RequestError<'a> =
  | Forbidden
  | Validation of ValidationError list
  | Operation of 'a

type FunctionContext with
  member this.UserId = this.Items[nameof UserId] :?> UserId