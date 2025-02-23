﻿module Telegram.Workflows

open Domain.Repos
open MusicPlatform
open Domain.Core
open Domain.Workflows
open Microsoft.FSharp.Core
open Resources
open Telegram.Constants
open Telegram.Core
open Telegram.Repos
open otsom.fs.Bot
open otsom.fs.Core
open otsom.fs.Extensions
open otsom.fs.Telegram.Bot.Auth.Spotify
open System
open otsom.fs.Extensions.String
open Telegram.Helpers

[<Literal>]
let keyboardColumns = 4

[<Literal>]
let buttonsPerPage = 20

let private getPresetMessage =
  fun (preset: Preset) ->
    task {
      let presetId = preset.Id |> PresetId.value

      let likedTracksHandlingText, likedTracksButtonText, likedTracksButtonData =
        match preset.Settings.LikedTracksHandling with
        | PresetSettings.LikedTracksHandling.Include ->
          Messages.LikedTracksIncluded, Buttons.ExcludeLikedTracks, $"p|{presetId}|{CallbackQueryConstants.excludeLikedTracks}"
        | PresetSettings.LikedTracksHandling.Exclude ->
          Messages.LikedTracksExcluded, Buttons.IgnoreLikedTracks, $"p|{presetId}|{CallbackQueryConstants.ignoreLikedTracks}"
        | PresetSettings.LikedTracksHandling.Ignore ->
          Messages.LikedTracksIgnored, Buttons.IncludeLikedTracks, $"p|{presetId}|{CallbackQueryConstants.includeLikedTracks}"

      let recommendationsText, recommendationsButtonText, recommendationsButtonData =
        match preset.Settings.RecommendationsEnabled with
        | true ->
          Messages.RecommendationsEnabled,
          Buttons.DisableRecommendations,
          sprintf "p|%s|%s" presetId CallbackQueryConstants.disableRecommendations
        | false ->
          Messages.RecommendationsDisabled,
          Buttons.EnableRecommendations,
          sprintf "p|%s|%s" presetId CallbackQueryConstants.enableRecommendations

      let uniqueArtistsText, uniqueArtistsButtonText, uniqueArtistsButtonData =
        match preset.Settings.UniqueArtists with
        | true ->
          Messages.UniqueArtistsEnabled,
          Buttons.DisableUniqueArtists,
          sprintf "p|%s|%s" presetId CallbackQueryConstants.disableUniqueArtists
        | false ->
          Messages.UniqueArtistsDisabled,
          Buttons.EnableUniqueArtists,
          sprintf "p|%s|%s" presetId CallbackQueryConstants.enableUniqueArtists

      let text =
        String.Format(
          Messages.PresetInfo,
          preset.Name,
          likedTracksHandlingText,
          recommendationsText,
          uniqueArtistsText,
          (preset.Settings.Size |> PresetSettings.Size.value)
        )

      let keyboard =
        seq {
          MessageButton(likedTracksButtonText, likedTracksButtonData)
          MessageButton(uniqueArtistsButtonText, uniqueArtistsButtonData)
          MessageButton(recommendationsButtonText, recommendationsButtonData)
        }

      return (text, keyboard)
    }

let private sendPresetsMessage sendOrEditButtons =
  fun user message ->
    task {
      let keyboardMarkup =
        user.Presets
        |> Seq.map (fun p -> MessageButton(p.Name, $"p|{p.Id |> PresetId.value}|i"))
        |> Seq.singleton

      do! sendOrEditButtons message keyboardMarkup
    }

