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
using Nethereum.Web3;
using Nethereum.ABI.FunctionEncoding.Attributes;
using System.Numerics;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace VehicleDataIngestor
{
    [FunctionOutput]
    public class GetTemperatureOutputDTO : IFunctionOutputDTO
    {
        [Parameter("int", "minTemp", 1)]
        public virtual BigInteger MinTemp { get; set; }
        [Parameter("int", "maxTemp", 2)]
        public virtual BigInteger MaxTemp { get; set; }
    }
    public static class IoTHubToAzureDigitalTwinsFunction
    {
        // ADT Instance
        private static readonly string adtInstanceUrl = Environment.GetEnvironmentVariable("ADT_SERVICE_URL");
        private static readonly HttpClient httpClient = new HttpClient();

        // Contract addresses
        private static string selfAddress = "0xc9D4a723C47F9E2Ee3Aa813D4384e3d0855BD715";
        private static string avAddress = "0x8f4410d9cD82b5D4108d2b6ADc95e1375e4c75eF";
        private static string sepoliaApiKey = "87deefc774f04134bb6afb66d6bf3cb5";


        [FunctionName("IoTHubToADTFunction")]
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
                log.LogInformation($"Vechile ADT client connection created!");

                //if we receive data
                if (eventGridEvent != null && eventGridEvent.Data != null)
                {
                    //Log the data
                    log.LogInformation(eventGridEvent.Data.ToString());

                    //Covert to json
                    JObject vehicleMessage = (JObject)JsonConvert.DeserializeObject(eventGridEvent.Data.ToString());

                    //Get device data from object
                    string vehicleId = (String)vehicleMessage["systemProperties"]["iothub-connection-device-id"];
                    int deviceCurrentTemperature = (int)vehicleMessage["body"]["TemperatureReading"];
                    string functionalState = "Functional";

                    //Log the telemetry
                    log.LogInformation($"Device: {vehicleId} Temperature is: {deviceCurrentTemperature}, Functional state is: {functionalState}");

                    // Smart Contract or Blockchain code goes here

                    // Get Temperature Range from Analyzer Vehicle
                    var web3 = new Web3($"https://sepolia.infura.io/v3/{sepoliaApiKey}");
                    
                    var avABI = @"[{""inputs"": [],""stateMutability"": ""nonpayable"",""type"": ""constructor""},
                    {""inputs"": [],""name"": ""getTempThresholds"",""outputs"": [{""internalType"": ""uint256"",""name"": """",""type"": ""uint256""},
                    {""internalType"": ""uint256"",""name"": """",""type"": ""uint256""}],""stateMutability"": ""view"",""type"": ""function""}]";

                    // Initialize AV Contract
                    var contract = web3.Eth.GetContract(avABI, avAddress);
                    var tempRange = contract.GetFunction("getTempThresholds");
                    
                    //Deserialize
                    var tempValues = await tempRange.CallDeserializingToObjectAsync<GetTemperatureOutputDTO>();

                    //Log the values
                    log.LogInformation($"Min temp: {tempValues.MinTemp}");
                    log.LogInformation($"Max temp: {tempValues.MaxTemp}");

                    int maxAvgTemp = (int)tempValues.MaxTemp;
                    int minAvgTemp = (int)tempValues.MinTemp;

                    if (deviceCurrentTemperature < minAvgTemp)
                    {
                        functionalState = "Non-functional";
                    }

                    if (deviceCurrentTemperature > maxAvgTemp)
                    {
                        functionalState = "Non-functional";
                    }

                    log.LogInformation($"Vehicle: {vehicleId} Temperature is: {deviceCurrentTemperature}, Vehicle state is: {functionalState}");

                    //Update Digital Twin
                    var updateTwinData = new JsonPatchDocument();
                    updateTwinData.AppendAdd("/Id", selfAddress);
                    updateTwinData.AppendAdd("/TemperatureReading", deviceCurrentTemperature);
                    updateTwinData.AppendAdd("/FunctionalState", functionalState);
                    await adtClient.UpdateDigitalTwinAsync(vehicleId, updateTwinData).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {

                log.LogError($"Error in Vehicle Ingest Function: {e.Message}");
            }
           


        }
    }
}
