module Web.Repos

open System.Threading.Tasks
open Domain.Core

// TODO: Is it possible to share contract with BE?
type IListPresets =
  abstract ListPresets: unit -> Task<SimplePreset list>

type IEnv =
  inherit IListPresets