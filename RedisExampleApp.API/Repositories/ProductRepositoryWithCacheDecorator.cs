using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using RedisExampleApp.API.Model;
using RedisExampleApp.Cache;
using StackExchange.Redis;

namespace RedisExampleApp.API.Repository;

public class ProductRepositoryWithCacheDecorator : IProductRepository
{
    private const string productKey = "productsCache";
    private readonly IProductRepository _productRepository;
    private readonly RedisService _redisService;
    private readonly IDatabase _cacheRepository;

    public ProductRepositoryWithCacheDecorator(IProductRepository productRepository, RedisService redisService)
    {
        _productRepository = productRepository;
        _redisService = redisService;
        _cacheRepository = _redisService.GetDatabase(1);
    }

    public async Task<List<Product>> GetAsync() //Cache'den veriyi getirme işlemi
    {
        if (!await _cacheRepository.KeyExistsAsync(productKey)) //Cache'de veri yoksa
        {
            return await LoadToCacheFromDbAsync(); //Databaseden veriyi cache'e yükle
        }
        
        var products = new List<Product>();
        var cacheProducts = await _cacheRepository.HashGetAllAsync(productKey); //Cache'den veriyi al
        foreach (var item in cacheProducts.ToList()) //Cache'den alınan veriyi listeye ekle
        {
            var product = JsonSerializer.Deserialize<Product>(item.Value); 
            products.Add(product);
        }
        return products;
    }

    public async Task<Product> GetByIdAsync(int id) //Cache'den veri getirme işlemi
    {
        if (_cacheRepository.KeyExists(productKey)) //Cache'de veri varsa
        {
            var product = await _cacheRepository.HashGetAsync(productKey, id); //Cache'den veriyi al
            return product.HasValue ? JsonSerializer.Deserialize<Product>(product) : null; //Cache'den alınan veriyi dön
        }
        var products = await LoadToCacheFromDbAsync(); //Cache'de veri yoksa databaseden veriyi cache'e yükle
        return products.FirstOrDefault(x => x.Id == id); 
    } 

    public async Task<Product> CreateAsync(Product product) //Cache ve Database'e yeni veri oluşturma işlemi
    {
        var newProduct = await _productRepository.CreateAsync(product); //Databasede yeni veri oluştur
        if (await _cacheRepository.KeyExistsAsync(productKey)) //Cache'de veri varsa
        {
            await _cacheRepository.HashSetAsync(productKey, product.Id, JsonSerializer.Serialize(newProduct)); //Cache'e yeni veriyi ekle
        }
        return newProduct;
    }

    private async Task<List<Product>> LoadToCacheFromDbAsync() //Databaseden veriyi cache'e yükleme işlemi
    {
        var products = await _productRepository.GetAsync();
        
        products.ForEach(p =>
        {
            _cacheRepository.HashSetAsync(productKey, p.Id, JsonSerializer.Serialize(p));
        });
        
        return products;
    } 
}