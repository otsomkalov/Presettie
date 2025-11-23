namespace Domain.Tests

open Domain.Core
open Domain.Tests
open Domain.Repos
open Domain.Workflows
open Moq
open Xunit
open FsUnit.Xunit

type ExcludedPlaylist() =
  let mockPresetRepo = Mock<IPresetRepo>()

  [<Fact>]
  member _.``remove should remove playlist from preset``() =
    mockPresetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    let expected =
      { Mocks.preset with
          ExcludedPlaylists = Set.empty }

    mockPresetRepo.Setup(_.SavePreset(expected)).ReturnsAsync(())

    let sut = ExcludedPlaylist.remove mockPresetRepo.Object

    task {
      let! result = sut Mocks.presetId Mocks.excludedPlaylist.Id

      result |> should equal (Result<Preset, Preset.RemoveExcludedPlaylistError>.Ok expected)

      mockPresetRepo.VerifyAll()
    }