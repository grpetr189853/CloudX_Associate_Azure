using Azure.Messaging.ServiceBus;
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Newtonsoft.Json;

namespace Microsoft.eShopWeb.ApplicationCore.Services;

public class ServiceBusPublisher
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;

    public ServiceBusPublisher(string connectionString, string queueName)
    {
        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(queueName);
    }

    public async Task PublishMessageAsync(Order message)
    {
        string messageBody = JsonConvert.SerializeObject(message);

        // create a Service Bus message
        ServiceBusMessage sbMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageBody));

        // send the message to the queue
        await _sender.SendMessageAsync(sbMessage);
    }

    public async Task CloseAsync()
    {
        // await any in-flight messages to be sent
        await _sender.CloseAsync();

        // finally, dispose of the client
        await _client.DisposeAsync();
    }
}
