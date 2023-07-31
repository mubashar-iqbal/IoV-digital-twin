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


namespace AnalyzerIngestor
{
    public static class AnalyzerIngestor
    {
        // ADT Instance
        private static readonly string adtInstanceUrl = Environment.GetEnvironmentVariable("ADT_SERVICE_URL");
        private static readonly HttpClient httpClient = new HttpClient();

        [FunctionName("IOTHubToAnalyzerADTFunction")]
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
                log.LogInformation($"Analyzer ADT client connection created!");

                //if we receive data
                if (eventGridEvent != null && eventGridEvent.Data != null)
                {
                    //Log the data
                    log.LogInformation(eventGridEvent.Data.ToString());

                    //Covert to json
                    JObject analyzerMessage = (JObject)JsonConvert.DeserializeObject(eventGridEvent.Data.ToString());

                    //Get device data from object
                    string analyzerId = (String)analyzerMessage["systemProperties"]["iothub-connection-device-id"];

                    double averageOfLastThreeReadings = (double)analyzerMessage["body"]["AverageOfLastThreeReadings"];
                    string compState = (string)analyzerMessage["body"]["ComputeState"];
                    string state = (string)analyzerMessage["body"]["FunctionalState"];

                    //Log the telemetry
                    log.LogInformation($"Device: {analyzerId} Functional State: {state}, Compute State: {compState}");

                    var updateAnalyzerTwinData = new JsonPatchDocument();
                    updateAnalyzerTwinData.AppendAdd("/FunctionalState", state);
                    updateAnalyzerTwinData.AppendAdd("/ComputeState", compState);
                    updateAnalyzerTwinData.AppendAdd("/AverageOfLastThreeReadings", averageOfLastThreeReadings);

                    await adtClient.UpdateDigitalTwinAsync(analyzerId, updateAnalyzerTwinData).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Error in Analyzer Ingest Function: {ex.Message}");
            }
        }
    }
}
