using System.Collections.Concurrent;
using System.Globalization;
using EphemeralMongo;

namespace Workleap.Extensions.Mongo.Ephemeral;

#pragma warning disable EMEX0001 // MongoRunnerPool is marked as experimental

internal sealed class ReusableMongoRunnerProvider : IDisposable
{
    private static readonly ConcurrentDictionary<string, Lazy<MongoRunnerPool>> LazyRunnerPools = new(StringComparer.Ordinal);

    private readonly Dictionary<string, List<IMongoRunner>> _rentedRunnersByClientName = [];

    public IMongoRunner GetRunner(string clientName)
    {
        var runner = LazyRunnerPools.GetOrAdd(clientName, CreateLazyMongoRunnerPool).Value.Rent();

        lock (this._rentedRunnersByClientName)
        {
            if (this._rentedRunnersByClientName.TryGetValue(clientName, out var rentedRunners))
            {
                rentedRunners.Add(runner);
            }
            else
            {
                this._rentedRunnersByClientName[clientName] = [runner];
            }
        }

        return runner;
    }

    private static Lazy<MongoRunnerPool> CreateLazyMongoRunnerPool(string clientName)
    {
        return new Lazy<MongoRunnerPool>(CreatePool);
    }

    private static MongoRunnerPool CreatePool()
    {
        var options = new MongoRunnerOptions
        {
            Version = MongoVersion.V8,
            UseSingleNodeReplicaSet = true,
        };

        var version = Environment.GetEnvironmentVariable("WORKLEAP_EXTENSIONS_MONGO_EPHEMERAL_VERSION")?.Trim();
        if (int.TryParse(version, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedVersion))
        {
            options.Version = parsedVersion switch
            {
                6 => MongoVersion.V6,
                7 => MongoVersion.V7,
                8 => MongoVersion.V8,
                _ => options.Version
            };
        }

        var edition = Environment.GetEnvironmentVariable("WORKLEAP_EXTENSIONS_MONGO_EPHEMERAL_EDITION")?.Trim();
        if ("enterprise".Equals(edition, StringComparison.OrdinalIgnoreCase))
        {
            options.Edition = MongoEdition.Enterprise;
            options.AdditionalArguments = ["--storageEngine", "inMemory"];
        }

        var connectionTimeout = Environment.GetEnvironmentVariable("WORKLEAP_EXTENSIONS_MONGO_EPHEMERAL_CONNECTIONTIMEOUT")?.Trim();
        if (TimeSpan.TryParse(connectionTimeout, CultureInfo.InvariantCulture, out var parsedConnectionTimeout))
        {
            options.ConnectionTimeout = parsedConnectionTimeout;
        }

        return new MongoRunnerPool(options, maxRentalsPerRunner: 100);
    }

    public void Dispose()
    {
        lock (this._rentedRunnersByClientName)
        {
            foreach (var (clientName, rentedRunners) in this._rentedRunnersByClientName)
            {
                foreach (var rentedRunner in rentedRunners)
                {
                    LazyRunnerPools[clientName].Value.Return(rentedRunner);
                }
            }

            this._rentedRunnersByClientName.Clear();
        }
    }
}