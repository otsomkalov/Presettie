﻿module Domain.Tests.User

open System.Threading.Tasks
open Xunit
open otsom.fs.Telegram.Bot.Core
open Domain.Core
open Domain.Workflows
open FsUnit
open otsom.fs.Extensions

let userPresetMock =
  { Id = PresetId("user-preset")
    Name = "user-preset-name" }

let userMock =
  { Id = UserId(1)
    CurrentPresetId = None
    Presets = [ userPresetMock ] }

let loadUser =
  fun userId ->
    userId |> should equal userMock.Id
    userMock |> Task.FromResult

[<Fact>]
let ``setCurrentPreset updates User.CurrentPresetId`` () =
  let expectedUser =
    { userMock with
        CurrentPresetId = Some userPresetMock.Id }

  let updateUser =
    fun user ->
      user |> should equal expectedUser
      Task.FromResult()

  let sut = User.setCurrentPreset loadUser updateUser

  sut userMock.Id userPresetMock.Id

[<Fact>]
let ``removePreset removes preset`` () =
  let loadUser = loadUser >> Task.map(fun u -> {u with CurrentPresetId = Some userPresetMock.Id })

  let expectedUser =
    { userMock with
        Presets = []
        CurrentPresetId = None }

  let updateUser =
    fun user ->
      user |> should equal expectedUser
      Task.FromResult()

  let removePreset =
    fun presetId ->
      presetId |> should equal userPresetMock.Id
      Task.FromResult()

  let sut = User.removePreset loadUser removePreset updateUser

  sut userMock.Id userPresetMock.Id
