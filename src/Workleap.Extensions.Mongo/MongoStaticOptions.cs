using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Workleap.Extensions.Mongo;

public sealed class MongoStaticOptions
{
    public MongoStaticOptions()
    {
        this.BsonSerializers = new Dictionary<Type, IBsonSerializer>();
        this.ConventionPacks = new List<NamedConventionPack>();

        // This enum will disappear in Mongo C# driver 3.x and V3 will be the default
        // https://www.mongodb.com/docs/drivers/csharp/current/upgrade/v3/#version-3.0-breaking-changes
#pragma warning disable CS0618 // Type or member is obsolete
        this.GuidRepresentationMode = GuidRepresentationMode.V3;
#pragma warning restore CS0618 // Type or member is obsolete
    }

#pragma warning disable CS0618 // Type or member is obsolete
    public GuidRepresentationMode GuidRepresentationMode { get; set; }
#pragma warning restore CS0618 // Type or member is obsolete

    public IDictionary<Type, IBsonSerializer> BsonSerializers { get; }

    public IList<NamedConventionPack> ConventionPacks { get; }
}