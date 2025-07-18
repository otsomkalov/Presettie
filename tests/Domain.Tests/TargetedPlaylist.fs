module Domain.Tests.TargetedPlaylist

open Domain.Core
open Domain.Repos
open Domain.Workflows
open Moq
open Xunit
open FsUnit.Xunit

[<Fact>]
let ``appendTracks should disable playlist overwriting`` () =
  let mock = Mock<IPresetRepo>()

  mock
    .Setup(_.LoadPreset(Mocks.presetId))
    .ReturnsAsync(
      Some
        { Mocks.preset with
            TargetedPlaylists =
              [ { Mocks.targetedPlaylist with
                    Overwrite = true } ] }
    )

  let expected =
    { Mocks.preset with
        TargetedPlaylists =
          [ { Mocks.targetedPlaylist with
                Overwrite = false } ] }

  mock.Setup(_.SavePreset(expected)).ReturnsAsync(())


  let sut = TargetedPlaylist.appendTracks mock.Object

  task {
    do! sut Mocks.presetId Mocks.targetedPlaylist.Id

    mock.VerifyAll()
  }

[<Fact>]
let ``overwriteTracks should enable playlist overwriting`` () =
  let mock = Mock<IPresetRepo>()

  mock
    .Setup(_.LoadPreset(Mocks.presetId))
    .ReturnsAsync(
      Some
        { Mocks.preset with
            TargetedPlaylists =
              [ { Mocks.targetedPlaylist with
                    Overwrite = false } ] }
    )

  let expected =
    { Mocks.preset with
        TargetedPlaylists =
          [ { Mocks.targetedPlaylist with
                Overwrite = true } ] }

  mock.Setup(_.SavePreset(expected)).ReturnsAsync(())


  let sut = TargetedPlaylist.overwriteTracks mock.Object

  task {
    do! sut Mocks.presetId Mocks.targetedPlaylist.Id

    mock.VerifyAll()
  }

[<Fact>]
let ``remove should remove playlist from preset`` () =
  let mock = Mock<IPresetRepo>()

  mock.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

  let expected =
    { Mocks.preset with
        TargetedPlaylists = [] }

  mock.Setup(_.SavePreset(expected)).ReturnsAsync(())


  let sut = TargetedPlaylist.remove mock.Object

  task {
    let! preset = sut Mocks.presetId Mocks.targetedPlaylist.Id

    mock.VerifyAll()

    preset.TargetedPlaylists |> should equal List.empty<TargetedPlaylist>
  }