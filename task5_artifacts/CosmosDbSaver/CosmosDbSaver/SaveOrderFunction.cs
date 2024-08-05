using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;

public class Address // ValueObject
{
    public string Street { get; private set; }

    public string City { get; private set; }

    public string State { get; private set; }

    public string Country { get; private set; }

    public string ZipCode { get; private set; }

#pragma warning disable CS8618 // Required by Entity Framework
    private Address() { }

    public Address(string street, string city, string state, string country, string zipcode)
    {
        Street = street;
        City = city;
        State = state;
        Country = country;
        ZipCode = zipcode;
    }
}

public class CatalogItemOrdered // ValueObject
{
    public CatalogItemOrdered(int catalogItemId, string productName, string pictureUri)
    {
        CatalogItemId = catalogItemId;
        ProductName = productName;
        PictureUri = pictureUri;
    }

#pragma warning disable CS8618 // Required by Entity Framework
    private CatalogItemOrdered() { }

    public int CatalogItemId { get; private set; }
    public string ProductName { get; private set; }
    public string PictureUri { get; private set; }
}

public class OrderItem
{ 
    public CatalogItemOrdered ItemOrdered { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int Units { get; private set; }

#pragma warning disable CS8618 // Required by Entity Framework
    private OrderItem() { }

    public OrderItem(CatalogItemOrdered itemOrdered, decimal unitPrice, int units)
    {
        ItemOrdered = itemOrdered;
        UnitPrice = unitPrice;
        Units = units;
    }
}

public class OrderRequest
{
    public string OrderId { get; set; }
    public string id { get; set; }
    public List<OrderItem> items { get; set; }
    public decimal TotalPrice { get; set; }
    public Address Address { get; set; }
}

public class Order
{
    public string Id { get; set; }
    public string BuyerId { get; private set; }
    public DateTimeOffset OrderDate { get; private set; } = DateTimeOffset.Now;
    public List<OrderItem> OrderItems { get; set; }
    public Address ShipToAddress { get; private set; }
    public decimal Total()
    {
        var total = 0m;
        foreach (var item in OrderItems)
        {
            total += item.UnitPrice * item.Units;
        }
        return total;
    }
}

public class CosmosDbOrderRepository
{
    //private readonly CosmosClient _cosmosClient;
    //private readonly Microsoft.Azure.Cosmos.Container _container;
    private readonly CosmosClient _cosmosClient;
    private readonly Microsoft.Azure.Cosmos.Container _container;
    private readonly string _databaseId;
    private readonly string _containerId;

    public CosmosDbOrderRepository(string cosmosDbEndpoint, string cosmosDbKey)
    {
        _cosmosClient = new CosmosClient(
            cosmosDbEndpoint,
            cosmosDbKey
            //new CosmosClientOptions
            //{
            //    SerializerOptions = new CosmosSerializationOptions { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase },
            //});
            );
        _container = _cosmosClient.GetContainer("orders", "orders");
    }

    public async Task SaveOrderAsync(Order order)
    {
        try
        {
            //await _container.CreateItemAsync<List<Order>>(orders, new PartitionKey("replace_with_new_partition_key_value"));
            try
            {
                // Используйте OrderId в качестве partition key
                var order_ = new OrderRequest
                {
                    OrderId = Guid.NewGuid().ToString(),
                    id = Guid.NewGuid().ToString(),
                    items = order.OrderItems,
                    TotalPrice = order.Total(),
                    Address = order.ShipToAddress
                };
                await _container.CreateItemAsync(order_, new PartitionKey(order_.OrderId));
            }
            catch (Exception ex)
            {
                // Обработка ошибок
                Console.WriteLine($"Error saving order: {ex.Message}");
            }
        }
        catch (CosmosException ex)
        {
            // Handle any exceptions that may occur during the operation
            Console.WriteLine($"Error saving order: {ex.Message}");
            throw;
        }
    }
}

public static class SaveOrderFunction
{
    [FunctionName("SaveOrder")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        // Read the order data from the request body
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        log.LogInformation($"Processed order request {requestBody}");
        var orderRequest = JsonConvert.DeserializeObject<Order>(requestBody);
        log.LogInformation($"Processed order request {orderRequest}");
        // Save the order to Cosmos DB
        var cosmosDbEndpoint = Environment.GetEnvironmentVariable("CosmosDbEndpoint");
        var cosmosDbKey = Environment.GetEnvironmentVariable("CosmosDbKey");
        log.LogInformation($"CosmosDB endpoint {cosmosDbEndpoint}");
        log.LogInformation($"CosmosDB dbKey {cosmosDbKey}");
        var repository = new CosmosDbOrderRepository(cosmosDbEndpoint, cosmosDbKey);
        log.LogInformation($"CosmosDB reposiory { repository}");
        await repository.SaveOrderAsync(orderRequest);

        return new OkResult();
    }
}