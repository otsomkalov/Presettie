[<RequireQualifiedAccess>]
module Web.Layout

open Bolero.Html
open Microsoft.AspNetCore.Components.Routing
open Web.Shared

module Header =
  let view dispatch = nav {
    attr.``class`` "navbar bg-primary navbar-expand-lg"
    "data-bs-theme" => "dark"

    div {
      attr.``class`` "container-fluid"

      a {
        attr.``class`` "navbar-brand"
        attr.href "/"
        "Presettie"
      }

      button {
        attr.``class`` "navbar-toggler"
        attr.``type`` "button"
        "data-bs-toggle" => "collapse"
        "data-bs-target" => "#navbarNavDropdown"
        "aria-controls" => "navbarNavDropdown"
        "aria-expanded" => "false"
        "aria-label" => "Toggle navigation"

        span { attr.``class`` "navbar-toggler-icon" }
      }

      div {
        attr.``class`` "collapse navbar-collapse"
        attr.id "navbarNavDropdown"

        ul {
          attr.``class`` "navbar-nav"

          li {
            attr.``class`` "nav-item"

            navLink NavLinkMatch.All {
              attr.``class`` "nav-link"
              attr.href "/"

              "Home"
            }
          }

          li {
            attr.``class`` "nav-item"

            navLink NavLinkMatch.All {
              attr.``class`` "nav-link"
              attr.href (router.Link(Page.Presets))

              "Presets"
            }
          }
        }
      }
    }
  }