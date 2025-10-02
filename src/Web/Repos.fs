module Web.Repos

open System.Threading.Tasks
open Domain.Core

// TODO: Is it possible to share contract with BE?
type IListPresets =
  abstract ListPresets: unit -> Task<SimplePreset list>

type IGetPreset =
  abstract GetPreset': RawPresetId -> Task<Preset>

type IRemovePreset =
  abstract RemovePreset: PresetId -> Task<unit>

type IEnv =
  inherit IListPresets
  inherit IGetPreset
  inherit IRemovePreset