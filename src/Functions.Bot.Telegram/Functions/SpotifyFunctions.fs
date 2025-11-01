namespace Functions.Bot.Telegram

open System.Threading.Tasks
open Bot.Telegram.Settings
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.Functions.Worker
open Microsoft.Extensions.Options
open Functions.Bot.Telegram.Extensions.IQueryCollection
open StackExchange.Redis
open otsom.fs.Auth
open otsom.fs.Extensions

type SpotifyFunctions
  (_telegramOptions: IOptions<TelegramSettings>, _connectionMultiplexer: IConnectionMultiplexer, authService: IAuthService) =

  let _telegramSettings = _telegramOptions.Value

  [<Function("HandleCallbackAsync")>]
  member this.HandleCallbackAsync([<HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "spotify/callback")>] request: HttpRequest) =
    let onSuccess (state: string) =
      RedirectResult($"{_telegramSettings.BotUrl}?start={state}", true) :> IActionResult

    let onError error =
      match error with
      | FulfillmentError.StateNotFound -> BadRequestObjectResult("State not found in the cache") :> IActionResult

    match request.Query["state"], request.Query["code"] with
    | QueryParam state, QueryParam code ->
      authService.FulfillAuth(State.Parse state, Code code)
      |> TaskResult.either onSuccess onError
    | QueryParam _, _ -> BadRequestObjectResult("Code is empty") :> IActionResult |> Task.FromResult
    | _, QueryParam _ -> BadRequestObjectResult("State is empty") :> IActionResult |> Task.FromResult
    | _, _ ->
      BadRequestObjectResult("State and code are empty") :> IActionResult
      |> Task.FromResult