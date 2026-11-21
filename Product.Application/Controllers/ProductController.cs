using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Product.Application.Models.ViewModels;
using Shared.Events;
using Shared.Services.Abstraction;
using System.Threading.Tasks;

namespace Product.Application.Controllers
{
    public class ProductController : Controller
    {
        private readonly IEventStoreService _eventStoreService;
        private readonly IMongoDbService _mongoDbService;

        public ProductController(IEventStoreService eventStoreService, IMongoDbService mongoDbService)
        {
            _eventStoreService = eventStoreService;
            _mongoDbService = mongoDbService;
        }

        public async Task<IActionResult> Index()
        {
            var productCollection = _mongoDbService.GetCollection<Shared.Models.Product>("Products");
            var products = await (productCollection.Find(_ => true)).ToListAsync();
            return View(products);
        }

        public IActionResult CreateProduct()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct(CreateProductVM model)
        {
            NewProductAddedEvent newProductAddedEvent = new NewProductAddedEvent
            {
                ProductId = Guid.NewGuid().ToString(),
                ProductName = model.ProductName,
                InitialCount = model.Count,
                IsAvailable = model.IsAvaliable,
                InitialPrice = model.Price
            };

            await _eventStoreService.AppendToStreamAsync("product-stream", new[] { _eventStoreService.GenerateEventData(newProductAddedEvent) });

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(string ProductId)
        {
            var productCollection = _mongoDbService.GetCollection<Shared.Models.Product>("Products");
            var product = await productCollection.Find(p => p.Id == ProductId).FirstOrDefaultAsync();
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> CountUpdate(Shared.Models.Product model)
        {
            var productCollection = _mongoDbService.GetCollection<Shared.Models.Product>("Products");
            var product = await productCollection.Find(p => p.Id == model.Id).FirstOrDefaultAsync();

            if (product.Count > model.Count)
            {
                CountDecreasedEvent countDecreasedEvent = new CountDecreasedEvent
                {
                    ProductId = product.Id,
                    DecrementAmount = model.Count
                };
                await _eventStoreService.AppendToStreamAsync("product-stream", new[] { _eventStoreService.GenerateEventData(countDecreasedEvent) });
            }
            else if (product.Count < model.Count)
            {
                CountIncreasedEvent countIncreasedEvent = new CountIncreasedEvent
                {
                    ProductId = product.Id,
                    IncrementAmount = model.Count
                };
                await _eventStoreService.AppendToStreamAsync("product-stream", new[] { _eventStoreService.GenerateEventData(countIncreasedEvent) });
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> PriceUpdate(Shared.Models.Product model)
        {
            var productCollection = _mongoDbService.GetCollection<Shared.Models.Product>("Products");
            var product = await productCollection.Find(p => p.Id == model.Id).FirstOrDefaultAsync();

            if (product.Price > model.Price)
            {
                PriceDecreasedEvent priceDecreasedEvent = new()
                {
                    ProductId = product.Id,
                    DecreasedPrice = model.Price
                };
                await _eventStoreService.AppendToStreamAsync("product-stream", new[] { _eventStoreService.GenerateEventData(priceDecreasedEvent) });
            }

            else if (product.Price < model.Price)
            {
                PriceIncreasedEvent priceIncreasedEvent = new()
                {
                    ProductId = product.Id,
                    IncreasedPrice = model.Price
                };
                await _eventStoreService.AppendToStreamAsync("product-stream", new[] { _eventStoreService.GenerateEventData(priceIncreasedEvent) });
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AvaliableUpdate(Shared.Models.Product model)
        {
            var productCollection = _mongoDbService.GetCollection<Shared.Models.Product>("Products");
            var product = await productCollection.Find(p => p.Id == model.Id).FirstOrDefaultAsync();

            if (product.IsAvaliable != model.IsAvaliable)
            {
                AvailabilityChangedEvent availabilityChangedEvent = new()
                {
                    ProductId = product.Id,
                    IsAvaliable = model.IsAvaliable
                };
                await _eventStoreService.AppendToStreamAsync("product-stream", new[] { _eventStoreService.GenerateEventData(availabilityChangedEvent) });
            }
            return RedirectToAction("Index");
        }
    }
}
