using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;

namespace Workleap.Extensions.Mongo.Ephemeral;

// Drops the default database when disposed, meaning that each test drops its own database when disposed.
// Dispose() is only called when IMongoClient is requested from the dependency injection service provider.
// The service provider, when disposed, also dispose alive objects that are registered.
// https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines#disposal-of-services
internal sealed class DisposableMongoClient : IMongoClient, IDisposable
{
    private readonly IMongoClient _underlyingMongoClient;
    private readonly string _defaultDatabaseName;

    public DisposableMongoClient(IMongoClient underlyingMongoClient, DefaultDatabaseNameHolder defaultDatabaseNameHolder)
    {
        this._underlyingMongoClient = underlyingMongoClient;
        this._defaultDatabaseName = defaultDatabaseNameHolder.DatabaseName;
    }

    public ICluster Cluster => this._underlyingMongoClient.Cluster;

    public MongoClientSettings Settings => this._underlyingMongoClient.Settings;

    public void DropDatabase(string name, CancellationToken cancellationToken = default)
    {
        this._underlyingMongoClient.DropDatabase(name, cancellationToken);
    }

    public void DropDatabase(IClientSessionHandle session, string name, CancellationToken cancellationToken = default)
    {
        this._underlyingMongoClient.DropDatabase(session, name, cancellationToken);
    }

    public Task DropDatabaseAsync(string name, CancellationToken cancellationToken = default)
    {
        return this._underlyingMongoClient.DropDatabaseAsync(name, cancellationToken);
    }

    public Task DropDatabaseAsync(IClientSessionHandle session, string name, CancellationToken cancellationToken = default)
    {
        return this._underlyingMongoClient.DropDatabaseAsync(session, name, cancellationToken);
    }

    public IMongoDatabase GetDatabase(string name, MongoDatabaseSettings? settings = null)
    {
        return this._underlyingMongoClient.GetDatabase(name, settings);
    }

    public IAsyncCursor<string> ListDatabaseNames(CancellationToken cancellationToken = default)
    {
        return this._underlyingMongoClient.ListDatabaseNames(cancellationToken);
    }

    public IAsyncCursor<string> ListDatabaseNames(ListDatabaseNamesOptions options, CancellationToken cancellationToken = default)
    {
        return this._underlyingMongoClient.ListDatabaseNames(options, cancellationToken);
    }

    public IAsyncCursor<string> ListDatabaseNames(IClientSessionHandle session, CancellationToken cancellationToken = default)
    {
        return this._underlyingMongoClient.ListDatabaseNames(session, cancellationToken);
    }

    public IAsyncCursor<string> ListDatabaseNames(IClientSessionHandle session, ListDatabaseNamesOptions options, CancellationToken cancellationToken = default)
    {
        return this._underlyingMongoClient.ListDatabaseNames(session, options, cancellationToken);
    }

    public Task<IAsyncCursor<string>> ListDatabaseNamesAsync(CancellationToken cancellationToken = default)
    {
        return this._underlyingMongoClient.ListDatabaseNamesAsync(cancellationToken);
    }

    public Task<IAsyncCursor<string>> ListDatabaseNamesAsync(ListDatabaseNamesOptions options, CancellationToken cancellationToken = default)
    {
        return this._underlyingMongoClient.ListDatabaseNamesAsync(options, cancellationToken);
    }

    public Task<IAsyncCursor<string>> ListDatabaseNamesAsync(IClientSessionHandle session, CancellationToken cancellationToken = default)
    {
        return this._underlyingMongoClient.ListDatabaseNamesAsync(session, cancellationToken);
    }

    public Task<IAsyncCursor<string>> ListDatabaseNamesAsync(IClientSessionHandle session, ListDatabaseNamesOptions options, CancellationToken cancellationToken = default)
    {
        return this._underlyingMongoClient.ListDatabaseNamesAsync(session, options, cancellationToken);
    }

    public IAsyncCursor<BsonDocument> ListDatabases(CancellationToken cancellationToken = default)
    {
        return this._underlyingMongoClient.ListDatabases(cancellationToken);
    }

    public IAsyncCursor<BsonDocument> ListDatabases(ListDatabasesOptions options, CancellationToken cancellationToken = default)
    {
        return this._underlyingMongoClient.ListDatabases(options, cancellationToken);
    }

    public IAsyncCursor<BsonDocument> ListDatabases(IClientSessionHandle session, CancellationToken cancellationToken = default)
    {
        return this._underlyingMongoClient.ListDatabases(session, cancellationToken);
    }

    public IAsyncCursor<BsonDocument> ListDatabases(IClientSessionHandle session, ListDatabasesOptions options, CancellationToken cancellationToken = default)
    {
        return this._underlyingMongoClient.ListDatabases(session, options, cancellationToken);
    }

    public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(CancellationToken cancellationToken = default)
    {
        return this._underlyingMongoClient.ListDatabasesAsync(cancellationToken);
    }

    public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(ListDatabasesOptions options, CancellationToken cancellationToken = default)
    {
        return this._underlyingMongoClient.ListDatabasesAsync(options, cancellationToken);
    }

    public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(IClientSessionHandle session, CancellationToken cancellationToken = default)
    {
        return this._underlyingMongoClient.ListDatabasesAsync(session, cancellationToken);
    }

    public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(IClientSessionHandle session, ListDatabasesOptions options, CancellationToken cancellationToken = default)
    {
        return this._underlyingMongoClient.ListDatabasesAsync(session, options, cancellationToken);
    }

    public IClientSessionHandle StartSession(ClientSessionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingMongoClient.StartSession(options, cancellationToken);
    }

    public Task<IClientSessionHandle> StartSessionAsync(ClientSessionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingMongoClient.StartSessionAsync(options, cancellationToken);
    }

    public IChangeStreamCursor<TResult> Watch<TResult>(PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingMongoClient.Watch(pipeline, options, cancellationToken);
    }

    public IChangeStreamCursor<TResult> Watch<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingMongoClient.Watch(session, pipeline, options, cancellationToken);
    }

    public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingMongoClient.WatchAsync(pipeline, options, cancellationToken);
    }

    public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default)
    {
        return this._underlyingMongoClient.WatchAsync(session, pipeline, options, cancellationToken);
    }

    public IMongoClient WithReadConcern(ReadConcern readConcern)
    {
        return this._underlyingMongoClient.WithReadConcern(readConcern);
    }

    public IMongoClient WithReadPreference(ReadPreference readPreference)
    {
        return this._underlyingMongoClient.WithReadPreference(readPreference);
    }

    public IMongoClient WithWriteConcern(WriteConcern writeConcern)
    {
        return this._underlyingMongoClient.WithWriteConcern(writeConcern);
    }

    public void Dispose()
    {
        this.DropDatabase(this._defaultDatabaseName);
    }
}