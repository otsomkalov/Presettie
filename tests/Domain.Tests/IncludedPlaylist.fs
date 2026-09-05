namespace Domain.Tests

open Domain.Core
open Domain.Repos
open Domain.Workflows
open Moq
open Xunit

type IncludedPlaylist() =
  let mockPresetRepo = Mock<IPresetRepo>()

  [<Fact>]
  member _.``remove should remove playlist from preset``() =
    mockPresetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    let expected =
      { Mocks.preset with
          IncludedPlaylists = [] }

    mockPresetRepo.Setup(_.SavePreset(expected)).ReturnsAsync(())

    let sut = IncludedPlaylist.remove mockPresetRepo.Object

    task {
      let! preset = sut Mocks.presetId Mocks.includedPlaylist.Id

      mockPresetRepo.VerifyAll()

      Assert.Equivalent(List.empty<IncludedPlaylist>, preset.IncludedPlaylists)
    }

  [<Fact>]
  member _.``setAll should update included playlist in preset``() =
    // Arrange

    let inputPlaylist =
      { Mocks.includedPlaylist with
          LikedOnly = true }

    let inputPreset =
      { Mocks.preset with
          IncludedPlaylists = [ inputPlaylist ] }

    mockPresetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some inputPreset)

    let expectedPlaylist =
      { Mocks.includedPlaylist with
          LikedOnly = false }

    let expectedPreset =
      { Mocks.preset with
          IncludedPlaylists = [ expectedPlaylist ] }

    mockPresetRepo.Setup(_.SavePreset(expectedPreset)).ReturnsAsync(())

    let sut = IncludedPlaylist.setAll mockPresetRepo.Object

    task {
      // Act
      do! sut Mocks.presetId inputPlaylist.Id

      // Assert
      mockPresetRepo.VerifyAll()
    }

  [<Fact>]
  member _.``setLikedOnly should update included playlist in preset``() =
    // Arrange
    let startingPreset =
      { Mocks.preset with
          IncludedPlaylists = [ Mocks.includedPlaylist ] }

    mockPresetRepo.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some startingPreset)

    let expectedPlaylist =
      { Mocks.includedPlaylist with
          LikedOnly = true }

    let expectedPreset =
      { Mocks.preset with
          IncludedPlaylists = [ expectedPlaylist ] }

    mockPresetRepo.Setup(_.SavePreset(expectedPreset)).ReturnsAsync(())

    let sut = IncludedPlaylist.setLikedOnly mockPresetRepo.Object

    task {
      // Act
      do! sut Mocks.presetId Mocks.includedPlaylist.Id

      // Assert
      mockPresetRepo.VerifyAll()
    }