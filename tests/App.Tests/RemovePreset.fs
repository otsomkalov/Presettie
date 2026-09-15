namespace App.Tests

open App
open Domain.Core
open Domain.Repos
open Moq
open Tests.Shared
open Xunit

type RemovePreset() =
  let userRepo = Mock<IUserRepo>()
  let presetRepo = Mock<IPresetRepo>()

  let sut: RemovePreset.Handler =
    RemovePreset.handler userRepo.Object presetRepo.Object

  [<Fact>]
  member _.``keeps current Preset untouched if other was removed``() =
    let user =
      { Mocks.user with
          CurrentPresetId = Some Mocks.otherPresetId }

    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(user)
    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)
    presetRepo.Setup(_.RemovePreset(Mocks.presetId)).ReturnsAsync(())

    task {
      let! result =
        sut
          { UserId = Mocks.userId
            PresetId = Mocks.presetId }

      Assert.Equal(Result<unit, Preset.GetPresetError>.Ok(), result)

      userRepo.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``unsets current Preset if successfully removed``() =
    let expectedUser =
      { Mocks.user with
          CurrentPresetId = None }

    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)
    userRepo.Setup(_.SaveUser(expectedUser)).ReturnsAsync(())
    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)
    presetRepo.Setup(_.RemovePreset(Mocks.presetId)).ReturnsAsync(())

    task {
      let! result =
        sut
          { UserId = Mocks.userId
            PresetId = Mocks.presetId }

      Assert.Equal(Result<unit, Preset.GetPresetError>.Ok(), result)

      userRepo.VerifyAll()
      presetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``returns error if Preset not found``() =
    userRepo.Setup(_.LoadUser(Mocks.userId)).ReturnsAsync(Mocks.user)
    presetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(None)

    task {
      let! result =
        sut
          { UserId = Mocks.userId
            PresetId = Mocks.presetId }

      Assert.Equal(Result<unit, Preset.GetPresetError>.Error Preset.GetPresetError.NotFound, result)

      userRepo.VerifyAll()
      presetRepo.VerifyAll()
    }