﻿using MongoDB.Bson.Serialization.Attributes;

namespace Database.Entities;

[BsonIgnoreExtraElements]
public class Settings
{
    public bool? IncludeLikedTracks { get; set; }

    public int Size { get; set; }

    public bool RecommendationsEnabled { get; set; }

    public bool UniqueArtists { get; set; }
}

[BsonIgnoreExtraElements]
public class Preset
{
    public string Id { get; set; }

    public string Name { get; set; }

    public long UserId { get; set; }

    public Settings Settings { get; set; }

    public IEnumerable<IncludedPlaylist> IncludedPlaylists { get; set; } = Array.Empty<IncludedPlaylist>();

    public IEnumerable<ExcludedPlaylist> ExcludedPlaylists { get; set; } = Array.Empty<ExcludedPlaylist>();

    public IEnumerable<TargetedPlaylist> TargetedPlaylists { get; set; } = Array.Empty<TargetedPlaylist>();
}