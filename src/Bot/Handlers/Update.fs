module Bot.Handlers.Update

open System
open Bot.Core
open Bot.Handlers.Click
open Bot.Handlers.Message
open Bot.Repos
open Bot.Resources
open Bot.Workflows
open FsToolkit.ErrorHandling
open Microsoft.Extensions.Logging
open Runners.Shared
open otsom.fs.Bot
open otsom.fs.Bot.Builders
open otsom.fs.OAuth
open otsom.fs.Resources

let private buildMessageHandlers authService userRepo userService presetRepo resp botService buildMusicPlatform apiClient = messageHandlers {
  startMessageHandler userRepo presetRepo authService resp botService

  faqMessageHandler resp botService
  privacyMessageHandler resp botService
  guideMessageHandler resp botService
  helpMessageHandler resp botService

  myPresetsMessageHandler apiClient resp botService
  presetSettingsMessageHandler userRepo presetRepo resp botService
  queuePresetRunMessageHandler userRepo apiClient resp botService

  createPresetMessageHandler apiClient resp botService
  createPresetButtonMessageHandler resp botService

  setPresetSizeMessageButtonHandler resp botService
  setPresetSizeMessageHandler userService userRepo presetRepo resp botService

  includePlaylistButtonMessageHandler buildMusicPlatform authService resp botService
  excludePlaylistButtonMessageHandler buildMusicPlatform authService resp botService
  targetPlaylistButtonMessageHandler buildMusicPlatform authService resp botService

  excludeArtistButtonMessageHandler buildMusicPlatform authService resp botService

  includePlaylistMessageHandler userRepo apiClient authService resp botService
  includeArtistMessageHandler userRepo apiClient authService resp botService
  excludePlaylistMessageHandler userRepo apiClient authService resp botService
  excludeArtistMessageHandler userRepo apiClient authService resp botService
  targetPlaylistMessageHandler userRepo apiClient authService resp botService

  backMessageButtonHandler userRepo presetRepo resp botService
}

let private buildClickHandlers userService presetService presetRepo resp botService buildMusicPlatform (apiClient: IApiClient) =
  clickHandlers {
    listPresetsClickHandler apiClient resp botService
    presetInfoClickHandler presetRepo resp botService
    presetSettingsClickHandler presetRepo resp botService
    runPresetClickHandler apiClient resp botService
    removePresetClickHandler apiClient resp botService
    setCurrentPresetClickHandler userService resp botService

    artistsAlbumsRecommendationsClickHandler presetRepo presetService resp botService
    reccoBeatsRecommendationsClickHandler presetRepo presetService resp botService
    musicaeRecommendationsClickHandler presetRepo presetService resp botService
    spotifyRecommendationsClickHandler presetRepo presetService resp botService
    disableRecommendationsClickHandler presetRepo presetService resp botService

    enableUniqueArtistsClickHandler presetRepo presetService resp botService
    disableUniqueArtistsClickHandler presetRepo presetService resp botService

    includeLikedTracksClickHandler presetRepo presetService resp botService
    excludeLikedTracksClickHandler presetRepo presetService resp botService
    ignoreLikedTracksClickHandler presetRepo presetService resp botService

    appendToTargetedPlaylistClickHandler presetRepo presetService buildMusicPlatform resp botService
    overwriteTargetedPlaylistClickHandler presetRepo presetService buildMusicPlatform resp botService

    showIncludedContentClickHandler presetRepo resp botService
    showExcludedContentClickHandler presetRepo resp botService

    listIncludedArtistsClickHandler presetRepo resp botService
    listExcludedArtistsClickHandler presetRepo resp botService

    showIncludedArtistClickHandler presetRepo resp botService
    showExcludedArtistClickHandler presetRepo resp botService

    listIncludedPlaylistsClickHandler presetRepo resp botService
    listExcludedPlaylistsClickHandler presetRepo resp botService
    listTargetedPlaylistsClickHandler presetRepo resp botService

    showIncludedPlaylistClickHandler presetRepo buildMusicPlatform resp botService
    showExcludedPlaylistClickHandler presetRepo buildMusicPlatform resp botService
    showTargetedPlaylistClickHandler presetRepo buildMusicPlatform resp botService

    removeIncludedArtistClickHandler apiClient resp botService
    removeExcludedArtistClickHandler apiClient resp botService

    removeIncludedPlaylistClickHandler apiClient resp botService
    removeExcludedPlaylistClickHandler apiClient resp botService
    removeTargetedPlaylistClickHandler apiClient resp botService
  }

