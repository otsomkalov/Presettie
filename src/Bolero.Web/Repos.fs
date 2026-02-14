module Bolero.Web.Repos

open System.Threading.Tasks
open Domain.Core
open Runners.Shared

// TODO: Is it possible to share contract with BE?

type IGetPreset =
  abstract GetPreset': PresetId -> Task<Preset>

type IRemovePreset =
  abstract RemovePreset: PresetId -> Task<unit>

type ICreatePreset =
  abstract CreatePreset: string -> Task<PresetId>

type IEnv =
  inherit IListPresets
  inherit IGetPreset
  inherit IRemovePreset
  inherit ICreatePreset