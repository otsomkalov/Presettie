module Bolero.Web.Repos

open System.Threading.Tasks
open Domain.Core

type IGetPreset =
  abstract GetPreset': PresetId -> Task<Preset>

type IRemovePreset =
  abstract RemovePreset: PresetId -> Task<unit>

type ICreatePreset =
  abstract CreatePreset: string -> Task<PresetId>

type IGetCurrentUser =
  abstract GetCurrentUser: unit -> Task<User>

type ILinkMusicPlatform =
  abstract LinkMusicPlatform: unit -> Task<unit>

type IEnv =
  inherit IGetPreset
  inherit IRemovePreset
  inherit ICreatePreset
  inherit IGetCurrentUser
  inherit ILinkMusicPlatform