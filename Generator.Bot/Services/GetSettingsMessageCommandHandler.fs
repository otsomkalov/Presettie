﻿module Generator.Bot.Services.GetSettingsMessageCommandHandler

open Database.Entities
open Generator.Bot.Constants
open Resources
open System
open Telegram.Bot.Types.ReplyMarkups

let handle (user: User) =
  let includeLikedTracksMark, includeLikedTracksButtonText, includeLikedTracksCallbackData =
    if user.Settings.IncludeLikedTracks then
      ("✅", Messages.ExcludeLikedTracks, CallbackQueryConstants.excludeLikedTracks)
    else
      ("❌", Messages.IncludeLikedTracks, CallbackQueryConstants.includeLikedTracks)

  let text =
    String.Format(Messages.CurrentSettings, includeLikedTracksMark, user.Settings.PlaylistSize)

  let replyMarkup =
    InlineKeyboardMarkup(
      [ InlineKeyboardButton(includeLikedTracksButtonText, CallbackData = includeLikedTracksCallbackData)
        InlineKeyboardButton(Messages.SetPlaylistSize, CallbackData = CallbackQueryConstants.setPlaylistSize) ]
    )

  (text, replyMarkup)
