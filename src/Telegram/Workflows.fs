module Telegram.Workflows

open Domain.Core.PresetSettings
open Domain.Repos
open Microsoft.Extensions.Options
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
        resp[Messages.LikedTracksIncluded], resp[Buttons.ExcludeLikedTracks], $"p|{presetId}|{CallbackQueryConstants.excludeLikedTracks}"
      | LikedTracksHandling.Exclude ->
        resp[Messages.LikedTracksExcluded], resp[Buttons.IgnoreLikedTracks], $"p|{presetId}|{CallbackQueryConstants.ignoreLikedTracks}"
      | LikedTracksHandling.Ignore ->
        resp[Messages.LikedTracksIgnored], resp[Buttons.IncludeLikedTracks], $"p|{presetId}|{CallbackQueryConstants.includeLikedTracks}"

    let recommendationsText, recommendationsButtonText, recommendationsButtonData =
      match preset.Settings.RecommendationsEngine with
      | Some RecommendationsEngine.ArtistAlbums ->
        resp[Messages.ArtistsAlbumsRecommendation],
        resp[Buttons.ReccoBeatsRecommendations],
        sprintf "p|%s|%s" presetId CallbackQueryConstants.reccoBeatsRecommendations
      | Some RecommendationsEngine.ReccoBeats ->
        resp[Messages.ReccoBeatsRecommendation],
        resp[Buttons.DisableRecommendations],
        sprintf "p|%s|%s" presetId CallbackQueryConstants.disableRecommendations
      | None ->
        resp[Messages.RecommendationsDisabled],
        resp[Buttons.ArtistsAlbumsRecommendations],
        sprintf "p|%s|%s" presetId CallbackQueryConstants.artistsAlbumsRecommendations

    let uniqueArtistsText, uniqueArtistsButtonText, uniqueArtistsButtonData =
      match preset.Settings.UniqueArtists with
      | true ->
        resp[Messages.UniqueArtistsEnabled],
        resp[Buttons.DisableUniqueArtists],
        sprintf "p|%s|%s" presetId CallbackQueryConstants.disableUniqueArtists
      | false ->
        resp[Messages.UniqueArtistsDisabled],
        resp[Buttons.EnableUniqueArtists],
        sprintf "p|%s|%s" presetId CallbackQueryConstants.enableUniqueArtists

    let text =
      resp[Messages.PresetInfo,
           [| preset.Name
              likedTracksHandlingText
              recommendationsText
              uniqueArtistsText
              preset.Settings.Size.Value |]]

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

