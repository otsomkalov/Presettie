module Domain.Tests.ExcludedPlaylist

open Domain.Core
open Domain.Repos
open Domain.Workflows
open Moq
open Xunit
open FsUnit.Xunit

[<Fact>]
let ``remove should remove playlist from preset`` () =
  let mock = Mock<IPresetRepo>()

  mock.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

  let expected =
    { Mocks.preset with
        ExcludedPlaylists = [] }

  mock.Setup(_.SavePreset(expected)).ReturnsAsync(())

  let sut = ExcludedPlaylist.remove mock.Object

  task {
    let! preset = sut Mocks.presetId Mocks.excludedPlaylist.Id

    mock.VerifyAll()

    preset.ExcludedPlaylists
    |> should equal List.empty<ExcludedPlaylist>
  }