﻿namespace Generator.Bot.Services.Playlist

open Database
open Database.Entities
open Telegram.Bot
open Telegram.Bot.Types

type AddSourcePlaylistCommandHandler(_bot: ITelegramBotClient, _context: AppDbContext, _playlistCommandHandler: PlaylistCommandHandler) =

  let addSourcePlaylistAsync (message: Message) playlistId =
    task {
      let! _ =
        SourcePlaylist(Url = playlistId, UserId = message.From.Id)
        |> _context.SourcePlaylists.AddAsync

      let! _ = _context.SaveChangesAsync()

      _bot.SendTextMessageAsync(ChatId(message.Chat.Id), "Source playlist successfully added!", replyToMessageId = message.MessageId)
      |> ignore
    }

  member this.HandleAsync(message: Message) =
    _playlistCommandHandler.HandleAsync message addSourcePlaylistAsync
