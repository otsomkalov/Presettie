module Functions.API.Shared

open System
open Domain.Core
open Microsoft.AspNetCore.Http
open otsom.fs.OAuth

type ValidationError = { Member: string; Error: string }

type UserId with
  member this.ToAccountId() = this.Value |> string |> AccountId

type RequestError<'a> =
  | Forbidden
  | Validation of ValidationError list
  | Operation of 'a

type HttpRequest with
  member this.UserId = this.Headers["X-MS-CLIENT-PRINCIPAL-ID"] |> Guid.Parse |> UserId