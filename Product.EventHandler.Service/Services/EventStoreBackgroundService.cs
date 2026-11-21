using MongoDB.Driver;
using Shared.Events;
using Shared.Services.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Product.EventHandler.Service.Services
{
    public class EventStoreBackgroundService : BackgroundService
    {
        private readonly IEventStoreService _eventStoreService;
        private readonly IMongoDbService _mongoDbService;

        public EventStoreBackgroundService(IEventStoreService eventStoreService, IMongoDbService mongoDbService)
        {
            _eventStoreService = eventStoreService;
            _mongoDbService = mongoDbService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _eventStoreService.SubscribeToStreamAsync("product-stream",
                async (subscription, resolvedEvent, cancellationToken) =>
            {
                string eventType = resolvedEvent.Event.EventType;
                object @event = JsonSerializer.Deserialize(Encoding.UTF8.GetString(resolvedEvent.Event.Data.ToArray()), Assembly.Load("Shared").GetTypes().FirstOrDefault(t => t.Name == eventType));

                var productCollection = _mongoDbService.GetCollection<Shared.Models.Product>("Products");
                Shared.Models.Product? product = null;

                switch (@event)
                {
                    case NewProductAddedEvent e:
                        var hasProduct = await productCollection.Find(p => p.Id == e.ProductId).AnyAsync();

                        if (!hasProduct)
                        {
                            await productCollection.InsertOneAsync(new()
                            {
                                Id = e.ProductId,
                                ProductName = e.ProductName,
                                Count = e.InitialCount,
                                Price = e.InitialPrice,
                                IsAvaliable = e.IsAvailable
                            });
                        }
                        break;

                    case CountDecreasedEvent e:
                        product = await productCollection.Find(p => p.Id == e.ProductId).FirstOrDefaultAsync();
                        if (product != null)
                        {
                            product.Count -= e.DecrementAmount;
                            await productCollection.FindOneAndReplaceAsync(p => p.Id == e.ProductId, product);
                        }
                        break;

                    case CountIncreasedEvent e:
                        product = await productCollection.Find(p => p.Id == e.ProductId).FirstOrDefaultAsync();
                        if (product != null)
                        {
                            product.Count += e.IncrementAmount;
                            await productCollection.FindOneAndReplaceAsync(p => p.Id == e.ProductId, product);
                        }
                        break;

                    case PriceDecreasedEvent e:
                        product = await productCollection.Find(p => p.Id == e.ProductId).FirstOrDefaultAsync();
                        if (product != null)
                        {
                            product.Price -= e.DecreasedPrice;
                            await productCollection.FindOneAndReplaceAsync(p => p.Id == e.ProductId, product);
                        }
                        break;

                    case PriceIncreasedEvent e:
                        product = await productCollection.Find(p => p.Id == e.ProductId).FirstOrDefaultAsync();
                        if (product != null)
                        {
                            product.Price += e.IncreasedPrice;
                            await productCollection.FindOneAndReplaceAsync(p => p.Id == e.ProductId, product);

                        }
                        break;

                    case AvailabilityChangedEvent e:
                        product = await productCollection.Find(p => p.Id == e.ProductId).FirstOrDefaultAsync();
                        if (product != null)
                        {
                            product.IsAvaliable = e.IsAvaliable;
                            await productCollection.FindOneAndReplaceAsync(p => p.Id == e.ProductId, product);
                        }
                        break;

                    default:
                        Console.WriteLine($"Unhandled event type: {eventType}");
                        break;
                }
            });
        }
    }
}
