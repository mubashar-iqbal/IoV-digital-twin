// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Azure.Messaging.EventGrid;


//TO interact with Azure ADT
using Azure.DigitalTwins.Core;
using Azure.Identity;
using System.Net.Http;
using Azure.Core.Pipeline;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Azure;

namespace RSUDataIngestor
{
    public static class IoTHubToAzureDigitalTwinsFunction
    {
        // ADT Instance
        private static readonly string adtInstanceUrl = Environment.GetEnvironmentVariable("ADT_SERVICE_URL");
        private static readonly HttpClient httpClient = new HttpClient();

        [FunctionName("IoTHubToRsuADTFunction")]
        public static async Task Run([EventGridTrigger]EventGridEvent eventGridEvent, ILogger log)
        {
            log.LogInformation(eventGridEvent.Data.ToString());

            if (adtInstanceUrl == null) log.LogError("Application setting \"ADT_SERVICE_URL\" not set");

            try
            {
                //Managed Identity Credentials
                var cred = new DefaultAzureCredential();

                //Instantiate ADT Client
                var adtClient = new DigitalTwinsClient(new Uri(adtInstanceUrl), cred);

                // Log successful connection creation
                log.LogInformation($"RSU ADT client connection created!");

                //if we receive data
                if (eventGridEvent != null && eventGridEvent.Data != null)
                {
                    //Log the data
                    log.LogInformation(eventGridEvent.Data.ToString());

                    //Covert to json
                    JObject rsuMessage = (JObject)JsonConvert.DeserializeObject(eventGridEvent.Data.ToString());

                    //Get device data from object
                    string rsuId = (String)rsuMessage["systemProperties"]["iothub-connection-device-id"];

                    string rsuAddress = (string)rsuMessage["body"]["Address"];
                    string vehicleAddress = (string)rsuMessage["body"]["VehicleAddress"];
                    string analyzerAddress = (string)rsuMessage["body"]["AnalyzerVehicleAddress"];
                    string state = (string)rsuMessage["body"]["State"];
                    
                    //log.LogInformation($"{location}");

                    //Log the telemetry
                    log.LogInformation($"Device: {rsuId} Address is: {rsuAddress}, Vehicle Address: {vehicleAddress}, Analyzer Address: {analyzerAddress} Functional state is: {state}");

                    // Smart Contract or Blockchain code goes here


                    //Update Digital Twin from data from IoT Hub

                    var updateTwinData = new JsonPatchDocument();
                    updateTwinData.AppendAdd("/Address", rsuAddress);
                    updateTwinData.AppendAdd("/VehicleAddress", vehicleAddress);
                    updateTwinData.AppendAdd("/AnalyzerVehicleAddress", analyzerAddress);
                    updateTwinData.AppendAdd("/State", state);

                    await adtClient.UpdateDigitalTwinAsync(rsuId, updateTwinData).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {

                log.LogError($"Error in Vehicle Ingest Function: {e.Message}");
            }
        }
    }
}
