module Web.Main

open Elmish
open Bolero
open Bolero.Html

[<RequireQualifiedAccess>]
type Page =
  | [<EndPoint("/")>] Home
  | [<EndPoint("**")>] Loading

type Model = { Page: Page }

let initModel = { Page = Page.Loading }, Cmd.none

type Message = PageChanged of Page

let update (message: Message) (model: Model) =
  match message, model.Page with
  | PageChanged(Page.Home), _ -> { model with Page = Page.Home }, Cmd.none
  | PageChanged(Page.Loading), _ -> { model with Page = Page.Loading }, Cmd.none

let view model dispatch = concat {
  Layout.Header.view dispatch

  div {
    attr.``class`` "container-md"

    match model.Page with
    | Page.Loading -> div { text "Loading..." }
    | Page.Home -> div {
        text "Home"

        a {
          attr.href "/presets"

          text "Presets"
        }
      }
  }
}

type App() =
  inherit ProgramComponent<Model, Message>()

  let defaultModel =
    function
    | Page.Home -> ()
    | Page.Loading -> ()

  override this.Program =
    let router = Router.inferWithModel PageChanged _.Page defaultModel

    Program.mkProgram (fun _ -> initModel) update view
    |> Program.withConsoleTrace
    |> Program.withRouter router