let internal createPlaylistsPage page (playlists: 'a list) playlistToButton presetId =
  let (Page page) = page
  let remainingPlaylists = playlists[page * buttonsPerPage ..]
  let playlistsForPage = remainingPlaylists[.. buttonsPerPage - 1]

  let playlistsButtons =
    [ 0..keyboardColumns .. playlistsForPage.Length ]
    |> List.map (fun idx -> playlistsForPage |> List.skip idx |> List.takeSafe keyboardColumns)
    |> List.map (Seq.map playlistToButton)

  let presetId = presetId |> PresetId.value

  let backButton = MessageButton("<< Back >>", $"p|{presetId}|i")

  let prevButton =
    if page > 0 then
      Some(MessageButton("<< Prev", $"p|{presetId}|ip|{page - 1}"))
    else
      None

  let nextButton =
    if remainingPlaylists.Length > buttonsPerPage then
      Some(MessageButton("Next >>", $"p|{presetId}|ip|{page + 1}"))
    else
      None

  let serviceButtons =
    match (prevButton, nextButton) with
    | Some pb, Some nb -> [ pb; backButton; nb ]
    | None, Some nb -> [ backButton; nb ]
    | Some pb, None -> [ pb; backButton ]
    | _ -> [ backButton ]

  Seq.append playlistsButtons (serviceButtons |> Seq.ofList |> Seq.singleton)

let private getPlaylistButtons presetId playlistId playlistType specificButtons =
  let presetId = presetId |> PresetId.value

  let buttonDataTemplate =
    sprintf "p|%s|%s|%s|%s" presetId playlistType (playlistId |> PlaylistId.value)

  seq {
    yield specificButtons

    yield seq {
      MessageButton("Remove", buttonDataTemplate "rm")
    }

    yield seq { MessageButton("<< Back >>", sprintf "p|%s|%s|%i" presetId playlistType 0) }
  }

let sendLoginMessage (initAuth: Auth.Init) (sendLink: SendLink) : SendLoginMessage =
  fun userId ->
    initAuth (userId |> UserId.value |> string |> AccountId)
    |> Task.bind (sendLink userId Messages.LoginToSpotify Buttons.Login)

[<RequireQualifiedAccess>]
module IncludedPlaylist =
  let list (presetRepo: #ILoadPreset) (botMessageCtx: #IEditMessageButtons) : IncludedPlaylist.List =
    let createButtonFromPlaylist presetId =
      fun (playlist: IncludedPlaylist) ->
        MessageButton(
          playlist.Name,
          sprintf "p|%s|ip|%s|i" (presetId |> PresetId.value) (playlist.Id |> ReadablePlaylistId.value |> PlaylistId.value)
        )

    fun presetId page ->
      let createButtonFromPlaylist = createButtonFromPlaylist presetId

      task {
        let! preset = presetRepo.LoadPreset presetId

        let replyMarkup =
          createPlaylistsPage page preset.IncludedPlaylists createButtonFromPlaylist preset.Id

        return! botMessageCtx.EditMessageButtons $"Preset *{preset.Name}* has the next included playlists:" replyMarkup
      }

  let show
    (chatCtx: #IEditMessageButtons)
    (presetRepo: #ILoadPreset)
    (countPlaylistTracks: Playlist.CountTracks)
    : IncludedPlaylist.Show =
    fun presetId playlistId ->
      task {
        let! preset = presetRepo.LoadPreset presetId

        let includedPlaylist =
          preset.IncludedPlaylists |> List.find (fun p -> p.Id = playlistId)

        let! playlistTracksCount = countPlaylistTracks (playlistId |> ReadablePlaylistId.value)

        let messageText =
          String.Format(Messages.IncludedPlaylistDetails, includedPlaylist.Name, playlistTracksCount, includedPlaylist.LikedOnly)

        let buttons = getPlaylistButtons presetId (playlistId |> ReadablePlaylistId.value) "ip" Seq.empty

        return! chatCtx.EditMessageButtons messageText buttons
      }

  let remove
    (presetRepo: #ILoadPreset) (botMessageCtx: #IEditMessageButtons)
    (removeIncludedPlaylist: Domain.Core.IncludedPlaylist.Remove)
    showNotification
    : IncludedPlaylist.Remove =
    fun presetId playlistId ->
      task {
        do! removeIncludedPlaylist presetId playlistId
        do! showNotification "Included playlist successfully removed"

        return! list presetRepo botMessageCtx presetId (Page 0)
      }

[<RequireQualifiedAccess>]
module ExcludedPlaylist =
  let list (presetRepo: #ILoadPreset) (botMessageCtx: #IEditMessageButtons) : ExcludedPlaylist.List =
    let createButtonFromPlaylist presetId =
      fun (playlist: ExcludedPlaylist) ->
        MessageButton(
          playlist.Name,
          sprintf "p|%s|ep|%s|i" (presetId |> PresetId.value) (playlist.Id |> ReadablePlaylistId.value |> PlaylistId.value)
        )

    fun presetId page ->
      let createButtonFromPlaylist = createButtonFromPlaylist presetId

      task {
        let! preset = presetRepo.LoadPreset presetId

        let replyMarkup =
          createPlaylistsPage page preset.ExcludedPlaylists createButtonFromPlaylist preset.Id

        return! botMessageCtx.EditMessageButtons $"Preset *{preset.Name}* has the next excluded playlists:" replyMarkup
      }

  let show
    (botMessageCtx: #IEditMessageButtons)
    (presetRepo: #ILoadPreset)
    (countPlaylistTracks: Playlist.CountTracks)
    : ExcludedPlaylist.Show =
    fun presetId playlistId ->
      task {
        let! preset = presetRepo.LoadPreset presetId

        let excludedPlaylist =
          preset.ExcludedPlaylists |> List.find (fun p -> p.Id = playlistId)

        let! playlistTracksCount = countPlaylistTracks (playlistId |> ReadablePlaylistId.value)

        let messageText =
          sprintf "*Name:* %s\n*Tracks count:* %i" excludedPlaylist.Name playlistTracksCount

        let buttons = getPlaylistButtons presetId (playlistId |> ReadablePlaylistId.value) "ep" Seq.empty

        return! botMessageCtx.EditMessageButtons messageText buttons
      }

  let remove
    presetRepo botMessageCtx
    (removeExcludedPlaylist: Domain.Core.ExcludedPlaylist.Remove)
    showNotification
    : ExcludedPlaylist.Remove =
    fun presetId playlistId ->
      task {
        do! removeExcludedPlaylist presetId playlistId
        do! showNotification "Excluded playlist successfully removed"

        return! list presetRepo botMessageCtx presetId (Page 0)
      }

[<RequireQualifiedAccess>]
module TargetedPlaylist =
  let list (presetRepo: #ILoadPreset) (botMessageCtx: #IEditMessageButtons) : TargetedPlaylist.List =
    let createButtonFromPlaylist presetId =
      fun (playlist: TargetedPlaylist) ->
        MessageButton(
          playlist.Name,
          sprintf "p|%s|tp|%s|i" (presetId |> PresetId.value) (playlist.Id |> WritablePlaylistId.value |> PlaylistId.value)
        )

    fun presetId page ->
      let createButtonFromPlaylist = createButtonFromPlaylist presetId

      task {
        let! preset = presetRepo.LoadPreset presetId

        let replyMarkup =
          createPlaylistsPage page preset.TargetedPlaylists createButtonFromPlaylist preset.Id

        return! botMessageCtx.EditMessageButtons $"Preset *{preset.Name}* has the next targeted playlists:" replyMarkup
      }

  let show
    (botMessageCtx: #IEditMessageButtons)
    (presetRepo: #ILoadPreset)
    (countPlaylistTracks: Playlist.CountTracks)
    : TargetedPlaylist.Show =
    fun presetId playlistId ->
      task {
        let! preset = presetRepo.LoadPreset presetId

        let targetPlaylist =
          preset.TargetedPlaylists |> List.find (fun p -> p.Id = playlistId)

        let! playlistTracksCount = countPlaylistTracks (playlistId |> WritablePlaylistId.value)

        let buttonText, buttonDataBuilder =
          if targetPlaylist.Overwrite then
            ("Append", sprintf "p|%s|tp|%s|a")
          else
            ("Overwrite", sprintf "p|%s|tp|%s|o")

        let presetId' = (presetId |> PresetId.value)
        let playlistId' = (playlistId |> WritablePlaylistId.value |> PlaylistId.value)

        let buttonData = buttonDataBuilder presetId' playlistId'

        let additionalButtons = Seq.singleton (MessageButton(buttonText, buttonData))

        let buttons = getPlaylistButtons presetId (playlistId |> WritablePlaylistId.value) "tp" additionalButtons

        let messageText =
          sprintf "*Name:* %s\n*Tracks count:* %i\n*Overwrite?:* %b" targetPlaylist.Name playlistTracksCount targetPlaylist.Overwrite

        return! botMessageCtx.EditMessageButtons messageText buttons
      }

  let appendTracks
    (appendToTargetedPlaylist: TargetedPlaylist.AppendTracks)
    showNotification
    (showTargetedPlaylist: TargetedPlaylist.Show)
    : TargetedPlaylist.AppendTracks =
    fun presetId playlistId ->
      task {
        do! appendToTargetedPlaylist presetId playlistId
        do! showNotification "Target playlist will be appended with generated tracks"

        return! showTargetedPlaylist presetId playlistId
      }

  let overwriteTracks
    (overwriteTargetedPlaylist: TargetedPlaylist.OverwriteTracks)
    showNotification
    (showTargetedPlaylist: TargetedPlaylist.Show)
    : TargetedPlaylist.OverwriteTracks =
    fun presetId playlistId ->
      task {
        do! overwriteTargetedPlaylist presetId playlistId
        do! showNotification "Target playlist will be overwritten with generated tracks"

        return! showTargetedPlaylist presetId playlistId
      }

  let remove
    presetRepo botMessageCtx
    (removeTargetedPlaylist: Domain.Core.TargetedPlaylist.Remove)
    showNotification
    : TargetedPlaylist.Remove =
    fun presetId playlistId ->
      task {
        do! removeTargetedPlaylist presetId playlistId
        do! showNotification "Target playlist successfully removed"

        return! list presetRepo botMessageCtx presetId (Page 0)
      }

[<RequireQualifiedAccess>]
module Preset =
  let internal show' showButtons =
    fun preset ->
      task {
        let! text, keyboard = getPresetMessage preset

        let presetId = preset.Id |> PresetId.value

        let keyboardMarkup =
          seq {
            seq {
              MessageButton("Included playlists", $"p|%s{presetId}|ip|0")
              MessageButton("Excluded playlists", $"p|%s{presetId}|ep|0")
              MessageButton("Target playlists", $"p|%s{presetId}|tp|0")
            }

            keyboard

            seq { MessageButton("Run", $"p|%s{presetId}|r") }

            seq { MessageButton("Set as current", $"p|%s{presetId}|c") }

            seq { MessageButton("Remove", sprintf "p|%s|rm" presetId) }

            seq { MessageButton("<< Back >>", "p") }
          }

        do! showButtons text keyboardMarkup
      }

  let show (presetRepo: #ILoadPreset) (botMessageCtx: #IEditMessageButtons) : Preset.Show =
    presetRepo.LoadPreset
    >> Task.bind (show' botMessageCtx.EditMessageButtons)

  let queueRun
    (chatCtx: #ISendMessage)
    (queueRun': Domain.Core.Preset.QueueRun)
    (answerCallbackQuery: AnswerCallbackQuery)
    : Preset.Run =
    let onSuccess (preset: Preset) =
      chatCtx.SendMessage $"Preset *{preset.Name}* run is queued!"
      |> Task.ignore

    let onError errors =
      let errorsText =
        errors
        |> Seq.map (function
          | Preset.ValidationError.NoIncludedPlaylists -> "No included playlists!"
          | Preset.ValidationError.NoTargetedPlaylists -> "No targeted playlists!")
        |> String.concat Environment.NewLine

      chatCtx.SendMessage errorsText
      |> Task.ignore
      |> Task.taskTap answerCallbackQuery

    queueRun'
    >> TaskResult.taskEither onSuccess onError

  let run (chatCtx: #ISendMessage & #IBuildBotMessageContext) (runPreset: Domain.Core.Preset.Run) : Preset.Run =
    fun presetId ->
      let onSuccess (botMessageCtx: #IEditMessage) =
        fun (preset: Preset) -> botMessageCtx.EditMessage $"Preset *{preset.Name}* executed!"

      let onError (botMessageCtx: #IEditMessage) =
        function
        | Preset.RunError.NoIncludedTracks -> botMessageCtx.EditMessage "Your preset has 0 included tracks"
        | Preset.RunError.NoPotentialTracks -> botMessageCtx.EditMessage "Playlists combination in your preset produced 0 potential tracks"

      task {
        let! sentMessageId = chatCtx.SendMessage "Running preset..."
        let botMessageContext = chatCtx.BuildBotMessageContext sentMessageId

        return! runPreset presetId |> TaskResult.taskEither (onSuccess botMessageContext) (onError botMessageContext)
      }

[<RequireQualifiedAccess>]
module CurrentPreset =
  let includePlaylist
    (chatMessageCtx: #IReplyToMessage)
    (userRepo: #ILoadUser)
    (includePlaylist: Preset.IncludePlaylist)
    (initAuth: Auth.Init)
    (sendLink: SendLink)
    : Playlist.Include =
    fun userId rawPlaylistId ->
      task {
        let! currentPresetId = userRepo.LoadUser userId |> Task.map (fun u -> u.CurrentPresetId |> Option.get)
        let includePlaylistResult = includePlaylist userId currentPresetId rawPlaylistId

        let onSuccess (playlist: IncludedPlaylist) =
          chatMessageCtx.ReplyToMessage $"*{playlist.Name}* successfully included into current preset!"

        let onError =
          function
          | Preset.IncludePlaylistError.IdParsing(Playlist.IdParsingError id) ->
            chatMessageCtx.ReplyToMessage (String.Format(Messages.PlaylistIdCannotBeParsed, id))
          | Preset.IncludePlaylistError.Load(Playlist.LoadError.NotFound) ->
            let (Playlist.RawPlaylistId rawPlaylistId) = rawPlaylistId

            chatMessageCtx.ReplyToMessage (String.Format(Messages.PlaylistNotFoundInSpotify, rawPlaylistId))
          | Preset.IncludePlaylistError.Unauthorized ->
            sendLoginMessage initAuth sendLink userId

        return! includePlaylistResult |> TaskResult.taskEither onSuccess onError |> Task.ignore
      }

  let excludePlaylist
    (chatMessageCtx: #IReplyToMessage)
    (userRepo: #ILoadUser)
    (excludePlaylist: Preset.ExcludePlaylist)
    (initAuth: Auth.Init)
    (sendLink: SendLink)
    : Playlist.Exclude =
    fun userId rawPlaylistId ->
      task {
        let! currentPresetId = userRepo.LoadUser userId |> Task.map (fun u -> u.CurrentPresetId |> Option.get)

        let excludePlaylistResult = excludePlaylist userId currentPresetId rawPlaylistId

        let onSuccess (playlist: ExcludedPlaylist) =
          chatMessageCtx.ReplyToMessage $"*{playlist.Name}* successfully excluded from current preset!"

        let onError =
          function
          | Preset.ExcludePlaylistError.IdParsing(Playlist.IdParsingError id) ->
            chatMessageCtx.ReplyToMessage (String.Format(Messages.PlaylistIdCannotBeParsed, id))
          | Preset.ExcludePlaylistError.Load(Playlist.LoadError.NotFound) ->
            let (Playlist.RawPlaylistId rawPlaylistId) = rawPlaylistId
            chatMessageCtx.ReplyToMessage (String.Format(Messages.PlaylistNotFoundInSpotify, rawPlaylistId))
          | Preset.ExcludePlaylistError.Unauthorized ->
            sendLoginMessage initAuth sendLink userId

        return! excludePlaylistResult |> TaskResult.taskEither onSuccess onError |> Task.ignore
      }

  let targetPlaylist
    (chatMessageCtx: #IReplyToMessage)
    (loadUser: User.Get)
    (targetPlaylist: Playlist.TargetPlaylist)
    (initAuth: Auth.Init)
    (sendLink: SendLink)
    : Playlist.Target =
    fun userId rawPlaylistId ->
      task {
        let! currentPresetId = loadUser userId |> Task.map (fun u -> u.CurrentPresetId |> Option.get)

        let targetPlaylistResult = rawPlaylistId |> targetPlaylist currentPresetId

        let onSuccess (playlist: TargetedPlaylist) =
          chatMessageCtx.ReplyToMessage $"*{playlist.Name}* successfully targeted for current preset!"

        let onError =
          function
          | Playlist.TargetPlaylistError.IdParsing(Playlist.IdParsingError id) ->
            chatMessageCtx.ReplyToMessage (String.Format(Messages.PlaylistIdCannotBeParsed, id))
          | Playlist.TargetPlaylistError.Load(Playlist.LoadError.NotFound) ->
            let (Playlist.RawPlaylistId rawPlaylistId) = rawPlaylistId
            chatMessageCtx.ReplyToMessage (String.Format(Messages.PlaylistNotFoundInSpotify, rawPlaylistId))
          | Playlist.TargetPlaylistError.AccessError _ -> chatMessageCtx.ReplyToMessage Messages.PlaylistIsReadonly
          | Playlist.TargetPlaylistError.Unauthorized ->
            sendLoginMessage initAuth sendLink userId

        return! targetPlaylistResult |> TaskResult.taskEither onSuccess onError |> Task.ignore
      }

[<RequireQualifiedAccess>]
module User =
  let private showPresets' sendOrEditButtons loadUser =
    fun userId ->
      task {
        let! user = loadUser userId

        return! sendPresetsMessage sendOrEditButtons user "Your presets"
      }

  let internal sendPresets (chatCtx: #ISendMessageButtons) loadUser  =
    showPresets' (fun text buttons -> chatCtx.SendMessageButtons text buttons |> Task.map ignore) loadUser

  let showPresets (botMessageCtx: #IEditMessageButtons) loadUser : User.ShowPresets =
    showPresets' botMessageCtx.EditMessageButtons loadUser

  let sendCurrentPreset (loadUser: User.Get) (getPreset: Preset.Get) (chatCtx: #ISendKeyboard)
    : User.SendCurrentPreset
    =
    fun userId ->
      userId |> loadUser &|> _.CurrentPresetId
      &|&> (function
      | Some presetId -> task {
          let! preset = getPreset presetId
          let! text, _ = getPresetMessage preset

          let keyboard : Keyboard =
            [  [ KeyboardButton(Buttons.RunPreset) ]
               [ KeyboardButton(Buttons.MyPresets) ]
               [ KeyboardButton(Buttons.CreatePreset) ]

               [ KeyboardButton(Buttons.IncludePlaylist); KeyboardButton(Buttons.ExcludePlaylist); KeyboardButton(Buttons.TargetPlaylist) ]

               [ Buttons.Settings ] ]

          return! chatCtx.SendKeyboard text keyboard &|> ignore
        }
      | None ->
        let keyboard : Keyboard =
          [ [ KeyboardButton(Buttons.MyPresets) ]; [ KeyboardButton(Buttons.CreatePreset) ] ]

        chatCtx.SendKeyboard "You did not select current preset" keyboard &|> ignore)

  let runOnCurrentPresetId (getUser: User.Get) (chatCtx: #ISendKeyboard & #ISendMessageButtons) fn =
    fun userId ->
      userId
      |> getUser
      |> Task.bind (fun user ->
        match user.CurrentPresetId with
        | Some presetId -> fn presetId
        | None when user.Presets.IsEmpty ->
          let buttons: Keyboard = [ [ KeyboardButton(Buttons.CreatePreset) ] ]

          chatCtx.SendKeyboard Messages.NoCurrentPreset buttons &|> ignore
        | _ -> sendPresetsMessage (chatCtx.SendMessageButtons >> (fun a -> a >> Task.ignore)) user Messages.NoCurrentPreset)

  let internal sendCurrentPresetSettings
    (chatCtx: #ISendKeyboard)
    (loadUser: User.Get)
    (getPreset: Preset.Get)
    : User.SendCurrentPresetSettings =

    let handler presetId =
      presetId
      |> getPreset
      |> Task.bind getPresetMessage
      |> Task.bind (fun (text, _) ->
        let buttons: Keyboard =
          [| [| KeyboardButton Buttons.SetPresetSize |]; [| KeyboardButton(Buttons.Back) |] |]

        chatCtx.SendKeyboard text buttons &|> ignore)

    runOnCurrentPresetId loadUser chatCtx handler

  let removePreset (botMessageCtx: #IEditMessageButtons) loadUser (removePreset: User.RemovePreset) : User.RemovePreset =
    fun userId presetId ->
      task {
        do! removePreset userId presetId

        return! showPresets' botMessageCtx.EditMessageButtons loadUser userId
      }

  let internal setCurrentPresetSize
    (chatCtx: #ISendMessage)
    (sendSettingsMessage: User.SendCurrentPresetSettings)
    (setPresetSize: Domain.Core.User.SetCurrentPresetSize)
    : User.SetCurrentPresetSize
    =
    fun userId size ->
      let onSuccess () = sendSettingsMessage userId

      let onError =
        function
        | PresetSettings.Size.TooSmall -> chatCtx.SendMessage Messages.PresetSizeTooSmall
        | PresetSettings.Size.TooBig -> chatCtx.SendMessage Messages.PresetSizeTooBig
        | PresetSettings.Size.NotANumber -> chatCtx.SendMessage Messages.PresetSizeNotANumber

      setPresetSize userId size
      |> TaskResult.taskEither onSuccess (onError >> Task.ignore)

  let setCurrentPreset showNotification (setCurrentPreset: Domain.Core.User.SetCurrentPreset) : User.SetCurrentPreset =
    fun userId presetId ->
      task {
        do! setCurrentPreset userId presetId

        return! showNotification "Current preset is successfully set!"
      }

  let queueCurrentPresetRun
    (chatCtx: #ISendMessage)
    (queueRun: Domain.Core.Preset.QueueRun)
    (loadUser: User.Get)
    (answerCallbackQuery: AnswerCallbackQuery)
    : User.QueueCurrentPresetRun =

    fun userId ->
      userId
      |> loadUser
      &|> (fun u -> u.CurrentPresetId |> Option.get)
      &|&> (Preset.queueRun chatCtx queueRun answerCallbackQuery)

  let createPreset (chatCtx: #ISendMessageButtons) (createPreset: Domain.Core.User.CreatePreset) : User.CreatePreset =
    fun userId name ->
      createPreset userId name
      &|&> Preset.show' (fun text buttons -> chatCtx.SendMessageButtons text buttons &|> ignore)

[<RequireQualifiedAccess>]
module PresetSettings =
  let enableUniqueArtists
    presetRepo
    botMessageCtx
    (enableUniqueArtists: PresetSettings.EnableUniqueArtists)
    showNotification
    : PresetSettings.EnableUniqueArtists =
    fun presetId ->
      task {
        do! enableUniqueArtists presetId

        do! showNotification Messages.Updated

        return! Preset.show presetRepo botMessageCtx presetId
      }

  let disableUniqueArtists
    presetRepo
    botMessageCtx
    (disableUniqueArtists: PresetSettings.DisableUniqueArtists)
    showNotification
    : PresetSettings.DisableUniqueArtists =
    fun presetId ->
      task {
        do! disableUniqueArtists presetId

        do! showNotification Messages.Updated

        return! Preset.show presetRepo botMessageCtx presetId
      }

  let enableRecommendations
    presetRepo
    botMessageCtx
    (enableRecommendations: PresetSettings.EnableRecommendations)
    showNotification
    : PresetSettings.EnableRecommendations =
    fun presetId ->
      task {
        do! enableRecommendations presetId

        do! showNotification Messages.Updated

        return! Preset.show presetRepo botMessageCtx presetId
      }

  let disableRecommendations
    presetRepo
    botMessageCtx
    (disableRecommendations: PresetSettings.DisableRecommendations)
    showNotification
    : PresetSettings.DisableRecommendations =
    fun presetId ->
      task {
        do! disableRecommendations presetId

        do! showNotification Messages.Updated

        return! Preset.show presetRepo botMessageCtx presetId
      }

  let private setLikedTracksHandling presetRepo botMessageCtx showNotification setLikedTracksHandling =
    fun presetId ->
      task {
        do! setLikedTracksHandling presetId

        do! showNotification Messages.Updated

        return! Preset.show presetRepo botMessageCtx presetId
      }

  let includeLikedTracks presetRepo botMessageCtx showNotification (includeLikedTracks: PresetSettings.IncludeLikedTracks) : PresetSettings.IncludeLikedTracks =
    setLikedTracksHandling presetRepo botMessageCtx showNotification includeLikedTracks

  let excludeLikedTracks presetRepo botMessageCtx showNotification (excludeLikedTracks: PresetSettings.ExcludeLikedTracks) : PresetSettings.ExcludeLikedTracks =
    setLikedTracksHandling presetRepo botMessageCtx showNotification excludeLikedTracks

  let ignoreLikedTracks presetRepo botMessageCtx showNotification (ignoreLikedTracks: PresetSettings.IgnoreLikedTracks) : PresetSettings.IgnoreLikedTracks =
    setLikedTracksHandling presetRepo botMessageCtx showNotification ignoreLikedTracks

let faqMessageHandler (chatCtx: #ISendMessage) : MessageHandler =
  fun message -> task {
    match message.Text with
    | Equals "/faq" ->
      do! chatCtx.SendMessage Messages.FAQ &|> ignore

      return Some()
    | _ ->
      return None
  }

let privacyMessageHandler (chatCtx: #ISendMessage) : MessageHandler =
  fun message -> task {
    match message.Text with
    | Equals "/privacy" ->
      do! chatCtx.SendMessage Messages.Privacy &|> ignore

      return Some()
    | _ ->
      return None
  }

let guideMessageHandler (chatCtx: #ISendMessage) : MessageHandler =
  fun message ->task {
    match message.Text with
    | Equals "/guide" ->
      do! chatCtx.SendMessage Messages.Guide &|> ignore

      return Some()
    | _ ->
      return None
  }

let helpMessageHandler (chatCtx: #ISendMessage) : MessageHandler =
  fun message -> task {
    match message.Text with
    | Equals "/help" ->
      do! chatCtx.SendMessage Messages.Help &|> ignore

      return Some()
    | _ ->
      return None
  }

let myPresetsMessageHandler (getUser: User.Get) (chatRepo: #ILoadChat) (chatCtx: #ISendMessageButtons) : MessageHandler =
  let sendUserPresets = User.sendPresets chatCtx getUser

  fun message -> task {
    let! chat = chatRepo.LoadChat message.ChatId

    match message.Text with
    | Equals Buttons.MyPresets ->
      do! sendUserPresets chat.UserId

      return Some()
    | Equals "/presets" ->
      do! sendUserPresets chat.UserId

      return Some()
    | _ -> return None
  }

let backMessageHandler loadUser getPreset (chatRepo: #ILoadChat) (chatCtx: #ISendKeyboard) : MessageHandler =
  fun message -> task {
    let! chat = chatRepo.LoadChat message.ChatId

    match message.Text with
    | Equals Buttons.Back ->
      do! User.sendCurrentPreset loadUser getPreset chatCtx chat.UserId

      return Some()
    | _ -> return None
  }

let presetSettingsMessageHandler getUser getPreset (chatRepo: #ILoadChat) chatCtx : MessageHandler =
  let sendSettingsMessage = User.sendCurrentPresetSettings chatCtx getUser getPreset

  fun message -> task {
    let! chat = chatRepo.LoadChat message.ChatId

    match message.Text with
    | Equals Buttons.Settings ->
      do! sendSettingsMessage chat.UserId

      return Some()
    | _ -> return None
  }

let setPresetSizeMessageHandler setPresetSize loadUser getPreset (chatRepo: #ILoadChat) chatCtx : MessageHandler =
  let setTargetPresetSize =
    User.setCurrentPresetSize chatCtx (User.sendCurrentPresetSettings chatCtx loadUser getPreset) setPresetSize

  fun message -> task {
    let! chat = chatRepo.LoadChat message.ChatId

    match message.ReplyMessage with
    | Some { Text = text } when text = Buttons.SetPresetSize ->
      do! setTargetPresetSize chat.UserId (PresetSettings.RawPresetSize message.Text)

      return Some()
    | _ ->
      return None
  }

let createPresetButtonMessageHandler (chatCtx: #IAskForReply) : MessageHandler =
  fun message -> task {
    match message.Text with
    | Equals Buttons.CreatePreset ->
      do! chatCtx.AskForReply Messages.SendPresetName

      return Some()
    | _ -> return None
  }

let createPresetMessageHandler createPreset (chatRepo: #ILoadChat) chatCtx : MessageHandler =
  let createPreset = User.createPreset chatCtx createPreset

  fun message -> task {
    let! chat = chatRepo.LoadChat message.ChatId

    match message with
    | { Text = text
        ReplyMessage = Some { Text = replyText } } when replyText = Buttons.CreatePreset ->
      do! createPreset chat.UserId text

      return Some()
    | { Text = CommandWithData "/new" text } ->
      do! createPreset chat.UserId text

      return Some()
    | _ -> return None
  }

let includePlaylistButtonMessageHandler
  (chatRepo: #ILoadChat)
  (userRepo: #ILoadUser)
  (buildMusicPlatform: BuildMusicPlatform)
  initAuth
  sendLink
  (chatCtx: #IChatContext)
  : MessageHandler =
  fun message -> task {
    match message.Text with
    | Equals Buttons.IncludePlaylist ->

      let! chat = chatRepo.LoadChat message.ChatId
      let! user = userRepo.LoadUser chat.UserId
      let! musicPlatform = buildMusicPlatform (user.Id |> UserId.value |> string |> MusicPlatform.UserId)

      match musicPlatform with
      | Some _ ->
        do! chatCtx.AskForReply Messages.SendIncludedPlaylist

        return Some()
      | _ ->
        do! sendLoginMessage initAuth sendLink chat.UserId &|> ignore

        return None
    | _ -> return None
  }

let excludePlaylistButtonMessageHandler
  (chatRepo: #ILoadChat)
  (userRepo: #ILoadUser)
  (buildMusicPlatform: BuildMusicPlatform)
  initAuth
  sendLink
  (chatCtx: #IChatContext)
  : MessageHandler =
  fun message -> task {
    match message.Text with
    | Equals Buttons.ExcludePlaylist ->

      let! chat = chatRepo.LoadChat message.ChatId
      let! user = userRepo.LoadUser chat.UserId
      let! musicPlatform = buildMusicPlatform (user.Id |> UserId.value |> string |> MusicPlatform.UserId)

      match musicPlatform with
      | Some _ ->
        do! chatCtx.AskForReply Messages.SendExcludedPlaylist

        return Some()
      | _ ->
        do! sendLoginMessage initAuth sendLink chat.UserId &|> ignore

        return None
    | _ -> return None
  }

let targetPlaylistButtonMessageHandler
  (chatRepo: #ILoadChat)
  (userRepo: #ILoadUser)
  (buildMusicPlatform: BuildMusicPlatform)
  initAuth
  sendLink
  (chatCtx: #IChatContext)
  : MessageHandler =
  fun message -> task {
    match message.Text with
    | Equals Buttons.TargetPlaylist ->

      let! chat = chatRepo.LoadChat message.ChatId
      let! user = userRepo.LoadUser chat.UserId
      let! musicPlatform = buildMusicPlatform (user.Id |> UserId.value |> string |> MusicPlatform.UserId)

      match musicPlatform with
      | Some _ ->
        do! chatCtx.AskForReply Messages.SendTargetedPlaylist

        return Some()
      | _ ->
        do! sendLoginMessage initAuth sendLink chat.UserId &|> ignore

        return None
    | _ -> return None
  }

let includePlaylistMessageHandler
  (userRepo: #ILoadUser)
  (chatRepo: #ILoadChat)
  (includePlaylist: Preset.IncludePlaylist)
  initAuth
  sendLink
  (chatCtx: #IChatContext)
  : MessageHandler =
  fun message -> task {
    let! chat = chatRepo.LoadChat message.ChatId
    let chatMessageCtx = chatCtx.BuildChatMessageContext message.Id

    let includePlaylist =
      CurrentPreset.includePlaylist chatMessageCtx userRepo includePlaylist initAuth sendLink

    match message with
    | { Text = text
        ReplyMessage = Some { Text = replyText } } when replyText = Messages.SendIncludedPlaylist ->
      do! includePlaylist chat.UserId (Playlist.RawPlaylistId text)

      return Some()
    | { Text = CommandWithData "/include" text } ->
      do! includePlaylist chat.UserId (Playlist.RawPlaylistId text)

      return Some()
    | _ -> return None
  }

let excludePlaylistMessageHandler
  (userRepo: #ILoadUser)
  (chatRepo: #ILoadChat)
  (excludePlaylist: Preset.ExcludePlaylist)
  initAuth
  sendLink
  (chatCtx: #IChatContext)
  : MessageHandler =
  fun message -> task {
    let! chat = chatRepo.LoadChat message.ChatId
    let chatMessageCtx = chatCtx.BuildChatMessageContext message.Id

    let excludePlaylist =
      CurrentPreset.excludePlaylist chatMessageCtx userRepo excludePlaylist initAuth sendLink

    match message with
    | { Text = text
        ReplyMessage = Some { Text = replyText } } when replyText = Messages.SendExcludedPlaylist ->
      do! excludePlaylist chat.UserId (Playlist.RawPlaylistId text)

      return Some()
    | { Text = CommandWithData "/exclude" text } ->
      do! excludePlaylist chat.UserId (Playlist.RawPlaylistId text)

      return Some()
    | _ -> return None
  }