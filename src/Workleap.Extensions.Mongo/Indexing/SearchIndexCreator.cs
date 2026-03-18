using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Workleap.Extensions.Mongo.Indexing;

/// <summary>
/// Ensures that Atlas Search indexes for a particular document type exist on the database
/// with the desired index definitions declared in the code.
/// </summary>
/// <remarks>
/// Unlike regular indexes, Atlas Search index names are NOT hash-suffixed because they are referenced
/// by name in $search aggregation stages. Change detection is done by comparing a SHA256 hash of the
/// definition document at runtime: once from the code, once from the 'latestDefinition' field returned
/// by the database.
/// </remarks>
internal sealed class SearchIndexCreator<TDocument>
    where TDocument : class
{
    private readonly MongoIndexProvider<TDocument> _provider;
    private readonly IMongoCollection<TDocument> _collection;
    private readonly ILogger _logger;
    private readonly CancellationToken _cancellationToken;

    private SearchIndexCreator(MongoIndexProvider<TDocument> provider, IMongoDatabase database, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        this._provider = provider;
        this._collection = database.GetCollection<TDocument>();
        this._logger = loggerFactory.CreateLogger<SearchIndexCreator<TDocument>>();
        this._cancellationToken = cancellationToken;
    }

    public static Task<IList<string>> ProcessAsync(MongoIndexProvider<TDocument> provider, IMongoDatabase database, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        return new SearchIndexCreator<TDocument>(provider, database, loggerFactory, cancellationToken).ProcessAsync();
    }

    private async Task<IList<string>> ProcessAsync()
    {
        var desiredIndexModels = this._provider.CreateSearchIndexModels().ToList();
        if (desiredIndexModels.Count == 0)
        {
            return [];
        }

        Dictionary<string, BsonDocument> existingIndexes;
        try
        {
            existingIndexes = await this.GetExistingSearchIndexesAsync().ConfigureAwait(false);
        }
        catch (MongoCommandException ex) when (IsSearchNotSupportedException(ex))
        {
            this._logger.AtlasSearchNotAvailable(ex, this._collection.Database.DatabaseNamespace.DatabaseName);
            return [];
        }

        this._cancellationToken.ThrowIfCancellationRequested();

        var expectedNames = new List<string>(desiredIndexModels.Count);

        foreach (var model in desiredIndexModels)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                throw new ArgumentException($"All search indexes in '{this._provider.GetType()}' must have a non-empty Name");
            }

            expectedNames.Add(model.Name);

            var desiredHash = ComputeDefinitionHash(model.Definition);

            try
            {
                if (!existingIndexes.TryGetValue(model.Name, out var latestDefinition))
                {
                    this._logger.CreatingNewSearchIndex(typeof(TDocument).Name, model.Name, this._collection.Database.DatabaseNamespace.DatabaseName);
                    await this._collection.SearchIndexes.CreateOneAsync(model, cancellationToken: this._cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var existingHash = ComputeDefinitionHash(latestDefinition);
                    if (existingHash == desiredHash)
                    {
                        this._logger.SkippingUpToDateSearchIndex(typeof(TDocument).Name, model.Name, this._collection.Database.DatabaseNamespace.DatabaseName);
                    }
                    else
                    {
                        this._logger.UpdatingSearchIndex(typeof(TDocument).Name, model.Name, this._collection.Database.DatabaseNamespace.DatabaseName);
                        await this._collection.SearchIndexes.UpdateAsync(model.Name, model.Definition, this._cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (MongoCommandException ex) when (IsSearchNotSupportedException(ex))
            {
                this._logger.AtlasSearchNotAvailable(ex, this._collection.Database.DatabaseNamespace.DatabaseName);
                return [];
            }
        }

        return expectedNames;
    }

    private async Task<Dictionary<string, BsonDocument>> GetExistingSearchIndexesAsync()
    {
        var result = new Dictionary<string, BsonDocument>(StringComparer.Ordinal);

        using var cursor = await this._collection.SearchIndexes.ListAsync(null, null, this._cancellationToken).ConfigureAwait(false);
        var indexes = await cursor.ToListAsync(this._cancellationToken).ConfigureAwait(false);

        foreach (var index in indexes)
        {
            var name = index["name"].AsString;
            var latestDefinition = index.GetValue("latestDefinition", BsonNull.Value);
            if (latestDefinition.IsBsonDocument)
            {
                result[name] = latestDefinition.AsBsonDocument;
            }
        }

        return result;
    }

    private static string ComputeDefinitionHash(BsonDocument? definition)
    {
        if (definition is null)
        {
            return string.Empty;
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(definition.ToString()));
        return Convert.ToHexString(hash)[..32].ToLowerInvariant();
    }

    private static bool IsSearchNotSupportedException(MongoCommandException ex)
    {
        // Error code 31082 is returned by non-Atlas clusters that do not have the mongot process running.
        // https://www.mongodb.com/docs/atlas/atlas-search/atlas-search-overview/
        return ex.Code == 31082;
    }
}
