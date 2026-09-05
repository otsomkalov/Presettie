namespace Domain.Tests.Recommenders

open Domain.Tests
open Domain.Workflows
open Moq
open MusicPlatform
open Xunit
open FSharp.Control

type ArtistsAlbumsTests() =
  let musicPlatformMock = Mock<IMusicPlatform>()

  let recommender = ArtistAlbumsRecommender(musicPlatformMock.Object) :> IRecommender

  [<Fact>]
  member this.``returns tracks from seed tracks artists albums``() =
    // Arrange

    musicPlatformMock.Setup(_.ListArtistTracks(Mocks.artist1.Id)).Returns(TaskSeq.ofList [ Mocks.recommendedTrack ])
    musicPlatformMock.Setup(_.ListArtistTracks(Mocks.artist2.Id)).Returns(TaskSeq.ofList [ Mocks.recommendedTrack ])

    task {
      // Act
      let! result = recommender.Recommend([ Mocks.includedTrack ])

      // Assert
      Assert.Equal<Track>([ Mocks.recommendedTrack ], result)

      musicPlatformMock.VerifyAll()
    }

  [<Fact>]
  member this.``takes only first 50 tracks as a seed``() =
    musicPlatformMock.Setup(_.ListArtistTracks(Mocks.artist1.Id)).Returns(TaskSeq.ofList [ Mocks.recommendedTrack ])
    musicPlatformMock.Setup(_.ListArtistTracks(Mocks.artist2.Id)).Returns(TaskSeq.ofList [ Mocks.recommendedTrack ])

    let inputTracks = (List.replicate 50 Mocks.includedTrack) @ [ Mocks.excludedTrack ]

    task {
      // Act
      let! result = recommender.Recommend(inputTracks)

      // Assert
      Assert.Equal<Track>([ Mocks.recommendedTrack ], result)

      musicPlatformMock.VerifyAll()
    }

  [<Fact>]
  member this.``loads tracks only for distinct artists``() =
    musicPlatformMock.Setup(_.ListArtistTracks(Mocks.artist1.Id)).Returns(TaskSeq.ofList [ Mocks.recommendedTrack ])
    musicPlatformMock.Setup(_.ListArtistTracks(Mocks.artist2.Id)).Returns(TaskSeq.ofList [ Mocks.recommendedTrack ])

    let inputTracks = List.replicate 50 Mocks.includedTrack

    task {
      // Act
      let! result = recommender.Recommend(inputTracks)

      // Assert
      Assert.Equal<Track>([ Mocks.recommendedTrack ], result)

      musicPlatformMock.Verify(_.ListArtistTracks(Mocks.artist1.Id), Times.Once())
      musicPlatformMock.Verify(_.ListArtistTracks(Mocks.artist2.Id), Times.Once())
    }