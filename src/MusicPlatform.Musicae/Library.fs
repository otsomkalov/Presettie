namespace MusicPlatform.Musicae

open System.Net.Http
open System.Net.Http.Json
open System.Text.Json.Serialization
open Microsoft.AspNetCore.WebUtilities
open Microsoft.Extensions.Options
open MusicPlatform
open otsom.fs.Extensions

[<CLIMutable>]
type Settings =
  { Host: string
    Key: string }

  static member SectionName = "Musicae"

type internal ArtistResponse =
  { Id: string
    Name: string }

  member this.ToDomain() : Artist =
    { Id = ArtistId this.Id
      Name = this.Name }

type internal TrackResponse =
  { Id: string
    Artists: ArtistResponse list }

  member this.ToDomain() : Track =
    { Id = TrackId this.Id
      Artists = this.Artists |> List.map _.ToDomain() |> Set.ofList }

[<CLIMutable>]
type internal Response = { Tracks: TrackResponse list }

type MusicaeRecommender(httpClientFactory: IHttpClientFactory, options: IOptions<Settings>) =
  [<Literal>]
  let seedsLimit = 5

  [<Literal>]
  let recommendationsLimit = 100

  let jsonSettings = JsonFSharpOptions.Default().ToJsonSerializerOptions()

  do jsonSettings.PropertyNameCaseInsensitive <- true

  let settings = options.Value

  interface IRecommender with
    member this.Recommend(tracks) =
      let queryParams =
        [ ("seed_tracks", String.concat "," (tracks |> List.takeSafe seedsLimit |> List.map _.Id.Value))
          ("limit", string recommendationsLimit) ]
        |> dict

      let path = QueryHelpers.AddQueryString("recommendations", queryParams)

      task {
        use httpClient = httpClientFactory.CreateClient(Settings.SectionName)
        use request = new HttpRequestMessage(HttpMethod.Get, path)

        request.Headers.Add("x-rapidapi-host", settings.Host)
        request.Headers.Add("x-rapidapi-key", settings.Key)

        use! response = httpClient.SendAsync(request)

        do response.EnsureSuccessStatusCode() |> ignore

        let! responseContent = response.Content.ReadFromJsonAsync<Response>(jsonSettings)

        return responseContent.Tracks |> List.map _.ToDomain()
      }