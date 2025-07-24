module Domain.Tests.Tracks

open Domain.Workflows
open MusicPlatform
open Xunit
open FsUnit.Xunit

[<Fact>]
let ``uniqueByArtists returns tracks which have only unique artists`` () =
  // Arrange

  let tracks: Track list =
    [ Mocks.includedTrack; Mocks.excludedTrack; Mocks.likedTrack ]

  // Act

  let result = Tracks.uniqueByArtists tracks

  // Assert

  result |> should equalSeq [ Mocks.includedTrack; Mocks.likedTrack ]