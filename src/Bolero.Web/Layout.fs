[<RequireQualifiedAccess>]
module Bolero.Web.Layout

open Bolero
open Bolero.Html
open Microsoft.AspNetCore.Components.Authorization
open Microsoft.AspNetCore.Components.Routing
open Bolero.Web.Models
open Bolero.Web.Router
open Bolero.Web.Util

[<RequireQualifiedAccess>]
module internal HeaderLinks =
  let view dispatch = ul {
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
        attr.href (router.Link(Page.Presets({ Model = { Presets = AsyncOp.Loading } })))

        "Presets"
      }
    }

    li {
      attr.``class`` "nav-item"

      navLink NavLinkMatch.All {
        attr.``class`` "nav-link"
        attr.href (router.Link(Page.About))

        "About"
      }
    }
  }

[<RequireQualifiedAccess>]
module internal HeaderLogin =
  let view dispatch = li {
    attr.``class`` "nav-item"

    navLink NavLinkMatch.All {
      attr.``class`` "nav-link"
      attr.href (router.Link(Page.Auth "login"))

      "Login"
    }
  }

[<RequireQualifiedAccess>]
module internal HeaderAuth =
  let view (state: AuthenticationState) dispatch = li {
    attr.``class`` "nav-item dropdown"

    navLink NavLinkMatch.All {
      attr.``class`` "nav-link dropdown-toggle"
      attr.href "#"
      "role" => "button"
      "data-bs-toggle" => "dropdown"
      "aria-expanded" => "false"

      string state.User.Identity.Name
    }

    ul {
      attr.``class`` "dropdown-menu"

      li {
        attr.``class`` "dropdown-item"

        navLink NavLinkMatch.All {
          attr.``class`` "nav-link"

          attr.href (router.Link(Page.Profile))

          "Profile"
        }
      }

      li {
        attr.``class`` "dropdown-item"

        navLink NavLinkMatch.All {
          attr.``class`` "nav-link"

          attr.href (router.Link(Page.Auth "logout"))

          "Logout"
        }
      }
    }
  }

module Header =
  let view (authState: AuthenticationState option) dispatch = nav {
    attr.``class`` "navbar bg-primary navbar-expand-lg mb-2"
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
        attr.``class`` "collapse navbar-collapse justify-content-between"
        attr.id "navbarNavDropdown"

        HeaderLinks.view dispatch

        ul {
          attr.``class`` "navbar-nav"

          match authState with
          | Some state when state.User.Identity.IsAuthenticated -> HeaderAuth.view state dispatch
          | Some state when not state.User.Identity.IsAuthenticated -> HeaderLogin.view dispatch
          | _ -> empty ()
        }
      }
    }
  }