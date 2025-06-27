module Domain.Tests.User

open System.Threading.Tasks
open Domain.Repos
open Moq
open Xunit
open Domain.Core
open Domain.Workflows
open FsUnit.Xunit

[<Fact>]
let ``setCurrentPreset updates User.CurrentPresetId`` () =
  let repo = Mock<IUserRepo>()

  repo
    .Setup(_.LoadUser(Mocks.userId))
    .ReturnsAsync(
      { Mocks.user with
          CurrentPresetId = None }
    )

  let expectedUser =
    { Mocks.user with
        CurrentPresetId = Some Mocks.presetId }

  repo.Setup(_.SaveUser(expectedUser)).ReturnsAsync(())

  let sut = User.setCurrentPreset repo.Object

  task {
    do! sut Mocks.userId Mocks.presetId

    repo.VerifyAll()
  }

[<Fact>]
let ``removePreset removes preset`` () =
  let userRepo = Mock<IUserRepo>()

  userRepo
    .Setup(_.LoadUser(Mocks.userId))
    .ReturnsAsync(
      { Mocks.user with
          CurrentPresetId = None }
    )

  let expectedUser =
    { Mocks.user with
        CurrentPresetId = None }

  userRepo.Setup(_.SaveUser(expectedUser)).ReturnsAsync(())

  let removePreset =
    fun presetId ->
      presetId |> should equal Mocks.presetId
      Task.FromResult()

  let presetService = Mock<IPresetService>()

  presetService
    .Setup(_.RemovePreset(Mocks.presetId))
    .ReturnsAsync(())

  let sut = User.removePreset userRepo.Object presetService.Object

  task {
    do! sut Mocks.userId Mocks.presetId

    userRepo.VerifyAll()
  }