using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Workleap.Extensions.Mongo.Tests;

public sealed class MongoCollectionBuilderTests
{
    [Fact]
    public void Builder_Sets_ClientName_Correctly()
    {
        var builder = new MongoCollectionBuilder<TestDocument>();
        builder.CollectionName("collection1").ClientName("myCluster");

        var metadata = builder.Build() as MongoCollectionMetadata<TestDocument>;

        Assert.NotNull(metadata);
        Assert.Equal("myCluster", metadata.ClientName);
    }

    [Fact]
    public void Builder_ClientName_Is_Null_By_Default()
    {
        var builder = new MongoCollectionBuilder<TestDocument>();
        builder.CollectionName("collection1");

        var metadata = builder.Build() as MongoCollectionMetadata<TestDocument>;

        Assert.NotNull(metadata);
        Assert.Null(metadata.ClientName);
    }

    [Fact]
    public void Builder_Builds_Correctly()
    {
        const string collectionName = "collection1";
        const string otherDatabaseName = "otherDatabaseName";

        static void classMapInitializer(BsonClassMap<TestDocument> map)
        {
            map.MapIdProperty(x => x.Id);
        }

        var builder = new MongoCollectionBuilder<TestDocument>();
        builder
            .CollectionName(collectionName)
            .IndexProvider<TestDocumentIndexProvider>()
            .BsonClassMap(classMapInitializer);

        var otherDatabaseBuilder = new MongoCollectionBuilder<TestDocument>();
        otherDatabaseBuilder
            .CollectionName(collectionName)
            .DatabaseName(otherDatabaseName)
            .IndexProvider<TestDocumentIndexProvider>()
            .BsonClassMap(classMapInitializer);

        AssertMetadata(builder.Build() as MongoCollectionMetadata<TestDocument>, collectionName, null, classMapInitializer);
        AssertMetadata(otherDatabaseBuilder.Build() as MongoCollectionMetadata<TestDocument>, collectionName, otherDatabaseName, classMapInitializer);
    }

    private static void AssertMetadata<TDocument>(MongoCollectionMetadata<TDocument>? metadata, string expectedCollectionName, string? expectedDatabaseName, Action<BsonClassMap<TDocument>> expectedClassMapInitializer)
        where TDocument : class
    {
        Assert.NotNull(metadata);
        Assert.Equal(expectedCollectionName, metadata.CollectionName);
        Assert.Equal(expectedDatabaseName, metadata.DatabaseName);
        Assert.Equal(typeof(TestDocumentIndexProvider), metadata.IndexProviderType);
        Assert.Equal(expectedClassMapInitializer, metadata.ClassMapInitializer);
    }

    public sealed class TestDocument
    {
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public string Name { get; set; } = string.Empty;
    }

    public sealed class TestDocumentIndexProvider : MongoIndexProvider<TestDocument>
    {
        public override IEnumerable<CreateIndexModel<TestDocument>> CreateIndexModels()
        {
            yield return new CreateIndexModel<TestDocument>(this.IndexKeys.Ascending(x => x.Id));
        }
    }
}