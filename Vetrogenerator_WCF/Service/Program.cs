using System;
using System.Configuration;
using System.Globalization;
using System.ServiceModel;
using Common;

namespace Service
{
    class Program
    {
        static void Main(string[] args)
        {
            double underPerformanceAlpha = ReadDouble("UnderPerformanceAlpha", 0.75);
            double yawMisalignThresholdDeg = ReadDouble("YawMisalignThresholdDeg", 15);
            double frequencyDeviationAbsHz = ReadDouble("FrequencyDeviationAbsHz", 0.5);
            double frequencySpikeThresholdHz = ReadDouble("FrequencySpikeThresholdHz", 0.2);

            WindTurbineService serviceInstance = new WindTurbineService();

            serviceInstance.OnTransferStarted += (s, e) =>
                Console.WriteLine($"[EVENT] OnTransferStarted | TurbineId={e.Meta.TurbineId} | File={e.Meta.FileName} | Rows={e.Meta.TotalRows} | Prenos u toku...");

            serviceInstance.OnSampleReceived += (s, e) =>
                Console.WriteLine($"[EVENT] OnSampleReceived | {e.TurbineId} | Red {e.Sample.RowIndex} | {e.Sample.Timestamp:yyyy-MM-dd HH:mm} | Wind={e.Sample.WindSpeedMs:F1} m/s | Power={e.Sample.PowerKW:F1} kW | Freq={e.Sample.GridFrequencyHz:F3} Hz");

            serviceInstance.OnTransferCompleted += (s, e) =>
                Console.WriteLine($"[EVENT] OnTransferCompleted | TurbineId={e.TurbineId} | Primljeno={e.TotalReceived} | Prenos završen.");

            serviceInstance.OnWarningRaised += (s, e) =>
                Console.WriteLine($"[EVENT] OnWarningRaised | {e.TurbineId} | Red {e.RowIndex} | {e.Message} | {e.RaisedAt:HH:mm:ss}");

            using (ServiceHost host = new ServiceHost(serviceInstance))
            {
                host.Open();
                Console.WriteLine("[Service] Vetrogenerator WCF servis pokrenut. Pritisnite ENTER za zaustavljanje.");
                Console.WriteLine($"[Service] Pragovi iz app.config: UnderPerformanceAlpha={underPerformanceAlpha}, YawMisalignThresholdDeg={yawMisalignThresholdDeg}, FrequencyDeviationAbsHz={frequencyDeviationAbsHz}, FrequencySpikeThresholdHz={frequencySpikeThresholdHz}");
                Console.ReadLine();
                host.Close();
            }
        }

        static double ReadDouble(string key, double fallback)
        {
            string raw = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(raw)) return fallback;
            return double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out double value) ? value : fallback;
        }
    }
}
