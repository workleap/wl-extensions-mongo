using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Workleap.Extensions.Mongo.Indexing;

/// <summary>
/// Deletes Atlas Search indexes for collections by comparing existing indexes in the database
/// with the expected index names declared in the code.
/// </summary>
internal sealed class SearchIndexDeleter
{
    private readonly IMongoDatabase _database;
    private readonly ILogger _logger;
    private readonly Dictionary<string, IList<string>> _expectedSearchIndexNames;
    private readonly CancellationToken _cancellationToken;

    private SearchIndexDeleter(IMongoDatabase database, Dictionary<string, IList<string>> expectedSearchIndexNames, ILogger<SearchIndexDeleter> logger, CancellationToken cancellationToken)
    {
        this._database = database;
        this._logger = logger;
        this._cancellationToken = cancellationToken;
        this._expectedSearchIndexNames = expectedSearchIndexNames;
    }

    public static Task ProcessAsync(IMongoDatabase database, Dictionary<string, IList<string>> expectedSearchIndexNames, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        return new SearchIndexDeleter(database, expectedSearchIndexNames, loggerFactory.CreateLogger<SearchIndexDeleter>(), cancellationToken).ProcessAsync();
    }

    private async Task ProcessAsync()
    {
        foreach (var (collectionName, expectedNames) in this._expectedSearchIndexNames)
        {
            this._cancellationToken.ThrowIfCancellationRequested();

            List<string> existingNames;
            try
            {
                existingNames = await this.GetExistingSearchIndexNamesAsync(collectionName).ConfigureAwait(false);
            }
            catch (MongoCommandException ex) when (IsSearchNotSupportedException(ex))
            {
                this._logger.AtlasSearchNotAvailable(ex, this._database.DatabaseNamespace.DatabaseName);
                return;
            }

            this._cancellationToken.ThrowIfCancellationRequested();

            var expectedNameSet = new HashSet<string>(expectedNames, StringComparer.Ordinal);
            var collection = this._database.GetCollection<BsonDocument>(collectionName);

            foreach (var existingName in existingNames)
            {
                if (!expectedNameSet.Contains(existingName))
                {
                    try
                    {
                        this._logger.DroppingOrphanedSearchIndex(existingName, collectionName, this._database.DatabaseNamespace.DatabaseName);
                        await collection.SearchIndexes.DropOneAsync(existingName, this._cancellationToken).ConfigureAwait(false);
                    }
                    catch (MongoCommandException ex) when (IsSearchNotSupportedException(ex))
                    {
                        this._logger.AtlasSearchNotAvailable(ex, this._database.DatabaseNamespace.DatabaseName);
                        return;
                    }
                }
            }
        }
    }

    private async Task<List<string>> GetExistingSearchIndexNamesAsync(string collectionName)
    {
        var names = new List<string>();
        var collection = this._database.GetCollection<BsonDocument>(collectionName);

        using var cursor = await collection.SearchIndexes.ListAsync(null, null, this._cancellationToken).ConfigureAwait(false);
        var indexes = await cursor.ToListAsync(this._cancellationToken).ConfigureAwait(false);

        foreach (var index in indexes)
        {
            names.Add(index["name"].AsString);
        }

        return names;
    }

    private static bool IsSearchNotSupportedException(MongoCommandException ex)
    {
        // Error code 31082 is returned by non-Atlas clusters that do not have the mongot process running.
        // https://www.mongodb.com/docs/atlas/atlas-search/atlas-search-overview/
        return ex.Code == 31082;
    }
}
