namespace Domain.Tests

open Domain.Core
open Domain.Tests
open Domain.Repos
open Domain.Workflows
open Moq
open Xunit
open FsUnit.Xunit

type IncludedArtist() =
  let mockPresetRepo = Mock<IPresetRepo>()

  [<Fact>]
  member _.``remove should remove artist from preset``() =
    mockPresetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    let expected =
      { Mocks.preset with
          IncludedArtists = [] }

    mockPresetRepo.Setup(_.SavePreset(expected)).ReturnsAsync(())

    let sut = IncludedArtist.remove mockPresetRepo.Object

    task {
      let! preset = sut Mocks.presetId Mocks.artist1.Id

      mockPresetRepo.VerifyAll()

      preset.IncludedArtists |> should equivalent List.empty<IncludedArtist>
    }
