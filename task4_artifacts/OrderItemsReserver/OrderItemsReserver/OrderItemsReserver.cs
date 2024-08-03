using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using System.Text.Json;
using Azure.Core;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;


namespace OrderItemsReserver
{
    public static class OrderItemsReserver
    {
        [FunctionName("OrderItemsReserver")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            dynamic data = JsonConvert.DeserializeObject<List<JObject>>(requestBody);

            // Generate an order request
            var orderRequest = new
            {
                OrderId = Guid.NewGuid().ToString(),
                Items = data
            };

            string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings:BlobStorageConnection");
            log.LogInformation(connectionString);
            try
            {
                // Создание клиента Blob Storage
                var blobServiceClient = new BlobServiceClient(connectionString, new BlobClientOptions
                {
                    Retry =
            {
                MaxRetries = 3,
                Mode = RetryMode.Exponential,
                MaxDelay = TimeSpan.FromSeconds(30)
            },
                    //Timeout = TimeSpan.FromMinutes(5) // Увеличение времени ожидания до 5 минут
                });
                var blobContainerClient = blobServiceClient.GetBlobContainerClient("orders");

                // Сериализовать данные заказа в формат JSON
                var serializerSettings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore
                };

                string orderJson = JsonConvert.SerializeObject(orderRequest, serializerSettings);
                //var orderJson = System.Text.Json.JsonSerializer.Serialize(orderRequest, new JsonSerializerOptions
                //{
                //    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                //});

                // Загрузить сериализованные данные в Blob Storage
                var blobClient = blobContainerClient.GetBlobClient($"{orderRequest.OrderId}.json");
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(orderJson)))
                {
                    var res = await blobClient.UploadAsync(stream);
                    return new OkObjectResult(res);
                }


            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error occurred while uploading order data to Blob Storage.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}