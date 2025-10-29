namespace Domain.Tests.Recommenders

open System.Threading.Tasks
open Domain.Tests
open Domain.Workflows
open Moq
open MusicPlatform
open Xunit
open FsUnit.Xunit

type ArtistsAlbumsTests() =
  let musicPlatformMock = Mock<IMusicPlatform>()

  let recommender = ArtistAlbumsRecommender(musicPlatformMock.Object) :> IRecommender

  [<Fact>]
  member this.``returns tracks from seed tracks artists albums``() =
    // Arrange

    musicPlatformMock.Setup(_.ListArtistTracks(Mocks.artist1.Id)).Returns(Task.FromResult([ Mocks.recommendedTrack ]))
    musicPlatformMock.Setup(_.ListArtistTracks(Mocks.artist2.Id)).Returns(Task.FromResult([ Mocks.recommendedTrack ]))

    task {
      // Act
      let! result = recommender.Recommend([ Mocks.includedTrack ])

      // Assert
      result |> should equal [ Mocks.recommendedTrack ]

      musicPlatformMock.VerifyAll()
    }

  [<Fact>]
  member this.``takes only first 50 tracks as a seed``() =
    musicPlatformMock.Setup(_.ListArtistTracks(Mocks.artist1.Id)).Returns(Task.FromResult([ Mocks.recommendedTrack ]))
    musicPlatformMock.Setup(_.ListArtistTracks(Mocks.artist2.Id)).Returns(Task.FromResult([ Mocks.recommendedTrack ]))

    let inputTracks = (List.replicate 50 Mocks.includedTrack) @ [ Mocks.excludedTrack ]

    task {
      // Act
      let! result = recommender.Recommend(inputTracks)

      // Assert
      result |> should equal [ Mocks.recommendedTrack ]

      musicPlatformMock.VerifyAll()
    }

  [<Fact>]
  member this.``loads tracks only for distinct artists``() =
    musicPlatformMock.Setup(_.ListArtistTracks(Mocks.artist1.Id)).Returns(Task.FromResult([ Mocks.recommendedTrack ]))
    musicPlatformMock.Setup(_.ListArtistTracks(Mocks.artist2.Id)).Returns(Task.FromResult([ Mocks.recommendedTrack ]))

    let inputTracks = List.replicate 50 Mocks.includedTrack

    task {
      // Act
      let! result = recommender.Recommend(inputTracks)

      // Assert
      result |> should equal [ Mocks.recommendedTrack ]

      musicPlatformMock.Verify(_.ListArtistTracks(Mocks.artist1.Id), Times.Once())
      musicPlatformMock.Verify(_.ListArtistTracks(Mocks.artist2.Id), Times.Once())
    }