using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Search;

namespace Workleap.Extensions.Mongo;

internal sealed class MongoCollectionProxy<TDocument> : IMongoCollection<TDocument>
{
    private readonly IMongoCollection<TDocument> _underlyingCollection;

    public MongoCollectionProxy(IMongoClientProvider clientProvider, IOptionsMonitor<MongoClientOptions> optionsMonitor)
    {
        var collectionInfo = MongoCollectionInformationCache.GetCollectionInformation(typeof(TDocument));
        var clientName = collectionInfo.ClientName ?? MongoDefaults.ClientName;
        var client = clientProvider.GetClient(clientName);
        var options = optionsMonitor.Get(clientName);
        var databaseName = collectionInfo.DatabaseName ?? options.DefaultDatabaseName;

        this._underlyingCollection = client.GetDatabase(databaseName).GetCollection<TDocument>(collectionInfo.CollectionName);
    }

    public CollectionNamespace CollectionNamespace => this._underlyingCollection.CollectionNamespace;

    public IMongoDatabase Database => this._underlyingCollection.Database;

    public IBsonSerializer<TDocument> DocumentSerializer => this._underlyingCollection.DocumentSerializer;

    public IMongoIndexManager<TDocument> Indexes => this._underlyingCollection.Indexes;

    public IMongoSearchIndexManager SearchIndexes => this._underlyingCollection.SearchIndexes;

    public MongoCollectionSettings Settings => this._underlyingCollection.Settings;

    public IAsyncCursor<TResult> Aggregate<TResult>(PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.Aggregate(pipeline, options, cancellationToken);
    }

    public IAsyncCursor<TResult> Aggregate<TResult>(IClientSessionHandle session, PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.Aggregate(session, pipeline, options, cancellationToken);
    }

