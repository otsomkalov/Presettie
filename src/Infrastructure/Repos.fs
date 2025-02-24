module internal Infrastructure.Repos

open Azure.Storage.Queues
open Domain.Core
open Domain.Repos
open FSharp
open MongoDB.Bson
open MongoDB.Driver
open Database
open MusicPlatform
open MusicPlatform.Spotify
open otsom.fs.Core
open otsom.fs.Extensions
open Infrastructure.Mapping
open System.Threading.Tasks

[<RequireQualifiedAccess>]
module PresetRepo =
  let load (collection: IMongoCollection<Entities.Preset>) =
    fun (PresetId presetId) -> task {
      let presetsFilter = Builders<Entities.Preset>.Filter.Eq(_.Id, presetId)

      let! dbPreset = collection.Find(presetsFilter).SingleOrDefaultAsync()

      return dbPreset |> Preset.fromDb
    }

  let save (collection: IMongoCollection<Entities.Preset>) =
    fun preset -> task {
      let dbPreset = preset |> Preset.toDb

      let presetsFilter =
        Builders<Entities.Preset>.Filter.Eq(_.Id, preset.Id.Value)

      return!
        collection.ReplaceOneAsync(presetsFilter, dbPreset, ReplaceOptions(IsUpsert = true))
        &|> ignore
    }

  let remove (collection: IMongoCollection<Entities.Preset>) =
    fun (PresetId presetId) ->
      let presetsFilter = Builders<Entities.Preset>.Filter.Eq(_.Id, presetId)

      collection.DeleteOneAsync(presetsFilter) |> Task.ignore

  let private listPlaylistsTracks (listTracks: PlaylistId -> Task<Track list>) =
    List.map listTracks >> Task.WhenAll >> Task.map List.concat

  let listExcludedTracks logger listTracks =
    let listTracks = listPlaylistsTracks listTracks

    fun playlists -> task {
      let! playlistsTracks = playlists |> List.map _.Id |> listTracks

      Logf.logfi logger "Preset has %i{ExcludedTracksCount} excluded tracks" playlistsTracks.Length

      return playlistsTracks
    }

[<RequireQualifiedAccess>]
module UserRepo =
  let load (collection: IMongoCollection<Entities.User>) =
    fun (UserId userId) ->
      let usersFilter = Builders<Entities.User>.Filter.Eq(_.Id, ObjectId userId)

      collection.Find(usersFilter).SingleOrDefaultAsync() |> Task.map User.fromDb

  let save (collection: IMongoCollection<Entities.User>) =
    fun (user: User) ->
      let usersFilter = Builders<Entities.User>.Filter.Eq(_.Id, ObjectId user.Id.Value)

      let dbUser = user |> User.toDb

      collection.ReplaceOneAsync(usersFilter, dbUser, ReplaceOptions(IsUpsert = true))
      |> Task.map ignore

  let create (db: IMongoDatabase) =
    fun user ->
      let collection = db.GetCollection "users"
      let dbUser = user |> User.toDb

      task { do! collection.InsertOneAsync(dbUser) }

type PresetRepo(db: IMongoDatabase, queueClient: QueueClient) =
  let collection = db.GetCollection<Entities.Preset> "presets"

  interface IPresetRepo with
    member this.LoadPreset(presetId) = PresetRepo.load collection presetId
    member this.SavePreset(preset) = PresetRepo.save collection preset

    member this.QueueRun(userId, presetId) =
      {| UserId = userId.Value
         PresetId = presetId.Value |}
      |> JSON.serialize
      |> queueClient.SendMessageAsync
      |> Task.map ignore

    member this.RemovePreset(presetId) = PresetRepo.remove collection presetId

type UserRepo(db: IMongoDatabase) =
  let collection = db.GetCollection<Entities.User> "users"

  interface IUserRepo with
    member this.LoadUser(userId) = UserRepo.load collection userId
    member this.SaveUser(user) = UserRepo.save collection user

    member this.GenerateUserId() =
      ObjectId.GenerateNewId().ToString() |> UserId