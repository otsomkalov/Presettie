module API.Services

open System
open System.IdentityModel.Tokens.Jwt
open System.Security.Claims
open System.Text
open System.Threading.Tasks
open Microsoft.Extensions.Options
open Microsoft.IdentityModel.Tokens
open otsom.fs.Core
open otsom.fs.Extensions

[<CLIMutable>]
type JWTSettings =
  { Issuer: string
    Audience: string
    Secret: string
    Expiration: TimeSpan }

  static member SectionName = "Auth"

type TokenUser = { Id: UserId }

type IDecodeToken =
  abstract DecodeToken: token: string -> Task<TokenUser option>

type IGenerateToken =
  abstract GenerateToken: userId: UserId -> string

type IJWTService =
  inherit IDecodeToken
  inherit IGenerateToken

type JWTService(options: IOptions<JWTSettings>) =
  let settings = options.Value
  let secretBytes = Encoding.UTF8.GetBytes(settings.Secret)
  let securityKey = SymmetricSecurityKey(secretBytes)

  let signingCredentials =
    SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256)

  interface IJWTService with
    member this.GenerateToken(userId: UserId) =
      let claims =
        [ Claim(JwtRegisteredClaimNames.Sub, userId.Value) ]

      let token =
        JwtSecurityToken(
          settings.Issuer,
          settings.Audience,
          claims,
          Nullable<DateTime>(),
          DateTime.UtcNow.Add(settings.Expiration),
          signingCredentials
        )

      let tokenHandler = JwtSecurityTokenHandler()

      tokenHandler.WriteToken(token)

    member this.DecodeToken(token) =
      let tokenHandler = JwtSecurityTokenHandler()

      let tokenValidationParameters =
        TokenValidationParameters(
          ValidateIssuerSigningKey = true,
          IssuerSigningKey = SymmetricSecurityKey(secretBytes),

          ValidateIssuer = true,
          ValidIssuer = settings.Issuer,

          ValidateAudience = true,
          ValidAudience = settings.Audience,

          ValidateLifetime = true
        )

      tokenHandler.ValidateTokenAsync(token, tokenValidationParameters)
      |> Task.map (fun result ->
        match result.IsValid with
        | true ->
          match result.Claims.TryGetValue(ClaimTypes.NameIdentifier) with
          | true, userId -> Some({ Id = userId |> string |> UserId })
          | _ -> None
        | _ -> None)