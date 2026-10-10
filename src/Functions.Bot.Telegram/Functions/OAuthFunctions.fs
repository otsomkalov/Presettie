namespace Functions.Bot.Telegram.Functions

open System
open System.Net.Http
open System.Net.Http.Headers
open System.Net.Http.Json
open System.Text.Json.Serialization
open System.Threading.Tasks
open Bot.Core
open Bot.Repos
open Domain.Core
open Domain.Repos
open Domain.Workflows
open FsToolkit.ErrorHandling
open Infrastructure.Core
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.Functions.Worker
open Microsoft.Extensions.Options
open otsom.fs.Bot
open otsom.fs.OAuth
open otsom.fs.OAuth.Keycloak
open otsom.fs.Resources

[<CLIMutable>]
type AdditionalOidcConfiguration =
  {
    [<JsonPropertyName("userinfo_endpoint")>]
    UserinfoEndpoint: string
  }

[<CLIMutable>]
type UserInfo =
  {
    [<JsonPropertyName("sub")>]
    Id: Guid
  }

type OAuthFunctions
  (
    userRepo: IUserRepo,
    chatRepo: IChatRepo,
    oAuthClient: IOAuthClient,
    resourcesOptions: IOptions<ResourcesSettings>,
    additionalOAuthOptions: IOptions<AdditionalOAuthSettings>,
    keycloakOAuthOptions: IOptions<KeycloakOAuthSettings>,
    httpClient: HttpClient
  ) =
  let additionalOAuthSettings = additionalOAuthOptions.Value
  let resourcesSettings = resourcesOptions.Value
  let keycloakOAuthSettings = keycloakOAuthOptions.Value

  [<Function("AppCallback")>]
  member this.AppCallback
    ([<HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "oauth/app/callback")>] request: HttpRequest)
    : Task<IActionResult> =

    let handler =
      fun (request: HttpRequest) -> taskResult {
        let state = request.Query["state"]
        let code = request.Query["code"]

        let! completedAuth = oAuthClient.CompleteAuth(State state, Code code)

        let! oidcSettings =
          httpClient.GetFromJsonAsync<AdditionalOidcConfiguration>(keycloakOAuthSettings.OpenIdConfigurationUri)

        use request = new HttpRequestMessage(HttpMethod.Get, oidcSettings.UserinfoEndpoint)

        request.Headers.Authorization <- AuthenticationHeaderValue("Bearer", completedAuth.AccessToken.Value)

        use! response = httpClient.SendAsync(request)

        response.EnsureSuccessStatusCode() |> ignore

        let! userInfo = response.Content.ReadFromJsonAsync<UserInfo>()

        let! user = User.loadOrCreate userRepo (UserId userInfo.Id)

        let chat: Chat =
          {
            Id = completedAuth.AccountId.Value |> int64 |> ChatId
            UserId = user.Id
            Lang = resourcesSettings.DefaultLang
          }

        do! chatRepo.SaveChat chat

        return ()
      }

    handler request
    |> TaskResult.foldResult (fun _ -> RedirectResult(additionalOAuthSettings.ReturnUri, true) :> IActionResult) (fun err ->
      BadRequestObjectResult(err) :> IActionResult)