using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace Workleap.Extensions.Mongo;

public sealed class MongoClientOptions
{
    public const string SectionName = "Mongo";

    public MongoClientOptions()
    {
        this.Indexing = new MongoIndexingOptions();
        this.Telemetry = new MongoTelemetryOptions();
        this.CommandPerformanceAnalysis = new MongoCommandPerformanceAnalysisOptions();
    }

    public string ConnectionString { get; set; } = string.Empty;

    public string DefaultDatabaseName { get; set; } = string.Empty;

    public Action<MongoClientSettings>? MongoClientSettingsConfigurator { get; set; }

    public MongoIndexingOptions Indexing { get; }

    public MongoTelemetryOptions Telemetry { get; }

    public MongoCommandPerformanceAnalysisOptions CommandPerformanceAnalysis { get; }

    internal Action<List<IEventSubscriber>>? PostConfigureEventSubscribers { get; set; }
}

public sealed class MongoIndexingOptions
{
    public string DistributedLockName { get; set; } = "mongo-indexing";

    public int LockMaxLifetimeInSeconds { get; set; } = 300;

    public int LockAcquisitionTimeoutInSeconds { get; set; } = 60;
}

public sealed class MongoTelemetryOptions
{
    internal static readonly string[] DefaultIgnoredCommandNames =
    [
        // These commands would generate a lot of noise in the instrumentation output.
        "isMaster", "buildInfo", "saslStart", "saslContinue", "getLastError", "getMore", "listIndexes", "ping",
    ];

    public MongoTelemetryOptions()
    {
        this.IgnoredCommandNames = new HashSet<string>(DefaultIgnoredCommandNames, StringComparer.Ordinal);
    }

    public bool CaptureCommandText { get; set; }

    public ISet<string> IgnoredCommandNames { get; }

    public bool CaptureDiagnosticEvents { get; set; }
}

public sealed class MongoCommandPerformanceAnalysisOptions
{
    public bool EnableCollectionScanDetection { get; set; }

    internal bool IsPerformanceAnalysisEnabled
    {
        get => this.EnableCollectionScanDetection;
    }
}