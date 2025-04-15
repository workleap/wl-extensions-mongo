using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using MongoDB.Driver.Core.Events;
using Workleap.Extensions.Mongo.Telemetry;

namespace Workleap.Extensions.Mongo.ApplicationInsights;

internal sealed class ApplicationInsightsEventSubscriber : AggregatorEventSubscriber
{
    private const string TelemetryType = "mongodb";

    private readonly TelemetryClient _telemetryClient;
    private readonly MongoClientOptions _options;
    private readonly ConcurrentDictionary<int, ActivityAwareOperationHolder> _currentOperationsMap;

    public ApplicationInsightsEventSubscriber(TelemetryClient telemetryClient, MongoClientOptions options)
    {
        this._telemetryClient = telemetryClient;
        this._options = options;
        this._currentOperationsMap = new ConcurrentDictionary<int, ActivityAwareOperationHolder>();

        this.Subscribe<CommandStartedEvent>(this.CommandStartedEventHandler);
        this.Subscribe<CommandSucceededEvent>(this.CommandSucceededEventHandler);
        this.Subscribe<CommandFailedEvent>(this.CommandFailedEventHandler);
    }

    private void CommandStartedEventHandler(CommandStartedEvent evt)
    {
        if (this._options.Telemetry.IgnoredCommandNames.Contains(evt.CommandName))
        {
            return;
        }

        var name = evt.GetCollectionName() is { } collectionName
            ? $"{evt.DatabaseNamespace.DatabaseName}.{collectionName}.{evt.CommandName}"
            : $"{evt.DatabaseNamespace.DatabaseName}.{evt.CommandName}";

        var operation = this.StartActivityAwareDependencyOperation(name);

        operation.Telemetry.Type = TelemetryType;

        operation.Telemetry.Target = evt.ConnectionId?.ServerId?.EndPoint switch
        {
            IPEndPoint endPoint => $"{endPoint.Address}:{endPoint.Port}",
            DnsEndPoint endPoint => $"{endPoint.Host}:{endPoint.Port}",
            _ => "unknown",
        };

        if (this._options.Telemetry.CaptureCommandText)
        {
            operation.Telemetry.Data = evt.Command.ToString();
        }

        // Originating activity must be captured AFTER that the operation is created
        // Because ApplicationInsights SDK creates another intermediate Activity
        var originatingActivity = Activity.Current;

        this._currentOperationsMap.TryAdd(evt.RequestId, new ActivityAwareOperationHolder(operation, originatingActivity));
    }

    private void CommandSucceededEventHandler(CommandSucceededEvent evt)
    {
        if (!this._currentOperationsMap.TryRemove(evt.RequestId, out var operation))
        {
            return;
        }

        using (operation)
        {
            operation.Telemetry.Duration = evt.Duration;
            operation.Telemetry.Success = true;
        }
    }

    private void CommandFailedEventHandler(CommandFailedEvent evt)
    {
        if (!this._currentOperationsMap.TryRemove(evt.RequestId, out var operation))
        {
            return;
        }

        using (operation)
        {
            operation.Telemetry.Duration = evt.Duration;
            operation.Telemetry.Success = false;

            if (!operation.Telemetry.Properties.ContainsKey("Exception"))
            {
                operation.Telemetry.Properties.Add("Exception", evt.Failure.ToString());
            }
        }
    }

    private IOperationHolder<DependencyTelemetry> StartActivityAwareDependencyOperation(string name)
    {
        if (Activity.Current is { } activity && TracingHelper.IsMongoActivity(activity))
        {
            // When the current activity is our own Mongo activity created in our previous command tracing behavior,
            // then we use it to initialize the Application Insights operation.
            // The Application Insights SDK will take care of populating the parent-child relationship
            // and bridge the gap between our activity, its own internal activity and the AI operation telemetry.
            // Not doing that could cause some Application Insights AND OpenTelemetry spans to be orphans.
            var operation = this._telemetryClient.StartOperation<DependencyTelemetry>(activity);
            operation.Telemetry.Name = name;

            // Remove telemetry copied from our Mongo activity as we already have it in the AI operation
            foreach (var item in activity.Baggage.Union(activity.Tags))
            {
                operation.Telemetry.Properties.Remove(item.Key);
            }

            return operation;
        }

        return this._telemetryClient.StartOperation<DependencyTelemetry>(name);
    }

    private sealed class ActivityAwareOperationHolder : IOperationHolder<DependencyTelemetry>
    {
        private readonly IOperationHolder<DependencyTelemetry> _innerOperationHolder;
        private readonly Activity? _originatingActivity;

        public ActivityAwareOperationHolder(IOperationHolder<DependencyTelemetry> operation, Activity? originatingActivity)
        {
            this._innerOperationHolder = operation;
            this._originatingActivity = originatingActivity;
        }

        public DependencyTelemetry Telemetry => this._innerOperationHolder.Telemetry;

        public void Dispose()
        {
            var currentActivity = Activity.Current;

            if (currentActivity == this._originatingActivity)
            {
                this._innerOperationHolder.Dispose();
                return;
            }

            try
            {
                Activity.Current = this._originatingActivity;
                this._innerOperationHolder.Dispose();
            }
            finally
            {
                Activity.Current = currentActivity;
            }
        }
    }
}