﻿module Telegram.Tests.TargetedPlaylist

open System.Threading.Tasks
open Domain.Core
open Domain.Repos
open Domain.Tests
open Telegram.Core
open FsUnit.Xunit
open Xunit
open Domain.Workflows
open Telegram.Workflows
open otsom.fs.Bot

let presetRepo =
  { new ILoadPreset with
      member this.LoadPreset(presetId) =
        presetId |> should equal Mocks.preset.Id

        Mocks.preset |> Task.FromResult }

[<Fact>]
let ``list should send targeted playlists`` () =
  let botMessageCtx =
    { new IEditMessageButtons with
        member this.EditMessageButtons =
          fun text buttons ->
            buttons |> Seq.length |> should equal 2

            Task.FromResult() }

  let sut = TargetedPlaylist.list presetRepo botMessageCtx

  sut Mocks.presetId (Page 0)

[<Fact>]
let ``show should send targeted playlist details`` () =
  let botMessageCtx =
    { new IEditMessageButtons with
        member this.EditMessageButtons =
          fun text buttons ->
            buttons |> Seq.length |> should equal 3

            Task.FromResult() }

  let countPlaylistTracks =
    fun playlistId ->
      playlistId
      |> should equal (Mocks.targetedPlaylist.Id |> WritablePlaylistId.value)

      0 |> Task.FromResult

  let sut = TargetedPlaylist.show botMessageCtx presetRepo countPlaylistTracks

  sut Mocks.presetId Mocks.targetedPlaylist.Id

[<Fact>]
let ``remove should delete playlist and show targeted playlists`` () =
  let removePlaylist =
    fun presetId playlistId ->
      presetId |> should equal Mocks.presetId
      playlistId |> should equal Mocks.targetedPlaylist.Id
      Task.FromResult()

  let showNotification = fun _ -> Task.FromResult()

  let botMessageCtx =
    { new IEditMessageButtons with
        member this.EditMessageButtons =
          fun text buttons ->
            buttons |> Seq.length |> should equal 2

            Task.FromResult() }

  let sut =
    TargetedPlaylist.remove presetRepo botMessageCtx removePlaylist showNotification

  sut Mocks.presetId Mocks.targetedPlaylist.Id
