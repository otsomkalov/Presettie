module Bolero.Web.Repos

open System.Threading.Tasks
open Domain.Core

type IGetPreset =
  abstract GetPreset': PresetId -> Task<Preset>

type ICreatePreset =
  abstract CreatePreset: string -> Task<PresetId>

type IGetCurrentUser =
  abstract GetCurrentUser: unit -> Task<User>

type ILinkMusicPlatform =
  abstract LinkMusicPlatform: unit -> Task<unit>

type IEnv =
  inherit IGetPreset
  inherit ICreatePreset
  inherit IGetCurrentUser
  inherit ILinkMusicPlatform