module Domain.Repos

open System.Threading.Tasks
open Domain.Core
open otsom.fs.Core

type ILoadPreset = abstract LoadPreset: PresetId -> Task<Preset>
type ISavePreset = abstract SavePreset: Preset -> Task<unit>
type IQueueRun = abstract QueueRun: UserId * PresetId -> Task<unit>
type IRemovePreset = abstract RemovePreset: PresetId -> Task<unit>

type IPresetRepo =
  inherit ILoadPreset
  inherit ISavePreset
  inherit IQueueRun
  inherit IRemovePreset

type ILoadUser = abstract LoadUser: userId: UserId -> Task<User>
type ISaveUser = abstract SaveUser: user: User -> Task<unit>
type IUserIdGenerator = abstract GenerateUserId: unit -> UserId

type IUserRepo =
  inherit ILoadUser
  inherit ISaveUser
  inherit IUserIdGenerator