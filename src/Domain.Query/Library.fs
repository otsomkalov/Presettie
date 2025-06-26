namespace Domain.Query

open System.Threading.Tasks
open otsom.fs.Core

type SimplePreset = { Id: string; Name: string }

type IListUserPresets =
  abstract ListUserPresets: UserId -> Task<SimplePreset list>

type IPresetReadRepo =
  inherit IListUserPresets
