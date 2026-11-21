using EventStore.Client;
using Shared.Services.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Services.Concrete
{
    public class EventStoreService : IEventStoreService
    {

        EventStoreClientSettings GetEventStoreClientSettings(string connectionString = "esdb://localhost:2113?tls=false&tlsVerifyCert=false") => EventStoreClientSettings.Create(connectionString);
        EventStoreClient Client { get => new EventStoreClient(GetEventStoreClientSettings()); }



        public async Task AppendToStreamAsync(string streamName, IEnumerable<EventData> eventData)
            =>await Client.AppendToStreamAsync(
                streamName : streamName, 
                expectedState: StreamState.Any,
                eventData: eventData);


        public EventData GenerateEventData(object @event)
            => new EventData(
                eventId: Uuid.NewUuid(),
                type: @event.GetType().Name,
                data: Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(@event))
            );

        public async Task SubscribeToStreamAsync(string streamName, Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeard)
            => await Client.SubscribeToStreamAsync(
                streamName: streamName,
                start : FromStream.Start,
                eventAppeared: eventAppeard,
                subscriptionDropped: (subscription, reason, exception) =>
                {
                    Console.WriteLine($"Subscription dropped. Reason: {reason}. Exception: {exception?.Message}");
                }
            );
    }
}
