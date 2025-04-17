using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Workleap.Extensions.Mongo.Indexing;
using Workleap.Extensions.Mongo.Security;

namespace Workleap.Extensions.Mongo;

public static class MongoServiceCollectionExtensions
{
    private static readonly MethodInfo ConfigureMethod = typeof(MongoServiceCollectionExtensions).GetMethod(nameof(Configure), BindingFlags.NonPublic | BindingFlags.Static)
                                                         ?? throw new InvalidOperationException($"Could not find public instance method {nameof(MongoServiceCollectionExtensions)}.{nameof(Configure)}");

    private static readonly object AddConfigurationLockObject = new();

    private static ConcurrentBag<Type> RegisteredConfigurations { get; } = new();

    public static MongoBuilder AddMongo(this IServiceCollection services, Action<MongoClientOptions>? configure = null)
    {
        services.ConfigureOptions<ConfigureMongoStaticOptions>();

        if (configure != null)
        {
            services.Configure(MongoDefaults.ClientName, configure);
        }

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<MongoClientOptions>, PostConfigureMongoClientOptions>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<MongoClientOptions>, ValidateMongoClientOptions>());

        services.TryAddSingleton<IMongoClientProvider, MongoClientProvider>();

        // MongoDB C# driver documentation says that IMongoClient, IMongoDatabase and IMongoCollection<> are thread-safe and can be stored globally (i.e. as singletons):
        // https://mongodb.github.io/mongo-csharp-driver/2.10/reference/driver/connecting/
        services.TryAddSingleton(CreateDefaultMongoClient);
        services.TryAddSingleton(CreateDefaultMongoDatabase);

        // Inspired from Microsoft's ILogger<> singleton service descriptor:
        // https://github.com/dotnet/runtime/blob/v6.0.0/src/libraries/Microsoft.Extensions.Logging/src/LoggingServiceCollectionExtensions.cs#L42
        services.TryAdd(ServiceDescriptor.Singleton(typeof(IMongoCollection<>), typeof(MongoCollectionProxy<>)));

        services.TryAddSingleton<MongoStaticInitializer>();
        services.TryAddSingleton<IMongoIndexer, MongoIndexer>();
        services.TryAddSingleton<IMongoValueEncryptor, NoopMongoValueEncryptor>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMongoEventSubscriberFactory, MongoEventSubscriberFactory>());

        return new MongoBuilder(services);
    }

    /// <summary>
    /// Adds Configuration based support for Mongo Collection Names, Index Providers and Class maps.
    /// Each Collection requires a name. Index Providers are optional, as are Class maps.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="assemblies">Assemblies in which we can find the implementations of IMongoCollectionConfiguration</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static MongoBuilder AddCollectionConfigurations(this MongoBuilder builder, params Assembly[] assemblies)
    {
        var configurationTypes = GetMongoCollectionConfigurationTypes(assemblies);

        var registeredDocumentTypes = new HashSet<Type>();

        lock (AddConfigurationLockObject)
        {
            foreach (var configurationType in configurationTypes)
            {
                var (concreteType, configurationInterface) = configurationType;

                if (RegisteredConfigurations.Contains(concreteType))
                {
                    continue;
                }

                var configuration = GetMongoCollectionConfiguration(concreteType);

                var documentType = configurationInterface.GetGenericArguments().Single();

                if (!registeredDocumentTypes.Add(documentType))
                {
                    throw new InvalidOperationException($"Cannot register multiple configurations for the same document type {documentType}");
                }

                var builderType = typeof(MongoCollectionBuilder<>).MakeGenericType(documentType);

                if (Activator.CreateInstance(builderType) is not MongoCollectionBuilder configurationBuilder)
                {
                    throw new InvalidOperationException($"Cannot create {builderType}");
                }

                var metadata = GetMongoCollectionMetadata(documentType, configuration, configurationBuilder);

                if (string.IsNullOrWhiteSpace(metadata.CollectionName))
                {
                    throw new ArgumentNullException($"{builderType} must specify a CollectionName");
                }

                MongoCollectionInformationCache.SetCollectionInformation(documentType, metadata.CollectionName!, metadata.DatabaseName);

                MongoConfigurationIndexStore.AddIndexProviderType(documentType, metadata.IndexProviderType);

                RegisteredConfigurations.Add(concreteType);
            }
        }

        return builder;
    }

    private static object GetMongoCollectionConfiguration(Type concreteType)
    {
        concreteType.EnsureHasPublicParameterlessConstructor();

        var configuration = Activator.CreateInstance(concreteType);

        return configuration ?? throw new InvalidOperationException($"Cannot create {concreteType}");
    }

    private static MongoCollectionMetadata GetMongoCollectionMetadata(Type documentType, object configuration, MongoCollectionBuilder configurationBuilder)
    {
        var configureMethod = ConfigureMethod.MakeGenericMethod(documentType);

        configureMethod.Invoke(null, new[] { configuration, configurationBuilder });

        var metadata = configurationBuilder.Build(); // BsonClassMap registration happens here

        return metadata;
    }

    private static IEnumerable<(Type ConcreteType, Type Interface)> GetMongoCollectionConfigurationTypes(Assembly[] assemblies)
    {
        var configurationTypes = assemblies.SelectMany(assembly => assembly.GetTypes()
            .Where(t => !t.IsAbstract)
            .Select(t => (ConcreteType: t, Interface: t.GetInterfaces().FirstOrDefault(i => i.IsMongoCollectionConfigurationInterface())))
            .Where(t => t.Interface != null))
            .OfType<(Type, Type)>();

        return configurationTypes;
    }

    private static IMongoClient CreateDefaultMongoClient(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<IMongoClientProvider>().GetClient(MongoDefaults.ClientName);
    }

    private static IMongoDatabase CreateDefaultMongoDatabase(IServiceProvider serviceProvider)
    {
        var client = serviceProvider.GetRequiredService<IMongoClient>();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<MongoClientOptions>>().Get(MongoDefaults.ClientName);
        return client.GetDatabase(options.DefaultDatabaseName);
    }

    private static void Configure<TDocument>(IMongoCollectionConfiguration<TDocument> configuration, IMongoCollectionBuilder<TDocument> configurationBuilder)
        where TDocument : class
    {
        configuration.Configure(configurationBuilder);
    }
}