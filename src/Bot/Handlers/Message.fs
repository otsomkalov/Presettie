module Bot.Handlers.Message

open Domain.Repos
open MusicPlatform
open Bot.Constants
open otsom.fs.Auth
open otsom.fs.Bot
open otsom.fs.Extensions.String
open otsom.fs.Extensions
open Bot.Workflows
open Domain.Core
open Bot.Helpers
open Domain.Workflows
open System
open otsom.fs.Resources
open Bot.Core
open Bot.Resources
open FsToolkit.ErrorHandling

let startMessageHandler
  (userRepo: #ILoadUser)
  (presetRepo: #ILoadPreset)
  (authService: #ICompleteAuth)
  (resp: IResourceProvider)
  (chatCtx: #ISendMessage)
  : MessageHandler =
  fun message -> task {
    match message with
    | { Text = Equals Commands.start } ->
      do! User.sendCurrentPreset resp userRepo presetRepo chatCtx message.Chat.UserId

      return Some()
    | { Text = CommandWithData Commands.start state } ->
      let! user = userRepo.LoadUser message.Chat.UserId

      let processSuccessfulLogin =
        fun () -> chatCtx.SendMessage resp[Messages.SuccessfulLogin] |> Task.map ignore

      let sendErrorMessage =
        function
        | CompleteError.StateNotFound -> chatCtx.SendMessage resp[Messages.StateNotFound]
        | CompleteError.StateDoesntBelongToUser -> chatCtx.SendMessage resp[Messages.OtherUserState]

      do!
        authService.CompleteAuth(user.Id.ToAccountId(), State.Parse state)
        |> TaskResult.taskEither processSuccessfulLogin (sendErrorMessage >> Task.ignore)

      return Some()
    | _ -> return None
  }

let faqMessageHandler (resp: IResourceProvider) (chatCtx: #ISendMessage) : MessageHandler =
  fun message -> task {
    match message.Text with
    | Equals Commands.faq ->
      do! chatCtx.SendMessage(resp[Messages.FAQ]) |> Task.map ignore

      return Some()
    | _ -> return None
  }

let privacyMessageHandler (resp: IResourceProvider) (chatCtx: #ISendMessage) : MessageHandler =
  fun message -> task {
    match message.Text with
    | Equals Commands.privacy ->
      do! chatCtx.SendMessage(resp[Messages.Privacy]) |> Task.map ignore

      return Some()
    | _ -> return None
  }

let guideMessageHandler (resp: IResourceProvider) (chatCtx: #ISendMessage) : MessageHandler =
  fun message -> task {
    match message.Text with
    | Equals Commands.guide ->
      do! chatCtx.SendMessage(resp[Messages.Guide]) |> Task.map ignore

      return Some()
    | _ -> return None
  }

let helpMessageHandler (resp: IResourceProvider) (chatCtx: #ISendMessage) : MessageHandler =
  fun message -> task {
    match message.Text with
    | Equals Commands.help ->
      do! chatCtx.SendMessage(resp[Messages.Help]) |> Task.map ignore

      return Some()
    | _ -> return None
  }

let myPresetsMessageHandler presetRepo (resp: IResourceProvider) (chatCtx: #ISendMessageButtons) : MessageHandler =
  let sendUserPresets = User.sendPresets resp chatCtx presetRepo

  fun message -> task {
    match message.Text with
    | Equals(resp.Item(Buttons.MyPresets)) ->
      do! sendUserPresets message.Chat.UserId
      return Some()
    | Equals Commands.presets ->
      do! sendUserPresets message.Chat.UserId
      return Some()
    | _ -> return None
  }

let backMessageButtonHandler loadUser getPreset (resp: IResourceProvider) (chatCtx: #ISendKeyboard) : MessageHandler =
  fun message -> task {
    match message.Text with
    | Equals(resp.Item(Buttons.Back)) ->
      do! User.sendCurrentPreset resp loadUser getPreset chatCtx message.Chat.UserId

      return Some()
    | _ -> return None
  }

let presetSettingsMessageHandler userRepo presetRepo (resp: IResourceProvider) chatCtx : MessageHandler =
  let sendSettingsMessage =
    User.sendCurrentPresetSettings resp userRepo presetRepo chatCtx

  fun message -> task {
    match message.Text with
    | Equals(resp.Item(Buttons.Settings)) ->
      do! sendSettingsMessage message.Chat.UserId

      return Some()
    | _ -> return None
  }

let setPresetSizeMessageButtonHandler (resp: IResourceProvider) (chatCtx: #IAskForReply) : MessageHandler =
  fun message -> task {
    match message.Text with
    | Equals(resp.Item(Buttons.SetPresetSize)) ->
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
        ReplyMessage = Some { Text = replyText } } when replyText = resp[Buttons.SetPresetSize] ->
      do!
        userService.SetCurrentPresetSize(message.Chat.UserId, (PresetSettings.RawPresetSize text))
        |> TaskResult.taskEither (onSuccess message.Chat.UserId) (onError >> Task.ignore)

      return Some()
    | { Text = CommandWithData Commands.size text } ->
      do!
        (userService.SetCurrentPresetSize(message.Chat.UserId, (PresetSettings.RawPresetSize text))
         |> TaskResult.taskEither (onSuccess message.Chat.UserId) (onError >> Task.ignore))

      return Some()
    | _ -> return None
  }

let createPresetButtonMessageHandler (resp: IResourceProvider) (chatCtx: #IAskForReply) : MessageHandler =
  fun message -> task {
    match message.Text with
    | Equals(resp.Item(Buttons.CreatePreset)) ->
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
    | { Text = CommandWithData Commands.newPreset text } ->
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
    | Equals(resp.Item(Buttons.IncludePlaylist)) ->
      let! musicPlatform = musicPlatformFactory.GetMusicPlatform(message.Chat.UserId.ToMusicPlatformId())

      match musicPlatform with
      | Some _ ->
        do! chatCtx.AskForReply resp[Messages.SendIncludedPlaylist]

        return Some()
      | _ ->
        do! sendLoginMessage authService resp chatCtx message.Chat.UserId |> Task.map ignore

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
    | Equals(resp.Item(Buttons.ExcludePlaylist)) ->
      let! musicPlatform = musicPlatformFactory.GetMusicPlatform(message.Chat.UserId.ToMusicPlatformId())

      match musicPlatform with
      | Some _ ->
        do! chatCtx.AskForReply resp[Messages.SendExcludedPlaylist]

        return Some()
      | _ ->
        do! sendLoginMessage authService resp chatCtx message.Chat.UserId |> Task.map ignore

        return Some()
    | _ -> return None
  }

let excludeArtistButtonMessageHandler
  (musicPlatformFactory: IMusicPlatformFactory)
  authService
  (resp: IResourceProvider)
  (chatCtx: #IAskForReply)
  : MessageHandler =
  fun message -> task {
    match message.Text with
    | Equals(resp.Item(Buttons.ExcludeArtist)) ->
      let! musicPlatform = musicPlatformFactory.GetMusicPlatform(message.Chat.UserId.ToMusicPlatformId())

      match musicPlatform with
      | Some _ ->
        do! chatCtx.AskForReply resp[Messages.SendExcludedArtist]

        return Some()
      | _ ->
        do! sendLoginMessage authService resp chatCtx message.Chat.UserId |> Task.map ignore

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
    | Equals(resp.Item(Buttons.TargetPlaylist)) ->

      let! musicPlatform = musicPlatformFactory.GetMusicPlatform(message.Chat.UserId.ToMusicPlatformId())

      match musicPlatform with
      | Some _ ->
        do! chatCtx.AskForReply resp[Messages.SendTargetedPlaylist]

        return Some()
      | _ ->
        do! sendLoginMessage authService resp chatCtx message.Chat.UserId |> Task.map ignore

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
        chatCtx.SendMessage resp[Messages.PlaylistIncluded, [| playlist.Name |]]

      let onError =
        function
        | Preset.IncludePlaylistError.IdParsing(Playlist.IdParsingError id) ->
          chatCtx.SendMessage resp[Messages.PlaylistIdCannotBeParsed, [| id |]]
        | Preset.IncludePlaylistError.Load(Playlist.LoadError.NotFound) ->
          let (Playlist.RawPlaylistId rawPlaylistId) = rawPlaylistId

          chatCtx.SendMessage resp[Messages.PlaylistNotFoundInSpotify, [| rawPlaylistId |]]
        | Preset.IncludePlaylistError.Unauthorized -> sendLoginMessage authService resp chatCtx userId

      return! includePlaylistResult |> TaskResult.taskEither onSuccess onError |> Task.ignore
    }

  fun message -> task {
    match message with
    | { Text = text
        ReplyMessage = Some { Text = replyText } } when replyText = resp[Messages.SendIncludedPlaylist] ->
      do! includePlaylist message.Chat.UserId (Playlist.RawPlaylistId text)

      return Some()
    | { Text = CommandWithData Commands.includePlaylist text } ->
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
        chatCtx.SendMessage resp[Messages.PlaylistExcluded, [| playlist.Name |]]

      let onError =
        function
        | Preset.ExcludePlaylistError.IdParsing(Playlist.IdParsingError id) ->
          chatCtx.SendMessage resp[Messages.PlaylistIdCannotBeParsed, [| id |]]
        | Preset.ExcludePlaylistError.Load(Playlist.LoadError.NotFound) ->
          let (Playlist.RawPlaylistId rawPlaylistId) = rawPlaylistId
          chatCtx.SendMessage resp[Messages.PlaylistNotFoundInSpotify, [| rawPlaylistId |]]
        | Preset.ExcludePlaylistError.Unauthorized -> sendLoginMessage authService resp chatCtx userId

      return! excludePlaylistResult |> TaskResult.taskEither onSuccess onError |> Task.ignore
    }

  fun message -> task {
    match message with
    | { Text = text
        ReplyMessage = Some { Text = replyText } } when replyText = resp[Messages.SendExcludedPlaylist] ->
      do! excludePlaylist message.Chat.UserId (Playlist.RawPlaylistId text)

      return Some()
    | { Text = CommandWithData Commands.excludePlaylist text } ->
      do! excludePlaylist message.Chat.UserId (Playlist.RawPlaylistId text)

      return Some()
    | _ -> return None
  }

let excludeArtistMessageHandler
  (userRepo: #ILoadUser)
  (presetService: #IExcludeArtist)
  authService
  (resp: IResourceProvider)
  (chatCtx: #ISendMessage)
  : MessageHandler =
  let excludeArtist =
    fun userId rawArtistId -> task {
      let! currentPresetId = userRepo.LoadUser userId |> Task.map (fun u -> u.CurrentPresetId |> Option.get)

      let excludeArtistResult =
        presetService.ExcludeArtist(userId, currentPresetId, rawArtistId)

      let onSuccess (artist: ExcludedArtist) =
        chatCtx.SendMessage resp[Messages.ArtistExcluded, [| artist.Name |]]

      let onError =
        function
        | Preset.ExcludeArtistError.IdParsing(Artist.IdParsingError id) ->
          chatCtx.SendMessage resp[Messages.ArtistIdCannotBeParsed, [| id |]]
        | Preset.ExcludeArtistError.Load(Artist.LoadError.NotFound) ->
          let (Artist.RawArtistId rawArtistId) = rawArtistId
          chatCtx.SendMessage resp[Messages.ArtistNotFoundInSpotify, [| rawArtistId |]]
        | Preset.ExcludeArtistError.Unauthorized -> sendLoginMessage authService resp chatCtx userId

      return! excludeArtistResult |> TaskResult.taskEither onSuccess onError |> Task.ignore
    }

  fun message -> task {
    match message with
    | { Text = text
        ReplyMessage = Some { Text = replyText } } when replyText = resp[Messages.SendExcludedArtist] ->
      do! excludeArtist message.Chat.UserId (Artist.RawArtistId text)

      return Some()
    | { Text = CommandWithData Commands.excludeArtist text } ->
      do! excludeArtist message.Chat.UserId (Artist.RawArtistId text)

      return Some()
    | _ -> return None
  }

let includeArtistMessageHandler
  (userRepo: #ILoadUser)
  (presetService: #IIncludeArtist)
  authService
  (resp: IResourceProvider)
  (chatCtx: #ISendMessage)
  : MessageHandler =
  let includeArtist =
    fun userId rawArtistId -> task {
      let! currentPresetId = userRepo.LoadUser userId |> Task.map (fun u -> u.CurrentPresetId |> Option.get)

      let includeArtistResult =
        presetService.IncludeArtist(userId, currentPresetId, rawArtistId)

      let onSuccess (artist: IncludedArtist) =
        chatCtx.SendMessage resp[Messages.ArtistIncluded, [| artist.Name |]]

      let onError =
        function
        | Preset.IncludeArtistError.IdParsing(Artist.IdParsingError id) ->
          chatCtx.SendMessage resp[Messages.ArtistIdCannotBeParsed, [| id |]]
        | Preset.IncludeArtistError.Load(Artist.LoadError.NotFound) ->
          let (Artist.RawArtistId rawArtistId) = rawArtistId
          chatCtx.SendMessage resp[Messages.ArtistNotFoundInSpotify, [| rawArtistId |]]
        | Preset.IncludeArtistError.Unauthorized -> sendLoginMessage authService resp chatCtx userId

      return! includeArtistResult |> TaskResult.taskEither onSuccess onError |> Task.ignore
    }

  fun message -> task {
    match message with
    | { Text = text
        ReplyMessage = Some { Text = replyText } } when replyText = resp[Messages.SendIncludedArtist] ->
      do! includeArtist message.Chat.UserId (Artist.RawArtistId text)

      return Some()
    | { Text = CommandWithData Commands.includeArtist text } ->
      do! includeArtist message.Chat.UserId (Artist.RawArtistId text)

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
        chatCtx.SendMessage resp[Messages.PlaylistTargeted, [| playlist.Name |]]

      let onError =
        function
        | Preset.TargetPlaylistError.IdParsing(Playlist.IdParsingError id) ->
          chatCtx.SendMessage resp[Messages.PlaylistIdCannotBeParsed, [| id |]]
        | Preset.TargetPlaylistError.Load(Playlist.LoadError.NotFound) ->
          let (Playlist.RawPlaylistId rawPlaylistId) = rawPlaylistId
          chatCtx.SendMessage resp[Messages.PlaylistNotFoundInSpotify, [| rawPlaylistId |]]
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
    | { Text = CommandWithData Commands.targetPlaylist text } ->
      do! targetPlaylist message.Chat.UserId (Playlist.RawPlaylistId text)

      return Some()
    | _ -> return None
  }

let queuePresetRunMessageHandler userRepo presetService (resp: IResourceProvider) chatCtx : MessageHandler =
  let queueRun = User.queueCurrentPresetRun resp userRepo chatCtx presetService

  fun message -> task {
    match message.Text with
    | Equals Commands.runPreset ->
      do! queueRun message.Chat.UserId
      return Some()
    | Equals(resp.Item(Buttons.RunPreset)) ->
      do! queueRun message.Chat.UserId
      return Some()
    | _ -> return None
  }