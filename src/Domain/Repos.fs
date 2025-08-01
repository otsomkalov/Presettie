module Domain.Repos

open System.Threading.Tasks
open Domain.Core
open otsom.fs.Core

type IIdGenerator =
  abstract GenerateId: unit -> string

type IPresetIdParser =
  abstract ParseId: RawPresetId -> PresetId option

type ILoadPreset =
  abstract LoadPreset: PresetId -> Task<Preset option>

type ISavePreset =
  abstract SavePreset: Preset -> Task<unit>

type IQueueRun =
  abstract QueueRun: UserId * PresetId -> Task<unit>

type IRemovePreset =
  abstract RemovePreset: PresetId -> Task<unit>

type IListUserPresets =
  abstract ListUserPresets: UserId -> Task<SimplePreset list>

type IPresetRepo =
  inherit ILoadPreset
  inherit ISavePreset
  inherit IQueueRun
  inherit IRemovePreset
  inherit IIdGenerator
  inherit IListUserPresets
  inherit IPresetIdParser

type ILoadUser =
  abstract LoadUser: userId: UserId -> Task<User>

type ILoadUserByMusicPlatform =
  abstract LoadUserByMusicPlatform: MusicPlatform.UserId -> Task<User>

type ISaveUser =
  abstract SaveUser: user: User -> Task<unit>

type IUserRepo =
  inherit ILoadUser
  inherit ILoadUserByMusicPlatform
  inherit ISaveUser
  inherit IIdGenerator