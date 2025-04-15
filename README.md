# Workleap.Extensions.Mongo

[![nuget](https://img.shields.io/nuget/v/Workleap.Extensions.Mongo.svg?logo=nuget)](https://www.nuget.org/packages/Workleap.Extensions.Mongo/)
[![build](https://img.shields.io/github/actions/workflow/status/workleap/wl-extensions-mongo/publish.yml?logo=github&branch=main)](https://github.com/workleap/wl-extensions-mongo/actions/workflows/publish.yml)

Workleap.Extensions.Mongo is a convenient set of .NET libraries designed to enhance and streamline the [MongoDB C# driver](https://github.com/mongodb/mongo-csharp-driver) integration into your C# projects.

## Value proposition and features overview

Integrating the MongoDB C# driver into your C# projects can often lead to several questions:

* What's the optimal way to configure a MongoDB client from my app configuration? Should I use `appsettings.json`, environment variables, custom option classes, plain C# code?
* How can I expose the MongoDB client to the rest of my code? Should I use dependency injection?
* What are the best practices for configuring the MongoDB client? How should MongoDB C# driver static settings be handled?
* If I want to support multiple MongoDB clusters and/or databases, won't that require a significant refactor of my existing code?
* What's the most effective way to manage my indexes? Should they be created manually, or within my C# code? How can I ensure that indexes are synchronized with my code?
* If I have multiple C# applications, how can I prevent MongoDB setup code duplication?
* How can I instrument my code, considering the MongoDB C# driver doesn't natively support OpenTelemetry?
* How can I execute integration tests in an isolated environment? Using a shared database requires cleanup, leading to unreliable test results.

**Workleap.Extensions.Mongo** was developed to address these challenges. We offer a straightforward, flexible, and standard approach to adding and configuring the MongoDB C# driver in your C# projects. Here is an overview of the features of Workleap.Extensions.Mongo:

* **Support for dependency injection**: We use [Microsoft's modern dependency injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection) (`IServiceCollection`) to expose MongoDB's classes and interfaces.
* **Standardized configuration**: We leverage [Microsoft's configuration](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration), enabling easy configuration of MongoDB settings via diverse configuration providers of your choice. The [options pattern](https://learn.microsoft.com/en-us/dotnet/core/extensions/options) simplifies overriding and extending any setting, including static MongoDB settings (custom serializers and convention packs).
* **Support for multiple MongoDB clusters and/or databases**: You won't need to refactor your entire codebase to support multiple MongoDB data sources - it's supported by default.
* **Elimination of boilerplate and duplicated code**: Remove redundant, copy-pasted MongoDB C# code from your codebase, enabling you to focus on actually utilizing the driver.
* **Built-in instrumentation**: We provide built-in support for OpenTelemetry instrumentation, adhering to  [OpenTelemetry's semantic conventions for MongoDB](https://opentelemetry.io/docs/specs/semconv/database/mongodb/). Additionally, we offer an extra NuGet package for Application Insights .NET SDK support.
* **Optional index management**: Declare indexes in your C# code, and then use our C# API to automatically create and update indexes based on what's declared in your code. Built-in Roslyn analyzers will assist developers in considering new indexes.
* **Optional async enumerables support**: You can simplify your code by using our extension methods that employ `IAsyncEnumerable` rather than more verbose MongoDB cursors.
* **Optional field-level encryption**: Implement your own encrypt and decrypt methods, which can then automatically encrypt annotated MongoDB document fields for at-rest security.
* **Optional ephemeral database for integration tests**: Each of your integration test methods can have its own new MongoDB database operating locally.

## Getting started

We offer three main NuGet packages:

**Firstly**, [Workleap.Extensions.Mongo](https://www.nuget.org/packages/Workleap.Extensions.Mongo/), is the package that you'd ideally install in your *startup project*, where your main method resides. This is where you would incorporate our library into your dependency injection services and link your configuration to our option classes:

```csharp
// Method 1: Directly set the options values with C# code
services.AddMongo(options =>
{
    options.ConnectionString = "[...]";
    options.DefaultDatabaseName = "marketing";
});

// Method 2: Bind the options values to a configuration section
services.AddMongo(configuration.GetRequiredSection("Mongo").Bind);

// Method 3: Lazily bind the options values to a configuration section
services.AddMongo();
services.AddOptions<MongoClientOptions>().Bind(configuration.GetRequiredSection("Mongo"));

// appsettings.json (or any other configuration source such as environment variables or Azure KeyVault)
{
  "Mongo": {
    "ConnectionString": "[...]",
    "DefaultDatabaseName": "marketing"
  }
}

// Method 4: Implement IConfigureNamedOptions<MongoClientOptions>:
// https://learn.microsoft.com/en-us/dotnet/core/extensions/options#use-di-services-to-configure-options
```

**The second NuGet package**, [Workleap.Extensions.Mongo.Abstractions](https://www.nuget.org/packages/Workleap.Extensions.Mongo.Abstractions/), only provides abstractions and extension methods, aligning with the principles of Microsoft's extension libraries such as [Microsoft.Extensions.Logging.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions/) and [Microsoft.Extensions.Configuration.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Abstractions/). You would typically install this package in your domain-specific .NET projects to avoid unnecessary NuGet dependencies. **This package already includes the MongoDB C# driver**, so there's no need to install it separately (unless you need a specific version).

```csharp
// 1) Directly inject a collection bound to the default database
var people = serviceProvider.GetRequiredService<IMongoCollection<PersonDocument>>();

// 2) You can inject the default database
var people = serviceProvider.GetRequiredService<IMongoDatabase>().GetCollection<PersonDocument>();

// 3) You can inject the default client
var people = serviceProvider.GetRequiredService<IMongoClient>()
    .GetDatabase("marketing").GetCollection<PersonDocument>();

// 4) Finally, you can inject a specific client for a specific registered MongoDB cluster
// More on that later in this document
var people = serviceProvider.GetRequiredService<IMongoClientProvider>()
    .GetClient("mycluster").GetDatabase("marketing").GetCollection<PersonDocument>();

[MongoCollection("people")]
public class PersonDocument : IMongoDocument // IMongoDocument is an empty marker interface (required)
{
    // [...]
}
```

**The third NuGet package**, [Workleap.Extensions.Mongo.Ephemeral](https://www.nuget.org/packages/Workleap.Extensions.Mongo.Ephemeral/), is designed for your integration tests. It can be utilized whenever you require a real yet ephemeral MongoDB cluster with a single node replica set. Through dependency injection, each integration test method can have access to a unique and isolated database.

## Adding and configuring MongoDB clients

When registering your dependency injection services, you can invoke `services.AddMongo(...)` as demonstrated in the previous section. This action registers MongoDB dependencies and the main MongoDB cluster by providing the connection string and default (primary) database name.

It is also possible to register multiple additional MongoDB clusters:

```csharp
services.AddMongo(options => { /* [...] */ })
    .AddNamedClient("anotherCluster", options => { /* [...] */ })
    .AddNamedClient("andAnotherOne", options => { /* [...] */ });

// There are many ways to configure these named options
services.AddOptions<MongoClientOptions>("anotherCluster").Bind(configuration.GetRequiredSection("Mongo:AnotherCluster"));
services.AddOptions<MongoClientOptions>("andAnotherOne").Bind(configuration.GetRequiredSection("Mongo:AndAnotherOne"));
```

The `MongoClientOptions` option class further permits you to configure the `MongoClientSettings` for a cluster:

```csharp
services.AddMongo(options =>
{
    options.MongoClientSettingsConfigurator = settings =>
    {
        settings.ApplicationName = "myapp";

        settings.ClusterConfigurator = cluster =>
        {
            // [...]
        };
    };
});
```

While `MongoClientOptions` is the option class for configuring a specific MongoDB cluster connection, `MongoStaticOptions` is available to customize MongoDB static options such as BSON serializers and convention packs for the entire application:

```csharp
services.AddMongo().ConfigureStaticOptions(options =>
{
    // There are built-in serializers and conventions registered, but you can remove or override them
    // ⚠ Caution, these are objects that will live for the entire lifetime of the application (singleton) as the MongoDB C# driver
    // uses static properties to configure its behavior and serialization
    options.GuidRepresentationMode = GuidRepresentationMode.V2; // V3 is the default
    options.BsonSerializers[typeof(Guid)] = new GuidSerializer(GuidRepresentation.Standard);
    options.ConventionPacks.Add(new MyConventionPack());
});
```

You can also use different options patterns to configure static options:

```csharp
services.AddOptions<MongoStaticOptions>().Configure(options => { /* [...] */ })
```

When using the `AddMongo()` method, multiple conventions are added automatically:
- `IgnoreExtraElementsConvention(ignoreExtraElements: true)`
- `EnumRepresentationConvention(BsonType.String)`, so changing an enum member name is a breaking change
- `DateTime` and `DateTimeOffset` are serialized as `DateTime` instead of the default Ticks or (Ticks, Offset). In MongoDB, DateTime only supports precision up to the milliseconds. If you need more precision, you need to set the serializer at property level.

## Declaring Mongo Documents
### With Attributes Decoration

The process doesn't deviate much from the standard way of declaring and using MongoDB collections in C#. However, there are two additional steps:

* You must decorate the document class with the `MongoCollectionAttribute` to specify the collection name,
* The document class must implement the empty marker interface `IMongoDocument` for generic constraints purposes.

```csharp
[MongoCollection("people")]
public class PersonDocument : IMongoDocument
{
    // [...]
}
```

For multiple databases setup, you can specify the database name directly on the attribute. This will have the following effect:
* The index creation from `UpdateIndexesAsync` can be invoked on the assembly and the indexer will use the proper database
* The injection of `IMongoCollection<TDocument>` will work, regardless of which database the collection is bound to

```csharp
[MongoCollection("people", DatabaseName = "foo")]
public class PersonDocument : IMongoDocument
{
    // [...]
}
```

### With Configuration

In certain scenarios, like in Domain Driven Design (DDD), one would like to persist their Domain Aggregates as is in the Document Database. These Domain objects are not aware of how they are persisted. They cannot be decorated with Persistence level attributes (ie `[MongoCollection()]`), nor can they implement `IMongoDocument`.

You can configure your Object to Database mapping throught `IMongoCollectionConfiguration<TDocument>` instead.

```csharp
public sealed class Person
{
    // [...]
}
```

```csharp
internal sealed class PersonConfiguration: IMongoCollectionConfiguration<Person>
{
    public void Configure(IMongoCollectionBuilder<Person> builder) 
    {
        builder.CollectionName("people");
        builder.DatabaseName("foo"); // optional, not calling this will use the default database
    }
}
```

#### Bootstrapping Configurations

Since the Configuration approach uses reflection to find the implementations of `IMongoCollectionConfiguration<T>` during the startup, we have to tell the library that we opt-in the Configuration mode by calling AddCollectionConfigurations and pass it the Assemblies where you can locate the Configurations.

```csharp
services.AddMongo().AddCollectionConfigurations(InfrastructureAssemblyHandle.Assembly);
```

## Usage
Refer back to the [getting started section](#getting-started) to learn how to resolve `IMongoCollection<TDocument>` from the dependency injection services.

## Extensions
We also provide `IAsyncEnumerable<TDocument>` extensions on `IAsyncCursor<TDocument>` and `IAsyncCursorSource<TDocument>`, eliminating the need to deal with cursors. For example:

```csharp
var people = await collection.Find(FilterDefinition<PersonDocument>.Empty).ToAsyncEnumerable();
```

## Property Mapping

You can use Mongo Attributes for Property Mapping, or BsonClassMaps. However, if you are using Configuration, you probably do not want to use Attributes on your Models.

### With Attributes

```csharp
[MongoCollection("people")]
public class PersonDocument : IMongoDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("n")]
    public string Name { get; set; } = string.Empty;
}
```

[Mapping Models with Attributes](https://www.mongodb.com/docs/drivers/csharp/v2.19/fundamentals/serialization/poco/)

### With Configuration

```csharp
internal sealed class PersonConfiguration: IMongoCollectionConfiguration<Person>
{
    public void Configure(IMongoCollectionBuilder<Person> builder) 
    {
        builder.CollectionName("people")
            .BsonClassMap(map => 
            {
                map.MapIdProperty(x => x.Id)
                    .SetSerializer(new StringSerializer(BsonType.ObjectId));
                
                map.MapProperty(x => x.Name).SetElementName("n");
            });
    }
}
```

[Mapping Models with ClassMaps](https://www.mongodb.com/docs/drivers/csharp/v2.19/fundamentals/serialization/class-mapping/)

## Logging and distributed tracing

**Workleap.Extensions.Mongo** supports modern logging with `ILogger` and log level filtering. MongoDB commands can be logged at the `Debug` level and optionally with their BSON content only if you set `MongoClientOptions.Telemetry.CaptureCommandText` to `true`.

**Distributed tracing** with OpenTelemetry is also integrated. We follow the [semantic conventions for MongoDB](https://opentelemetry.io/docs/specs/semconv/database/mongodb/). You can simply observe activities (traces) originating from our `Workleap.Extensions.Mongo` assembly.

We also support distributed tracing with the [Application Insights .NET SDK](https://github.com/microsoft/ApplicationInsights-dotnet). To enable this feature, you need to install the additional [Workleap.Extensions.Mongo.ApplicationInsights NuGet package](https://www.nuget.org/packages/Workleap.Extensions.Mongo.ApplicationInsights/). Simply use the `.AddApplicationInsights()` on the builder object returned by `services.AddMongo()`:

```csharp
services.AddMongo().AddApplicationInsights();
```

By default, some commands such as `isMaster`, `buildInfo`, `saslStart`, etc., are ignored by our instrumentation. You can either ignore additional commands or undo the ignoring of commands by modifying the `MongoClientOptions.Telemetry.DefaultIgnoredCommandNames` collection.
Additionally, subscribing to more granular driver diagnostic events can be done by setting `MongoClientOptions.Telemetry.CaptureDiagnosticEvents` to `true`.

## Index management

We provide a mechanism for you to declare your collection indexes and ensure they are applied to your database. To do this, declare your indexes by implementing a custom `MongoIndexProvider<TDocument>`:

```csharp
public class PersonDocumentIndexes : MongoIndexProvider<PersonDocument>
{
    public override IEnumerable<CreateIndexModel<PersonDocument>> CreateIndexModels()
    {
        // Index name is mandatory
        yield return new CreateIndexModel<PersonDocument>(
            Builders<PersonDocument>.IndexKeys.Combine().Ascending(x => x.Name),
            new CreateIndexOptions { Name = "name" });
    }
}
```

### With Attributes Decoration

```csharp
[MongoCollection("people", IndexProviderType = typeof(PersonDocumentIndexes))]
public class PersonDocument : IMongoDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
```

### With Configuration

```csharp
internal sealed class PersonConfiguration: IMongoCollectionConfiguration<Person>
{
    public void Configure(IMongoCollectionBuilder<Person> builder) 
    {
        builder.IndexProvider<PersonDocumentIndexes>();
    }
}
```

### Updating Indexes

At this stage, nothing will happen. To actually create or update the index, you need to inject our `IMongoIndexer` service and then call one of its `UpdateIndexesAsync()` method overloads, for example:

```csharp
var indexer = this.Services.GetRequiredService<IMongoIndexer>();
await indexer.UpdateIndexesAsync(new[] { typeof(PersonDocument) });
```

**It is up to you to decide when and where to run the process of creating and updating the indexes**. You could do it at the start of your application, in a separate application that runs in a continuous delivery pipeline, etc.

Our indexation engine handles:

1. Discovering the `CreateIndexModel<TDocument>` declared in your code using reflection.
2. Computing a **unique index name** based on the model (we append a hash to the provided index name, for example, `name_512cbbb935626e2b4b7c44972597c4a8`).
3. Discovering existing indexes in the MongoDB collection.
4. Comparing the names and hashes of both sides to determine if:
   1. We need to create a missing index.
   2. We need to drop and recreate an updated index (the hashes don't match).
   3. We need to drop an index that is no longer declared in your code.
5. Leaving any other index intact. We only manage the indexes that have a name ending with a generated hash.
6. Handling distributed race conditions. If many instances of an application call `indexer.UpdateIndexesAsync()` at the same time, only one will actually succeed (we use a distributed lock).

**Note:**
We do not recommend you try and run multiple `UpdateIndexesAsync` tasks at the same time given that only one process can update indexes at a time through the use of a distributed lock. 
For example in the code below, once a task has acquired the distributed lock, the other will wait until the lock is released before acquiring it and running the index update process.
In the end you're not saving any time by having multiple tasks run at the same time.

```csharp
// ⚠️ Updating indexes in parallel is not recommended
var indexer = this.Services.GetRequiredService<IMongoIndexer>();
await Task.WhenAll(
    indexer.UpdateIndexesAsync(AssemblyHandle.Assembly),
    indexer.UpdateIndexesAsync(new[] { typeof(PersonDocument) })
);
```

Ideally if all your indexes are in the same assembly then you only have to call `UpdateIndexesAsync` once.
But if you really do need to call it multiple times, then the code above should be re-written to the following:
```csharp
var indexer = this.Services.GetRequiredService<IMongoIndexer>();
await indexer.UpdateIndexesAsync(AssemblyHandle.Assembly);
await indexer.UpdateIndexesAsync(new[] { typeof(PersonDocument) })
```

> We include a Roslyn analyzer, detailed in a section below, that encourages developers to adorn classes that consume MongoDB collections with attributes (`IndexByAttribute` or `NoIndexNeededAttribute`). The aim is to increase awareness about which indexes should be used (or created) when querying MongoDB collections.

### Support for inheritance

The indexer mechanism support document inheritance and different indexer for a same collection. For example:

```csharp 

[BsonKnownTypes(typeof(DogPersonDocument), typeof(DogPersonDocument))]
[MongoCollection("people", IndexProviderType = typeof(PersonDocumentIndexes))]
public class PersonDocument : IMongoDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class PersonDocumentIndexes : MongoIndexProvider<PersonDocument>
{
    public override IEnumerable<CreateIndexModel<PersonDocument>> CreateIndexModels()
    {
        yield return new CreateIndexModel<PersonDocument>(
            Builders<PersonDocument>.IndexKeys.Combine().Ascending(x => x.Name),
            new CreateIndexOptions { Name = "name" });
    }
}

// No special indexer for this class
[BsonDiscriminator("Dog")]
public class DogPersonDocument : PersonDocument
{
    public int DogCount { get; set; } = string.Empty;
}

// Need to redefine MongoCollectionAttribute to use a different indexer
[BsonDiscriminator("Cat")]
[MongoCollection("people", IndexProviderType = typeof(CatDocumentIndexes))]
public class CatPersonDocument : PersonDocument
{
    public int CatCount { get; set; }
}

public class CatDocumentIndexes : MongoIndexProvider<PersonDocument>
{
    public override IEnumerable<CreateIndexModel<PersonDocument>> CreateIndexModels()
    {
        yield return new CreateIndexModel<PersonDocument>(
            Builders<PersonDocument>.IndexKeys.Combine().Ascending(x => x.CatCount),
            new CreateIndexOptions { Name = "cat_count" });
    }
}
```

## Field encryption

The [Workleap.Extensions.Mongo](https://www.nuget.org/packages/Workleap.Extensions.Mongo/) library supports field-level encryption at rest, which means you can specify in your C# code which document fields should be encrypted in your MongoDB database. Any C# property can be encrypted, as long as you provide how data gets encrypted and decrypted. These properties then become binary data in your documents.

To enable field-level encryption, simply decorate the sensitive properties with the `[SensitiveInformationAttribute]`:

```csharp
[MongoCollection("people")]
public class PersonDocument : IMongoDocument
{
    // [...]

    [SensitiveInformation(SensitivityScope.User)] // Other values are "Tenant" and "Application"
    public string Address { get; set; } = string.Empty;
}
```

Next, create a class that implements `IMongoValueEncryptor`:

```csharp
public class MyMongoalueEncryptor : IMongoValueEncryptor
{
    public byte[] Encrypt(byte[] bytes, SensitivityScope sensitivityScope)
    {
        // return protected bytes using the method of your choice
    }

    public byte[] Decrypt(byte[] bytes, SensitivityScope sensitivityScope)
    {
        // return unprotected bytes using the method of your choice
    }
}
```

Finally, register this class in the dependency injection services:

```csharp
// This ends up registered using the singleton service lifetime
services.AddMongo().AddEncryptor<MyMongoalueEncryptor>();
```

Keep in mind that encrypted values become binary data, which can make querying them more difficult. You'll need to take this into account when designing your database schema and queries.

## Ephemeral MongoDB databases for integration tests

When creating integration tests, instead of using a shared MongoDB database for all your tests, you could assign a brand new ephemeral database for each individual test method. This approach reduces test flakiness, prevents the state of one test from impacting others and remove the need for manual or automatic cleanup.

This is what our NuGet package [Workleap.Extensions.Mongo.Ephemeral](https://www.nuget.org/packages/Workleap.Extensions.Mongo.Ephemeral/) does when you invoke its `UseEphemeralRealServer()` method:

```csharp
services.AddMongo().UseEphemeralRealServer();
```

When this method is called, each time a database or collection is requested within the scope of an individual `IServiceProvider`:
* A MongoDB server starts (we use [EphemeralMongo](https://github.com/asimmon/ephemeral-mongo)),
* A randomly named database is provided to your code.

When you dispose of the `IServiceProvider`, the related resources are destroyed. We leverage internal caching to avoid running multiple instances of MongoDB servers concurrently, opting instead to reuse a single instance. This method allows you to run multiple concurrent tests, each with their own MongoDB database. If your test runner crashes, the MongoDB process will be terminated, preventing orphaned processes from consuming unnecessary resources.

Available environment variables:
- `WORKLEAP_EXTENSIONS_MONGO_EPHEMERAL_BINARYDIRECTORY`: Specify the path of the MongoDB binaries
- `WORKLEAP_EXTENSIONS_MONGO_EPHEMERAL_DATADIRECTORY`: Specify the path to store data
- `WORKLEAP_EXTENSIONS_MONGO_EPHEMERAL_CONNECTIONTIMEOUT`: Specify the timeout to connect to the database
- `WORKLEAP_EXTENSIONS_MONGO_EPHEMERAL_USESINGLENODEREPLICASET`: Configure the replicaset

## Included Roslyn analyzers

| Rule ID | Category | Severity | Description                                                        |
|---------|----------|----------|--------------------------------------------------------------------|
| GMNG01  | Design   | Warning  | Add 'IndexBy' or 'NoIndexNeeded' attributes on the containing type |

To modify the severity of one of these diagnostic rules, you can use a `.editorconfig` file. For example:

```ini
## Disable analyzer for test files
[**Tests*/**.cs]
dotnet_diagnostic.GMNG01.severity = none
```

To learn more about configuring or suppressing code analysis warnings, refer to [this documentation](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/suppress-warnings).

## License

Copyright © 2023, Workleap. This code is licensed under the Apache License, Version 2.0. You may obtain a copy of this license at https://github.com/workleap/gsoft-license/blob/master/LICENSE.