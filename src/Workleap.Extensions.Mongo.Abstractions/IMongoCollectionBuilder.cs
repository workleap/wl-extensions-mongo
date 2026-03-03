using MongoDB.Bson.Serialization;

namespace Workleap.Extensions.Mongo;

public interface IMongoCollectionBuilder<TDocument>
    where TDocument : class
{
    IMongoCollectionBuilder<TDocument> CollectionName(string collectionName);

    IMongoCollectionBuilder<TDocument> ClientName(string clientName);

    IMongoCollectionBuilder<TDocument> DatabaseName(string databaseName);

    IMongoCollectionBuilder<TDocument> IndexProvider<TIndexProvider>()
        where TIndexProvider : MongoIndexProvider<TDocument>;

    IMongoCollectionBuilder<TDocument> BsonClassMap(Action<BsonClassMap<TDocument>> classMapInitializer);
}