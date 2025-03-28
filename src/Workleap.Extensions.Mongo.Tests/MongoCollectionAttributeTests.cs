using Workleap.Extensions.Xunit;

namespace Workleap.Extensions.Mongo.Tests;

public class MongoCollectionAttributeTests : BaseUnitTest
{
    public MongoCollectionAttributeTests(EmptyUnitFixture fixture, ITestOutputHelper testOutputHelper) : base(fixture, testOutputHelper)
    {
    }

    [Theory]
    [InlineData("SalesTaxes")]
    [InlineData("salesTaxes")]
    [InlineData("S4lesTakes")]
    [InlineData("sales_takes")]
    [InlineData("SalesTakes4")]
    public void Attribute_Ok_Given_Valid_CollectionName(string collectionName)
    {
        var mongoCollection = new MongoCollectionAttribute(collectionName);
        Assert.Equal(collectionName, mongoCollection.Name);
    }

    [Theory]
    [InlineData("4SalesTakes")]
    [InlineData("sales-takes")]
    [InlineData("Sales^Takes")]
    [InlineData("Sales$Takes")]
    [InlineData("sale$Taxes")]
    public void Attribute_Throws_Given_Invalid_CollectionName(string collectionName)
    {
        Assert.Throws<ArgumentException>(() => new MongoCollectionAttribute(collectionName));
    }
}