let createPlaylistsPage (resp: IResourceProvider) =
  fun page (playlists: 'a list) playlistToButton (presetId: PresetId) ->
    let (Page page) = page
    let remainingPlaylists = playlists[page * buttonsPerPage ..]
    let playlistsForPage = remainingPlaylists[.. buttonsPerPage - 1]

    let playlistsButtons =
      [ 0..keyboardColumns .. playlistsForPage.Length ]
      |> List.map (fun idx -> playlistsForPage |> List.skip idx |> List.takeSafe keyboardColumns)
      |> List.map (Seq.map playlistToButton)

    let backButton = MessageButton(resp[Buttons.Back], $"p|{presetId.Value}|i")

    let prevButton =
      if page > 0 then
        Some(MessageButton(resp[Buttons.PrevPage], $"p|{presetId.Value}|ip|{page - 1}"))
      else
        None

    let nextButton =
      if remainingPlaylists.Length > buttonsPerPage then
        Some(MessageButton(resp[Buttons.NextPage], $"p|{presetId.Value}|ip|{page + 1}"))
      else
        None

    let serviceButtons =
      match (prevButton, nextButton) with
      | Some pb, Some nb -> [ pb; backButton; nb ]
      | None, Some nb -> [ backButton; nb ]
      | Some pb, None -> [ pb; backButton ]
      | _ -> [ backButton ]

    Seq.append playlistsButtons (serviceButtons |> Seq.ofList |> Seq.singleton)

let getPlaylistButtons (resp: IResourceProvider) =
  fun (presetId: PresetId) (playlistId: PlaylistId) playlistType specificButtons ->
    let buttonDataTemplate =
      sprintf "p|%s|%s|%s|%s" presetId.Value playlistType playlistId.Value

    seq {
      yield specificButtons

      yield seq { MessageButton(resp[Buttons.Remove], buttonDataTemplate "rm") }

      yield seq { MessageButton(resp[Buttons.Back], sprintf "p|%s|%s|%i" presetId.Value playlistType 0) }
    }

let sendLoginMessage (authService: #IInitAuth) (resp: IResourceProvider) (chatCtx: #ISendLink) =
  fun (userId: UserId) ->
    authService.InitAuth(userId.ToAccountId())
    |> Task.bind (fun uri -> chatCtx.SendLink(resp[Messages.LoginToSpotify], resp[Buttons.Login], uri))

[<RequireQualifiedAccess>]
module IncludedPlaylist =
  let list resp (botMessageCtx: #IEditMessageButtons) =
    let createButtonFromPlaylist (presetId: PresetId) =
      fun (playlist: IncludedPlaylist) -> MessageButton(playlist.Name, sprintf "p|%s|ip|%s|i" presetId.Value playlist.Id.Value.Value)

    fun messageId (preset: Preset) page -> task {
      let createButtonFromPlaylist = createButtonFromPlaylist preset.Id

      let replyMarkup =
        createPlaylistsPage resp page preset.IncludedPlaylists createButtonFromPlaylist preset.Id

      do! botMessageCtx.EditMessageButtons(messageId, resp[Messages.IncludedPlaylists, [| preset.Name |]], replyMarkup)
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
        resp[Messages.IncludedPlaylistDetails, [| includedPlaylist.Name; playlistTracksCount; includedPlaylist.LikedOnly |]]

      let buttonText, buttonDataBuilder =
        if includedPlaylist.LikedOnly then
          ("All", sprintf "p|%s|ip|%s|a")
        else
          ("Only liked", sprintf "p|%s|ip|%s|o")

      let buttonData = buttonDataBuilder presetId.Value playlistId.Value

      let additionalButtons = Seq.singleton (MessageButton(buttonText, buttonData))

      let buttons = getPlaylistButtons resp presetId playlistId "ip" additionalButtons

      do! botMessageCtx.EditMessageButtons(messageId, messageText, buttons)
    }

[<RequireQualifiedAccess>]
module ExcludedPlaylist =
  let list resp (botMessageCtx: #IEditMessageButtons) =
    let createButtonFromPlaylist (presetId: PresetId) =
      fun (playlist: ExcludedPlaylist) -> MessageButton(playlist.Name, sprintf "p|%s|ep|%s|i" presetId.Value playlist.Id.Value.Value)

    fun messageId (preset: Preset) page -> task {
      let createButtonFromPlaylist = createButtonFromPlaylist preset.Id

      let replyMarkup =
        createPlaylistsPage resp page preset.ExcludedPlaylists createButtonFromPlaylist preset.Id

      do! botMessageCtx.EditMessageButtons(messageId, resp[Messages.ExcludedPlaylists, [| preset.Name |]], replyMarkup)
    }

[<RequireQualifiedAccess>]
module TargetedPlaylist =
  let list resp (botMessageCtx: #IEditMessageButtons) =
    let createButtonFromPlaylist (presetId: PresetId) =
      fun (playlist: TargetedPlaylist) -> MessageButton(playlist.Name, sprintf "p|%s|tp|%s|i" presetId.Value playlist.Id.Value.Value)

    fun messageId (preset: Preset) page -> task {
      let createButtonFromPlaylist = createButtonFromPlaylist preset.Id

      let replyMarkup =
        createPlaylistsPage resp page preset.TargetedPlaylists createButtonFromPlaylist preset.Id

      do! botMessageCtx.EditMessageButtons(messageId, resp[Messages.TargetedPlaylists, [| preset.Name |]], replyMarkup)
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
        resp[Messages.TargetedPlaylistDetails, [| targetedPlaylist.Name; playlistTracksCount; targetedPlaylist.Overwrite |]]

      let buttonText, buttonDataBuilder =
        if targetedPlaylist.Overwrite then
          (resp[Buttons.Append], sprintf "p|%s|tp|%s|a")
        else
          (resp[Buttons.Overwrite], sprintf "p|%s|tp|%s|o")

      let buttonData = buttonDataBuilder presetId.Value playlistId.Value.Value

      let additionalButtons = Seq.singleton (MessageButton(buttonText, buttonData))

      let buttons =
        getPlaylistButtons resp presetId playlistId.Value "tp" additionalButtons

      do! botMessageCtx.EditMessageButtons(messageId, messageText, buttons)
    }

[<RequireQualifiedAccess>]
module Preset =
  let show' showButtons (resp: IResourceProvider) =
    fun preset -> task {
      let text, keyboard = getPresetMessage resp preset

      let keyboardMarkup = seq {
        seq {
          MessageButton(resp[Buttons.IncludedPlaylists], $"p|%s{preset.Id.Value}|ip|0")
          MessageButton(resp[Buttons.ExcludedPlaylists], $"p|%s{preset.Id.Value}|ep|0")
          MessageButton(resp[Buttons.TargetedPlaylists], $"p|%s{preset.Id.Value}|tp|0")
        }

        keyboard

        seq { MessageButton(resp[Buttons.RunPreset], $"p|%s{preset.Id.Value}|r") }

        seq { MessageButton(resp[Buttons.SetCurrentPreset], $"p|%s{preset.Id.Value}|c") }

        seq { MessageButton(resp[Buttons.Remove], sprintf "p|%s|rm" preset.Id.Value) }

        seq { MessageButton(resp[Buttons.Back], "p") }
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

  let run (resp: IResourceProvider) (chatCtx: #ISendMessage & #IEditMessage) (presetService: #IRunPreset) =
    fun presetId ->
      let onSuccess =
        fun (preset: Preset) -> chatCtx.SendMessage resp[Messages.PresetExecuted, [| preset.Name |]]

      let onError messageId =
        function
        | Preset.RunError.NoIncludedTracks -> chatCtx.EditMessage(messageId, resp[Messages.NoIncludedTracks])
        | Preset.RunError.NoPotentialTracks -> chatCtx.EditMessage(messageId, resp[Messages.NoPotentialTracks])
        | Preset.Unauthorized -> chatCtx.EditMessage(messageId, resp[Messages.NotAuthorized])

      task {
        let! sentMessageId = chatCtx.SendMessage(resp[Messages.RunningPreset])

        return!
          presetService.RunPreset presetId
          |> TaskResult.taskEither (onSuccess >> Task.ignore) (onError sentMessageId)
      }

  let send (resp: IResourceProvider) (chatCtx: #ISendKeyboard) =
    fun preset ->
      let text, _ = getPresetMessage resp preset

      let keyboard: Keyboard =
        [ [ KeyboardButton(resp[Buttons.RunPreset]) ]
          [ KeyboardButton(resp[Buttons.MyPresets]) ]
          [ KeyboardButton(resp[Buttons.CreatePreset]) ]

          [ KeyboardButton(resp[Buttons.IncludePlaylist])
            KeyboardButton(resp[Buttons.ExcludePlaylist])
            KeyboardButton(resp[Buttons.TargetPlaylist]) ]

          [ resp[Buttons.Settings] ] ]

      chatCtx.SendKeyboard(text, keyboard) &|> ignore

[<RequireQualifiedAccess>]
module User =
  let private showPresets' (resp: IResourceProvider) sendOrEditButtons (presetRepo: #IListUserPresets) =
    fun userId -> task {
      let! presets = presetRepo.ListUserPresets userId

      return! sendPresetsMessage sendOrEditButtons presets resp[Messages.YourPresets]
    }

  let sendPresets resp (chatCtx: #ISendMessageButtons) presetRepo =
    let sendButtons text buttons =
      chatCtx.SendMessageButtons(text, buttons) &|> ignore

    showPresets' resp sendButtons presetRepo

  let listPresets resp (botMessageCtx: #IEditMessageButtons) presetRepo =
    let editButtons messageId text buttons =
      botMessageCtx.EditMessageButtons(messageId, text, buttons)

    fun messageId -> showPresets' resp (editButtons messageId) presetRepo

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
          [ [ KeyboardButton(resp[Buttons.MyPresets]) ]
            [ KeyboardButton(resp[Buttons.CreatePreset]) ] ]

        chatCtx.SendKeyboard(resp[Messages.NoCurrentPreset], keyboard) &|> ignore)

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
          [| [| KeyboardButton(resp[Buttons.SetPresetSize]) |]
             [| KeyboardButton(resp[Buttons.Back]) |] |]

        do! chatCtx.SendKeyboard(text, buttons) &|> ignore

        return ()
      | None ->
        let sendButtons text buttons =
          chatCtx.SendMessageButtons(text, buttons) &|> ignore

        let! presets = presetRepo.ListUserPresets userId

        do! sendPresetsMessage sendButtons presets (resp[Messages.NoCurrentPreset])

        return ()
    }

  let queueCurrentPresetRun (resp: IResourceProvider) (userRepo: #ILoadUser) (chatCtx: #ISendMessage) (presetService: #IQueueRun) =
    let onSuccess (preset: Preset) =
      chatCtx.SendMessage resp[Messages.PresetQueued, [| preset.Name |]]
      |> Task.ignore

    let onError errors =
      let errorsText =
        errors
        |> Seq.map (function
          | Preset.ValidationError.NoIncludedPlaylists -> resp[Messages.NoIncludedPlaylists]
          | Preset.ValidationError.NoTargetedPlaylists -> resp[Messages.NoTargetedPlaylists])
        |> String.concat Environment.NewLine

      chatCtx.SendMessage errorsText |> Task.ignore

    fun userId ->
      userId |> userRepo.LoadUser &|> (fun u -> u.CurrentPresetId |> Option.get)
      &|&> (fun p -> presetService.QueueRun(userId, p))
      |> TaskResult.taskEither onSuccess onError

[<RequireQualifiedAccess>]
module Chat =
  let create (chatRepo: #ISaveChat) (userService: #ICreateUser) (resourceSettings: ResourcesSettings) =
    fun chatId lang -> task {
      let! newUser = userService.CreateUser()

      let newChat: Chat =
        { Id = chatId
          UserId = newUser.Id
          Lang = lang |> Option.defaultValue resourceSettings.DefaultLang }

      do! chatRepo.SaveChat newChat

      return newChat
    }

[<RequireQualifiedAccess>]
module Resources =
  let getResourceProvider createResp createDefaultResp : Resources.GetResourceProvider =
    function
    | Some l -> createResp l
    | None -> createDefaultResp ()

type ChatService(chatRepo: IChatRepo, userService: IUserService, resourceOptions: IOptions<ResourcesSettings>) =
  interface IChatService with
    member this.CreateChat(chatId, lang) =
      Chat.create chatRepo userService resourceOptions.Value chatId lang