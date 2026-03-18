using Microsoft.Extensions.Logging;

namespace Workleap.Extensions.Mongo;

// High-performance logging to prevent too many allocations
// https://docs.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator
internal static partial class LoggingExtensions
{
    // IndexProcessor
    [LoggerMessage(1, LogLevel.Information, "Skipping {DocumentType} index {IndexName} as it is already up-to-date on the database {DatabaseName}")]
    public static partial void SkippingUpToDateIndex(this ILogger logger, string documentType, string indexName, string databaseName);

    [LoggerMessage(2, LogLevel.Information, "Dropping {DocumentType} index {IndexName} as its definition has changed on the database {DatabaseName}")]
    public static partial void DroppingOutdatedIndex(this ILogger logger, string documentType, string indexName, string databaseName);

    [LoggerMessage(3, LogLevel.Information, "Dropping {DocumentType} index {IndexName} as it is not referenced in the code anymore on the database {DatabaseName}")]
    public static partial void DroppingOrphanedIndex(this ILogger logger, string documentType, string indexName, string databaseName);

    [LoggerMessage(4, LogLevel.Information, "Creating {DocumentType} index {IndexName} for the first time on the database {DatabaseName}")]
    public static partial void CreatingCompletelyNewIndex(this ILogger logger, string documentType, string indexName, string databaseName);

    [LoggerMessage(5, LogLevel.Information, "Creating {DocumentType} index {IndexName} after dropping an older version on the database {DatabaseName}")]
    public static partial void CreatingUpdatedIndex(this ILogger logger, string documentType, string indexName, string databaseName);

    // MongoLoggingEventSubscriber
    [LoggerMessage(6, LogLevel.Debug, "Executing mongo command {MongoCommandName}:{MongoRequestId} {MongoCommandJson}")]
    public static partial void CommandStartedSensitive(this ILogger logger, string mongoCommandName, int mongoRequestId, string mongoCommandJson);

    [LoggerMessage(7, LogLevel.Debug, "Executing mongo command {MongoCommandName}:{MongoRequestId}")]
    public static partial void CommandStartedNonSensitive(this ILogger logger, string mongoCommandName, int mongoRequestId);

    [LoggerMessage(8, LogLevel.Debug, "Successfully executed mongo command {MongoCommandName}:{MongoRequestId} in {MongoCommandDuration} seconds")]
    public static partial void CommandSucceeded(this ILogger logger, string mongoCommandName, int mongoRequestId, double mongoCommandDuration);

    [LoggerMessage(EventId = 9, Message = "Failed to execute mongo command {MongoCommandName}:{MongoRequestId} in {MongoCommandDuration} seconds")]
    public static partial void CommandFailed(this ILogger logger, LogLevel logLevel, Exception? ex, string mongoCommandName, int mongoRequestId, double mongoCommandDuration);

    // MongoDistributedLock
    [LoggerMessage(10, LogLevel.Debug, "Attempting to acquire distributed lock {LockId} by owner {OwnerId}")]
    public static partial void AcquiringDistributedLock(this ILogger logger, string lockId, string ownerId);

    [LoggerMessage(11, LogLevel.Information, "Distributed lock {LockId} has been acquired by owner {OwnerId}")]
    public static partial void DistributedLockAcquired(this ILogger logger, string lockId, string ownerId);

    [LoggerMessage(12, LogLevel.Debug, "Distributed lock {LockId} has not been acquired by owner {OwnerId}")]
    public static partial void DistributedLockNotAcquired(this ILogger logger, string lockId, string ownerId);

    [LoggerMessage(13, LogLevel.Information, "Distributed lock {LockId} has been released by owner {OwnerId}")]
    public static partial void DistributedLockReleased(this ILogger logger, string lockId, string ownerId);

    // CommandPerformanceAnalyzer
    [LoggerMessage(14, LogLevel.Warning, "Collection scan detected on command {MongoRequestId}")]
    public static partial void CollectionScanDetected(this ILogger logger, int mongoRequestId);

    // SearchIndexCreator / SearchIndexDeleter
    [LoggerMessage(15, LogLevel.Information, "Skipping {DocumentType} search index {IndexName} as it is already up-to-date on the database {DatabaseName}")]
    public static partial void SkippingUpToDateSearchIndex(this ILogger logger, string documentType, string indexName, string databaseName);

    [LoggerMessage(16, LogLevel.Information, "Creating {DocumentType} search index {IndexName} for the first time on the database {DatabaseName}")]
    public static partial void CreatingNewSearchIndex(this ILogger logger, string documentType, string indexName, string databaseName);

    [LoggerMessage(17, LogLevel.Information, "Updating {DocumentType} search index {IndexName} as its definition has changed on the database {DatabaseName}")]
    public static partial void UpdatingSearchIndex(this ILogger logger, string documentType, string indexName, string databaseName);

    [LoggerMessage(18, LogLevel.Information, "Dropping orphaned search index {IndexName} on collection {CollectionName} on the database {DatabaseName}")]
    public static partial void DroppingOrphanedSearchIndex(this ILogger logger, string indexName, string collectionName, string databaseName);

    [LoggerMessage(19, LogLevel.Warning, "Atlas Search is not available on the database {DatabaseName}. Skipping search index management.")]
    public static partial void AtlasSearchNotAvailable(this ILogger logger, Exception exception, string databaseName);
}