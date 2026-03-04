using MongoDB.Bson.Serialization;

namespace Workleap.Extensions.Mongo;

internal abstract class MongoCollectionMetadata
{
    public string? CollectionName { get; internal set; }

    public string? ClientName { get; internal set; }

    public string? DatabaseName { get; internal set; }

    public Type? IndexProviderType { get; internal set; }
}

internal sealed class MongoCollectionMetadata<T> : MongoCollectionMetadata
    where T : class
{
    public Action<BsonClassMap<T>>? ClassMapInitializer { get; internal set; }
}