    public Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.AggregateAsync(pipeline, options, cancellationToken);
    }

    public Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(IClientSessionHandle session, PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.AggregateAsync(session, pipeline, options, cancellationToken);
    }

    public void AggregateToCollection<TResult>(PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
    {
        this._underlyingCollection.AggregateToCollection(pipeline, options, cancellationToken);
    }

    public void AggregateToCollection<TResult>(IClientSessionHandle session, PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
    {
        this._underlyingCollection.AggregateToCollection(session, pipeline, options, cancellationToken);
    }

    public Task AggregateToCollectionAsync<TResult>(PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.AggregateToCollectionAsync(pipeline, options, cancellationToken);
    }

    public Task AggregateToCollectionAsync<TResult>(IClientSessionHandle session, PipelineDefinition<TDocument, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.AggregateToCollectionAsync(session, pipeline, options, cancellationToken);
    }

    public BulkWriteResult<TDocument> BulkWrite(IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.BulkWrite(requests, options, cancellationToken);
    }

    public BulkWriteResult<TDocument> BulkWrite(IClientSessionHandle session, IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.BulkWrite(session, requests, options, cancellationToken);
    }

    public Task<BulkWriteResult<TDocument>> BulkWriteAsync(IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.BulkWriteAsync(requests, options, cancellationToken);
    }

    public Task<BulkWriteResult<TDocument>> BulkWriteAsync(IClientSessionHandle session, IEnumerable<WriteModel<TDocument>> requests, BulkWriteOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.BulkWriteAsync(session, requests, options, cancellationToken);
    }

    [Obsolete("Use CountDocuments or EstimatedDocumentCount instead.")]
    public long Count(FilterDefinition<TDocument> filter, CountOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.Count(filter, options, cancellationToken);
    }

    [Obsolete("Use CountDocuments or EstimatedDocumentCount instead.")]
    public long Count(IClientSessionHandle session, FilterDefinition<TDocument> filter, CountOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.Count(session, filter, options, cancellationToken);
    }

    [Obsolete("Use CountDocumentsAsync or EstimatedDocumentCountAsync instead.")]
    public Task<long> CountAsync(FilterDefinition<TDocument> filter, CountOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.CountAsync(filter, options, cancellationToken);
    }

    [Obsolete("Use CountDocumentsAsync or EstimatedDocumentCountAsync instead.")]
    public Task<long> CountAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, CountOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.CountAsync(session, filter, options, cancellationToken);
    }

    public long CountDocuments(FilterDefinition<TDocument> filter, CountOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.CountDocuments(filter, options, cancellationToken);
    }

    public long CountDocuments(IClientSessionHandle session, FilterDefinition<TDocument> filter, CountOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.CountDocuments(session, filter, options, cancellationToken);
    }

    public Task<long> CountDocumentsAsync(FilterDefinition<TDocument> filter, CountOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.CountDocumentsAsync(filter, options, cancellationToken);
    }

    public Task<long> CountDocumentsAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, CountOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.CountDocumentsAsync(session, filter, options, cancellationToken);
    }

    public DeleteResult DeleteMany(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.DeleteMany(filter, cancellationToken);
    }

    public DeleteResult DeleteMany(FilterDefinition<TDocument> filter, DeleteOptions options, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.DeleteMany(filter, options, cancellationToken);
    }

    public DeleteResult DeleteMany(IClientSessionHandle session, FilterDefinition<TDocument> filter, DeleteOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.DeleteMany(session, filter, options, cancellationToken);
    }

    public Task<DeleteResult> DeleteManyAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.DeleteManyAsync(filter, cancellationToken);
    }

    public Task<DeleteResult> DeleteManyAsync(FilterDefinition<TDocument> filter, DeleteOptions options, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.DeleteManyAsync(filter, options, cancellationToken);
    }

    public Task<DeleteResult> DeleteManyAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, DeleteOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.DeleteManyAsync(session, filter, options, cancellationToken);
    }

    public DeleteResult DeleteOne(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.DeleteOne(filter, cancellationToken);
    }

    public DeleteResult DeleteOne(FilterDefinition<TDocument> filter, DeleteOptions options, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.DeleteOne(filter, options, cancellationToken);
    }

    public DeleteResult DeleteOne(IClientSessionHandle session, FilterDefinition<TDocument> filter, DeleteOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.DeleteOne(session, filter, options, cancellationToken);
    }

    public Task<DeleteResult> DeleteOneAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.DeleteOneAsync(filter, cancellationToken);
    }

    public Task<DeleteResult> DeleteOneAsync(FilterDefinition<TDocument> filter, DeleteOptions options, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.DeleteOneAsync(filter, options, cancellationToken);
    }

    public Task<DeleteResult> DeleteOneAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, DeleteOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.DeleteOneAsync(session, filter, options, cancellationToken);
    }

    public IAsyncCursor<TField> Distinct<TField>(FieldDefinition<TDocument, TField> field, FilterDefinition<TDocument> filter, DistinctOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.Distinct(field, filter, options, cancellationToken);
    }

    public IAsyncCursor<TField> Distinct<TField>(IClientSessionHandle session, FieldDefinition<TDocument, TField> field, FilterDefinition<TDocument> filter, DistinctOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.Distinct(session, field, filter, options, cancellationToken);
    }

    public Task<IAsyncCursor<TField>> DistinctAsync<TField>(FieldDefinition<TDocument, TField> field, FilterDefinition<TDocument> filter, DistinctOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.DistinctAsync(field, filter, options, cancellationToken);
    }

    public Task<IAsyncCursor<TField>> DistinctAsync<TField>(IClientSessionHandle session, FieldDefinition<TDocument, TField> field, FilterDefinition<TDocument> filter, DistinctOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.DistinctAsync(session, field, filter, options, cancellationToken);
    }

    public IAsyncCursor<TItem> DistinctMany<TItem>(FieldDefinition<TDocument, IEnumerable<TItem>> field, FilterDefinition<TDocument> filter, DistinctOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.DistinctMany(field, filter, options, cancellationToken);
    }

    public IAsyncCursor<TItem> DistinctMany<TItem>(IClientSessionHandle session, FieldDefinition<TDocument, IEnumerable<TItem>> field, FilterDefinition<TDocument> filter, DistinctOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.DistinctMany(session, field, filter, options, cancellationToken);
    }

    public Task<IAsyncCursor<TItem>> DistinctManyAsync<TItem>(FieldDefinition<TDocument, IEnumerable<TItem>> field, FilterDefinition<TDocument> filter, DistinctOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.DistinctManyAsync(field, filter, options, cancellationToken);
    }

    public Task<IAsyncCursor<TItem>> DistinctManyAsync<TItem>(IClientSessionHandle session, FieldDefinition<TDocument, IEnumerable<TItem>> field, FilterDefinition<TDocument> filter, DistinctOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.DistinctManyAsync(session, field, filter, options, cancellationToken);
    }

    public long EstimatedDocumentCount(EstimatedDocumentCountOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.EstimatedDocumentCount(options, cancellationToken);
    }

    public Task<long> EstimatedDocumentCountAsync(EstimatedDocumentCountOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.EstimatedDocumentCountAsync(options, cancellationToken);
    }

    public IAsyncCursor<TProjection> FindSync<TProjection>(FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.FindSync(filter, options, cancellationToken);
    }

    public IAsyncCursor<TProjection> FindSync<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.FindSync(session, filter, options, cancellationToken);
    }

    public Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.FindAsync(filter, options, cancellationToken);
    }

    public Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.FindAsync(session, filter, options, cancellationToken);
    }

    public TProjection FindOneAndDelete<TProjection>(FilterDefinition<TDocument> filter, FindOneAndDeleteOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.FindOneAndDelete(filter, options, cancellationToken);
    }

    public TProjection FindOneAndDelete<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, FindOneAndDeleteOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.FindOneAndDelete(session, filter, options, cancellationToken);
    }

    public Task<TProjection> FindOneAndDeleteAsync<TProjection>(FilterDefinition<TDocument> filter, FindOneAndDeleteOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.FindOneAndDeleteAsync(filter, options, cancellationToken);
    }

    public Task<TProjection> FindOneAndDeleteAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, FindOneAndDeleteOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.FindOneAndDeleteAsync(session, filter, options, cancellationToken);
    }

    public TProjection FindOneAndReplace<TProjection>(FilterDefinition<TDocument> filter, TDocument replacement, FindOneAndReplaceOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.FindOneAndReplace(filter, replacement, options, cancellationToken);
    }

    public TProjection FindOneAndReplace<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, TDocument replacement, FindOneAndReplaceOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.FindOneAndReplace(session, filter, replacement, options, cancellationToken);
    }

    public Task<TProjection> FindOneAndReplaceAsync<TProjection>(FilterDefinition<TDocument> filter, TDocument replacement, FindOneAndReplaceOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.FindOneAndReplaceAsync(filter, replacement, options, cancellationToken);
    }

    public Task<TProjection> FindOneAndReplaceAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, TDocument replacement, FindOneAndReplaceOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.FindOneAndReplaceAsync(session, filter, replacement, options, cancellationToken);
    }

    public TProjection FindOneAndUpdate<TProjection>(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, FindOneAndUpdateOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.FindOneAndUpdate(filter, update, options, cancellationToken);
    }

    public TProjection FindOneAndUpdate<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, FindOneAndUpdateOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.FindOneAndUpdate(session, filter, update, options, cancellationToken);
    }

    public Task<TProjection> FindOneAndUpdateAsync<TProjection>(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, FindOneAndUpdateOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
    }

    public Task<TProjection> FindOneAndUpdateAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, FindOneAndUpdateOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.FindOneAndUpdateAsync(session, filter, update, options, cancellationToken);
    }

    public void InsertOne(TDocument document, InsertOneOptions? options = null, CancellationToken cancellationToken = default)
    {
        this._underlyingCollection.InsertOne(document, options, cancellationToken);
    }

    public void InsertOne(IClientSessionHandle session, TDocument document, InsertOneOptions? options = null, CancellationToken cancellationToken = default)
    {
        this._underlyingCollection.InsertOne(session, document, options, cancellationToken);
    }

    [Obsolete("Use the new overload of InsertOneAsync with an InsertOneOptions parameter instead.")]
    public Task InsertOneAsync(TDocument document, CancellationToken _cancellationToken)
    {
        return this._underlyingCollection.InsertOneAsync(document, _cancellationToken);
    }

    public Task InsertOneAsync(TDocument document, InsertOneOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.InsertOneAsync(document, options, cancellationToken);
    }

    public Task InsertOneAsync(IClientSessionHandle session, TDocument document, InsertOneOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.InsertOneAsync(session, document, options, cancellationToken);
    }

    public void InsertMany(IEnumerable<TDocument> documents, InsertManyOptions? options = null, CancellationToken cancellationToken = default)
    {
        this._underlyingCollection.InsertMany(documents, options, cancellationToken);
    }

    public void InsertMany(IClientSessionHandle session, IEnumerable<TDocument> documents, InsertManyOptions? options = null, CancellationToken cancellationToken = default)
    {
        this._underlyingCollection.InsertMany(session, documents, options, cancellationToken);
    }

    public Task InsertManyAsync(IEnumerable<TDocument> documents, InsertManyOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.InsertManyAsync(documents, options, cancellationToken);
    }

    public Task InsertManyAsync(IClientSessionHandle session, IEnumerable<TDocument> documents, InsertManyOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.InsertManyAsync(session, documents, options, cancellationToken);
    }

    [Obsolete("Use Aggregation pipeline instead.")]
    public IAsyncCursor<TResult> MapReduce<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TDocument, TResult>? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.MapReduce(map, reduce, options, cancellationToken);
    }

    [Obsolete("Use Aggregation pipeline instead.")]
    public IAsyncCursor<TResult> MapReduce<TResult>(IClientSessionHandle session, BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TDocument, TResult>? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.MapReduce(session, map, reduce, options, cancellationToken);
    }

    [Obsolete("Use Aggregation pipeline instead.")]
    public Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TDocument, TResult>? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.MapReduceAsync(map, reduce, options, cancellationToken);
    }

    [Obsolete("Use Aggregation pipeline instead.")]
    public Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(IClientSessionHandle session, BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TDocument, TResult>? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.MapReduceAsync(session, map, reduce, options, cancellationToken);
    }

    public IFilteredMongoCollection<TDerivedDocument> OfType<TDerivedDocument>() where TDerivedDocument : TDocument
    {
        return this._underlyingCollection.OfType<TDerivedDocument>();
    }

    public ReplaceOneResult ReplaceOne(FilterDefinition<TDocument> filter, TDocument replacement, ReplaceOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.ReplaceOne(filter, replacement, options, cancellationToken);
    }

    [Obsolete("Use the overload that takes a ReplaceOptions instead of an UpdateOptions.")]
    public ReplaceOneResult ReplaceOne(FilterDefinition<TDocument> filter, TDocument replacement, UpdateOptions options, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.ReplaceOne(filter, replacement, options, cancellationToken);
    }

    public ReplaceOneResult ReplaceOne(IClientSessionHandle session, FilterDefinition<TDocument> filter, TDocument replacement, ReplaceOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.ReplaceOne(session, filter, replacement, options, cancellationToken);
    }

    [Obsolete("Use the overload that takes a ReplaceOptions instead of an UpdateOptions.")]
    public ReplaceOneResult ReplaceOne(IClientSessionHandle session, FilterDefinition<TDocument> filter, TDocument replacement, UpdateOptions options, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.ReplaceOne(session, filter, replacement, options, cancellationToken);
    }

    public Task<ReplaceOneResult> ReplaceOneAsync(FilterDefinition<TDocument> filter, TDocument replacement, ReplaceOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.ReplaceOneAsync(filter, replacement, options, cancellationToken);
    }

    [Obsolete("Use the overload that takes a ReplaceOptions instead of an UpdateOptions.")]
    public Task<ReplaceOneResult> ReplaceOneAsync(FilterDefinition<TDocument> filter, TDocument replacement, UpdateOptions options, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.ReplaceOneAsync(filter, replacement, options, cancellationToken);
    }

    public Task<ReplaceOneResult> ReplaceOneAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, TDocument replacement, ReplaceOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.ReplaceOneAsync(session, filter, replacement, options, cancellationToken);
    }

    [Obsolete("Use the overload that takes a ReplaceOptions instead of an UpdateOptions.")]
    public Task<ReplaceOneResult> ReplaceOneAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, TDocument replacement, UpdateOptions options, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.ReplaceOneAsync(session, filter, replacement, options, cancellationToken);
    }

    public UpdateResult UpdateMany(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.UpdateMany(filter, update, options, cancellationToken);
    }

    public UpdateResult UpdateMany(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.UpdateMany(session, filter, update, options, cancellationToken);
    }

    public Task<UpdateResult> UpdateManyAsync(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.UpdateManyAsync(filter, update, options, cancellationToken);
    }

    public Task<UpdateResult> UpdateManyAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.UpdateManyAsync(session, filter, update, options, cancellationToken);
    }

    public UpdateResult UpdateOne(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.UpdateOne(filter, update, options, cancellationToken);
    }

    public UpdateResult UpdateOne(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.UpdateOne(session, filter, update, options, cancellationToken);
    }

    public Task<UpdateResult> UpdateOneAsync(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.UpdateOneAsync(filter, update, options, cancellationToken);
    }

    public Task<UpdateResult> UpdateOneAsync(IClientSessionHandle session, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.UpdateOneAsync(session, filter, update, options, cancellationToken);
    }

    public IChangeStreamCursor<TResult> Watch<TResult>(PipelineDefinition<ChangeStreamDocument<TDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.Watch(pipeline, options, cancellationToken);
    }

    public IChangeStreamCursor<TResult> Watch<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<TDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.Watch(session, pipeline, options, cancellationToken);
    }

    public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(PipelineDefinition<ChangeStreamDocument<TDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.WatchAsync(pipeline, options, cancellationToken);
    }

    public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<TDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingCollection.WatchAsync(session, pipeline, options, cancellationToken);
    }

    public IMongoCollection<TDocument> WithReadConcern(ReadConcern readConcern)
    {
        return this._underlyingCollection.WithReadConcern(readConcern);
    }

    public IMongoCollection<TDocument> WithReadPreference(ReadPreference readPreference)
    {
        return this._underlyingCollection.WithReadPreference(readPreference);
    }

    public IMongoCollection<TDocument> WithWriteConcern(WriteConcern writeConcern)
    {
        return this._underlyingCollection.WithWriteConcern(writeConcern);
    }
}