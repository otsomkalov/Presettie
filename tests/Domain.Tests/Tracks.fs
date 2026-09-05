namespace Domain.Tests

open Domain.Workflows
open MusicPlatform
open Xunit

type Tracks() =
  [<Fact>]
  member _.``uniqueByArtists returns tracks which have only unique artists``() =
    // Arrange
    let tracks: Track list =
      [ Mocks.includedTrack; Mocks.excludedTrack; Mocks.likedTrack ]

    // Act
    let result = Tracks.uniqueByArtists tracks

    // Assert
    Assert.Equal<Track>([ Mocks.includedTrack; Mocks.likedTrack ], result)