namespace Domain.Tests

open Domain.Core
open Domain.Tests
open Domain.Repos
open Domain.Workflows
open Moq
open Xunit

type ExcludedArtist() =
  let mockPresetRepo = Mock<IPresetRepo>()

  [<Fact>]
  member _.``remove should remove artist from preset``() =
    mockPresetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    let expected =
      { Mocks.preset with
          ExcludedArtists = [] }

    mockPresetRepo.Setup(_.SavePreset(expected)).ReturnsAsync(())

    let sut = ExcludedArtist.remove mockPresetRepo.Object

    task {
      let! preset = sut Mocks.presetId Mocks.artist2.Id

      mockPresetRepo.VerifyAll()

      Assert.Equivalent([], preset.ExcludedArtists)
    }