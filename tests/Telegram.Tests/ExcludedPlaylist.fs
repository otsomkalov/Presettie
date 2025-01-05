﻿module Telegram.Tests.ExcludedPlaylist

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
let ``list should send excluded playlists`` () =
  let botMessageCtx =
    { new IEditMessageButtons with
        member this.EditMessageButtons =
          fun text buttons ->
            buttons |> Seq.length |> should equal 2

            Task.FromResult() }

  let sut = ExcludedPlaylist.list presetRepo botMessageCtx

  sut Mocks.preset.Id (Page 0)

[<Fact>]
let ``show should send excluded playlist details`` () =
  let botMessageCtx =
    { new IEditMessageButtons with
        member this.EditMessageButtons =
          fun text buttons ->
            buttons |> Seq.length |> should equal 3

            Task.FromResult() }

  let countPlaylistTracks =
    fun playlistId ->
      playlistId
      |> should equal (Mocks.excludedPlaylist.Id |> ReadablePlaylistId.value)

      0L |> Task.FromResult

  let sut = ExcludedPlaylist.show botMessageCtx presetRepo countPlaylistTracks

  sut Mocks.presetId Mocks.excludedPlaylist.Id

[<Fact>]
let ``remove should delete playlist and show excluded playlists`` () =
  let removePlaylist =
    fun presetId playlistId ->
      presetId |> should equal Mocks.presetId
      playlistId |> should equal Mocks.excludedPlaylist.Id
      Task.FromResult()

  let botMessageCtx =
    { new IEditMessageButtons with
        member this.EditMessageButtons =
          fun text buttons ->
            buttons |> Seq.length |> should equal 2

            Task.FromResult() }

  let showNotification = fun _ -> Task.FromResult()

  let sut =
    ExcludedPlaylist.remove presetRepo botMessageCtx removePlaylist showNotification

  sut Mocks.presetId Mocks.excludedPlaylist.Id
