module Telegram.Handlers.Message

open Domain.Repos
open MusicPlatform
open Resources
open otsom.fs.Auth
open otsom.fs.Bot
open otsom.fs.Extensions.String
open otsom.fs.Extensions
open Telegram.Workflows
open Domain.Core
open Telegram.Helpers
open Domain.Workflows
open System
open otsom.fs.Resources
open Telegram.Core
open Telegram.Resources

let startMessageHandler
  (userRepo: #ILoadUser)
  (presetRepo: #ILoadPreset)
  (authService: #ICompleteAuth)
  (resp: IResourceProvider)
  (chatCtx: #ISendMessage)
  : MessageHandler =
  fun message -> task {
    match message with
    | { Text = Equals "/start" } ->
      do! User.sendCurrentPreset resp userRepo presetRepo chatCtx message.Chat.UserId

      return Some()
    | { Text = CommandWithData "/start" state } ->
      let! user = userRepo.LoadUser message.Chat.UserId

      let processSuccessfulLogin =
        fun () -> chatCtx.SendMessage "Successful login!" &|> ignore

      let sendErrorMessage =
        function
        | CompleteError.StateNotFound -> chatCtx.SendMessage "State not found. Try to login via fresh link."
        | CompleteError.StateDoesntBelongToUser ->
          chatCtx.SendMessage("State provided does not belong to your login request. Try to login via fresh link.")

      do!
        authService.CompleteAuth(user.Id.ToAccountId(), State.Parse state)
        |> TaskResult.taskEither processSuccessfulLogin (sendErrorMessage >> Task.ignore)

      return Some()
    | _ -> return None
  }

let faqMessageHandler (resp: IResourceProvider) (chatCtx: #ISendMessage) : MessageHandler =
  fun message -> task {
    match message.Text with
    | Equals "/faq" ->
      do! chatCtx.SendMessage(resp[Messages.FAQ]) &|> ignore

      return Some()
    | _ -> return None
  }

let privacyMessageHandler (resp: IResourceProvider) (chatCtx: #ISendMessage) : MessageHandler =
  fun message -> task {
    match message.Text with
    | Equals "/privacy" ->
      do! chatCtx.SendMessage(resp[Messages.Privacy]) &|> ignore

      return Some()
    | _ -> return None
  }

let guideMessageHandler (resp: IResourceProvider) (chatCtx: #ISendMessage) : MessageHandler =
  fun message -> task {
    match message.Text with
    | Equals "/guide" ->
      do! chatCtx.SendMessage(resp[Messages.Guide]) &|> ignore

      return Some()
    | _ -> return None
  }

let helpMessageHandler (resp: IResourceProvider) (chatCtx: #ISendMessage) : MessageHandler =
  fun message -> task {
    match message.Text with
    | Equals "/help" ->
      do! chatCtx.SendMessage(resp[Messages.Help]) &|> ignore

      return Some()
    | _ -> return None
  }

let myPresetsMessageHandler presetRepo (resp: IResourceProvider) (chatCtx: #ISendMessageButtons) : MessageHandler =
  let sendUserPresets = User.sendPresets chatCtx presetRepo

  fun message -> task {
    match message.Text with
    | Equals Buttons.MyPresets
    | Equals "/presets" ->
      do! sendUserPresets message.Chat.UserId

      return Some()
    | _ -> return None
  }

let backMessageButtonHandler loadUser getPreset (resp: IResourceProvider) (chatCtx: #ISendKeyboard) : MessageHandler =
  fun message -> task {
    match message.Text with
    | Equals Buttons.Back ->
      do! User.sendCurrentPreset resp loadUser getPreset chatCtx message.Chat.UserId

      return Some()
    | _ -> return None
  }

let presetSettingsMessageHandler userRepo presetRepo (resp: IResourceProvider) chatCtx : MessageHandler =
  let sendSettingsMessage = User.sendCurrentPresetSettings resp userRepo presetRepo chatCtx

  fun message -> task {
    match message.Text with
    | Equals Buttons.Settings ->
      do! sendSettingsMessage message.Chat.UserId

      return Some()
    | _ -> return None
  }

let setPresetSizeMessageButtonHandler (resp: IResourceProvider) (chatCtx: #IAskForReply) : MessageHandler =
  fun message -> task {
    match message.Text with
    | Equals Buttons.SetPresetSize ->
      do! chatCtx.AskForReply resp[Messages.SendPresetSize]

      return Some()
    | _ -> return None
  }

let setPresetSizeMessageHandler
  (userService: #ISetCurrentPresetSize)
  userRepo
  presetRepo
  (resp: IResourceProvider)
  (chatCtx: #ISendMessage)
  : MessageHandler =
  let onSuccess chat =
    fun () -> User.sendCurrentPresetSettings resp userRepo presetRepo chatCtx chat

  let onError =
    function
    | PresetSettings.ParsingError.TooSmall -> chatCtx.SendMessage resp[Messages.PresetSizeTooSmall]
    | PresetSettings.ParsingError.TooBig -> chatCtx.SendMessage resp[Messages.PresetSizeTooBig]
    | PresetSettings.ParsingError.NotANumber -> chatCtx.SendMessage resp[Messages.PresetSizeNotANumber]

  fun message -> task {
    match message with
    | { Text = text
        ReplyMessage = Some { Text = replyText } } when replyText = Buttons.SetPresetSize ->
      do!
        userService.SetCurrentPresetSize(message.Chat.UserId, (PresetSettings.RawPresetSize text))
        |> TaskResult.taskEither (onSuccess message.Chat.UserId) (onError >> Task.ignore)

      return Some()
    | { Text = CommandWithData "/size" text } ->
      do!
        (userService.SetCurrentPresetSize(message.Chat.UserId, (PresetSettings.RawPresetSize text))
         |> TaskResult.taskEither (onSuccess message.Chat.UserId) (onError >> Task.ignore))

      return Some()
    | _ -> return None
  }

let createPresetButtonMessageHandler (resp: IResourceProvider) (chatCtx: #IAskForReply) : MessageHandler =
  fun message -> task {
    match message.Text with
    | Equals Buttons.CreatePreset ->
      do! chatCtx.AskForReply resp[Messages.SendPresetName]

      return Some()
    | _ -> return None
  }

let createPresetMessageHandler (presetService: IPresetService) (resp: IResourceProvider) (chatCtx: #ISendMessage) : MessageHandler =
  fun message -> task {
    match message with
    | { Text = text
        ReplyMessage = Some { Text = replyText } } when replyText = resp[Messages.SendPresetName] ->
      let! preset = presetService.CreatePreset(message.Chat.UserId, text)

      do! Preset.send resp chatCtx preset

      return Some()
    | { Text = CommandWithData "/new" text } ->
      let! preset = presetService.CreatePreset(message.Chat.UserId, text)

      do! Preset.send resp chatCtx preset

      return Some()
    | _ -> return None
  }

let includePlaylistButtonMessageHandler
  (musicPlatformFactory: IMusicPlatformFactory)
  authService
  (resp: IResourceProvider)
  (chatCtx: #IAskForReply)
  : MessageHandler =
  fun message -> task {
    match message.Text with
    | Equals Buttons.IncludePlaylist ->
      let! musicPlatform = musicPlatformFactory.GetMusicPlatform(message.Chat.UserId.ToMusicPlatformId())

      match musicPlatform with
      | Some _ ->
        do! chatCtx.AskForReply resp[Messages.SendIncludedPlaylist]

        return Some()
      | _ ->
        do! sendLoginMessage authService resp chatCtx message.Chat.UserId &|> ignore

        return Some()
    | _ -> return None
  }

let excludePlaylistButtonMessageHandler
  (musicPlatformFactory: IMusicPlatformFactory)
  authService
  (resp: IResourceProvider)
  (chatCtx: #IAskForReply)
  : MessageHandler =
  fun message -> task {
    match message.Text with
    | Equals Buttons.ExcludePlaylist ->
      let! musicPlatform = musicPlatformFactory.GetMusicPlatform(message.Chat.UserId.ToMusicPlatformId())

      match musicPlatform with
      | Some _ ->
        do! chatCtx.AskForReply resp[Messages.SendExcludedPlaylist]

        return Some()
      | _ ->
        do! sendLoginMessage authService resp chatCtx message.Chat.UserId &|> ignore

        return Some()
    | _ -> return None
  }

let targetPlaylistButtonMessageHandler
  (musicPlatformFactory: IMusicPlatformFactory)
  authService
  (resp: IResourceProvider)
  (chatCtx: #IBotService)
  : MessageHandler =
  fun message -> task {
    match message.Text with
    | Equals Buttons.TargetPlaylist ->

      let! musicPlatform = musicPlatformFactory.GetMusicPlatform(message.Chat.UserId.ToMusicPlatformId())

      match musicPlatform with
      | Some _ ->
        do! chatCtx.AskForReply resp[Messages.SendTargetedPlaylist]

        return Some()
      | _ ->
        do! sendLoginMessage authService resp chatCtx message.Chat.UserId &|> ignore

        return Some()
    | _ -> return None
  }

let includePlaylistMessageHandler
  (userRepo: #ILoadUser)
  (presetService: #IIncludePlaylist)
  authService
  (resp: IResourceProvider)
  (chatCtx: #ISendMessage)
  : MessageHandler =
  let includePlaylist =
    fun userId rawPlaylistId -> task {
      let! currentPresetId = userRepo.LoadUser userId |> Task.map (fun u -> u.CurrentPresetId |> Option.get)

      let includePlaylistResult =
        presetService.IncludePlaylist(userId, currentPresetId, rawPlaylistId)

      let onSuccess (playlist: IncludedPlaylist) =
        chatCtx.SendMessage($"*{playlist.Name}* successfully included into current preset!")

      let onError =
        function
        | Preset.IncludePlaylistError.IdParsing(Playlist.IdParsingError id) ->
          chatCtx.SendMessage(String.Format(Messages.PlaylistIdCannotBeParsed, id))
        | Preset.IncludePlaylistError.Load(Playlist.LoadError.NotFound) ->
          let (Playlist.RawPlaylistId rawPlaylistId) = rawPlaylistId

          chatCtx.SendMessage(String.Format(Messages.PlaylistNotFoundInSpotify, rawPlaylistId))
        | Preset.IncludePlaylistError.Unauthorized -> sendLoginMessage authService resp chatCtx userId

      return! includePlaylistResult |> TaskResult.taskEither onSuccess onError |> Task.ignore
    }

  fun message -> task {
    match message with
    | { Text = text
        ReplyMessage = Some { Text = replyText } } when replyText = resp[Messages.SendIncludedPlaylist] ->
      do! includePlaylist message.Chat.UserId (Playlist.RawPlaylistId text)

      return Some()
    | { Text = CommandWithData "/include" text } ->
      do! includePlaylist message.Chat.UserId (Playlist.RawPlaylistId text)

      return Some()
    | _ -> return None
  }

let excludePlaylistMessageHandler
  (userRepo: #ILoadUser)
  (presetService: #IExcludePlaylist)
  authService
  (resp: IResourceProvider)
  (chatCtx: #ISendMessage)
  : MessageHandler =
  let excludePlaylist =
    fun userId rawPlaylistId -> task {
      let! currentPresetId = userRepo.LoadUser userId |> Task.map (fun u -> u.CurrentPresetId |> Option.get)

      let excludePlaylistResult =
        presetService.ExcludePlaylist(userId, currentPresetId, rawPlaylistId)

      let onSuccess (playlist: ExcludedPlaylist) =
        chatCtx.SendMessage $"*{playlist.Name}* successfully excluded from current preset!"

      let onError =
        function
        | Preset.ExcludePlaylistError.IdParsing(Playlist.IdParsingError id) ->
          chatCtx.SendMessage(String.Format(Messages.PlaylistIdCannotBeParsed, id))
        | Preset.ExcludePlaylistError.Load(Playlist.LoadError.NotFound) ->
          let (Playlist.RawPlaylistId rawPlaylistId) = rawPlaylistId
          chatCtx.SendMessage(String.Format(Messages.PlaylistNotFoundInSpotify, rawPlaylistId))
        | Preset.ExcludePlaylistError.Unauthorized -> sendLoginMessage authService resp chatCtx userId

      return! excludePlaylistResult |> TaskResult.taskEither onSuccess onError |> Task.ignore
    }

  fun message -> task {
    match message with
    | { Text = text
        ReplyMessage = Some { Text = replyText } } when replyText = resp[Messages.SendExcludedPlaylist] ->
      do! excludePlaylist message.Chat.UserId (Playlist.RawPlaylistId text)

      return Some()
    | { Text = CommandWithData "/exclude" text } ->
      do! excludePlaylist message.Chat.UserId (Playlist.RawPlaylistId text)

      return Some()
    | _ -> return None
  }

let targetPlaylistMessageHandler
  (userRepo: #ILoadUser)
  (presetService: #ITargetPlaylist)
  authService
  (resp: IResourceProvider)
  (chatCtx: #ISendMessage)
  : MessageHandler =
  let targetPlaylist =
    fun userId rawPlaylistId -> task {
      let! currentPresetId = userRepo.LoadUser userId |> Task.map (fun u -> u.CurrentPresetId |> Option.get)

      let targetPlaylistResult =
        presetService.TargetPlaylist(userId, currentPresetId, rawPlaylistId)

      let onSuccess (playlist: TargetedPlaylist) =
        chatCtx.SendMessage $"*{playlist.Name}* successfully targeted for current preset!"

      let onError =
        function
        | Preset.TargetPlaylistError.IdParsing(Playlist.IdParsingError id) ->
          chatCtx.SendMessage(String.Format(Messages.PlaylistIdCannotBeParsed, id))
        | Preset.TargetPlaylistError.Load(Playlist.LoadError.NotFound) ->
          let (Playlist.RawPlaylistId rawPlaylistId) = rawPlaylistId
          chatCtx.SendMessage(String.Format(Messages.PlaylistNotFoundInSpotify, rawPlaylistId))
        | Preset.TargetPlaylistError.AccessError _ -> chatCtx.SendMessage resp[Messages.PlaylistIsReadonly]
        | Preset.TargetPlaylistError.Unauthorized -> sendLoginMessage authService resp chatCtx userId

      return! targetPlaylistResult |> TaskResult.taskEither onSuccess onError |> Task.ignore
    }

  fun message -> task {
    match message with
    | { Text = text
        ReplyMessage = Some { Text = replyText } } when replyText = resp[Messages.SendTargetedPlaylist] ->
      do! targetPlaylist message.Chat.UserId (Playlist.RawPlaylistId text)

      return Some()
    | { Text = CommandWithData "/target" text } ->
      do! targetPlaylist message.Chat.UserId (Playlist.RawPlaylistId text)

      return Some()
    | _ -> return None
  }

let queuePresetRunMessageHandler userRepo presetService (resp: IResourceProvider) chatCtx : MessageHandler =
  fun message -> task {

    match message.Text with
    | Equals "/run"
    | Equals Buttons.RunPreset ->
      do! User.queueCurrentPresetRun userRepo chatCtx presetService message.Chat.UserId

      return Some()
    | _ -> return None
  }