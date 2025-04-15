using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Workleap.Extensions.Mongo.ApplicationInsights;

public static class MongoBuilderExtensions
{
    public static MongoBuilder AddApplicationInsights(this MongoBuilder builder)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IMongoEventSubscriberFactory, ApplicationInsightsEventSubscriberFactory>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<MongoClientOptions>, ConfigureMongoClientOptions>());

        return builder;
    }
}