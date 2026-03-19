using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Workleap.Extensions.Mongo.Indexing;
using Workleap.Extensions.Xunit;

namespace Workleap.Extensions.Mongo.Tests;

/// <summary>
/// Tests for Atlas Search index support via <see cref="MongoIndexProvider{TDocument}.CreateSearchIndexModels"/>.
/// Because integration tests run against a community MongoDB (ephemeral), Atlas Search is not available.
/// These tests therefore validate the graceful-degradation path: search index management is silently
/// skipped while regular index management continues to work normally.
/// Full Atlas Search lifecycle tests (create / update / drop) require a MongoDB Atlas cluster with mongot.
/// </summary>
public class MongoIndexerSearchIndexTests : BaseIntegrationTest<MongoFixture>
{
    public MongoIndexerSearchIndexTests(MongoFixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture, testOutputHelper)
    {
    }

    [Fact]
    public async Task UpdateIndexesAsync_Does_Not_Throw_When_Provider_Has_Only_Search_Indexes()
    {
        // A provider that has no regular indexes, only Atlas Search indexes.
        // On a non-Atlas cluster this should complete without throwing.
        var exception = await Record.ExceptionAsync(() =>
            this.Services.GetRequiredService<IMongoIndexer>().UpdateIndexesAsync(new[] { typeof(SearchOnlyDocument) }));

        Assert.Null(exception);
    }

    [Fact]
    public async Task UpdateIndexesAsync_Regular_Indexes_Are_Still_Created_When_Provider_Also_Has_Search_Indexes()
    {
        await this.Services.GetRequiredService<IMongoIndexer>().UpdateIndexesAsync(new[] { typeof(HybridDocument) });

        var collection = this.Services.GetRequiredService<IMongoCollection<HybridDocument>>();
        using var cursor = await collection.Indexes.ListAsync();
        var indexNames = await cursor.ToAsyncEnumerable().Select(x => x["name"].AsString).ToArrayAsync();

        // The regular index should have been created; Atlas Search index silently skipped.
        Assert.Contains("title_", indexNames.Single(n => n.StartsWith("title_", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task UpdateIndexesAsync_Regular_Indexes_Are_Deleted_When_Provider_Also_Has_Search_Indexes()
    {
        // First call: create the regular index.
        await this.Services.GetRequiredService<IMongoIndexer>().UpdateIndexesAsync(new[] { typeof(HybridDocument) });

        // Second call with a provider that no longer declares the regular index.
        // The regular index should be dropped; the missing search index management is silently skipped.
        var exception = await Record.ExceptionAsync(() =>
            this.Services.GetRequiredService<IMongoIndexer>().UpdateIndexesAsync(new[] { typeof(HybridDocumentWithoutRegularIndex) }));

        Assert.Null(exception);

        var collection = this.Services.GetRequiredService<IMongoCollection<HybridDocumentWithoutRegularIndex>>();
        using var cursor = await collection.Indexes.ListAsync();
        var indexNames = await cursor.ToAsyncEnumerable().Select(x => x["name"].AsString).ToArrayAsync();

        // Only the default _id_ index should remain.
        Assert.Single(indexNames);
        Assert.Contains("_id_", indexNames);
    }

    // -----------------------------------------------------------------------
    // Test documents and providers
    // -----------------------------------------------------------------------

    [MongoCollection("searchOnlyDocument", IndexProviderType = typeof(SearchOnlyDocumentIndexProvider))]
    private sealed class SearchOnlyDocument : MongoDocument
    {
        public string Title { get; set; } = string.Empty;
    }

    private sealed class SearchOnlyDocumentIndexProvider : MongoIndexProvider<SearchOnlyDocument>
    {
        public override IEnumerable<CreateIndexModel<SearchOnlyDocument>> CreateIndexModels()
            => [];

        public override IEnumerable<CreateSearchIndexModel> CreateSearchIndexModels()
        {
            yield return new CreateSearchIndexModel("default", new BsonDocument
            {
                { "mappings", new BsonDocument { { "dynamic", true } } },
            });
        }
    }

    [MongoCollection("hybridDocument", IndexProviderType = typeof(HybridDocumentIndexProvider))]
    private sealed class HybridDocument : MongoDocument
    {
        public string Title { get; set; } = string.Empty;
    }

    private sealed class HybridDocumentIndexProvider : MongoIndexProvider<HybridDocument>
    {
        public override IEnumerable<CreateIndexModel<HybridDocument>> CreateIndexModels()
        {
            yield return new CreateIndexModel<HybridDocument>(
                Builders<HybridDocument>.IndexKeys.Ascending(x => x.Title),
                new CreateIndexOptions { Name = "title" });
        }

        public override IEnumerable<CreateSearchIndexModel> CreateSearchIndexModels()
        {
            yield return new CreateSearchIndexModel("default", new BsonDocument
            {
                { "mappings", new BsonDocument { { "dynamic", true } } },
            });
        }
    }

    // Represents the "next release" where the regular index was removed.
    [MongoCollection("hybridDocument", IndexProviderType = typeof(HybridDocumentWithoutRegularIndexProvider))]
    private sealed class HybridDocumentWithoutRegularIndex : MongoDocument
    {
        public string Title { get; set; } = string.Empty;
    }

    private sealed class HybridDocumentWithoutRegularIndexProvider : MongoIndexProvider<HybridDocumentWithoutRegularIndex>
    {
        public override IEnumerable<CreateIndexModel<HybridDocumentWithoutRegularIndex>> CreateIndexModels()
            => [];

        public override IEnumerable<CreateSearchIndexModel> CreateSearchIndexModels()
        {
            yield return new CreateSearchIndexModel("default", new BsonDocument
            {
                { "mappings", new BsonDocument { { "dynamic", true } } },
            });
        }
    }
}
