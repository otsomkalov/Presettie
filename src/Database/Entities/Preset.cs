using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Database.Entities;

public enum RecommendationsEngine
{
    ArtistsAlbums,
    ReccoBeats,
    Spotify
}

[BsonIgnoreExtraElements]
public class Settings
{
    public bool? IncludeLikedTracks { get; set; }

    public int Size { get; set; }

    public RecommendationsEngine? RecommendationsEngine { get; set; }

    public bool UniqueArtists { get; set; }
}

[BsonIgnoreExtraElements]
public class Preset
{
    public ObjectId Id { get; set; }

    public string Name { get; set; }

    public ObjectId OwnerId { get; set; }

    public Settings Settings { get; set; }

    public IEnumerable<IncludedPlaylist> IncludedPlaylists { get; set; } = [];

    public IEnumerable<ExcludedPlaylist> ExcludedPlaylists { get; set; } = [];

    public IEnumerable<ExcludedArtist> ExcludedArtists { get; set; } = [];

    public IEnumerable<TargetedPlaylist> TargetedPlaylists { get; set; } = [];
}