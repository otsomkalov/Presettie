module Telegram.Workflows

open Domain.Core.PresetSettings
open Domain.Repos
open MusicPlatform
open Domain.Core
open Domain.Workflows
open Microsoft.FSharp.Core
open Resources
open Telegram.Constants
open Telegram.Core
open Telegram.Repos
open otsom.fs.Auth
open otsom.fs.Bot
open otsom.fs.Core
open otsom.fs.Extensions
open System
open otsom.fs.Resources

[<Literal>]
let keyboardColumns = 4

[<Literal>]
let buttonsPerPage = 20

let getPresetMessage (resp: IResourceProvider) =
  fun (preset: Preset) ->
    let presetId = preset.Id.Value

    let likedTracksHandlingText, likedTracksButtonText, likedTracksButtonData =
      match preset.Settings.LikedTracksHandling with
      | LikedTracksHandling.Include ->
        resp[Messages.LikedTracksIncluded], Buttons.ExcludeLikedTracks, $"p|{presetId}|{CallbackQueryConstants.excludeLikedTracks}"
      | LikedTracksHandling.Exclude ->
        resp[Messages.LikedTracksExcluded], Buttons.IgnoreLikedTracks, $"p|{presetId}|{CallbackQueryConstants.ignoreLikedTracks}"
      | LikedTracksHandling.Ignore ->
        resp[Messages.LikedTracksIgnored], Buttons.IncludeLikedTracks, $"p|{presetId}|{CallbackQueryConstants.includeLikedTracks}"

    let recommendationsText, recommendationsButtonText, recommendationsButtonData =
      match preset.Settings.RecommendationsEngine with
      | Some RecommendationsEngine.ArtistAlbums ->
        resp[Messages.ArtistsAlbumsRecommendation],
        Buttons.ReccoBeatsRecommendations,
        sprintf "p|%s|%s" presetId CallbackQueryConstants.reccoBeatsRecommendations
      | Some RecommendationsEngine.ReccoBeats ->
        resp[Messages.ReccoBeatsRecommendation],
        Buttons.DisableRecommendations,
        sprintf "p|%s|%s" presetId CallbackQueryConstants.disableRecommendations
      | None ->
        resp[Messages.RecommendationsDisabled],
        Buttons.ArtistsAlbumsRecommendations,
        sprintf "p|%s|%s" presetId CallbackQueryConstants.artistsAlbumsRecommendations

    let uniqueArtistsText, uniqueArtistsButtonText, uniqueArtistsButtonData =
      match preset.Settings.UniqueArtists with
      | true ->
        resp[Messages.UniqueArtistsEnabled], Buttons.DisableUniqueArtists, sprintf "p|%s|%s" presetId CallbackQueryConstants.disableUniqueArtists
      | false ->
        resp[Messages.UniqueArtistsDisabled], Buttons.EnableUniqueArtists, sprintf "p|%s|%s" presetId CallbackQueryConstants.enableUniqueArtists

    let text =
      String.Format(
        resp[Messages.PresetInfo],
        preset.Name,
        likedTracksHandlingText,
        recommendationsText,
        uniqueArtistsText,
        preset.Settings.Size.Value
      )

    let keyboard = seq {
      MessageButton(likedTracksButtonText, likedTracksButtonData)
      MessageButton(uniqueArtistsButtonText, uniqueArtistsButtonData)
      MessageButton(recommendationsButtonText, recommendationsButtonData)
    }

    (text, keyboard)

let private sendPresetsMessage sendOrEditButtons =
  fun (presets: SimplePreset list) message -> task {
    let keyboardMarkup =
      presets
      |> Seq.map (fun p -> MessageButton(p.Name, $"p|{p.Id.Value}|i"))
      |> Seq.singleton

    do! sendOrEditButtons message keyboardMarkup &|> ignore
  }

