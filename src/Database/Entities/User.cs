using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Database.Entities;

[BsonIgnoreExtraElements]
public class User
{
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; }

    public ObjectId? CurrentPresetId { get; set; }

    public IEnumerable<string> MusicPlatforms { get; set; }
}