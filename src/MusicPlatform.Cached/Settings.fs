module MusicPlatform.Cached.Settings

[<CLIMutable>]
type RedisSettings =
  { ConnectionString: string }

  static member SectionName = "Redis"