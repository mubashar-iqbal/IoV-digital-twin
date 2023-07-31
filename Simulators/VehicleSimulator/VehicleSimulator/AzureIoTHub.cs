using Azure.Messaging.EventHubs.Consumer;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;

namespace DeviceSimulator
{
    public static class AzureIoTHub
    {
        private static int taskDelay = 10 * 1000;

        private static string hubName = ConfigurationManager.AppSettings["hubName"].ToString();
        private static string hubSharedAccessKey = ConfigurationManager.AppSettings["hubSharedAccessKey"].ToString();

        private static string deviceName = ConfigurationManager.AppSettings["deviceName"].ToString();        
        private static string deviceSharedAccessKey = ConfigurationManager.AppSettings["deviceSharedAccessKey"].ToString();

        private static string iotHubConnectionString = @$"HostName={hubName}.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey={hubSharedAccessKey}";
        private static string deviceConnectionString = $"HostName={hubName}.azure-devices.net;DeviceId={deviceName};SharedAccessKey={deviceSharedAccessKey}";

        private static string deviceId = deviceName;

        public static async Task<string> CreateDeviceIdentityAsync(string deviceName)
        {
            var registryManager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);
            var device = new Device(deviceName);
            try
            {
                device = await registryManager.AddDeviceAsync(device);
            }
            catch (DeviceAlreadyExistsException)
            {
                device = await registryManager.GetDeviceAsync(deviceName);
            }

            return device.Authentication.SymmetricKey.PrimaryKey;
        }

        public static async Task SendDeviceToCloudMessageAsync(CancellationToken cancelToken)
        {
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString);
            string id = deviceId;

            double temperature = 70.0D;
            var rand = new Random();

            while (!cancelToken.IsCancellationRequested)
            {
                double currentTemperature = temperature + rand.NextDouble() * 4 - 3;

                var telemetryDataPoint = new
                {
                    TemperatureReading = currentTemperature,
                };
                var messageString = JsonSerializer.Serialize(telemetryDataPoint);
                var message = new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes(messageString))
                {
                    ContentType = "application/json",
                    ContentEncoding = "utf-8"
                };
                await deviceClient.SendEventAsync(message);
                Console.WriteLine($"{DateTime.Now} > Sending message: {messageString}");

                //Keep this value above 1000 to keep a safe buffer above the ADT service limits
                //See https://aka.ms/adt-limits for more info
                await Task.Delay(60000);
            }
        }

        public static async Task<string> ReceiveCloudToDeviceMessageAsync()
        {
            var oneSecond = TimeSpan.FromSeconds(1);
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString);

            while (true)
            {
                var receivedMessage = await deviceClient.ReceiveAsync();
                if (receivedMessage == null)
                {
                    await Task.Delay(oneSecond);
                    continue;
                }

                var messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                await deviceClient.CompleteAsync(receivedMessage);
                return messageData;
            }
        }

        public static async Task ReceiveMessagesFromDeviceAsync(CancellationToken cancelToken)
        {
            try
            {
                string eventHubConnectionString = await IotHubConnection.GetEventHubsConnectionStringAsync(iotHubConnectionString);
                await using var consumerClient = new EventHubConsumerClient(
                    EventHubConsumerClient.DefaultConsumerGroupName,
                    eventHubConnectionString);

                await foreach (PartitionEvent partitionEvent in consumerClient.ReadEventsAsync(cancelToken))
                {
                    if (partitionEvent.Data == null) continue;

                    string data = Encoding.UTF8.GetString(partitionEvent.Data.Body.ToArray());
                    Console.WriteLine($"Message received. Partition: {partitionEvent.Partition.PartitionId} Data: '{data}'");
                }
            }
            catch (TaskCanceledException) { } // do nothing
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading event: {ex}");
            }
        }
    }
}