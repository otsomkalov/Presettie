module Bot.Handlers.Update

open Bot.Core
open Bot.Handlers.Click
open Bot.Handlers.Message
open Bot.Repos
open Bot.Resources
open FsToolkit.ErrorHandling
open Microsoft.Extensions.Logging
open otsom.fs.Bot
open otsom.fs.Bot.Builders
open otsom.fs.Extensions

let private buildMessageHandlers authService userRepo userService presetService presetRepo resp botService buildMusicPlatform = messageHandlers {
  startMessageHandler userRepo presetRepo authService resp botService

  faqMessageHandler resp botService
  privacyMessageHandler resp botService
  guideMessageHandler resp botService
  helpMessageHandler resp botService

  myPresetsMessageHandler presetRepo resp botService
  presetSettingsMessageHandler userRepo presetRepo resp botService
  queuePresetRunMessageHandler userRepo presetService resp botService

  createPresetMessageHandler presetService resp botService
  createPresetButtonMessageHandler resp botService

  setPresetSizeMessageButtonHandler resp botService
  setPresetSizeMessageHandler userService userRepo presetRepo resp botService

  includePlaylistButtonMessageHandler buildMusicPlatform authService resp botService
  excludePlaylistButtonMessageHandler buildMusicPlatform authService resp botService
  targetPlaylistButtonMessageHandler buildMusicPlatform authService resp botService

  excludeArtistButtonMessageHandler buildMusicPlatform authService resp botService

  includePlaylistMessageHandler userRepo presetService authService resp botService
  includeArtistMessageHandler userRepo presetService authService resp botService
  excludePlaylistMessageHandler userRepo presetService authService resp botService
  excludeArtistMessageHandler userRepo presetService authService resp botService
  targetPlaylistMessageHandler userRepo presetService authService resp botService

  backMessageButtonHandler userRepo presetRepo resp botService
}

let private buildClickHandlers userService presetService presetRepo resp botService buildMusicPlatform = clickHandlers {
  listPresetsClickHandler presetRepo resp botService
  presetInfoClickHandler presetRepo resp botService
  presetSettingsClickHandler presetRepo resp botService
  runPresetClickHandler presetService resp botService
  removePresetClickHandler presetRepo userService resp botService
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

  removeIncludedArtistClickHandler presetService resp botService
  removeExcludedArtistClickHandler presetService resp botService

  removeIncludedPlaylistClickHandler presetService resp botService
  removeExcludedPlaylistClickHandler presetService resp botService
  removeTargetedPlaylistClickHandler presetService resp botService
}

let main
  authService
  userRepo
  userService
  presetService
  presetRepo
  buildMusicPlatform
  buildChatContext
  getResp
  (chatRepo: IChatRepo)
  (chatService: IChatService)
  (logger: ILogger)
  =
  fun (update: Update) -> task {
    let botSvc = buildChatContext update.ChatId
    let! resp = getResp update.Lang

    let! chat =
      chatRepo.LoadChat update.ChatId
      |> Task.bind (Option.defaultWithTask (fun () -> chatService.CreateChat(update.ChatId, update.Lang)))

    match update.Data with
    | Msg msg ->
      let! result = buildMessageHandlers authService userRepo userService presetService presetRepo resp botSvc buildMusicPlatform chat msg

      match result with
      | Some() -> return ()
      | None ->
        logger.LogWarning "Message content didn't match any handler. Running default one."

        return! botSvc.SendMessage resp[Messages.UnknownCommand] |> Task.map ignore
    | Click click ->
      let! result = buildClickHandlers userService presetService presetRepo resp botSvc buildMusicPlatform chat click

      match result with
      | Some() -> return ()
      | None ->
        logger.LogWarning "Button click data didn't match any handler. Running default one."

        return! botSvc.SendNotification(click.Id, resp[Notifications.UnknownCommand])
  }