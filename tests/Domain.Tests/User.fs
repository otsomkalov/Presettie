namespace Domain.Tests.User

open Domain.Repos
open Moq
open Tests.Shared
open Xunit
open Domain.Core
open Domain.Workflows

type PatchUser() =
  let repo = Mock<IUserRepo>()

  let sut: IUserService = UserService(repo.Object)

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


    task {
      let! updatedUser =
        sut.PatchUser(Mocks.userId, [ PatchCurrentPreset(Some Mocks.presetId) ])

      Assert.Equal(expectedUser, updatedUser)

      repo.VerifyAll()
    }