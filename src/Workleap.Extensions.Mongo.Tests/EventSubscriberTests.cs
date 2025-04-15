using System.Text;
using Workleap.Extensions.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace Workleap.Extensions.Mongo.Tests;

public class EventSubscriberTests : BaseIntegrationTest<EventSubscriberTests.EventSubscriberFixture>
{
    public EventSubscriberTests(EventSubscriberFixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture, testOutputHelper)
    {
    }

    [Fact]
    public async Task EventSubscribers_Are_Executed_In_The_Right_Order()
    {
        _ = await this.Services.GetRequiredService<IMongoCollection<DummyDocument>>().FindAsync(Builders<DummyDocument>.Filter.Empty);
        var actualOutput = this.Services.GetRequiredService<StringBuilder>().ToString();
        this.Logger.LogInformation("Event subscribers call hierarchy: \r\n{Ouput}", actualOutput);

        const string expectedOutput = @"
CommandTracingEventSubscriber.CommandStartedEvent(find)
  ApplicationInsightsEventSubscriber.CommandStartedEvent(find)
    CommandLoggingEventSubscriber.CommandStartedEvent(find)
      UserDefinedEventSubscriber1.CommandStartedEvent(find)
        UserDefinedEventSubscriber2.CommandStartedEvent(find)
        UserDefinedEventSubscriber2.CommandSucceededEvent(find)
      UserDefinedEventSubscriber1.CommandSucceededEvent(find)
    CommandLoggingEventSubscriber.CommandSucceededEvent(find)
  ApplicationInsightsEventSubscriber.CommandSucceededEvent(find)
CommandTracingEventSubscriber.CommandSucceededEvent(find)";

        Assert.Equal(expectedOutput.Trim(), actualOutput.Trim());
    }

    public class EventSubscriberFixture : MongoFixture
    {
        public override IServiceCollection ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddSingleton<StringBuilder>();
            services.AddOptions<MongoClientOptions>().Configure<StringBuilder>(WrapRegisteredEventSubscribersWithStringBuilderOutput);
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IMongoEventSubscriberFactory, UserDefinedEventSubscriberFactory>());

            return services;
        }

        private static void WrapRegisteredEventSubscribersWithStringBuilderOutput(MongoClientOptions options, StringBuilder output)
        {
            var originalPostConfigureEventSubscribers = options.PostConfigureEventSubscribers;

            options.PostConfigureEventSubscribers = eventSubscribers =>
            {
                originalPostConfigureEventSubscribers?.Invoke(eventSubscribers);

                for (var i = 0; i < eventSubscribers.Count; i++)
                {
                    eventSubscribers[i] = new StringBuilderOutputEventSubscriber(eventSubscribers[i], output, i);
                }
            };
        }
    }

    [MongoCollection("dummy")]
    private sealed class DummyDocument : MongoDocument
    {
    }

    private sealed class StringBuilderOutputEventSubscriber : IEventSubscriber
    {
        private static readonly object LockObj = new object();

        private readonly IEventSubscriber _eventSubscriber;
        private readonly StringBuilder _output;
        private readonly int _indentLevel;

        public StringBuilderOutputEventSubscriber(IEventSubscriber eventSubscriber, StringBuilder output, int indentLevel)
        {
            this._eventSubscriber = eventSubscriber;
            this._output = output;
            this._indentLevel = indentLevel;
        }

        public bool TryGetEventHandler<TEvent>(out Action<TEvent> handler)
        {
            if (this._eventSubscriber.TryGetEventHandler(out handler))
            {
                var originalHandler = handler;
                handler = evt => this.Something(evt, originalHandler);
            }

            return handler != null;
        }

        private void Something<TEvent>(TEvent evt, Action<TEvent> originalHandler)
        {
            var commandName = GetCommandName(evt);
            var indent = new string(' ', this._indentLevel * 2);

            if (!MongoTelemetryOptions.DefaultIgnoredCommandNames.Contains(commandName))
            {
                lock (LockObj)
                {
                    this._output.AppendLine($"{indent}{this._eventSubscriber.GetType().Name}.{typeof(TEvent).Name}({commandName})");
                }
            }

            originalHandler(evt);
        }

        private static string GetCommandName<TEvent>(TEvent evt) => evt switch
        {
            CommandStartedEvent x => x.CommandName,
            CommandSucceededEvent x => x.CommandName,
            CommandFailedEvent x => x.CommandName,
            _ => string.Empty,
        };
    }

    private sealed class UserDefinedEventSubscriber1 : AggregatorEventSubscriber
    {
        public UserDefinedEventSubscriber1()
        {
            this.Subscribe<CommandStartedEvent>(_ => { });
            this.Subscribe<CommandSucceededEvent>(_ => { });
            this.Subscribe<CommandFailedEvent>(_ => { });
        }
    }

    private sealed class UserDefinedEventSubscriber2 : AggregatorEventSubscriber
    {
        public UserDefinedEventSubscriber2()
        {
            this.Subscribe<CommandStartedEvent>(_ => { });
            this.Subscribe<CommandSucceededEvent>(_ => { });
            this.Subscribe<CommandFailedEvent>(_ => { });
        }
    }

    private sealed class UserDefinedEventSubscriberFactory : IMongoEventSubscriberFactory
    {
        public IEnumerable<IEventSubscriber> CreateEventSubscribers(string clientName)
        {
            yield return new UserDefinedEventSubscriber1();
            yield return new UserDefinedEventSubscriber2();
        }
    }
}