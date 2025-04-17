using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Workleap.Extensions.Mongo;

internal sealed class ValidateMongoClientOptions : IValidateOptions<MongoClientOptions>
{
    public ValidateOptionsResult Validate(string? name, MongoClientOptions options)
    {
        List<string> errors = [];

        ValidatePrimaryProperties(options, errors);
        ValidateIndexingProperties(options, errors);

        return errors.Count > 0 ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;
    }

    private static void ValidatePrimaryProperties(MongoClientOptions options, List<string> errors)
    {
        if (string.IsNullOrEmpty(options.ConnectionString))
        {
            errors.Add($"{nameof(options.ConnectionString)} is required.");
        }

        try
        {
            _ = new MongoUrl(options.ConnectionString);
        }
        catch (Exception ex)
        {
            errors.Add($"{nameof(options.ConnectionString)} is invalid: {ex.Message}");
        }

        if (string.IsNullOrEmpty(options.DefaultDatabaseName))
        {
            errors.Add($"{nameof(options.DefaultDatabaseName)} is required.");
        }
    }

    private static void ValidateIndexingProperties(MongoClientOptions options, List<string> errors)
    {
        if (string.IsNullOrEmpty(options.Indexing.DistributedLockName))
        {
            errors.Add($"{nameof(options.Indexing)}.{nameof(options.Indexing.DistributedLockName)} is required.");
        }

        if (options.Indexing.LockMaxLifetimeInSeconds <= 0)
        {
            errors.Add($"{nameof(options.Indexing)}.{nameof(options.Indexing.LockMaxLifetimeInSeconds)} must be greater than 0.");
        }

        if (options.Indexing.LockAcquisitionTimeoutInSeconds <= 0)
        {
            errors.Add($"{nameof(options.Indexing)}.{nameof(options.Indexing.LockAcquisitionTimeoutInSeconds)} must be greater than 0.");
        }
    }
}