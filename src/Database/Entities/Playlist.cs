﻿using MongoDB.Bson.Serialization.Attributes;

namespace Database.Entities;

[BsonIgnoreExtraElements]
public abstract class Playlist
{
    public string Id { get; init; }

    public string Name { get; set; }
}

[BsonIgnoreExtraElements]
public class IncludedPlaylist : Playlist
{
    public bool LikedOnly { get; set; }
}

[BsonIgnoreExtraElements]
public class ExcludedPlaylist : Playlist
{
}

[BsonIgnoreExtraElements]
public class TargetedPlaylist : Playlist
{
    public bool Overwrite { get; set; }
}