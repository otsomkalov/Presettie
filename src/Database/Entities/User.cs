using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Database.Entities;

[BsonIgnoreExtraElements]
public class User
{
    public ObjectId Id { get; set; }

    public ObjectId? CurrentPresetId { get; set; }

    public IEnumerable<string> MusicPlatforms { get; set; }
}