let createPlaylistsPage page (playlists: 'a list) playlistToButton (presetId: PresetId) =
  let (Page page) = page
  let remainingPlaylists = playlists[page * buttonsPerPage ..]
  let playlistsForPage = remainingPlaylists[.. buttonsPerPage - 1]

  let playlistsButtons =
    [ 0..keyboardColumns .. playlistsForPage.Length ]
    |> List.map (fun idx -> playlistsForPage |> List.skip idx |> List.takeSafe keyboardColumns)
    |> List.map (Seq.map playlistToButton)

  let backButton = MessageButton("<< Back >>", $"p|{presetId.Value}|i")

  let prevButton =
    if page > 0 then
      Some(MessageButton("<< Prev", $"p|{presetId.Value}|ip|{page - 1}"))
    else
      None

  let nextButton =
    if remainingPlaylists.Length > buttonsPerPage then
      Some(MessageButton("Next >>", $"p|{presetId.Value}|ip|{page + 1}"))
    else
      None

  let serviceButtons =
    match (prevButton, nextButton) with
    | Some pb, Some nb -> [ pb; backButton; nb ]
    | None, Some nb -> [ backButton; nb ]
    | Some pb, None -> [ pb; backButton ]
    | _ -> [ backButton ]

  Seq.append playlistsButtons (serviceButtons |> Seq.ofList |> Seq.singleton)

let getPlaylistButtons (presetId: PresetId) (playlistId: PlaylistId) playlistType specificButtons =
  let buttonDataTemplate =
    sprintf "p|%s|%s|%s|%s" presetId.Value playlistType playlistId.Value

  seq {
    yield specificButtons

    yield seq { MessageButton("Remove", buttonDataTemplate "rm") }

    yield seq { MessageButton("<< Back >>", sprintf "p|%s|%s|%i" presetId.Value playlistType 0) }
  }

let sendLoginMessage (authService: #IInitAuth) (resp: IResourceProvider) (chatCtx: #ISendLink) =
  fun (userId: UserId) ->
    authService.InitAuth(userId.ToAccountId())
    |> Task.bind (fun uri -> chatCtx.SendLink(resp[Messages.LoginToSpotify], Buttons.Login, uri))

[<RequireQualifiedAccess>]
module IncludedPlaylist =
  let list (botMessageCtx: #IEditMessageButtons) =
    let createButtonFromPlaylist (presetId: PresetId) =
      fun (playlist: IncludedPlaylist) -> MessageButton(playlist.Name, sprintf "p|%s|ip|%s|i" presetId.Value playlist.Id.Value.Value)

    fun messageId (preset: Preset) page -> task {
      let createButtonFromPlaylist = createButtonFromPlaylist preset.Id

      let replyMarkup =
        createPlaylistsPage page preset.IncludedPlaylists createButtonFromPlaylist preset.Id

      do! botMessageCtx.EditMessageButtons(messageId, $"Preset *{preset.Name}* has the next included playlists:", replyMarkup)
    }

  let show (resp: IResourceProvider) (botMessageCtx: #IEditMessageButtons) (presetRepo: #ILoadPreset) (mp: #ILoadPlaylist option) =
    fun messageId presetId playlistId -> task {
      let! preset = presetRepo.LoadPreset presetId |> Task.map Option.get

      let includedPlaylist =
        preset.IncludedPlaylists
        |> List.find (fun p -> p.Id = ReadablePlaylistId playlistId)

      let! playlistTracksCount =
        mp
        |> Option.taskMap (fun m -> m.LoadPlaylist playlistId)
        |> Task.map (
          Option.map (
            Result.map (function
              | Writable p -> p.TracksCount
              | Readable r -> r.TracksCount)
            >> Result.defaultValue 0
          )
        )

      let messageText =
        String.Format(resp[Messages.IncludedPlaylistDetails], includedPlaylist.Name, playlistTracksCount, includedPlaylist.LikedOnly)

      let buttonText, buttonDataBuilder =
        if includedPlaylist.LikedOnly then
          ("All", sprintf "p|%s|ip|%s|a")
        else
          ("Only liked", sprintf "p|%s|ip|%s|o")

      let buttonData = buttonDataBuilder presetId.Value playlistId.Value

      let additionalButtons = Seq.singleton (MessageButton(buttonText, buttonData))

      let buttons = getPlaylistButtons presetId playlistId "ip" additionalButtons

      do! botMessageCtx.EditMessageButtons(messageId, messageText, buttons)
    }

[<RequireQualifiedAccess>]
module ExcludedPlaylist =
  let list (botMessageCtx: #IEditMessageButtons) =
    let createButtonFromPlaylist (presetId: PresetId) =
      fun (playlist: ExcludedPlaylist) -> MessageButton(playlist.Name, sprintf "p|%s|ep|%s|i" presetId.Value playlist.Id.Value.Value)

    fun messageId (preset: Preset) page -> task {
      let createButtonFromPlaylist = createButtonFromPlaylist preset.Id

      let replyMarkup =
        createPlaylistsPage page preset.ExcludedPlaylists createButtonFromPlaylist preset.Id

      do! botMessageCtx.EditMessageButtons(messageId, $"Preset *{preset.Name}* has the next excluded playlists:", replyMarkup)
    }

[<RequireQualifiedAccess>]
module TargetedPlaylist =
  let list (botMessageCtx: #IEditMessageButtons) =
    let createButtonFromPlaylist (presetId: PresetId) =
      fun (playlist: TargetedPlaylist) -> MessageButton(playlist.Name, sprintf "p|%s|tp|%s|i" presetId.Value playlist.Id.Value.Value)

    fun messageId (preset: Preset) page -> task {
      let createButtonFromPlaylist = createButtonFromPlaylist preset.Id

      let replyMarkup =
        createPlaylistsPage page preset.TargetedPlaylists createButtonFromPlaylist preset.Id

      do! botMessageCtx.EditMessageButtons(messageId, $"Preset *{preset.Name}* has the next targeted playlists:", replyMarkup)
    }

  let show (resp: IResourceProvider) (botMessageCtx: #IEditMessageButtons) (presetRepo: #ILoadPreset) (mp: #ILoadPlaylist option) =
    fun messageId presetId playlistId -> task {
      let! preset = presetRepo.LoadPreset presetId |> Task.map Option.get

      let targetedPlaylist =
        preset.TargetedPlaylists |> List.find (fun p -> p.Id = playlistId)

      let! playlistTracksCount =
        mp
        |> Option.taskMap (fun m -> m.LoadPlaylist playlistId.Value)
        |> Task.map (
          Option.map (
            Result.map (function
              | Writable p -> p.TracksCount
              | Readable r -> r.TracksCount)
            >> Result.defaultValue 0
          )
        )

      let messageText =
        String.Format(resp[Messages.TargetedPlaylistDetails], targetedPlaylist.Name, playlistTracksCount, targetedPlaylist.Overwrite)

      let buttonText, buttonDataBuilder =
        if targetedPlaylist.Overwrite then
          ("Append", sprintf "p|%s|tp|%s|a")
        else
          ("Overwrite", sprintf "p|%s|tp|%s|o")

      let buttonData = buttonDataBuilder presetId.Value playlistId.Value.Value

      let additionalButtons = Seq.singleton (MessageButton(buttonText, buttonData))

      let buttons = getPlaylistButtons presetId playlistId.Value "tp" additionalButtons

      do! botMessageCtx.EditMessageButtons(messageId, messageText, buttons)
    }

[<RequireQualifiedAccess>]
module Preset =
  let show' showButtons (resp: IResourceProvider) =
    fun preset -> task {
      let text, keyboard = getPresetMessage resp preset

      let keyboardMarkup = seq {
        seq {
          MessageButton("Included playlists", $"p|%s{preset.Id.Value}|ip|0")
          MessageButton("Excluded playlists", $"p|%s{preset.Id.Value}|ep|0")
          MessageButton("Target playlists", $"p|%s{preset.Id.Value}|tp|0")
        }

        keyboard

        seq { MessageButton("Run", $"p|%s{preset.Id.Value}|r") }

        seq { MessageButton("Set as current", $"p|%s{preset.Id.Value}|c") }

        seq { MessageButton("Remove", sprintf "p|%s|rm" preset.Id.Value) }

        seq { MessageButton("<< Back >>", "p") }
      }

      do! showButtons text keyboardMarkup
    }

  let show (presetRepo: #ILoadPreset) (botService: #IEditMessageButtons) (resp: IResourceProvider) =
    let editButtons messageId text buttons =
      botService.EditMessageButtons(messageId, text, buttons)

    fun messageId ->
      presetRepo.LoadPreset
      >> Task.map Option.get
      >> Task.bind (show' (editButtons messageId) resp)

  let run (chatCtx: #ISendMessage & #IEditMessage) (presetService: #IRunPreset) =
    fun presetId ->
      let onSuccess =
        fun (preset: Preset) -> chatCtx.SendMessage($"Preset *{preset.Name}* executed!")

      let onError messageId =
        function
        | Preset.RunError.NoIncludedTracks -> chatCtx.EditMessage(messageId, "Your preset has 0 included tracks")
        | Preset.RunError.NoPotentialTracks ->
          chatCtx.EditMessage(messageId, "Playlists combination in your preset produced 0 potential tracks")
        | Preset.Unauthorized -> chatCtx.EditMessage(messageId, "You are not authorized to music platform!")

      task {
        let! sentMessageId = chatCtx.SendMessage("Running preset...")

        return!
          presetService.RunPreset presetId
          |> TaskResult.taskEither (onSuccess >> Task.ignore) (onError sentMessageId)
      }

  let send (resp: IResourceProvider) (chatCtx: #ISendKeyboard) =
    fun preset ->
      let text, _ = getPresetMessage resp preset

      let keyboard: Keyboard =
        [ [ KeyboardButton(Buttons.RunPreset) ]
          [ KeyboardButton(Buttons.MyPresets) ]
          [ KeyboardButton(Buttons.CreatePreset) ]

          [ KeyboardButton(Buttons.IncludePlaylist)
            KeyboardButton(Buttons.ExcludePlaylist)
            KeyboardButton(Buttons.TargetPlaylist) ]

          [ Buttons.Settings ] ]

      chatCtx.SendKeyboard(text, keyboard) &|> ignore

[<RequireQualifiedAccess>]
module User =
  let private showPresets' sendOrEditButtons (presetRepo: #IListUserPresets) =
    fun userId -> task {
      let! presets = presetRepo.ListUserPresets userId

      return! sendPresetsMessage sendOrEditButtons presets "Your presets"
    }

  let sendPresets (chatCtx: #ISendMessageButtons) presetRepo =
    let sendButtons text buttons =
      chatCtx.SendMessageButtons(text, buttons) &|> ignore

    showPresets' sendButtons presetRepo

  let listPresets (botMessageCtx: #IEditMessageButtons) presetRepo =
    let editButtons messageId text buttons =
      botMessageCtx.EditMessageButtons(messageId, text, buttons)

    fun messageId -> showPresets' (editButtons messageId) presetRepo

  let sendCurrentPreset (resp: IResourceProvider) (userRepo: #ILoadUser) (presetRepo: #ILoadPreset) (chatCtx: #ISendKeyboard) =
    fun userId ->
      userId |> userRepo.LoadUser &|> _.CurrentPresetId
      &|&> (function
      | Some presetId -> task {
          let! preset = presetRepo.LoadPreset presetId |> Task.map Option.get
          return! Preset.send resp chatCtx preset
        }
      | None ->
        let keyboard: Keyboard =
          [ [ KeyboardButton(Buttons.MyPresets) ]
            [ KeyboardButton(Buttons.CreatePreset) ] ]

        chatCtx.SendKeyboard("You did not select current preset", keyboard) &|> ignore)

  let sendCurrentPresetSettings
    (resp: IResourceProvider)
    (userRepo: #ILoadUser)
    (presetRepo: #ILoadPreset & #IListUserPresets)
    (chatCtx: #ISendKeyboard & #ISendMessageButtons)
    =
    fun userId -> task {
      let! user = userRepo.LoadUser userId

      match user.CurrentPresetId with
      | Some presetId ->
        let! preset = presetRepo.LoadPreset presetId |> Task.map Option.get

        let text, _ = getPresetMessage resp preset

        let buttons: Keyboard =
          [| [| KeyboardButton Buttons.SetPresetSize |]
             [| KeyboardButton(Buttons.Back) |] |]

        do! chatCtx.SendKeyboard(text, buttons) &|> ignore

        return ()
      | None ->
        let sendButtons text buttons =
          chatCtx.SendMessageButtons(text, buttons) &|> ignore

        let! presets = presetRepo.ListUserPresets userId

        do! sendPresetsMessage sendButtons presets (resp[Messages.NoCurrentPreset])

        return ()
    }

  let queueCurrentPresetRun (userRepo: #ILoadUser) (chatCtx: #ISendMessage) (presetService: #IQueueRun) =
    let onSuccess (preset: Preset) =
      chatCtx.SendMessage $"Preset *{preset.Name}* run is queued!" |> Task.ignore

    let onError errors =
      let errorsText =
        errors
        |> Seq.map (function
          | Preset.ValidationError.NoIncludedPlaylists -> "No included playlists!"
          | Preset.ValidationError.NoTargetedPlaylists -> "No targeted playlists!")
        |> String.concat Environment.NewLine

      chatCtx.SendMessage errorsText |> Task.ignore

    fun userId ->
      userId |> userRepo.LoadUser &|> (fun u -> u.CurrentPresetId |> Option.get)
      &|&> (fun p -> presetService.QueueRun(userId, p))
      |> TaskResult.taskEither onSuccess onError

[<RequireQualifiedAccess>]
module Chat =
  let create (chatRepo: #ISaveChat) (userService: #ICreateUser) =
    fun chatId -> task {
      let! newUser = userService.CreateUser()

      let newChat: Chat = { Id = chatId; UserId = newUser.Id }
      do! chatRepo.SaveChat newChat

      return newChat
    }

[<RequireQualifiedAccess>]
module Resources =
  let getResourceProvider createResp createDefaultResp : Resources.GetResourceProvider =
    function
    | Some l -> createResp l
    | None -> createDefaultResp ()

type ChatService(chatRepo: IChatRepo, userService: IUserService) =
  interface IChatService with
    member this.CreateChat(chatId) = Chat.create chatRepo userService chatId