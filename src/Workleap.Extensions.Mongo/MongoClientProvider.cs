using System.Collections.Concurrent;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;

namespace Workleap.Extensions.Mongo;

internal sealed class MongoClientProvider : IMongoClientProvider, IDisposable
{
    // Default socket timeout is infinite, that's a real problem as we already experienced 15+ minutes socket timeout with this default setting
    // https://github.com/mongodb/mongo-csharp-driver/blob/v2.17.1/src/MongoDB.Driver/MongoDefaults.cs#L43
    private static readonly TimeSpan DefaultInfiniteSocketTimeout = TimeSpan.Zero;

    // https://github.com/mongodb/mongo-csharp-driver/blob/v2.17.1/src/MongoDB.Driver/MongoDefaults.cs#L32
    private static readonly TimeSpan DefaultThirtySecondsConnectTimeout = TimeSpan.FromSeconds(30);

    // https://github.com/mongodb/mongo-csharp-driver/blob/v2.25.0/src/MongoDB.Driver/MongoDefaults.cs#L34
    private static readonly TimeSpan DefaultTenMinutesMaxConnectionIdleTime = TimeSpan.FromMinutes(10);

    // Officevibe also uses a 60 seconds socket timeout, it's better than the infinite default
    private static readonly TimeSpan ReasonableSocketTimeout = TimeSpan.FromSeconds(60);

    // Officevibe also uses a 10 seconds connect timeout
    private static readonly TimeSpan ReasonableConnectTimeout = TimeSpan.FromSeconds(10);

    // Officevibe also uses a 60 seconds max connection idle time
    private static readonly TimeSpan ReasonableMaxConnectionIdleTime = TimeSpan.FromSeconds(60);

    private readonly IServiceProvider _serviceProvider;
    private readonly List<IDisposable> _disposableDependencies;

    // MongoDB C# driver documentation says that IMongoClient, IMongoDatabase and IMongoCollection<> are thread-safe and can be stored globally (i.e. as singletons):
    // https://mongodb.github.io/mongo-csharp-driver/2.10/reference/driver/connecting/
    private readonly ConcurrentDictionary<string, Lazy<IMongoClient>> _mongoClients;

    public MongoClientProvider(IServiceProvider serviceProvider)
    {
        this._serviceProvider = serviceProvider;
        this._mongoClients = new ConcurrentDictionary<string, Lazy<IMongoClient>>(StringComparer.Ordinal);
        this._disposableDependencies = new List<IDisposable>();
    }

    public IMongoClient GetClient(string clientName)
    {
        if (clientName == null)
        {
            throw new ArgumentNullException(nameof(clientName));
        }

        return this._mongoClients.GetOrAdd(clientName, this.CreateLazyMongoClient).Value;
    }

    private Lazy<IMongoClient> CreateLazyMongoClient(string clientName)
    {
        return new Lazy<IMongoClient>(() => this.CreateMongoClient(clientName));
    }

    private IMongoClient CreateMongoClient(string clientName)
    {
        this._serviceProvider.GetRequiredService<MongoStaticInitializer>().Initialize();

        var options = this._serviceProvider.GetRequiredService<IOptionsMonitor<MongoClientOptions>>().Get(clientName);
        var settings = MongoClientSettings.FromConnectionString(options.ConnectionString);

        if (settings.SocketTimeout == DefaultInfiniteSocketTimeout)
        {
            settings.SocketTimeout = ReasonableSocketTimeout;
        }

        if (settings.ConnectTimeout == DefaultThirtySecondsConnectTimeout)
        {
            settings.ConnectTimeout = ReasonableConnectTimeout;
        }

        if (settings.MaxConnectionIdleTime == DefaultTenMinutesMaxConnectionIdleTime)
        {
            settings.MaxConnectionIdleTime = ReasonableMaxConnectionIdleTime;
        }

        var eventSubscriberFactories = this._serviceProvider.GetServices<IMongoEventSubscriberFactory>();
        var eventSubscribers = eventSubscriberFactories.SelectMany(x => x.CreateEventSubscribers(clientName)).ToList();
        options.PostConfigureEventSubscribers?.Invoke(eventSubscribers);

        this._disposableDependencies.AddRange(eventSubscribers.OfType<IDisposable>());

        // Allow consumers to override mongo client settings
        options.MongoClientSettingsConfigurator?.Invoke(settings);

        var userDefinedClusterConfiguration = settings.ClusterConfigurator;

        settings.ClusterConfigurator = builder =>
        {
            userDefinedClusterConfiguration?.Invoke(builder);
            builder.ConfigureTcp(EnableKeepAlives);
            builder.Subscribe(new OrderedAggregatorEventSubscriber(eventSubscribers));
        };

        return new MongoClient(settings);
    }

    private static TcpStreamSettings EnableKeepAlives(TcpStreamSettings tcpStreamSettings)
    {
        return tcpStreamSettings.With(socketConfigurator: (Action<Socket>)SocketConfigurator);
        static void SocketConfigurator(Socket s) => s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
    }

    public void Dispose()
    {
        foreach (var disposable in this._disposableDependencies)
        {
            disposable.Dispose();
        }
    }
}