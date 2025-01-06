module Telegram.Tests.PresetSettings

open System.Threading.Tasks
open Domain.Repos
open Domain.Tests
open FsUnit.Xunit
open Xunit
open Telegram.Workflows
open otsom.fs.Bot

let loadPreset =
  { new ILoadPreset with
      member this.LoadPreset presetId =
        presetId |> should equal Mocks.presetId

        Mocks.preset |> Task.FromResult }

let botMessageCtx =
  { new IEditMessageButtons with
      member this.EditMessageButtons =
        fun text buttons ->
          buttons |> Seq.length |> should equal 6

          Task.FromResult() }

[<Fact>]
let ``enableUniqueArtists should update preset and show updated`` () =
  let disableUniqueArtists =
    fun presetId ->
      presetId |> should equal Mocks.presetId

      Task.FromResult()

  let showNotification = fun _ -> Task.FromResult()

  let sut =
    PresetSettings.enableUniqueArtists loadPreset botMessageCtx disableUniqueArtists showNotification

  sut Mocks.presetId

[<Fact>]
let ``disableUniqueArtists should update preset and show updated`` () =
  let disableUniqueArtists =
    fun presetId ->
      presetId |> should equal Mocks.presetId

      Task.FromResult()

  let showNotification = fun _ -> Task.FromResult()

  let showPresetInfo =
    fun presetId ->
      presetId |> should equal Mocks.presetId

      Task.FromResult()

  let sut =
    PresetSettings.disableUniqueArtists loadPreset botMessageCtx disableUniqueArtists showNotification

  sut Mocks.presetId