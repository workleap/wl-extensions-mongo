using Microsoft.Extensions.Options;
using Workleap.Extensions.Mongo.Telemetry;

namespace Workleap.Extensions.Mongo.ApplicationInsights;

internal sealed class ConfigureMongoClientOptions : IConfigureNamedOptions<MongoClientOptions>
{
    public void Configure(MongoClientOptions options)
    {
    }

    public void Configure(string? name, MongoClientOptions options)
    {
        var existingPostConfigureEventSubscribers = options.PostConfigureEventSubscribers;

        options.PostConfigureEventSubscribers = eventSubscribers =>
        {
            var openTelemetryEventSubscriberIdx = eventSubscribers.FindIndex(x => x is CommandTracingEventSubscriber);
            var appInsightsEventSubscriberIdx = eventSubscribers.FindIndex(x => x is ApplicationInsightsEventSubscriber);

            if (openTelemetryEventSubscriberIdx != -1 && appInsightsEventSubscriberIdx != -1)
            {
                // Make sure the Application Insights (AI) event subscriber is placed AFTER the OpenTelemetry (OTel) event subscriber.
                // Both AI and OTel are heavily activity-based (System.Diagnostics.Activity). Having AI's activity created after OTel's activity
                // ensures the span hierarchy is correct in AI and OTel when using both:
                // https://github.com/microsoft/ApplicationInsights-dotnet/blob/2.21.0/BASE/src/Microsoft.ApplicationInsights/TelemetryClientExtensions.cs#L368-L383
                // We did the same for MediatR in this PR: https://github.com/workleap/gsoft-extensions-mediatr/pull/21
                var appInsightsEventSubscriber = eventSubscribers[appInsightsEventSubscriberIdx];
                eventSubscribers.RemoveAt(appInsightsEventSubscriberIdx);
                eventSubscribers.Insert(openTelemetryEventSubscriberIdx + 1, appInsightsEventSubscriber);
            }

            existingPostConfigureEventSubscribers?.Invoke(eventSubscribers);
        };
    }
}