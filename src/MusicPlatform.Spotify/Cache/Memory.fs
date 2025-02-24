module internal MusicPlatform.Spotify.Cache.Memory

module UserRepo =
  let listLikedTracks listLikedTracks =
    let listLikedTracksLazy = lazy listLikedTracks()

    fun () ->
      listLikedTracksLazy.Value