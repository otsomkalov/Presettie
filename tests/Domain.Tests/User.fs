namespace Domain.Tests.User

open Domain.Repos
open Domain.Tests
open Moq
open Xunit
open Domain.Core
open Domain.Workflows
open FsUnit.Xunit

type SetCurrentPreset() =
  let repo = Mock<IUserRepo>()

  [<Fact>]
  member _.``updates User.CurrentPresetId``() =
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

type RemovePreset() =
  let userRepo = Mock<IUserRepo>()
  let presetService = Mock<IPresetService>()

  let sut = UserService(userRepo.Object, presetService.Object) :> IRemoveUserPreset

  [<Fact>]
  member _.``keeps current Preset untouched if other was removed``() =
    let expectedUser =
      { Mocks.user with
          CurrentPresetId = Some Mocks.otherPresetId }

    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(expectedUser)

    presetService.Setup(_.RemovePreset(Mocks.userId, Mocks.rawPresetId)).ReturnsAsync(Ok Mocks.preset)

    task {
      let! result = sut.RemoveUserPreset(Mocks.userId, Mocks.rawPresetId)

      result |> should equal (Result<unit, Preset.GetPresetError>.Ok())

      userRepo.VerifyAll()
      presetService.VerifyAll()
    }

  [<Fact>]
  member _.``unsets current Preset if successfully removed``() =
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)

    let expectedUser =
      { Mocks.user with
          CurrentPresetId = None }

    userRepo.Setup(_.SaveUser(expectedUser)).ReturnsAsync(())

    presetService.Setup(_.RemovePreset(Mocks.userId, Mocks.rawPresetId)).ReturnsAsync(Ok Mocks.preset)

    task {
      let! result = sut.RemoveUserPreset(Mocks.userId, Mocks.rawPresetId)

      result |> should equal (Result<unit, Preset.GetPresetError>.Ok())

      userRepo.VerifyAll()
      presetService.VerifyAll()
    }

  [<Fact>]
  member _.``returns error if Preset not found``() =
    presetService.Setup(_.RemovePreset(Mocks.userId, Mocks.rawPresetId)).ReturnsAsync(Error Preset.GetPresetError.NotFound)

    task {
      let! result = sut.RemoveUserPreset(Mocks.userId, Mocks.rawPresetId)

      result |> should equal (Result<unit, _>.Error Preset.GetPresetError.NotFound)

      userRepo.VerifyAll()
      presetService.VerifyAll()
    }