[<RequireQualifiedAccess>]
module Bolero.Web.Layout

open Bolero
open Bolero.Html
open Bolero.Web
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Authorization
open Microsoft.AspNetCore.Components.Routing
open Bolero.Web.Models
open Bolero.Web.Router
open Bolero.Web.Util
open Microsoft.AspNetCore.Components.WebAssembly.Authentication

[<RequireQualifiedAccess>]
module internal HeaderLinks =
  let view () = ul {
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
        attr.href "presets"

        "Presets"
      }
    }

    li {
      attr.``class`` "nav-item"

      navLink NavLinkMatch.All {
        attr.``class`` "nav-link"
        attr.href "about"

        "About"
      }
    }
  }

[<RequireQualifiedAccess>]
module internal HeaderLogin =
  let view () = li {
    attr.``class`` "nav-item"

    navLink NavLinkMatch.All {
      attr.``class`` "nav-link"
      attr.href "authentication/login"

      "Login"
    }
  }

[<RequireQualifiedAccess>]
module internal HeaderAuth =
  let view (navManager: NavigationManager) (state: AuthenticationState) = li {
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
          attr.href "profile"

          "Profile"
        }
      }

      li {
        attr.``class`` "dropdown-item"

        button {
          attr.``class`` "nav-link"
          on.click (fun _ -> navManager.NavigateToLogout("authentication/logout"))

          "Logout"
        }

      // navLink NavLinkMatch.All {
      //   attr.href "authentication/logout"
      //
      // }
      }
    }
  }

module internal Header =
  let render navManager = nav {
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

        HeaderLinks.view ()

        ul {
          attr.``class`` "navbar-nav"

          comp<AuthorizeView> {
            attr.fragmentWith "Authorized" (fun (state: AuthenticationState) -> HeaderAuth.view navManager state)
            attr.fragmentWith "NotAuthorized" (fun (_: AuthenticationState) -> HeaderLogin.view ())
          }
        }
      }
    }
  }

[<RequireQualifiedAccess>]
module internal Layout =
  let internal render navManager (body: RenderFragment) = concat {
    Header.render navManager

    div {
      attr.``class`` "container-fluid"

      body
    }
  }

type Layout() =
  inherit LayoutComponentBase()

  [<Inject>]
  member val NavigationManager = Unchecked.defaultof<NavigationManager> with get, set

  override this.BuildRenderTree(builder) =
    base.BuildRenderTree(builder)

    Layout.render this.NavigationManager this.Body
    |> _.Invoke(this, builder, 0)
    |> ignore

// https://otsom.eu.auth0.com/oidc/logout?
// id_token_hint=eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6InRLeFI3ZnhQd1Q2N1Fyb2hmSmI3OCJ9.eyJuaWNrbmFtZSI6ImluZmluaXR1MTMyNyIsIm5hbWUiOiJPbGVnIFRzb21rYWxvdiIsInBpY3R1cmUiOiJodHRwczovL3MuZ3JhdmF0YXIuY29tL2F2YXRhci9jZjBkNGU1ZjcyMTRkYWYzMDgzYzg4MTAyMDc4Y2ZiMD9zPTQ4MCZyPXBnJmQ9aHR0cHMlM0ElMkYlMkZjZG4uYXV0aDAuY29tJTJGYXZhdGFycyUyRm90LnBuZyIsInVwZGF0ZWRfYXQiOiIyMDI1LTEyLTI3VDIwOjEyOjIwLjE2M1oiLCJpc3MiOiJodHRwczovL290c29tLmV1LmF1dGgwLmNvbS8iLCJhdWQiOiJEc29kdDJKVmI2ODZSZ2RMTnA0d0pHVmhMVTRhcWFIUSIsInN1YiI6Im9hdXRoMnxzcG90aWZ5fHNwb3RpZnk6dXNlcjpqaTh2NWVubDc0a3JxZTBjOHQ2ZGtqN2JjIiwiaWF0IjoxNzY2ODY3MTYzLCJleHAiOjE3NjY5MDMxNjMsInNpZCI6ImcwYWZZNWhuWVJQRTN2SXNEdWcwVklLSC1WcXE1Ym5zIn0.FAvJkahg8Vlss_2TptdZKvXAN3xipdrwX4wUlf0fFXHO8BeuiytMQ6Adtaf5mYKJiBGuhvSnyfqRrgDvEL8M1kKzd-AMtF2Q3nnI0e02jqU0YDvBY7K35wz_c9npa-qJgQ_Sy2Vdz5HTpIjh_HEbhElwd2_9N_BaFEY_q264O6xP_qDVjJ1eC_14kcK-VXHk6_BVGJn0bYPRCseW2O70-RA6KjCnyMvISjyT7b3x2Z6GYVEvTxsPwGJ8qQ2jP4YeMhOf01GslKGpsOz4KkzEdnFddNGoN6mS63wVQsQpLWVeyEivi_lIAP1PjVlKIweJpQWj1L8j6cYasouioaHfQg&post_logout_redirect_uri=https%3A%2F%2F127.0.0.1%3A5001%2Fauthentication%2Flogout-callback&state=9cdda4c2313e40cfae83949845b493b9&audience=https%3A%2F%2Fapi.presettie.otsom.pp.ua