using MongoDB.Bson.Serialization;

namespace Workleap.Extensions.Mongo;

public abstract class MongoCollectionBuilder
{
    internal abstract MongoCollectionMetadata Build();
}

public sealed class MongoCollectionBuilder<TDocument> : MongoCollectionBuilder, IMongoCollectionBuilder<TDocument>
    where TDocument : class
{
    private readonly MongoCollectionMetadata<TDocument> _metadata = new();

    public IMongoCollectionBuilder<TDocument> CollectionName(string collectionName)
    {
        this._metadata.CollectionName = collectionName;
        return this;
    }

    public IMongoCollectionBuilder<TDocument> ClientName(string clientName)
    {
        this._metadata.ClientName = clientName;
        return this;
    }

    public IMongoCollectionBuilder<TDocument> DatabaseName(string databaseName)
    {
        this._metadata.DatabaseName = databaseName;
        return this;
    }

    public IMongoCollectionBuilder<TDocument> IndexProvider<TIndexProvider>()
        where TIndexProvider : MongoIndexProvider<TDocument>
    {
        this._metadata.IndexProviderType = typeof(TIndexProvider);
        return this;
    }

    public IMongoCollectionBuilder<TDocument> BsonClassMap(Action<BsonClassMap<TDocument>> classMapInitializer)
    {
        this._metadata.ClassMapInitializer = classMapInitializer;
        return this;
    }

    internal override MongoCollectionMetadata Build()
    {
        if (this._metadata.ClassMapInitializer != null)
        {
            MongoDB.Bson.Serialization.BsonClassMap.TryRegisterClassMap(this._metadata.ClassMapInitializer);
        }

        return this._metadata;
    }
}