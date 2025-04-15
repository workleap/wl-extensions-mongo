using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Options;
using MongoDB.Driver.Core.Events;

namespace Workleap.Extensions.Mongo.ApplicationInsights;

internal sealed class ApplicationInsightsEventSubscriberFactory : IMongoEventSubscriberFactory
{
    private readonly IOptionsMonitor<MongoClientOptions> _optionsMonitor;
    private readonly TelemetryClient? _telemetryClient;

    public ApplicationInsightsEventSubscriberFactory(IOptionsMonitor<MongoClientOptions> optionsMonitor, TelemetryClient? telemetryClient = null)
    {
        this._telemetryClient = telemetryClient;
        this._optionsMonitor = optionsMonitor;
    }

    public IEnumerable<IEventSubscriber> CreateEventSubscribers(string clientName)
    {
        if (this._telemetryClient != null)
        {
            var options = this._optionsMonitor.Get(clientName);
            yield return new ApplicationInsightsEventSubscriber(this._telemetryClient, options);
        }
    }
}