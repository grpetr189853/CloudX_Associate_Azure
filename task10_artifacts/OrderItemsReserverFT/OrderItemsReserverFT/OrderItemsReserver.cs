using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static QueueToBlob.OrderItemsReserver;

namespace QueueToBlob
{
    public class OrderItemsReserver
    {
        //private readonly BlobServiceClient _blobServiceClient;
        //private readonly ILogger<OrderItemsReserver> _logger;

        //public OrderItemsReserver(BlobServiceClient blobServiceClient, ILogger<OrderItemsReserver> logger)
        //{
        //    //_blobServiceClient = blobServiceClient;
        //    string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings:BlobStorageConnection");
        //    try
        //    {
        //        // Создание клиента Blob Storage
        //        _blobServiceClient = new BlobServiceClient(connectionString, new BlobClientOptions
        //        {
        //            Retry =
        //            {
        //                MaxRetries = 3,
        //                Mode = RetryMode.Exponential,
        //                MaxDelay = TimeSpan.FromSeconds(30)
        //            },
        //            //Timeout = TimeSpan.FromMinutes(5) // Увеличение времени ожидания до 5 минут
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        //log.LogError(ex, "Error occurred while uploading order data to Blob Storage.");
        //        //return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        //    }


        //    _logger = logger;
        //}

        [FunctionName("OrderItemsReserver")]
        public async Task Run([ServiceBusTrigger("your-queue", Connection = "your-servicebus-connection-string")]Order order, ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {order}");
            log.LogInformation($"ServiceBusConnectionString: {Environment.GetEnvironmentVariable("ServiceBusConnectionString")}");
            try
            {
                // Create a JSON file with the order details
                string jsonContent = JsonConvert.SerializeObject(order);
                order.Id = Guid.NewGuid().ToString();
                log.LogInformation($"Received order {jsonContent}");
                // Upload the JSON file to Blob Storage
                await UploadJsonFileToBlobStorageAsync(order.Id, jsonContent, log);

                //_logger.LogInformation($"Order item with ID {order.Id} has been reserved.");
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error processing order item with ID {order.Id}.");

                // Fallback scenario: Send an email using Azure Logic Apps
                //await SendEmailUsingLogicAppsAsync(order);
            }
        }
        private async Task UploadJsonFileToBlobStorageAsync(string orderId, string jsonContent, ILogger log)
        {
            // Retry policy: Attempt file upload up to 3 times
            var retryCount = 0;
            const int maxRetries = 3;

            while (retryCount < maxRetries)
            {
                try
                {
                    //_blobServiceClient = blobServiceClient;
                    string connectionString = "your-storage-account-connection-string";//Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                    log.LogInformation($"Connection string to Blob Storage {connectionString}");
                    try
                    {
                        // Создание клиента Blob Storage
                        var _blobServiceClient = new BlobServiceClient(connectionString, new BlobClientOptions
                        {
                            Retry =
                                {
                                    MaxRetries = 3,
                                    Mode = RetryMode.Exponential,
                                    MaxDelay = TimeSpan.FromSeconds(30)
                                },
                            //Timeout = TimeSpan.FromMinutes(5) // Увеличение времени ожидания до 5 минут
                        });
                        // Get a reference to the container
                        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient("orders");

                        // Create the container if it doesn't exist
                        await containerClient.CreateIfNotExistsAsync();

                        // Upload the JSON file to the container
                        BlobClient blobClient = containerClient.GetBlobClient($"{orderId}.json");
                        await blobClient.UploadAsync(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonContent)));

                        log.LogInformation($"JSON file for order item with ID {orderId} has been uploaded to Blob Storage.");
                        return;

                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, "Error occurred while uploading order data to Blob Storage.");
                        await SendEmailUsingLogicAppsAsync(orderId);
                        log.LogInformation("Successfully sent an email");
                        //return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                    }
                }
                catch (Exception ex)
                {
                    retryCount++;
                    log.LogError(ex, $"Error uploading JSON file for order item with ID {orderId}. Retry attempt {retryCount}/{maxRetries}.");

                    if (retryCount == maxRetries)
                    {
                        await SendEmailUsingLogicAppsAsync(orderId);
                        log.LogInformation("Successfully sent an email");
                        throw;
                    }

                    // Add a delay before the next retry attempt
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }
        }

        private async Task SendEmailUsingLogicAppsAsync(string orderId)
        {
            var logicAppUrl = "your-logic-app-url";
            var emailData = new
            {
                To = "grpetr189853@gmail.com",
                Subject = $"Error processing order {orderId}",
                Body = $"An error occurred while processing order {orderId}. Please investigate."
            };

            // Get the API key or access token required by the Logic App
            var apiKey = Environment.GetEnvironmentVariable("LogicAppApiKey");

            using (var httpClient = new HttpClient())
            {
                // Add the API key or access token to the request headers
                httpClient.DefaultRequestHeaders.Add("x-functions-key", apiKey);

                var response = await httpClient.PostAsJsonAsync(logicAppUrl, emailData);
                response.EnsureSuccessStatusCode();
            }
        }
        //public class OrderItem
        //{
        //    public string Id { get; set; }
        //    public int Quantity { get; set; }
        //}
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

    }
}
