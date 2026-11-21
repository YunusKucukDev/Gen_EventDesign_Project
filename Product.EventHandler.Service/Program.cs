

using Product.EventHandler.Service.Services;
using Shared.Services.Abstraction;
using Shared.Services.Concrete;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<EventStoreBackgroundService>();

builder.Services.AddSingleton<IEventStoreService, EventStoreService>();
builder.Services.AddSingleton<IMongoDbService, MongoDbService>();

var host = builder.Build();
host.Run();
