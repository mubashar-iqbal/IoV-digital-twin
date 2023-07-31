using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalyzerVehicleSimulator
{
    public class AnalyzerVehicleTelemetry
    {
        public string id { get; set; }
        public string functionalState { get; set; }
        public double temperatureReading { get; set; }

        public double averageLastThreeReading { get; set; }
    }
}
