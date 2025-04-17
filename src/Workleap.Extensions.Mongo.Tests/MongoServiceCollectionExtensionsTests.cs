using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Workleap.Extensions.Mongo.Tests;

public class MongoServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMongo_PostConfigures_ConnectionStringFromConfigurationSection()
    {
        // Arrange
        var configurationDict = new Dictionary<string, string?>
        {
            ["Mongo:ConnectionString"] = "mongodb://configsection",
            ["Mongo:DefaultDatabaseName"] = "testdb",
        };
        var services = new ServiceCollection();
        services.AddConfiguration(configurationDict);
        services.AddLogging();

        // Act
        services.AddMongo();
        using var serviceProvider = services.BuildServiceProvider();

        // Assert
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<MongoClientOptions>>();
        var options = optionsMonitor.Get(MongoDefaults.ClientName);
        Assert.Equal("mongodb://configsection", options.ConnectionString);
        Assert.Equal("testdb", options.DefaultDatabaseName);

        Assert.Throws<OptionsValidationException>(() => optionsMonitor.Get("NonRegisteredClient"));
    }

    [Fact]
    public void AddMongo_PostConfigures_ConnectionStringFromConnectionStrings()
    {
        // Arrange
        var configurationDict = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Mongo"] = "mongodb://connectionstrings:27017/testdb",
        };
        var services = new ServiceCollection();
        services.AddConfiguration(configurationDict);
        services.AddLogging();

        // Act
        services.AddMongo();
        using var serviceProvider = services.BuildServiceProvider();

        // Assert
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<MongoClientOptions>>();
        var options = optionsMonitor.Get(MongoDefaults.ClientName);
        Assert.Equal("mongodb://connectionstrings:27017/testdb", options.ConnectionString);
        Assert.Equal("testdb", options.DefaultDatabaseName);

        Assert.Throws<OptionsValidationException>(() => optionsMonitor.Get("NonRegisteredClient"));
    }

    [Fact]
    public void AddMongo_PostConfigures_ExplicitOptionsHavePriority()
    {
        // Arrange
        var configurationDict = new Dictionary<string, string?>
        {
            ["Mongo:ConnectionString"] = "mongodb://configsection:27017",
            ["Mongo:DefaultDatabaseName"] = "testdb",
        };
        var services = new ServiceCollection();
        services.AddConfiguration(configurationDict);
        services.AddLogging();

        // Act
        services.AddMongo(options =>
        {
            options.ConnectionString = "mongodb://explicit:27017";
            options.DefaultDatabaseName = "explicitdb";
        });
        using var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetRequiredService<IOptions<MongoClientOptions>>().Value;
        Assert.Equal("mongodb://explicit:27017", options.ConnectionString);
        Assert.Equal("explicitdb", options.DefaultDatabaseName);
    }

    [Fact]
    public void AddMongo_PostConfigures_DatabaseNameFromConnectionString()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMongo(x => x.ConnectionString = "mongodb://localhost:27017/dbFromConnectionString");
        using var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetRequiredService<IOptions<MongoClientOptions>>().Value;
        Assert.Equal("dbFromConnectionString", options.DefaultDatabaseName);

        // Verify we can get the default database
        var database = serviceProvider.GetRequiredService<IMongoDatabase>();
        Assert.NotNull(database);
        Assert.Equal("dbFromConnectionString", database.DatabaseNamespace.DatabaseName);
    }

    [Fact]
    public void AddNamedMongo_PostConfigures_DatabaseNameFromConnectionString()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMongo().AddNamedClient("CustomName", x => x.ConnectionString = "mongodb://localhost:27017/dbFromConnectionString");
        using var serviceProvider = services.BuildServiceProvider();

        // Assert
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<MongoClientOptions>>();
        var options = optionsMonitor.Get("CustomName");
        Assert.Equal("dbFromConnectionString", options.DefaultDatabaseName);

        // The default client isn't configured
        Assert.Throws<OptionsValidationException>(serviceProvider.GetRequiredService<IMongoDatabase>);
    }

    [Fact]
    public void AddMongo_Validates_FailsWhenConnectionStringIsNotProvided()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongo();

        // Act
        using var serviceProvider = services.BuildServiceProvider();

        // Verify we cannot get a client due to validation failure
        var ex = Assert.Throws<OptionsValidationException>(serviceProvider.GetRequiredService<IMongoClient>);
        Assert.Contains(nameof(MongoClientOptions.ConnectionString), ex.Message);
    }

    [Fact]
    public void AddMongo_Validates_FailsWhenConnectionStringIsInvalid()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongo(x => x.ConnectionString = "invalid-connection-string");

        // Act
        using var serviceProvider = services.BuildServiceProvider();

        // Assert
        var ex = Assert.Throws<OptionsValidationException>(serviceProvider.GetRequiredService<IMongoClient>);
        Assert.Contains(nameof(MongoClientOptions.ConnectionString), ex.Message);
    }

    [Fact]
    public void AddMongo_Validates_FailsWhenDefaultDatabaseNameIsNotProvided()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongo(x => x.ConnectionString = "mongodb://localhost:27017");

        // Act
        using var serviceProvider = services.BuildServiceProvider();

        // Assert
        var ex = Assert.Throws<OptionsValidationException>(serviceProvider.GetRequiredService<IMongoClient>);
        Assert.Contains(nameof(MongoClientOptions.DefaultDatabaseName), ex.Message);
    }

    [Fact]
    public void AddMongo_Validates_FailsWhenIndexingPropertiesAreInvalid()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongo(options =>
        {
            options.ConnectionString = "mongodb://localhost:27017";
            options.DefaultDatabaseName = "testdb";
            options.Indexing.DistributedLockName = string.Empty;
            options.Indexing.LockMaxLifetimeInSeconds = 0;
            options.Indexing.LockAcquisitionTimeoutInSeconds = 0;
        });

        // Act & Assert
        using var serviceProvider = services.BuildServiceProvider();
        var ex = Assert.Throws<OptionsValidationException>(serviceProvider.GetRequiredService<IMongoClient>);

        Assert.Contains("DistributedLockName", ex.Message);
        Assert.Contains("LockMaxLifetimeInSeconds", ex.Message);
        Assert.Contains("LockAcquisitionTimeoutInSeconds", ex.Message);
    }

    [Fact]
    public void PostConfigure_DoesNothing_WhenConfigurationIsNullAndConnectionStringIsProvided()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongo(options =>
        {
            options.ConnectionString = "mongodb://localhost:27017";
            options.DefaultDatabaseName = "testdb";
        });

        // Act
        using var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.Null(serviceProvider.GetService<IConfiguration>());

        var options = serviceProvider.GetRequiredService<IOptions<MongoClientOptions>>().Value;
        Assert.Equal("mongodb://localhost:27017", options.ConnectionString);
        Assert.Equal("testdb", options.DefaultDatabaseName);
    }
}

file static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConfiguration(this IServiceCollection services, Dictionary<string, string?> initialData)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(initialData)
            .Build();

        return services.AddSingleton<IConfiguration>(configuration);
    }
}
