namespace Domain.Tests

open Domain.Core
open Domain.Repos
open Domain.Workflows
open Moq
open Xunit
open FsUnit.Xunit

type TargetedPlaylist() =
  let mock = Mock<IPresetRepo>()

  [<Fact>]
  member this.``appendTracks should disable playlist overwriting``() =
    mock
      .Setup(_.LoadPreset(Mocks.presetId))
      .ReturnsAsync(
        Some
          { Mocks.preset with
              TargetedPlaylists =
                Set.singleton
                  { Mocks.targetedPlaylist with
                      Overwrite = true } }
      )

    let expected =
      { Mocks.preset with
          TargetedPlaylists =
            Set.singleton
              { Mocks.targetedPlaylist with
                  Overwrite = false } }

    mock.Setup(_.SavePreset(expected)).ReturnsAsync(())


    let sut = TargetedPlaylist.appendTracks mock.Object

    task {
      do! sut Mocks.presetId Mocks.targetedPlaylist.Id

      mock.VerifyAll()
    }

  [<Fact>]
  member this.``overwriteTracks should enable playlist overwriting``() =
    mock
      .Setup(_.LoadPreset(Mocks.presetId))
      .ReturnsAsync(
        Some
          { Mocks.preset with
              TargetedPlaylists =
                Set.singleton
                  { Mocks.targetedPlaylist with
                      Overwrite = false } }
      )

    let expected =
      { Mocks.preset with
          TargetedPlaylists =
            Set.singleton
              { Mocks.targetedPlaylist with
                  Overwrite = true } }

    mock.Setup(_.SavePreset(expected)).ReturnsAsync(())


    let sut = TargetedPlaylist.overwriteTracks mock.Object

    task {
      do! sut Mocks.presetId Mocks.targetedPlaylist.Id

      mock.VerifyAll()
    }

  [<Fact>]
  member this.``remove should remove playlist from preset``() =
    mock.Setup(_.LoadPreset(Mocks.presetId)).ReturnsAsync(Some Mocks.preset)

    let expected =
      { Mocks.preset with
          TargetedPlaylists = Set.empty }

    mock.Setup(_.SavePreset(expected)).ReturnsAsync(())

    let sut = TargetedPlaylist.remove mock.Object

    task {
      let! result = sut Mocks.presetId Mocks.targetedPlaylist.Id

      result
      |> should equal (Result<_, Preset.RemoveTargetedPlaylistError>.Ok(expected))

      mock.VerifyAll()
    }