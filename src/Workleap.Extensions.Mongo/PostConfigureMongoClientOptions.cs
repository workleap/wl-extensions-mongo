using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Workleap.Extensions.Mongo;

internal sealed class PostConfigureMongoClientOptions(IConfiguration? configuration = null) : IPostConfigureOptions<MongoClientOptions>
{
    public void PostConfigure(string? name, MongoClientOptions options)
    {
        var isConfiguringDefaultClient = name == MongoDefaults.ClientName;
        if (isConfiguringDefaultClient)
        {
            this.BindConfiguration(options);
        }

        TryAssignDefaultDatabaseNameFromConnectionString(options);
    }

    private void BindConfiguration(MongoClientOptions options)
    {
        if (configuration == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(options.ConnectionString))
        {
            var mongoSection = configuration.GetSection(MongoClientOptions.SectionName);
            if (mongoSection.Exists() && mongoSection[nameof(MongoClientOptions.ConnectionString)] is { } connectionString1)
            {
                options.ConnectionString = connectionString1;
            }
            else if (configuration.GetConnectionString(MongoClientOptions.SectionName) is { } connectionString2)
            {
                options.ConnectionString = connectionString2;
            }
        }

        if (string.IsNullOrEmpty(options.DefaultDatabaseName))
        {
            var mongoSection = configuration.GetSection(MongoClientOptions.SectionName);
            if (mongoSection.Exists() && mongoSection[nameof(MongoClientOptions.DefaultDatabaseName)] is { } defaultDatabaseName)
            {
                options.DefaultDatabaseName = defaultDatabaseName;
            }
        }
    }

    private static void TryAssignDefaultDatabaseNameFromConnectionString(MongoClientOptions options)
    {
        if (string.IsNullOrEmpty(options.ConnectionString) || !string.IsNullOrEmpty(options.DefaultDatabaseName))
        {
            return;
        }

        try
        {
            var mongoUrl = new MongoUrl(options.ConnectionString);
            if (!string.IsNullOrEmpty(mongoUrl.DatabaseName))
            {
                options.DefaultDatabaseName = mongoUrl.DatabaseName;
            }
        }
        catch
        {
            // This will be catched by the options validator which runs after this
        }
    }
}