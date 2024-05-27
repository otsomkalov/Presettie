﻿module Infrastructure.Telegram.Services

open System.Reflection
open Resources
open Telegram
open Infrastructure
open Infrastructure.Telegram.Helpers
open Infrastructure.Workflows
open System.Collections.Generic
open System.Threading.Tasks
open Azure.Storage.Queues
open Domain.Core
open Domain.Workflows
open Infrastructure.Spotify
open Microsoft.Extensions.Options
open MongoDB.Driver
open SpotifyAPI.Web
open StackExchange.Redis
open Telegram.Bot
open Telegram.Bot.Types
open Telegram.Bot.Types.Enums
open Telegram.Core
open System
open otsom.fs.Extensions
open otsom.fs.Extensions.String
open otsom.fs.Telegram.Bot.Auth.Spotify
open otsom.fs.Telegram.Bot.Auth.Spotify.Settings
open otsom.fs.Telegram.Bot.Auth.Spotify.Workflows
open otsom.fs.Telegram.Bot.Core
open Infrastructure.Repos

type SpotifyClientProvider(createClientFromTokenResponse: CreateClientFromTokenResponse, loadCompletedAuth: Completed.Load) =
  let _clientsByTelegramId =
    Dictionary<int64, ISpotifyClient>()

  member this.GetAsync userId : Task<ISpotifyClient> =
    let userId' = userId |> UserId.value

    if _clientsByTelegramId.ContainsKey(userId') then
      _clientsByTelegramId[userId'] |> Task.FromResult
    else
      task {
        let! auth = loadCompletedAuth userId

        return!
          match auth with
          | None -> Task.FromResult null
          | Some auth ->
            let client =
              AuthorizationCodeTokenResponse(RefreshToken = auth.Token)
              |> createClientFromTokenResponse

            this.SetClient(userId', client)

            client |> Task.FromResult
      }

  member this.SetClient(telegramId: int64, client: ISpotifyClient) =
    if _clientsByTelegramId.ContainsKey(telegramId) then
      ()
    else
      (telegramId, client) |> _clientsByTelegramId.Add

type AuthState =
  | Authorized
  | Unauthorized

type MessageService
  (
    _spotifyClientProvider: SpotifyClientProvider,
    _bot: ITelegramBotClient,
    _database: IMongoDatabase,
    _connectionMultiplexer: IConnectionMultiplexer,
    _spotifyOptions: IOptions<SpotifySettings>,
    _queueClient: QueueClient,
    initAuth: Auth.Init,
    completeAuth: Auth.Complete,
    sendUserMessage: SendUserMessage,
    replyToUserMessage: ReplyToUserMessage,
    sendUserKeyboard: SendUserKeyboard,
    sendUserMessageButtons: SendUserMessageButtons,
    askUserForReply: AskUserForReply
  ) =

  let sendUserPresets sendMessage (message: Message) getUser =
    let sendUserPresets = Telegram.Workflows.sendUserPresets sendMessage getUser
    sendUserPresets (message.From.Id |> UserId)

  member this.ProcessAsync(message: Message) =
    let userId = message.From.Id |> UserId

    let loadPreset = PresetRepo.load _database
    let updatePreset = PresetRepo.update _database
    let getPreset = Preset.get loadPreset

    let sendMessage = sendUserMessage userId
    let sendLink = Workflows.sendLink _bot userId
    let sendKeyboard = sendUserKeyboard userId
    let replyToMessage = replyToUserMessage userId message.MessageId
    let sendButtons = sendUserMessageButtons userId
    let askForReply = askUserForReply userId message.MessageId
    let savePreset = Preset.save _database
    let updateUser = User.update _database
    let loadUser = UserRepo.load _database
    let getUser = User.get loadUser

    let sendCurrentPresetInfo = Telegram.Workflows.sendCurrentPresetInfo getUser getPreset sendKeyboard
    let sendSettingsMessage = Telegram.Workflows.sendSettingsMessage getUser getPreset sendKeyboard

    let sendLoginMessage () =
      initAuth userId [ Scopes.PlaylistModifyPrivate; Scopes.PlaylistModifyPublic; Scopes.UserLibraryRead ]
      |> Task.bind (sendLink Messages.LoginToSpotify Buttons.Login)

    task {
      let! spotifyClient = _spotifyClientProvider.GetAsync userId

      let authState =
        if spotifyClient = null then
          AuthState.Unauthorized
        else
          AuthState.Authorized

      let parsePlaylistId = Playlist.parseId
      let loadFromSpotify = Playlist.loadFromSpotify spotifyClient

      return!
        match message.Type with
        | MessageType.Text ->
          let includePlaylist = Playlist.includePlaylist parsePlaylistId loadFromSpotify getPreset updatePreset
          let includePlaylist = Workflows.Playlist.includePlaylist replyToMessage getUser includePlaylist

          let excludePlaylist = Playlist.excludePlaylist parsePlaylistId loadFromSpotify getPreset updatePreset
          let excludePlaylist = Workflows.Playlist.excludePlaylist replyToMessage getUser excludePlaylist

          let targetPlaylist = Playlist.targetPlaylist parsePlaylistId loadFromSpotify getPreset updatePreset
          let targetPlaylist = Workflows.Playlist.targetPlaylist replyToMessage getUser targetPlaylist

          let queueGeneration = Workflows.Playlist.queueGeneration _queueClient replyToMessage getUser getPreset Preset.validate

          match isNull message.ReplyToMessage with
          | false ->
            match (message.ReplyToMessage.Text, authState) with
            | Equals Messages.SendIncludedPlaylist, Unauthorized
            | Equals Messages.SendExcludedPlaylist, Unauthorized
            | Equals Messages.SendTargetedPlaylist, Unauthorized -> sendLoginMessage()

            | Equals Messages.SendPlaylistSize, _ ->
              match message.Text with
              | Int size ->
                let setPlaylistSize = Preset.setPlaylistSize getPreset updatePreset
                let setPlaylistSize = Workflows.setPlaylistSize sendMessage sendSettingsMessage getUser setPlaylistSize

                setPlaylistSize userId size
              | _ ->
                replyToMessage Messages.WrongPlaylistSize
                |> Task.ignore
            | Equals Messages.SendIncludedPlaylist, Authorized ->
              includePlaylist userId (Playlist.RawPlaylistId message.Text)
            | Equals Messages.SendExcludedPlaylist, Authorized ->
              excludePlaylist userId (Playlist.RawPlaylistId message.Text)
            | Equals Messages.SendTargetedPlaylist, Authorized ->
              targetPlaylist userId (Playlist.RawPlaylistId message.Text)
            | Equals Messages.SendPresetName, _ ->
              let createPreset = Preset.create savePreset getUser updateUser userId
              let sendPresetInfo = Telegram.Workflows.sendPresetInfo getPreset sendButtons
              let createPreset = Telegram.Workflows.Message.createPreset createPreset sendPresetInfo

              createPreset message.Text

            | _ ->
              replyToMessage "Unknown command"
              |> Task.ignore
          | _ ->
            match (message.Text, authState) with
            | StartsWith "/include", Unauthorized | StartsWith "/exclude", Unauthorized | StartsWith "/target", Unauthorized
            | Equals Buttons.IncludePlaylist, Unauthorized | Equals Buttons.ExcludePlaylist, Unauthorized | Equals Buttons.TargetPlaylist, Unauthorized
            | Equals Buttons.GeneratePlaylist, Unauthorized | StartsWith "/generate", Unauthorized | Equals "/start", Unauthorized
             -> sendLoginMessage()

            | Equals "/start", Authorized ->
              sendCurrentPresetInfo userId
            | CommandWithData "/start" state, _ ->
              let processSuccessfulLogin =
                let createUserIfNotExists = User.createIfNotExists _database
                fun () ->
                  task{
                    do! createUserIfNotExists userId
                    do! sendCurrentPresetInfo userId
                  }

              let sendErrorMessage =
                function
                | Auth.CompleteError.StateNotFound ->
                  replyToMessage "State not found. Try to login via fresh link."
                | Auth.CompleteError.StateDoesntBelongToUser ->
                  replyToMessage "State provided does not belong to your login request. Try to login via fresh link."

              completeAuth userId state
              |> TaskResult.taskEither processSuccessfulLogin (sendErrorMessage >> Task.ignore)
            | Equals "/help", _ ->
              sendMessage Messages.Help
            | Equals "/guide", _ -> sendMessage Messages.Guide
            | Equals "/privacy", _ -> sendMessage Messages.Privacy
            | Equals "/faq", _ -> sendMessage Messages.FAQ
            | Equals "/generate", Authorized -> queueGeneration userId
            | Equals "/version", Authorized ->
              sendMessage (Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion)
            | CommandWithData "/include" rawPlaylistId, Authorized ->
              if String.IsNullOrEmpty rawPlaylistId then
                replyToMessage "You have entered empty playlist url"
                |> Task.ignore
              else
                includePlaylist userId (rawPlaylistId |> Playlist.RawPlaylistId)
                |> Task.ignore
            | CommandWithData "/exclude" rawPlaylistId, Authorized ->
              if String.IsNullOrEmpty rawPlaylistId then
                replyToMessage "You have entered empty playlist url"
                |> Task.ignore
              else
                excludePlaylist userId (rawPlaylistId |> Playlist.RawPlaylistId)
            | CommandWithData "/target" rawPlaylistId, Authorized ->
              if String.IsNullOrEmpty rawPlaylistId then
                replyToMessage "You have entered empty playlist url"
                |> Task.ignore
              else
                targetPlaylist userId (rawPlaylistId |> Playlist.RawPlaylistId)
            | Equals Buttons.SetPlaylistSize, _ -> askForReply Messages.SendPlaylistSize
            | Equals Buttons.CreatePreset, _ -> askForReply Messages.SendPresetName
            | Equals Buttons.GeneratePlaylist, Authorized -> queueGeneration userId
            | Equals Buttons.MyPresets, _ -> sendUserPresets sendButtons message getUser
            | Equals Buttons.Settings, _ -> sendSettingsMessage userId
            | Equals Buttons.IncludePlaylist, Authorized -> askForReply Messages.SendIncludedPlaylist
            | Equals Buttons.ExcludePlaylist, Authorized -> askForReply Messages.SendExcludedPlaylist
            | Equals Buttons.TargetPlaylist, Authorized -> askForReply Messages.SendTargetedPlaylist
            | Equals "Back", _ -> sendCurrentPresetInfo userId

            | _ ->
              replyToMessage "Unknown command"
              |> Task.ignore
        | _ -> Task.FromResult()
    }

type CallbackQueryService
  (
    _bot: ITelegramBotClient,
    _queueClient: QueueClient,
    _connectionMultiplexer: IConnectionMultiplexer,
    _database: IMongoDatabase,
    editBotMessageButtons: EditBotMessageButtons
  ) =

  member this.ProcessAsync(callbackQuery: CallbackQuery) =
    let updatePreset = PresetRepo.update _database

    let userId = callbackQuery.From.Id |> UserId
    let botMessageId = callbackQuery.Message.MessageId |> BotMessageId

    let updateUser = User.update _database
    let editMessageButtons = editBotMessageButtons userId botMessageId
    let answerCallbackQuery = Workflows.answerCallbackQuery _bot callbackQuery.Id
    let countPlaylistTracks = Playlist.countTracks _connectionMultiplexer
    let loadUser = UserRepo.load _database
    let getUser = User.get loadUser

    let showUserPresets = Workflows.sendUserPresets editMessageButtons getUser

    let loadPreset = PresetRepo.load _database
    let getPreset = Preset.get loadPreset

    let sendPresetInfo = Workflows.sendPresetInfo getPreset editMessageButtons

    let showIncludedPlaylists = Workflows.showIncludedPlaylists getPreset editMessageButtons
    let showExcludedPlaylists = Workflows.showExcludedPlaylists getPreset editMessageButtons
    let showTargetedPlaylists = Workflows.showTargetedPlaylists getPreset editMessageButtons

    let showIncludedPlaylist = Workflows.showIncludedPlaylist editMessageButtons getPreset countPlaylistTracks
    let showExcludedPlaylist = Workflows.showExcludedPlaylist editMessageButtons getPreset countPlaylistTracks
    let showTargetedPlaylist = Workflows.showTargetedPlaylist editMessageButtons getPreset countPlaylistTracks

    match callbackQuery.Data |> Workflows.parseAction with
    | Action.ShowPresetInfo presetId -> sendPresetInfo presetId
    | Action.SetCurrentPreset presetId ->
      let setCurrentPreset = Domain.Workflows.User.setCurrentPreset getUser updateUser
      let setCurrentPreset = Workflows.setCurrentPreset answerCallbackQuery setCurrentPreset

      setCurrentPreset userId presetId
    | Action.RemovePreset presetId ->
      let removePreset = PresetRepo.remove _database
      let removeUserPreset = Domain.Workflows.User.removePreset getUser removePreset updateUser
      let removeUserPreset = Telegram.Workflows.User.removePreset removeUserPreset showUserPresets
      removeUserPreset userId presetId
    | Action.ShowIncludedPlaylists(presetId, page) -> showIncludedPlaylists presetId page
    | Action.ShowIncludedPlaylist(presetId, playlistId) -> showIncludedPlaylist presetId playlistId
    | Action.EnableIncludedPlaylist(presetId, playlistId) ->
      let enableIncludedPlaylist = IncludedPlaylist.enable getPreset updatePreset
      let enableIncludedPlaylist = Workflows.IncludedPlaylist.enable enableIncludedPlaylist answerCallbackQuery showIncludedPlaylist

      enableIncludedPlaylist presetId playlistId
    | Action.DisableIncludedPlaylist(presetId, playlistId) ->
      let disableIncludedPlaylist = IncludedPlaylist.disable getPreset updatePreset
      let disableIncludedPlaylist = Workflows.IncludedPlaylist.disable disableIncludedPlaylist answerCallbackQuery showIncludedPlaylist

      disableIncludedPlaylist presetId playlistId
    | Action.RemoveIncludedPlaylist(presetId, playlistId) ->
      let removeIncludedPlaylist = IncludedPlaylist.remove getPreset updatePreset
      let removeIncludedPlaylist = Workflows.removeIncludedPlaylist removeIncludedPlaylist answerCallbackQuery showIncludedPlaylists

      removeIncludedPlaylist presetId playlistId
    | Action.ShowExcludedPlaylists(presetId, page) -> showExcludedPlaylists presetId page
    | Action.ShowExcludedPlaylist(presetId, playlistId) -> showExcludedPlaylist presetId playlistId
    | Action.EnableExcludedPlaylist(presetId, playlistId) ->
      let enableExcludedPlaylist = ExcludedPlaylist.enable getPreset updatePreset
      let enableExcludedPlaylist = Workflows.ExcludedPlaylist.enable enableExcludedPlaylist answerCallbackQuery showExcludedPlaylist

      enableExcludedPlaylist presetId playlistId
    | Action.DisableExcludedPlaylist(presetId, playlistId) ->
      let disableExcludedPlaylist = ExcludedPlaylist.disable getPreset updatePreset
      let disableExcludedPlaylist = Workflows.ExcludedPlaylist.disable disableExcludedPlaylist answerCallbackQuery showExcludedPlaylist

      disableExcludedPlaylist presetId playlistId
    | Action.RemoveExcludedPlaylist(presetId, playlistId) ->
      let removeExcludedPlaylist = ExcludedPlaylist.remove getPreset updatePreset
      let removeExcludedPlaylist = Workflows.removeExcludedPlaylist removeExcludedPlaylist answerCallbackQuery showExcludedPlaylists

      removeExcludedPlaylist presetId playlistId
    | Action.ShowTargetedPlaylists(presetId, page) -> showTargetedPlaylists presetId page
    | Action.ShowTargetedPlaylist(presetId, playlistId) -> showTargetedPlaylist presetId playlistId
    | Action.AppendToTargetedPlaylist(presetId, playlistId) ->
      let appendToTargetedPlaylist = TargetedPlaylist.appendTracks getPreset updatePreset
      let appendToTargetedPlaylist = Workflows.appendToTargetedPlaylist appendToTargetedPlaylist answerCallbackQuery showTargetedPlaylist

      appendToTargetedPlaylist presetId playlistId
    | Action.OverwriteTargetedPlaylist(presetId, playlistId) ->
      let overwriteTargetedPlaylist = TargetedPlaylist.overwriteTracks getPreset updatePreset
      let overwriteTargetedPlaylist = Workflows.overwriteTargetedPlaylist overwriteTargetedPlaylist answerCallbackQuery showTargetedPlaylist

      overwriteTargetedPlaylist presetId playlistId
    | Action.RemoveTargetedPlaylist(presetId, playlistId) ->
      let removeTargetedPlaylist = TargetedPlaylist.remove getPreset updatePreset
      let removeTargetedPlaylist = Workflows.removeTargetedPlaylist removeTargetedPlaylist answerCallbackQuery showTargetedPlaylists

      removeTargetedPlaylist presetId playlistId
    | Action.IncludeLikedTracks presetId ->
      let includeLikedTracks = Preset.includeLikedTracks getPreset updatePreset
      let includeLikedTracks = Workflows.includeLikedTracks answerCallbackQuery sendPresetInfo includeLikedTracks

      includeLikedTracks presetId
    | Action.ExcludeLikedTracks presetId ->
      let excludeLikedTracks = Preset.excludeLikedTracks getPreset updatePreset
      let excludeLikedTracks = Workflows.excludeLikedTracks answerCallbackQuery sendPresetInfo excludeLikedTracks

      excludeLikedTracks presetId
    | Action.IgnoreLikedTracks presetId ->
      let ignoreLikedTracks = Preset.ignoreLikedTracks getPreset updatePreset
      let ignoreLikedTracks = Workflows.ignoreLikedTracks answerCallbackQuery sendPresetInfo ignoreLikedTracks

      ignoreLikedTracks presetId
    | Action.EnableRecommendations presetId ->
      let enableRecommendations = Preset.enableRecommendations getPreset updatePreset
      let enableRecommendations = Workflows.enableRecommendations enableRecommendations answerCallbackQuery sendPresetInfo

      enableRecommendations presetId
    | Action.DisableRecommendations presetId ->
      let disableRecommendations = Preset.disableRecommendations getPreset updatePreset
      let disableRecommendations =
        Workflows.disableRecommendations disableRecommendations answerCallbackQuery sendPresetInfo

      disableRecommendations presetId
    | Action.EnableUniqueArtists presetId ->
      let enableUniqueArtists = Preset.enableUniqueArtists loadPreset updatePreset
      let enableUniqueArtists = Workflows.enableUniqueArtists enableUniqueArtists answerCallbackQuery sendPresetInfo

      enableUniqueArtists presetId
    | Action.DisableUniqueArtists presetId ->
      let disableUniqueArtists = Preset.disableUniqueArtists loadPreset updatePreset
      let disableUniqueArtists =
        Workflows.disableUniqueArtists disableUniqueArtists answerCallbackQuery sendPresetInfo

      disableUniqueArtists presetId
    | Action.ShowUserPresets -> showUserPresets userId