module Domain.Tests.IncludedPlaylist

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
        IncludedPlaylists = [] }

  mock.Setup(_.SavePreset(expected)).ReturnsAsync(())

  let sut = IncludedPlaylist.remove mock.Object

  task {
    let! preset = sut Mocks.presetId Mocks.includedPlaylist.Id

    mock.VerifyAll()

    preset.IncludedPlaylists
    |> should equal List.empty<IncludedPlaylist>
  }