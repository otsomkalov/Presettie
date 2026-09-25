module Functions.API.Shared

open System
open Domain.Core
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Http
open FsToolkit.ErrorHandling
open otsom.fs.Auth
open otsom.fs.Extensions

type TokenUser = { UserId: UserId }

type ValidRequest<'a> = { User: TokenUser; Body: 'a }

type ValidationError = { Member: string; Error: string }

type UserId with
  member this.ToAccountId() = this.Value |> string |> AccountId

type RequestError<'a> =
  | Unauthorized
  | Validation of ValidationError list
  | Operation of 'a

let validateUser (authnService: IAuthenticationService) (req: HttpRequest) : TaskResult<TokenUser, RequestError<_>> =
  authnService.AuthenticateAsync(req.HttpContext, JwtBearerDefaults.AuthenticationScheme)
  |> Task.map (Option.someIf _.Succeeded)
  |> Task.map (Option.bind (_.Principal >> Option.ofObj))
  |> Task.map (Option.bind (_.Identity >> Option.ofObj))
  |> Task.map (Option.bind (_.Name >> Option.ofObj))
  |> Task.map (Option.bind (Guid.TryParse >> Option.someIf fst >> Option.map snd))
  |> TaskOption.map (fun userId -> { UserId = UserId userId })
  |> Task.map (Result.requireSome RequestError.Unauthorized)