module Web.Main

open Elmish
open Bolero
open Bolero.Html
open Web.Shared

[<RequireQualifiedAccess>]
module Loading =
  let render () _ = div {
    attr.``class`` "d-flex flex-column align-items-center justify-content-center vh-100"

    div {
      attr.``class`` "spinner-border text-primary"
      "role" => "status"

      span {
        attr.``class`` "visually-hidden"
        text "Loading..."
      }
    }
  }

let initModel = { Page = Page.Loading }, Cmd.none

let update (message: Message) (model: Model) =
  match message, model.Page with
  | PageChanged(Page.Home), _ -> { model with Page = Page.Home }, Cmd.none
  | PageChanged(Page.Presets), _ -> { model with Page = Page.Presets }, Cmd.none
  | PageChanged(Page.NotFound), _ -> { model with Page = Page.NotFound }, Cmd.none
  | PageChanged(Page.Loading), _ -> { model with Page = Page.Loading }, Cmd.none

let view model dispatch = concat {
  Layout.Header.view dispatch

  div {
    attr.``class`` "container-md"

    match model.Page with
    | Page.Home -> div { text "Home" }
    | Page.Presets -> div { text "Presets" }
    | Page.NotFound -> div { text "Not Found" }
    | Page.Loading -> Loading.render () dispatch
  }
}

type App() =
  inherit ProgramComponent<Model, Message>()

  override this.Program =
    Program.mkProgram (fun _ -> initModel) update view
    |> Program.withConsoleTrace
    |> Program.withRouter router