let private exceptionHandlingMiddleware (logger: ILogger) : BotMiddleware<Update> =
  fun next context -> task {
    try
      do! next context
    with e ->
      logger.LogError(e, "Error processing an update")
  }

let private authenticationMiddleware
  (buildDefaultResp: CreateDefaultResourceProvider)
  (appOAuthClient: IOAuthClient)
  (chatRepo: IChatRepo)
  (apiClientFactory: IApiClientFactory)
  : BotMiddleware<Update> =
  fun next context -> task {
    let! chat = chatRepo.LoadChat context.Update.ChatId
    let! apiClient = apiClientFactory.Create context.Update.ChatId

    match chat, apiClient with
    | Some chat, Some apiClient ->
      context.SetFeature chat
      context.SetFeature apiClient

      do! next context
    | _ ->
      let! resp = buildDefaultResp ()

      let! loginLink =
        appOAuthClient.InitAuth(context.Update.ChatId.Value |> string |> AccountId)

      return!
        context.Client.SendLink(resp[Messages.Login], resp[Buttons.Login], Uri(loginLink))
        |> Task.ignore
  }

let private resourceProviderEnricher (buildResp: CreateResourceProvider) : BotContextEnricher<Update> =
  fun context -> task {
    let chat = context.GetFeature<Chat>()
    let! resp = buildResp chat.Lang

    context.SetFeature resp
  }

let private contextEnricherMiddleware (enrichers: BotContextEnricher<Update> list) : BotMiddleware<Update> =
  fun next context -> task {
    for enricher in enrichers do
      do! enricher context

    do! next context
  }

let private router (logger: ILogger) buildMusicPlatform userService authService userRepo presetRepo presetService : BotHandler<Update> =
  fun context ->
    let chat = context.GetFeature<Chat>()
    let resp = context.GetFeature<IResourceProvider>()
    let apiClient = context.GetFeature<IApiClient>()

    task {
      match context.Update.Data with
      | Msg msg ->
        let! result =
          buildMessageHandlers authService userRepo userService presetRepo resp context.Client buildMusicPlatform apiClient chat msg

        match result with
        | Some() -> return ()
        | None ->
          logger.LogWarning "Message content didn't match any handler. Running default one."

          return! context.Client.SendMessage resp[Messages.UnknownCommand] |> Task.map ignore
      | Click click ->
        let! result =
          buildClickHandlers userService presetService presetRepo resp context.Client buildMusicPlatform apiClient chat click

        match result with
        | Some() -> return ()
        | None ->
          logger.LogWarning "Button click data didn't match any handler. Running default one."

          return! context.Client.SendNotification(click.Id, resp[Notifications.UnknownCommand])
    }

let main
  authService
  userRepo
  userService
  presetService
  presetRepo
  buildMusicPlatform
  buildBotClient
  chatRepo
  appOAuthClient
  apiClientFactory
  buildDefaultResp
  buildResp
  logger
  =
  fun (update: Update) -> task {
    let botSvc = buildBotClient update.ChatId
    let botCtx = BotContext(update, botSvc)

    let pipeline =
      exceptionHandlingMiddleware logger
      << authenticationMiddleware buildDefaultResp appOAuthClient chatRepo apiClientFactory
      << contextEnricherMiddleware [ resourceProviderEnricher buildResp ]
      <| router logger buildMusicPlatform userService authService userRepo presetRepo presetService

    do! pipeline botCtx
  }