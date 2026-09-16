namespace Domain.Tests.User

open Domain.Repos
open Domain.Tests
open Moq
open Tests.Shared
open Xunit
open Domain.Core
open Domain.Workflows

type SetCurrentPreset() =
  let repo = Mock<IUserRepo>()

  [<Fact>]
  member _.``updates User.CurrentPresetId``() =
    repo
      .Setup(_.LoadUser(Mocks.userId))
      .ReturnsAsync(
        { Mocks.user with
            CurrentPresetId = None
        }
      )

    let expectedUser =
      { Mocks.user with
          CurrentPresetId = Some Mocks.presetId
      }

    repo.Setup(_.SaveUser(expectedUser)).ReturnsAsync(())

    let sut = User.setCurrentPreset repo.Object

    task {
      do! sut Mocks.userId Mocks.presetId

      repo.VerifyAll()
    }