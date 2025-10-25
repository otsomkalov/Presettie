namespace MusicPlatform.ReccoBeats

open System.Net.Http
open System.Net.Http.Json
open System.Text.Json.Serialization
open Microsoft.AspNetCore.WebUtilities
open MusicPlatform
open MusicPlatform.ReccoBeats
open otsom.fs.Extensions

[<CLIMutable>]
type Settings =
  { Url: string }

  static member SectionName = "ReccoBeats"

type internal ArtistResponse =
  { Href: string }

  member this.ToDomain() =
    { Id = this.Href |> Helpers.extractId |> ArtistId }

type internal TrackResponse =
  { Href: string
    Artists: ArtistResponse list }

  member this.ToDomain() =
    { Id = this.Href |> Helpers.extractId |> TrackId
      Artists = this.Artists |> List.map _.ToDomain() |> Set.ofList }

type internal Response = { Content: TrackResponse list }

type ReccoBeatsRecommender(httpClientFactory: IHttpClientFactory) =
  [<Literal>]
  let seedsLimit = 5

  [<Literal>]
  let recommendationsLimit = 100

  let jsonSettings = JsonFSharpOptions.Default().ToJsonSerializerOptions()

  do jsonSettings.PropertyNameCaseInsensitive <- true

  interface IRecommender with
    member this.Recommend(tracks) =
      let queryParams =
        [ ("seeds", String.concat "," (tracks |> List.takeSafe seedsLimit |> List.map _.Id.Value))
          ("size", string recommendationsLimit) ]
        |> dict

      let path = QueryHelpers.AddQueryString("track/recommendation", queryParams)

      task {
        use httpClient = httpClientFactory.CreateClient(Settings.SectionName)

        let! response = httpClient.GetFromJsonAsync<Response>(path, jsonSettings)

        return response.Content |> List.map _.ToDomain()
      }